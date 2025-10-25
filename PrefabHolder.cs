using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabHolder : MonoBehaviour
{
    public static PrefabHolder _Instance;

    public GameObject _LoadPrefab;
    public GameObject _SavePrefab;
    public GameObject _NpcParent;
    public Material _Pw_URP_Shared;
    public Material _Pw_URP_Solid;

    private void Awake()
    {
        _Instance = this;
    }
}
