using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CarriableObject : MonoBehaviour
{
    public ItemHandleData _ItemHandleData { get { return _itemHandleData; } set { _itemHandleData = value; } }
    private ItemHandleData _itemHandleData;

    public AsyncOperationHandle<GameObject> _Handle { get; set; }
    public Vector2Int _Chunk { get; set; }

    private float _checkForChunkChangedCounter;

    private void Update()
    {
        if (_ItemHandleData == null) return;

        if (_checkForChunkChangedCounter < 2f)
        {
            _checkForChunkChangedCounter += Time.deltaTime;
            return;
        }
        _checkForChunkChangedCounter = 0f;

        Vector2Int checkChunk = GameManager._Instance.GetChunkFromPosition(transform.position);
        if (checkChunk.x != _Chunk.x || checkChunk.y != _Chunk.y)
            ChunkChanged();
        else
        {
            int index = GameManager._Instance._ObjectsInChunk[_Chunk.x, _Chunk.y].IndexOf(_ItemHandleData._AssetRef);
            GameManager._Instance._ObjectPositionsInChunk[_Chunk.x, _Chunk.y][index] = transform.position;
            GameManager._Instance._ObjectRotationsInChunk[_Chunk.x, _Chunk.y][index] = transform.localEulerAngles;
            GameManager._Instance._ObjectParentsInChunk[_Chunk.x, _Chunk.y][index] = transform.parent;
        }
    }
    private void ChunkChanged()
    {
        RemoveFromChunk();
        AddToChunk();
    }
    private void RemoveFromChunk()
    {
        if (_Handle.Result == null)
        {
            Debug.LogError("Handle null for chunk!");
            return;
        }

        if (AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y].Contains(_Handle))
        {
            AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y].Remove(_Handle);
            GameManager._Instance.DestroyEnvironmentPrefabFromWorld(_ItemHandleData);
        }
    }
    private void AddToChunk()
    {
        if (_Handle.Result == null)
        {
            Debug.LogError("Handle null for chunk!");
            return;
        }

        _Chunk = GameManager._Instance.GetChunkFromPosition(transform.position);
        _ItemHandleData._XChunk = _Chunk.x;
        _ItemHandleData._YChunk = _Chunk.y;
        if (AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y] == null)
            AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y] = new List<AsyncOperationHandle<GameObject>>();
        AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y].Add(_Handle);
        GameManager._Instance.CreateEnvironmentPrefabToWorld(_ItemHandleData._AssetRef, transform.parent, transform.position, transform.localEulerAngles, ref _itemHandleData);
    }
}
