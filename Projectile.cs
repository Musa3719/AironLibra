using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TerrainUtils;
using UnityEngine.UIElements;

public class Projectile : MonoBehaviour, ICanDamage
{
    public Vector3 _AttackForward { get; set; }
    public Weapon _FromWeapon { get; set; }

    private Rigidbody _rb;
    private Collider _mainCollider;
    private Collider _hitCollider;
    private Collider _warningCollider;
    private bool _isFlying;
    private Vector3 _lastPos;
    private float _checkDistance;
    private float _flyTime;
    private float _lastDirectionCheckCounter;

    private Item _projectileItem;
    private Humanoid _attacker;
    private List<ICanGetHurt> _alreadyHit = new List<ICanGetHurt>();

    private Coroutine _flyCoroutine;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _mainCollider = GetComponent<Collider>();
        _hitCollider = transform.Find("AttackCollider").GetComponent<Collider>();
        _warningCollider = transform.Find("AttackWarning").GetComponent<Collider>();
    }
    private void Update()
    {
        if (_isFlying)
        {
            if (_flyTime < 1f || _lastDirectionCheckCounter > 0.2f)
            {
                if (Vector3.Angle(transform.forward.normalized, _rb.linearVelocity.normalized) > 1f)
                {
                    _lastDirectionCheckCounter = 0f;
                    transform.forward = _rb.linearVelocity.normalized;
                }
            }
            else
                _lastDirectionCheckCounter += Time.deltaTime;
        }
    }
    public void StartProjectileLogicFromSave(bool isInActiveFly, bool isStickToHitbox, Item projectileItem, RangedWeapon connectedWeapon, Humanoid attacker, Transform parent, Vector3 localPos, Vector3 localAngles, Vector3 linearSpeed)
    {
        _projectileItem = projectileItem;
        _FromWeapon = connectedWeapon;
        transform.parent = parent;
        transform.localPosition = localPos;
        transform.localEulerAngles = localAngles;
        _rb.linearVelocity = linearSpeed;
        _rb.useGravity = false;
        _attacker = attacker;
        //_rb.angularVelocity = transform.forward * linearSpeed.magnitude / 2f;

        if (isInActiveFly)
        {
            Init(connectedWeapon, linearSpeed.normalized, linearSpeed.magnitude, projectileItem);
        }
        else
        {
            _rb.isKinematic = true;
            _mainCollider.enabled = true;
            if (GetComponent<CarriableObject>() == null)
                gameObject.AddComponent<CarriableObject>();
        }
    }
    public Vector3 GetDirFromAimPos(Vector3 aimPos, RangedWeapon rangedWeapon) => (aimPos - rangedWeapon._ProjectileMesh.transform.position).normalized;
    public void Init(RangedWeapon weapon, Vector3 dir, float speed, Item projectileItemFromSave = null)
    {
        _projectileItem = projectileItemFromSave != null ? projectileItemFromSave : weapon._ReloadedItem;
        if (_projectileItem == null) { Debug.LogError("Error after seperating Projectile!"); return; }

        _mainCollider.enabled = false;
        weapon._ReloadedItem = null;
        _FromWeapon = weapon;
        _isFlying = true;
        _lastPos = transform.position;
        _warningCollider.gameObject.SetActive(true);

        _rb.useGravity = false;
        _rb.linearVelocity = dir * speed;
        transform.forward = dir;
        _checkDistance = (_hitCollider as BoxCollider).size.z * 1.5f;

        //_rb.angularVelocity = transform.forward * speed / 2f;

        if (projectileItemFromSave == null)
        {
            transform.position = (_FromWeapon as RangedWeapon)._ProjectileMesh.transform.position;
            _attacker = _FromWeapon._ConnectedItem._EquippedHumanoid;
        }
        _flyCoroutine = StartCoroutine(FlyCoroutine());
    }

    private IEnumerator FlyCoroutine()
    {
        _flyTime = 0f;
        while (true)
        {
            _flyTime += Time.deltaTime;
            if (_flyTime > 1.5f && !_rb.useGravity)
                _rb.useGravity = true;

            Vector3 move = transform.position - _lastPos;
            if (move.magnitude > 0f)
            {
                if (Physics.Raycast(_lastPos, move.normalized, out RaycastHit hit, _checkDistance, LayerMask.GetMask("HitBox")))
                {
                    OnTrigger(hit.collider, hit.normal);
                }
                if (Physics.Raycast(_lastPos, move.normalized, out hit, _checkDistance, GameManager._Instance._TerrainSolidWaterMask))
                {
                    HitAndStop(hit.collider, false, hit.normal);
                }
            }

            _lastPos = transform.position;
            yield return null;
        }
    }
    private void HitAndStop(Collider collider, bool isHitbox, Vector3 normal)
    {
        if (!_isFlying) return;

        _isFlying = false;
        StopCoroutine(_flyCoroutine);
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _warningCollider.gameObject.SetActive(false);

        if (ICanDamageMethods.GetHurtable(collider) != null)
            transform.SetParent(collider.transform, true);
        else
            transform.SetParent(GameManager._Instance._EnvironmentTransform, true);
        transform.forward = Vector3.Lerp(transform.forward, -normal, 0.3f);
        _mainCollider.enabled = true;

        if (GetComponent<CarriableObject>() == null)
            gameObject.AddComponent<CarriableObject>();
        GetComponent<CarriableObject>()._ItemRefForProjectiles = _projectileItem;
    }

    private void OnTrigger(Collider other, Vector3 normal)
    {
        if (other.isTrigger && !ICanDamageMethods.IsHitBox(other)) return;
        if (other.GetComponent<MeleeWeapon>() != null || other.name.StartsWith("AttackCollider")) return;

        ICanGetHurt hurtable = ICanDamageMethods.GetHurtable(other);
        if (hurtable == (_attacker as ICanGetHurt)) return;
        if (_alreadyHit.Contains(hurtable)) return;
        _alreadyHit.Add(hurtable);

        if (other.name.StartsWith("AttackWarning"))
        {
            //warning
            return;
        }

        if (hurtable == null) return;

        if (hurtable._IsBlocking)
        {
            if (ICanDamageMethods.GetBlockAngle(hurtable._Transform.forward, _AttackForward) < 100f)
                ICanDamageMethods.GiveDamage(this, other, hurtable);
            else
            {
                if (hurtable._IsHandsEmpty)
                {
                    hurtable.Blocked(ICanDamageMethods.InitDamage(this, other));
                    ICanDamageMethods.GiveDamage(this, other, hurtable, true);
                }
                else if (hurtable._LastTimeTriedParry + hurtable._ParryTime > Time.timeAsDouble)
                    HandStateMethods.AttackGotParried(_attacker, _AttackForward);
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

        HitAndStop(other, true, normal);
    }
}
