using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneOpeningPanel : MonoBehaviour
{
    private Image _image;
    private void Awake()
    {
        _image = GetComponent<Image>();
        StartCoroutine(SceneOpeningCoroutine());
    }
    private IEnumerator SceneOpeningCoroutine()
    {
        double startTime = Time.timeAsDouble;
        while (Time.timeAsDouble < startTime + 1)
        {
            if (M_Input.GetButtonDown("Esc"))
                break;
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, _image.color.a - Time.deltaTime * 0.7f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
