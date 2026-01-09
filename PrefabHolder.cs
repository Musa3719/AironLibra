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


    public Sprite _EmptyItemBackground;
    public Sprite _LoadingAdressableProcessSprite;
    public Sprite _TakeItemSprite;
    public Sprite _SendItemSprite;
    public Sprite _EquipItemSprite;
    public Sprite _UnequipItemSprite;

    #region Items
    public GameObject _BoltProjectilePrefab;
    public GameObject _ArrowProjectilePrefab;
    public GameObject _Magic_1_ProjectilePrefab;
    #endregion

    private void Awake()
    {
        _Instance = this;
    }
}
