using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MovementStateMethods
{
    public static void Rotate60Degrees(Humanoid human, bool isRight)
    {
        if (human._LastTimeRotated + 0.5f > Time.time) return;

        human._LastTimeRotated = Time.time;
        if (isRight)
        {
            human.ChangeAnimation("TurnRightLocomotion");
        }
        else
        {
            human.ChangeAnimation("TurnLeftLocomotion");
        }
        GameManager._Instance.CoroutineCall(ref human._RotateAroundCoroutine, RotateCoroutine(human, isRight), GameManager._Instance);
    }
    private static IEnumerator RotateCoroutine(Humanoid human, bool isRight)
    {
        float startTime = Time.time;
        float startY = human.transform.localEulerAngles.y;
        while (startTime + 0.35f > Time.time)
        {
            if (human._Rigidbody.linearVelocity.magnitude > 0.1f)
            {
                human.ChangeAnimation("Strafing Movement");
                yield break;
            }

            human.transform.Rotate(isRight ? Vector3.up : -Vector3.up, Time.deltaTime * 60f / 0.35f, Space.Self);
            yield return null;
        }
    }
    public static void ControlLocomotionType(Humanoid human)
    {
        bool isInCrouch = human._MovementState is Crouch;
        bool isAiming = human._IsInCombatMode;
        if (!human._IsGrounded) isAiming = false;

        if (isAiming)
        {
            human._IsStrafing = true;
            SetControllerMoveSpeed(human._LocomotionSystem.AimingMovementSetting, human);
            human._LocomotionSystem.SetAnimatorMoveSpeed(human._LocomotionSystem.AimingMovementSetting);
        }
        else
        {
            human._IsStrafing = false;

            if (isInCrouch)
            {
                SetControllerMoveSpeed(human._LocomotionSystem.AimingMovementSetting, human);
                human._LocomotionSystem.SetAnimatorMoveSpeed(human._LocomotionSystem.AimingMovementSetting);
            }
            else
            {
                SetControllerMoveSpeed(human._LocomotionSystem.FreeMovementSetting, human);
                human._LocomotionSystem.SetAnimatorMoveSpeed(human._LocomotionSystem.FreeMovementSetting);
            }
        }


        MoveCharacter(human._LocomotionSystem.moveDirection, human);
    }

    public static void ControlRotationType(Humanoid human, float rotationSpeed = 0f)
    {
        bool isMoving = human._DirectionInput != Vector2.zero;
        //bool isMoving = (human is Player) ? (human as Player).DirectionInput != Vector2.zero : human.GetVelocity() != Vector3.zero;

        if (isMoving)
        {
            // calculate input smooth
            Vector3 targetSmooth = human._DirectionInput;
            human._LocomotionSystem.inputSmooth = Vector3.Lerp(human._LocomotionSystem.inputSmooth, targetSmooth, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.deltaTime);

            if (!human._IsStrafing)
                RotateToDirection(human._LocomotionSystem.moveDirection, human);

        }
    }

    public static void UpdateAnimator(Humanoid human)
    {
        human._LocomotionSystem.UpdateAnimator();
    }
    public static void UpdateMoveDirection(Humanoid human, Transform referenceTransform)
    {
        if (human is Player)
        {
            float magnitude = human._DirectionInput.magnitude;
            if (magnitude <= 0.01)
            {
                human._LocomotionSystem.moveDirection = Vector3.Lerp(human._LocomotionSystem.moveDirection, Vector3.zero, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.deltaTime);
                return;
            }
        }
        else
        {
            (human as NPC).ArrangeMovementCorners();
            Vector3 dist = (human as NPC)._MoveTargetPosition - human.transform.position;
            float magnitude = dist.magnitude;
            if (magnitude <= 1f)
            {
                human._LocomotionSystem.moveDirection = Vector3.Lerp(human._LocomotionSystem.moveDirection, Vector3.zero, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.deltaTime);
                return;
            }
        }

        //get the right-facing direction of the referenceTransform
        Vector3 right = human is Player ? referenceTransform.right : Vector3.right;
        right.y = 0;
        //get the forward direction relative to referenceTransform Right
        Vector3 forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
        // determine the direction the player will face based on input and the referenceTransform's right and forward directions
        human._LocomotionSystem.moveDirection = (human._LocomotionSystem.inputSmooth.x * right) + (human._LocomotionSystem.inputSmooth.y * forward);
    }

    private static bool IsMovingForward(Humanoid human)
    {
        Vector2 first = new Vector2(human._Rigidbody.linearVelocity.x, human._Rigidbody.linearVelocity.z).normalized;
        Vector2 second = new Vector2(human.transform.forward.x, human.transform.forward.z).normalized;
        return Vector2.Dot(first, second) > 0.4f;
    }
    private static bool CanRunWithStamina(Humanoid human)
    {
        if (human._LocomotionSystem.Stamina > 0 && Time.time > 2f + human._LocomotionSystem.WaitForRunLastTriggerTime) return true;
        else if (Time.time > 2f + human._LocomotionSystem.WaitForRunLastTriggerTime)
        {
            human._LocomotionSystem.WaitForRunLastTriggerTime = Time.time;
            return false;
        }
        return false;
    }
    public static void CheckSprint(Humanoid human)
    {
        if (human is Player)
            CheckSprintForPlayer(human._RunInput, human._SprintInput, human as Player);
        else
            CheckSprintForNPC(human._RunInput, human._SprintInput, human as NPC);
    }
    private static void CheckSprintForPlayer(bool runInput, bool sprintInput, Player player)
    {
        bool sprintConditions = player._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(player) && player._IsGrounded && IsMovingForward(player);

        if ((sprintInput || runInput) && sprintConditions)
        {
            bool isVelocityEnoughForSprint = player._Rigidbody.linearVelocity.magnitude > player._LocomotionSystem.FreeMovementSetting.runningSpeed - 2f;
            if (sprintInput && isVelocityEnoughForSprint)
            {
                if (player._IsStrafing)
                    player._LocomotionSystem.AimingMovementSetting.walkByDefault = false;
                else
                    player._LocomotionSystem.FreeMovementSetting.walkByDefault = false;
            }

            if (player._DirectionInput.sqrMagnitude > 0.1f && !player._IsSprinting)
            {
                player._IsSprinting = true;
            }
            else if (player._DirectionInput.sqrMagnitude <= 0.1f && player._IsSprinting)
            {
                if (player._IsStrafing)
                    player._LocomotionSystem.AimingMovementSetting.walkByDefault = true;
                else
                    player._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
                player._IsSprinting = false;
            }
        }
        else if (player._IsSprinting)
        {
            if (player._IsStrafing)
                player._LocomotionSystem.AimingMovementSetting.walkByDefault = true;
            else
                player._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            player._IsSprinting = false;
        }
    }
    private static void CheckSprintForNPC(bool runInput, bool sprintInput, NPC npc)
    {
        bool sprintConditions = npc._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(npc) && npc._IsGrounded && IsMovingForward(npc);

        if ((sprintInput || runInput) && sprintConditions)
        {
            bool isVelocityEnoughForSprint = npc._Rigidbody.linearVelocity.magnitude > npc._LocomotionSystem.FreeMovementSetting.runningSpeed - 2f;
            if (sprintInput && isVelocityEnoughForSprint)
            {
                if (npc._IsStrafing)
                    npc._LocomotionSystem.AimingMovementSetting.walkByDefault = false;
                else
                    npc._LocomotionSystem.FreeMovementSetting.walkByDefault = false;
            }

            if (npc.GetVelocity().sqrMagnitude > 0.1f && !npc._IsSprinting)
            {
                npc._IsSprinting = true;
            }
            else if (npc.GetVelocity().sqrMagnitude <= 0.1f && npc._IsSprinting)
            {
                if (npc._IsStrafing)
                    npc._LocomotionSystem.AimingMovementSetting.walkByDefault = true;
                else
                    npc._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
                npc._IsSprinting = false;
            }
        }
        else if (npc._IsSprinting)
        {
            if (npc._IsStrafing)
                npc._LocomotionSystem.AimingMovementSetting.walkByDefault = true;
            else
                npc._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            npc._IsSprinting = false;
        }
    }
    #region Jump Methods
    public static void CheckJump(Humanoid human)
    {
        if (human is Player)
        {
            if ((human as Player)._JumpBuffer && JumpConditions(human))
            {
                GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._JumpBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._JumpCoroutine);
                Jump(human);
            }
        }
        else
        {
            if ((human as NPC)._WantsToJump && JumpConditions(human))
            {
                Jump(human);
            }
        }
    }

    private static bool JumpConditions(Humanoid human)
    {
        return human._IsGrounded && MovementStateMethods.GroundAngle(human) < human._LocomotionSystem.slopeLimit && !human._IsJumping && !human._StopMove;
    }

    private static void Jump(Humanoid human)
    {
        // trigger jump behaviour
        human._JumpCounter = human._JumpTimer;
        human._IsJumping = true;

        // trigger jump animations
        if (human.GetVelocity().magnitude < 2f)
            human.ChangeAnimation("Jump", 0.1f);
        else
            human.ChangeAnimation("JumpMove", 0.2f);
    }


    public static void ControlJumpBehaviour(Humanoid human)
    {
        if (!human._IsJumping) return;

        human._JumpCounter -= Time.deltaTime;
        if (human._JumpCounter <= 0)
        {
            human._JumpCounter = 0;
            human._IsJumping = false;
        }
        // apply extra force to the jump height   
        var vel = human.GetVelocity();
        vel.y = human._LocomotionSystem.jumpHeight;

        human._Rigidbody.linearVelocity = vel;
        /*if (human is Player)
            (human as Player).Rigidbody.velocity = vel;
        else
            (human as NPC).Jump(vel);*/

    }

    public static void AirControl(Humanoid human)
    {
        if ((human._IsGrounded && !human._IsJumping)) return;
        if (human.transform.position.y > human._LocomotionSystem.heightReached) human._LocomotionSystem.heightReached = human.transform.position.y;

        Vector3 targetSmooth = human._DirectionInput;
        human._LocomotionSystem.inputSmooth = Vector3.Lerp(human._LocomotionSystem.inputSmooth, targetSmooth, human._LocomotionSystem.airSmooth * Time.deltaTime);

        if (human._LocomotionSystem.jumpWithRigidbodyForce && !human._IsGrounded)
        {
            //if (human is Player)
            human._Rigidbody.AddForce(human._LocomotionSystem.moveDirection * human._LocomotionSystem.airSpeed * Time.deltaTime, ForceMode.VelocityChange);
            return;
        }

        human._LocomotionSystem.moveDirection.y = 0;
        human._LocomotionSystem.moveDirection.x = Mathf.Clamp(human._LocomotionSystem.moveDirection.x, -1f, 1f);
        human._LocomotionSystem.moveDirection.z = Mathf.Clamp(human._LocomotionSystem.moveDirection.z, -1f, 1f);

        Vector3 targetPosition = human._Rigidbody.position + (human._LocomotionSystem.moveDirection * human._LocomotionSystem.airSpeed) * Time.deltaTime;
        Vector3 targetVelocity = (targetPosition - human.transform.position) / Time.deltaTime;

        targetVelocity.y = human._Rigidbody.linearVelocity.y;
        human._Rigidbody.linearVelocity = Vector3.Lerp(human._Rigidbody.linearVelocity, targetVelocity, human._LocomotionSystem.airSmooth * Time.deltaTime);
    }

    /*public static bool jumpFwdCondition(Humanoid human)
    {
        Vector3 p1 = human.transform.position + human.AnimationSystem._capsuleCollider.center + Vector3.up * -human.AnimationSystem._capsuleCollider.height * 0.5F;
        Vector3 p2 = p1 + Vector3.up * human.AnimationSystem._capsuleCollider.height;
        return Physics.CapsuleCastAll(p1, p2, human.AnimationSystem._capsuleCollider.radius * 0.5f, human.transform.forward, 0.6f, human.AnimationSystem.groundLayer).Length == 0;
    }*/

    #endregion

    #region Ground Check                

    public static void CheckGround(Humanoid human)
    {
        CheckGroundDistance(human);
        ControlMaterialPhysics(human);

        if (human._LocomotionSystem.groundDistance <= human._LocomotionSystem.groundMinDistance)
        {
            human._IsGrounded = true;
            if (!human._IsJumping && human._LocomotionSystem.groundDistance > 0.05f)
                human._Rigidbody.AddForce(human.transform.up * (human._LocomotionSystem.extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);

            human._LocomotionSystem.heightReached = human.transform.position.y;
        }
        else
        {
            if (human._LocomotionSystem.groundDistance >= human._LocomotionSystem.groundMaxDistance)
            {
                // set IsGrounded to false 
                human._IsGrounded = false;
                // check vertical velocity
                human._LocomotionSystem.verticalVelocity = human._Rigidbody.linearVelocity.y;
                // apply extra gravity when falling
                if (!human._IsJumping)
                {
                    human._Rigidbody.AddForce(human.transform.up * human._LocomotionSystem.extraGravity * Time.deltaTime, ForceMode.VelocityChange);
                }
            }
            else if (!human._IsJumping)
            {
                human._Rigidbody.AddForce(human.transform.up * (human._LocomotionSystem.extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);
            }
        }
    }

    private static void ControlMaterialPhysics(Humanoid human)
    {
        // change the physics material to very slip when not grounded
        human._LocomotionSystem._defaultCollider.material = (human._IsGrounded && GroundAngle(human) <= human._LocomotionSystem.slopeLimit + 1) ? human._LocomotionSystem.frictionPhysics : human._LocomotionSystem.slippyPhysics;

        Vector3 dir = human._DirectionInput;
        if (human._IsGrounded && dir == Vector3.zero)
            human._LocomotionSystem._defaultCollider.material = human._LocomotionSystem.maxFrictionPhysics;
        else if (human._IsGrounded && dir != Vector3.zero)
            human._LocomotionSystem._defaultCollider.material = human._LocomotionSystem.frictionPhysics;
        else
            human._LocomotionSystem._defaultCollider.material = human._LocomotionSystem.slippyPhysics;
    }

    private static void CheckGroundDistance(Humanoid human)
    {
        if (human._LocomotionSystem._defaultCollider != null)
        {
            // radius of the SphereCast
            float radius = human._LocomotionSystem._defaultCollider.radius * 0.9f;
            var dist = 10f;
            // ray for RayCast
            Ray ray2 = new Ray(human.transform.position + new Vector3(0, human._LocomotionSystem.colliderHeight / 2, 0), Vector3.down);
            // raycast for check the ground distance
            if (Physics.Raycast(ray2, out human._LocomotionSystem.groundHit, (human._LocomotionSystem.colliderHeight / 2) + dist, human._LocomotionSystem.groundLayer) && !human._LocomotionSystem.groundHit.collider.isTrigger)
                dist = human.transform.position.y - human._LocomotionSystem.groundHit.point.y;
            // sphere cast around the base of the capsule to check the ground distance
            if (dist >= human._LocomotionSystem.groundMinDistance)
            {
                Vector3 pos = human.transform.position + Vector3.up * (human._LocomotionSystem._defaultCollider.radius);
                Ray ray = new Ray(pos, -Vector3.up);
                if (Physics.SphereCast(ray, radius, out human._LocomotionSystem.groundHit, human._LocomotionSystem._defaultCollider.radius + human._LocomotionSystem.groundMaxDistance, human._LocomotionSystem.groundLayer) && !human._LocomotionSystem.groundHit.collider.isTrigger)
                {
                    Physics.Linecast(human._LocomotionSystem.groundHit.point + (Vector3.up * 0.1f), human._LocomotionSystem.groundHit.point + Vector3.down * 0.15f, out human._LocomotionSystem.groundHit, human._LocomotionSystem.groundLayer);
                    float newDist = human.transform.position.y - human._LocomotionSystem.groundHit.point.y;
                    if (dist > newDist) dist = newDist;
                }
            }
            human._LocomotionSystem.groundDistance = (float)System.Math.Round(dist, 2);
        }
    }

    private static float GroundAngle(Humanoid human)
    {
        var groundAngle = Vector3.Angle(human._LocomotionSystem.groundHit.normal, Vector3.up);
        return groundAngle;
    }

    /*private static float GroundAngleFromDirection(Humanoid human)
    {
        var dir = human.IsStrafing && (human as Player).DirectionInput.magnitude > 0 ? (human.transform.right * (human as Player).HorizontalInput + human.transform.forward * (human as Player).VerticalInput).normalized : human.transform.forward;
        var movementAngle = Vector3.Angle(dir, human.LocomotionSystem.groundHit.normal) - 90;
        return movementAngle;
    }*/

    #endregion

    #region Locomotion

    private static void SetControllerMoveSpeed(LocomotionSystem.MovementSetting speed, Humanoid human)
    {
        if (speed.walkByDefault)
            human._LocomotionSystem.moveSpeed = Mathf.Lerp(human._LocomotionSystem.moveSpeed, human._IsSprinting ? speed.runningSpeed : speed.walkSpeed, speed.movementSmooth * Time.deltaTime);
        else
            human._LocomotionSystem.moveSpeed = Mathf.Lerp(human._LocomotionSystem.moveSpeed, human._IsSprinting ? speed.sprintSpeed : speed.runningSpeed, speed.movementSmooth * Time.deltaTime);
    }

    private static void MoveCharacter(Vector3 direction, Humanoid human)
    {
        // calculate input smooth
        human._LocomotionSystem.inputSmooth = Vector3.Lerp(human._LocomotionSystem.inputSmooth, human._DirectionInput, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.deltaTime);

        if (!human._IsGrounded || human._IsJumping) return;

        direction.y = 0;
        direction.x = Mathf.Clamp(direction.x, -1f, 1f);
        direction.z = Mathf.Clamp(direction.z, -1f, 1f);
        // limit the input
        if (direction.magnitude > 1f)
            direction.Normalize();

        Vector3 targetPosition = human.transform.position + direction * (human._StopMove ? 0 : human._LocomotionSystem.moveSpeed * human._LocomotionSystem.MovementSpeedMultiplier) * Time.deltaTime;
        Vector3 targetVelocity = (targetPosition - human.transform.position) / Time.deltaTime;

        bool useVerticalVelocity = true;
        if (useVerticalVelocity) targetVelocity.y = human._Rigidbody.linearVelocity.y;
        human._Rigidbody.linearVelocity = targetVelocity;
    }

    public static void CheckSlopeLimit(Humanoid human)
    {
        if (human._DirectionInput.sqrMagnitude < 0.1) return;
        //if (human is Player && (human as Player).DirectionInput.sqrMagnitude < 0.1) return;
        //if (human is NPC && (human as NPC).GetVelocity().sqrMagnitude < 0.1) return;

        RaycastHit hitinfo;
        var hitAngle = 0f;

        if (Physics.Linecast(human.transform.position + Vector3.up * (human._LocomotionSystem._defaultCollider.height * 0.5f), human.transform.position + human._LocomotionSystem.moveDirection.normalized * (human._LocomotionSystem._defaultCollider.radius + 0.2f), out hitinfo, human._LocomotionSystem.groundLayer))
        {
            hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

            var targetPoint = hitinfo.point + human._LocomotionSystem.moveDirection.normalized * human._LocomotionSystem._defaultCollider.radius;
            if ((hitAngle > human._LocomotionSystem.slopeLimit) && Physics.Linecast(human.transform.position + Vector3.up * (human._LocomotionSystem._defaultCollider.height * 0.5f), targetPoint, out hitinfo, human._LocomotionSystem.groundLayer))
            {
                hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

                if (hitAngle > human._LocomotionSystem.slopeLimit && hitAngle < 85f)
                {
                    human._StopMove = true;
                    return;
                }
            }
        }
        human._StopMove = false;
    }

    /*public static void RotateToPosition(Vector3 position, Humanoid human)
    {
        Vector3 desiredDirection = position - human.transform.position;
        RotateToDirection(desiredDirection.normalized, human);
    }*/

    private static void RotateToDirection(Vector3 direction, Humanoid human)
    {
        float speed = human._AimSpeed * 1.4f;
        direction.y = 0f;
        Vector3 desiredForward = Vector3.RotateTowards(human.transform.forward, direction.normalized, speed * Time.deltaTime, 1f);
        if (desiredForward == Vector3.zero) return;
        Quaternion _newRotation = Quaternion.LookRotation(desiredForward);
        human.transform.rotation = _newRotation;
    }


    #endregion
}