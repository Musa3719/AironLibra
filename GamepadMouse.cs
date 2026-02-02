using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Windows;

public class GamepadMouse : MonoBehaviour
{
    public static GamepadMouse _Instance;
    [SerializeField] private RectTransform _cursorRectTransform;
    public RectTransform _CursorRect => _cursorRectTransform;
    private RectTransform _canvasRect;
    private float _cursorSpeed = 800f;
    private Vector2 _moveInput;
    private float _activeSpeed;
    private Coroutine _pressedCoroutine;
    private RaycastHit _raycastToWorld;

    public Vector2 _PosForRangedAim { get; set; }

    public List<RectTransform> _RectTransformTargets { get; private set; }
    private List<InventorySlotUI> _inventorySlotUIs;
    private List<InventorySlotUI> _tempInventorySlotUIs;

    private void Awake()
    {
        _Instance = this;
        _inventorySlotUIs = new List<InventorySlotUI>();
        _tempInventorySlotUIs = new List<InventorySlotUI>();
        _canvasRect = GameObject.Find("UI").GetComponent<RectTransform>();
        _RectTransformTargets = new List<RectTransform>();
        var tempButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tempButtons.Length; i++)
            _RectTransformTargets.Add(tempButtons[i].GetComponent<RectTransform>());
        var tempSloutUIs = FindObjectsByType<InventorySlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tempSloutUIs.Length; i++)
            _RectTransformTargets.Add(tempSloutUIs[i].GetComponent<RectTransform>());
        var tempSliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tempSliders.Length; i++)
            _RectTransformTargets.Add(tempSliders[i].GetComponent<RectTransform>());
    }
    void Update()
    {
        if (Gamepad.current == null || !_CursorRect.gameObject.activeInHierarchy) return;

        _moveInput = Gamepad.current.leftStick.ReadValue();
        if (_moveInput.magnitude < 0.15f)
            _moveInput = Vector2.zero;

        _activeSpeed = Mathf.Lerp(_activeSpeed, _cursorSpeed * (_moveInput.magnitude < 0.02f ? 0f : (Gamepad.current.rightTrigger.isPressed ? 1.75f : 1f)), Time.unscaledDeltaTime * 8f);
        Vector2 newPosition = _cursorRectTransform.anchoredPosition + _moveInput * _activeSpeed * Time.unscaledDeltaTime;

        float clampedX = Mathf.Clamp(newPosition.x, -_canvasRect.sizeDelta.x / 2f, _canvasRect.sizeDelta.x / 2f);
        float clampedY = Mathf.Clamp(newPosition.y, -_canvasRect.sizeDelta.y / 2f, _canvasRect.sizeDelta.y / 2f);

        _cursorRectTransform.anchoredPosition = new Vector2(clampedX, clampedY);


        CheckForInventorySlotUI();

        if (Gamepad.current.buttonSouth.isPressed)
            GameManager._Instance._CarryUITimerForGamepad += Time.unscaledDeltaTime;
        else
            GameManager._Instance._CarryUITimerForGamepad = 0f;

        if (Gamepad.current.dpad.left.wasPressedThisFrame)
            MoveUIInDirection(Vector2.left);
        else if (Gamepad.current.dpad.right.wasPressedThisFrame)
            MoveUIInDirection(Vector2.right);
        else if (Gamepad.current.dpad.up.wasPressedThisFrame)
            MoveUIInDirection(Vector2.up);
        else if (Gamepad.current.dpad.down.wasPressedThisFrame)
            MoveUIInDirection(Vector2.down);


        bool isInventoryMode = GameManager._Instance._InventoryScreen != null && GameManager._Instance._InventoryScreen.activeInHierarchy;
        if (isInventoryMode && GameManager._Instance._CarryUITimerForGamepad > 0.06f)
        {
            GameObject topObject = GetOnTopObject();
            if (topObject != null)
            {
                InventorySlotUI inventorySlotUI = topObject.GetComponent<InventorySlotUI>();
                if (inventorySlotUI != null && inventorySlotUI._ItemRef != null && !inventorySlotUI._IsCarryMode && GameManager._Instance._InventoryCarryModeSlotUI == null)
                {
                    inventorySlotUI.CarryStarted();
                    GameManager._Instance._IsCarryUIFromGamepad = true;
                }
            }

        }

        if ((GameManager._Instance._GameHUD == null || !GameManager._Instance._GameHUD.activeInHierarchy) && Gamepad.current.buttonSouth.isPressed)
        {
            GameManager._Instance.CoroutineCall(ref _pressedCoroutine, PressedCoroutine(), this);
            GameObject topObject = GetOnTopObject();
            if (topObject != null)
            {
                Button button = topObject.GetComponent<Button>();
                InventorySlotUI inventorySlotUI = topObject.GetComponent<InventorySlotUI>();
                Slider slider = topObject.GetComponent<Slider>();
                if (slider != null)
                {
                    GameManager._Instance.UpdateSliderFromCursor(slider, _cursorRectTransform.position);
                }
                else if (button != null && button.interactable && Gamepad.current.buttonSouth.wasPressedThisFrame)
                {
                    button.onClick?.Invoke();
                }
                else if (inventorySlotUI != null && inventorySlotUI._ItemRef != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                {
                    inventorySlotUI.MouseClick();
                }

            }
        }

        if ((GameManager._Instance._GameHUD == null || !GameManager._Instance._GameHUD.activeInHierarchy) && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            GameManager._Instance.CoroutineCall(ref _pressedCoroutine, PressedCoroutine(), this);
            GameObject topObject = GetOnTopObject();
            if (topObject != null)
            {
                InventorySlotUI inventorySlotUI = topObject.GetComponent<InventorySlotUI>();
                if (inventorySlotUI != null && inventorySlotUI._ItemRef != null)
                {
                    inventorySlotUI.RightClick();
                }
            }
        }
    }
    private void CheckForInventorySlotUI()
    {
        _inventorySlotUIs.Clear();

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, _cursorRectTransform.position);
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        List<RaycastResult> results = new List<RaycastResult>();
        GameManager._Instance._GraphicRaycaster.Raycast(pointerData, results);
        if (results.Count > 0)
        {
            foreach (var item in results)
            {
                if (item.gameObject != null && item.gameObject.GetComponent<InventorySlotUI>() != null)
                    _inventorySlotUIs.Add(item.gameObject.GetComponent<InventorySlotUI>());
            }
        }

        foreach (var item in _tempInventorySlotUIs)
        {
            if (!_inventorySlotUIs.Contains(item))
                item.PointerExit();
        }
        foreach (var item in _inventorySlotUIs)
        {
            if (!_tempInventorySlotUIs.Contains(item))
                item.PointerEnter(true);
        }

        _tempInventorySlotUIs.Clear();
        foreach (var item in _inventorySlotUIs)
        {
            _tempInventorySlotUIs.Add(item);
        }
    }
    private GameObject GetOnTopObject()
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, _cursorRectTransform.position);
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos,
            radius = Vector2.one
        };
        List<RaycastResult> results = new List<RaycastResult>();
        GameManager._Instance._GraphicRaycaster.Raycast(pointerData, results);
        foreach (var item in results)
        {
            if (item.gameObject != null && item.gameObject.name == "Background" && item.gameObject.transform.parent.GetComponent<Slider>() != null)
                return item.gameObject.transform.parent.gameObject;
        }
        foreach (var item in results)
        {
            if (item.gameObject != null && item.gameObject.GetComponent<Button>() != null)
                return item.gameObject;
        }
        foreach (var item in results)
        {
            if (item.gameObject != null && item.gameObject.GetComponent<InventorySlotUI>() != null)
                return item.gameObject;
        }
        return null;
    }

    private IEnumerator PressedCoroutine()
    {
        float alpha = GameManager._Instance._InventoryCarryModeSlotUI == null ? 0.75f : 0.4f;
        _cursorRectTransform.GetComponentInChildren<Image>().color = new Color(0.5f, 0.5f, 0.5f, alpha);
        yield return new WaitForSecondsRealtime(0.15f);
        _cursorRectTransform.GetComponentInChildren<Image>().color = new Color(1f, 1f, 1f, alpha);

    }

    private void MoveUIInDirection(Vector2 direction)
    {
        RectTransform best = null;
        float bestScore = Mathf.Infinity;

        Vector2 fromPos = GetRectWorldCenter(_cursorRectTransform) + direction;
        Vector2 dir = direction.normalized;
        List<RectTransform> tempListForDeletion = new List<RectTransform>();
        foreach (var t in _RectTransformTargets)
        {
            if (t == null) { tempListForDeletion.Add(t); continue; }
            if (t == _cursorRectTransform || !t.gameObject.activeInHierarchy) continue;

            Vector2 targetPos = GetRectWorldCenter(t);
            Vector2 diff = targetPos - fromPos;

            float dot = Vector2.Dot(diff.normalized, dir);
            if (dot <= 0.01f) continue;

            float anglePenalty = 1f - dot;

            float verticalPenalty = Mathf.Abs(Vector2.Perpendicular(dir).x * diff.x + Vector2.Perpendicular(dir).y * diff.y);

            float projected = Vector2.Dot(diff, dir);

            if (projected <= 0) continue;

            float cost = anglePenalty * 10f + verticalPenalty * 0.5f + projected * 0.1f;

            if (cost < bestScore)
            {
                bestScore = cost;
                best = t;
            }
        }

        for (int i = 0; i < tempListForDeletion.Count; i++)
            _RectTransformTargets.Remove(tempListForDeletion[i]);

        if (best != null)
        {
            Vector2 worldCenter = GetRectWorldCenter(best);
            Vector2 anchored = WorldToAnchored(_cursorRectTransform.parent as RectTransform, worldCenter);
            _cursorRectTransform.anchoredPosition = anchored;
        }
    }

    private Vector2 GetRectWorldCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
    }
    private Vector2 WorldToAnchored(RectTransform parent, Vector3 worldPos)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            worldPos,
            null, // Canvas Screen Space - Overlay ise null
            out localPoint
        );
        return localPoint;
    }

    public Vector3 GetRangedAimPoint(bool isArrangingPos)
    {
        if (isArrangingPos)
        {
            Vector2 input = Gamepad.current.rightStick.ReadValue();
            if (input.magnitude < 0.15f)
                input = Vector2.zero;
            _PosForRangedAim += input * 1000f * Time.deltaTime;
        }

        float halfW = Screen.width * 0.48f;
        float halfH = Screen.height * 0.48f;
        _PosForRangedAim = new Vector2(Mathf.Clamp(_PosForRangedAim.x, -halfW, halfW), Mathf.Clamp(_PosForRangedAim.y, -halfH, halfH)); 

        Vector2 screenCenter = new Vector2(halfW, halfH);
        Vector2 screenPos = screenCenter + _PosForRangedAim;
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out _raycastToWorld, 1000f, GameManager._Instance._TerrainSolidWaterHumanMask, QueryTriggerInteraction.Collide) && _raycastToWorld.collider != null)
        {
            return _raycastToWorld.point;
        }
        _raycastToWorld = default;
        return Vector3.zero;
    }
    public Vector3 GetRangedAimNormal()
    {
        return _raycastToWorld.normal;
    }
}
