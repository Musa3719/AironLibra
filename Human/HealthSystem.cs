using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HealthState
{
    Healthy,
    Sick,
    Wounded,
    Critical,
    Dead
}
public class HealthSystem
{
    public HealthState _HealthState { get; private set; }

    private float _bloodLevel;
    public float _BloodLevel { get => _bloodLevel; set { _bloodLevel = Mathf.Clamp(value, 0f, 100f); } }

    //body parts

    //sick mechanics
}
