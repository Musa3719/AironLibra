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
        _stateChangeCounter = 0.1f;
        _human._LocomotionSystem._defaultCollider.enabled = true;
        _human.ChangeAnimation("Free Locomotion");
        _human._LocomotionSystem.MovementSpeedMultiplier = 1f;
    }

    public void ExitState(MovementState newState)
    {
        _human._LocomotionSystem._defaultCollider.enabled = false;
    }
    public void DoState()
    {
        //Check For State Change
        if (_stateChangeCounter > 0f)
            _stateChangeCounter -= Time.deltaTime;
        if (_stateChangeCounter <= 0f)
        {
            if (_human._CrouchInput && !_Human._IsInCombatMode)
            {
                _human.EnterState(new Crouch(_human));
            }
        }


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
            _human._LocomotionSystem.MovementSpeedMultiplier = 0.9f;
    }

    public void ExitState(MovementState newState)
    {
        _human._LocomotionSystem._crouchCollider.SetActive(false);
    }
    public void DoState()
    {
        if (_waitForMoveAfterProne > 0f)
        {
            _human._LocomotionSystem.MovementSpeedMultiplier = 0f;
            _waitForMoveAfterProne -= Time.deltaTime;
            if (_waitForMoveAfterProne <= 0f)
                _human._LocomotionSystem.MovementSpeedMultiplier = 0.9f;
        }

        //Check For State Change
        if (_stateChangeCounter > 0f)
            _stateChangeCounter -= Time.deltaTime;
        if (_stateChangeCounter <= 0f)
        {
            if (_human._JumpInput || !_human._IsGrounded)
            {
                if (_human is Player)
                    GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._JumpBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._JumpCoroutine);
                _human.EnterState(new Locomotion(_human));
            }
            else if (_human._CrouchInput && !_Human._IsInCombatMode)
            {
                _human.EnterState(new Prone(_human));
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

        _human._LocomotionSystem.MovementSpeedMultiplier = 0.5f;
    }

    public void ExitState(MovementState newState)
    {
        _human._LocomotionSystem._defaultCollider.enabled = true;
        _human._LocomotionSystem._proneCollider.SetActive(false);
    }
    public void DoState()
    {
        //Check For State Change
        if (_stateChangeCounter > 0f)
            _stateChangeCounter -= Time.deltaTime;
        if (_stateChangeCounter <= 0f)
        {
            if (_human._JumpInput || !_human._IsGrounded)
            {
                if (_human is Player)
                    GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._JumpBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._JumpCoroutine);
                _human.EnterState(new Crouch(_human));
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
public class Empty : HandState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public Empty(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(HandState oldState)
    {

    }

    public void ExitState(HandState newState)
    {

    }
    public void DoState()
    {
        HandStateMethods.ArrangeAimRotation(_human);
    }
}
public class CarryBigHandState : HandState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public CarryBigHandState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(HandState oldState)
    {

    }

    public void ExitState(HandState newState)
    {

    }
    public void DoState()
    {
        HandStateMethods.ArrangeAimRotation(_human);
    }
}
public class ToolHandState : HandState
{
    public Humanoid _Human => _human;
    private Humanoid _human;

    private bool _isUsingTool;

    public ToolHandState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(HandState oldState)
    {

    }

    public void ExitState(HandState newState)
    {

    }
    public void DoState()
    {
        if (!_isUsingTool)
            HandStateMethods.ArrangeAimRotation(_human);
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

    }

    public void ExitState(HandState newState)
    {

    }
    public void DoState()
    {
        HandStateMethods.ArrangeAimRotation(_human);
    }
}

#endregion
