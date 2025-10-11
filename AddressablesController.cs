using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesController : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject _treePrefab;


    public static AddressablesController _Instance;
    public bool[,] _IsChunkLoadedToScene { get; private set; }
    public List<AsyncOperationHandle<GameObject>>[,] _HandlesForSpawned { get; private set; }
    public List<GameObject>[,] _NpcListForChunk { get; private set; }
    
    #region Method Parameters For Optimization
    private List<AssetReferenceGameObject> _objectsWillBeSpawned;
    private List<Vector3> _objectPositionsWillBeSpawned;
    #endregion

    private void Awake()
    {
        _Instance = this;
        _NpcListForChunk = new List<GameObject>[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
        _IsChunkLoadedToScene = new bool[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
        _objectsWillBeSpawned = new List<AssetReferenceGameObject>();
    }
    private void Start()
    {
        _HandlesForSpawned = new List<AsyncOperationHandle<GameObject>>[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
    }
    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Q))
            _currentTreeHandle = _treePrefab.InstantiateAsync();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_currentTreeHandle.IsValid())
            {
                Addressables.ReleaseInstance(_currentTreeHandle);
                _currentTreeHandle = default;
            }
        }*/
    }

    public void UnloadTerrainObjects(int x, int y)
    {
        if (!_IsChunkLoadedToScene[x, y]) return;
        if (_HandlesForSpawned[x, y] == null) return;

        _IsChunkLoadedToScene[x, y] = false;
        for (int i = 0; i < _HandlesForSpawned[x, y].Count; i++)
        {
            var handle = _HandlesForSpawned[x, y][i];
            if (handle.IsDone)
            {
                GameObject obj = handle.Result;
                if (obj != null)
                {
                    obj.SetActive(false);
                }
                Addressables.ReleaseInstance(handle);
            }
            else
            {

                handle.Completed += (h) =>
                {
                    GameObject obj = handle.Result;
                    if (obj != null)
                    {
                        obj.SetActive(false); 
                    }
                    Addressables.ReleaseInstance(h); 
                };
            }
        }
        _HandlesForSpawned[x, y].Clear();
    }
    public void LoadTerrainObjects(int x, int y)
    {
        if (_IsChunkLoadedToScene[x, y]) return;

        _IsChunkLoadedToScene[x, y] = true;
        SetAdressablesForSpawn(x, y);
        for (int i = 0; i < _objectsWillBeSpawned.Count; i++)
        {
            SpawnObj(x, y, i);
        }
    }
    private void SetAdressablesForSpawn(int x, int y)
    {
        _objectsWillBeSpawned = GameManager._Instance._ObjectsInChunk[x, y];
        _objectPositionsWillBeSpawned = GameManager._Instance._ObjectPositionsInChunk[x, y];
    }
    private void SpawnObj(int x, int y, int i)
    {
        if (_HandlesForSpawned[x, y] == null)
            _HandlesForSpawned[x, y] = new List<AsyncOperationHandle<GameObject>>();

        Vector3 pos = _objectPositionsWillBeSpawned[i]; //for action buffer
        _objectsWillBeSpawned[i].InstantiateAsync().Completed += (handle) =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded) return;
            GameObject obj = handle.Result;
            obj.transform.position = pos;
            GameManager._Instance.SetTerrainLinks(obj);
            _HandlesForSpawned[x, y].Add(handle);
            handle.Result.GetComponent<CarriableObject>()._Handle = handle;
            handle.Result.GetComponent<CarriableObject>()._Chunk = new Vector2Int(x, y);
        };
    }

    public void SpawnNpcs(int x, int y)
    {
        if (_NpcListForChunk[x, y] == null) return;

        int count = _NpcListForChunk[x, y].Count;
        for (int i = 0; i < count; i++)
        {
            _NpcListForChunk[x, y][i].GetComponent<NPC>().SpawnNPCChild();
        }
    }
    public void DespawnNpcs(int x, int y)
    {
        if (_NpcListForChunk[x, y] == null) return;

        int count = _NpcListForChunk[x, y].Count;
        for (int i = 0; i < count; i++)
        {
            _NpcListForChunk[x, y][i].GetComponent<NPC>().DestroyNPCChild();
        }
    }
}
