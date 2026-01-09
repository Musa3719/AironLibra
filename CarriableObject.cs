using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CarriableObject : MonoBehaviour
{
    public ItemHandleData _ItemHandleData { get { return _itemHandleData; } set { _itemHandleData = value; } }
    private ItemHandleData _itemHandleData;

    public Vector2Int _Chunk { get; set; }

    public void TakeCarriableToInventory(Inventory inventory)
    {
        Item item = _ItemHandleData._ItemRef;
        if (!inventory.CanTakeThisItem(item)) return;

        GameManager._Instance.DestroyEnvironmentPrefabFromWorld(_ItemHandleData);
        item.TakenTo(inventory);
    }

}
