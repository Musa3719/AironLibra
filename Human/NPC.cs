using System.Collections;
using System.Collections.Generic;
using UMA;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class NPC : Humanoid
{
    private Transform _ownTransform;
    public ushort _NpcIndex;
    public NpcLogic _NpcLogic;
    public NpcDialogue _NpcDialogue;
    public Vector3 _Pos;
    [HideInInspector] public float _UpdateNpcCounter;
    [HideInInspector] public bool? _NpcRequestForPathfinding;
    public override Vector2 _DirectionInput => _directionCurrent;
    public GameObject _TargetPoolNPC { get; private set; }
    public Vector3 _LastCornerFromPath { get; private set; }
    private Vector2 _directionCurrent;
    private Vector2 _directionInputFromPath;
    private List<Vector3> _cornersFromPath;
    private NavMeshPath _CurrentPath { get { if (_currentPath == null) _currentPath = new NavMeshPath(); return _currentPath; } }
    private NavMeshPath _currentPath;
    private float _logicUpdateCounter;
    private float _logicUpdateThreshold;
    private bool _isPathEnded;
    private float _segmentationMagnitude = 100f;
    private float _stuckOnPathTimer;
    private float _stuckArrangingTimer;
    private Vector3 _stuckArrangingPosition;
    private float _stuckThreshold = 2f;
    private double _lastTimeJumped;
    private float _nextPositionCheckCounter;
    private float _nextPositionCheckThresholdForUse => _IsSprinting ? _nextPositionCheckThreshold / 2.5f : _nextPositionCheckThreshold;
    private float _nextPositionCheckThreshold = 0.4f;
    private float _cliffCheckForwardForDir;
    private float _cliffCheckForwardForVel;
    private float _runtimeLoadUnloadCounter;
    private float _arrangeDistanceCounter;
    private float _arrangPosCounter;
    private float _arrangDirecitonCounter;
    public Vector3 _DistanceToPlayer { get; private set; }
    public int _NpcDistanceListIndex { get; set; }

    public Vector3 _MoveTargetPosition { get; set; }
    public bool _IsOnLinkMovement { get; private set; }

    private Coroutine _destroyMeshCoroutine;
    private Coroutine _attackReadyCoroutine;

    private Vector3 _checkCornerDist;

    private Ray _ray;
    private RaycastHit[] _checkForwardHit;

    protected override void Awake()
    {
        _ownTransform = transform;
        _ray = new Ray();
        _checkForwardHit = new RaycastHit[1];
        _DistanceToPlayer = 200f * Vector3.one;
        _NpcDistanceListIndex = 2000;
        Random.InitState(System.DateTime.Now.Millisecond + GetInstanceID());
        _UpdateNpcCounter = Random.Range(2f, 5f);
        _arrangeDistanceCounter = Random.Range(0f, 2f);
        _runtimeLoadUnloadCounter = Random.Range(0f, 1f);
        _arrangPosCounter = Random.Range(0f, 0.1f);
        _arrangDirecitonCounter = Random.Range(0f, 0.1f);
        _nextPositionCheckCounter = Random.Range(0f, _nextPositionCheckThreshold);
        if (_Class == null)
            _Class = new Peasant();//

        _NpcLogic = new NpcLogic(this);
        _NpcDialogue = new NpcDialogue(this);


        _isPathEnded = true;
        _Rigidbody = GetComponent<Rigidbody>();
        NPCManager.AddToList(this);
        if (transform.childCount == 0) { AwakeForAway(); return; }
        _MainCollider = transform.GetChild(0).Find("char").Find("MainCollider").GetComponent<CapsuleCollider>();

        base.Awake();
        _checkCornerDist = Vector3.zero;
        _cornersFromPath = new List<Vector3>();
    }
    protected override void Start()
    {
        if (_Rigidbody.isKinematic) return;

        if (_ExpressionPlayer != null)
        {
            _ExpressionPlayer.enabled = Options._Instance._IsExpressionPlayerEnabled;
            _ExpressionPlayer.GetComponent<UMA.TwistBones>().enabled = Options._Instance._IsExpressionPlayerEnabled;
        }
        if (_FootIKComponent != null)
            _FootIKComponent.enabled = Options._Instance._IsFootIKEnabled;
        if (_LeaninganimatorComponent != null)
            _LeaninganimatorComponent.enabled = Options._Instance._IsLeaningEnabled;

        Stop();
        base.Start();
        _Pos = _ownTransform.position;
        ArrangeNewMovementTarget(_Pos);
    }
    private void OnDestroy()
    {
        NPCManager.RemoveFromList(this);
    }

    public void UpdateNpc()
    {
        _NpcLogic.UpdateLogic();
    }
    protected override void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        _arrangPosCounter += Time.deltaTime;
        if (_arrangPosCounter > 0.1f)
        {
            _Pos = _ownTransform.position;
            _arrangPosCounter = 0f;
        }

        _arrangeDistanceCounter += Time.deltaTime;
        if (_arrangeDistanceCounter > 2f)
        {
            _DistanceToPlayer = GameManager._Instance._PlayerPos - _Pos;
            _arrangeDistanceCounter = 0f;
        }

        ControlUmaDataRuntimeLoadUnload();

        if (_UmaDynamicAvatar != null)
        {
            DisableInputs();

            if (GameManager._Instance._Is1)
                ArrangePath();
            if (GameManager._Instance._Is2)
                ArrangeDirection();
            if (GameManager._Instance._Is3)
                ArrangeStuck();
            if (GameManager._Instance._Is4)
                CheckNextPosition();
        }

        if (GameManager._Instance._Is5)
        {
            _Rigidbody.linearVelocity= new Vector3(1f, 0f, 1f) * 4f;
        }

        if (false)
            base.Update();

        if (_UmaDynamicAvatar != null && _MovementState != null && false)
            _MovementState.FixedUpdate();

        if (_InteractInput)
        {
            CheckForNearItemHolders(false, out InventoryHolder nearestInventoryHolder, out CarriableObject nearestCarriable);
            ///take send equip unequip if public, if not steal or trade 
        }

        //testing
        //ArrangeNewMovementTarget(WorldHandler._Instance._Player.transform.position);

        /*if (M_Input.GetKeyDown(KeyCode.Alpha1))
            ChangeMuscleAmount(false);
        if (M_Input.GetKeyDown(KeyCode.Alpha2))
            ChangeMuscleAmount(true);
        if (M_Input.GetKeyDown(KeyCode.Alpha3))
            ChangeFatAmount(false);
        if (M_Input.GetKeyDown(KeyCode.Alpha4))
            ChangeFatAmount(true);
        if (M_Input.GetKeyDown(KeyCode.F))
            _IsMale = true;*/
        if (M_Input.GetKeyDownForTesting(KeyCode.G))
            TryAttacking(false, 0.15f);
        if (M_Input.GetKeyDownForTesting(KeyCode.J))
            RangedAimToTransform(WorldHandler._Instance._Player.transform);
        //WorldAndNpcCreation.SetGender(_UmaDynamicAvatar, Random.Range(0, 2) == 0);
        //WorldAndNpcCreation.ChangeColor(_UmaDynamicAvatar, "Skin", Color.red);
        if (M_Input.GetKeyDownForTesting(KeyCode.M))
            ArrangeNewMovementTarget(WorldHandler._Instance._TestTransform.position);
        if (M_Input.GetKeyForTesting(KeyCode.N))
            _SprintInput = true;
        else
            _SprintInput = false;
    }

    public void DisableInputs()
    {
        _JumpInput = false;
        _CrouchInput = false;
        _InteractInput = false;
    }

    public void TryAttacking(bool isPunch, float targetAttackReadyTime)
    {
        if (!HandStateMethods.IsAttackPossible(this)) return;

        if (_HandState is MeleeWeaponHandState)
        {
            HandStateMethods.ArrangeIsAttackingFromLeftHand(this);
            HandStateMethods.ReadyAttackAnimation(this, HandStateMethods.GetCurrentWeaponType(this, _IsAttackingFromLeftHandWeapon));
            GameManager._Instance.CoroutineCall(ref _attackReadyCoroutine, AttackReadyCoroutine(isPunch, targetAttackReadyTime), this);
        }
        else if (_HandState is RangedWeaponHandState state && _IsInRangedAim)
        {
            _LightAttackInput = true;
        }
    }
    public void RangedAimToTransform(Transform target)
    {
        if (_HandState is RangedWeaponHandState state && HandStateMethods.IsRangedAimPossible(this))
        {
            _AimInput = true;
            _AimPosition = target.position;
        }
    }
    private IEnumerator AttackReadyCoroutine(bool isPunch, float targetAttackReadyTime)
    {
        float timer = 0f;
        while (timer < targetAttackReadyTime)
        {
            timer += Time.deltaTime;
            _AttackReadyTime = timer;
            yield return null;
        }
        _AttackReadyTime = targetAttackReadyTime;
        if (!HandStateMethods.IsAttackPossible(this)) { HandStateMethods.AttackCancelled(this); yield break; }

        if (isPunch)
            PunchOrKick(targetAttackReadyTime);
        else
            LightOrHeavyAttack(targetAttackReadyTime);
    }
    public void LightOrHeavyAttack(float targetAttackReadyTime)
    {
        if (targetAttackReadyTime > _HeavyAttackThreshold)
            _HeavyAttackInput = true;
        else
            _LightAttackInput = true;
    }
    public void PunchOrKick(float targetAttackReadyTime)
    {
        _KickInput = true;
    }

    public void SpawnNPCChild()
    {
        if (transform.childCount != 0) return;

        GameManager._Instance._NPCPool.GetOneFromPool(_TargetPoolNPC).transform.SetParent(transform);
        transform.GetChild(0).localPosition = Vector3.zero;
        _Rigidbody.isKinematic = false;
        _Rigidbody.WakeUp();
        enabled = true;

        Awake();
        Start();
    }
    public void DestroyNPCChild()
    {
        if (transform.childCount == 0) return;

        if (!transform.GetChild(0).name.StartsWith("NPC")) Debug.LogError("0 index child is not NPC!");
        _TargetPoolNPC = transform.GetChild(0).gameObject;
        GameManager._Instance._NPCPool.GameObjectToPool(transform.GetChild(0).gameObject);

        _Rigidbody.isKinematic = true;
        _Rigidbody.Sleep();
        DisableInputs();
        enabled = false;
    }
    private void ControlUmaDataRuntimeLoadUnload()
    {
        _runtimeLoadUnloadCounter += Time.deltaTime;
        if (_runtimeLoadUnloadCounter < 1f) return;
        _runtimeLoadUnloadCounter = 0f;

        if (_DistanceToPlayer.sqrMagnitude < 900f) //30
            EnableHumanData();
        else if (_DistanceToPlayer.sqrMagnitude > 1600f) //40
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
    private bool IsInNearestNPCs()
    {
        return _NpcDistanceListIndex < (Options._Instance._Quality == 0 ? 5 : (Options._Instance._Quality == 1 ? 10 : 20));
    }
    private void SetHumanLayer(int layer)
    {
        gameObject.layer = layer;
        Transform child = transform.GetChild(0);
        child.gameObject.layer = layer;
        child.Find("ProneCollider").gameObject.layer = layer;
        child.Find("CrouchCollider").gameObject.layer = layer;
        child.Find("char").gameObject.layer = layer;
        child.Find("char/MainCollider").gameObject.layer = layer;
    }
    private void EnableHumanData()
    {
        if (_UmaDynamicAvatar.BuildCharacterEnabled) return;

        if (_destroyMeshCoroutine != null)
            StopCoroutine(_destroyMeshCoroutine);

        SetHumanLayer(LayerMask.NameToLayer("HumanNear"));
        SetGender(_IsMale);
        SetDna(true);
        SetWardrobe();

        if (_CharacterColors != null)
        {
            foreach (var color in _CharacterColors)
            {
                ChangeColor(color.Key, color.Value);
            }
        }

        _Animator.enabled = true;
        _UmaDynamicAvatar.BuildCharacterEnabled = true;
    }
    private void DisableHumanData()
    {
        //if (!_UmaDynamicAvatar.BuildCharacterEnabled) return;
        if (_SkinnedMeshRenderer == null || _SkinnedMeshRenderer.sharedMesh == null) return;

        _UmaDynamicAvatar.BuildCharacterEnabled = false;
        SetHumanLayer(LayerMask.NameToLayer("HumanFar"));

        if (_RightHandEquippedItemRef != null)
            _RightHandEquippedItemRef.DespawnHandItem();
        if (_LeftHandEquippedItemRef != null)
            _LeftHandEquippedItemRef.DespawnHandItem();
        if (_BackCarryItemRef != null)
            _BackCarryItemRef.DespawnBackCarryItem();

        _Animator.enabled = false;
        DisableHumanAdditionals();

        GameManager._Instance.CoroutineCall(ref _destroyMeshCoroutine, DestroyMeshCoroutine(), this);
    }
    private IEnumerator DestroyMeshCoroutine()
    {
        while (_UmaDynamicAvatar.umaData != null && (_UmaDynamicAvatar.umaData.isMeshDirty || _UmaDynamicAvatar.umaData.isTextureDirty || _UmaDynamicAvatar.umaData.isAtlasDirty))
            yield return null;

        if (_SkinnedMeshRenderer != null && _SkinnedMeshRenderer.sharedMesh != null)
        {
            Destroy(_SkinnedMeshRenderer.sharedMesh);
            _SkinnedMeshRenderer.sharedMesh = null;
        }

        if (_UmaDynamicAvatar.umaData != null)
        {
            _UmaDynamicAvatar.umaData.CleanAvatar();
            _UmaDynamicAvatar.umaData.CleanMesh(false);
            _UmaDynamicAvatar.umaData.CleanTextures();
        }
    }
    private void EnableHumanAdditionals()
    {
        if (_MovementState is UnconsciousMoveState) return;

        if (!_TwistBones.enabled)
            _TwistBones.enabled = true;

        if (Options._Instance._IsExpressionPlayerEnabled)
        {
            if (!_ExpressionPlayer.enabled)
                _ExpressionPlayer.enabled = true;
        }
        else
        {
            if (_ExpressionPlayer.enabled)
                _ExpressionPlayer.enabled = false;
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
        if (_TwistBones.enabled)
            _TwistBones.enabled = false;
        if (_ExpressionPlayer.enabled)
            _ExpressionPlayer.enabled = false;
        if (_LeaninganimatorComponent.enabled)
            _LeaninganimatorComponent.enabled = false;
        if (_FootIKComponent.enabled)
            _FootIKComponent.enabled = false;
        if (_FootIKComponent.enabled)
            _FootIKComponent.enabled = false;
    }

    private void ArrangeDirection()
    {
        _arrangDirecitonCounter += Time.deltaTime;
        if (_arrangDirecitonCounter < 0.1f)
            return;
        _arrangDirecitonCounter = 0f;

        if (_isPathEnded)
        {
            if (_directionCurrent != Vector2.zero)
            {
                _directionCurrent = Vector2.Lerp(_directionCurrent, Vector2.zero, Time.deltaTime * 12f);
                if (_directionCurrent.sqrMagnitude < 0.05f)
                    _directionCurrent = Vector2.zero;
            }
            return;
        }

        _directionInputFromPath = new Vector2(_LastCornerFromPath.x - _Pos.x, _LastCornerFromPath.z - _Pos.z).normalized;
        if (Vector2.Dot(_directionCurrent, _directionInputFromPath) < 0f)
            _directionCurrent = Vector2.zero;
        _directionCurrent = Vector2.Lerp(_directionCurrent, _directionInputFromPath, Time.deltaTime * 8f).normalized;
    }
    private void ArrangeStuck()
    {
        if (!_isPathEnded)
        {
            if (_Rigidbody.linearVelocity.magnitude < 0.5f || _stuckArrangingTimer > 0f)
                _stuckOnPathTimer += Time.deltaTime;
            else
                _stuckOnPathTimer = 0f;

            if (_stuckArrangingTimer > 0f)
                _stuckArrangingTimer -= Time.deltaTime;

            if (_stuckOnPathTimer > _stuckThreshold * 0.5f && _stuckArrangingTimer == 0f)
            {
                _stuckArrangingTimer = 1f;
                _stuckArrangingPosition = _Pos;
                _stuckArrangingPosition.y = 0f;
                TryJump();
            }
            if (_stuckOnPathTimer > _stuckThreshold)
            {
                _stuckArrangingTimer = 0f;
                _stuckOnPathTimer = 0f;
                if ((_stuckArrangingPosition - new Vector3(_Pos.x, 0f, _Pos.z)).magnitude < 0.2f)
                {
                    Vector3 target = _MoveTargetPosition;
                    Stop(true);
                    ArrangeNewMovementTarget(target);
                }

            }
        }
    }
    private void CheckNextPosition()
    {
        if (_isPathEnded) return;

        if (_nextPositionCheckCounter < _nextPositionCheckThresholdForUse)
        {
            _nextPositionCheckCounter += Time.deltaTime;
        }
        else
        {
            _nextPositionCheckCounter -= _nextPositionCheckThresholdForUse;

            //check for cliff
            bool isStopping;
            Vector3 directionInput = new Vector3(_Rigidbody.linearVelocity.x, 0f, _Rigidbody.linearVelocity.z);
            _cliffCheckForwardForVel = Mathf.Lerp(_cliffCheckForwardForVel, directionInput.magnitude < 0.1f ? 1f : (_IsSprinting ? 3.25f : 1.5f), Time.deltaTime * 2f);
            //Debug.DrawRay(transform.position + Vector3.up * 0.8f + directionInput.normalized * _cliffCheckForwardForVel, -Vector3.up);
            _ray.origin = _Pos + Vector3.up * 0.8f + directionInput.normalized * _cliffCheckForwardForVel;
            _ray.direction = -Vector3.up;
            Physics.RaycastNonAlloc(_ray, _checkForwardHit, 8f, GameManager._Instance._TerrainSolidWaterMask);
            isStopping = _checkForwardHit[0].collider == null;

            directionInput = GameManager._Instance.Vector2ToVector3(_DirectionInput);
            _cliffCheckForwardForDir = Mathf.Lerp(_cliffCheckForwardForDir, directionInput.magnitude < 0.1f ? 1f : (_IsSprinting ? 3.25f : 1.5f), Time.deltaTime * 2f);
            //Debug.DrawRay(transform.position + Vector3.up * 0.8f + directionInput.normalized * _cliffCheckForwardForDir, -Vector3.up);
            _ray.origin = _Pos + Vector3.up * 0.8f + directionInput.normalized * _cliffCheckForwardForDir;
            _ray.direction = -Vector3.up;
            Physics.RaycastNonAlloc(_ray, _checkForwardHit, 8f, GameManager._Instance._TerrainSolidWaterMask);
            isStopping = isStopping ? isStopping : _checkForwardHit[0].collider == null;

            if (isStopping)
            {
                Vector3 target = _MoveTargetPosition;
                Stop(true);
                ArrangeNewMovementTarget(target);
                return;
            }

            //check for obstacle
            _ray.origin = _Pos + Vector3.up * 0.2f;
            _ray.direction = -Vector3.up;
            Physics.RaycastNonAlloc(_ray, _checkForwardHit, 8f, GameManager._Instance._TerrainSolidMask);
            if (_checkForwardHit[0].collider != null)
            {
                Vector3 rayDirection = new Vector3(_Rigidbody.linearVelocity.x, 0f, _Rigidbody.linearVelocity.z).normalized;
                if (_checkForwardHit[0].normal != Vector3.zero)
                {
                    rayDirection = Vector3.Cross(Vector3.Cross(_checkForwardHit[0].normal, rayDirection), _checkForwardHit[0].normal).normalized;
                }

                _ray.origin = _Pos + Vector3.up * 0.2f;
                _ray.direction = rayDirection;
                Physics.RaycastNonAlloc(_ray, _checkForwardHit, 8f, GameManager._Instance._TerrainSolidMask);
                if (_checkForwardHit[0].collider != null)
                {
                    if (_checkForwardHit[0].collider != null && Vector3.Dot(_checkForwardHit[0].normal, Vector3.up) < 0.3f)
                    {
                        _ray.origin = _Pos + Vector3.up * 0.8f;
                        _ray.direction = rayDirection;
                        Physics.RaycastNonAlloc(_ray, _checkForwardHit, 8f, GameManager._Instance._TerrainSolidMask);
                        if (_checkForwardHit[0].collider == null)
                            TryJump();
                    }
                }
            }
        }
    }
    private void TryJump()
    {
        if (_lastTimeJumped + 1 < Time.timeAsDouble)
        {
            _lastTimeJumped = Time.timeAsDouble;
            _JumpInput = true;
        }
    }

    public void Stop(bool willRetry = false)
    {
        _cornersFromPath.Clear();
        _LastCornerFromPath = _Pos;
        _directionInputFromPath = Vector2.zero;
        _directionCurrent = Vector2.zero;
        _isPathEnded = true;
        if (!willRetry)
            _MoveTargetPosition = _Pos;
    }
    public void ArrangeNewMovementTarget(Vector3 targetPos)
    {
        _MoveTargetPosition = targetPos;
    }
    private void ArrangePath()
    {
        Vector3 dist = (_MoveTargetPosition - _Pos);
        if (new Vector3(dist.x, 0f, dist.z).sqrMagnitude < 1f) { Stop(); return; }
        if (!_isPathEnded) return;

        if (_NpcRequestForPathfinding == null)
            _NpcRequestForPathfinding = false;
        if (!_NpcRequestForPathfinding.HasValue || !_NpcRequestForPathfinding.Value)
            return;

        _NpcRequestForPathfinding = null;

        Vector3 segmentatedTarget = _MoveTargetPosition;
        if (new Vector3(dist.x, 0f, dist.z).sqrMagnitude > _segmentationMagnitude * _segmentationMagnitude)
        {
            segmentatedTarget = _Pos + new Vector3((_MoveTargetPosition - _Pos).x, _Pos.y, (_MoveTargetPosition - _Pos).z).normalized * _segmentationMagnitude;
            //segmentatedTarget = GetWalkableTerrainPosition(segmentatedTarget);
        }
        _CurrentPath.ClearCorners();
        NavMesh.CalculatePath(_Pos, segmentatedTarget, NavMesh.AllAreas, _CurrentPath);
        _isPathEnded = false;
        _cornersFromPath.Clear();
        _nextPositionCheckCounter = _nextPositionCheckThresholdForUse;
        if ((_CurrentPath.corners.Length <= 1 || Vector3.Distance(_CurrentPath.corners[0], _CurrentPath.corners[1]) < 0.5f) && Vector3.Distance(_Pos, segmentatedTarget) > 0.5f)
        {
            _CurrentPath.ClearCorners();
            _cornersFromPath.Add(_Pos);
            _cornersFromPath.Add(segmentatedTarget);
        }
        foreach (var corner in _CurrentPath.corners)
        {
            //var a = new GameObject(); a.transform.position = corner;
            _cornersFromPath.Add(corner);
        }
        if (_cornersFromPath.Count > 1)
        {
            _LastCornerFromPath = _cornersFromPath[1];
            _directionInputFromPath = new Vector2(_cornersFromPath[1].x - _Pos.x, _cornersFromPath[1].z - _Pos.z).normalized;
            _cornersFromPath.RemoveAt(1);
            _cornersFromPath.RemoveAt(0);
        }
        else
        {
            _cornersFromPath.Clear();
            _directionInputFromPath = Vector2.zero;
        }
    }

    public void ArrangeMovementCorners()
    {
        if (_isPathEnded) return;

        _checkCornerDist = _LastCornerFromPath - _Pos;
        _checkCornerDist.y = 0f;

        if (_checkCornerDist.magnitude < (_IsSprinting ? 1f : 0.35f))
        {
            if (_cornersFromPath.Count > 0)
            {
                _LastCornerFromPath = _cornersFromPath[0];
                _directionInputFromPath = new Vector2(_cornersFromPath[0].x - _Pos.x, _cornersFromPath[0].z - _Pos.z).normalized;
                _cornersFromPath.RemoveAt(0);
            }
            else
            {
                Stop();
            }

        }
    }
}

