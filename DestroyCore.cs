using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyCore : MonoBehaviour
{
    
    private void Awake()
    {
        //arrange global volume profile for areas

        Transform[] childs = new Transform[transform.childCount];

        int i = 0;
        foreach (Transform child in transform)
        {
            childs[i] = child;
            i++;
        }
        foreach (Transform child in childs)
        {
            child.SetParent(null);
        }

        Destroy(gameObject);

    }
}