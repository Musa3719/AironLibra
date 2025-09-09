using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabHolder : MonoBehaviour
{
    public static PrefabHolder _Instance;

    [SerializeField] public GameObject _LoadPrefab;
    [SerializeField] public GameObject _SavePrefab;

    private void Awake()
    {
        _Instance = this;
    }
}
