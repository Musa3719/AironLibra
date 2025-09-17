using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlaneSoundType : byte
{
    Sand,
    Grass,
    Dirt,
    WoodenDirt,
    Stone,
    Wood,
    Fabric,
    Water,
    Ice,
    Snow,
    Swimming,
}
public class PlaneSound : MonoBehaviour
{
    [SerializeField]
    private PlaneSoundType _planeSoundType;

    public PlaneSoundType PlaneSoundType => _planeSoundType;
}
