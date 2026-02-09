using System.Collections.Generic;
using UnityEngine;

public class AttackColliderHandle : MonoBehaviour, ICanDamage
{
    public Vector3 _AttackForward { get; set; }
    public Weapon _FromWeapon { get; set; }

    private List<ICanGetHurt> _alreadyHit = new List<ICanGetHurt>();
    private void Awake()
    {
        if (transform.parent != null && transform.parent.GetComponent<Weapon>() != null)
            _FromWeapon = transform.parent.GetComponent<Weapon>();

        if (transform.parent.gameObject.name.Contains("Punch") || transform.parent.gameObject.name.Contains("Claw") || transform.parent.gameObject.name.Contains("Kick"))
        {
            _FromWeapon._ConnectedItem = new WeaponItem(Default_MeleeWeapon._Instance);
            _FromWeapon._ConnectedItem._EquippedHumanoid = ICanDamageMethods.GetHurtable(GetComponent<Collider>()) as Humanoid;
            (_FromWeapon._ConnectedItem._ItemDefinition as Default_MeleeWeapon).SetDamageOverride(_FromWeapon._ConnectedItem._EquippedHumanoid._DefaultMeleeWeaponDamage);
        }
    }
    private void OnEnable()
    {
        _alreadyHit.Clear();
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!other.isTrigger) return;
        if (!ICanDamageMethods.IsHitBox(other)) return;

        ICanGetHurt hurtable = ICanDamageMethods.GetHurtable(other);
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
                if (ICanDamageMethods.GetBlockAngle(hurtable._Transform.forward, _AttackForward) < 80f)
                    ICanDamageMethods.GiveDamage(this,other, hurtable);
                else
                {
                    if (hurtable._IsHandsEmpty)
                    {
                        hurtable.Blocked(ICanDamageMethods.InitDamage(this,other));
                        ICanDamageMethods.GiveDamage(this, other, hurtable, true);
                    }
                    else if (hurtable._LastTimeTriedParry + hurtable._ParryTime > Time.timeAsDouble)
                        HandStateMethods.AttackGotParried(_FromWeapon._ConnectedItem._EquippedHumanoid, _AttackForward);
                    else if (hurtable._LastTimeTriedParry + hurtable._ParryOverTime > Time.timeAsDouble)
                        HandStateMethods.ParryFailed(hurtable as Humanoid, _AttackForward);
                    else
                        hurtable.Blocked(ICanDamageMethods.InitDamage(this, other));
                }
            }
            else
            {
                ICanDamageMethods.GiveDamage(this, other, hurtable);
            }
        }
    }
}
