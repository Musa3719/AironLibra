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
    public float _AmountBlocked;
    public ArmorItem _TargetArmor;
    public Vector3 _Direction;
    public Vector3 _AttackerDirection;

    public void Init(DamageType type, DamagePart part, float amount, Vector3 dir, Vector3 attackerDirection, AttackDirectionFrom attackDirectionFrom)
    {
        _DamageType = type;
        _DamagePart = part;
        _Amount = amount;
        _Direction = dir;
        _AttackerDirection = attackerDirection;
        _AttackDirectionFrom = attackDirectionFrom;
    }

    public virtual Damage CalculateDamage(ICanGetHurt target, float heavyAttackMultiplier)
    {
        Damage calculatedDamage = new Damage();
        float newAmount = _Amount * heavyAttackMultiplier;
        calculatedDamage.Init(_DamageType, _DamagePart, newAmount, _Direction, _AttackerDirection, _AttackDirectionFrom);

        if (calculatedDamage._DamagePart == DamagePart.Head) calculatedDamage._TargetArmor = target._HeadGearItemRef as ArmorItem;
        if (calculatedDamage._DamagePart == DamagePart.Hands) calculatedDamage._TargetArmor = target._GlovesItemRef as ArmorItem;
        if (calculatedDamage._DamagePart == DamagePart.Chest) calculatedDamage._TargetArmor = target._ChestArmorItemRef;
        if (calculatedDamage._DamagePart == DamagePart.Legs) calculatedDamage._TargetArmor = target._LegsArmorItemRef;
        if (calculatedDamage._DamagePart == DamagePart.Feet) calculatedDamage._TargetArmor = target._BootsItemRef as ArmorItem;
        if (calculatedDamage._TargetArmor != null)
        {
            if (!calculatedDamage._TargetArmor._IsSteel)
            {
                if (GameManager._Instance.RandomPercentageChance(calculatedDamage._TargetArmor._Durability))
                    calculatedDamage._Amount *= 1 - calculatedDamage._TargetArmor._ProtectionValue;
            }
            else
            {
                if (GameManager._Instance.RandomPercentageChance(calculatedDamage._TargetArmor._Durability))
                    calculatedDamage._Amount = 0f;
            }

        }
        calculatedDamage._AmountBlocked = _Amount - calculatedDamage._Amount;

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
    public float _LastTimeTriedParry { get; set; }
    public float _ParryTime => 0.3f;
    public float _ParryOverTime => 0.6f;
    public void Blocked(Damage damage);
    public void TakeDamage(Damage damage);
    public void Die();
}
public interface ICanDamage
{
    public bool IsHitBox(Collider other);
    public ICanGetHurt GetHurtable(Collider other);
    public Vector3 _AttackForward { get; set; }
}
public interface Weapon
{
    public WeaponType _WeaponType { get; }
    public Rigidbody _Rigidbody { get; }
    public WeaponItem _ConnectedItem { get; set; }
    public void Init(WeaponItem item);
    public Damage _Damage { get; set; }
    public void Attack(string animName, float heavyAttackMultiplier, AttackDirectionFrom attackDirectionFrom);
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
