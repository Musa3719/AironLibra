using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GamepadMouse : MonoBehaviour
{
    public static GamepadMouse _Instance;
    [SerializeField] private RectTransform _cursorRectTransform;
    public RectTransform _CursorRect => _cursorRectTransform;
    private RectTransform _canvasRect;
    private GraphicRaycaster _raycaster;
    private float _cursorSpeed = 800f;
    private Vector2 _moveInput;
    private float _activeSpeed;
    private Coroutine _pressedCoroutine;

    private void Awake()
    {
        _Instance = this;
        _raycaster = FindFirstObjectByType<GraphicRaycaster>();
        _canvasRect = GameObject.Find("UI").GetComponent<RectTransform>();
    }
    void Update()
    {
        if (Gamepad.current == null) return;

        _moveInput = Gamepad.current.leftStick.ReadValue();

        _activeSpeed = Mathf.Lerp(_activeSpeed, _cursorSpeed * (_moveInput.magnitude < 0.02f ? 0f : (Gamepad.current.rightTrigger.isPressed ? 1.75f : 1f)), Time.unscaledDeltaTime * 8f);
        Vector2 newPosition = _cursorRectTransform.anchoredPosition + _moveInput * _activeSpeed * Time.unscaledDeltaTime;

        float clampedX = Mathf.Clamp(newPosition.x, -_canvasRect.sizeDelta.x / 2f, _canvasRect.sizeDelta.x / 2f);
        float clampedY = Mathf.Clamp(newPosition.y, -_canvasRect.sizeDelta.y / 2f, _canvasRect.sizeDelta.y / 2f);

        _cursorRectTransform.anchoredPosition = new Vector2(clampedX, clampedY);

        if (Gamepad.current.leftStickButton.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            GameManager._Instance.CoroutineCall(ref _pressedCoroutine, PressedCoroutine(), this);

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, _cursorRectTransform.position);

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            List<RaycastResult> results = new List<RaycastResult>();
            _raycaster.Raycast(pointerData, results);

            if (results.Count > 0)
            {
                foreach (var item in results)
                {
                    GameObject topObject = item.gameObject;

                    Button button = topObject.GetComponent<Button>();
                    if (button != null && button.interactable)
                    {
                        button.onClick?.Invoke();
                        break;
                    }
                }
            }
        }
    }

    private IEnumerator PressedCoroutine()
    {
        _cursorRectTransform.GetComponent<Image>().color = Color.grey;
        yield return new WaitForSecondsRealtime(0.15f);
        _cursorRectTransform.GetComponent<Image>().color = Color.white;
    }
}
