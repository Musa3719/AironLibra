using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MeleeWeapon : MonoBehaviour, ICanDamage
{
    public Damage _Damage { get { return _damage; } set { _damage = value; } }
    private Damage _damage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger && !IsHitBox(other)) return;

        ICanGetHurt hurtable = GetHurtable(other);
        if (hurtable != null)
            _Damage.Inflict(hurtable);
    }

    private bool IsHitBox(Collider other)
    {
        if (other.CompareTag("HitBox"))
            return true;
        return false;
    }
    private ICanGetHurt GetHurtable(Collider other)
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


