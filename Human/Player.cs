using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Humanoid
{
    public PlayerInputController _PlayerInputController { get; protected set; }

    public Transform _LookAtForCam;

    #region Inputs

    public override Vector2 _DirectionInput => new Vector2(_HorizontalInput, _VerticalInput).normalized;

    public float _HorizontalInput { get; set; }
    public float _VerticalInput { get; set; }
    public bool _CameraAngleInput { get; set; }
    public Vector2 _LastLookVectorForGamepad { get; set; }

    public bool _JumpBuffer;
    public bool _AttackBuffer;

    public Coroutine _JumpCoroutine;
    public Coroutine _AttackCoroutine;

    public float _LastJumpedTime { get; set; }
    public Vector3 _LastJumpedPosition { get; set; }
    #endregion


    protected override void Awake()
    {
        //_IsMale = true;/////////
        _Rigidbody = GetComponent<Rigidbody>();
        //_MainCollider = transform.Find("MainCollider").GetComponent<CapsuleCollider>();
        _MainCollider = transform.Find("char").Find("Root").Find("Global").Find("Position").Find("Hips").Find("MainCollider").GetComponent<CapsuleCollider>();
        base.Awake();
        _PlayerInputController = new PlayerInputController(this);
        _LastJumpedTime = -2f;
    }

    protected override void Start()
    {
        base.Start();
        DisableHumanData();
        EnableHumanData();
        _VerticalInput = 1f;
    }
    protected override void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;
        base.Update();
        _PlayerInputController.ArrangeInput(_LocomotionSystem);
    }

}

public class PlayerInputController
{
    private Player _player;
    private float _quitCombatModeCounter;
    public PlayerInputController(Player player)
    {
        _player = player;
    }

    public void ArrangeInput(LocomotionSystem locomotionSystem)
    {
        if (M_Input.GetButtonDown("Interact"))
        {
            CheckForNearInventories();
        }
        if (M_Input.GetButtonDown("Jump"))
        {
            GameManager._Instance.CoroutineCall(ref _player._JumpCoroutine, JumpBufferCoroutine(), _player);
        }
        if (M_Input.GetButtonDown("Fire1"))
        {
            GameManager._Instance.CoroutineCall(ref _player._AttackCoroutine, AttackBufferCoroutine(), _player);
        }
        if (M_Input.GetButton("CombatMode"))
        {
            if (_player._MovementState is Locomotion)
            {
                if (_quitCombatModeCounter >= 0f)
                    _quitCombatModeCounter += Time.deltaTime;
                if (_quitCombatModeCounter > 0.4f)
                {
                    _player._IsInCombatMode = !_player._IsInCombatMode;
                    _quitCombatModeCounter = -1f;
                }
            }
        }
        else
        {
            _quitCombatModeCounter = 0f;
        }

        _player._HorizontalInput = M_Input.GetAxis("Horizontal");
        _player._VerticalInput = M_Input.GetAxis("Vertical");
        _player._RunInput = M_Input.GetButton("Run");
        _player._SprintInput = M_Input.GetButton("Sprint");
        _player._CrouchInput = M_Input.GetButtonDown("Crouch");
        _player._JumpInput = _player._JumpBuffer;
        _player._AttackInput = _player._AttackBuffer;
        _player._InteractInput = M_Input.GetButtonDown("Interact");
        _player._CameraAngleInput = M_Input.GetButton("CameraAngle");

        /*if (Input.GetButtonDown("Map"))
        {
            //stop the game and open the map
        }*/
    }

    public IEnumerator JumpBufferCoroutine()
    {
        if (_player._JumpCoroutine != null)
            _player.StopCoroutine(_player._JumpCoroutine);
        _player._JumpBuffer = true;
        yield return new WaitForSeconds(0.3f);
        _player._JumpBuffer = false;
    }
    public IEnumerator AttackBufferCoroutine()
    {
        if (_player._AttackCoroutine != null)
            _player.StopCoroutine(_player._AttackCoroutine);
        _player._AttackBuffer = true;
        yield return new WaitForSeconds(0.225f);
        _player._AttackBuffer = false;
    }


    private void CheckForNearInventories()
    {
        Inventory nearestInventory = null;
        float nearestDistance = 0f;
        Collider[] colliders = Physics.OverlapSphere(_player.transform.position, 1f);
        foreach (var collider in colliders)
        {
            if (collider != null && collider.transform.parent != null && collider.transform.parent.TryGetComponent(out Inventory anotherInventory))
            {
                if (nearestInventory == null || nearestDistance > (collider.transform.position - _player.transform.position).magnitude)
                {
                    nearestDistance = (collider.transform.position - _player.transform.position).magnitude;
                    nearestInventory = anotherInventory;
                }
            }
        }

        if (nearestInventory != null)
            GameManager._Instance.OpenAnotherInventory(nearestInventory);
    }

}

