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
        foreach (var item in _Items)
        {
            if (item is T)
            {
                item._Count += newItem._Count;
                return;
            }
        }
        //not found
        _Items.Add(newItem);
    }
    public void RemoveItemFromInventory<T>(T newItem) where T : Item
    {
        foreach (var item in _Items)
        {
            if (item is T)
            {
                if (item._Count < newItem._Count)
                    Debug.LogError("You Tried To Remove More Than You Have...");
                else if(item._Count == newItem._Count)
                    _Items.Remove(item);
                else
                    item._Count -= newItem._Count;
                return;
            }
        }
        //not found
        Debug.LogError("Item not found...");
    }
}

public abstract class Item
{
    public string _Name;
    public float _BaseWeight;
    public int _Count;

    public Item()
    {
        _Count = 1;
    }

    public float GetRealWeight()
    {
        return _BaseWeight * _Count;
    }
}
public abstract class HeavyItem : Item
{
    public abstract void Equip();
    public abstract void Drop();
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
public class WaterBottle : ConsumableItem
{
    public WaterBottle()
    {
        _Name = "Water Bottle";
        _BaseWeight = 0.2f;
    }

    public override void Consume()
    {

    }
}
#endregion
