using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Inventory _Inventory { get; set; }
    public Item _ItemRef { get; set; }
    public bool _IsCarryMode { get; set; }
    public bool _IsHoverFromGamepad { get; set; }
    public bool _IsPlayerInventory => transform.parent.parent.name.StartsWith("Own") || transform.parent.parent.parent.name.StartsWith("Own");
    public bool _IsBackCarryInventory => transform.parent.parent.name.StartsWith("Back");
    public bool _IsEquipmentSlot => transform.parent.name.Equals("Equipments");
    private float _lastTimeClicked;

    private bool _isHovered;
    private bool _isHoveredNotProcess;
    private void OnDisable()
    {
        MouseExit();
        GameManager._Instance._InteractMenuSlotUI = null;
        GameManager._Instance._InventoryItemInteractPopup.SetActive(false);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnter(false);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExit();
    }
    public void PointerEnter(bool isHoverFromGamepad)
    {
        if (_ItemRef == null)
        {
            MouseExit();
            _isHoveredNotProcess = true;
        }
        else
        {
            MouseEnter(isHoverFromGamepad);
        }
    }
    public void PointerExit()
    {
        MouseExit();
    }
    public void MouseEnter(bool isHoverFromGamepad)
    {
        _isHovered = true;
        _isHoveredNotProcess = true;
        _IsHoverFromGamepad = isHoverFromGamepad;
        OnHover();
    }
    public void MouseExit()
    {
        _isHovered = false;
        _isHoveredNotProcess = false;
        _IsHoverFromGamepad = false;
        GameManager._Instance._InventoryItemInfoPopup.SetActive(false);
    }

    private void Update()
    {
        if (_isHoveredNotProcess) { if (_ItemRef != null) _isHovered = true; }
        if (!_isHovered) return;
        if (_isHovered && _ItemRef == null) { _isHovered = false; return; }

        if (M_Input.GetButtonDown("Fire1") && !_IsHoverFromGamepad)
        {
            MouseClick();
        }
        if (M_Input.GetButtonDown("Fire2") && !_IsHoverFromGamepad)
        {
            RightClick();
        }

        if (_IsHoverFromGamepad && M_Input.IsQuickBackCarryInteractionInput())
        {
            GameManager._Instance.TakeOrSend(this, true, true);
        }
    }
    public void MouseClick()
    {
        GameManager._Instance._CarryUITimerForMouse = 0f;
        GameManager._Instance.PlayButtonSound(0.115f, 1.35f);
        if (M_Input.GetButton("Run"))
        {
            QuickTake();
        }
        else if (M_Input.IsQuickBackCarryInteractionInput())
        {
            GameManager._Instance.TakeOrSend(this, true, true);
        }
        else if (_lastTimeClicked + 0.32f > Time.unscaledTime)
        {
            DoubleClicked();
        }
        else
        {
            _lastTimeClicked = Time.unscaledTime;
            GameManager._Instance._LastClickedSlotUI = this;
        }
    }
    public void CarryStarted()
    {
        if (GameManager._Instance._InventoryCarryModeSlotUI != null) { Debug.LogError("_InventoryCarryModeSlotUI is not null!"); return; }

        GameManager._Instance._InventoryItemInteractPopup.SetActive(false);
        _IsCarryMode = true;
        GameManager._Instance._InventoryCarryModeSlotUI = this;
        GameManager._Instance._InventoryCarryModeSlotUITransform.position = M_Input.IsLastInputFromGamepadForAim() ? GamepadMouse._Instance._CursorRect.position : Input.mousePosition;
        GameManager._Instance._InventoryCarryModeSlotUITransform.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        GameManager._Instance._InventoryCarryModeSlotUITransform.gameObject.SetActive(true);
        GameManager._Instance._InventoryCarryModeSlotUIImage.sprite = _IsEquipmentSlot ? transform.Find("DynamicImage").GetComponent<Image>().sprite : GetComponent<Image>().sprite;
        DisableGraphicsOnSlot();
        GameManager._Instance.SetDurabilityAndCountUI(GameManager._Instance._InventoryCarryModeSlotUITransform.GetComponent<Image>(), GameManager._Instance._InventoryCarryModeSlotUI._ItemRef);
    }
    public void CarryEnded(bool isValid = true)
    {
        if (GameManager._Instance._InventoryCarryModeSlotUI == null) { Debug.LogError("_InventoryCarryModeSlotUI is null!"); return; }

        _IsCarryMode = false;
        GameManager._Instance._InventoryCarryModeSlotUI = null;
        GameManager._Instance._InventoryCarryModeSlotUITransform.gameObject.SetActive(false);
        GameManager._Instance._InventoryCarryModeSlotUIImage.sprite = PrefabHolder._Instance._EmptyItemBackground;
        EnableGraphicsOnSlot();
        if (isValid)
        {
            Vector3 checkPos = GameManager._Instance._IsCarryUIFromGamepad ? GamepadMouse._Instance._CursorRect.position : Input.mousePosition;
            InventorySlotUI droppedToInventorySlotUI = GameManager._Instance.GetInventorySlotUIFromPosition(checkPos)?.GetComponent<InventorySlotUI>();
            if (droppedToInventorySlotUI == null && !GameManager._Instance.IsCursorOnUI(checkPos))
            {
                if (_ItemRef._IsEquipped)
                    _ItemRef.Unequip(true, false);
                else
                    _ItemRef.DropFrom(true);
            }
            else if (droppedToInventorySlotUI != null && ExchangeConditions(droppedToInventorySlotUI))
            {
                GameManager._Instance.UpdateInventoryUIInstant();//use slotUIs real itemRef
                if (droppedToInventorySlotUI._ItemRef == null)
                {
                    SendArranger(droppedToInventorySlotUI._Inventory, droppedToInventorySlotUI._IsEquipmentSlot, droppedToInventorySlotUI.gameObject.name.Equals("LeftHand"));
                }
                else
                {
                    TrySameTimeExchange(droppedToInventorySlotUI, droppedToInventorySlotUI.gameObject.name.Equals("LeftHand"));
                }
            }
        }
    }
    private bool ExchangeConditions(InventorySlotUI droppedToInventorySlotUI)
    {
        if (droppedToInventorySlotUI._IsBackCarryInventory && _ItemRef is CarryItem) return false;
        if (!_IsPlayerInventory && _IsEquipmentSlot && !_Inventory._IsPublic) return false;
        if (!droppedToInventorySlotUI._IsPlayerInventory && droppedToInventorySlotUI._IsEquipmentSlot && !droppedToInventorySlotUI._Inventory._IsPublic) return false;
        return true;
    }
    private void TrySameTimeExchange(InventorySlotUI droppedToInventorySlotUI, bool isToLeftHand)
    {
        bool isEquipping = droppedToInventorySlotUI._IsEquipmentSlot;
        Inventory targetInv = droppedToInventorySlotUI._Inventory;
        Item item = droppedToInventorySlotUI._ItemRef;

        bool selfIsEquipping = _IsEquipmentSlot;
        Inventory selfInv = _Inventory;
        Item selfItem = _ItemRef;

        if (item == selfItem) return;

        bool itemWasEquipped = item._IsEquipped;
        if (item._IsEquipped)
            item.Unequip(false, false);
        else
            item.DropFrom(false);

        bool itemRefWasEquipped = selfItem._IsEquipped;
        if (selfItem._IsEquipped)
            selfItem.Unequip(false, false);
        else
            selfItem.DropFrom(false);

        if ((!isEquipping && !targetInv.CanTakeThisItem(selfItem)) || (isEquipping && !targetInv.CanEquipThisItem(selfItem)) || (!selfIsEquipping && !selfInv.CanTakeThisItem(item)) || (selfIsEquipping && !selfInv.CanEquipThisItem(item)))//failed
        {
            if (itemWasEquipped)
                item.Equip(targetInv);
            else
                item.TakenTo(targetInv);

            if (itemRefWasEquipped)
                selfItem.Equip(selfInv);
            else
                selfItem.TakenTo(selfInv);
            return;
        }

        if (isEquipping)
            selfItem.Equip(targetInv, isToLeftHand);
        else
            selfItem.TakenTo(targetInv);

        if (selfIsEquipping)
            item.Equip(selfInv);
        else
            item.TakenTo(selfInv);
    }

    private bool SendArranger(Inventory targetInv, bool isEquipping, bool isToLeftHand)
    {
        if (!isEquipping && !targetInv.CanTakeThisItem(_ItemRef)) return false;
        if (isEquipping && !targetInv.CanEquipThisItem(_ItemRef)) return false;

        Item item = _ItemRef;
        if (item._IsEquipped)
            item.Unequip(false, false);

        if (isEquipping)
            item.Equip(targetInv, isToLeftHand);
        else
            item.TakenTo(targetInv);
        return true;
    }

    private void DisableGraphicsOnSlot()
    {
        Image image = _IsEquipmentSlot ? transform.Find("DynamicImage").GetComponent<Image>() : GetComponent<Image>();
        image.enabled = false;
        GameManager._Instance.SetDurabilityAndCountUI(GetComponent<Image>(), null);

    }
    private void EnableGraphicsOnSlot()
    {
        Image image = _IsEquipmentSlot ? transform.Find("DynamicImage").GetComponent<Image>() : GetComponent<Image>();
        image.enabled = true;
        GameManager._Instance.SetDurabilityAndCountUI(GetComponent<Image>(), _ItemRef);
    }

    public void RightClick()
    {
        GameManager._Instance._InventoryItemInteractPopup.SetActive(true);
        ArrangeInteractPopup();
    }
    private void QuickTake()
    {
        if (!_IsPlayerInventory)
        {
            SendArranger(WorldHandler._Instance._Player._Inventory, false, false);
        }
        else if (_IsPlayerInventory && GameManager._Instance._AnotherInventory != null)
        {
            SendArranger(GameManager._Instance._AnotherInventory, false, false);
        }
    }

    private void OnHover()
    {
        if (!GameManager._Instance._InventoryItemInfoPopup.activeSelf)
            GameManager._Instance._InventoryItemInfoPopup.SetActive(true);
        //GameManager._Instance.PlayButtonSound(0.05f, 1.25f);
        ArrangeInfoPopup();
    }
    private void DoubleClicked()
    {
        GameManager._Instance._InventoryItemInteractPopup.SetActive(false);
        if (_ItemRef._ItemDefinition._CanBeEquipped)
        {
            if (_ItemRef._IsEquipped)
                _ItemRef.Unequip(false, true);
            else
                _ItemRef.Equip(WorldHandler._Instance._Player._Inventory);
        }
        else if (_ItemRef._ItemDefinition is ICanBeConsumed canBeConsumed)
        {
            canBeConsumed.Consume(_ItemRef);
        }
    }
    private void ArrangeInteractPopup()
    {
        GameManager._Instance._InteractMenuSlotUI = this;
        Vector3 offset = Vector3.right * Screen.width / (transform.parent.parent.gameObject.name.StartsWith("Own") ? 23f : -23f);
        GameManager._Instance._InventoryItemInteractPopup.GetComponent<RectTransform>().position = GetComponent<RectTransform>().position + offset;
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("TakeSend").GetComponent<Image>().sprite = _IsPlayerInventory ? PrefabHolder._Instance._SendItemSprite : PrefabHolder._Instance._TakeItemSprite;
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("EquipUnequip").GetComponent<Image>().sprite = _ItemRef._IsEquipped ? PrefabHolder._Instance._UnequipItemSprite : PrefabHolder._Instance._EquipItemSprite;
        GameManager._Instance._SplitAmount = Mathf.RoundToInt(_ItemRef._Count * GameManager._Instance._InventoryItemInteractPopup.transform.Find("SplitSlider").GetComponent<Slider>().value);
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("SplitSlider").Find("SplitAmountText").GetComponent<TextMeshProUGUI>().text = GameManager._Instance._SplitAmount.ToString();

        GameManager._Instance._InventoryItemInteractPopup.transform.Find("TakeSend").GetComponent<Button>().interactable = GameManager._Instance._SplitAmount != 0 || _ItemRef is ICanBeEquipped;
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("TakeSendBackCarry").GetComponent<Button>().interactable = (GameManager._Instance._SplitAmount != 0 || _ItemRef is ICanBeEquipped) && GameManager._Instance.IsInventoryHaveBackCarryOrSelf(_ItemRef._AttachedInventoryCommon) && !(_ItemRef is CarryItem);

        GameManager._Instance._InventoryItemInteractPopup.transform.Find("EquipUnequip").GetComponent<Button>().interactable = _ItemRef._ItemDefinition._CanBeEquipped;
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("Consume").GetComponent<Button>().interactable = _ItemRef._ItemDefinition is ICanBeConsumed;

        if (_ItemRef is ICanBeEquipped)
        {
            GameManager._Instance._InventoryItemInteractPopup.transform.Find("EquipUnequip").gameObject.SetActive(true);
            GameManager._Instance._InventoryItemInteractPopup.transform.Find("Consume").gameObject.SetActive(false);
            GameManager._Instance._InventoryItemInteractPopup.transform.Find("SplitSlider").gameObject.SetActive(false);
        }
        else
        {
            GameManager._Instance._InventoryItemInteractPopup.transform.Find("EquipUnequip").gameObject.SetActive(false);
            GameManager._Instance._InventoryItemInteractPopup.transform.Find("Consume").gameObject.SetActive(true);
            GameManager._Instance._InventoryItemInteractPopup.transform.Find("SplitSlider").gameObject.SetActive(true);
        }
    }
    private void ArrangeInfoPopup()
    {
        GameManager._Instance._InventoryItemInfoPopup.transform.Find("InfoText").GetComponent<TextMeshProUGUI>().text = _ItemRef._ItemDefinition._ItemDescription;
    }
}