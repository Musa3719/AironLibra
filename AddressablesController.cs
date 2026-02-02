using System.Collections;
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
    public AssetReferenceGameObject _Backpack_Item;
    public AssetReferenceGameObject _ChestArmor_1_Item;
    public AssetReferenceGameObject _LongSword_1_Item;
    public AssetReferenceGameObject _Crossbow_Item;
    public AssetReferenceGameObject _SurvivalBow_Item;
    public AssetReferenceGameObject _HuntingBow_Item;
    public AssetReferenceGameObject _CompositeBow_Item;

    public AssetReferenceSprite _Apple_Sprite;
    public AssetReferenceSprite _Backpack_Sprite;
    public AssetReferenceSprite _ChestArmor_1_Sprite;
    public AssetReferenceSprite _LongSword_1_Sprite;
    public AssetReferenceSprite _Crossbow_Sprite;
    public AssetReferenceSprite _SurvivalBow_Sprite;
    public AssetReferenceSprite _HuntingBow_Sprite;
    public AssetReferenceSprite _CompositeBow_Sprite;
    public AssetReferenceSprite _Arrow_Sprite;
    public AssetReferenceSprite _Bolt_Sprite;

    #endregion
    #endregion

    public bool[,] _IsChunkLoadedToScene { get; private set; }
    public bool[,] _IsChunkLoading { get; private set; }
    public bool[,] _IsChunkUnloading { get; private set; }
    public Coroutine[,] _IsChunkLoadingCoroutines { get; private set; }
    public Coroutine[,] _IsChunkUnloadingCoroutines { get; private set; }
    public List<AsyncOperationHandle<GameObject>>[,] _HandlesForSpawned { get; private set; }
    //public List<GameObject>[,] _NpcListForChunk { get; private set; }

    #region Method Parameters For Optimization
    private List<ItemHandleData> _objectsWillBeSpawned;
    private List<Transform> _objectsParentForSpawn;
    private List<Vector3> _objectPositionsWillBeSpawned;
    private List<Vector3> _objectRotationsWillBeSpawned;
    #endregion

    private void Awake()
    {
        _Instance = this;
        //_NpcListForChunk = new List<GameObject>[GameManager._Instance._NumberOfColumnsForTerrains, GameManager._Instance._NumberOfRowsForTerrains];
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
            int index = GameManager._Instance._ItemHandleDatasInChunk[x, y].IndexOf(handle);
            if (index != -1)
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
    public void SetAdressablesForSpawn(int x, int y)
    {
        _objectsWillBeSpawned = GameManager._Instance._ItemHandleDatasInChunk[x, y];
        _objectPositionsWillBeSpawned = GameManager._Instance._ObjectPositionsInChunk[x, y];
        _objectRotationsWillBeSpawned = GameManager._Instance._ObjectRotationsInChunk[x, y];
        _objectsParentForSpawn = GameManager._Instance._ObjectParentsInChunk[x, y];
    }
    public AsyncOperationHandle SpawnObj(int x, int y, int i)
    {
        if (_HandlesForSpawned[x, y] == null)
            _HandlesForSpawned[x, y] = new List<AsyncOperationHandle<GameObject>>();

        Vector3 pos = _objectPositionsWillBeSpawned[i];
        Vector3 angles = _objectRotationsWillBeSpawned[i];
        Transform parentTransform = _objectsParentForSpawn[i];
        ItemHandleData itemHandleData = _objectsWillBeSpawned[i];
        AsyncOperationHandle<GameObject> asynchandle = itemHandleData._AssetRef.InstantiateAsync(pos, Quaternion.Euler(angles), parentTransform);
        itemHandleData._SpawnHandle = asynchandle;
        _HandlesForSpawned[x, y].Add(asynchandle);
        asynchandle.Completed += (handle) =>
         {
             if (handle.Status != AsyncOperationStatus.Succeeded) return;
             GameObject obj = handle.Result;
             GameManager._Instance.SetTerrainLinks(obj);
             if (handle.Result.GetComponent<CarriableObject>() == null)
                 handle.Result.AddComponent<CarriableObject>();
             handle.Result.GetComponent<Collider>().enabled = true;
             handle.Result.GetComponent<CarriableObject>()._ItemHandleData = itemHandleData;
             handle.Result.GetComponent<CarriableObject>()._Chunk = new Vector2Int(x, y);
             //if (handle.Result.CompareTag("InventoryHolder"))
             //LoadInventoryHolderData(handle.Result);
             //if (handle.Result.CompareTag("Plant"))
             //LoadPlantData(handle.Result);
         };

        return asynchandle;
    }

    /*public List<AsyncOperationHandle> SpawnNpcs(int x, int y)
    {
        List<AsyncOperationHandle> allHandles = new List<AsyncOperationHandle>();
        if (_NpcListForChunk[x, y] == null) return allHandles;

        StartCoroutine(SpawnNpcChildCoroutine(x, y));
        return allHandles;
    }
    private IEnumerator SpawnNpcChildCoroutine(int x, int y)
    {
        int count = _NpcListForChunk[x, y].Count;

        for (int i = 0; i < count; i++)
        {
            _NpcListForChunk[x, y][i].GetComponent<NPC>().SpawnNPCChild();
            yield return null;
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
    }*/
}
