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
    public bool _AttackReadyBuffer;
    public bool _SelectAttackBuffer;
    public bool _KickBuffer;
    public bool _DodgeBuffer;

    public Coroutine _JumpCoroutine;
    public Coroutine _AttackReadyCoroutine;
    public Coroutine _SelectAttackCoroutine;
    public Coroutine _KickCoroutine;
    public Coroutine _DodgeCoroutine;
    public float _LastJumpedTime { get; set; }
    public Vector3 _LastJumpedPosition { get; set; }

    #endregion


    protected override void Awake()
    {
        //_IsMale = true;/////////
        _Rigidbody = GetComponent<Rigidbody>();
        //_MainCollider = transform.Find("MainCollider").GetComponent<CapsuleCollider>();
        _MainCollider = transform.Find("char").Find("MainCollider").GetComponent<CapsuleCollider>();
        base.Awake();
        _PlayerInputController = new PlayerInputController(this);
        _LastJumpedTime = -2f;
    }

    protected override void Start()
    {
        base.Start();
        DisableHumanData();
        EnableHumanData();
        _VerticalInput = -1f;
    }
    protected override void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;
        base.Update();
        _PlayerInputController.ArrangeInput(_LocomotionSystem);
        GameManager._Instance.UpdateInGameUI(_Stamina / _MaxStamina);
    }
}

public class PlayerInputController
{
    private Player _player;
    private float _quitCombatModeCounter;
    private bool _attackHappenedForInput;
    private float _attackReadyTime;
    private float _lastAttackReadyTime;
    private float _runInputCounter;
    private float _lastTimeRunInputPressed;

    public PlayerInputController(Player player)
    {
        _player = player;
        _attackHappenedForInput = true;
        _lastTimeRunInputPressed = -1f;
    }

    protected void SelectHeavyOrLightAttack()
    {
        if (_player._AttackReadyTime < _player._HeavyAttackThreshold)
            _player._LightAttackInput = true;
        else
            _player._HeavyAttackInput = true;
    }
    public void ArrangeInput(LocomotionSystem locomotionSystem)
    {
        if (M_Input.GetButtonDown("Fire1") && _attackHappenedForInput)
        {
            _lastAttackReadyTime = Time.time;
            _attackReadyTime = 0f;
            _attackHappenedForInput = false;
            GameManager._Instance.CoroutineCall(ref _player._AttackReadyCoroutine, AttackReadyBufferCoroutine(), _player);
        }
        else if (M_Input.GetButton("Fire1") && !_attackHappenedForInput)
        {
            _attackReadyTime += Time.deltaTime;
            if (_attackReadyTime > 1f)
            {
                _attackHappenedForInput = true;
                GameManager._Instance.CoroutineCall(ref _player._SelectAttackCoroutine, SelectAttackCoroutine(), _player);
            }
        }
        else if (!_attackHappenedForInput)
        {
            _attackHappenedForInput = true;
            GameManager._Instance.CoroutineCall(ref _player._SelectAttackCoroutine, SelectAttackCoroutine(), _player);
        }

        if (_player._AttackReadyBuffer && HandStateMethods.IsAttackPossible(_player))
        {
            GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._AttackReadyBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._AttackReadyCoroutine);
            HandStateMethods.ArrangeIsAttackingFromLeftHand(_player);
            HandStateMethods.ReadyAttackAnimation(_player, HandStateMethods.GetCurrentWeaponType(_player, _player._IsAttackingFromLeftHandWeapon));
        }
        else if (_player._SelectAttackBuffer && HandStateMethods.IsAttackPossible(_player) && _player._IsInAttackReady && _attackReadyTime <= (Time.time - _lastAttackReadyTime))
        {
            GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._SelectAttackBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._SelectAttackCoroutine);
            _player._AttackReadyTime = _attackReadyTime;
            SelectHeavyOrLightAttack();
        }

        if (M_Input.GetButtonDown("Interact"))
        {
            _player.CheckForNearInventories(true);
        }
        if (M_Input.GetButtonDown("Jump"))
        {
            GameManager._Instance.CoroutineCall(ref _player._JumpCoroutine, JumpBufferCoroutine(), _player);
        }
        if (M_Input.GetButtonDown("Kick"))
        {
            GameManager._Instance.CoroutineCall(ref _player._KickCoroutine, KickBufferCoroutine(), _player);
        }
        if (M_Input.GetButtonDown("Dodge"))
        {
            GameManager._Instance.CoroutineCall(ref _player._DodgeCoroutine, DodgeBufferCoroutine(), _player);
        }
        if (M_Input.GetButton("CombatMode"))
        {
            if (_player._MovementState is LocomotionState)
            {
                if (_quitCombatModeCounter >= 0f)
                    _quitCombatModeCounter += Time.deltaTime;
                if (_quitCombatModeCounter > 0.4f)
                {
                    if (_player._IsInCombatMode)
                        _player.DisableCombatMode();
                    else
                        _player.ActivateCombatMode();
                    _quitCombatModeCounter = -1f;
                }
            }
        }
        else
        {
            _quitCombatModeCounter = 0f;
        }

        if (M_Input.GetButtonDown("Run") && !_player._IsInCombatMode)
        {
            if (_lastTimeRunInputPressed + 0.4f > Time.time)
                _player._IsInFastWalkMode = !_player._IsInFastWalkMode;
            _lastTimeRunInputPressed = Time.time;
        }

        if (M_Input.GetButton("Run"))
            _runInputCounter += Time.deltaTime;
        else
            _runInputCounter = 0f;


        _player._SprintInput = _runInputCounter > 0.25f;
        _player._HorizontalInput = M_Input.GetAxis("Horizontal");
        _player._VerticalInput = M_Input.GetAxis("Vertical");
        _player._CrouchInput = M_Input.GetButtonDown("Crouch");
        _player._JumpInput = _player._JumpBuffer;
        //_player._LightAttackInput = _player._LightAttackBuffer;
        //_player._HeavyAttackInput = _player._HeavyAttackBuffer;
        _player._KickInput = _player._KickBuffer;
        _player._DodgeInput = _player._DodgeBuffer;
        _player._BlockInput = M_Input.GetButton("Fire2");
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
    public IEnumerator AttackReadyBufferCoroutine()
    {
        if (_player._AttackReadyCoroutine != null)
            _player.StopCoroutine(_player._AttackReadyCoroutine);
        _player._AttackReadyBuffer = true;
        yield return new WaitForSeconds(1.4f);
        _player._AttackReadyBuffer = false;
    }
    public IEnumerator SelectAttackCoroutine()
    {
        if (_player._SelectAttackCoroutine != null)
            _player.StopCoroutine(_player._SelectAttackCoroutine);
        _player._SelectAttackBuffer = true;
        yield return new WaitForSeconds(1.4f);
        _player._SelectAttackBuffer = false;
    }
    public IEnumerator KickBufferCoroutine()
    {
        if (_player._KickCoroutine != null)
            _player.StopCoroutine(_player._KickCoroutine);
        _player._KickBuffer = true;
        yield return new WaitForSeconds(0.225f);
        _player._KickBuffer = false;
    }
    public IEnumerator DodgeBufferCoroutine()
    {
        if (_player._DodgeCoroutine != null)
            _player.StopCoroutine(_player._DodgeCoroutine);
        _player._DodgeBuffer = true;
        yield return new WaitForSeconds(0.225f);
        _player._DodgeBuffer = false;
    }

}

