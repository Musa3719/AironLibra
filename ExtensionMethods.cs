using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class ExtensionMethods
{
    public static int IndexOf(this List<ItemHandleData> itemHandleData, AsyncOperationHandle<GameObject> handle)
    {
        for (int i = 0; i < itemHandleData.Count; i++)
        {
            if (itemHandleData[i]._MeshSpawnHandle.HasValue && itemHandleData[i]._MeshSpawnHandle.Value.Equals(handle))
                return i;
        }
        return -1;
    }
    public static bool Contains(this LayerMask mask, int layer)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
