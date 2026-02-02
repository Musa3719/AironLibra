using NUnit;
using System.Collections;
using System.Collections.Generic;
using UMA;
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

    public float _CarryCapacity => _IsHuman ? (_InventoryHolder._Human._MuscleLevel * 80f + _InventoryHolder._Human._FatLevel * 20f + _InventoryHolder._Human._Height * 40f) : (_IsBackCarry ? 50f : 500f);
    public float _CarryCapacityUse { get; set; }

    public string _Name => _CanEquip ? _InventoryHolder._Human._Name : (_IsBackCarry ? _BackCarryToItem._ItemDefinition._Name : _InventoryHolder.gameObject.name);
    public bool _IsHuman => _InventoryHolder == null ? false : GameManager._Instance._HumanMask.Contains(_InventoryHolder.gameObject.layer);
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
        _ItemLimit = _IsHuman ? 8 : (_IsBackCarry ? 21 : 32);
        _CanEquip = _IsHuman;
        _CanCarryBigItems = !_IsHuman;
        _isPublic = !_IsHuman;
    }
    private void InitItems()
    {
        //for testing
        if (_IsBackCarry) return;
        new ArmorItem(ChestArmor_1._Instance).TakenTo(this);
        new WeaponItem(LongSword_1._Instance).TakenTo(this);
        new WeaponItem(Crossbow._Instance).TakenTo(this);
        new WeaponItem(SurvivalBow._Instance).TakenTo(this);
        new WeaponItem(HuntingBow._Instance).TakenTo(this);
        new WeaponItem(CompositeBow._Instance).TakenTo(this);
        if (_InventoryHolder?._Human != null)
            new CarryItem(Backpack._Instance).Equip(this);

        int random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(Apple._Instance).TakenTo(this);
        }
        random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(Arrow._Instance).TakenTo(this);
        }
        random = Random.Range(1, 15);
        for (int i = 0; i < random; i++)
        {
            new Item(BoltArrow._Instance).TakenTo(this);
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

    public float CurrentCarryCapacityUseCommon(bool isRecursive = false)
    {
        float sum = 0f;
        foreach (var item in _Items)
        {
            sum += item.GetRealCapacity();
        }

        if (isRecursive) return sum;

        if (_IsHuman)
        {
            if (_IsBackCarry)
            {
                if (_BackCarryToEquippedHuman != null)
                {
                    sum += _BackCarryToEquippedHuman._Inventory.CurrentCarryCapacityUseCommon(true);
                    sum += _BackCarryToEquippedHuman._Inventory.GetEquipmentsTotalWeight();
                }
            }
            else
            {
                float before = sum;
                if (_InventoryHolder._Human._BackCarryItemRef != null)
                    sum += (_InventoryHolder._Human._BackCarryItemRef as CarryItem)._Inventory.CurrentCarryCapacityUseCommon(true);
                sum += GetEquipmentsTotalWeight();
            }

        }
        return sum;
    }
    private float GetEquipmentsTotalWeight()
    {
        float sum = 0f;
        if (_InventoryHolder._Human._HeadGearItemRef != null)
            sum += _InventoryHolder._Human._HeadGearItemRef.GetRealCapacity();
        if (_InventoryHolder._Human._GlovesItemRef != null)
            sum += _InventoryHolder._Human._GlovesItemRef.GetRealCapacity();
        if (_InventoryHolder._Human._BackCarryItemRef != null)
            sum += _InventoryHolder._Human._BackCarryItemRef.GetRealCapacity();
        if (_InventoryHolder._Human._ClothingItemRef != null)
            sum += _InventoryHolder._Human._ClothingItemRef.GetRealCapacity();
        if (_InventoryHolder._Human._ChestArmorItemRef != null)
            sum += _InventoryHolder._Human._ChestArmorItemRef.GetRealCapacity();
        if (_InventoryHolder._Human._LegsArmorItemRef != null)
            sum += _InventoryHolder._Human._LegsArmorItemRef.GetRealCapacity();
        if (_InventoryHolder._Human._BootsItemRef != null)
            sum += _InventoryHolder._Human._BootsItemRef.GetRealCapacity();
        if (_InventoryHolder._Human._LeftHandEquippedItemRef != null)
            sum += _InventoryHolder._Human._LeftHandEquippedItemRef.GetRealCapacity();
        if (_InventoryHolder._Human._RightHandEquippedItemRef != null)
            sum += _InventoryHolder._Human._RightHandEquippedItemRef.GetRealCapacity();
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
        //if (GetCurrentCarryCapacity(false) + item.GetRealCapacity() > _CarryCapacity) return false;
        return true;
    }
    public bool CanTakeThisItem(Item item)
    {
        if (CanTakeThisItemCommon(item)) return true;
        if (_IsHuman && _InventoryHolder.GetComponent<Humanoid>()._BackCarryItemRef != null && (_InventoryHolder.GetComponent<Humanoid>()._BackCarryItemRef as CarryItem)._Inventory.CanTakeThisItemCommon(item)) return true;
        return false;
    }
    public bool CanEquipThisItem(Item item, bool isToLeftHand)
    {
        if (!(item is ICanBeEquipped)) return false;
        if (!_CanEquip) return false;
        if (!CanTakeBigItem(item, true)) return false;
        if (!item.CheckViableForHandEquip(this, isToLeftHand)) return false;
        return true;
    }
    public bool CanTakeThisItemCommon(Item item)
    {
        if (_IsBackCarry && item is CarryItem) return false;

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
    public Item _ItemRef;
    public AssetReferenceGameObject _AssetRef;
    public AsyncOperationHandle<GameObject>? _SpawnHandle;
    public CarriableObject _CarriableObjectReferance => _SpawnHandle.HasValue ? _SpawnHandle.Value.Result?.GetComponent<CarriableObject>() : null;
}
public abstract class ItemDefinition
{
    public string _Name;
    public float _BaseWeight;
    public bool _IsBig;
    public bool _CanBeEquipped;

    public string _ItemDescription;

    public ItemDefinition(string name, float weight, bool isBig, bool canBeEquipped)
    {
        _Name = name;
        _BaseWeight = weight;
        _IsBig = isBig;
        _CanBeEquipped = canBeEquipped;
    }
}

public class Item
{
    public ItemDefinition _ItemDefinition;
    public ItemHandleData _ItemHandleData;

    public Inventory _AttachedInventoryCommon => _IsEquipped ? _EquippedHumanoid._Inventory : _AttachedInventory;
    public Inventory _AttachedInventory;
    public Humanoid _EquippedHumanoid;

    public int _Count;
    public bool _IsEquipped => _EquippedHumanoid != null;
    public bool _IsSplittingBuffer;

    public Item(ItemDefinition def)
    {
        _ItemDefinition = def;
        _ItemHandleData = new ItemHandleData();
        _ItemHandleData._ItemRef = this;
        _ItemHandleData._AssetRef = GameManager._Instance._ItemNameToPrefab[def._Name];
        _Count = 1;
    }
    public Item Copy()
    {
        Item copy = new Item(_ItemDefinition);
        copy._AttachedInventory = _AttachedInventory;
        copy._EquippedHumanoid = _EquippedHumanoid;
        copy._ItemHandleData = new ItemHandleData();
        copy._ItemHandleData._AssetRef = _ItemHandleData._AssetRef;
        copy._ItemHandleData._ItemRef = copy;
        copy._Count = _Count;
        return copy;
    }
    /*public virtual void SpawnInstanceToWorld(Vector3 pos, Vector3 angles)
    {
        if (!GameManager._Instance._ItemNameToPrefab.ContainsKey(_ItemDefinition._Name)) { Debug.LogError("Item(" + _ItemDefinition._Name + ") Prefab Not Found!"); return; }
        GameManager._Instance.CreateEnvironmentPrefabToWorld(GameManager._Instance._ItemNameToPrefab[_ItemDefinition._Name], GameManager._Instance._EnvironmentTransform, pos, angles, ref _ItemHandleData);
    }
    public virtual void DespawnInstanceFromWorld()
    {
        GameManager._Instance.DestroyEnvironmentPrefabFromWorld(_ItemHandleData);
    }*/
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
    public bool CheckViableForHandEquip(Inventory targetInv, bool isTargetLeft)
    {
        int index = (this as ICanBeEquipped)._SlotIndex;
        if (index != 7 && index != 8) return true;
        if (!(_ItemDefinition is IWeapon equippedForDefinition)) return true;

        if (isTargetLeft)
        {
            if (equippedForDefinition._IsTwoHandedWeapon) return false;
            if (targetInv._InventoryHolder._Human._RightHandEquippedItemRef != null && (targetInv._InventoryHolder._Human._RightHandEquippedItemRef._ItemDefinition as IWeapon)._IsTwoHandedWeapon) return false;
            return true;
        }
        else
        {
            if (equippedForDefinition._IsTwoHandedWeapon && targetInv._InventoryHolder._Human._LeftHandEquippedItemRef != null)
                return false;
            return true;
        }
    }

    private void SpawnItemHandle(Transform parentTransform, Vector3 pos, Vector3 angles, ICanBeEquipped equippableItem)
    {
        equippableItem._SpawnedHandle = GameManager._Instance._ItemNameToPrefab[_ItemDefinition._Name].InstantiateAsync(parentTransform);
        equippableItem._SpawnedHandle.Value.Completed += (handle) =>
        {
            if (!handle.IsValid() || !handle.IsDone || handle.Result == null) return; handle.Result.transform.localPosition = pos; handle.Result.transform.localEulerAngles = angles;
            if (equippableItem is WeaponItem weaponItem) handle.Result.GetComponent<Weapon>().Init(weaponItem);
            if (equippableItem is CarryItem carryItem) carryItem.Init();
        };
    }


    public void SpawnBackCarryItem()
    {
        if (_ItemHandleData._SpawnHandle.HasValue && _ItemHandleData._SpawnHandle.Value.IsValid()) return;

        ICanBeEquipped carryItem = this as ICanBeEquipped;
        Transform targetTransform = _EquippedHumanoid._BackpackTransform;
        ICanBeEquippedForDefinition canBeEquippedForDefinition = (_ItemDefinition as ICanBeEquippedForDefinition);
        SpawnItemHandle(targetTransform, canBeEquippedForDefinition._PosOffset, canBeEquippedForDefinition._AnglesOffset, carryItem);
    }
    public void DespawnBackCarryItem()
    {
        ICanBeEquipped carryItem = this as ICanBeEquipped;
        if (carryItem._SpawnedHandle.HasValue && carryItem._SpawnedHandle.Value.IsValid())
        {
            Addressables.ReleaseInstance(carryItem._SpawnedHandle.Value);
        }
    }

    public void SpawnHandItem()
    {
        if (_ItemHandleData._SpawnHandle.HasValue && _ItemHandleData._SpawnHandle.Value.IsValid()) return;

        ICanBeEquipped handItem = this as ICanBeEquipped;
        Transform targetTransform = handItem._SlotIndex == 8 ? (_EquippedHumanoid._IsInCombatMode ? _EquippedHumanoid._RightHandHolderTransform : _EquippedHumanoid._RightWeaponHolder) :
                (_EquippedHumanoid._IsInCombatMode ? _EquippedHumanoid._LeftHandHolderTransform : _EquippedHumanoid._LeftWeaponHolder);
        ICanBeEquippedForDefinition canBeEquippedForDefinition = (_ItemDefinition as ICanBeEquippedForDefinition);
        SpawnItemHandle(targetTransform, _EquippedHumanoid._IsInCombatMode ? canBeEquippedForDefinition._PosOffset : canBeEquippedForDefinition._DefPosOffset,
            _EquippedHumanoid._IsInCombatMode ? canBeEquippedForDefinition._AnglesOffset : canBeEquippedForDefinition._DefAnglesOffset, handItem);

        if (_EquippedHumanoid._IsInCombatMode)
        {
            _EquippedHumanoid.ChangeAnimation(handItem._SlotIndex == 8 ? "RightHoldWeapon" : "LeftHoldWeapon");
        }
    }

    public void DespawnHandItem()
    {
        if (_EquippedHumanoid._IsAttacking)
            HandStateMethods.AttackIsOver(_EquippedHumanoid, _EquippedHumanoid._LastAttackWeapon);

        ICanBeEquipped handItem = this as ICanBeEquipped;
        if (handItem._SpawnedHandle.HasValue && handItem._SpawnedHandle.Value.IsValid())
        {
            Addressables.ReleaseInstance(handItem._SpawnedHandle.Value);
        }
    }

    public void Equip(Inventory targetInv, bool isToLeftHand = false)
    {
        if (!targetInv.CanEquipThisItem(this, isToLeftHand)) return;

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
            else
                weaponItem._SlotIndexDynamic = 8;
        }

        int slotIndex = (this as ICanBeEquipped)._SlotIndex;
        ArrangeExistingEquipSlot(targetInv, slotIndex);
        SetHumanoidEquipSlot(slotIndex, true);

        if (this is ClothingItem || this is ArmorItem)
        {
            if (_EquippedHumanoid._UmaDynamicAvatar != null && _EquippedHumanoid._UmaDynamicAvatar.BuildCharacterEnabled)
                _EquippedHumanoid.WearWardrobe(_EquippedHumanoid.GetRecipeFromItemName(_ItemDefinition._Name));
        }
        else if (slotIndex == 7 || slotIndex == 8)
        {
            if (_EquippedHumanoid._UmaDynamicAvatar != null && _EquippedHumanoid._UmaDynamicAvatar.BuildCharacterEnabled)
                SpawnHandItem();
        }
        else if (slotIndex == 2 && _EquippedHumanoid._UmaDynamicAvatar != null && _EquippedHumanoid._UmaDynamicAvatar.BuildCharacterEnabled)
            SpawnBackCarryItem();

        SetCurrentCarryCapacityUse(_EquippedHumanoid._Inventory);

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
            TakenTo(_EquippedHumanoid._Inventory, true);
        SetHumanoidEquipSlot((this as ICanBeEquipped)._SlotIndex, false);

        if (this is ClothingItem || this is ArmorItem)
        {
            _EquippedHumanoid.RemoveWardrobe(_EquippedHumanoid.GetRecipeFromItemName(_ItemDefinition._Name));
        }
        else if (this is WeaponItem weaponItem)
        {
            weaponItem._SlotIndexDynamic = 8;
            DespawnHandItem();
        }
        else if (this is CarryItem carryItem)
            DespawnBackCarryItem();

        SetCurrentCarryCapacityUse(_EquippedHumanoid._Inventory);
        _EquippedHumanoid = null;

        if (isVisible)
            GameManager._Instance.UpdateInventoryUIBuffer();
    }

    public virtual void TakenTo(Inventory inv, bool isFromUnequip = false)
    {
        if (inv == null) { Debug.LogError("inventory null"); return; }
        if (!inv.CanTakeThisItemCommon(this))
        {
            if (inv._IsHuman && inv._InventoryHolder._Human._BackCarryItemRef != null) { inv = (inv._InventoryHolder._Human._BackCarryItemRef as CarryItem)._Inventory; } else { if (isFromUnequip) DropFrom(true); return; }
        }
        if (!inv.CanTakeThisItem(this))
        {
            if (isFromUnequip) DropFrom(true);
            return;
        }

        Inventory oldInv = null;
        if (_AttachedInventory != null)
        {
            oldInv = _AttachedInventory;
            DropFrom(false);
        }
        inv.AddItemToInventory(this);
        _AttachedInventory = inv;

        SetCurrentCarryCapacityUse(inv);
        if (oldInv != null)
            SetCurrentCarryCapacityUse(oldInv);

        if (inv._IsBackCarry)
            inv._BackCarryToItem.StretchChanged();

        if (_AttachedInventory.IsInventoryVisibleInScreen() || (oldInv != null && oldInv.IsInventoryVisibleInScreen()))
            GameManager._Instance.UpdateInventoryUIBuffer();
    }
    public void SetCurrentCarryCapacityUse(Inventory inventory)
    {
        if (inventory._BackCarryToEquippedHuman != null)
            inventory._BackCarryToEquippedHuman._Inventory._CarryCapacityUse = inventory._BackCarryToEquippedHuman._Inventory.CurrentCarryCapacityUseCommon();
        else
            inventory._CarryCapacityUse = inventory.CurrentCarryCapacityUseCommon();
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
            Vector3 spawnPos = _EquippedHumanoid != null ? _EquippedHumanoid.transform.position : (_AttachedInventory._InventoryHolder != null ? _AttachedInventory._InventoryHolder.transform.position : (_AttachedInventory._BackCarryToEquippedHuman._InventoryHolder.transform.position));
            GameManager._Instance.CreateNewCarriableObjectToWorld(this, spawnPos);
        }

        if (_AttachedInventory != null)
            SetCurrentCarryCapacityUse(_AttachedInventory);


        if (_AttachedInventory != null && _AttachedInventory._IsBackCarry)
            _AttachedInventory._BackCarryToItem.StretchChanged();

        if ((_AttachedInventory != null && _AttachedInventory.IsInventoryVisibleInScreen()) || (_EquippedHumanoid != null && _EquippedHumanoid._Inventory.IsInventoryVisibleInScreen()))
            GameManager._Instance.UpdateInventoryUIBuffer();
        _AttachedInventory = null;
    }

    public float GetRealCapacity()
    {
        return _ItemDefinition._BaseWeight * _Count;
    }
}

public interface ICanBeEquipped { public int _SlotIndex { get; } public float _Durability { get; set; } public float _DurabilityMax { get; set; } public void DurabilityChanged(Item itemInstance); public AsyncOperationHandle<GameObject>? _SpawnedHandle { get; set; } }
public interface ICanLoadItem { public Item _LoadedItem { get; set; } }
public interface ICanBeEquippedForDefinition { public int _SlotIndex { get; } public Vector3 _PosOffset { get; } public Vector3 _AnglesOffset { get; } public Vector3 _DefPosOffset { get; } public Vector3 _DefAnglesOffset { get; } }
public interface IWeapon : ICanBeEquippedForDefinition { public bool _IsTwoHandedWeapon { get; } public DamageType _DamageType { get; } public float _BaseDamage { get; } public float _BaseArmorPen { get; } public float _BaseSpeed { get; } }
public interface IRangedWeapon : IWeapon { public string _AnimName { get; } public bool _IsCrossbow { get; } }
public interface IMeleeWeapon : IWeapon { }
public interface IArmor : ICanBeEquippedForDefinition { public float _ProtectionValue { get; } }
public interface ICarry : ICanBeEquippedForDefinition { public float _CarryCapacity { get; } }
public interface ICanBeConsumed { public float _ConsumeValue { get; } public abstract void Consume(Item item); }
public class CarryItem : Item, ICanBeEquipped
{
    public Inventory _Inventory => _inventory;
    private Inventory _inventory;

    public CarryItem(ItemDefinition def) : base(def)
    {
        _Durability = 100f;
        _DurabilityMax = 100f;
        _inventory = new Inventory(null, backCarryToItem: this);
    }
    public int _SlotIndex => (_ItemDefinition as ICanBeEquippedForDefinition)._SlotIndex;
    public float _CarryCapacity => (_ItemDefinition as ICarry)._CarryCapacity;
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;

    public AsyncOperationHandle<GameObject>? _SpawnedHandle { get => _ItemHandleData?._SpawnHandle; set { _ItemHandleData._SpawnHandle = value; } }
    private Coroutine _stretchCoroutine;
    public void DurabilityChanged(Item itemInstance)
    {
        if (_EquippedHumanoid == null) return;

        if (_Durability <= 0f)
        {
            Unequip(false, true);
        }
    }
    public void Init()
    {
        StretchChanged();
    }
    public void StretchChanged()
    {
        float normalizedFullness = _Inventory._CarryCapacityUse / _inventory._CarryCapacity;
        GameManager._Instance.CoroutineCall(ref _stretchCoroutine, StretchChangedCoroutine(normalizedFullness), GameManager._Instance);
    }
    private IEnumerator StretchChangedCoroutine(float normalizedFullness)
    {
        while (_ItemHandleData._SpawnHandle?.Result == null) { Debug.LogError("Backpack handle is null"); yield return null; }
        float stretch = (1f - normalizedFullness) * 80f;
        Transform backpack = _ItemHandleData._SpawnHandle.Value.Result.transform;
        backpack.Find("BackpackMesh/backpack").GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, stretch);
        if (_EquippedHumanoid != null)
        {
            Vector3 tempPos = backpack.Find("BackpackMesh/Armature/Stretch").localPosition;
            backpack.Find("BackpackMesh/Armature/Stretch").localPosition = new Vector3(tempPos.x, tempPos.y, 0.00175f + 0.00165f * (_EquippedHumanoid._FatLevel - 0.5f) + 0.00165f * (_EquippedHumanoid._MuscleLevel - 0.5f));
        }
    }
}
public class WeaponItem : Item, ICanBeEquipped, ICanLoadItem
{
    public Item _LoadedItem { get; set; }
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

    public AsyncOperationHandle<GameObject>? _SpawnedHandle { get => _ItemHandleData?._SpawnHandle; set { _ItemHandleData._SpawnHandle = value; } }

    public void DurabilityChanged(Item itemInstance)
    {
        if (_EquippedHumanoid == null) return;

        if (_Durability <= 0f)
        {
            Unequip(false, true);
        }
    }
}

public class ArmorItem : Item, ICanBeEquipped
{
    public ArmorItem(ItemDefinition def) : base(def)
    {
        _Durability = 100f;
        _DurabilityMax = 100f;
    }
    public int _SlotIndex => (_ItemDefinition as ICanBeEquippedForDefinition)._SlotIndex;
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;
    public AsyncOperationHandle<GameObject>? _SpawnedHandle { get => _ItemHandleData?._SpawnHandle; set { _ItemHandleData._SpawnHandle = value; } }
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
public class ClothingItem : Item, ICanBeEquipped
{
    public ClothingItem(ItemDefinition def, float coldProtectionValue) : base(def)
    {
        _Durability = 100f;
        _DurabilityMax = 100f;
        _ColdProtectionValue = coldProtectionValue;
    }
    public int _SlotIndex => (_ItemDefinition as ICanBeEquippedForDefinition)._SlotIndex;
    public float _Durability { get { return _durability; } set { _durability = value; } }
    private float _durability;
    public float _DurabilityMax { get { return _durabilityMax; } set { _durabilityMax = value; } }
    private float _durabilityMax;
    public AsyncOperationHandle<GameObject>? _SpawnedHandle { get => _ItemHandleData?._SpawnHandle; set { _ItemHandleData._SpawnHandle = value; } }
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
public class Backpack : ItemDefinition, ICarry
{
    public static Backpack _Instance { get { if (_instance == null) _instance = new Backpack(); return _instance; } }
    private static Backpack _instance;
    public int _SlotIndex => 2;
    public bool _IsTwoHandedWeapon => false;
    public float _CarryCapacity => 300f;
    public DamageType _DamageType => DamageType.Crush;
    public Vector3 _PosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _AnglesOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefPosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefAnglesOffset => new Vector3(0f, 0f, 0f);


    public Backpack() : base("Backpack", 3f, true, true) { _ItemDescription = Localization._Instance._UI[77] + _CarryCapacity; }
}

#region Items
public class Default_MeleeWeapon : ItemDefinition, IMeleeWeapon
{
    public static Default_MeleeWeapon _Instance { get { if (_instance == null) _instance = new Default_MeleeWeapon(); return _instance; } }
    private static Default_MeleeWeapon _instance;
    public int _SlotIndex => 8;
    public bool _IsTwoHandedWeapon => false;
    public float _BaseDamage => _damageOverride;
    public float _BaseArmorPen => 0f;
    public float _BaseSpeed => 1f;

    private float _damageOverride;

    public DamageType _DamageType => DamageType.Crush;
    public Vector3 _PosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _AnglesOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefPosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefAnglesOffset => new Vector3(0f, 0f, 0f);


    public Default_MeleeWeapon() : base("Punch", 0f, false, true) { }

    public void SetDamageOverride(float damageOverride) => _damageOverride = damageOverride;
}
public class LongSword_1 : ItemDefinition, IMeleeWeapon
{
    public static LongSword_1 _Instance { get { if (_instance == null) _instance = new LongSword_1(); return _instance; } }
    private static LongSword_1 _instance;
    public int _SlotIndex => 8;//also 7
    public bool _IsTwoHandedWeapon => false;
    public float _BaseDamage => 40f;
    public float _BaseArmorPen => 15f;
    public float _BaseSpeed => 1.2f;
    public DamageType _DamageType => DamageType.Cut;
    public Vector3 _PosOffset => new Vector3(0.027f, -0.03f, 0.017f);
    public Vector3 _AnglesOffset => new Vector3(12.995f, -88.867f, 7.152f);
    public Vector3 _DefPosOffset => new Vector3(-0.085f, -0.01f, 0.016f);
    public Vector3 _DefAnglesOffset => new Vector3(-0.936f, -9.928f, -94.704f);


    public LongSword_1() : base("LongSword_1", 7.5f, true, true) { _ItemDescription = Localization._Instance._UI[76] + Localization._Instance._UI[83] + _BaseDamage + Localization._Instance._UI[85] + _BaseArmorPen; }
}
public class Crossbow : ItemDefinition, IRangedWeapon
{
    public static Crossbow _Instance { get { if (_instance == null) _instance = new Crossbow(); return _instance; } }
    private static Crossbow _instance;
    public int _SlotIndex => 8;
    public bool _IsTwoHandedWeapon => true;
    public float _BaseDamage => 55f;
    public float _BaseArmorPen => 45f;
    public float _BaseSpeed => 0.7f;
    public DamageType _DamageType => DamageType.Pierce;
    public Vector3 _PosOffset => new Vector3(-0.015f, 0.022f, 0.016f);
    public Vector3 _AnglesOffset => new Vector3(183.483f, 79.567f, 179.664f);
    public Vector3 _DefPosOffset => new Vector3(0.01f, 0.013f, 0.019f);
    public Vector3 _DefAnglesOffset => new Vector3(6.842f, 82.137f, 178.847f);
    public string _AnimName => "Crossbow Trigger";
    public bool _IsCrossbow => true;
    public Item _LoadedItem { get; set; }

    public Crossbow() : base("Crossbow", 10f, true, true) { _ItemDescription = Localization._Instance._UI[78] + Localization._Instance._UI[83] + _BaseDamage + Localization._Instance._UI[85] + _BaseArmorPen; }
}
public class SurvivalBow : ItemDefinition, IRangedWeapon
{
    public static SurvivalBow _Instance { get { if (_instance == null) _instance = new SurvivalBow(); return _instance; } }
    private static SurvivalBow _instance;
    public int _SlotIndex => 8;
    public bool _IsTwoHandedWeapon => true;
    public float _BaseDamage => 32f;
    public float _BaseArmorPen => 10f;
    public float _BaseSpeed => 0.8f;
    public DamageType _DamageType => DamageType.Pierce;
    public bool _IsCrossbow => false;
    public string _AnimName => "Bow Trigger";
    public Vector3 _PosOffset => new Vector3(0.06f, 0.029f, 0.008f);
    public Vector3 _AnglesOffset => new Vector3(14.964f, -92.576f, 4.083f);
    public Vector3 _DefPosOffset => new Vector3(-0.152f, 0.08f, -0.118f);
    public Vector3 _DefAnglesOffset => new Vector3(90f, 0f, -89.582f);
    public Item _LoadedItem { get; set; }

    public SurvivalBow() : base("SurvivalBow", 8f, true, true) { _ItemDescription = Localization._Instance._UI[80] + Localization._Instance._UI[83] + _BaseDamage + Localization._Instance._UI[85] + _BaseArmorPen; }
}
public class HuntingBow : ItemDefinition, IRangedWeapon
{
    public static HuntingBow _Instance { get { if (_instance == null) _instance = new HuntingBow(); return _instance; } }
    private static HuntingBow _instance;
    public int _SlotIndex => 8;
    public bool _IsTwoHandedWeapon => true;
    public float _BaseDamage => 50f;
    public float _BaseArmorPen => 20f;
    public float _BaseSpeed => 1f;
    public bool _IsCrossbow => false;
    public string _AnimName => "Bow Trigger";
    public DamageType _DamageType => DamageType.Pierce;
    public Vector3 _PosOffset => new Vector3(0.059f, 0.002f, 0.014f);
    public Vector3 _AnglesOffset => new Vector3(12.565f, -92.751f, 2.941f);
    public Vector3 _DefPosOffset => new Vector3(-0.153f, 0.09f, -0.114f);
    public Vector3 _DefAnglesOffset => new Vector3(89.886f, 0f, -90.639f);
    public Item _LoadedItem { get; set; }

    public HuntingBow() : base("HuntingBow", 6.5f, true, true) { _ItemDescription = Localization._Instance._UI[81] + Localization._Instance._UI[83] + _BaseDamage + Localization._Instance._UI[85] + _BaseArmorPen; }
}
public class CompositeBow : ItemDefinition, IRangedWeapon
{
    public static CompositeBow _Instance { get { if (_instance == null) _instance = new CompositeBow(); return _instance; } }
    private static CompositeBow _instance;
    public int _SlotIndex => 8;
    public bool _IsTwoHandedWeapon => true;
    public float _BaseDamage => 45f;
    public float _BaseArmorPen => 40f;
    public float _BaseSpeed => 1.25f;
    public bool _IsCrossbow => false;
    public string _AnimName => "Bow Trigger";
    public DamageType _DamageType => DamageType.Pierce;
    public Vector3 _PosOffset => new Vector3(0.014f, 0.0084f, 0.0136f);
    public Vector3 _AnglesOffset => new Vector3(165.036f, 87.424f, -175.917f);
    public Vector3 _DefPosOffset => new Vector3(-0.26f, 0.098f, -0.126f);
    public Vector3 _DefAnglesOffset => new Vector3(90.16199f, 76.563f, -14.73901f);
    public Item _LoadedItem { get; set; }

    public CompositeBow() : base("CompositeBow", 5f, true, true) { _ItemDescription = Localization._Instance._UI[82] + Localization._Instance._UI[83] + _BaseDamage + Localization._Instance._UI[85] + _BaseArmorPen; }
}
public class ChestArmor_1 : ItemDefinition, IArmor
{
    public static ChestArmor_1 _Instance { get { if (_instance == null) _instance = new ChestArmor_1(); return _instance; } }
    private static ChestArmor_1 _instance;
    public int _SlotIndex => 4;
    public bool _IsTwoHandedWeapon => false;
    public float _ProtectionValue => 55f;
    public DamageType _DamageType => DamageType.Crush;
    public Vector3 _PosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _AnglesOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefPosOffset => new Vector3(0f, 0f, 0f);
    public Vector3 _DefAnglesOffset => new Vector3(0f, 0f, 0f);

    public ChestArmor_1() : base("ChestArmor_1", 35f, true, true) { _ItemDescription = Localization._Instance._UI[75] + Localization._Instance._UI[84] + _ProtectionValue; }
}

public class Apple : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple() : base("Apple", 0.2f, false, false) { _ItemDescription = Localization._Instance._UI[87] + _ConsumeValue + Localization._Instance._UI[88]; }

    public void Consume(Item item) { bool isVisible = item._AttachedInventory.IsInventoryVisibleInScreen(); item._Count--; if (item._Count == 0) item.DropFrom(false); if (isVisible) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Arrow : ItemDefinition
{
    public static Arrow _Instance { get { if (_instance == null) _instance = new Arrow(); return _instance; } }
    private static Arrow _instance;
    public Arrow() : base("Arrow", 0.3f, false, false) { _ItemDescription = Localization._Instance._UI[70]; }
}
public class BoltArrow : ItemDefinition
{
    public static BoltArrow _Instance { get { if (_instance == null) _instance = new BoltArrow(); return _instance; } }
    private static BoltArrow _instance;
    public BoltArrow() : base("BoltArrow", 0.2f, false, false) { _ItemDescription = Localization._Instance._UI[79]; }
}

public class Apple2 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple2() : base("Apple2", 0.2f, false, false) { _ItemDescription = Localization._Instance._UI[87] + _ConsumeValue + Localization._Instance._UI[88]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple3 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple3() : base("Apple3", 0.2f, false, false) { _ItemDescription = Localization._Instance._UI[87] + _ConsumeValue + Localization._Instance._UI[88]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple4 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple4() : base("Apple4", 0.2f, false, false) { _ItemDescription = Localization._Instance._UI[87] + _ConsumeValue + Localization._Instance._UI[88]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple5 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple5() : base("Apple5", 0.2f, false, false) { _ItemDescription = Localization._Instance._UI[87] + _ConsumeValue + Localization._Instance._UI[88]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class Apple6 : ItemDefinition, ICanBeConsumed
{
    public static Apple _Instance { get { if (_instance == null) _instance = new Apple(); return _instance; } }
    private static Apple _instance;
    public float _ConsumeValue => 5f;
    public Apple6() : base("Apple6", 0.2f, false, false) { _ItemDescription = Localization._Instance._UI[87] + _ConsumeValue + Localization._Instance._UI[88]; }

    public void Consume(Item item) { item._Count--; if (item._Count == 0) item.DropFrom(false); if (item._AttachedInventory.IsInventoryVisibleInScreen()) GameManager._Instance.UpdateInventoryUIBuffer(); }
}
public class CopperCoin : ItemDefinition
{
    public static CopperCoin _Instance { get { if (_instance == null) _instance = new CopperCoin(); return _instance; } }
    private static CopperCoin _instance;
    public CopperCoin() : base("Copper Coin", 0.003f, false, false) { _ItemDescription = Localization._Instance._UI[71]; }
}
public class SilverCoin : ItemDefinition
{
    public static SilverCoin _Instance { get { if (_instance == null) _instance = new SilverCoin(); return _instance; } }
    private static SilverCoin _instance;
    public SilverCoin() : base("Silver Coin", 0.003f, false, false) { _ItemDescription = Localization._Instance._UI[72]; }
}
public class GoldCoin : ItemDefinition
{
    public static GoldCoin _Instance { get { if (_instance == null) _instance = new GoldCoin(); return _instance; } }
    private static GoldCoin _instance;
    public GoldCoin() : base("Gold Coin", 0.006f, false, false) { _ItemDescription = Localization._Instance._UI[73]; }
}
public class DiamondShard : ItemDefinition
{
    public static DiamondShard _Instance { get { if (_instance == null) _instance = new DiamondShard(); return _instance; } }
    private static DiamondShard _instance;
    public DiamondShard() : base("Diamond Shard", 0.01f, false, false) { _ItemDescription = Localization._Instance._UI[74]; }
}
#endregion