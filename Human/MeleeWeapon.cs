using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MeleeWeapon : MonoBehaviour, Weapon
{
    public Rigidbody _Rigidbody { get { if (_rigidbody == null) _rigidbody = GetAttachedHuman().GetComponent<Rigidbody>(); return _rigidbody; } }
    private Rigidbody _rigidbody;
    public WeaponType _WeaponType => HandStateMethods.GetWeaponTypeFromString(name);
    public Transform _AttackCollider => transform.GetChild(2);
    public Transform _AttackWarning => transform.GetChild(1);
    public Transform _Tip => transform.GetChild(0);

    public WeaponItem _ConnectedItem { get { return _connectedItem; } set { _connectedItem = value; } }
    private WeaponItem _connectedItem;


    public Vector3 _AttackForward => _connectedItem._EquippedHumanoid.transform.forward;
    public AttackDirectionFrom _AttackDirectionFrom { get; set; }
    public Damage _Damage { get { return _damage; } set { _damage = value; } }
    private Damage _damage;
    public Vector3 _LastPos { get; set; }
    public float _HeavyAttackMultiplier { get; set; }

    private Vector3 _lastTipPosition;
    private Coroutine _attackCoroutine;
    public void Init(WeaponItem item)
    {
        _connectedItem = item;
    }

    private void LateUpdate()
    {
        _lastTipPosition = _Tip.position;
    }
    public Vector3 GetHitDirection()
    {
        return (_Tip.position - _lastTipPosition).normalized;
    }

    public void Attack(string animName, float heavyAttackMultiplier, AttackDirectionFrom attackDirectionFrom)
    {
        if (_ConnectedItem != null)//is not default melee
            _ConnectedItem._EquippedHumanoid._LastAttackWeapon = this;
        GameManager._Instance.CoroutineCall(ref _attackCoroutine, AttackCoroutine(animName, heavyAttackMultiplier, attackDirectionFrom), this);
    }
    private IEnumerator AttackCoroutine(string animName, float heavyAttackMultiplier, AttackDirectionFrom attackDirectionFrom)
    {
        _AttackDirectionFrom = attackDirectionFrom;
        _HeavyAttackMultiplier = heavyAttackMultiplier;
        _Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _AttackWarning.gameObject.SetActive(true);
        float waitForOpen = GameManager._Instance._AnimNameToAttackStartTime[animName];
        float waitForClose = GameManager._Instance._AnimNameToAttackEndTime[animName];
        yield return new WaitForSeconds(waitForOpen);
        _AttackCollider.gameObject.SetActive(true);
        float checkTime = waitForClose - waitForOpen;
        float timer = 0f;
        BoxCollider swordCollider = _AttackCollider.GetComponent<BoxCollider>();
        _LastPos = swordCollider.bounds.center;
        while (timer < checkTime)
        {
            Vector3 currentPos = swordCollider.bounds.center;
            Vector3 dir = currentPos - _LastPos;
            //if (Physics.SphereCast(_LastPos, 1.5f, dir.normalized, out RaycastHit hit, dir.magnitude, LayerMask.GetMask("HitBox")))
            if (Physics.BoxCast(_LastPos, swordCollider.bounds.extents * 2, dir.normalized, out RaycastHit hit, swordCollider.transform.rotation, dir.magnitude, LayerMask.GetMask("HitBox")))
            {
                _AttackCollider.GetComponent<AttackColliderHandle>().OnTrigger(hit.collider);
            }
            _LastPos = currentPos;
            timer += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(waitForClose - waitForOpen);

        HandStateMethods.AttackIsOver(_ConnectedItem._EquippedHumanoid, this);
    }
    public Transform GetAttachedHuman()
    {
        Transform parent = transform;
        while (parent.parent != null)
            parent = parent.parent;
        if (parent.gameObject.layer != LayerMask.NameToLayer("Human")) return null;
        return parent;
    }

}


