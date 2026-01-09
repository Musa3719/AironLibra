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

public class LocomotionState : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    private float _stateChangeCounter;
    public LocomotionState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        float animChangeTime = (oldState is SwimMoveState) ? 0.65f : 0.2f;
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
        _human.DisableCombatMode();
    }
    public void DoState()
    {
        if (_human._HealthSystem._IsUnconscious)
        {
            _human.EnterState(new UnconsciousMoveState(_human, _human._HealthSystem._LastHitForce, _human._HealthSystem._LastHitBoneName, _human._HealthSystem._LastHitDir));
            return;
        }

        //Check For State Change
        if (MovementStateMethods.IsInDeepWater(_human))
        {
            _human.EnterState(new SwimMoveState(_human));
            return;
        }

        if (_stateChangeCounter > 0f)
            _stateChangeCounter -= Time.deltaTime;
        if (_stateChangeCounter <= 0f)
        {
            if (_human._CrouchInput && !_human._IsJumping && _human._IsGrounded && !_Human._IsInCombatMode)
            {
                _human.EnterState(new CrouchMoveState(_human));
                return;
            }
        }

        if (!_human._FootIKComponent.enabled && _human._Animator.avatar != null && Options._Instance._IsFootIKEnabled)
            _human._FootIKComponent.enabled = true;
        else if ((!Options._Instance._IsFootIKEnabled || _human._Animator.avatar == null) && _human._FootIKComponent.enabled)
            _human._FootIKComponent.enabled = false;


        MovementStateMethods.UpdateMoveDirection(_human, GameManager._Instance._MainCamera.transform);
        MovementStateMethods.CheckDodge(_human);
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

public class CrouchMoveState : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;

    private float _stateChangeCounter;
    private float _waitForMoveAfterProne;
    public CrouchMoveState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        _stateChangeCounter = 0.1f;
        _human._LocomotionSystem._crouchCollider.SetActive(true);
        _human._IsSprinting = false;


        if (oldState is ProneMoveState)
            _waitForMoveAfterProne = 0.75f;
        else
        {
            _human.ChangeAnimation("Crouch");
            _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0.9f;
        }

        if (_human._Animator.avatar != null && Options._Instance._IsFootIKEnabled)
            _human._FootIKComponent.enabled = true;
    }

    public void ExitState(MovementState newState)
    {
        _human.ChangeAnimation("EmptyArms");
        _human._LocomotionSystem._crouchCollider.SetActive(false);
    }
    public void DoState()
    {
        if (_human._HealthSystem._IsUnconscious)
        {
            _human.EnterState(new UnconsciousMoveState(_human, _human._HealthSystem._LastHitForce, _human._HealthSystem._LastHitBoneName, _human._HealthSystem._LastHitDir));
            return;
        }

        if (_waitForMoveAfterProne > 0f)
        {
            _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0f;
            _waitForMoveAfterProne -= Time.deltaTime;
            if (_waitForMoveAfterProne <= 0f)
            {
                _human.ChangeAnimation("Crouch");
                _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0.9f;
            }
        }
        //Check For State Change
        if (MovementStateMethods.IsInDeepWater(_human))
        {
            _human.EnterState(new LocomotionState(_human));
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
                _human.EnterState(new LocomotionState(_human));
                return;
            }
            else if (_human._CrouchInput && !_Human._IsInCombatMode)
            {
                _human.EnterState(new ProneMoveState(_human));
                return;
            }
        }


        if (!_human._FootIKComponent.enabled && _human._Animator.avatar != null && Options._Instance._IsFootIKEnabled)
            _human._FootIKComponent.enabled = true;
        else if ((!Options._Instance._IsFootIKEnabled || _human._Animator.avatar == null) && _human._FootIKComponent.enabled)
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
public class ProneMoveState : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;

    private float _stateChangeCounter;
    public ProneMoveState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        _stateChangeCounter = 0.1f;
        _human._LocomotionSystem._proneCollider.SetActive(true);
        _human._IsSprinting = false;

        if (oldState is CrouchMoveState)
            _human.ChangeAnimation("CrouchToProne");
        else
            _human.ChangeAnimation("Prone");

        _human._LocomotionSystem.MovementSpeedMultiplierMoveState = 0.8f;
        _human._FootIKComponent.enabled = false;
    }

    public void ExitState(MovementState newState)
    {
        _human._LocomotionSystem._proneCollider.SetActive(false);
        _human.ChangeAnimation("ProneToCrouch");
    }
    public void DoState()
    {
        if (_human._HealthSystem._IsUnconscious)
        {
            _human.EnterState(new UnconsciousMoveState(_human, _human._HealthSystem._LastHitForce, _human._HealthSystem._LastHitBoneName, _human._HealthSystem._LastHitDir));
            return;
        }

        //Check For State Change
        if (MovementStateMethods.IsInDeepWater(_human))
        {
            _human.EnterState(new LocomotionState(_human));
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
                _human.EnterState(new CrouchMoveState(_human));
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
public class SwimMoveState : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    private bool _isSwimAnimForward;
    public SwimMoveState(Humanoid human)
    {
        this._human = human;
    }
    public void EnterState(MovementState oldState)
    {
        _human._Rigidbody.useGravity = false;
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
        if (_human._HealthSystem._IsUnconscious)
        {
            _human.EnterState(new UnconsciousMoveState(_human, _human._HealthSystem._LastHitForce, _human._HealthSystem._LastHitBoneName, _human._HealthSystem._LastHitDir));
            return;
        }

        //Check For State Change
        if (!MovementStateMethods.IsInDeepWater(_human))
        {
            _human.EnterState(new LocomotionState(_human));
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
        MovementStateMethods.ControlLocomotionType(_human);     // handle the controller locomotion type and movespeed
        MovementStateMethods.ControlRotationType(_human, 4f);       // handle the controller rotation type*/

        float targetYVel = (WorldHandler._Instance._SeaLevel - 1.5f - _human.transform.position.y) * 7.5f;
        _human._Rigidbody.linearVelocity = new Vector3(_human._Rigidbody.linearVelocity.x, Mathf.Lerp(_human._Rigidbody.linearVelocity.y, targetYVel, Time.deltaTime * 3f), _human._Rigidbody.linearVelocity.z);
    }
}

public class SitMoveState : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public SitMoveState(Humanoid human)
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
        if (_human._HealthSystem._IsUnconscious)
        {
            _human.EnterState(new UnconsciousMoveState(_human, _human._HealthSystem._LastHitForce, _human._HealthSystem._LastHitBoneName, _human._HealthSystem._LastHitDir));
            return;
        }
    }
    public void FixedUpdate()
    {

    }
}
public class RestMoveState : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;
    public RestMoveState(Humanoid human)
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
        if (_human._HealthSystem._IsUnconscious)
        {
            _human.EnterState(new UnconsciousMoveState(_human, _human._HealthSystem._LastHitForce, _human._HealthSystem._LastHitBoneName, _human._HealthSystem._LastHitDir));
            return;
        }
    }
    public void FixedUpdate()
    {

    }
}
public class UnconsciousMoveState : MovementState
{
    public Humanoid _Human => _human;
    private Humanoid _human;

    private Coroutine _getUpCoroutine;
    private bool _isGettingUp;

    private Vector3 _hitVelocity;
    private string _hitBoneName;
    private Vector3 _hitPos;
    public UnconsciousMoveState(Humanoid human, Vector3 hitVel, string hitBoneName, Vector3 hitPos)
    {
        _human = human;
        _hitVelocity = hitVel;
        _hitBoneName = hitBoneName;
        _hitPos = hitPos;
    }
    public void EnterState(MovementState oldState)
    {
        _human._Animator.enabled = false;
        _human.DisableHumanAdditionals();
        Vector3 currentVel = _human._Rigidbody.linearVelocity;
        _human._Rigidbody.isKinematic = true;
        _human._Rigidbody.Sleep();
        _human._RagdollAvatar.EnableRagdoll(currentVel, _hitVelocity, _hitBoneName, _hitPos);
        if (_human._RightHandEquippedItemRef != null)
            _human._RightHandEquippedItemRef.Unequip(true, false);
        if (_human._LeftHandEquippedItemRef != null)
            _human._LeftHandEquippedItemRef.Unequip(true, false);
    }

    public void ExitState(MovementState newState)
    {
        if (_getUpCoroutine != null)
            GameManager._Instance.StopCoroutine(_getUpCoroutine);
    }
    public void DoState()
    {
        if (!_human._HealthSystem._IsUnconscious && !_isGettingUp)
        {
            _isGettingUp = true;
            _human._RagdollAvatar.DisableRagdoll();

            GameManager._Instance.CoroutineCall(ref _getUpCoroutine, GetUpCoroutine(), _human);
        }
        _human._LocomotionSystem.UpdateAnimator();
    }
    public void FixedUpdate()
    {

    }
    private IEnumerator GetUpCoroutine()
    {
        float startTime = Time.time;
        List<Transform> ragdollBones = _human._RagdollAvatar._Bones;
        Animator animator = _human._Animator;


        bool faceUp = Vector3.Dot(
            _human._LocomotionSystem.transform.Find("char/Root/Global/Position/Hips").forward,
            Vector3.up
        ) > 0;
        string animName = faceUp ? "GetUpFromFront" : "GetUpFromBack";
        AnimationClip getUpClip = null;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animName)
            {
                getUpClip = clip;
                break;
            }
        }

        if (getUpClip == null)
        {
            Debug.LogError("GetUp clip bulunamadý: " + animName);
            yield break;
        }

        var startPos = new Vector3[ragdollBones.Count];
        var startRot = new Quaternion[ragdollBones.Count];
        for (int i = 0; i < ragdollBones.Count; i++)
        {
            startPos[i] = ragdollBones[i].localPosition;
            startRot[i] = ragdollBones[i].localRotation;
        }

        animator.enabled = true;
        getUpClip.SampleAnimation(animator.gameObject, 0f);

        var targetPos = new Vector3[ragdollBones.Count];
        var targetRot = new Quaternion[ragdollBones.Count];
        for (int i = 0; i < ragdollBones.Count; i++)
        {
            targetPos[i] = ragdollBones[i].localPosition;
            targetRot[i] = ragdollBones[i].localRotation;
        }

        for (int i = 0; i < ragdollBones.Count; i++)
        {
            ragdollBones[i].localPosition = startPos[i];
            ragdollBones[i].localRotation = startRot[i];
        }
        animator.enabled = false;

        float t = 0f;
        float duration = 0.5f;
        bool isHipArranged = false;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            for (int i = 0; i < ragdollBones.Count; i++)
            {
                if (ragdollBones[i].name == "Hips") { if (!isHipArranged) { isHipArranged = true; ArrangeHumanoidToHips(ragdollBones[i], targetPos[i], targetRot[i]); } continue; }
                ragdollBones[i].localPosition = Vector3.Lerp(startPos[i], targetPos[i], t);
                ragdollBones[i].localRotation = Quaternion.Slerp(startRot[i], targetRot[i], t);
            }
            yield return null;
        }

        animator.enabled = true;
        _human._Animator.Play(animName);

        while (startTime + 1.5f > Time.time)
            yield return null;

        _human.EnterState(new LocomotionState(_human));
        _human._Rigidbody.isKinematic = false;
        _human._Rigidbody.WakeUp();
    }
    private void ArrangeHumanoidToHips(Transform hips, Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 oldDir = hips.up; // actual forward axis
        oldDir.y = 0f;
        oldDir.Normalize();

        hips.localPosition = targetPos;
        hips.localRotation = targetRot;

        Vector3 newDir = hips.up;
        newDir.y = 0f;
        newDir.Normalize();

        float deltaY = Vector3.SignedAngle(oldDir, newDir, Vector3.up);
        _human.transform.Rotate(0f, -deltaY, 0f, Space.World);
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
        _human.ChangeAnimation("EmptyHandsRight");
        _human.ChangeAnimation("EmptyHandsLeft");
    }

    public void ExitState(HandState newState)
    {

    }
    public void DoState()
    {
        if (_human._MovementState is UnconsciousMoveState)
            return;

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
        _human = human;
    }
    public void EnterState(HandState oldState)
    {
        _human._LocomotionSystem.MovementSpeedMultiplierHandState = 1f;
        if (_human is Player player)
            GameManager._Instance.BufferActivated(ref player._AttackReadyBuffer, player, ref player._AttackReadyCoroutine);
    }

    public void ExitState(HandState newState)
    {
        _human.StopCombatActions();
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

        HandStateMethods.CheckAttack(_human);
        HandStateMethods.ArrangeBlock(_human);
        HandStateMethods.TryParry(_human);

        if (_human._IsInAttackReady)
        {
            _human._AttackReadySpeed = Mathf.Pow(Mathf.Clamp(_human._LastAttackReadyTime + 1f - Time.time, 0f, 0.8f), 1.75f);
        }
    }
}

#endregion
