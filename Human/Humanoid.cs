using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FischlWorks;
using UMA;
using UMA.CharacterSystem;
using FIMSpace;

public abstract class Humanoid : MonoBehaviour, ICanGetHurt
{
    public Transform _Transform => transform;
    public Animator _Animator { get { if (_animator == null) { _animator = _LocomotionSystem.GetComponentInChildren<Animator>(); _animator.updateMode = AnimatorUpdateMode.Fixed; _animator.cullingMode = AnimatorCullingMode.CullCompletely; } return _animator; } }
    private Animator _animator;
    public Rigidbody _Rigidbody { get; protected set; }
    public CapsuleCollider _MainCollider { get; protected set; }
    public LocomotionSystem _LocomotionSystem { get; protected set; }
    public csHomebrewIK _FootIKComponent { get; protected set; }
    public LeaningAnimator _LeaninganimatorComponent { get; protected set; }
    public SkinnedMeshRenderer _SkinnedMeshRenderer { get { if (_skinnedMeshRenderer == null) _skinnedMeshRenderer = _UmaDynamicAvatar?.transform.Find("UMARenderer")?.GetComponent<SkinnedMeshRenderer>(); return _skinnedMeshRenderer; } }
    private SkinnedMeshRenderer _skinnedMeshRenderer;
    public UMA.PoseTools.ExpressionPlayer _ExpressionPlayer { get; protected set; }
    public UMA.Dynamics.UMAPhysicsAvatar _RagdollAvatar { get { if (_ragdollAvatar == null) _ragdollAvatar = _UmaDynamicAvatar.GetComponent<UMA.Dynamics.UMAPhysicsAvatar>(); return _ragdollAvatar; } }
    private UMA.Dynamics.UMAPhysicsAvatar _ragdollAvatar;
    public DynamicCharacterAvatar _UmaDynamicAvatar { get; protected set; }
    public Dictionary<string, DnaSetter> _DNA { get; private set; }
    public Dictionary<string, float> _DnaData { get; set; }
    public List<UMATextRecipe> _WardrobeData { get; set; }
    public Dictionary<string, Color> _CharacterColors { get; set; }

    //Systems
    public string _Name { get; protected set; }
    public bool _IsMale { get; set; }
    public Class _Class { get; protected set; }
    public Family _Family { get; protected set; }
    public Group _AttachedGroup { get; protected set; }//not instance, a referance
    public InventoryHolder _InventoryHolder { get; protected set; }
    public Inventory _Inventory => _InventoryHolder._Inventory;
    public HealthSystem _HealthSystem { get; protected set; }
    public MovementState _MovementState { get; protected set; }
    public HandState _HandState { get; protected set; }

    public float _MuscleLevel { get; set; }
    public float _FatLevel { get; set; }
    public float _Height { get; set; }

    public float _NeedSleepAmount { get => _needSleepAmount; set => _needSleepAmount = Mathf.Clamp(value, 0f, 99f); }
    private float _needSleepAmount;
    public float _NeedCleaningAmount { get => _needCleaningAmount; set => _needCleaningAmount = Mathf.Clamp(value, 0f, 99f); }
    private float _needCleaningAmount;
    public float _NeedEatAmount { get => _needEatAmount; set => _needEatAmount = Mathf.Clamp(value, 0f, 99f); }
    private float _needEatAmount;
    public float _NeedDrinkAmount { get => _needDrinkAmount; set => _needDrinkAmount = Mathf.Clamp(value, 0f, 99f); }
    private float _needDrinkAmount;
    public float _NeedPissingAmount { get => _needPissingAmount; set => _needPissingAmount = Mathf.Clamp(value, 0f, 99f); }
    private float _needPissingAmount;
    public float _NeedPoopingAmount { get => _needPoopingAmount; set => _needPoopingAmount = Mathf.Clamp(value, 0f, 99f); }
    private float _needPoopingAmount;
    public float _MaxStamina { get; set; }
    public float _Stamina { get => _stamina; set { _stamina = Mathf.Clamp(value, 0f, _MaxStamina); } }
    private float _stamina;
    public float _WaitForRunLastTriggerTime { get; set; }

    //public Characteristic _Characteristic { get; private set; }

    public float _DefaultMeleeWeaponDamage => 25f;
    public Transform _RightHandHolderTransform { get; private set; }
    public Transform _LeftHandHolderTransform { get; private set; }
    public Transform _RightWeaponHolder { get; private set; }
    public Transform _LeftWeaponHolder { get; private set; }

    public MeleeWeapon _RightHandPunch { get; private set; }
    public MeleeWeapon _LeftHandPunch { get; private set; }
    public MeleeWeapon _RightKick { get; private set; }
    public MeleeWeapon _LeftKick { get; private set; }

    #region Equippables
    public Item _RightHandEquippedItemRef;
    public Item _LeftHandEquippedItemRef;
    public Item _BackCarryItemRef;
    public Item _HeadGearItemRef { get { return _headGear; } set { _headGear = value; } }
    private Item _headGear;
    public Item _GlovesItemRef { get { return _gloves; } set { _gloves = value; } }
    private Item _gloves;
    public Item _ClothingItemRef { get { return _clothing; } set { _clothing = value; } }
    private Item _clothing;
    public ArmorItem _ChestArmorItemRef { get { return _chestArmor; } set { _chestArmor = value; } }
    private ArmorItem _chestArmor;
    public ArmorItem _LegsArmorItemRef { get { return _legsArmor; } set { _legsArmor = value; } }
    private ArmorItem _legsArmor;
    public Item _BootsItemRef { get { return _boots; } set { _boots = value; } }
    private Item _boots;
    #endregion

    public float _SizeMultiplier { get; private set; }
    public virtual Vector2 _DirectionInput { get; }
    public float _SlopeSpeedAdder { get; set; }
    public bool _IsInFastWalkMode { get; set; }
    public bool _SprintInput { get; set; }
    public bool _IsInCombatMode { get; set; }
    public bool _CrouchInput { get; set; }
    public bool _JumpInput { get; set; }
    public bool _InteractInput { get; set; }
    public bool _LightAttackInput { get; set; }
    public bool _HeavyAttackInput { get; set; }
    public bool _KickInput { get; set; }
    public bool _DodgeInput { get; set; }
    public bool _BlockInput { get; set; }
    public bool _ParryInput { get; set; }
    public bool _AimInputForThrowInput { get; set; }

    public float _AttackReadyTime { get; set; }
    public float _WaitTimeForNextAttack { get; set; }
    public string _LastReadyAnimName { get; set; }
    public float _AttackReadySpeed { get; set; }
    public float _HeavyAttackThreshold => 0.4f;
    public float _HeavyAttackMultiplier => _AttackReadyTime < _HeavyAttackThreshold ? 1f : _AttackReadyTime / 2f + 1f;
    public bool _IsHandsEmpty => _RightHandEquippedItemRef == null && _LeftHandEquippedItemRef == null;
    public AttackDirectionFrom _LastAttackDirectionFrom { get; set; }
    public bool _IsAttackingFromLeftHandWeapon { get; set; }
    public Weapon _LastAttackWeapon { get; set; }

    public bool _IsDodging { get; set; }
    public bool _IsBlocking { get; set; }
    public bool _IsAttacking { get; set; }
    public bool _IsInAttackReady { get; set; }
    public bool _IsStaggered { get; set; }
    public bool _IsStrafing { get; set; }
    public bool _IsJumping { get; set; }
    public bool _IsGrounded { get; set; }
    public bool _IsSprinting { get; set; }
    public bool _StopMove { get; set; }

    private Transform _headTransform;

    [HideInInspector] public float _JumpTimer = 0.25f;
    [HideInInspector] public float _JumpCounter;
    public float _AimSpeed { get { if (_MovementState is CrouchMoveState) return 3f; else if (_MovementState is ProneMoveState) return 1.25f; else return 5f; } }
    public float _LastTimeRotated { get; set; }
    public float _LastTimeAttacked { get; set; }
    public float _LastAttackReadyTime { get; set; }
    public float _LastTimeDodged { get; set; }
    public float _LastTimeTriedParry { get; set; }

    public RaycastHit _RayFoLook;
    public Coroutine _RotateAroundCoroutine;

    public Coroutine _DodgeMoveCoroutine;
    public Coroutine _AttackMoveCoroutine;
    public Coroutine _StaggerCoroutine;
    public bool _IsInClosedSpace { get; private set; }

    private float _walkSoundCounter;
    private bool _umaWaitingForCompletion;

    private float _runtimeLoadUnloadCounter;
    private float _checkForClosedSpaceCounter;
    private float _lastAlphaForSnow;
    private float _targetAlphaForSnow;
    private float _checkForSnowCounter;
    private float _checkForSnowThreshold;

    public bool _ChangeShaderCompleted { get; private set; }
    private Vector3 _distanceToPlayer;
    private Coroutine _changeShaderFinishedCoroutine;
    private Coroutine _rightHandWeaponTransformCoroutine;
    private Coroutine _leftHandWeaponTransformCoroutine;

    #region Method Parameters For Opt
    #endregion
    protected void AwakeForAway()
    {
        if (_HealthSystem == null)
        {
            _HealthSystem = new HealthSystem();
            _HealthSystem.Init(this);
        }

        if (_InventoryHolder == null)
            _InventoryHolder = GetComponent<InventoryHolder>();
    }
    protected virtual void Awake()
    {
        AwakeForAway();

        _LocomotionSystem = GetComponentInChildren<LocomotionSystem>();
        _FootIKComponent = _LocomotionSystem.GetComponentInChildren<csHomebrewIK>();
        _LeaninganimatorComponent = _LocomotionSystem.GetComponentInChildren<LeaningAnimator>();
        _UmaDynamicAvatar = _LocomotionSystem.transform.Find("char").GetComponent<DynamicCharacterAvatar>();
        _ExpressionPlayer = _UmaDynamicAvatar.GetComponent<UMA.PoseTools.ExpressionPlayer>();
        _ragdollAvatar = _UmaDynamicAvatar.GetComponent<UMA.Dynamics.UMAPhysicsAvatar>();
        if (_UmaDynamicAvatar != null)
            NPCManager.SetGender(_UmaDynamicAvatar, _IsMale);
        InitOrLoadUmaCharacter();
        _checkForSnowThreshold = Random.Range(0.85f, 1f);
        _RightHandHolderTransform = _LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand/RightHolder/RMovable");
        _LeftHandHolderTransform = _LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHolder/LMovable");
        _LeftHandHolderTransform.localEulerAngles = new Vector3(180f, 0f, 0f);
        _RightWeaponHolder = _LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LowerBack/RightWeaponHolder/RWMovable");
        _LeftWeaponHolder = _LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LowerBack/LeftWeaponHolder/LWMovable");
        _RightHandPunch = _LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand/RightPunch").GetComponent<MeleeWeapon>();
        _LeftHandPunch = _LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftPunch").GetComponent<MeleeWeapon>();
        _RightKick = _LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/RightUpLeg/RightLeg/RightFoot/RightKick").GetComponent<MeleeWeapon>();
        _LeftKick = _LocomotionSystem.transform.Find("char/Root/Global/Position/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftKick").GetComponent<MeleeWeapon>();
    }
    protected virtual void Start()
    {
        _LocomotionSystem.Init();
        ArrangeStartingStates();
    }
    protected virtual void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;
        _distanceToPlayer = new Vector3(GameManager._Instance._Player.transform.position.x - transform.position.x, 0f, GameManager._Instance._Player.transform.position.z - transform.position.z);
        _HealthSystem.Update();

        if (_UmaDynamicAvatar != null)
            ControlUmaDataRuntimeLoadUnload();

        if (_UmaDynamicAvatar != null && _UmaDynamicAvatar.BuildCharacterEnabled && _MovementState != null)
        {
            ArrangePlaneSound();
            ArrangeStamina();
            ArrangeIsInClosedSpace();
            ArrangeSnowLayer();
            _MovementState.DoState();
            _HandState.DoState();
            ArrangeExtraGravity();
        }

        if (_umaWaitingForCompletion && _Animator.avatar != null)
            UmaUpdateCompleted();
    }
    private void FixedUpdate()
    {
        if (_Rigidbody.isKinematic) return;

        if (_UmaDynamicAvatar != null && _UmaDynamicAvatar.BuildCharacterEnabled && _MovementState != null)
            _MovementState.FixedUpdate();
    }
    private void OnAnimatorMove()
    {
        ControlAnimatorRootMotion();
    }
    private void InitOrLoadUmaCharacter()
    {
        _UmaDynamicAvatar.CharacterCreated.AddListener((a) => UmaCreated());
        _UmaDynamicAvatar.CharacterUpdated.AddListener((a) => UmaUpdated());
    }
    public void UmaCreated()
    {
        _UmaDynamicAvatar.BuildCharacterEnabled = false;

        _headTransform = _UmaDynamicAvatar.transform.Find("Root").Find("Global").Find("Position").Find("Hips").Find("LowerBack").Find("Spine").Find("Spine1").Find("Neck").Find("Head");
        SetDna(true);
        SetWardrobe();
        UmaUpdated();

        if (_RightHandEquippedItemRef != null)
            _RightHandEquippedItemRef.SpawnHandItem();
        if (_LeftHandEquippedItemRef != null)
            _LeftHandEquippedItemRef.SpawnHandItem();
    }
    public void UmaUpdated()
    {
        _umaWaitingForCompletion = true;
    }
    private void UmaUpdateCompleted()
    {
        if (_headTransform == null) return;

        _umaWaitingForCompletion = false;
        _SizeMultiplier = (_headTransform.position.y - transform.position.y) / 1.77f;
        if (GetComponentInChildren<csHomebrewIK>() != null)
            GetComponentInChildren<csHomebrewIK>().StartForUma();
        _UmaDynamicAvatar.BuildCharacterEnabled = true;
        _Rigidbody.freezeRotation = false; // animation bug workaround
        GameManager._Instance.CallForAction(() =>
        {
            _animator = _LocomotionSystem.GetComponentInChildren<Animator>(); _animator.updateMode = AnimatorUpdateMode.Fixed; _animator.cullingMode = AnimatorCullingMode.CullCompletely; _Rigidbody.freezeRotation = true;
            //float height = GetDnaValueByName("height");
            //_animator.speed = height <= 0.5f ? 0.77f + (0.9f - 0.77f) * (height / 0.5f) : 0.9f + (1.25f - 0.9f) * ((height - 0.5f) / 0.5f);
        }, 0.1f);
    }

    private bool IsInNearestNPCs()
    {
        return NPCManager._AllNPCs.IndexOf(this as NPC) < (Options._Instance._Quality == 0 ? 5 : (Options._Instance._Quality == 1 ? 10 : 20));
    }
    private void ControlUmaDataRuntimeLoadUnload()
    {
        if (this is Player) return;

        _runtimeLoadUnloadCounter += Time.deltaTime;
        if (_runtimeLoadUnloadCounter < 1f) return;
        _runtimeLoadUnloadCounter = 0f;


        if (_distanceToPlayer.magnitude < 30f)
            EnableHumanData();
        else if (_distanceToPlayer.magnitude > 40f)
            DisableHumanData();

        /*if (IsInNearestNPCs())
            transform.Find("NPC(Clone)").Find("Canvas").Find("Test").GetComponent<UnityEngine.UI.Image>().enabled = true;
        else
            transform.Find("NPC(Clone)").Find("Canvas").Find("Test").GetComponent<UnityEngine.UI.Image>().enabled = false;*/

        if (IsInNearestNPCs())
            EnableHumanAdditionals();
        else
            DisableHumanAdditionals();
    }
    private void EnableHumanAdditionals()
    {
        if (_MovementState is UnconsciousMoveState) return;

        if (Options._Instance._IsExpressionPlayerEnabled)
        {
            if (!_ExpressionPlayer.enabled)
            {
                _ExpressionPlayer.enabled = true;
                _ExpressionPlayer.GetComponent<TwistBones>().enabled = true;
            }
        }
        else
        {
            if (_ExpressionPlayer.enabled)
            {
                _ExpressionPlayer.enabled = false;
                _ExpressionPlayer.GetComponent<TwistBones>().enabled = false;
            }
        }
        if (Options._Instance._IsLeaningEnabled)
        {
            if (!_LeaninganimatorComponent.enabled)
                _LeaninganimatorComponent.enabled = true;
        }
        else
        {
            if (_LeaninganimatorComponent.enabled)
                _LeaninganimatorComponent.enabled = false;
        }

        if (Options._Instance._IsFootIKEnabled && ((_MovementState is LocomotionState) || (_MovementState is CrouchMoveState)))
        {
            if (!_FootIKComponent.enabled)
                _FootIKComponent.enabled = true;
        }
        else
        {
            if (_FootIKComponent.enabled)
                _FootIKComponent.enabled = false;
        }
    }
    public void DisableHumanAdditionals()
    {
        if (_ExpressionPlayer.enabled)
        {
            _ExpressionPlayer.enabled = false;
            _ExpressionPlayer.GetComponent<TwistBones>().enabled = false;
        }
        if (_LeaninganimatorComponent.enabled)
            _LeaninganimatorComponent.enabled = false;
        if (_FootIKComponent.enabled)
            _FootIKComponent.enabled = false;

    }
    public void EnableHumanData()
    {
        if (this is NPC && _UmaDynamicAvatar.BuildCharacterEnabled) return;

        NPCManager.SetGender(_UmaDynamicAvatar, _IsMale);
        SetDna(true);
        SetWardrobe();

        if (_CharacterColors != null)
        {
            foreach (var color in _CharacterColors)
            {
                NPCManager.ChangeColor(_UmaDynamicAvatar, color.Key, color.Value);
            }
        }

        _Animator.enabled = true;
        _UmaDynamicAvatar.BuildCharacterEnabled = true;
    }
    public void DisableHumanData()
    {
        //if (!_UmaDynamicAvatar.BuildCharacterEnabled) return;

        if (this is NPC && (_SkinnedMeshRenderer == null || _SkinnedMeshRenderer.sharedMesh == null)) return;

        if (_RightHandEquippedItemRef != null)
            _RightHandEquippedItemRef.DespawnHandItemHandle();
        if (_LeftHandEquippedItemRef != null)
            _LeftHandEquippedItemRef.DespawnHandItemHandle();

        if (!(this is Player))
            DisableHumanAdditionals();

        _Animator.enabled = false;


        if (_SkinnedMeshRenderer != null && _SkinnedMeshRenderer.sharedMesh != null)
        {
            Destroy(_SkinnedMeshRenderer.sharedMesh);
            _SkinnedMeshRenderer.sharedMesh = null;
        }

        _UmaDynamicAvatar.BuildCharacterEnabled = false;
        if (_UmaDynamicAvatar.umaData != null)
        {
            _UmaDynamicAvatar.umaData.CleanAvatar();
            _UmaDynamicAvatar.umaData.CleanMesh(false);
            _UmaDynamicAvatar.umaData.CleanTextures();
        }
    }

    public void ActivateCombatMode()
    {
        if (CameraController._Instance._IsInCoolAngleMode) return;
        if (_IsInCombatMode) return;

        bool conditions = _MovementState is LocomotionState;
        if (!conditions) return;

        _IsInFastWalkMode = false;
        _IsInCombatMode = true;
        if (_RightHandEquippedItemRef != null)
            GameManager._Instance.CoroutineCall(ref _rightHandWeaponTransformCoroutine, HandWeaponTransformCoroutine(true, true), this);
        if (_LeftHandEquippedItemRef != null)
            GameManager._Instance.CoroutineCall(ref _leftHandWeaponTransformCoroutine, HandWeaponTransformCoroutine(true, false), this);
    }
    public void DisableCombatMode()
    {
        if (!_IsInCombatMode) return;

        bool conditions = _MovementState is LocomotionState;
        if (!conditions) return;

        _IsInCombatMode = false;
        if (_RightHandEquippedItemRef != null)
            GameManager._Instance.CoroutineCall(ref _rightHandWeaponTransformCoroutine, HandWeaponTransformCoroutine(false, true), this);
        if (_LeftHandEquippedItemRef != null)
            GameManager._Instance.CoroutineCall(ref _leftHandWeaponTransformCoroutine, HandWeaponTransformCoroutine(false, false), this);
    }
    public void StopCombatActions()
    {
        _IsBlocking = false;
        //stop attacks
    }
    private IEnumerator HandWeaponTransformCoroutine(bool isToHand, bool isRight)
    {
        float timer = 0;
        float firstThreshold = isToHand ? 0.2f : 0.5f;
        float secondThreshold = isToHand ? 0.3f : 0.6f;

        var handle = ((isRight ? _RightHandEquippedItemRef : _LeftHandEquippedItemRef) as WeaponItem)._SpawnedHandle;
        while (!(handle.IsValid() && handle.IsDone && handle.Result != null))
        {
            if (timer > 1f)
            {
                Debug.LogError("adressable did not load weapon!");
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if ((isRight ? _RightHandEquippedItemRef : _LeftHandEquippedItemRef) == null)
            yield break;

        timer = 0;
        Transform beforeParentTransform = isToHand ? (isRight ? _RightWeaponHolder : _LeftWeaponHolder) : (isRight ? _RightHandHolderTransform : _LeftHandHolderTransform);
        Transform weaponTransform = ((isRight ? _RightHandEquippedItemRef : _LeftHandEquippedItemRef) as WeaponItem)._SpawnedHandle.Result.transform;
        if (weaponTransform == null) yield break;
        weaponTransform.SetParent(beforeParentTransform, true);
        //weaponTransform.localPosition = Vector3.zero;
        //weaponTransform.localEulerAngles = Vector3.zero;
        ChangeAnimation(isToHand ? (isRight ? "RightTakeFromHolder" : "LeftTakeFromHolder") : (isRight ? "RightToHolder" : "LeftToHolder"));

        while (timer < firstThreshold)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if ((isRight ? _RightHandEquippedItemRef : _LeftHandEquippedItemRef) == null || weaponTransform == null)
            yield break;

        Transform targetParentTransform = isToHand ? (isRight ? _RightHandHolderTransform : _LeftHandHolderTransform) : (isRight ? _RightWeaponHolder : _LeftWeaponHolder);
        weaponTransform.SetParent(targetParentTransform, true);
        ICanBeEquippedForDefinition iCanBeEquippedForDefinition = ((isRight ? _RightHandEquippedItemRef : _LeftHandEquippedItemRef)._ItemDefinition as ICanBeEquippedForDefinition);
        Vector3 targetPos = isToHand ? iCanBeEquippedForDefinition._PosOffset : iCanBeEquippedForDefinition._DefPosOffset;
        Vector3 targetAngles = isToHand ? iCanBeEquippedForDefinition._AnglesOffset : iCanBeEquippedForDefinition._DefAnglesOffset;
        Quaternion targetQuaternion = Quaternion.Euler(targetAngles);

        while (timer < secondThreshold)
        {
            if (weaponTransform == null)
                yield break;
            weaponTransform.localPosition = Vector3.Lerp(weaponTransform.localPosition, targetPos, Time.deltaTime * 12f);
            weaponTransform.localRotation = Quaternion.Lerp(weaponTransform.localRotation, targetQuaternion, Time.deltaTime * 8f);
            timer += Time.deltaTime;
            yield return null;
        }
        if (weaponTransform == null)
            yield break;
        weaponTransform.localPosition = targetPos;
        weaponTransform.localRotation = targetQuaternion;
    }

    public void WearWardrobe(UMATextRecipe recipe, bool isRefresh = false)
    {
        if (!isRefresh)
        {
            if (_WardrobeData.Contains(recipe)) return;

            UMATextRecipe tempRecipe;
            for (int i = 0; i < _WardrobeData.Count; i++)
            {
                tempRecipe = _WardrobeData[i];
                if (recipe.wardrobeSlot == tempRecipe.wardrobeSlot)
                {
                    Debug.LogError("Wardrobe slot is equipped!");
                    return;
                }
            }

            _WardrobeData.Add(recipe);
        }

        if (_UmaDynamicAvatar == null) return;

        _UmaDynamicAvatar.SetSlot(recipe);

        if (_UmaDynamicAvatar.BuildCharacterEnabled)
        {
            _UmaDynamicAvatar.BuildCharacterEnabled = false;
            _UmaDynamicAvatar.BuildCharacterEnabled = true;
        }
    }
    public void RemoveWardrobe(UMATextRecipe recipe)
    {
        if (!_WardrobeData.Contains(recipe)) return;

        string removedSlot = recipe.wardrobeSlot;
        _WardrobeData.Remove(recipe);

        if (_UmaDynamicAvatar == null) return;

        _UmaDynamicAvatar.ClearSlot(removedSlot);

        /*for (int i = 0; i < _WardrobeData.Count; i++)
        {
            recipe = _WardrobeData[i];
            if (recipe.wardrobeSlot == removedSlot)
            {
                //_UmaDynamicAvatar.SetSlot(recipe);
                Debug.LogError("Still Has Wardrobe Slot!");
                break;
            }
        }*/

        if (_UmaDynamicAvatar.BuildCharacterEnabled)
        {
            _UmaDynamicAvatar.BuildCharacterEnabled = false;
            _UmaDynamicAvatar.BuildCharacterEnabled = true;
        }
    }
    public float GetDnaValueByName(string dnaName)
    {
        if (_UmaDynamicAvatar == null) return -1f;

        var values = _UmaDynamicAvatar.GetDNAValues();
        if (values.ContainsKey(dnaName))
            return values[dnaName];
        return -1f;
    }
    public void ChangeMuscleAmount(bool isIncreasing, int amount = 1, bool isRebuilding = true)
    {
        float newAmount = _MuscleLevel;
        if (isIncreasing)
            newAmount += 0.025f * amount;
        else
            newAmount -= 0.025f * amount;
        newAmount = Mathf.Clamp(newAmount, 0.15f, 0.75f);
        _MuscleLevel = newAmount;

        SetLevelsToAvatar();
    }
    public void ChangeFatAmount(bool isIncreasing, int amount = 1, bool isRebuilding = true)
    {
        float newAmount = _FatLevel;
        if (isIncreasing)
            newAmount += 0.025f * amount;
        else
            newAmount -= 0.025f * amount;
        newAmount = Mathf.Clamp(newAmount, 0.15f, 0.75f);
        _FatLevel = newAmount;

        SetLevelsToAvatar();
    }
    public void SetLevelsToAvatar()
    {
        _DnaData["armWidth"] = _MuscleLevel;
        _DnaData["forearmWidth"] = _MuscleLevel;
        _DnaData["upperMuscle"] = _MuscleLevel;
        _DnaData["lowerMuscle"] = _MuscleLevel;

        if (_IsMale)
            _DnaData["bodyFitness"] = _MuscleLevel;

        _DnaData["upperWeight"] = _MuscleLevel;
        _DnaData["lowerWeight"] = _MuscleLevel;
        _DnaData["belly"] = _MuscleLevel;
        _DnaData["lowerMuscle"] = _MuscleLevel;
    }


    public void SetDna(bool isRebuilding)
    {
        if (_DnaData == null || _UmaDynamicAvatar == null) return;


        _DNA = _UmaDynamicAvatar.GetDNA();
        foreach (var item in _DNA.Keys)
        {
            if (_DnaData.ContainsKey(item))
                _DNA[item].Set(_DnaData[item]);
        }

        if (isRebuilding && _UmaDynamicAvatar.BuildCharacterEnabled)
        {
            _UmaDynamicAvatar.BuildCharacterEnabled = false;
            _UmaDynamicAvatar.BuildCharacterEnabled = true;
        }
    }
    private void SetWardrobe()
    {
        if (_WardrobeData == null || _UmaDynamicAvatar == null) return;

        _UmaDynamicAvatar.ClearSlots();
        foreach (var wardrobe in _WardrobeData)
        {
            WearWardrobe(wardrobe, true);
        }
    }

    private IEnumerator ChangeShaderFinishedCoroutine()
    {
        _ChangeShaderCompleted = false;
        yield return new WaitForSeconds(1f);
        _ChangeShaderCompleted = true;
    }
    private Texture2D ConvertToMaskMap(Texture metallicTex, float smoothness)
    {
        if (metallicTex == null) return null;

        RenderTexture tmp = RenderTexture.GetTemporary(metallicTex.width, metallicTex.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(metallicTex, tmp);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D src = new Texture2D(metallicTex.width, metallicTex.height, TextureFormat.RGBA32, false);
        src.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        src.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(tmp);

        // pixel düzenle
        Color[] pixels = src.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float r = pixels[i].r;
            float s = pixels[i].a;
            pixels[i] = new Color(r, 1f, 1f, s == 1f ? smoothness : s);
        }

        Texture2D mask = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        mask.SetPixels(pixels);
        mask.Apply();

        return mask;
    }


    private void ArrangeIsInClosedSpace()
    {
        if (_checkForClosedSpaceCounter > 0.5f)
        {
            _checkForClosedSpaceCounter = 0f;

            if (GameManager._Instance.IsInClosedSpace(transform.position))
                _IsInClosedSpace = true;
            else
                _IsInClosedSpace = false;
        }
        else
            _checkForClosedSpaceCounter += Time.deltaTime;
    }
    private void ArrangeExtraGravity()
    {
        if (_IsGrounded)
            _LocomotionSystem.extraGravity = 0f;
        else
            _LocomotionSystem.extraGravity = -8f;
    }
    private void ArrangeSnowLayer()
    {
        if (_UmaDynamicAvatar == null || _SkinnedMeshRenderer == null) return;

        if (Gaia.ProceduralWorldsGlobalWeather.Instance.IsSnowing && !_IsInClosedSpace) // && _Rigidbody.linearVelocity.magnitude <= 2f)
            _targetAlphaForSnow = 1f;
        else
            _targetAlphaForSnow = 0f;

        /*if (_lastAlphaForSnow > 0.4f && _Rigidbody.linearVelocity.magnitude > 2f)
        {
            _lastAlphaForSnow = 0f;
            _targetAlphaForSnow = 0f;
            Destroy(Instantiate(PrefabHolder._Instance._SnowFallsVFX, transform.position + Vector3.up, Quaternion.identity), 2f);
        }*/

        _checkForSnowCounter += Time.deltaTime;
        if (_checkForSnowCounter < _checkForSnowThreshold) return;
        _checkForSnowCounter = 0f;

        if (_lastAlphaForSnow != _targetAlphaForSnow)
        {
            _lastAlphaForSnow = Mathf.MoveTowards(_lastAlphaForSnow, _targetAlphaForSnow, 0.05f);
            Material[] mats = _SkinnedMeshRenderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i].shader.name.StartsWith("Shader Graphs/PW_")) continue;

                mats[i].SetColor("_PW_CoverLayer1Color", new Color(1f, 1f, 1f, _lastAlphaForSnow));
            }
        }
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

        EnterState(new LocomotionState(this));//get from save
        EnterState(new EmptyHandsState(this));//get from save

        //Arrange Max Speed
        float maxSpeedMultiplier = 1f;//get dexterity, exhaust level, % carry weight, health system
        _LocomotionSystem.AnimatorMaxSpeedMultiplier = maxSpeedMultiplier;
        //LocomotionSystem.FreeMovementSetting.sprintSpeed = 5.5f * maxSpeedMultiplier;

        //Arrange Stamina
        float maxStamina = 220f;//get exhaust level, str and health system
        _MaxStamina = maxStamina;
        _Stamina = maxStamina;

        //Arrange
    }
    public IEnumerator Staggering(float second)
    {
        float timer = 0;
        while (timer < second)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        _IsStaggered = false;
        ChangeAnimation("EmptyArms", 0.1f);
    }
    public IEnumerator Dodging()
    {
        float timer = 0;
        while (timer < 0.25f)
        {
            Vector3 targetVel = Vector3.ProjectOnPlane(new Vector3(0f, _Rigidbody.linearVelocity.y, 0f), _LocomotionSystem.groundHit.normal);
            _Rigidbody.linearVelocity = Vector3.Lerp(_Rigidbody.linearVelocity, targetVel, Time.deltaTime * 1.4f);
            timer += Time.deltaTime;
            yield return null;
        }
        _FootIKComponent.SetTargetWeight(1f);
        _IsDodging = false;
    }
    public IEnumerator AttackMoving()
    {
        float timer = 0;
        while (timer < 0.5f)
        {
            Vector3 targetVel = Vector3.ProjectOnPlane(new Vector3(0f, _Rigidbody.linearVelocity.y, 0f), _LocomotionSystem.groundHit.normal);
            _Rigidbody.linearVelocity = Vector3.Lerp(_Rigidbody.linearVelocity, targetVel, Time.deltaTime * 1.6f);
            timer += Time.deltaTime;
            yield return null;
        }
        _FootIKComponent.SetTargetWeight(1f);
    }
    public void ChangeAnimation(string name, float fadeTime = 0.2f, float attackSpeedMultiplier = 1f, int layer = -1)
    {
        _Animator.SetFloat(AnimatorParameters.AttackSpeedMultiplier, attackSpeedMultiplier);
        _Animator.CrossFadeInFixedTime(name, fadeTime, layer);
    }
    public void ChangeAnimationWithOffset(string name, float normalizedTimeOffset, float fadeTime, float attackSpeedMultiplier = 1f, int layer = -1)
    {
        _Animator.CrossFadeInFixedTime(name, fadeTime, layer, normalizedTimeOffset);
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
        if (_distanceToPlayer.magnitude > 40f) return;

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
            _Stamina -= Time.deltaTime * 15f;
        }
        else if (!_LocomotionSystem.FreeMovementSetting.walkByDefault)
        {
            _Stamina -= Time.deltaTime * 2f;
        }
        else
        {
            _Stamina += Time.deltaTime * 5f;
        }
    }
    public void LookAt(Vector3 pos, float lerpSpeed = 10f)
    {
        if (pos == transform.position) return;

        transform.forward = Vector3.Lerp(transform.forward, (pos - transform.position).normalized, Time.deltaTime * lerpSpeed);
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
    }

    public void Blocked(Damage damage)
    {
        MovementStateMethods.Stagger(this, 0.2f, damage._Direction);
        ChangeAnimation("Blocked");
    }
    public InventoryHolder CheckForNearInventories(bool isPlayer)
    {
        InventoryHolder nearestInventoryHolder = null;
        float nearestDistance = 0f;
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1f);
        foreach (var collider in colliders)
        {
            Humanoid human = GetHumanoidFromCollider(collider);
            if (collider != null && collider.transform.parent != null && collider.transform.parent.TryGetComponent(out InventoryHolder anotherInventoryHolder))
            {
                ChecForNearInventoriesCommon(anotherInventoryHolder, ref nearestDistance, ref nearestInventoryHolder, collider);
            }
            else if (human != null && human != this)
            {
                ChecForNearInventoriesCommon(human._InventoryHolder, ref nearestDistance, ref nearestInventoryHolder, collider);
            }
        }

        if (nearestInventoryHolder != null && isPlayer)
            GameManager._Instance.OpenAnotherInventory(nearestInventoryHolder._Inventory);

        return nearestInventoryHolder;
    }
    private void ChecForNearInventoriesCommon(InventoryHolder holder, ref float nearestDistance, ref InventoryHolder nearestInventoryHolder, Collider collider)
    {
        if (nearestInventoryHolder == null || nearestDistance > (collider.transform.position - transform.position).magnitude)
        {
            nearestDistance = (collider.transform.position - transform.position).magnitude;
            nearestInventoryHolder = holder;
        }
    }
    private Humanoid GetHumanoidFromCollider(Collider collider)
    {
        if (collider == null || (collider.gameObject.layer != LayerMask.NameToLayer("Human") && collider.gameObject.layer != LayerMask.NameToLayer("RagdollHitbox"))) return null;
        Transform parent = collider.transform;
        while (parent.parent != null)
        {
            parent = parent.parent;
        }
        return parent.GetComponent<Humanoid>();
    }
    public virtual void TakeDamage(Damage damage)
    {
        if (damage._TargetArmor != null)
            damage._TargetArmor._Durability -= damage._AmountBlocked / (damage._DamageType == DamageType.Cut ? 4f : (damage._DamageType == DamageType.Pierce ? 2.75f : 9f)) * (damage._TargetArmor._IsSteel ? 0.1f : 1f);
        float bleedingDamage = damage._Amount / 125f;
        if (damage._DamageType == DamageType.Cut) bleedingDamage = damage._Amount / 75f;
        else if (damage._DamageType == DamageType.Pierce) bleedingDamage = damage._Amount / 125f;
        else if (damage._DamageType == DamageType.Crush) bleedingDamage = damage._Amount / 400f;
        MovementStateMethods.Stagger(this, 1f, damage._Direction);
        HandStateMethods.PlayHitAnimation(this, damage);
        _HealthSystem.TakeDamage(damage, bleedingDamage);
    }
    public virtual void Die()
    {
        _IsInCombatMode = false;
        //IsDead = true; make state unconscious
    }

}
