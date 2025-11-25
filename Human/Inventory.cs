using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Inventory
{
    public InventoryHolder _InventoryHolder { get; private set; }
    public Inventory(InventoryHolder holder, CarryItem backCarryToItem = null)
    {
        _InventoryHolder = holder;
        _BackCarryToItem = backCarryToItem;
        Init();
        GameManager._Instance.CallForAction(InitItems, 0.1f, true);
    }

    public float _CarryVolumeLimit { get { return _IsBackCarry ? (_BackCarryToItem._ItemDefinition as ICanBeEquippedForDefinition)._Value : _InventoryHolder._CarryVolumeLimit; } }
    public float _CarryWeightLimit => _IsHuman ? (_InventoryHolder._Human._MuscleLevel * 80f + _InventoryHolder._Human._FatLevel * 20f + _InventoryHolder._Human._Height * 40f) : (_CarryVolumeLimit * 5);

    public string _Name => _CanEquip ? _InventoryHolder._Human._Name : (_IsBackCarry ? _BackCarryToItem._ItemDefinition._Name : _InventoryHolder.gameObject.name);
    public bool _IsHuman => _InventoryHolder?.gameObject.layer == LayerMask.NameToLayer("Human");
    public bool _IsBackCarry => _InventoryHolder == null;
    public bool _IsPublic { get { if ((_InventoryHolder?._Human != null && _InventoryHolder._Human._HealthSystem._IsUnconscious) || (_BackCarryToEquippedHuman != null && _BackCarryToEquippedHuman._HealthSystem._IsUnconscious)) return true; return _isPublic; } }
    private bool _isPublic;
    public Humanoid _BackCarryToEquippedHuman { get; set; }
    public CarryItem _BackCarryToItem { get; set; }
    public bool _CanCarryBigItems { get; private set; }
    public bool _CanEquip { get; private set; }
    public int _ItemLimit { get; private set; }
    public List<Item> _Items { get; private set; }

    private void Init()
    {
        _Items = new List<Item>();
        _ItemLimit = _IsHuman ? 4 : (_IsBackCarry ? 24 : 32);
        _CanEquip = _IsHuman;
        _CanCarryBigItems = !_IsHuman;
        _isPublic = !_IsHuman;
    }
    private void InitItems()
    {
        //test
        if (_IsBackCarry) return;
        new ArmorItem(ChestArmor_1._Instance).TakenTo(this);
        new WeaponItem(LongSword_1._Instance).TakenTo(this);
        new CarryItem(SmallPack._Instance).Equip(this);

        int random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(Apple._Instance).TakenTo(this);
        }
        random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(Apple2._Instance).TakenTo(this);
        }
        random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(Apple3._Instance).TakenTo(this);
        }
        random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(Apple4._Instance).TakenTo(this);
        }
        random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(Apple5._Instance).TakenTo(this);
        }
        random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(Apple6._Instance).TakenTo(this);
        }
    }
    public float GetCurrentCarryVolume()
    {
        float sum = 0f;
        foreach (var item in _Items)
        {
            sum += item.GetRealVolume();
        }
        return sum;
    }
    public float GetCurrentCarryWeight(bool isRecursive)
    {
        float sum = 0f;
        foreach (var item in _Items)
        {
            sum += item.GetRealWeight();
        }

        if (isRecursive) return sum;

        if (_IsHuman)
        {
            if (_IsBackCarry)
            {
                sum += _BackCarryToEquippedHuman._Inventory.GetCurrentCarryWeight(true);
                sum += _BackCarryToEquippedHuman._Inventory.GetEquipmentsTotalWeight();
            }
            else
            {
                if (_InventoryHolder._Human._BackCarryItemRef != null)
                    sum += (_InventoryHolder._Human._BackCarryItemRef as CarryItem)._Inventory.GetCurrentCarryWeight(true);
                sum += GetEquipmentsTotalWeight();
            }

        }
        return sum;
    }
    private float GetEquipmentsTotalWeight()
    {
        float sum = 0f;
        if (_InventoryHolder._Human._HeadGearItemRef != null)
            sum += _InventoryHolder._Human._HeadGearItemRef.GetRealWeight();
        if (_InventoryHolder._Human._GlovesItemRef != null)
            sum += _InventoryHolder._Human._GlovesItemRef.GetRealWeight();
        if (_InventoryHolder._Human._BackCarryItemRef != null)
            sum += _InventoryHolder._Human._BackCarryItemRef.GetRealWeight();
        if (_InventoryHolder._Human._ClothingItemRef != null)
            sum += _InventoryHolder._Human._ClothingItemRef.GetRealWeight();
        if (_InventoryHolder._Human._ChestArmorItemRef != null)
            sum += _InventoryHolder._Human._ChestArmorItemRef.GetRealWeight();
        if (_InventoryHolder._Human._LegsArmorItemRef != null)
            sum += _InventoryHolder._Human._LegsArmorItemRef.GetRealWeight();
        if (_InventoryHolder._Human._BootsItemRef != null)
            sum += _InventoryHolder._Human._BootsItemRef.GetRealWeight();
        if (_InventoryHolder._Human._LeftHandEquippedItemRef != null)
            sum += _InventoryHolder._Human._LeftHandEquippedItemRef.GetRealWeight();
        if (_InventoryHolder._Human._RightHandEquippedItemRef != null)
            sum += _InventoryHolder._Human._RightHandEquippedItemRef.GetRealWeight();
        return sum;
    }
    public bool IsInventoryVisibleInScreen()
    {
        return GameManager._Instance._AnotherInventory == this || WorldHandler._Instance._Player._Inventory == this ||
            (GameManager._Instance._AnotherInventory?._InventoryHolder?._Human?._BackCarryItemRef as CarryItem)?._Inventory == this || (WorldHandler._Instance._Player._BackCarryItemRef as CarryItem)?._Inventory == this;
    }
    public bool IsFull()
    {
        return _Items.Count >= _ItemLimit;
    }
    public bool CanTakeBigItem(Item item, bool checkForEquip)
    {
        if (!checkForEquip && !_CanCarryBigItems) return false;
        if (GetCurrentCarryWeight(false) + item.GetRealWeight() > _CarryWeightLimit) return false;
        if (GetCurrentCarryVolume() + item.GetRealVolume() > _CarryVolumeLimit) return false;
        return true;
    }
    public bool CanTakeThisItem(Item item)
    {
        if (CanTakeThisItemCommon(item)) return true;
        if (_IsHuman && _InventoryHolder.GetComponent<Humanoid>()._BackCarryItemRef != null && (_InventoryHolder.GetComponent<Humanoid>()._BackCarryItemRef as CarryItem)._Inventory.CanTakeThisItemCommon(item)) return true;
        return false;
    }
    public bool CanEquipThisItem(Item item)
    {
        if (!(item is ICanBeEquipped)) return false;
        if (!_CanEquip) return false;
        if (!CanTakeBigItem(item, true)) return false;
        if (!item.CheckViableForHandEquip(this)) return false;
        return true;
    }
    public bool CanTakeThisItemCommon(Item item)
    {
        if (!IsFull())
        {
            if (item._ItemDefinition._IsBig)
            {
                if (CanTakeBigItem(item, false))
                    return true;
                return false;
            }
            return true;
        }

        if (item is ICanBeEquipped) return false;

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
    public void SetPublicity(bool publicity)
    {
        _isPublic = publicity;
    }
    public void AddItemToInventory(Item newItem)
    {
        if (_Items.Contains(newItem)) { Debug.LogError($"'{newItem}' : already exist in inventory!"); return; }
        if (!(newItem._ItemDefinition._CanBeEquipped))
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

        if (existingItem._ItemDefinition._CanBeEquipped)
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

[System.Serializable]
public class ItemHandleData
{
    public int _XChunk;
    public int _YChunk;
    public AssetReferenceGameObject _AssetRef;
}
public abstract class ItemDefinition
{
    public string _Name;
    public float _BaseWeight;
    public float _BaseVolume;
    public bool _IsBig;//two handed for weapons, all wearables, slow and two handed carry for others 
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

    public Inventory _AttachedInventoryCommon => _IsEquipped ? _EquippedHumanoid._Inventory : _AttachedInventory;
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
    public virtual void SpawnInstanceToWorld(Vector3 pos, Vector3 angles)
    {
        if (!GameManager._Instance._ItemNameToPrefab.ContainsKey(_ItemDefinition._Name)) { Debug.LogError("Item(" + _ItemDefinition._Name + ") Prefab Not Found!"); return; }
        GameManager._Instance.CreateEnvironmentPrefabToWorld(GameManager._Instance._ItemNameToPrefab[_ItemDefinition._Name], GameManager._Instance._EnvironmentTransform, pos, angles, ref _ItemHandleData);
    }
    public virtual void DespawnInstanceFromWorld()
    {
        GameManager._Instance.DestroyEnvironmentPrefabFromWorld(_ItemHandleData);
    }
    public void ArrangeExistingEquipSlot(Inventory targetInv, int targetIndex)
    {
        switch (targetIndex)
        {
            case 0:
                if (targetInv._InventoryHolder._Human._HeadGearItemRef == null) return;
                targetInv._InventoryHolder._Human._HeadGearItemRef.Unequip(false, true);
                break;
            case 1:
                if (targetInv._InventoryHolder._Human._GlovesItemRef == null) return;
                targetInv._InventoryHolder._Human._GlovesItemRef.Unequip(false, true);
                break;
            case 2:
                if (targetInv._InventoryHolder._Human._BackCarryItemRef == null) return;
                targetInv._InventoryHolder._Human._BackCarryItemRef.Unequip(false, true);
                break;
            case 3:
                if (targetInv._InventoryHolder._Human._ClothingItemRef == null) return;
                targetInv._InventoryHolder._Human._ClothingItemRef.Unequip(false, true);
                break;
            case 4:
                if (targetInv._InventoryHolder._Human._ChestArmorItemRef == null) return;
                targetInv._InventoryHolder._Human._ChestArmorItemRef.Unequip(false, true);
                break;
            case 5:
                if (targetInv._InventoryHolder._Human._LegsArmorItemRef == null) return;
                targetInv._InventoryHolder._Human._LegsArmorItemRef.Unequip(false, true);
                break;
            case 6:
                if (targetInv._InventoryHolder._Human._BootsItemRef == null) return;
                targetInv._InventoryHolder._Human._BootsItemRef.Unequip(false, true);
                break;
            case 7:
                if (targetInv._InventoryHolder._Human._LeftHandEquippedItemRef == null) return;
                targetInv._InventoryHolder._Human._LeftHandEquippedItemRef.Unequip(false, true);
                break;
            case 8:
                if (targetInv._InventoryHolder._Human._RightHandEquippedItemRef == null) return;
                targetInv._InventoryHolder._Human._RightHandEquippedItemRef.Unequip(false, true);
                break;
            default:
                Debug.LogError("unknown equipment slot!");
                break;
        }
    }
    public void SetHumanoidEquipSlot(int index, bool isEquipping)
    {
        switch (index)
        {
            case 0:
                _EquippedHumanoid._HeadGearItemRef = isEquipping ? this : null;
                break;
            case 1:
                _EquippedHumanoid._GlovesItemRef = isEquipping ? this : null;
                break;
            case 2:
                _EquippedHumanoid._BackCarryItemRef = isEquipping ? this : null;
                (this as CarryItem)._Inventory._BackCarryToEquippedHuman = isEquipping ? _EquippedHumanoid : null;
                break;
            case 3:
                _EquippedHumanoid._ClothingItemRef = isEquipping ? this : null;
                break;
            case 4:
                _EquippedHumanoid._ChestArmorItemRef = isEquipping ? this as ArmorItem : null;
                break;
            case 5:
                _EquippedHumanoid._LegsArmorItemRef = isEquipping ? this as ArmorItem : null;
                break;
            case 6:
                _EquippedHumanoid._BootsItemRef = isEquipping ? this : null;
                break;
            case 7:
                _EquippedHumanoid._LeftHandEquippedItemRef = isEquipping ? this : null;
                break;
            case 8:
                _EquippedHumanoid._RightHandEquippedItemRef = isEquipping ? this : null;
                break;
            default:
                Debug.LogError("unknown equipment slot!");
                break;
        }
    }
    public bool CheckViableForHandEquip(Inventory targetInv)
    {
        int index = (this as ICanBeEquipped)._SlotIndex;
        if (index == 7)
        {
            if (_ItemDefinition._IsBig) return false;
            if (targetInv._InventoryHolder._Human._RightHandEquippedItemRef != null && targetInv._InventoryHolder._Human._RightHandEquippedItemRef._ItemDefinition._IsBig) return false;
            return true;
        }
        else if (index == 8)
        {
            if (_ItemDefinition._IsBig && targetInv._InventoryHolder._Human._LeftHandEquippedItemRef != null) return false;
            return true;
        }
        return true;
    }
    public void SpawnHandItem()
    {
        ICanBeEquipped handItem = this as ICanBeEquipped;
        Transform targetTransform = handItem._SlotIndex == 8 ? (_EquippedHumanoid._IsInCombatMode ? _EquippedHumanoid._RightHandHolderTransform : _EquippedHumanoid._RightWeaponHolder) :
                (_EquippedHumanoid._IsInCombatMode ? _EquippedHumanoid._LeftHandHolderTransform : _EquippedHumanoid._LeftWeaponHolder);
        ICanBeEquippedForDefinition canBeEquippedForDefinition = (_ItemDefinition as ICanBeEquippedForDefinition);
        SpawnHandItemHandle(targetTransform, _EquippedHumanoid._IsInCombatMode ? canBeEquippedForDefinition._PosOffset : canBeEquippedForDefinition._DefPosOffset,
            _EquippedHumanoid._IsInCombatMode ? canBeEquippedForDefinition._AnglesOffset : canBeEquippedForDefinition._DefAnglesOffset, handItem);

        if (_EquippedHumanoid._IsInCombatMode)
        {
            _EquippedHumanoid.ChangeAnimation(handItem._SlotIndex == 8 ? "RightHoldWeapon" : "LeftHoldWeapon");
        }
    }

    private void SpawnHandItemHandle(Transform parentTransform, Vector3 pos, Vector3 angles, ICanBeEquipped handItem)
    {
        handItem._SpawnedHandle = GameManager._Instance._ItemNameToPrefab[_ItemDefinition._Name].InstantiateAsync(parentTransform);
        handItem._SpawnedHandle.Completed += (handle) => { if (!handle.IsValid() || !handle.IsDone || handle.Result == null) return; handle.Result.transform.localPosition = pos; handle.Result.transform.localEulerAngles = angles; if (handItem is WeaponItem weapon) handle.Result.GetComponent<Weapon>().Init(weapon); };
    }
    private void DespawnHandItem()
    {
        if (_EquippedHumanoid._IsAttacking)
            HandStateMethods.AttackIsOver(_EquippedHumanoid, _EquippedHumanoid._LastAttackWeapon);

        DespawnHandItemHandle();
    }
    public void DespawnHandItemHandle()
    {
        ICanBeEquipped handItem = this as ICanBeEquipped;
        if (handItem._SpawnedHandle.IsValid())
        {
            Addressables.ReleaseInstance(handItem._SpawnedHandle);
        }
    }
    public void Equip(Inventory targetInv, bool isToLeftHand = false)
    {
        if (!targetInv.CanEquipThisItem(this)) return;

        Humanoid oldHuman = null;
        if (_EquippedHumanoid != null)
            oldHuman = _EquippedHumanoid;
        if (_IsEquipped)
            Unequip(false, false);
        else if (_AttachedInventory != null)
            DropFrom(false);

        _EquippedHumanoid = targetInv._InventoryHolder._Human;

        if ((this is WeaponItem weaponItem))
        {
            if (isToLeftHand)
                weaponItem._SlotIndexDynamic = 7;
            else if (_EquippedHumanoid._RightHandEquippedItemRef != null && _EquippedHumanoid._LeftHandEquippedItemRef == null)
                weaponItem._SlotIndexDynamic = 7;
            else
                weaponItem._SlotIndexDynamic = 8;
        }

        int slotIndex = (this as ICanBeEquipped)._SlotIndex;
        ArrangeExistingEquipSlot(targetInv, slotIndex);
        SetHumanoidEquipSlot(slotIndex, true);

        if (slotIndex == 7 || slotIndex == 8)
        {
            if (_EquippedHumanoid._UmaDynamicAvatar != null)
            {
                SpawnHandItem();
            }
        }

        if (_EquippedHumanoid._Inventory.IsInventoryVisibleInScreen() || (oldHuman != null && oldHuman._Inventory.IsInventoryVisibleInScreen()))
            GameManager._Instance.UpdateInventoryUIBuffer();
    }
    public void Unequip(bool isDropping, bool isTaking)
    {
        if (!isDropping && isTaking && !_EquippedHumanoid._Inventory.CanTakeThisItem(this)) { isDropping = true; isTaking = false; }
        bool isVisible = _EquippedHumanoid._Inventory.IsInventoryVisibleInScreen();

        if (isDropping)
            DropFrom(true);
        else if (isTaking)
            TakenTo(_EquippedHumanoid._Inventory);
        SetHumanoidEquipSlot((this as ICanBeEquipped)._SlotIndex, false);

        if (this is WeaponItem weaponItem)
        {
            weaponItem._SlotIndexDynamic = 8;
            DespawnHandItem();
        }
        _EquippedHumanoid = null;

        //equipped logic
        //equipped graphic

        if (isVisible)
            GameManager._Instance.UpdateInventoryUIBuffer();
    }

    public virtual void TakenTo(Inventory inv)
    {
        if (inv == null) { Debug.LogError("inventory null"); return; }
        if (!inv.CanTakeThisItemCommon(this))
        {
            if (inv._IsHuman && inv._InventoryHolder._Human._BackCarryItemRef != null) { inv = (inv._InventoryHolder._Human._BackCarryItemRef as CarryItem)._Inventory; } else return;
        }
        if (!inv.CanTakeThisItem(this)) return;
        Inventory oldInv = null;
        if (_AttachedInventory != null)
        {
            oldInv = _AttachedInventory;
            DropFrom(false);
        }
        inv.AddItemToInventory(this);
        _AttachedInventory = inv;

        if (_AttachedInventory.IsInventoryVisibleInScreen() || (oldInv != null && oldInv.IsInventoryVisibleInScreen()))
            GameManager._Instance.UpdateInventoryUIBuffer();
    }
    public virtual void DropFrom(bool isInstancing)
    {
        if (_AttachedInventory == null)
        {
            if (!_IsEquipped) { Debug.LogError("item does not exist in any inventory!"); return; }
        }

        if (_AttachedInventory != null)
            _AttachedInventory.RemoveItemFromInventory(this);
        if (isInstancing)
        {
            Vector3 spawnPos = _AttachedInventory != null ? (_AttachedInventory._IsBackCarry ? _AttachedInventory._BackCarryToEquippedHuman.transform.position : _AttachedInventory._InventoryHolder.transform.position) : _EquippedHumanoid.transform.position;
            SpawnInstanceToWorld(spawnPos, Vector3.zero);
        }

        if ((_AttachedInventory != null && _AttachedInventory.IsInventoryVisibleInScreen()) || (_EquippedHumanoid != null && _EquippedHumanoid._Inventory.IsInventoryVisibleInScreen()))
            GameManager._Instance.UpdateInventoryUIBuffer();
        _AttachedInventory = null;
    }

    public float GetRealWeight()
    {
        return _ItemDefinition._BaseWeight * _Count;
    }
    public float GetRealVolume()
    {
        return _ItemDefinition._BaseVolume * _Count;
    }
}

public interface ICanBeEquipped { public int _SlotIndex { get; } public float _Durability { get; set; } public float _DurabilityMax { get; set; } public void DurabilityChanged(Item itemInstance); public AsyncOperationHandle<GameObject> _SpawnedHandle { get; set; } }
public interface ICanBeEquippedForDefinition { public int _SlotIndex { get; } public float _Value { get; } public DamageType _DamageType { get; } public Vector3 _PosOffset { get; } public Vector3 _AnglesOffset { get; } public Vector3 _DefPosOffset { get; } public Vector3 _DefAnglesOffset { get; } }
public interface ICanBeConsumed { public float _ConsumeValue { get; } public abstract void Consume(Item item); }
public class CarryItem : Item, ICanBeEquipped
{
    public Inventory _Inventory => _inventory;
    private Inventory _inventory;

    public CarryItem(ItemDefinition def) : base(def)
    {
        _Durability = 100f;
        _DurabilityMax = 100f;
        _inventory = new Inventory(null, this);
    }
    public int _SlotIndex => (_ItemDefinition as ICanBeEquippedForDefinition)._SlotIndex;
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;

    public AsyncOperationHandle<GameObject> _SpawnedHandle { get { return _spawnedHandle; } set { _spawnedHandle = value; } }
    private AsyncOperationHandle<GameObject> _spawnedHandle;

    public void DurabilityChanged(Item itemInstance)
    {
        if (_EquippedHumanoid == null) return;

        if (_Durability <= 0f)
        {
            Unequip(false, true);
        }
    }
}
public class WeaponItem : Item, ICanBeEquipped
{
    public WeaponItem(ItemDefinition def) : base(def)
    {
        _Durability = 100f;
        _DurabilityMax = 100f;
        _SlotIndexDynamic = 8;
    }
    public int _SlotIndexDynamic;
    public int _SlotIndex => _SlotIndexDynamic;
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;

    public AsyncOperationHandle<GameObject> _SpawnedHandle { get { return _spawnedHandle; } set { _spawnedHandle = value; } }
    private AsyncOperationHandle<GameObject> _spawnedHandle;

    public void DurabilityChanged(Item itemInstance)
    {
        if (_EquippedHumanoid == null) return;

        if (_Durability <= 0f)
        {
            Unequip(false, true);
        }
    }
}
public class ClothingItem : Item, ICanBeEquipped
{
    public ClothingItem(ItemDefinition def, float coldProtectionValue) : base(def)
    {
        _Durability = 100f;
        _DurabilityMax = 100f;
        _ColdProtectionValue = coldProtectionValue;
    }
    public int _SlotIndex => (_ItemDefinition as ICanBeEquippedForDefinition)._SlotIndex;
    public GameObject _SpawnedEquipment { get { return _spawnedEquipment; } set { _spawnedEquipment = value; } }
    private GameObject _spawnedEquipment;
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;
    public AsyncOperationHandle<GameObject> _SpawnedHandle { get { return _spawnedHandle; } set { _spawnedHandle = value; } }
    private AsyncOperationHandle<GameObject> _spawnedHandle;
    public void DurabilityChanged(Item itemInstance)
    {
        if (_EquippedHumanoid == null) return;

        if (_Durability <= 0f)
        {
            Unequip(false, true);
        }
    }

    public float _ColdProtectionValue;
}
public class ArmorItem : Item, ICanBeEquipped
{
    public ArmorItem(ItemDefinition def) : base(def)
    {
        _Durability = 100f;
        _DurabilityMax = 100f;
    }
    public int _SlotIndex => (_ItemDefinition as ICanBeEquippedForDefinition)._SlotIndex;
    public GameObject _SpawnedEquipment { get { return _spawnedEquipment; } set { _spawnedEquipment = value; } }
    private GameObject _spawnedEquipment;
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;
    public AsyncOperationHandle<GameObject> _SpawnedHandle { get { return _spawnedHandle; } set { _spawnedHandle = value; } }
    private AsyncOperationHandle<GameObject> _spawnedHandle;
    public void DurabilityChanged(Item itemInstance)
    {
        if (_EquippedHumanoid == null) return;

        if (_Durability <= 0f)
        {
            Unequip(false, true);
        }
    }

    public bool _IsSteel;
    public float _ProtectionValue;
}

#region Items
public class Default_MeleeWeapon : ItemDefinition, ICanBeEquippedForDefinition
{
    public static Default_MeleeWeapon _Instance { get { if (_instance == null) _instance = new Default_MeleeWeapon(); return _instance; } }
    private static Default_MeleeWeapon _instance;
    public int _SlotIndex => 8;
    public float _Value => _damageOverride;
    private float _damageOverride;

    public float _WaitTimeForNextAttackMultiplier => 1f;
    public DamageType _DamageType => DamageType.Crush;
    public Vector3 _PosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _AnglesOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefPosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefAnglesOffset => new Vector3(0f, 0f, 0f);


    public Default_MeleeWeapon() : base("Punch", 0f, 0f, false, true) { }

    public void SetDamageOverride(float damageOverride) => _damageOverride = damageOverride;
}
public class LongSword_1 : ItemDefinition, ICanBeEquippedForDefinition
{
    public static LongSword_1 _Instance { get { if (_instance == null) _instance = new LongSword_1(); return _instance; } }
    private static LongSword_1 _instance;
    public int _SlotIndex => 8;//also 7
    public float _Value => 40f;
    public DamageType _DamageType => DamageType.Cut;
    public Vector3 _PosOffset => new Vector3(0.027f, -0.03f, 0.017f);
    public Vector3 _AnglesOffset => new Vector3(12.995f, -88.867f, 7.152f);
    public Vector3 _DefPosOffset => new Vector3(-0.064f, 0.014f, 0.017f);
    public Vector3 _DefAnglesOffset => new Vector3(2.842f, 14.831f, 258.479f);


    public LongSword_1() : base("LongSword_1", 1f, 0.4f, false, true) { _ItemDescription = Localization._Instance._UI[76] + _Value; }
}
public class ChestArmor_1 : ItemDefinition, ICanBeEquippedForDefinition
{
    public static ChestArmor_1 _Instance { get { if (_instance == null) _instance = new ChestArmor_1(); return _instance; } }
    private static ChestArmor_1 _instance;
    public int _SlotIndex => 4;
    public float _Value => 0.6f;
    public DamageType _DamageType => DamageType.Crush;
    public Vector3 _PosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _AnglesOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefPosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefAnglesOffset => new Vector3(0f, 0f, 0f);

    public ChestArmor_1() : base("ChestArmor_1", 1.5f, 0.75f, true, true) { _ItemDescription = Localization._Instance._UI[75] + _Value; }
}
public class SmallPack : ItemDefinition, ICanBeEquippedForDefinition
{
    public static SmallPack _Instance { get { if (_instance == null) _instance = new SmallPack(); return _instance; } }
    private static SmallPack _instance;
    public int _SlotIndex => 2;
    public float _Value => 300f;
    public DamageType _DamageType => DamageType.Crush;
    public Vector3 _PosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _AnglesOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefPosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefAnglesOffset => new Vector3(0f, 0f, 0f);


    public SmallPack() : base("SmallPack", 1.5f, 0.75f, true, true) { _ItemDescription = Localization._Instance._UI[77] + _Value; }
}
public class Apple : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple() : base("Apple", 0.2f, 0.1f, false, false) { _ItemDescription = Localization._Instance._UI[69] + _ConsumeValue + Localization._Instance._UI[70]; }

    public void Consume(Item item) { bool isVisible = item._AttachedInventory.IsInventoryVisibleInScreen(); item._Count--; if (item._Count == 0) item.DropFrom(false); if (isVisible) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple2 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple2() : base("Apple2", 0.2f, 0.1f, false, false) { _ItemDescription = Localization._Instance._UI[69] + _ConsumeValue + Localization._Instance._UI[70]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple3 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple3() : base("Apple3", 0.2f, 0.1f, false, false) { _ItemDescription = Localization._Instance._UI[69] + _ConsumeValue + Localization._Instance._UI[70]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple4 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple4() : base("Apple4", 0.2f, 0.1f, false, false) { _ItemDescription = Localization._Instance._UI[69] + _ConsumeValue + Localization._Instance._UI[70]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple5 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple5() : base("Apple5", 0.2f, 0.1f, false, false) { _ItemDescription = Localization._Instance._UI[69] + _ConsumeValue + Localization._Instance._UI[70]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple6 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple6() : base("Apple6", 0.2f, 0.1f, false, false) { _ItemDescription = Localization._Instance._UI[69] + _ConsumeValue + Localization._Instance._UI[70]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class CopperCoin : ItemDefinition
{
    public static CopperCoin _Instance { get { if (_instance == null) _instance = new CopperCoin(); return _instance; } }
    private static CopperCoin _instance;
    public CopperCoin() : base("Copper Coin", 0.003f, 0.01f, false, false) { _ItemDescription = Localization._Instance._UI[71]; }
}
public class SilverCoin : ItemDefinition
{
    public static SilverCoin _Instance { get { if (_instance == null) _instance = new SilverCoin(); return _instance; } }
    private static SilverCoin _instance;
    public SilverCoin() : base("Silver Coin", 0.003f, 0.01f, false, false) { _ItemDescription = Localization._Instance._UI[72]; }
}
public class GoldCoin : ItemDefinition
{
    public static GoldCoin _Instance { get { if (_instance == null) _instance = new GoldCoin(); return _instance; } }
    private static GoldCoin _instance;
    public GoldCoin() : base("Gold Coin", 0.006f, 0.01f, false, false) { _ItemDescription = Localization._Instance._UI[73]; }
}
public class DiamondShard : ItemDefinition
{
    public static DiamondShard _Instance { get { if (_instance == null) _instance = new DiamondShard(); return _instance; } }
    private static DiamondShard _instance;
    public DiamondShard() : base("Diamond Shard", 0.01f, 0.005f, false, false) { _ItemDescription = Localization._Instance._UI[74]; }
}
#endregion