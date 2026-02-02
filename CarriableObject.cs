using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CarriableObject : MonoBehaviour
{
    public ItemHandleData _ItemHandleData { get { return _itemHandleData; } set { _itemHandleData = value; } }
    private ItemHandleData _itemHandleData;

    public Item _ItemRefForProjectiles { get; set; }
    public Vector2Int _Chunk { get; set; }

    public void TakeCarriableToInventory(Inventory inventory)
    {
        Item item = _ItemRefForProjectiles != null ? _ItemRefForProjectiles : _ItemHandleData._ItemRef;
        if (inventory.CanTakeThisItem(item))
        {
            item.TakenTo(inventory);
            if (_ItemRefForProjectiles != null)
                GameManager._Instance.DestroyProjectileFromWorld(this);
            else
                GameManager._Instance.DestroyEnvironmentPrefabFromWorld(_ItemHandleData);
        }
        else if (inventory.CanEquipThisItem(item, false))
        {
            item.Equip(inventory);
            if(_ItemRefForProjectiles != null)
                GameManager._Instance.DestroyProjectileFromWorld(this);
            else
                GameManager._Instance.DestroyEnvironmentPrefabFromWorld(_ItemHandleData);
        }
    }
}
