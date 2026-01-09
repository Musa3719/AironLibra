using System.Collections;
using UnityEngine;

public class RangedWeapon : MonoBehaviour, Weapon
{
    public Rigidbody _Rigidbody { get { if (_rigidbody == null) _rigidbody = GetAttachedHuman().GetComponent<Rigidbody>(); return _rigidbody; } }
    private Rigidbody _rigidbody;
    public WeaponType _WeaponType => HandStateMethods.GetWeaponTypeFromString(name);

    public Damage _Damage { get { return _damage; } set { _damage = value; } }
    private Damage _damage;
    public WeaponItem _ConnectedItem { get { return _connectedItem; } set { _connectedItem = value; } }
    private WeaponItem _connectedItem;

    private Coroutine _attackCoroutine;
    public void Init(WeaponItem item)
    {
        _connectedItem = item;
    }

    public void Attack(string animName, float heavyAttackMultiplier, AttackDirectionFrom attackDirectionFrom)
    {
        if (_ConnectedItem != null)//is not default melee
            _ConnectedItem._EquippedHumanoid._LastAttackWeapon = this;
        GameManager._Instance.CoroutineCall(ref _attackCoroutine, AttackCoroutine(animName), this);
    }
    private IEnumerator AttackCoroutine(string animName)
    {
        float timer = 0f;
        float waitTime = Random.Range(0.25f, 0.4f);
        while (timer < waitTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        SpawnProjectile(_ConnectedItem._EquippedHumanoid.transform.forward);

        HandStateMethods.AttackIsOver(_ConnectedItem._EquippedHumanoid, this);
    }
    private void SpawnProjectile(Vector3 direction)
    {
        GameObject projectilePrefab = null;
        float speed = 0f;
        switch (_WeaponType)
        {
            case WeaponType.None:
                Debug.LogError("None weapon cannot create projectiles!");
                return;
            case WeaponType.Bow:
                projectilePrefab = PrefabHolder._Instance._BoltProjectilePrefab;
                speed = 20f;
                break;
            case WeaponType.Crossbow:
                projectilePrefab = PrefabHolder._Instance._ArrowProjectilePrefab;
                speed = 25f;
                break;
            case WeaponType.Talisman:
                projectilePrefab = PrefabHolder._Instance._Magic_1_ProjectilePrefab;
                speed = 15f;
                break;
            default:
                Debug.LogError("wrong weapon type for creating projectiles!");
                return;
        }

        if (projectilePrefab == null) { Debug.LogError("Projectile Prefab null!"); return; }

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        projectile.transform.forward = direction;
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
        projectileRb.linearVelocity = speed * direction;
        projectileRb.angularVelocity = projectile.transform.forward * speed / 2f;
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
