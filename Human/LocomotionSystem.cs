using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class AnimatorParameters
{
    public static int InputHorizontal = Animator.StringToHash("InputHorizontal");
    public static int InputVertical = Animator.StringToHash("InputVertical");
    public static int InputMagnitude = Animator.StringToHash("InputMagnitude");
    public static int IsRightHandEmpty = Animator.StringToHash("IsRightHandEmpty");
    public static int IsLeftHandEmpty = Animator.StringToHash("IsLeftHandEmpty");
    public static int IsGrounded = Animator.StringToHash("IsGrounded");
    public static int IsStrafing = Animator.StringToHash("IsStrafing");
    public static int IsSprinting = Animator.StringToHash("IsSprinting");
    public static int GroundDistance = Animator.StringToHash("GroundDistance");
    public static int FlatSpeed = Animator.StringToHash("FlatSpeed");
    public static int AttackSpeedMultiplier = Animator.StringToHash("AttackSpeedMultiplier");
    public static int IsBlocking = Animator.StringToHash("IsBlocking");
    public static int IsInCombatMode = Animator.StringToHash("IsInCombatMode");
    public static int IsGoingLeft = Animator.StringToHash("IsGoingLeft");
    public static int BlockingAnimNumber = Animator.StringToHash("BlockingAnimNumber");
    public static int ReadySpeed = Animator.StringToHash("ReadySpeed");
    public static int RangedAimNumber = Animator.StringToHash("RangedAimNumber");
    public static int ReloadNumber = Animator.StringToHash("ReloadNumber");
}

public class LocomotionSystem : MonoBehaviour
{
    #region Speed                

    public const float AnimatorWalkSpeedDefault = 0.26f;
    public const float AnimatorRunningSpeedDefault = 0.372f;
    public const float AnimatorSprintSpeedDefault = 0.7f;

    [HideInInspector] public float AnimatorMaxSpeedMultiplier = 1f;
    [HideInInspector] public float MovementSpeedMultiplier => Mathf.Clamp(MovementSpeedMultiplierMoveState * MovementSpeedMultiplierHandState * MovementSpeedMultiplierCarryCapacity * _human._HealthSystem._MovementSpeedMultiplierHealthState, 0.5f, 1.5f);
    [HideInInspector] public float MovementSpeedMultiplierMoveState = 1f;
    [HideInInspector] public float MovementSpeedMultiplierHandState = 1f;
    [HideInInspector] public float MovementSpeedMultiplierCarryCapacity => 1.25f - 0.25f * Mathf.Clamp(_human._CarryCapacityRate, 0f, 3f);

    public float GetAnimatorSprintSpeed()
    {
        return AnimatorSprintSpeedDefault * AnimatorMaxSpeedMultiplier;
    }

    #endregion

    #region Components

    internal Humanoid _human;
    internal PhysicsMaterial frictionPhysics, maxFrictionPhysics, slippyPhysics;
    public CapsuleCollider _defaultCollider { get; private set; }
    public GameObject _crouchCollider { get; private set; }
    public GameObject _proneCollider { get; private set; }
    #endregion

    #region Variables

    public LocomotionSystem.MovementSetting FreeMovementSetting;
    public LocomotionSystem.MovementSetting AimingMovementSetting;

    [Tooltip("Use the currently Rigidbody Velocity to influence on the Jump Distance")]
    public bool jumpWithRigidbodyForce = false;
    [Tooltip("Add Extra jump height, if you want to jump only with Root Motion leave the value with 0.")]
    public float jumpHeight = 4f;

    [Tooltip("Speed that the character will move while dodging")]
    public float dodgeSpeed = 5f;
    [Tooltip("Speed that the character will move while airborne")]
    public float airSpeed = 5f;
    [Tooltip("Smoothness of the direction while airborne")]
    public float airSmooth = 6f;
    [Tooltip("Apply extra gravity when the character is not grounded")]
    public float extraGravity = -10f;
    [HideInInspector]
    public float limitFallVelocity = -15f;

    [Header("- Ground")]
    [Tooltip("Layers that the character can walk on")]
    public LayerMask groundLayer = 1 << 0;
    [Tooltip("Distance to became not grounded")]
    public float groundMinDistance = 0.25f;
    public float groundMaxDistance = 0.5f;
    [Tooltip("Max angle to walk")]
    [Range(30, 80)] public float slopeLimit = 50f;

    public float inputMagnitude;                      // sets the inputMagnitude to update the animations in the animator controller
    public float verticalSpeed;                       // set the verticalSpeed based on the verticalInput
    public float horizontalSpeed;                     // set the horizontalSpeed based on the horizontalInput       
    public float moveSpeed;                           // set the current moveSpeed for the MoveCharacter method
    public float verticalVelocity;                    // set the vertical velocity of the rigidbody
    public float colliderHeight;                      // storage capsule collider extra information        
    public float heightReached;                       // max height that character reached in air;
    public float groundDistance;                      // used to know the distance from the ground
    public RaycastHit groundHit;                      // raycast to hit the ground 
    public RaycastHit forwardGroundHit;                      // forward raycast to hit the ground 
    public Vector3 inputSmooth;                       // generate smooth input based on the inputSmooth value       
    public Vector3 moveDirection;                     // used to know the direction you're moving 
    #endregion

    /*private void OnDestroy()
    {
        if (transform.parent != null && transform.parent.GetComponent<NPC>() != null)//npc destroyed
        {
            NPC npc = transform.parent.GetComponent<NPC>();
            if (npc._RightHandEquippedItemRef != null)
                npc._RightHandEquippedItemRef.DespawnHandItem();
            if (npc._LeftHandEquippedItemRef != null)
                npc._LeftHandEquippedItemRef.DespawnHandItem();
            if (npc._BackCarryItemRef != null)
                npc._BackCarryItemRef.DespawnBackCarryItem();
        }
    }*/
    public void Init()
    {
        _human = transform.parent == null ? GetComponent<Humanoid>() : transform.parent.GetComponent<Humanoid>();

        /*if (_human is Player)
        {
            // slides the character through walls and edges
            frictionPhysics = new PhysicsMaterial();
            frictionPhysics.name = "frictionPhysics";
            frictionPhysics.staticFriction = .25f;
            frictionPhysics.dynamicFriction = .25f;
            frictionPhysics.frictionCombine = PhysicsMaterialCombine.Multiply;

            // prevents the collider from slipping on ramps
            maxFrictionPhysics = new PhysicsMaterial();
            maxFrictionPhysics.name = "maxFrictionPhysics";
            maxFrictionPhysics.staticFriction = 1f;
            maxFrictionPhysics.dynamicFriction = 1f;
            maxFrictionPhysics.frictionCombine = PhysicsMaterialCombine.Maximum;

            // air physics 
            slippyPhysics = new PhysicsMaterial();
            slippyPhysics.name = "slippyPhysics";
            slippyPhysics.staticFriction = 0f;
            slippyPhysics.dynamicFriction = 0f;
            slippyPhysics.frictionCombine = PhysicsMaterialCombine.Minimum;
        }*/

        // capsule collider info
        _defaultCollider = _human._MainCollider;
        _crouchCollider = transform.Find("CrouchCollider").gameObject;
        _proneCollider = transform.Find("ProneCollider").gameObject;

        colliderHeight = _defaultCollider.height;

        _human._IsGrounded = true;
    }
    private float GetRangedNumber()
    {
        if(!(_human._HandState is RangedWeaponHandState state)) { Debug.LogError("Ranged number error state not found!"); return 0f; }
        if (state._RangedWeapon == null) { Debug.LogError("Ranged weapon is null! ranged number returns 0f"); return 0f; }

        return state._RangedWeapon._IsCrossbow ? 1f : 2f;
    }
    public virtual void UpdateAnimator()
    {
        if (_human._Animator == null || !_human._Animator.enabled) return;


        if (_human._Animator.GetBool(AnimatorParameters.IsInCombatMode) != _human._IsInCombatMode)
            _human._Animator.SetBool(AnimatorParameters.IsInCombatMode, _human._IsInCombatMode);
        if ((_human._Animator.GetFloat(AnimatorParameters.RangedAimNumber) != 0f) != _human._IsInRangedAim)
            _human._Animator.SetFloat(AnimatorParameters.RangedAimNumber, _human._IsInRangedAim ? (GetRangedNumber()) : 0f);
        if ((_human._Animator.GetFloat(AnimatorParameters.ReloadNumber) != 0f) != _human._IsReloading)
            _human._Animator.SetFloat(AnimatorParameters.ReloadNumber, _human._IsReloading ? (GetRangedNumber()) : 0f);
        if (_human._Animator.GetBool(AnimatorParameters.IsBlocking) != _human._IsBlocking)
            _human._Animator.SetBool(AnimatorParameters.IsBlocking, _human._IsBlocking);

        bool isGoingLeft = _human._Animator.GetFloat(AnimatorParameters.InputHorizontal) < 0f;
        if (_human._Animator.GetBool(AnimatorParameters.IsGoingLeft) != isGoingLeft)
            _human._Animator.SetBool(AnimatorParameters.IsGoingLeft, isGoingLeft);

        if (_human._HandState is MeleeWeaponHandState)
        {
            int targetAnimNumber = 0;
            if (_human._LeftHandEquippedItemRef == null && _human._RightHandEquippedItemRef == null)
                targetAnimNumber = 0;
            else if (_human._LeftHandEquippedItemRef == null && _human._RightHandEquippedItemRef != null)
                targetAnimNumber = 1;
            else if (_human._LeftHandEquippedItemRef != null && _human._LeftHandEquippedItemRef is WeaponItem weaponItem && weaponItem._ItemDefinition._Name.StartsWith("Shield"))
                targetAnimNumber = 3;
            else if (_human._LeftHandEquippedItemRef != null && _human._RightHandEquippedItemRef != null)
                targetAnimNumber = 2;

            if (_human._Animator.GetInteger(AnimatorParameters.BlockingAnimNumber) != targetAnimNumber)
                _human._Animator.SetInteger(AnimatorParameters.BlockingAnimNumber, targetAnimNumber);

            if (_human._IsInAttackReady)
            {
                if (_human._Animator.GetFloat(AnimatorParameters.ReadySpeed) != _human._AttackReadySpeed)
                    _human._Animator.SetFloat(AnimatorParameters.ReadySpeed, _human._AttackReadySpeed);
            }
        }

        if (_human._MovementState is UnconsciousMoveState)
        {
            if (_human._Animator.GetFloat(AnimatorParameters.InputHorizontal) != 0f)
                _human._Animator.SetFloat(AnimatorParameters.InputHorizontal, 0f);
            if (_human._Animator.GetFloat(AnimatorParameters.InputVertical) != 0f)
                _human._Animator.SetFloat(AnimatorParameters.InputVertical, 0f);
            if (_human._Animator.GetFloat(AnimatorParameters.InputMagnitude) != 0f)
                _human._Animator.SetFloat(AnimatorParameters.InputMagnitude, 0f);
            return;
        }
        else if (_human._MovementState is ProneMoveState)
        {
            Vector3 flatSpeed = _human._Rigidbody.linearVelocity;
            flatSpeed.y = 0f;
            float paramSpeed = _human._Animator.GetFloat(AnimatorParameters.FlatSpeed);
            _human._Animator.SetFloat(AnimatorParameters.FlatSpeed, Mathf.Lerp(paramSpeed, flatSpeed.magnitude, Time.deltaTime * 3.5f));
        }

        if (_human._Animator.GetBool(AnimatorParameters.IsStrafing) != _human._IsStrafing)
            _human._Animator.SetBool(AnimatorParameters.IsStrafing, _human._IsStrafing);
        if (_human._Animator.GetBool(AnimatorParameters.IsSprinting) != _human._IsSprinting)
            _human._Animator.SetBool(AnimatorParameters.IsSprinting, _human._IsSprinting);
        if (_human._Animator.GetBool(AnimatorParameters.IsGrounded) != _human._IsGrounded)
            _human._Animator.SetBool(AnimatorParameters.IsGrounded, _human._IsGrounded);
        if (_human._Animator.GetFloat(AnimatorParameters.GroundDistance) != groundDistance)
            _human._Animator.SetFloat(AnimatorParameters.GroundDistance, groundDistance);

        if (_human._IsStrafing)
        {
            if (_human._Animator.GetFloat(AnimatorParameters.InputHorizontal) != (_human._StopMove ? 0 : horizontalSpeed))
                _human._Animator.SetFloat(AnimatorParameters.InputHorizontal, _human._StopMove ? 0 : horizontalSpeed, AimingMovementSetting.animationSmooth, Time.deltaTime);
            if (_human._Animator.GetFloat(AnimatorParameters.InputVertical) != (_human._StopMove ? 0 : verticalSpeed))
                _human._Animator.SetFloat(AnimatorParameters.InputVertical, _human._StopMove ? 0 : verticalSpeed, AimingMovementSetting.animationSmooth, Time.deltaTime);
        }
        else
        {
            if (_human._Animator.GetFloat(AnimatorParameters.InputHorizontal) != (_human._StopMove ? 0 : horizontalSpeed))
                _human._Animator.SetFloat(AnimatorParameters.InputHorizontal, _human._StopMove ? 0 : horizontalSpeed, FreeMovementSetting.animationSmooth, Time.deltaTime);
            if (_human._Animator.GetFloat(AnimatorParameters.InputVertical) != (_human._StopMove ? 0 : verticalSpeed))
                _human._Animator.SetFloat(AnimatorParameters.InputVertical, _human._StopMove ? 0 : verticalSpeed, FreeMovementSetting.animationSmooth, Time.deltaTime);
        }

        if (_human._Animator.GetFloat(AnimatorParameters.InputMagnitude) != (_human._StopMove ? 0f : inputMagnitude))
            _human._Animator.SetFloat(AnimatorParameters.InputMagnitude, _human._StopMove ? 0f : inputMagnitude);

        if (_human._Animator.GetBool(AnimatorParameters.IsRightHandEmpty) != (_human._RightHandEquippedItemRef == null || !_human._IsInCombatMode))
            _human._Animator.SetBool(AnimatorParameters.IsRightHandEmpty, (_human._RightHandEquippedItemRef == null || !_human._IsInCombatMode));
        if (_human._Animator.GetBool(AnimatorParameters.IsLeftHandEmpty) != (_human._LeftHandEquippedItemRef == null || !_human._IsInCombatMode))
            _human._Animator.SetBool(AnimatorParameters.IsLeftHandEmpty, (_human._LeftHandEquippedItemRef == null || !_human._IsInCombatMode));
    }

    public virtual void SetAnimatorMoveSpeed(MovementSetting speed)
    {
        Vector3 relativeInput = transform.InverseTransformDirection(moveDirection);
        verticalSpeed = relativeInput.z * MovementSpeedMultiplier;
        horizontalSpeed = relativeInput.x * MovementSpeedMultiplier;

        float multiplier = 1f;
        if (speed.walkByDefault)
            multiplier = _human._IsSprinting ? AnimatorRunningSpeedDefault : (AnimatorWalkSpeedDefault * (_human._IsStrafing ? 1.25f : 1f));
        else
            multiplier = _human._IsSprinting ? GetAnimatorSprintSpeed() : AnimatorRunningSpeedDefault;

        if (_human._SizeMultiplier != 0f)
            _human._Animator.speed = 1f / _human._SizeMultiplier;

        var newInput = new Vector2(verticalSpeed, horizontalSpeed);
        float lerpSpeed = _human._IsStrafing ? AimingMovementSetting.animationSmooth : FreeMovementSetting.animationSmooth;
        lerpSpeed *= (newInput.magnitude * multiplier) < inputMagnitude ? 2f : 1f;
        inputMagnitude = Mathf.MoveTowards(inputMagnitude, newInput.magnitude * multiplier, Time.fixedDeltaTime * lerpSpeed);
    }

    [System.Serializable]
    public class MovementSetting
    {
        [Range(1f, 20f)]
        public float movementSmooth = 6f;
        [Range(0f, 1f)]
        public float animationSmooth = 0.2f;
        [Tooltip("Rotation speed of the character")]
        public float rotationSpeed = 16f;
        [Tooltip("Character will limit the movement to walk instead of running")]
        public bool walkByDefault = false;
        [Tooltip("Speed to Walk using rigidbody or extra speed if you're using RootMotion")]
        public float walkSpeed = 1.75f;
        [Tooltip("Speed to Run using rigidbody or extra speed if you're using RootMotion")]
        public float runningSpeed = 3f;
        [Tooltip("Speed to Sprint using rigidbody or extra speed if you're using RootMotion")]
        public float sprintSpeed = 4.5f;
    }
}
