using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, ICanDamage
{
    public Vector3 _AttackForward { get; set; }
    public Damage _Damage { get { return _damage; } set { _damage = value; } }
    private Damage _damage;

    private Item _connectedItem;
    private Rigidbody _rb;

    private Weapon _FromWeapon;
    private List<ICanGetHurt> _alreadyHit = new List<ICanGetHurt>();

    public void Init(WeaponItem item)
    {
        _connectedItem = item;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }
    public void InitiateProjectile(Weapon fromWeapon, Vector3 dir, float speed, Damage damage)
    {
        _FromWeapon = fromWeapon;
        _rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        _Damage = damage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger && !IsHitBox(other)) return;
        if (other.GetComponent<MeleeWeapon>() != null || other.name.StartsWith("AttackCollider")) return;

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
                if (GetBlockAngle(hurtable._Transform.forward, _AttackForward) < 100f)
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

        Destroy(gameObject);
    }
    private float GetBlockAngle(Vector3 hurtableTransformForward, Vector3 attackerTransformForward)
    {
        return Vector2.Angle(new Vector2(hurtableTransformForward.x, hurtableTransformForward.z), new Vector2(attackerTransformForward.x, attackerTransformForward.z));
    }
    private void GiveDamage(Collider other, ICanGetHurt hurtable, bool isDamageToHands = false)
    {
        InitDamage(other, isDamageToHands).Inflict(hurtable, 1f);
    }

    private Vector3 GetHitDirection()
    {
        return _rb.linearVelocity.normalized;
    }
    private Damage InitDamage(Collider other, bool isDamageToHands = false)
    {
        _FromWeapon._Damage = new Damage();
        DamageType damageType = (_FromWeapon._ConnectedItem._ItemDefinition as ICanBeEquippedForDefinition)._DamageType;
        DamagePart damagePart = isDamageToHands ? DamagePart.Hands : GetDamagePartFromBoneName(other.name);
        float damageAmount = (_FromWeapon._ConnectedItem._ItemDefinition as ICanBeEquippedForDefinition)._Value;
        Vector3 dir = GetHitDirection();
        _FromWeapon._Damage.Init(damageType, damagePart, damageAmount, dir, transform.forward, AttackDirectionFrom.Forward);
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
