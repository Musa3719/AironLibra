using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractBoxUI : MonoBehaviour
{
    private RectTransform _rect;
    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }
    public bool IsHovered(bool isForGamepad)
    {
        if (!gameObject.activeSelf) return false;

        if (isForGamepad)
        {
            Vector2 gamepadPos = GamepadMouse._Instance._CursorRect.position;
            bool isGamepadOver = RectTransformUtility.RectangleContainsScreenPoint(
             _rect,
             gamepadPos
            );
            return isGamepadOver;
        }
        else
        {
            Vector2 mousePos = Input.mousePosition;
            bool isMouseOver = RectTransformUtility.RectangleContainsScreenPoint(
            _rect,
            mousePos
            );
            return isMouseOver;
        }
    }
}
