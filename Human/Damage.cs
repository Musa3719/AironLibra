using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType { Cut, Crush, Pierce }
public enum DamagePart { Head, Hands, Chest, Legs, Feet }
public class Damage
{
    public DamageType _DamageType;
    public DamagePart _DamagePart;
    public AttackDirectionFrom _AttackDirectionFrom;
    public float _Amount;
    public float _ArmorPen;
    public float _AmountBlocked;
    public ArmorItem _TargetArmor;
    public Vector3 _Direction;
    public Vector3 _AttackerDirection;

    public void Init(DamageType type, DamagePart part, float damageAmount, float armorPenAmount, Vector3 dir, Vector3 attackerDirection, AttackDirectionFrom attackDirectionFrom)
    {
        _DamageType = type;
        _DamagePart = part;
        _Amount = damageAmount;
        _ArmorPen = armorPenAmount;
        _Direction = dir;
        _AttackerDirection = attackerDirection;
        _AttackDirectionFrom = attackDirectionFrom;
    }

    public virtual Damage CalculateDamage(ICanGetHurt target, float heavyAttackMultiplier)
    {
        Damage calculatedDamage = new Damage();
        float newAmount = _Amount * heavyAttackMultiplier;
        calculatedDamage.Init(_DamageType, _DamagePart, newAmount, _ArmorPen, _Direction, _AttackerDirection, _AttackDirectionFrom);

        if (calculatedDamage._DamagePart == DamagePart.Head) calculatedDamage._TargetArmor = target._HeadGearItemRef as ArmorItem;
        if (calculatedDamage._DamagePart == DamagePart.Hands) calculatedDamage._TargetArmor = target._GlovesItemRef as ArmorItem;
        if (calculatedDamage._DamagePart == DamagePart.Chest) calculatedDamage._TargetArmor = target._ChestArmorItemRef;
        if (calculatedDamage._DamagePart == DamagePart.Legs) calculatedDamage._TargetArmor = target._LegsArmorItemRef;
        if (calculatedDamage._DamagePart == DamagePart.Feet) calculatedDamage._TargetArmor = target._BootsItemRef as ArmorItem;
        if (calculatedDamage._TargetArmor != null)
        {
            if (GameManager._Instance.RandomPercentageChance(calculatedDamage._TargetArmor._Durability))
            {
                float protectValue = calculatedDamage._TargetArmor._ProtectionValue - calculatedDamage._ArmorPen;
                protectValue = protectValue < 0f ? 0f : protectValue;
                if (calculatedDamage._TargetArmor._IsSteel)
                {
                    if (calculatedDamage._DamageType == DamageType.Crush)
                        calculatedDamage._Amount = newAmount - protectValue / 2f;
                    else
                        calculatedDamage._Amount = newAmount - protectValue;

                }
                else
                {
                    if (calculatedDamage._DamageType == DamageType.Crush)
                        calculatedDamage._Amount = newAmount - protectValue / 2f;
                    else if (calculatedDamage._DamageType == DamageType.Pierce)
                        calculatedDamage._Amount = newAmount - protectValue / 4f;
                    else if (calculatedDamage._DamageType == DamageType.Cut)
                        calculatedDamage._Amount = newAmount - protectValue;

                }
            }
        }
        calculatedDamage._AmountBlocked = _Amount - calculatedDamage._Amount;

        calculatedDamage._Amount = Mathf.Clamp(calculatedDamage._Amount, 0f, float.MaxValue);
        return calculatedDamage;
    }

    public void Inflict(ICanGetHurt target, float heavyAttackMultiplier)
    {
        target.TakeDamage(CalculateDamage(target, heavyAttackMultiplier));
    }
}
public interface ICanGetHurt
{
    public Transform _Transform { get; }
    public Item _HeadGearItemRef { get; set; }
    public Item _GlovesItemRef { get; set; }
    public ArmorItem _ChestArmorItemRef { get; set; }
    public ArmorItem _LegsArmorItemRef { get; set; }
    public Item _BootsItemRef { get; set; }
    public bool _IsBlocking { get; set; }
    public bool _IsHandsEmpty { get; }
    public double _LastTimeTriedParry { get; set; }
    public float _ParryTime => 0.3f;
    public float _ParryOverTime => 0.6f;
    public void Blocked(Damage damage);
    public void TakeDamage(Damage damage);
    public void Die();
}
public interface ICanDamage
{
    public Vector3 _AttackForward { get; set; }
    public Weapon _FromWeapon { get; set; }
}
public static class ICanDamageMethods
{
    public static float GetBlockAngle(Vector3 hurtableTransformForward, Vector3 attackerTransformForward)
    {
        return Vector2.Angle(new Vector2(hurtableTransformForward.x, hurtableTransformForward.z), new Vector2(attackerTransformForward.x, attackerTransformForward.z));
    }
    public static void GiveDamage(ICanDamage iCanDamage, Collider other, ICanGetHurt hurtable, bool isDamageToHands = false)
    {
        InitDamage(iCanDamage, other, isDamageToHands).Inflict(hurtable, (iCanDamage._FromWeapon is MeleeWeapon ml) ? ml._HeavyAttackMultiplier : 1f);
    }

    public static Damage InitDamage(ICanDamage iCanDamage, Collider other, bool isDamageToHands = false)
    {
        Damage damage = new Damage();
        DamageType damageType = (iCanDamage._FromWeapon._ConnectedItem._ItemDefinition as IWeapon)._DamageType;
        DamagePart damagePart = isDamageToHands ? DamagePart.Hands : GetDamagePartFromBoneName(other.name);
        float damageAmount = (iCanDamage._FromWeapon._ConnectedItem._ItemDefinition as IWeapon)._BaseDamage;
        float armorPenAmount = (iCanDamage._FromWeapon._ConnectedItem._ItemDefinition as IWeapon)._BaseArmorPen;
        Vector3 dir = iCanDamage._FromWeapon is MeleeWeapon melee ? melee.GetHitDirection() : (iCanDamage as Projectile).transform.forward;
        Vector3 attackerDirection = iCanDamage._FromWeapon._ConnectedItem._EquippedHumanoid.transform.forward;
        AttackDirectionFrom attackDirectionFrom = (iCanDamage._FromWeapon is MeleeWeapon ml) ? ml._AttackDirectionFrom : AttackDirectionFrom.Forward;
        damage.Init(damageType, damagePart, damageAmount, armorPenAmount, dir, attackerDirection, attackDirectionFrom);
        return damage;
    }
    public static DamagePart GetDamagePartFromBoneName(string nameStr)
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

    public static bool IsHitBox(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("HitBox"))
            return true;
        return false;
    }
    public static ICanGetHurt GetHurtable(Collider other)
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
public interface Weapon
{
    public WeaponType _WeaponType { get; }
    public Rigidbody _Rigidbody { get; }
    public WeaponItem _ConnectedItem { get; set; }
    public void Init(WeaponItem item);
    public Transform GetAttachedHuman();

}

public enum AttackDirectionFrom
{
    Up, Right, Down, Left, Forward
}
public enum WeaponType
{
    None, BroadSword, LongSword, ColossalSword, Scimitar, Katana, Mace, Hammer, Halberd, Rapier, Dagger, Spear, Bow, Crossbow, Talisman
}
