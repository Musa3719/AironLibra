using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    [SerializeField]
    private Transform lookAtTransform;

    private void Update()
    {
        transform.LookAt(lookAtTransform);
    }
}
