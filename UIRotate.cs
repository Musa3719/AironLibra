using UnityEngine;

public class UIRotate : MonoBehaviour
{
    public float _Speed;
    private RectTransform _rectTransform;
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }
    private void Update()
    {
        _rectTransform.localEulerAngles = new Vector3(_rectTransform.localEulerAngles.x, _rectTransform.localEulerAngles.y, _rectTransform.localEulerAngles.z + Time.deltaTime * _Speed);
    }
}
