using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPC : Humanoid
{
    public ushort _NpcIndex;
    public override Vector2 _DirectionInput => _directionCurrent;
    public Vector3 _LastCornerFromPath { get; private set; }
    private Vector2 _directionCurrent;
    private Vector2 _directionInputFromPath;
    private List<Vector3> _cornersFromPath;
    private NavMeshPath _currentPath;
    private bool _isPathEnded;
    private float _segmentationMagnitude = 100f;
    private Coroutine _segmentatePathCoroutine;
    private float _stuckOnPathTimer;
    private float _stuckArrangingTimer;
    private Vector3 _stuckArrangingPosition;
    private float _stuckThreshold = 2f;
    private float _lastTimeJumped;
    private float _nextPositionCheckCounter;
    private float _nextPositionCheckThreshold = 0.1f;
    private float _cliffCheckForwardForDir;
    private float _cliffCheckForwardForVel;
    private float _tryToCreatePathTimer;
    private float _tryToCreatePathThreshold = 0.5f;
    public Vector3 _MoveTargetPosition { get; set; }
    public Vector3 _AimPosition { get; set; }
    public bool _IsOnLinkMovement { get; private set; }


    private Vector3 _checkCornerDist;

    protected override void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        NPCManager.AddToList(this);
        if (transform.childCount == 0) return;
        _MainCollider = transform.GetChild(0).Find("char").Find("Root").Find("Global").Find("Position").Find("Hips").Find("MainCollider").GetComponent<CapsuleCollider>();

        base.Awake();
        _checkCornerDist = Vector3.zero;
        _cornersFromPath = new List<Vector3>();

        _Class = new Peasant();//

    }
    protected override void Start()
    {
        if (_Rigidbody.isKinematic) return;

        Stop();
        base.Start();
    }
    private void OnDestroy()
    {
        NPCManager.RemoveFromList(this);
    }

    protected override void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        if (_UmaDynamicAvatar != null && _UmaDynamicAvatar.BuildCharacterEnabled)
        {
            //disable inputs
            _JumpInput = false;
            _CrouchInput = false;
            _InteractInput = false;

            ArrangeDirection();
            ArrangeStuck();
            CheckNextPosition();
        }

        base.Update();

        //testing
        if (M_Input.GetKeyDown(KeyCode.Alpha1))
            ChangeMuscleAmount(false);
        if (M_Input.GetKeyDown(KeyCode.Alpha2))
            ChangeMuscleAmount(true);
        if (M_Input.GetKeyDown(KeyCode.Alpha3))
            ChangeWeightAmount(false);
        if (M_Input.GetKeyDown(KeyCode.Alpha4))
            ChangeWeightAmount(true);
        if (M_Input.GetKeyDown(KeyCode.F))
            WorldAndNpcCreation.SetGender(_UmaDynamicAvatar, Random.Range(0, 2) == 0);
        //WorldAndNpcCreation.ChangeColor(_UmaDynamicAvatar, "Skin", Color.red);
        if (M_Input.GetKeyDown(KeyCode.E))
            DestroyNPCChild();
        if (M_Input.GetKeyDown(KeyCode.M))
            ArrangeNewMovementTarget(GameObject.Find("TargetPositionTest").transform.position);
        if (M_Input.GetKey(KeyCode.N))
            _SprintInput = true;
        else
            _SprintInput = false;

    }
    public void SpawnNPCChild()
    {
        if (transform.childCount != 0) return;

        GameManager._Instance._NPCPool.GetOneFromPool().transform.SetParent(transform);
        transform.GetChild(0).localPosition = Vector3.zero;
        _Rigidbody.isKinematic = false;
        _Rigidbody.WakeUp();
        this.enabled = true;

        Awake();
        Start();
    }
    public void DestroyNPCChild()
    {
        if (transform.childCount == 0) return;

        if (_ChangeShaderCompleted)
            GameManager._Instance._NPCPool.GameObjectToPool(transform.GetChild(0).gameObject);
        else
            Destroy(transform.GetChild(0).gameObject);

        _Rigidbody.isKinematic = true;
        _Rigidbody.Sleep();
        this.enabled = false;
    }
    private void ArrangeDirection()
    {
        if (_isPathEnded)
        {
            _directionCurrent = Vector2.Lerp(_directionCurrent, Vector2.zero, Time.deltaTime * 12f);
            return;
        }

        _directionInputFromPath = new Vector2(_LastCornerFromPath.x - transform.position.x, _LastCornerFromPath.z - transform.position.z).normalized;
        if (Vector2.Dot(_directionCurrent, _directionInputFromPath) < 0.1f)
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
                _stuckArrangingPosition = transform.position;
                _stuckArrangingPosition.y = 0f;
                TryJump();
            }
            if (_stuckOnPathTimer > _stuckThreshold)
            {
                _stuckArrangingTimer = 0f;
                _stuckOnPathTimer = 0f;
                if ((_stuckArrangingPosition - new Vector3(transform.position.x, 0f, transform.position.z)).magnitude < 0.2f)
                {
                    Vector3 target = _MoveTargetPosition;
                    Stop();
                    ArrangeNewMovementTarget(target);
                }

            }
        }
    }
    private void CheckNextPosition()
    {
        if (_isPathEnded) return;

        if (_nextPositionCheckCounter <= _nextPositionCheckThreshold)
        {
            _nextPositionCheckCounter += Time.deltaTime;
        }
        else
        {
            _nextPositionCheckCounter = 0f;

            //check for cliff
            bool isStopping;
            Vector3 directionInput = new Vector3(_Rigidbody.linearVelocity.x, 0f, _Rigidbody.linearVelocity.z);
            _cliffCheckForwardForVel = Mathf.Lerp(_cliffCheckForwardForVel, directionInput.magnitude < 0.1f ? 1f : (_IsSprinting ? 3.25f : 1.5f), Time.deltaTime * 2f);
            //Debug.DrawRay(transform.position + Vector3.up * 0.8f + directionInput.normalized * _cliffCheckForwardForVel, -Vector3.up);
            Physics.Raycast(transform.position + Vector3.up * 0.8f + directionInput.normalized * _cliffCheckForwardForVel, -Vector3.up, out RaycastHit hit, 8f, GameManager._Instance._TerrainSolidAndWaterMask);
            isStopping = hit.collider == null;

            directionInput = GameManager._Instance.Vector2ToVector3(_DirectionInput);
            _cliffCheckForwardForDir = Mathf.Lerp(_cliffCheckForwardForDir, directionInput.magnitude < 0.1f ? 1f : (_IsSprinting ? 3.25f : 1.5f), Time.deltaTime * 2f);
            //Debug.DrawRay(transform.position + Vector3.up * 0.8f + directionInput.normalized * _cliffCheckForwardForDir, -Vector3.up);
            Physics.Raycast(transform.position + Vector3.up * 0.8f + directionInput.normalized * _cliffCheckForwardForDir, -Vector3.up, out hit, 8f, GameManager._Instance._TerrainSolidAndWaterMask);
            isStopping = isStopping ? isStopping : hit.collider == null;

            if (isStopping)
            {
                Vector3 target = _MoveTargetPosition;
                Stop();
                ArrangeNewMovementTarget(target);
                return;
            }

            //check for obstacle
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, -Vector3.up, out hit, 1f, GameManager._Instance._TerrainAndSolidMask))
            {
                Vector3 rayDirection = new Vector3(_Rigidbody.linearVelocity.x, 0f, _Rigidbody.linearVelocity.z).normalized;
                if (hit.normal != Vector3.zero)
                {
                    rayDirection = Vector3.Cross(Vector3.Cross(hit.normal, rayDirection), hit.normal).normalized;
                }

                if (Physics.Raycast(transform.position + Vector3.up * 0.2f, rayDirection, out hit, Mathf.Clamp(_Rigidbody.linearVelocity.magnitude * 0.3f, 1f, float.MaxValue), GameManager._Instance._TerrainAndSolidMask))
                {
                    if (hit.collider != null && Vector3.Dot(hit.normal, Vector3.up) < 0.3f)
                    {
                        Physics.Raycast(transform.position + Vector3.up * 0.8f, rayDirection, out hit, Mathf.Clamp(_Rigidbody.linearVelocity.magnitude * 0.3f, 1f, float.MaxValue), GameManager._Instance._TerrainAndSolidMask);
                        if (hit.collider == null)
                            TryJump();
                    }
                }
            }
        }
    }
    private void TryJump()
    {
        if (_lastTimeJumped + 1f < Time.time)
        {
            _lastTimeJumped = Time.time;
            _JumpInput = true;
        }
    }

    private void ActivateCombatMode()
    {
        bool conditions = _MovementState is Locomotion;
        if (conditions)
            _IsInCombatMode = true;
    }
    private void DisableCombatMode()
    {
        bool conditions = _MovementState is Locomotion;
        if (conditions)
            _IsInCombatMode = false;
    }
    public void Stop()
    {
        if (_segmentatePathCoroutine != null)
            StopCoroutine(_segmentatePathCoroutine);
        _cornersFromPath.Clear();
        _MoveTargetPosition = transform.position;
        _LastCornerFromPath = transform.position;
        _directionInputFromPath = Vector2.zero;
        _directionCurrent = Vector2.zero;
        _isPathEnded = true;
    }
    public void ArrangeNewMovementTarget(Vector3 targetPos)
    {
        _MoveTargetPosition = targetPos;
        GameManager._Instance.CoroutineCall(ref _segmentatePathCoroutine, SegmentatePath(), this);
    }
    private IEnumerator SegmentatePath()
    {
        bool breakCoroutine = true;
        while (true)
        {
            if (_tryToCreatePathTimer < _tryToCreatePathThreshold)
            {
                _tryToCreatePathTimer += Time.deltaTime;
                yield return null;
                continue;
            }
            _tryToCreatePathTimer = 0f;

            if (!_isPathEnded) { yield return null; continue; }
            Vector3 dist = (_MoveTargetPosition - transform.position);
            if (new Vector3(dist.x, 0f, dist.z).magnitude < 1f) break;

            breakCoroutine = true;
            Vector3 segmentatedTarget = _MoveTargetPosition;
            if (new Vector3(dist.x, 0f, dist.z).magnitude > _segmentationMagnitude)
            {
                breakCoroutine = false;
                segmentatedTarget = transform.position + new Vector3((_MoveTargetPosition - transform.position).x, transform.position.y, (_MoveTargetPosition - transform.position).z).normalized * _segmentationMagnitude;
                //segmentatedTarget = GetWalkableTerrainPosition(segmentatedTarget);
            }
            _currentPath = new NavMeshPath();
            NavMesh.CalculatePath(transform.position, segmentatedTarget, NavMesh.AllAreas, _currentPath);
            _isPathEnded = false;
            _cornersFromPath.Clear();
            _nextPositionCheckCounter = 1f;
            foreach (var corner in _currentPath.corners)
            {
                _cornersFromPath.Add(corner);
            }
            if (_cornersFromPath.Count > 1)
            {
                _LastCornerFromPath = _cornersFromPath[1];
                _directionInputFromPath = new Vector2(_cornersFromPath[1].x - transform.position.x, _cornersFromPath[1].z - transform.position.z).normalized;
                _cornersFromPath.RemoveAt(1);
                _cornersFromPath.RemoveAt(0);
            }
            else
            {
                _cornersFromPath.Clear();
                _directionInputFromPath = Vector2.zero;
            }
            if (breakCoroutine)
            {
                yield break;
            }

            yield return null;
        }

    }
    /*
    private Vector3 GetWalkableTerrainPosition(Vector3 segmentatedPos)
    {
        Vector3 rayPos = segmentatedPos;
        float radiusStep = 2f;
        int angleStep = 30;
        float radius, angle;
        for (int i = 0; i < 20; i++)
        {
            if (Physics.Raycast(rayPos + Vector3.up * 1000f, -Vector3.up, out RaycastHit hit, 1200f, GameManager._Instance._TerrainAndSolidMask))
            {
                if (hit.collider == null) continue;

                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain"))
                {
                    segmentatedPos.y = hit.point.y;
                    return segmentatedPos;
                }
            }
            radius = (i / (360 / angleStep)) * radiusStep;
            angle = (i % (360 / angleStep)) * angleStep * Mathf.Deg2Rad;
            rayPos = segmentatedPos + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        }

        Debug.LogError("Segmentation Ray Did Not Hit!");
        return segmentatedPos;
    }*/
    public void ArrangeMovementCorners()
    {
        if (_isPathEnded) return;

        _checkCornerDist = _LastCornerFromPath - transform.position;
        _checkCornerDist.y = 0f;

        if (_checkCornerDist.magnitude < (_IsSprinting ? 1f : 0.35f))
        {
            if (_cornersFromPath.Count > 0)
            {
                _LastCornerFromPath = _cornersFromPath[0];
                _directionInputFromPath = new Vector2(_cornersFromPath[0].x - transform.position.x, _cornersFromPath[0].z - transform.position.z).normalized;
                _cornersFromPath.RemoveAt(0);
            }
            else
            {
                Stop();
            }

        }
    }
}

