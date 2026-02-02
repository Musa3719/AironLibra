using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HealthState
{
    Healthy,
    Unhealthy,
    Critical,
    Dead
}

public class HealthSystem
{
    public Humanoid _Human { get; private set; }
    public bool _IsDead { get; private set; }
    public bool _IsUnconscious { get; private set; }
    public bool _IsUnhealthy { get; private set; }
    public float _MovementSpeedMultiplierHealthState { get; private set; }


    public float _Sickness { get => _sickness; set { _sickness = Mathf.Clamp(value, 0f, 100f); } }
    private float _sickness;

    public float _BleedingMaxValue => 4f;
    public float _BleedingOverTime { get => _bleedingOverTime; set { _bleedingOverTime = Mathf.Clamp(value, 0f, _BleedingMaxValue); } }
    private float _bleedingOverTime;
    public float _BloodLevel { get => _bloodLevel; set { _bloodLevel = Mathf.Clamp(value, 0f, 100f); } }
    private float _bloodLevel;
    private float _criticalTimer;
    private float _criticalThreshold;

    public float GetTotalWound() => _HeadWoundAmount + _HandsWoundAmount + _ChestWoundAmount + _LegsWoundAmount;
    public bool HasSeriusWound() => _HeadWoundAmount > 35f || _HandsWoundAmount > 35f || _ChestWoundAmount > 35f || _LegsWoundAmount > 35f || GetTotalWound() > 70f;
    public bool HasCriticalWound() => _HeadWoundAmount > 70f || _HandsWoundAmount > 70f || _ChestWoundAmount > 70f || _LegsWoundAmount > 70f || GetTotalWound() > 140f;
    public float _HeadWoundAmount { get => _headWoundAmount; set { _headWoundAmount = Mathf.Clamp(value, 0f, 100f); } }
    private float _headWoundAmount;
    public float _HandsWoundAmount { get => _handsWoundAmount; set { _handsWoundAmount = Mathf.Clamp(value, 0f, 100f); } }
    private float _handsWoundAmount;
    public float _ChestWoundAmount { get => _chestWoundAmount; set { _chestWoundAmount = Mathf.Clamp(value, 0f, 100f); } }
    private float _chestWoundAmount;
    public float _LegsWoundAmount { get => _legsWoundAmount; set { _legsWoundAmount = Mathf.Clamp(value, 0f, 100f); } }
    private float _legsWoundAmount;

    private double _lastHitTime;
    public Vector3 _LastHitForce { get { if (_lastHitTime - Time.timeAsDouble > 0.2f) return Vector3.zero; return _lastHitForce; } }
    private Vector3 _lastHitForce;
    public string _LastHitBoneName { get { if (_lastHitTime - Time.timeAsDouble > 0.2f) return ""; return _lastHitBoneName; } }
    private string _lastHitBoneName;
    public Vector3 _LastHitDir { get { if (_lastHitTime - Time.timeAsDouble > 0.2f) return Vector3.zero; return _lastHitDir; } }
    private Vector3 _lastHitDir;

    private float _updateCounter;

    public void Init(Humanoid human)
    {
        _Human = human;
        _MovementSpeedMultiplierHealthState = 1f;
        _BloodLevel = 100f;
        _criticalThreshold = Random.Range(90f, 120f);
    }
    public void Update()
    {
        if (_updateCounter < 1f)
        {
            _updateCounter += Time.deltaTime;
            return;
        }

        _updateCounter -= 1f;
        _BloodLevel -= _BleedingOverTime;
        CheckForHealthStateChange();
    }
    public void CheckForHealthStateChange()
    {
        if (_IsDead) return;

        if (HasCriticalWound() || _Sickness > 85f)
            _criticalTimer += Time.deltaTime;

        if (_BloodLevel == 0f || _criticalTimer > _criticalThreshold)
        {
            _IsUnconscious = true;
            _IsUnhealthy = true;
            _IsDead = true;
            _Human.Die();
            return;
        }

        if (HasCriticalWound() || _Sickness > 85f || _BloodLevel < 15f)
        {
            _IsUnconscious = true;
            _IsUnhealthy = true;
        }
        else if (HasSeriusWound() || _Sickness > 50f || _BloodLevel < 15f)
        {
            _IsUnconscious = false;
            _IsUnhealthy = true;
        }
        else
        {
            _IsUnconscious = false;
            _IsUnhealthy = false;
        }

        if (_IsUnhealthy)
            _MovementSpeedMultiplierHealthState = 0.7f;
        else
            _MovementSpeedMultiplierHealthState = 1f;
    }
    public void TakeDamage(Damage damage, float bleedingDamage)
    {
        _BleedingOverTime += bleedingDamage;
        _BloodLevel -= bleedingDamage * 20f;
        _lastHitTime = Time.timeAsDouble;
        _lastHitDir = damage._Direction;
        Transform bone;

        switch (damage._DamagePart)
        {
            case DamagePart.Head:
                _lastHitBoneName = "Head";
                _HeadWoundAmount += damage._Amount;
                Debug.Log("head " + _HeadWoundAmount);
                break;
            case DamagePart.Hands:
                _lastHitBoneName = "Spine1";
                _HandsWoundAmount += damage._Amount;
                Debug.Log("hands " + _HandsWoundAmount);
                break;
            case DamagePart.Chest:
                _lastHitBoneName = "Spine1";
                _ChestWoundAmount += damage._Amount;
                Debug.Log("chest " + _ChestWoundAmount);
                break;
            case DamagePart.Legs:
                _lastHitBoneName = "Hips";
                _LegsWoundAmount += damage._Amount;
                Debug.Log("legs " + _LegsWoundAmount);
                break;
            case DamagePart.Feet:
                _lastHitBoneName = "Hips";
                _legsWoundAmount += damage._Amount;
                Debug.Log("legs " + _LegsWoundAmount);
                break;
            default:
                Debug.LogError("damage part not found: " + damage._DamagePart);
                break;
        }

        bone = GetBoneFromName(_lastHitBoneName);
        _lastHitForce = damage._Amount * 3f * _LastHitDir;

        CheckForHealthStateChange();
    }
    private Transform GetBoneFromName(string str)
    {
        if (str == "Hips")
        {
            return _Human._LocomotionSystem.transform.Find("char/Root/Global/Position/Hips");
        }
        else if (str == "Spine1")
        {
            return _Human._LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LowerBack/Spine/Spine1");
        }
        else if (str == "Head")
        {
            return _Human._LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head");
        }
        else
        {
            Debug.LogError("Bone Name not Found: " + str);
            return null;
        }
    }

    public HealthState GetHealthState()//for info
    {
        if (_IsDead)
            return HealthState.Dead;
        else if (_IsUnconscious)
            return HealthState.Critical;
        else if (_IsUnhealthy)
            return HealthState.Unhealthy;

        return HealthState.Healthy;
    }
}
