using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    public Humanoid _Human { get; }
    void DoState();
}

public interface MovementState : IState
{
    void EnterState(MovementState oldState);

    void ExitState(MovementState newState);
    void FixedUpdate();
}

#region MovementStates
/// <summary>
/// Contains Idle, Walk, Run, Sprint, Jump
/// </summary>

public class Locomotion : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    private float _stateChangeCounter;
    public Locomotion(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        float animChangeTime = (oldState is Swim) ? 0.65f : 0.2f;
        _stateChangeCounter = 0.1f;
        _human._LocomotionSystem._defaultCollider.enabled = true;
        _human.ChangeAnimation("Free Locomotion", animChangeTime);
        _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 1f;
        if (_human._Animator.avatar != null && Options._Instance._IsFootIKEnabled)
            _human._FootIKComponent.enabled = true;
    }

    public void ExitState(MovementState newState)
    {
        _human._LocomotionSystem._defaultCollider.enabled = false;
        _human._IsInCombatMode = false;
    }
    public void DoState()
    {
        //Check For State Change
        if (MovementStateMethods.IsInDeepWater(_human))
        {
            _human.EnterState(new Swim(_human));
            return;
        }

        if (_stateChangeCounter > 0f)
            _stateChangeCounter -= Time.deltaTime;
        if (_stateChangeCounter <= 0f)
        {
            if (_human._CrouchInput && !_human._IsJumping && _human._IsGrounded && !_Human._IsInCombatMode)
            {
                _human.EnterState(new Crouch(_human));
                return;
            }
        }

        if (!_human._FootIKComponent.enabled && _human._Animator.avatar != null && _human._IsGrounded && Options._Instance._IsFootIKEnabled)
            _human._FootIKComponent.enabled = true;
        else if ((!Options._Instance._IsFootIKEnabled || _human._Animator.avatar == null || !_human._IsGrounded) && _human._FootIKComponent.enabled)
            _human._FootIKComponent.enabled = false;


        MovementStateMethods.UpdateMoveDirection(_human, GameManager._Instance._MainCamera.transform);
        MovementStateMethods.CheckJump(_human);
        MovementStateMethods.CheckSprint(_human);
        MovementStateMethods.UpdateAnimator(_human);
    }
    public void FixedUpdate()
    {
        MovementStateMethods.CheckGround(_human);
        MovementStateMethods.CheckSlopeLimit(_human);
        MovementStateMethods.ControlJumpBehaviour(_human);
        MovementStateMethods.AirControl(_human);
        MovementStateMethods.ControlLocomotionType(_human);     // handle the controller locomotion type and movespeed
        MovementStateMethods.ControlRotationType(_human);       // handle the controller rotation type*/
    }
}

public class Crouch : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;

    private float _stateChangeCounter;
    private float _waitForMoveAfterProne;
    public Crouch(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        _stateChangeCounter = 0.1f;
        _human._LocomotionSystem._crouchCollider.SetActive(true);
        _human._IsSprinting = false;

        if (oldState is Prone)
            _human.ChangeAnimation("ProneToCrouch");
        else
            _human.ChangeAnimation("Crouch");

        if (oldState is Prone)
            _waitForMoveAfterProne = 0.75f;
        else
            _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0.9f;

        if (_human._Animator.avatar != null && Options._Instance._IsFootIKEnabled)
            _human._FootIKComponent.enabled = true;
    }

    public void ExitState(MovementState newState)
    {
        _human._LocomotionSystem._crouchCollider.SetActive(false);
    }
    public void DoState()
    {
        if (_waitForMoveAfterProne > 0f)
        {
            _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0f;
            _waitForMoveAfterProne -= Time.deltaTime;
            if (_waitForMoveAfterProne <= 0f)
                _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0.9f;
        }
        //Check For State Change
        if (MovementStateMethods.IsInDeepWater(_human))
        {
            _human.EnterState(new Locomotion(_human));
            return;
        }

        if (_stateChangeCounter > 0f)
            _stateChangeCounter -= Time.deltaTime;
        if (_stateChangeCounter <= 0f)
        {
            if ((_human._JumpInput && _waitForMoveAfterProne <= 0f) || !_human._IsGrounded)
            {
                if (_human is Player)
                    GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._JumpBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._JumpCoroutine);
                _human.EnterState(new Locomotion(_human));
                return;
            }
            else if (_human._CrouchInput && !_Human._IsInCombatMode)
            {
                _human.EnterState(new Prone(_human));
                return;
            }
        }


        if (!_human._FootIKComponent.enabled && _human._Animator.avatar != null && _human._IsGrounded && Options._Instance._IsFootIKEnabled)
            _human._FootIKComponent.enabled = true;
        else if ((!Options._Instance._IsFootIKEnabled || _human._Animator.avatar == null || !_human._IsGrounded) && _human._FootIKComponent.enabled)
            _human._FootIKComponent.enabled = false;

        if (_stateChangeCounter > 0f)
            _human._Rigidbody.linearVelocity = Vector3.Lerp(_human._Rigidbody.linearVelocity, Vector3.zero, Time.deltaTime * 2f);
        else
            MovementStateMethods.UpdateMoveDirection(_human, GameManager._Instance._MainCamera.transform);

        MovementStateMethods.UpdateAnimator(_human);

    }
    public void FixedUpdate()
    {
        MovementStateMethods.CheckGround(_human);
        MovementStateMethods.CheckSlopeLimit(_human);
        if (_stateChangeCounter <= 0f)
            MovementStateMethods.ControlLocomotionType(_human);     // handle the controller locomotion type and movespeed
        MovementStateMethods.ControlRotationType(_human);       // handle the controller rotation type*/
    }
}
public class Prone : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;

    private float _stateChangeCounter;
    public Prone(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        _stateChangeCounter = 0.1f;
        _human._LocomotionSystem._proneCollider.SetActive(true);
        _human._IsSprinting = false;

        if (oldState is Crouch)
            _human.ChangeAnimation("CrouchToProne");
        else
            _human.ChangeAnimation("Prone");

        _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0.5f;
        _human._FootIKComponent.enabled = false;
    }

    public void ExitState(MovementState newState)
    {
        _human._LocomotionSystem._proneCollider.SetActive(false);
    }
    public void DoState()
    {
        //Check For State Change
        if (MovementStateMethods.IsInDeepWater(_human))
        {
            _human.EnterState(new Locomotion(_human));
            return;
        }

        if (_stateChangeCounter > 0f)
            _stateChangeCounter -= Time.deltaTime;
        if (_stateChangeCounter <= 0f)
        {
            if (_human._JumpInput || !_human._IsGrounded)
            {
                if (_human is Player)
                    GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._JumpBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._JumpCoroutine);
                _human.EnterState(new Crouch(_human));
                return;
            }
        }

        if (_stateChangeCounter > 0f)
            _human._Rigidbody.linearVelocity = Vector3.Lerp(_human._Rigidbody.linearVelocity, Vector3.zero, Time.deltaTime * 2f);
        else
            MovementStateMethods.UpdateMoveDirection(_human, GameManager._Instance._MainCamera.transform);

        MovementStateMethods.UpdateAnimator(_human);
    }
    public void FixedUpdate()
    {
        MovementStateMethods.CheckGround(_human);
        MovementStateMethods.CheckSlopeLimit(_human);
        if (_stateChangeCounter <= 0f)
            MovementStateMethods.ControlLocomotionType(_human);     // handle the controller locomotion type and movespeed
        MovementStateMethods.ControlRotationType(_human);       // handle the controller rotation type*/
    }
}
public class Swim : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    private float _stateChangeCounter;
    private bool _isSwimAnimForward;
    public Swim(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        _human._Rigidbody.useGravity = false;
        _stateChangeCounter = 0.1f;
        _human._LocomotionSystem._defaultCollider.enabled = true;
        _human.ChangeAnimation("Swim_Idle");
        _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0.8f;
        _human._FootIKComponent.enabled = false;
        _human._IsJumping = false;
    }

    public void ExitState(MovementState newState)
    {
        _human._Rigidbody.useGravity = true;
        _human._LocomotionSystem._defaultCollider.enabled = false;
    }
    public void DoState()
    {
        //Check For State Change
        if (!MovementStateMethods.IsInDeepWater(_human))
        {
            _human.EnterState(new Locomotion(_human));
            return;
        }

        if (_human._DirectionInput.magnitude < 0.2f && _isSwimAnimForward)
        {
            _human.ChangeAnimation("Swim_Idle", 0.4f);
            _isSwimAnimForward = false;
        }
        else if (_human._DirectionInput.magnitude > 0.2f && !_isSwimAnimForward)
        {
            _human.ChangeAnimation("Swim_Forward", 0.4f);
            _isSwimAnimForward = true;
        }
        MovementStateMethods.UpdateMoveDirection(_human, GameManager._Instance._MainCamera.transform);
        MovementStateMethods.CheckSprint(_human);
        MovementStateMethods.UpdateAnimator(_human);
    }
    public void FixedUpdate()
    {
        _human._IsGrounded = true;
        float targetYVel = (WorldHandler._Instance._SeaLevel - 1.5f - _human.transform.position.y) * 7.5f;
        _human._Rigidbody.linearVelocity = new Vector3(_human._Rigidbody.linearVelocity.x, Mathf.Lerp(_human._Rigidbody.linearVelocity.y, targetYVel, Time.deltaTime * 3f), _human._Rigidbody.linearVelocity.z);
        MovementStateMethods.ControlLocomotionType(_human);     // handle the controller locomotion type and movespeed
        MovementStateMethods.ControlRotationType(_human, 4f);       // handle the controller rotation type*/
    }
}

public class Sit : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public Sit(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        _human._FootIKComponent.enabled = false;
    }

    public void ExitState(MovementState newState)
    {

    }
    public void DoState()
    {

    }
    public void FixedUpdate()
    {

    }
}
public class Rest : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public Rest(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        _human._FootIKComponent.enabled = false;
    }

    public void ExitState(MovementState newState)
    {

    }
    public void DoState()
    {

    }
    public void FixedUpdate()
    {

    }
}
#endregion

public interface HandState : IState
{
    void EnterState(HandState oldState);

    void ExitState(HandState newState);

}

#region HandStates
public class EmptyHandsState : HandState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public EmptyHandsState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(HandState oldState)
    {
        _human._LocomotionSystem.MovementSpeedMultiplierHandState = 1f;
        _human.ChangeAnimation("EmptyHands");
    }

    public void ExitState(HandState newState)
    {

    }
    public void DoState()
    {
        //Check For State Change
        if (HandStateMethods.CheckForCarryState(_human))
        {
            _human.EnterState(new CarryHandState(_human));
            return;
        }
        if (HandStateMethods.CheckForWeaponState(_human))
        {
            _human.EnterState(new WeaponHandState(_human));
            return;
        }

        if (_human is Player)
            HandStateMethods.ArrangeLookAtForCamPosition(_human as Player);
    }
}
public class CarryHandState : HandState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public CarryHandState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(HandState oldState)
    {
        _human._LocomotionSystem.MovementSpeedMultiplierHandState = 0.35f;//arrange with str and current tired&stamina value
        _human.ChangeAnimation("CarryHands");
    }

    public void ExitState(HandState newState)
    {

    }
    public void DoState()
    {
        //Check For State Change
        if (HandStateMethods.CheckForEmptyState(_human))
        {
            _human.EnterState(new EmptyHandsState(_human));
            return;
        }
        if (HandStateMethods.CheckForWeaponState(_human))
        {
            _human.EnterState(new WeaponHandState(_human));
            return;
        }


        if (_human is Player)
            HandStateMethods.ArrangeLookAtForCamPosition(_human as Player);
    }
}

public class WeaponHandState : HandState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public WeaponHandState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(HandState oldState)
    {
        _human._LocomotionSystem.MovementSpeedMultiplierHandState = 0.8f;
        _human.ChangeAnimation("WeaponHands");
    }

    public void ExitState(HandState newState)
    {

    }
    public void DoState()
    {
        //Check For State Change
        if (HandStateMethods.CheckForCarryState(_human))
        {
            _human.EnterState(new CarryHandState(_human));
            return;
        }
        if (HandStateMethods.CheckForEmptyState(_human))
        {
            _human.EnterState(new EmptyHandsState(_human));
            return;
        }

        if (_human is Player)
            HandStateMethods.ArrangeLookAtForCamPosition(_human as Player);
    }
}

#endregion
