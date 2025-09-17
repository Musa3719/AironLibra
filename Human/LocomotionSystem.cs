using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class AnimatorParameters
{
    public static int InputHorizontal = Animator.StringToHash("InputHorizontal");
    public static int InputVertical = Animator.StringToHash("InputVertical");
    public static int InputMagnitude = Animator.StringToHash("InputMagnitude");
    public static int IsGrounded = Animator.StringToHash("IsGrounded");
    public static int IsStrafing = Animator.StringToHash("IsStrafing");
    public static int IsSprinting = Animator.StringToHash("IsSprinting");
    public static int GroundDistance = Animator.StringToHash("GroundDistance");
}

public class LocomotionSystem : MonoBehaviour
{
    #region Speed                

    public const float AnimatorWalkSpeedDefault = 0.4f;
    public const float AnimatorRunningSpeedDefault = 0.7f;
    public const float AnimatorSprintSpeedDefault = 1f;

    [HideInInspector] public float AnimatorMaxSpeedMultiplier = 1f;
    [HideInInspector] public float MovementSpeedMultiplier = 1f;

    private float _stamina;
    public float Stamina { get => _stamina; set { _stamina = Mathf.Clamp(value, 0f, MaxStamina); } }
    public float MaxStamina { get; set; }
    public float WaitForRunLastTriggerTime { get; set; }

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
    [Range(30, 80)] public float slopeLimit = 75f;

    public float inputMagnitude;                      // sets the inputMagnitude to update the animations in the animator controller
    public float verticalSpeed;                       // set the verticalSpeed based on the verticalInput
    public float horizontalSpeed;                     // set the horizontalSpeed based on the horizontalInput       
    public float moveSpeed;                           // set the current moveSpeed for the MoveCharacter method
    public float verticalVelocity;                    // set the vertical velocity of the rigidbody
    public float colliderHeight;                      // storage capsule collider extra information        
    public float heightReached;                       // max height that character reached in air;
    public float groundDistance;                      // used to know the distance from the ground
    public RaycastHit groundHit;                      // raycast to hit the ground 
    public Vector3 inputSmooth;                       // generate smooth input based on the inputSmooth value       
    public Vector3 moveDirection;                     // used to know the direction you're moving 
    #endregion


    public void Init()
    {
        _human = GetComponent<Humanoid>();

        if (_human is Player)
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
        }

        // capsule collider info
        _defaultCollider = GetComponent<CapsuleCollider>();
        _crouchCollider = transform.Find("CrouchCollider").gameObject;
        _proneCollider = transform.Find("ProneCollider").gameObject;

        colliderHeight = _defaultCollider.height;

        _human._IsGrounded = true;
    }
    public virtual void UpdateAnimator()
    {
        if (_human._Animator == null || !_human._Animator.enabled) return;

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
    }

    public virtual void SetAnimatorMoveSpeed(MovementSetting speed)
    {
        Vector3 relativeInput = transform.InverseTransformDirection(moveDirection);
        verticalSpeed = relativeInput.z * MovementSpeedMultiplier;
        horizontalSpeed = relativeInput.x * MovementSpeedMultiplier;

        float multiplier = 1f;
        if (speed.walkByDefault)
            multiplier = _human._IsSprinting ? AnimatorRunningSpeedDefault : AnimatorWalkSpeedDefault;
        else
            multiplier = _human._IsSprinting ? GetAnimatorSprintSpeed() : AnimatorRunningSpeedDefault;

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
