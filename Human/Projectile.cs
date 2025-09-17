using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, ICanDamage
{
    public Damage Damage;
    public Damage CalculateDamage()
    {
        return Damage;
    }

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }
    public void InitiateProjectile(Vector3 dir, float speed, Damage damage)
    {
        _rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        Damage = damage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger && !IsHitBox(other)) return;

        ICanGetHurt hurtable = GetHurtable(other);
        if (hurtable != null)
            hurtable.TakeDamage(Damage);

        Destroy(gameObject);
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
        while (parent.parent != null)
        {
            parent = parent.parent;
            if (parent.GetComponent<ICanGetHurt>() != null)
            {
                return parent.GetComponent<ICanGetHurt>();
            }
        }
        return null;
    }
}
