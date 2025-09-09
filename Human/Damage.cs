using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damage
{

}
public interface ICanGetHurt
{
    public void TakeDamage(Damage damage);
    public void Die();
}


public interface ICanDamage
{
    public Damage GetDamage();
}
public abstract class Weapon : MonoBehaviour, ICanDamage
{
    public virtual Damage GetDamage()
    {
        return new Damage();
    }
}
