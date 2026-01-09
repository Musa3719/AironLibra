using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class AddressablesController : MonoBehaviour
{
    public static AddressablesController _Instance;
    #region Prefabs
    [SerializeField] private AssetReferenceGameObject _treePrefab;
    #region Items

    public AssetReferenceGameObject _ItemContainer;
    public AssetReferenceGameObject _BackpackItem;
    public AssetReferenceGameObject _ChestArmor_1Item;
    public AssetReferenceGameObject _LongSword_1Item;

    public AssetReferenceSprite _AppleSprite;
    public AssetReferenceSprite _BackpackSprite;
    public AssetReferenceSprite _ChestArmor_1Sprite;
    public AssetReferenceSprite _LongSword_1Sprite;

    #endregion
    #endregion

    public bool[,] _IsChunkLoadedToScene { get; private set; }
    public bool[,] _IsChunkLoading { get; private set; }
    public bool[,] _IsChunkUnloading { get; private set; }
    public Coroutine[,] _IsChunkLoadingCoroutines { get; private set; }
    public Coroutine[,] _IsChunkUnloadingCoroutines { get; private set; }
    public List<AsyncOperationHandle<GameObject>>[,] _HandlesForSpawned { get; private set; }
    public List<GameObject>[,] _NpcListForChunk { get; private set; }

    #region Method Parameters For Optimization
    private List<ItemHandleData> _objectsWillBeSpawned;
    private List<Transform> _objectsParentForSpawn;
    private List<Vector3> _objectPositionsWillBeSpawned;
    private List<Vector3> _objectRotationsWillBeSpawned;
    #endregion

    private void Awake()
    {
        _Instance = this;
        _NpcListForChunk = new List<GameObject>[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
        _IsChunkLoadedToScene = new bool[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
        _IsChunkLoading = new bool[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
        _IsChunkUnloading = new bool[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
        _IsChunkLoadingCoroutines = new Coroutine[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
        _IsChunkUnloadingCoroutines = new Coroutine[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
        _objectsWillBeSpawned = new List<ItemHandleData>();
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
        if (_HandlesForSpawned[x, y] == null) return;

        for (int i = 0; i < _HandlesForSpawned[x, y].Count; i++)
        {
            var handle = _HandlesForSpawned[x, y][i];
            Debug.Log(GameManager._Instance._ItemHandleDatasInChunk[x, y].Count + " : " + _HandlesForSpawned[x, y].Count);
            GameManager._Instance._ItemHandleDatasInChunk[x, y][i]._SpawnHandle = null;
            if (handle.IsDone)
            {
                DespawnObj(handle);
            }
            else
            {
                handle.Completed += (h) =>
                {
                    DespawnObj(handle);
                };
            }
        }
        _HandlesForSpawned[x, y].Clear();
    }
    public void DespawnObj(AsyncOperationHandle<GameObject> asyncOperationHandle)
    {
        GameObject obj = asyncOperationHandle.Result;
        if (obj != null)
        {
            obj.SetActive(false);
        }
        Addressables.ReleaseInstance(asyncOperationHandle);
    }

    public List<AsyncOperationHandle> LoadTerrainObjects(int x, int y)
    {
        List<AsyncOperationHandle> allHandles = new List<AsyncOperationHandle>();
        SetAdressablesForSpawn(x, y);
        if (_objectsWillBeSpawned == null) return allHandles;
        for (int i = 0; i < _objectsWillBeSpawned.Count; i++)
        {
            allHandles.Add(SpawnObj(x, y, i));
        }

        return allHandles;
    }
    private void SetAdressablesForSpawn(int x, int y)
    {
        _objectsWillBeSpawned = GameManager._Instance._ItemHandleDatasInChunk[x, y];
        _objectPositionsWillBeSpawned = GameManager._Instance._ObjectPositionsInChunk[x, y];
        _objectRotationsWillBeSpawned = GameManager._Instance._ObjectRotationsInChunk[x, y];
        _objectsParentForSpawn = GameManager._Instance._ObjectParentsInChunk[x, y];
    }
    private AsyncOperationHandle SpawnObj(int x, int y, int i)
    {
        if (_HandlesForSpawned[x, y] == null)
            _HandlesForSpawned[x, y] = new List<AsyncOperationHandle<GameObject>>();

        Vector3 pos = _objectPositionsWillBeSpawned[i];
        Vector3 angles = _objectRotationsWillBeSpawned[i];
        Transform parentTransform = _objectsParentForSpawn[i];
        ItemHandleData itemHandleData = _objectsWillBeSpawned[i];
        AsyncOperationHandle<GameObject> asynchandle = itemHandleData._AssetRef.InstantiateAsync(pos, Quaternion.Euler(angles), parentTransform);
        itemHandleData._SpawnHandle = asynchandle;
        asynchandle.Completed += (handle) =>
         {
             if (handle.Status != AsyncOperationStatus.Succeeded) return;
             GameObject obj = handle.Result;
             GameManager._Instance.SetTerrainLinks(obj);
             _HandlesForSpawned[x, y].Add(handle);
             handle.Result.GetComponent<CarriableObject>()._ItemHandleData = itemHandleData;
             handle.Result.GetComponent<CarriableObject>()._Chunk = new Vector2Int(x, y);

             //if (handle.Result.CompareTag("InventoryHolder"))
             //LoadInventoryHolderData(handle.Result);
             //if (handle.Result.CompareTag("Plant"))
             //LoadPlantData(handle.Result);
         };

        return asynchandle;
    }

    public List<AsyncOperationHandle> SpawnNpcs(int x, int y)
    {
        List<AsyncOperationHandle> allHandles = new List<AsyncOperationHandle>();
        if (_NpcListForChunk[x, y] == null) return allHandles;

        int count = _NpcListForChunk[x, y].Count;
        for (int i = 0; i < count; i++)
        {
            _NpcListForChunk[x, y][i].GetComponent<NPC>().SpawnNPCChild();
        }
        return allHandles;
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
