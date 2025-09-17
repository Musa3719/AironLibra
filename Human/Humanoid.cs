using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using FischlWorks;
using UMA.CharacterSystem;
using FIMSpace;

public abstract class Humanoid : MonoBehaviour, ICanGetHurt
{
    public Animator _Animator { get; protected set; }
    public Rigidbody _Rigidbody { get; protected set; }
    public LocomotionSystem _LocomotionSystem { get; protected set; }
    public csHomebrewIK _FootIKComponent { get; protected set; }
    public LeaningAnimator _LeaninganimatorComponent { get; protected set; }

    //Systems
    public string _Name { get; protected set; }
    public bool _IsMale { get; protected set; }
    public Class _Class { get; protected set; }
    public MovementState _MovementState { get; protected set; }
    public HandState _HandState { get; protected set; }
    public HealthSystem _HealthSystem { get; protected set; }
    public Inventory _Inventory { get; protected set; }
    public Family _Family { get; protected set; }
    public Group _AttachedGroup { get; protected set; }//not instance, a referance

    //public Characteristic _Characteristic { get; private set; }


    public virtual Vector2 _DirectionInput { get; }
    public bool _RunInput { get; set; }
    public bool _SprintInput { get; set; }
    public bool _IsInCombatMode { get; set; }
    public bool _CrouchInput { get; set; }
    public bool _JumpInput { get; set; }
    public bool _InteractInput { get; set; }
    public bool _AttackInput { get; set; }

    public bool _IsStrafing { get; set; }
    public bool _IsJumping { get; set; }
    public bool _IsGrounded { get; set; }
    public bool _IsSprinting { get; set; }
    public bool _StopMove { get; set; }


    [HideInInspector] public float _JumpTimer = 0.15f;
    [HideInInspector] public float _JumpCounter;
    public float _AimSpeed { get { if (_MovementState is Crouch) return 3f; else if (_MovementState is Prone) return 1.25f; else return 5f; } }
    public float _LastTimeRotated { get; set; }

    public RaycastHit _RayFoLook;
    public Coroutine _RotateAroundCoroutine;


    private float _walkSoundCounter;
    private bool _umaWaitingForCompletion;

    protected virtual void Awake()
    {
        _Animator = GetComponentInChildren<Animator>();
        _Rigidbody = GetComponent<Rigidbody>();
        _LocomotionSystem = GetComponentInChildren<LocomotionSystem>();
        _FootIKComponent = GetComponentInChildren<csHomebrewIK>();
        _LeaninganimatorComponent = GetComponentInChildren<LeaningAnimator>();
        InitOrLoadUmaCharacter();
    }
    protected virtual void Start()
    {
        _LocomotionSystem.Init();
        ArrangeStartingStates();
        ArrangeStatsFromLevels();
    }
    protected virtual void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        ControlUmaDataRuntimeLoadUnload();
        ArrangePlaneSound();
        ArrangeStamina();
        _MovementState.DoState();
        _HandState.DoState();

        if (_umaWaitingForCompletion && _Animator.avatar != null)
            UmaUpdateCompleted();
    }
    private void FixedUpdate()
    {
        _MovementState.FixedUpdate();
    }
    private void OnAnimatorMove()
    {
        ControlAnimatorRootMotion();
    }
    private void ControlUmaDataRuntimeLoadUnload()
    {
        if (this is Player) return;

        bool isBuildEnabled = transform.Find("char").GetComponent<DynamicCharacterAvatar>().BuildCharacterEnabled;
        Vector3 distance = (Player._Instance.transform.position - transform.position);
        distance.y = 0f;

        if (!isBuildEnabled && distance.magnitude < 40f)
            EnableHumanData();
        else if (isBuildEnabled && distance.magnitude > 60f)
            DisableHumanData();
    }
    public void UmaCreated()
    {
        //for init
        UmaUpdated();
    }
    public void UmaUpdated()
    {
        _umaWaitingForCompletion = true;
    }
    public void UmaUpdateCompleted()
    {
        _umaWaitingForCompletion = false;
        GetComponentInChildren<csHomebrewIK>().StartForUma();
    }
    public void EnableHumanData()
    {
        if (!(_MovementState is Prone))
            _FootIKComponent.enabled = true;
        _Animator.enabled = true;
        _LeaninganimatorComponent.enabled = true;
        transform.Find("char").GetComponent<DynamicCharacterAvatar>().BuildCharacterEnabled = true;
    }
    public void DisableHumanData()
    {
        _FootIKComponent.enabled = false;
        _Animator.enabled = false;
        _LeaninganimatorComponent.enabled = false;
        SkinnedMeshRenderer smr = transform.Find("char").Find("UMARenderer")?.GetComponent<SkinnedMeshRenderer>();
        if (smr != null && smr.sharedMesh != null)
        {
            Destroy(smr.sharedMesh);
            smr.sharedMesh = null;
        }

        transform.Find("char").GetComponent<DynamicCharacterAvatar>().BuildCharacterEnabled = false;
        UMA.UMAData data = transform.Find("char").GetComponent<DynamicCharacterAvatar>().umaData;
        data.CleanAvatar();
        data.CleanMesh(false);
        data.CleanTextures();
    }
    private void InitOrLoadUmaCharacter()
    {

    }
    public void ControlAnimatorRootMotion()
    {
        if (!this.enabled) return;

        if (_LocomotionSystem.inputSmooth == Vector3.zero)
        {
            transform.position = _Animator.rootPosition;
            transform.rotation = _Animator.rootRotation;
        }
    }

    private void ArrangeStartingStates()
    {
        //arrange 2 states, health system and inventory from saves or create it

        //temp
        EnterState(new Locomotion(this));
        EnterState(new Empty(this));
    }
    public void ChangeAnimation(string name, float fadeTime = 0.2f)
    {
        _Animator.CrossFadeInFixedTime(name, fadeTime);
    }
    public void EnterState(MovementState newState)
    {
        if (_MovementState != null)
            _MovementState.ExitState(newState);
        MovementState oldState = _MovementState;
        _MovementState = newState;
        _MovementState.EnterState(oldState);
    }
    public void EnterState(HandState newState)
    {
        if (_HandState != null)
            _HandState.ExitState(newState);
        HandState oldState = _HandState;
        _HandState = newState;
        _HandState.EnterState(oldState);
    }

    private void ArrangePlaneSound()
    {
        Vector3 dist = (Player._Instance.transform.position - transform.position);
        dist.y = 0f;
        if (dist.magnitude > 50f) return;

        Physics.Raycast(transform.position + Vector3.up, -Vector3.up, out RaycastHit hit, 2f, GameManager._Instance._TerrainAndSolidMask);
        float speed = _Rigidbody.linearVelocity.magnitude;
        if (hit.collider != null)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                if (_walkSoundCounter <= 0f)
                {
                    SoundManager._Instance.PlayPlaneSound(PlaneSoundType.Swimming, transform.position, speed);
                    _walkSoundCounter += 1f;
                }
            }
            else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain"))
            {
                if (_walkSoundCounter <= 0f)
                {
                    SoundManager._Instance.PlayPlaneSound(GetPlaneSoundTypeFromTerrain(hit), transform.position, speed);
                    _walkSoundCounter += 1f;
                }
            }
            else if (hit.collider.GetComponent<PlaneSound>() != null)
            {
                if (_walkSoundCounter <= 0f)
                {
                    SoundManager._Instance.PlayPlaneSound(hit.collider.GetComponent<PlaneSound>().PlaneSoundType, transform.position, speed);
                    _walkSoundCounter += 1f;
                }
            }
        }
        _walkSoundCounter -= Time.deltaTime * 1.65f * speed;

    }
    private PlaneSoundType GetPlaneSoundTypeFromTerrain(RaycastHit hit)
    {
        if (hit.collider == null) return PlaneSoundType.Dirt;
        Terrain terrain = hit.collider.GetComponent<Terrain>();
        if (terrain == null) return PlaneSoundType.Dirt;

        Vector3 localPos = hit.transform.InverseTransformPoint(hit.point);
        int x = Mathf.FloorToInt(localPos.x / terrain.terrainData.size.x * terrain.terrainData.alphamapWidth);
        int y = Mathf.FloorToInt(localPos.z / terrain.terrainData.size.z * terrain.terrainData.alphamapHeight);
        
        //Debug.Log(hit.collider.gameObject.GetComponent<TerrainBehaviour>()._SoundTypeMap[x, y]);
        return hit.collider.gameObject.GetComponent<TerrainBehaviour>()._SoundTypeMap[x, y];
    }

    private void ArrangeStamina()
    {
        if (_IsSprinting)
        {
            if (_IsStrafing)
            {
                if (_LocomotionSystem.AimingMovementSetting.walkByDefault)
                    _LocomotionSystem.Stamina -= Time.deltaTime * 1.5f;
                else
                    _LocomotionSystem.Stamina -= Time.deltaTime * 10f;
            }
            else
            {
                if (_LocomotionSystem.FreeMovementSetting.walkByDefault)
                    _LocomotionSystem.Stamina -= Time.deltaTime * 1.5f;
                else
                    _LocomotionSystem.Stamina -= Time.deltaTime * 10f;
            }
        }
        else
        {
            _LocomotionSystem.Stamina += Time.deltaTime * 5f;
        }
    }
    public void LookAt(Vector3 pos, float lerpSpeed = 10f)
    {
        if (pos == transform.position) return;

        transform.forward = Vector3.Lerp(transform.forward, (pos - transform.position).normalized, Time.deltaTime * lerpSpeed);
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
    }
    public void LevelChanged()
    {
        ArrangeStatsFromLevels();
    }
    public void ArrangeStatsFromLevels()
    {
        //Arrange Max Speed
        float maxSpeedMultiplier = 1f;//get dexterity, exhaust level, % carry weight, health system
        _LocomotionSystem.AnimatorMaxSpeedMultiplier = maxSpeedMultiplier;
        //LocomotionSystem.FreeMovementSetting.sprintSpeed = 5.5f * maxSpeedMultiplier;

        //Arrange Stamina
        float maxStamina = 220f;//get exhaust level, str and health system
        _LocomotionSystem.MaxStamina = maxStamina;
        _LocomotionSystem.Stamina = maxStamina;

        //Arrange
    }


    public virtual void TakeDamage(Damage damage)
    {
        //Health -= damage; arrange health system
        //if (Health <= 0f)
        //Die();
    }
    public virtual void Die()
    {
        //IsDead = true; make state dead
    }

}
