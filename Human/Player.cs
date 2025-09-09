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
    public bool _CameraZoomInput { get; set; }

    public bool _JumpBuffer;
    public bool _AttackBuffer;

    public Coroutine _JumpCoroutine;
    public Coroutine _AttackCoroutine;
    #endregion


    protected override void Awake()
    {
        base.Awake();
        _IsInCombatMode = false;
        _PlayerInputController = new PlayerInputController(this);
    }
    protected virtual void Start()
    {
        base.Start();
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
    public PlayerInputController(Player player)
    {
        _player = player;
    }

    public void ArrangeInput(LocomotionSystem locomotionSystem)
    {
        if (Input.GetButtonDown("Jump"))
        {
            GameManager._Instance.CoroutineCall(ref _player._JumpCoroutine, JumpBufferCoroutine(), _player);
        }
        if (Input.GetMouseButtonDown(0))
        {
            GameManager._Instance.CoroutineCall(ref _player._AttackCoroutine, AttackBufferCoroutine(), _player);
        }
        if (Input.GetButtonDown("CombatMode"))
        {
            if (_player._MovementState is Locomotion)
                _player._IsInCombatMode = !_player._IsInCombatMode;
        }

        _player._HorizontalInput = Input.GetAxis("Horizontal");
        _player._VerticalInput = Input.GetAxis("Vertical");
        _player._RunInput = Input.GetButton("Run");
        _player._SprintInput = Input.GetButton("Sprint");
        _player._CrouchInput = Input.GetButtonDown("Crouch");
        _player._JumpInput = _player._JumpBuffer;
        _player._InteractInput = Input.GetButtonDown("Interact");
        _player._CameraAngleInput = Input.GetButton("CameraAngle");
        _player._CameraZoomInput = Input.mouseScrollDelta.y != 0f;

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

}

