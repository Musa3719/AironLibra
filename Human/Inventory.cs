using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public string _Name => _CanEquip ? GetComponent<Humanoid>()._Name : gameObject.name;
    public bool _IsHuman => gameObject.layer == LayerMask.NameToLayer("Human");
    public bool _CanCarryBigItems { get; private set; }
    public bool _CanEquip { get; private set; }
    public int _ItemLimit { get; private set; }
    public List<Item> _Items { get; private set; }

    private void Awake()
    {
        _Items = new List<Item>();
        _ItemLimit = _IsHuman ? 12 : 24;
        _CanEquip = _IsHuman;
        _CanCarryBigItems = !_IsHuman;
    }
    private void Start()
    {
        int random = Random.Range(1, 6);
        for (int i = 0; i < random; i++)
        {
            new Item(AppleDefinition._Instance).Take(this);
        }
    }

    public bool IsInventoryVisibleInScreen()
    {
        return GameManager._Instance._AnotherInventory == this || WorldHandler._Instance._Player._Inventory == this;
    }
    public bool IsFull()
    {
        return _Items.Count >= _ItemLimit;
    }
    public bool CanTakeThisItem(Item item)
    {
        if (!IsFull()) return true;

        if (item is IHaveItemDurability) return false;

        if (HasItemType(item._ItemDefinition)) return true;

        return false;
    }

    public bool HasItemType(ItemDefinition itemDef)
    {
        foreach (var item in _Items)
        {
            if (item._ItemDefinition == itemDef)
                return true;
        }
        return false;
    }
    public void AddItemToInventory(Item newItem)
    {
        if (_Items.Contains(newItem)) { Debug.LogError($"'{newItem}' : already exist in inventory!"); return; }

        if (!(newItem._ItemDefinition._IsBig))
        {
            foreach (var item in _Items)
            {
                if (item._ItemDefinition == newItem._ItemDefinition)
                {
                    item._Count += newItem._Count;
                    return;
                }
            }
        }

        if (IsFull()) { Debug.LogError("inventory limit!"); return; }
        //not found
        _Items.Add(newItem);
    }
    public void RemoveItemFromInventory(Item existingItem)
    {
        if (!_Items.Contains(existingItem)) { if (existingItem._IsSplittingBuffer) existingItem._IsSplittingBuffer = false; else Debug.LogError($"inventory does not have to remove item :'{existingItem}'!"); return; }

        if (existingItem._ItemDefinition._IsBig)
        {
            _Items.Remove(existingItem);
            return;
        }
        foreach (var item in _Items)
        {
            if (item._ItemDefinition == existingItem._ItemDefinition)
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

public class ItemHandleData
{
    public int _XChunk;
    public int _YChunk;
    public UnityEngine.AddressableAssets.AssetReferenceGameObject _AssetRef;
}
public abstract class ItemDefinition
{
    public string _Name;
    public float _BaseWeight;
    public float _BaseVolume;
    public bool _IsBig;//two handed for weapons, slow and two handed carry for others 
    public bool _CanBeEquipped;

    public string _ItemDescription;

    public ItemDefinition(string name, float weight, float volume, bool isBig, bool canBeEquipped)
    {
        _Name = name;
        _BaseWeight = weight;
        _BaseVolume = volume;
        _IsBig = isBig;
        _CanBeEquipped = canBeEquipped;
    }
}

public class Item
{
    public ItemDefinition _ItemDefinition;

    public Inventory _AttachedInventory;
    public Humanoid _EquippedHumanoid;
    public ItemHandleData _ItemHandleData;

    public int _Count;
    public bool _IsEquipped => _EquippedHumanoid != null;
    public bool _IsSplittingBuffer;

    public Item(ItemDefinition def)
    {
        _ItemDefinition = def;
        _ItemHandleData = new ItemHandleData();
        _Count = 1;

        if (this is IHaveItemDurability durability)
        {
            durability._Durability = 100f;
            durability._DurabilityMax = 100f;
        }
    }
    public Item Copy()
    {
        Item copy = new Item(_ItemDefinition);
        copy._AttachedInventory = _AttachedInventory;
        copy._EquippedHumanoid = _EquippedHumanoid;
        copy._ItemHandleData = _ItemHandleData;
        copy._Count = _Count;
        return copy;
    }
    public virtual void SpawnInstanceToWorld()
    {
        if (!GameManager._Instance._ItemNameToPrefab.ContainsKey(_ItemDefinition._Name)) { Debug.LogError("Item(" + _ItemDefinition._Name + ") Prefab Not Found!"); return; }
        GameManager._Instance.CreateEnvironmentPrefabToWorld(GameManager._Instance._ItemNameToPrefab[_ItemDefinition._Name], GameManager._Instance._EnvironmentTransform, _AttachedInventory.transform.position, Vector3.zero, ref _ItemHandleData);
    }
    public virtual void DespawnInstanceFromWorld()
    {
        GameManager._Instance.DestroyEnvironmentPrefabFromWorld(_ItemHandleData);
    }
    public virtual bool Equip(Inventory targetInv)
    {
        if (!targetInv._CanEquip) return false;
        Humanoid oldHuman = null;
        if (_EquippedHumanoid != null)
            oldHuman = _EquippedHumanoid;
        if (_IsEquipped)
            Unequip(false, false);

        _EquippedHumanoid = targetInv.transform.GetComponent<Humanoid>();

        if (_EquippedHumanoid._Inventory.IsInventoryVisibleInScreen() || (oldHuman != null && oldHuman._Inventory.IsInventoryVisibleInScreen()))
            GameManager._Instance.UpdateInventoryUI();
        return true;
    }
    public virtual bool Unequip(bool isDropping, bool isTaking)
    {
        if (!_IsEquipped) return false;

        bool isVisible = _EquippedHumanoid._Inventory.IsInventoryVisibleInScreen();

        if (isDropping)
        {
            _EquippedHumanoid = null;
            Drop(true);
        }
        else if (isTaking)
            Take(_EquippedHumanoid.GetComponent<Inventory>());

        _EquippedHumanoid = null;

        if (isVisible)
            GameManager._Instance.UpdateInventoryUI();

        return true;
    }

    public virtual void Take(Inventory inv)
    {
        if (inv == null) { Debug.LogError("inventory null"); return; }
        if (_ItemDefinition._IsBig && !inv._CanCarryBigItems) return;
        if (!inv.CanTakeThisItem(this)) return;
        Inventory oldInv = null;
        if (_AttachedInventory != null)
        {
            oldInv = _AttachedInventory;
            Drop(false);
        }
        inv.AddItemToInventory(this);
        _AttachedInventory = inv;

        if (_AttachedInventory.IsInventoryVisibleInScreen() || (oldInv != null && oldInv.IsInventoryVisibleInScreen()))
            GameManager._Instance.UpdateInventoryUI();
    }
    public virtual void Drop(bool isInstancing)
    {
        if (_IsEquipped) { Debug.LogError("item is equipped!"); return; }
        if (_AttachedInventory == null) { Debug.LogError("item does not exist in any inventory!"); return; }
        _AttachedInventory.RemoveItemFromInventory(this);
        if (isInstancing)
            SpawnInstanceToWorld();

        if (_AttachedInventory.IsInventoryVisibleInScreen())
            GameManager._Instance.UpdateInventoryUI();
        _AttachedInventory = null;
    }
    public float GetRealWeight()
    {
        return _ItemDefinition._BaseWeight * _Count;
    }
}

public interface IHaveInventory { public Inventory _Inventory { get; set; } }
public interface IHaveItemDurability { public float _Durability { get; set; } public float _DurabilityMax { get; set; } public void DurabilityChanged(); }
public interface ICanBeConsumed { public float _ConsumeValue { get; } public abstract void Consume(Item item); }
public class WeaponItem : Item, IHaveItemDurability
{
    public WeaponItem(ItemDefinition def) : base(def)
    {

    }
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;

    public void DurabilityChanged()
    {
        if (_EquippedHumanoid == null) return;

        if (_Durability <= 0f)
        {
            bool isFull = _EquippedHumanoid._Inventory.IsFull();
            Unequip(isFull, !isFull);
        }
    }
    public override bool Equip(Inventory targetInv)
    {
        if (_IsEquipped) return false;

        if (!base.Equip(targetInv)) return false;
        return true;
    }
    public override bool Unequip(bool isDropping, bool isTaking)
    {
        if (!_IsEquipped) return false;

        if (!base.Unequip(isDropping, isTaking)) return false;
        return true;
    }
}
public class ClothingItem : Item, IHaveItemDurability
{
    public ClothingItem(ItemDefinition def, float coldProtectionValue) : base(def)
    {
        _ColdProtectionValue = coldProtectionValue;
    }
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;
    public void DurabilityChanged()
    {

    }

    public float _ColdProtectionValue;
}
public class ArmorItem : Item, IHaveItemDurability
{
    public ArmorItem(ItemDefinition def, float protection, bool isSteel) : base(def)
    {
        _ProtectionValue = protection;
        _IsSteel = isSteel;
    }
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;
    public void DurabilityChanged()
    {

    }

    public bool _IsSteel;
    public float _ProtectionValue;

    public override bool Equip(Inventory targetInv)
    {
        if (_IsEquipped) return false;

        if (!base.Equip(targetInv)) return false;
        return true;
    }
    public override bool Unequip(bool isDropping, bool isTaking)
    {
        if (!_IsEquipped) return false;

        if (!base.Unequip(isDropping, isTaking)) return false;
        return true;
    }
}

#region Items

public class AppleDefinition : ItemDefinition, ICanBeConsumed
{
    public static AppleDefinition _Instance { get { if (_instance == null) _instance = new AppleDefinition(); return _instance; } }
    private static AppleDefinition _instance;
    public float _ConsumeValue => 5f;
    public AppleDefinition() : base("Apple", 0.2f, 0.1f, false, false) { _ItemDescription = Localization._Instance._UI[69] + _ConsumeValue + Localization._Instance._UI[70]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item._AttachedInventory.RemoveItemFromInventory(item); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUI(); }
}
public class CopperCoinDefinition : ItemDefinition
{
    public static CopperCoinDefinition _Instance { get { if (_instance == null) _instance = new CopperCoinDefinition(); return _instance; } }
    private static CopperCoinDefinition _instance;
    public CopperCoinDefinition() : base("Copper Coin", 0.003f, 0.01f, false, false) { _ItemDescription = Localization._Instance._UI[71]; }
}
public class SilverCoinDefinition : ItemDefinition
{
    public static SilverCoinDefinition _Instance { get { if (_instance == null) _instance = new SilverCoinDefinition(); return _instance; } }
    private static SilverCoinDefinition _instance;
    public SilverCoinDefinition() : base("Silver Coin", 0.003f, 0.01f, false, false) { _ItemDescription = Localization._Instance._UI[72]; }
}
public class GoldCoinDefinition : ItemDefinition
{
    public static GoldCoinDefinition _Instance { get { if (_instance == null) _instance = new GoldCoinDefinition(); return _instance; } }
    private static GoldCoinDefinition _instance;
    public GoldCoinDefinition() : base("Gold Coin", 0.006f, 0.01f, false, false) { _ItemDescription = Localization._Instance._UI[73]; }
}
public class DiamondShardDefinition : ItemDefinition
{
    public static DiamondShardDefinition _Instance { get { if (_instance == null) _instance = new DiamondShardDefinition(); return _instance; } }
    private static DiamondShardDefinition _instance;
    public DiamondShardDefinition() : base("Diamond Shard", 0.01f, 0.01f, false, false) { _ItemDescription = Localization._Instance._UI[74]; }
}
#endregion