using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Item _ItemRef { get; set; }
    public bool _IsPlayerInventory => transform.parent.parent.name.StartsWith("Own");
    private float _lastTimeClicked;

    private bool _isHovered;
    private void OnDisable()
    {
        MouseExit();
        GameManager._Instance._LastClickedSlotUI = null;
        GameManager._Instance._InventoryItemInteractPopup.SetActive(false);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_ItemRef == null)
        {
            MouseExit();
        }
        else
        {
            _isHovered = true;
            OnHover();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MouseExit();
    }
    public void MouseExit()
    {
        _isHovered = false;
        GameManager._Instance._InventoryItemInfoPopup.SetActive(false);
    }

    private void Update()
    {
        if (!_isHovered) return;
        if (_isHovered && _ItemRef == null) { _isHovered = false; return; }

        if (M_Input.GetButtonDown("Fire1"))
        {
            GameManager._Instance.PlayButtonSound(0.115f, 1.35f);
            if (M_Input.GetButton("Run"))
            {
                QuickTake();
            }
            else if (_lastTimeClicked + 0.32f > Time.unscaledTime)
            {
                DoubleClicked();
            }
            else
            {
                _lastTimeClicked = Time.unscaledTime;
                LeftClicked();
            }

        }
        if (M_Input.GetButtonDown("Fire2"))
        {
            //RightClicked();
        }
    }
    private void LeftClicked()
    {
        GameManager._Instance._InventoryItemInteractPopup.SetActive(true);
        ArrangeInteractPopup();
    }
    private void QuickTake()
    {
        if (!_IsPlayerInventory)
        {
            if (_ItemRef._IsEquipped)
                _ItemRef.Unequip(false, false);
            _ItemRef.Take(WorldHandler._Instance._Player._Inventory);
        }
        else if (_IsPlayerInventory && GameManager._Instance._AnotherInventory != null)
        {
            if (_ItemRef._IsEquipped)
                _ItemRef.Unequip(false, false);
            _ItemRef.Take(GameManager._Instance._AnotherInventory);
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
                _ItemRef.Equip(_ItemRef._AttachedInventory);
        }
        else if (_ItemRef._ItemDefinition is ICanBeConsumed canBeConsumed)
        {
            canBeConsumed.Consume(_ItemRef);
        }
    }
    private void ArrangeInteractPopup()
    {
        GameManager._Instance._LastClickedSlotUI = this;
        Vector3 offset = Vector3.right * Screen.width / (transform.parent.parent.gameObject.name.StartsWith("Own") ? 23f : -23f);
        GameManager._Instance._InventoryItemInteractPopup.GetComponent<RectTransform>().position = GetComponent<RectTransform>().position + offset;
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("TakeSend").GetComponent<Image>().sprite = _IsPlayerInventory ? PrefabHolder._Instance._SendItemSprite : PrefabHolder._Instance._TakeItemSprite;
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("EquipUnequip").GetComponent<Image>().sprite = _ItemRef._IsEquipped ? PrefabHolder._Instance._UnequipItemSprite : PrefabHolder._Instance._EquipItemSprite;
        GameManager._Instance._SplitAmount = Mathf.RoundToInt(_ItemRef._Count * GameManager._Instance._InventoryItemInteractPopup.transform.Find("SplitSlider").GetComponent<Slider>().value);
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("Split").Find("SplitAmountText").GetComponent<TextMeshProUGUI>().text = GameManager._Instance._SplitAmount.ToString();

        GameManager._Instance._InventoryItemInteractPopup.transform.Find("EquipUnequip").GetComponent<Button>().interactable = _ItemRef._ItemDefinition._CanBeEquipped;
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("Consume").GetComponent<Button>().interactable = _ItemRef._ItemDefinition is ICanBeConsumed;
        GameManager._Instance._InventoryItemInteractPopup.transform.Find("Split").GetComponent<Button>().interactable = GameManager._Instance._SplitAmount > 0;
    }
    private void ArrangeInfoPopup()
    {
        GameManager._Instance._InventoryItemInfoPopup.transform.Find("InfoText").GetComponent<TextMeshProUGUI>().text = _ItemRef._ItemDefinition._ItemDescription;
    }
}