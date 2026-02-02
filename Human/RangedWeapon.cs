using System.Collections;
using UnityEngine;

public class RangedWeapon : MonoBehaviour, Weapon
{
    public Rigidbody _Rigidbody { get { if (_rigidbody == null) _rigidbody = GetAttachedHuman().GetComponent<Rigidbody>(); return _rigidbody; } }
    private Rigidbody _rigidbody;
    public WeaponType _WeaponType => HandStateMethods.GetWeaponTypeFromString(name);
    public bool _IsCrossbow => (_ConnectedItem._ItemDefinition as IRangedWeapon)._IsCrossbow;
    public WeaponItem _ConnectedItem { get { return _connectedItem; } set { _connectedItem = value; } }
    private WeaponItem _connectedItem;

    public bool _IsLoaded => _ReloadedItem != null;
    public Item _ReloadedItem { get { return _ConnectedItem._LoadedItem; } set { _ConnectedItem._LoadedItem = value; } }
    private string _animName => (_ConnectedItem._ItemDefinition as IRangedWeapon)._AnimName;
    private Coroutine _attackCoroutine;
    private Coroutine _reloadCoroutine;

    public GameObject _ProjectileMesh { get; private set; }
    private Transform _bowArcTransfrom;
    private Vector3? _loosePos;
    private Vector3? _stretchPos;
    public void Init(WeaponItem item)
    {
        _connectedItem = item;
        _ProjectileMesh = transform.Find("Projectilemesh").gameObject;
        if (!_IsCrossbow)
        {
            _bowArcTransfrom = transform.Find("mesh/Bone001/Point004").transform;
            _loosePos = transform.Find("mesh/Bone001/LoosePoint").transform.localPosition;
            _stretchPos = transform.Find("mesh/Bone001/StretchPoint").transform.localPosition;
        }
    }
    public void StartReloading(Humanoid human, Item projectileItem)
    {
        GameManager._Instance.CoroutineCall(ref _reloadCoroutine, ReloadCoroutine(human, projectileItem), this);
    }
    private IEnumerator ReloadCoroutine(Humanoid human, Item projectileItem)
    {
        if (_IsCrossbow)
        {
            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                ShapeArrange(1f - timer);
                yield return null;
            }
            (human._HandState as RangedWeaponHandState)._IsReloading = false;
            ShapeArrange(0f);
        }

        _ReloadedItem = HandStateMethods.SeperateOneCountForProjectile(projectileItem);
    }
    private void ShapeArrange(float value)
    {
        if (_IsCrossbow)
        {
            transform.Find("mesh/bow").GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, value * 100f);
        }
        else
        {
            _bowArcTransfrom.localPosition = Vector3.Lerp(_stretchPos.Value, _loosePos.Value, value);
        }

        if (value > 0.95f && _ProjectileMesh.activeSelf)
            _ProjectileMesh.SetActive(false);
        else if (value < 0.05f && !_ProjectileMesh.activeSelf)
            _ProjectileMesh.SetActive(true);
    }
    public void AimStarted(Humanoid human)
    {
        (human._HandState as RangedWeaponHandState)._LastAimStartedTime = Time.timeAsDouble;
        if (!_IsCrossbow)
            ShapeArrange(0f);
    }
    public void AimEnded()
    {
        if (!_IsCrossbow)
            ShapeArrange(1f);
    }

    public void Attack()
    {
        if (_ConnectedItem != null)//is not default melee
            _ConnectedItem._EquippedHumanoid._LastAttackWeapon = this;
        GameManager._Instance.CoroutineCall(ref _attackCoroutine, AttackCoroutine(_animName), this);
    }
    private IEnumerator AttackCoroutine(string animName)
    {
        float timer = 0f;
        float waitTime = Random.Range(0.04f, 0.1f);
        while (timer < waitTime)
        {
            timer += Time.deltaTime;
            ShapeArrange(timer / waitTime);
            yield return null;
        }
        ShapeArrange(1f);

        Vector3 aimPos = _ConnectedItem._EquippedHumanoid._AimPosition;
        SpawnProjectile(aimPos);

        HandStateMethods.AttackIsOver(_ConnectedItem._EquippedHumanoid, this);
    }
    private void SpawnProjectile(Vector3 aimPos)
    {
        if (_ReloadedItem == null) return;

        GameObject projectilePrefab = null;
        float speed = 0f;
        switch (_WeaponType)
        {
            case WeaponType.None:
                Debug.LogError("None weapon cannot create projectiles!");
                return;
            case WeaponType.Bow:
                projectilePrefab = PrefabHolder._Instance._ArrowProjectilePrefab;
                speed = 20f;
                break;
            case WeaponType.Crossbow:
                projectilePrefab = PrefabHolder._Instance._BoltProjectilePrefab;
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

        Vector3 dir = projectile.GetComponent<Projectile>().GetDirFromAimPos(aimPos, this);
        projectile.GetComponent<Projectile>().Init(this, dir, speed);

    }
    public Transform GetAttachedHuman()
    {
        Transform parent = transform;
        while (parent.parent != null)
            parent = parent.parent;
        if (!GameManager._Instance._HumanMask.Contains(parent.gameObject.layer)) return null;
        return parent;
    }
}
