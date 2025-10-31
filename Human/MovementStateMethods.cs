using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MovementStateMethods
{
    public static void TriggerRotateAnimation(Humanoid human, bool isRight)
    {
        if (human._LastTimeRotated + 1f > Time.time) return;

        human._LastTimeRotated = Time.time;
        if (isRight)
        {
            human.ChangeAnimation("TurnRightLocomotion");
        }
        else
        {
            human.ChangeAnimation("TurnLeftLocomotion");
        }
    }
    public static void ControlLocomotionType(Humanoid human)
    {
        bool isInCrouch = human._MovementState is Crouch;
        bool isAiming = human._IsInCombatMode;

        if (human._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(human) && human._IsGrounded && (human._RunInput || human._SprintInput))
            isAiming = false;

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
        human._LocomotionSystem.inputSmooth = Vector3.Lerp(human._LocomotionSystem.inputSmooth, human._DirectionInput, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.fixedDeltaTime);

        if (human._IsStrafing)
        {
            Vector3 aimTargetDirection = (human is Player pl) ? (pl._LookAtForCam.position - human.transform.position).normalized : ((human as NPC)._AimPosition - human.transform.position).normalized;
            Vector2 first = new Vector2(human.transform.forward.x, human.transform.forward.z);
            Vector2 second = new Vector2(aimTargetDirection.x, aimTargetDirection.z);
            float angle = Vector2.Angle(first, second);
            bool isRight = Vector2.SignedAngle(first, second) < 0 ? true : false;

            if (human._Rigidbody.linearVelocity.magnitude >= 0.1f || angle > 25f || human._LastTimeRotated + 0.25f > Time.time)
                RotateToDirection(aimTargetDirection, human, rotationSpeed);

            if (angle > 25f && human._Rigidbody.linearVelocity.magnitude < 0.1f)
                MovementStateMethods.TriggerRotateAnimation(human, isRight);
        }
        else
            RotateToDirection(human._LocomotionSystem.moveDirection, human, rotationSpeed);
    }

    public static void UpdateAnimator(Humanoid human)
    {
        human._LocomotionSystem.UpdateAnimator();
    }
    public static void UpdateMoveDirection(Humanoid human, Transform referenceTransform)
    {
        if (human is Player)
        {
            if (human._DirectionInput.magnitude <= 0.01)
            {
                human._LocomotionSystem.moveDirection = Vector3.Lerp(human._LocomotionSystem.moveDirection, Vector3.zero, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.deltaTime);
                return;
            }

            Vector3 right = referenceTransform.right;
            right.y = 0;
            Vector3 forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
            Vector3 up = Vector3.zero;
            if (human._MovementState is Prone)
            {
                up = human.transform.forward;
            }
            else if (human._MovementState is Locomotion)
            {
                float slopeAngle = Mathf.Acos(human._LocomotionSystem.groundHit.normal.y) * Mathf.Rad2Deg;
                float slopeFactor = Mathf.Clamp(slopeAngle / 15f, 0f, 3f);
                Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, human._LocomotionSystem.groundHit.normal).normalized;
                Vector3 inputDir = (human._LocomotionSystem.inputSmooth.x * right) + (human._LocomotionSystem.inputSmooth.y * forward);
                Vector3 moveDirFlat = Vector3.ProjectOnPlane(inputDir, Vector3.up).normalized;
                slopeFactor = Vector3.Dot(moveDirFlat, slopeDir) < 0f ? slopeFactor : 0f;
                up = slopeFactor * Vector3.up * 0.35f;
            }

            human._LocomotionSystem.moveDirection = (human._LocomotionSystem.inputSmooth.x * right) + (human._LocomotionSystem.inputSmooth.y * forward) + new Vector3(0f, up.y, 0f);
        }
        else if (human is NPC npc)
        {
            npc.ArrangeMovementCorners();
            if (npc._DirectionInput.magnitude < 0.15f)
            {
                npc._LocomotionSystem.moveDirection = Vector3.Lerp(npc._LocomotionSystem.moveDirection, Vector3.zero, (npc._IsStrafing ? npc._LocomotionSystem.AimingMovementSetting.movementSmooth : npc._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.deltaTime);
                return;
            }
            Vector3 up = Vector3.zero;
            if (human._MovementState is Prone)
            {
                up = human.transform.forward;
            }
            else if (human._MovementState is Locomotion)
            {
                float slopeAngle = Mathf.Acos(human._LocomotionSystem.groundHit.normal.y) * Mathf.Rad2Deg;
                float slopeFactor = Mathf.Clamp(slopeAngle / 15f, 0f, 3f);
                Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, human._LocomotionSystem.groundHit.normal).normalized;
                Vector3 inputDir = (npc._DirectionInput.x * Vector3.right) + (npc._DirectionInput.y * Vector3.forward);
                Vector3 moveDirFlat = Vector3.ProjectOnPlane(inputDir, Vector3.up).normalized;
                slopeFactor = Vector3.Dot(moveDirFlat, slopeDir) < 0f ? slopeFactor : 0f;
                up = slopeFactor * Vector3.up * 0.5f;
            }

            npc._LocomotionSystem.moveDirection = new Vector3(GameManager._Instance.Vector2ToVector3(npc._DirectionInput).normalized.x, up.y, GameManager._Instance.Vector2ToVector3(npc._DirectionInput).normalized.z);
        }
    }

    private static bool IsMovingForward(Humanoid human)
    {
        Vector2 first = new Vector2(human._Rigidbody.linearVelocity.x, human._Rigidbody.linearVelocity.z).normalized;
        Vector2 second = new Vector2(human.transform.forward.x, human.transform.forward.z).normalized;
        return Vector2.Dot(first, second) > 0.85f;
    }
    public static bool IsInDeepWater(Humanoid human)
    {
        float yLevel = Mathf.Max(human.transform.position.y + 1f, WorldHandler._Instance._SeaLevel + 0.5f);
        if (Physics.Raycast(new Vector3(human.transform.position.x, yLevel, human.transform.position.z), -Vector3.up, out RaycastHit hit, 1f, GameManager._Instance._TerrainSolidAndWaterMask))
            if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                Physics.Raycast(hit.point + Vector3.up * 0.1f, -Vector3.up, out hit, 1.5f, GameManager._Instance._TerrainAndSolidMask);
                if (hit.collider == null)
                    return true;
            }

        return false;
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
        bool sprintConditions;
        if (player._MovementState is Swim)
        {
            player._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            sprintConditions = player._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(player) && IsMovingForward(player);
            if ((sprintInput || runInput) && sprintConditions)
                player._IsSprinting = true;
            else
                player._IsSprinting = false;
            return;
        }

        sprintConditions = player._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(player) && player._IsGrounded && IsMovingForward(player) && player._MovementState is Locomotion;

        if ((sprintInput || runInput) && sprintConditions)
        {
            bool isVelocityEnoughForSprint = player._Rigidbody.linearVelocity.magnitude > (player._IsStrafing ? player._LocomotionSystem.AimingMovementSetting.runningSpeed : player._LocomotionSystem.FreeMovementSetting.runningSpeed) - 2f;
            if (sprintInput && isVelocityEnoughForSprint)
            {
                player._LocomotionSystem.FreeMovementSetting.walkByDefault = false;
            }

            if (player._DirectionInput.sqrMagnitude > 0.1f && !player._IsSprinting)
            {
                player._IsSprinting = true;
            }
            else if (player._DirectionInput.sqrMagnitude <= 0.1f && player._IsSprinting)
            {
                player._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
                player._IsSprinting = false;
            }
        }
        else if (player._IsSprinting)
        {
            player._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            player._IsSprinting = false;
        }
    }
    private static void CheckSprintForNPC(bool runInput, bool sprintInput, NPC npc)
    {
        bool sprintConditions;
        if (npc._MovementState is Swim)
        {
            npc._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            sprintConditions = npc._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(npc) && IsMovingForward(npc) && new Vector3((npc._LastCornerFromPath - npc.transform.position).x, 0f, (npc._LastCornerFromPath - npc.transform.position).z).magnitude > 0.7f;
            if ((sprintInput || runInput) && sprintConditions)
                npc._IsSprinting = true;
            else
                npc._IsSprinting = false;
            return;
        }

        sprintConditions = npc._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(npc) && npc._IsGrounded && IsMovingForward(npc) && npc._MovementState is Locomotion && new Vector3((npc._LastCornerFromPath - npc.transform.position).x, 0f, (npc._LastCornerFromPath - npc.transform.position).z).magnitude > 0.7f;

        if ((sprintInput || runInput) && sprintConditions)
        {
            bool isVelocityEnoughForSprint = npc._Rigidbody.linearVelocity.magnitude > (npc._IsStrafing ? npc._LocomotionSystem.AimingMovementSetting.runningSpeed : npc._LocomotionSystem.FreeMovementSetting.runningSpeed) - 2f;
            if (sprintInput && isVelocityEnoughForSprint)
            {
                npc._LocomotionSystem.FreeMovementSetting.walkByDefault = false;
            }

            if (npc._Rigidbody.linearVelocity.sqrMagnitude > 0.1f && !npc._IsSprinting)
            {
                npc._IsSprinting = true;
            }
            else if (npc._Rigidbody.linearVelocity.sqrMagnitude <= 0.1f && npc._IsSprinting)
            {
                npc._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
                npc._IsSprinting = false;
            }
        }
        else if (npc._IsSprinting)
        {
            npc._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            npc._IsSprinting = false;
        }
    }
    #region Jump Methods
    public static void CheckJump(Humanoid human)
    {
        if (human._JumpInput && JumpConditions(human))
        {
            if (human is Player)
                GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._JumpBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._JumpCoroutine);
            Jump(human);
        }
    }

    private static bool JumpConditions(Humanoid human)
    {
        return human._IsGrounded && MovementStateMethods.GroundAngle(human) < human._LocomotionSystem.slopeLimit && !human._IsJumping && !human._StopMove && !human._IsInCombatMode;
    }

    private static void Jump(Humanoid human)
    {
        if (human is Player pl) { pl._LastJumpedPosition = pl._LookAtForCam.position; pl._LastJumpedTime = Time.time; }

        // trigger jump behaviour
        human._JumpCounter = human._JumpTimer;
        human._IsJumping = true;

        // trigger jump animations
        if (human._Rigidbody.linearVelocity.magnitude < 2f)
            human.ChangeAnimation("Jump", 0.1f);
        else
            human.ChangeAnimation("JumpMove", 0.2f);
    }


    public static void ControlJumpBehaviour(Humanoid human)
    {
        if (!human._IsJumping) return;

        human._JumpCounter -= Time.fixedDeltaTime;
        if (human._JumpCounter <= 0)
        {
            human._JumpCounter = 0;
            human._IsJumping = false;
        }

        human._IsSprinting = true;//
        var vel = human._Rigidbody.linearVelocity;
        vel.y = human._LocomotionSystem.jumpHeight;

        human._Rigidbody.linearVelocity = vel;
    }

    public static void AirControl(Humanoid human)
    {
        if ((human._IsGrounded && !human._IsJumping)) return;
        if (human.transform.position.y > human._LocomotionSystem.heightReached) human._LocomotionSystem.heightReached = human.transform.position.y;

        human._LocomotionSystem.inputSmooth = Vector3.Lerp(human._LocomotionSystem.inputSmooth, human._DirectionInput, human._LocomotionSystem.airSmooth * Time.fixedDeltaTime);

        if (human._LocomotionSystem.jumpWithRigidbodyForce && !human._IsGrounded)
        {
            human._Rigidbody.AddForce(human._LocomotionSystem.moveDirection * human._LocomotionSystem.airSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
            return;
        }

        human._LocomotionSystem.moveDirection.y = 0;
        human._LocomotionSystem.moveDirection.x = Mathf.Clamp(human._LocomotionSystem.moveDirection.x, -1f, 1f);
        human._LocomotionSystem.moveDirection.z = Mathf.Clamp(human._LocomotionSystem.moveDirection.z, -1f, 1f);

        Vector3 targetPosition = human._Rigidbody.position + (human._LocomotionSystem.moveDirection * human._LocomotionSystem.airSpeed) * Time.fixedDeltaTime;
        Vector3 targetVelocity = (targetPosition - human.transform.position) / Time.fixedDeltaTime;

        targetVelocity.y = human._Rigidbody.linearVelocity.y;
        human._Rigidbody.linearVelocity = Vector3.Lerp(human._Rigidbody.linearVelocity, targetVelocity, human._LocomotionSystem.airSmooth * Time.fixedDeltaTime);
    }



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
                human._Rigidbody.AddForce(human.transform.up * (human._LocomotionSystem.extraGravity * 2 * Time.fixedDeltaTime), ForceMode.VelocityChange);

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
                    human._Rigidbody.AddForce(human.transform.up * human._LocomotionSystem.extraGravity * Time.fixedDeltaTime, ForceMode.VelocityChange);
                }
            }
            else if (!human._IsJumping)
            {
                human._Rigidbody.AddForce(human.transform.up * (human._LocomotionSystem.extraGravity * 2 * Time.fixedDeltaTime), ForceMode.VelocityChange);
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
            float dist = 10f;
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



    #endregion

    #region Locomotion

    private static void SetControllerMoveSpeed(LocomotionSystem.MovementSetting speed, Humanoid human)
    {
        float targetSpeed;
        if (speed.walkByDefault)
            targetSpeed = human._IsSprinting ? speed.runningSpeed : speed.walkSpeed;
        else
            targetSpeed = human._IsSprinting ? speed.sprintSpeed : speed.runningSpeed;

        if (human._MovementState is Swim && human._IsSprinting) targetSpeed *= 0.8f;

        float lerpMultiplier = (human._LocomotionSystem.moveSpeed > targetSpeed) ? 1.5f : 1f;
        human._LocomotionSystem.moveSpeed = Mathf.MoveTowards(human._LocomotionSystem.moveSpeed, targetSpeed, speed.movementSmooth * lerpMultiplier * Time.fixedDeltaTime);
    }

    private static void MoveCharacter(Vector3 direction, Humanoid human)
    {
        human._LocomotionSystem.inputSmooth = Vector3.Lerp(human._LocomotionSystem.inputSmooth, human._DirectionInput, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.fixedDeltaTime);

        if (!human._IsGrounded || human._IsJumping) return;

        float yVel = human._Rigidbody.linearVelocity.y;
        if (human._MovementState is Locomotion)
        {
            yVel = direction.y;
            direction.y = 0;
        }

        direction.x = Mathf.Clamp(direction.x, -1f, 1f);
        direction.z = Mathf.Clamp(direction.z, -1f, 1f);
        if (direction.magnitude > 1f)
            direction.Normalize();

        Vector3 targetVelocity = direction * (human._StopMove ? 0 : human._LocomotionSystem.moveSpeed * human._LocomotionSystem.MovementSpeedMultiplier);

        bool useVerticalVelocity = !(human._MovementState is Prone);
        if (useVerticalVelocity) targetVelocity.y = yVel;
        human._Rigidbody.linearVelocity = targetVelocity;
    }

    public static void CheckSlopeLimit(Humanoid human)
    {
        if (human._DirectionInput.sqrMagnitude < 0.1) return;
        //if (human is Player && (human as Player).DirectionInput.sqrMagnitude < 0.1) return;
        //if (human is NPC && (human as NPC).GetVelocity().sqrMagnitude < 0.1) return;

        RaycastHit hitinfo;
        float hitAngle = 0f;

        if (Physics.Linecast(human.transform.position + Vector3.up * (human._LocomotionSystem._defaultCollider.height * 0.5f), human.transform.position + human._LocomotionSystem.moveDirection.normalized * (human._LocomotionSystem._defaultCollider.radius + 0.2f), out hitinfo, human._LocomotionSystem.groundLayer))
        {
            hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

            Vector3 targetPoint = hitinfo.point + human._LocomotionSystem.moveDirection.normalized * human._LocomotionSystem._defaultCollider.radius;
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



    private static void RotateToDirection(Vector3 direction, Humanoid human, float speed = 0f)
    {
        if (speed == 0f)
            speed = human._AimSpeed * 1.4f;
        direction.y = 0f;
        Vector3 desiredForward;
        if (human._IsStrafing)
            desiredForward = Vector3.RotateTowards(human.transform.forward, direction.normalized, speed * Time.fixedDeltaTime, 1f);
        else
            desiredForward = direction.normalized;
        if (desiredForward == Vector3.zero) return;
        Vector3 up = Vector3.up;
        if (human._MovementState is Prone)
        {
            if (Physics.Raycast(human.transform.position + Vector3.up * 5f, -Vector3.up, out RaycastHit hit, 20f, LayerMask.GetMask("Terrain")))
                up = hit.normal;
        }
        Vector3 right = Vector3.Cross(up, desiredForward).normalized;
        desiredForward = Vector3.Cross(right, up).normalized;
        Quaternion lookRot = Quaternion.LookRotation(desiredForward, up);

        if (human._IsStrafing)
            human.transform.rotation = lookRot;
        else
            human.transform.rotation = Quaternion.Lerp(human.transform.rotation, lookRot, Time.fixedDeltaTime * speed);
    }

    #endregion
}