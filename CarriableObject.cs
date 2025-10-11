using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CarriableObject : MonoBehaviour
{
    public AsyncOperationHandle<GameObject> _Handle { get; set; }
    public Vector2Int _Chunk { get; set; }

    public void RemoveFromChunk()
    {
        if (_Handle.Result == null)
        {
            Debug.LogError("Handle null for chunk!");
            return;
        }

        if (AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y].Contains(_Handle))
            AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y].Remove(_Handle);
    }
    public void AddToChunk()
    {
        if (_Handle.Result == null)
        {
            Debug.LogError("Handle null for chunk!");
            return;
        }

        _Chunk = GameManager._Instance.GetChunkFromPosition(transform.position);
        if (AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y] == null)
            AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y] = new List<AsyncOperationHandle<GameObject>>();
        AddressablesController._Instance._HandlesForSpawned[_Chunk.x, _Chunk.y].Add(_Handle);
    }
}
