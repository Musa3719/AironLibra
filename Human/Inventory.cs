using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    public List<Item> _Items;

    public Inventory()
    {
        _Items = new List<Item>();
    }

    public void AddItemToInventory<T>(T newItem) where T : Item
    {
        if (_Items.Contains(newItem)) { Debug.LogError($"'{newItem}' : already exist in inventory!"); return; }

        if (!(newItem is EquippableItem))
        {
            foreach (var item in _Items)
            {
                if (item is T)
                {
                    item._Count += newItem._Count;
                    return;
                }
            }
        }

        if (_Items.Count >= 48) { Debug.LogError("inventory limit!"); return; }
        //not found
        _Items.Add(newItem);
    }
    public void RemoveItemFromInventory<T>(T existingItem) where T : Item
    {
        if (!_Items.Contains(existingItem)) { Debug.LogError($"inventory does not have to remove item :'{existingItem}'!"); return; }

        if (existingItem is EquippableItem)
        {
            _Items.Remove(existingItem);
            return;
        }
        foreach (var item in _Items)
        {
            if (item is T)
            {
                if (item._Count < existingItem._Count)
                    Debug.LogError("You Tried To Remove More Than You Have...");
                else if (item._Count == existingItem._Count)
                    _Items.Remove(item);
                else
                    item._Count -= existingItem._Count;
                return;
            }
        }
        //not found
        Debug.LogError("Item not found...");
    }
}

public abstract class Item
{
    public Inventory _AttachedInventory;
    public string _Name;
    public float _BaseWeight;
    public int _Count;

    public Item()
    {
        _Count = 1;

        if (this is IHaveItemDurability durability)
        {
            durability._Durability = 100f;
            durability._DurabilityMax = 100f;
        }
    }
    public virtual void Take(Inventory inv)
    {
        if (_AttachedInventory != null) { Debug.LogError("item already exist in another inventory!"); return; }
        if (inv == null) { Debug.LogError("inventory null"); return; }
        inv.AddItemToInventory(this);
        _AttachedInventory = inv;
    }
    public virtual void Drop(Inventory inv)
    {
        if (_AttachedInventory == null) { Debug.LogError("item does not exist in any inventory!"); return; }
        if (inv == null) { Debug.LogError("inventory null"); return; }
        inv.RemoveItemFromInventory(this);
        _AttachedInventory = null;
    }
    public float GetRealWeight()
    {
        return _BaseWeight * _Count;
    }
}

public abstract class EquippableItem : Item
{
    public bool _IsBig { get { if (this is WeaponItem || this is ArmorItem || this is ClothingItem) return true; return _isBig; } set { _isBig = value; } }
    private bool _isBig;
    public bool _IsEquipped;
    public void Init(bool isBig)
    {
        _isBig = isBig;
    }

    public virtual bool Equip()
    {
        if (_IsEquipped) return false;

        _IsEquipped = true;
        return true;
    }
    public virtual bool Unequip()
    {
        if (!_IsEquipped) return false;

        _IsEquipped = false;
        return true;
    }
}
public interface IHaveItemDurability { public float _Durability { get; set; } public float _DurabilityMax { get; set; } }
public abstract class WeaponItem : EquippableItem, IHaveItemDurability
{
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;
    public void Init()
    {

    }

    public override bool Equip()
    {
        if (_IsEquipped) return false;

        if (!base.Equip()) return false;
        return true;
    }
    public override bool Unequip()
    {
        if (!_IsEquipped) return false;

        if (!base.Unequip()) return false;
        return true;
    }
}
public abstract class ClothingItem : EquippableItem, IHaveItemDurability
{
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;

    public float _ColdProtectionValue;
    public void Init(float coldProtectionValue)
    {
        _ColdProtectionValue = coldProtectionValue;
    }
}
public abstract class ArmorItem : EquippableItem, IHaveItemDurability
{
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;

    public bool _IsSteel;
    public float _ProtectionValue;

    public void Init(float protection, bool isSteel)
    {
        _ProtectionValue = protection;
        _IsSteel = isSteel;
    }

    public override bool Equip()
    {
        if (_IsEquipped) return false;

        if (!base.Equip()) return false;
        return true;
    }
    public override bool Unequip()
    {
        if (!_IsEquipped) return false;

        if (!base.Unequip()) return false;
        return true;
    }
}


public abstract class ConsumableItem : Item
{
    public abstract void Consume();
}
#region Items
public class CopperCoin : Item
{
    public CopperCoin()
    {
        _Name = "Copper Coin";
        _BaseWeight = 0.001f;
    }
}
public class SilverCoin : Item
{
    public SilverCoin()
    {
        _Name = "Silver Coin";
        _BaseWeight = 0.0012f;
    }
}
public class GoldCoin : Item
{
    public GoldCoin()
    {
        _Name = "Gold Coin";
        _BaseWeight = 0.002f;
    }
}
public class DiamondShard : Item
{
    public DiamondShard()
    {
        _Name = "Diamond Shard";
        _BaseWeight = 0.005f;
    }
}
public class Apple : ConsumableItem
{
    public Apple()
    {
        _Name = "Water Bottle";
        _BaseWeight = 0.2f;
    }

    public override void Consume()
    {

    }
}
#endregion
