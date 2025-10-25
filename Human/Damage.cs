using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType { Cut, Crush, Pierce }
public enum DamagePart { Head, UpperBody, LowerBody }
public class Damage
{
    public DamageType _DamageType;
    public DamagePart _DamagePart;
    public float _Amount;
    public float _AmountBlocked;
    public ArmorItem _TargetArmor;

    public void Init(DamageType type, DamagePart part, float amount)
    {
        _DamageType = type;
        _DamagePart = part;
        _Amount = amount;
    }

    public virtual Damage CalculateDamage(ICanGetHurt target)
    {
        Damage calculatedDamage = new Damage();
        calculatedDamage.Init(_DamageType, _DamagePart, _Amount);

        if (calculatedDamage._DamagePart == DamagePart.Head) calculatedDamage._TargetArmor = target._HeadArmor;
        if (calculatedDamage._DamagePart == DamagePart.UpperBody) calculatedDamage._TargetArmor = target._UpperBodyArmor;
        if (calculatedDamage._DamagePart == DamagePart.LowerBody) calculatedDamage._TargetArmor = target._LowerBodyArmor;
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

    public void Inflict(ICanGetHurt target)
    {
        target.TakeDamage(CalculateDamage(target));
    }
}
public interface ICanGetHurt
{
    public ArmorItem _HeadArmor { get; set; }
    public ArmorItem _UpperBodyArmor { get; set; }
    public ArmorItem _LowerBodyArmor { get; set; }
    public void TakeDamage(Damage damage);
    public void Die();
}
public interface ICanDamage
{
    public Damage _Damage { get; set; }
}
