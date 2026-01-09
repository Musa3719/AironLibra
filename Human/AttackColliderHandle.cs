using System.Collections.Generic;
using UnityEngine;

public class AttackColliderHandle : MonoBehaviour, ICanDamage
{
    public Vector3 _AttackForward { get; set; }
    private Weapon _FromWeapon;

    private List<ICanGetHurt> _alreadyHit = new List<ICanGetHurt>();
    private void Awake()
    {
        if (transform.parent != null && transform.parent.GetComponent<Weapon>() != null)
            _FromWeapon = transform.parent.GetComponent<Weapon>();

        if (transform.parent.gameObject.name.Contains("Punch") || transform.parent.gameObject.name.Contains("Claw") || transform.parent.gameObject.name.Contains("Kick"))
        {
            _FromWeapon._ConnectedItem = new WeaponItem(Default_MeleeWeapon._Instance);
            _FromWeapon._ConnectedItem._EquippedHumanoid = GetHurtable(GetComponent<Collider>()) as Humanoid;
            (_FromWeapon._ConnectedItem._ItemDefinition as Default_MeleeWeapon).SetDamageOverride(_FromWeapon._ConnectedItem._EquippedHumanoid._DefaultMeleeWeaponDamage);
        }
    }
    private void OnEnable()
    {
        _alreadyHit.Clear();
    }
    public void OnTrigger(Collider other)
    {
        if (other == null) return;
        if (!other.isTrigger) return;
        if (!IsHitBox(other)) return;

        ICanGetHurt hurtable = GetHurtable(other);
        if (hurtable == (_FromWeapon._ConnectedItem._EquippedHumanoid as ICanGetHurt)) return;
        if (_alreadyHit.Contains(hurtable)) return;
        _alreadyHit.Add(hurtable);

        if (other.name.StartsWith("AttackWarning"))
        {
            //warning
            return;
        }

        if (hurtable != null)
        {
            if (hurtable._IsBlocking)
            {
                if (GetBlockAngle(hurtable._Transform.forward, _AttackForward) < 80f)
                    GiveDamage(other, hurtable);
                else
                {
                    if (hurtable._IsHandsEmpty)
                    {
                        hurtable.Blocked(InitDamage(other));
                        GiveDamage(other, hurtable, true);
                    }
                    else if (hurtable._LastTimeTriedParry + hurtable._ParryTime > Time.time)
                        HandStateMethods.AttackGotParried(_FromWeapon._ConnectedItem._EquippedHumanoid, _AttackForward);
                    else if (hurtable._LastTimeTriedParry + hurtable._ParryOverTime > Time.time)
                        HandStateMethods.ParryFailed(hurtable as Humanoid, _AttackForward);
                    else
                        hurtable.Blocked(InitDamage(other));
                }
            }
            else
            {
                GiveDamage(other, hurtable);
            }
        }
    }
    private float GetBlockAngle(Vector3 hurtableTransformForward, Vector3 attackerTransformForward)
    {
        return Vector2.Angle(new Vector2(hurtableTransformForward.x, hurtableTransformForward.z), new Vector2(attackerTransformForward.x, attackerTransformForward.z));
    }
    private void GiveDamage(Collider other, ICanGetHurt hurtable, bool isDamageToHands = false)
    {
        InitDamage(other, isDamageToHands).Inflict(hurtable, (_FromWeapon as MeleeWeapon)._HeavyAttackMultiplier);
    }

    private Damage InitDamage(Collider other, bool isDamageToHands = false)
    {
        _FromWeapon._Damage = new Damage();
        DamageType damageType = (_FromWeapon._ConnectedItem._ItemDefinition as ICanBeEquippedForDefinition)._DamageType;
        DamagePart damagePart = isDamageToHands ? DamagePart.Hands : GetDamagePartFromBoneName(other.name);
        float damageAmount = (_FromWeapon._ConnectedItem._ItemDefinition as ICanBeEquippedForDefinition)._Value;
        Vector3 dir = (_FromWeapon as MeleeWeapon).GetHitDirection();
        Vector3 attackerDirection = _FromWeapon._ConnectedItem._EquippedHumanoid.transform.forward;
        _FromWeapon._Damage.Init(damageType, damagePart, damageAmount, dir, attackerDirection, (_FromWeapon as MeleeWeapon)._AttackDirectionFrom);
        return _FromWeapon._Damage;
    }
    private DamagePart GetDamagePartFromBoneName(string nameStr)
    {
        if (nameStr == "Head" || nameStr == "Neck")
        {
            return DamagePart.Head;
        }
        else if (nameStr == "LeftForeArm" || nameStr == "LeftHand" || nameStr == "RightForeArm" || nameStr == "RightHand")
        {
            return DamagePart.Hands;
        }
        else if (nameStr == "RightUpLeg" || nameStr == "RightLeg" || nameStr == "LeftUpLeg" || nameStr == "LeftLeg")
        {
            return DamagePart.Legs;
        }
        else if (nameStr == "RightFoot" || nameStr == "LeftFoot")
        {
            return DamagePart.Feet;
        }
        return DamagePart.Chest;
    }

    public bool IsHitBox(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("HitBox"))
            return true;
        return false;
    }
    public ICanGetHurt GetHurtable(Collider other)
    {
        if (!IsHitBox(other)) return null;

        Transform parent = other.transform;
        ICanGetHurt component;
        while (parent.parent != null)
        {
            parent = parent.parent;
            if (parent.gameObject.TryGetComponent(out component))
            {
                return component;
            }
        }
        return null;
    }
}
