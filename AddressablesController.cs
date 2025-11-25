using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesController : MonoBehaviour
{
    public static AddressablesController _Instance;
    #region Prefabs
    [SerializeField] private AssetReferenceGameObject _treePrefab;
    #region Items

    public AssetReferenceGameObject _ItemContainer;
    public AssetReferenceGameObject _SmallPackItem;
    public AssetReferenceGameObject _ChestArmor_1Item;
    public AssetReferenceGameObject _LongSword_1Item;

    public AssetReferenceSprite _AppleSprite;
    public AssetReferenceSprite _SmallPackSprite;
    public AssetReferenceSprite _ChestArmor_1Sprite;
    public AssetReferenceSprite _LongSword_1Sprite;

    #endregion
    #endregion

    public bool[,] _IsChunkLoadedToScene { get; private set; }
    public List<AsyncOperationHandle<GameObject>>[,] _HandlesForSpawned { get; private set; }
    public List<GameObject>[,] _NpcListForChunk { get; private set; }

    #region Method Parameters For Optimization
    private List<AssetReferenceGameObject> _objectsWillBeSpawned;
    private List<Transform> _objectsParentForSpawn;
    private List<ItemHandleData> _objectsItemHandleForSpawn;
    private List<Vector3> _objectPositionsWillBeSpawned;
    private List<Vector3> _objectRotationsWillBeSpawned;
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

        SetAdressablesForSpawn(x, y);
        if (_objectsWillBeSpawned == null) return;
        for (int i = 0; i < _objectsWillBeSpawned.Count; i++)
        {
            SpawnObj(x, y, i);
        }
    }
    private void SetAdressablesForSpawn(int x, int y)
    {
        _objectsWillBeSpawned = GameManager._Instance._ObjectsInChunk[x, y];
        _objectPositionsWillBeSpawned = GameManager._Instance._ObjectPositionsInChunk[x, y];
        _objectRotationsWillBeSpawned = GameManager._Instance._ObjectRotationsInChunk[x, y];
        _objectsParentForSpawn = GameManager._Instance._ObjectParentsInChunk[x, y];
        _objectsItemHandleForSpawn = GameManager._Instance._ObjectItemHandleData[x, y];
    }
    private void SpawnObj(int x, int y, int i)
    {
        if (_HandlesForSpawned[x, y] == null)
            _HandlesForSpawned[x, y] = new List<AsyncOperationHandle<GameObject>>();

        Vector3 pos = _objectPositionsWillBeSpawned[i]; //for action buffer(index changes)
        Vector3 angles = _objectRotationsWillBeSpawned[i]; //for action buffer(index changes)
        Transform parentTransform = _objectsParentForSpawn[i]; //for action buffer(index changes)
        ItemHandleData itemHandle = _objectsItemHandleForSpawn[i]; //for action buffer(index changes)
        _objectsWillBeSpawned[i].InstantiateAsync(pos, Quaternion.Euler(angles), parentTransform).Completed += (handle) =>
         {
             if (handle.Status != AsyncOperationStatus.Succeeded) return;
             GameObject obj = handle.Result;
             GameManager._Instance.SetTerrainLinks(obj);
             _HandlesForSpawned[x, y].Add(handle);
             handle.Result.GetComponent<CarriableObject>()._ItemHandleData = itemHandle;
             handle.Result.GetComponent<CarriableObject>()._Handle = handle;
             handle.Result.GetComponent<CarriableObject>()._Chunk = new Vector2Int(x, y);

            //if (handle.Result.CompareTag("InventoryHolder"))
            //LoadInventoryHolderData(handle.Result);
            //if (handle.Result.CompareTag("Plant"))
            //LoadPlantData(handle.Result);
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
