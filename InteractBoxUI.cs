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
    public bool IsHovered()
    {
        if (!gameObject.activeSelf) return false;

        Vector2 mousePos = Input.mousePosition;

        bool isOver = RectTransformUtility.RectangleContainsScreenPoint(
            _rect,
            mousePos
        );

        return isOver;
    }
}
