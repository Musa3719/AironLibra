using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MovementStateMethods
{
    public static void TriggerRotateAnimation(Humanoid human, bool isRight)
    {
        if (human._LastTimeRotated + 0.3f > Time.time) return;
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
        bool isInCrouch = human._MovementState is CrouchMoveState;
        bool isAiming = human._IsInCombatMode;

        if (human._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(human) && human._IsGrounded && (human._IsInFastWalkMode || human._SprintInput))
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

        if (human._IsStrafing && !CameraController._Instance._IsInCoolAngleMode)
        {
            Vector3 aimTargetDirection = (human is Player pl) ? (pl._LookAtForCam.position - human.transform.position).normalized : ((human as NPC)._AimPosition - human.transform.position).normalized;
            Vector2 first = new Vector2(human.transform.forward.x, human.transform.forward.z);
            Vector2 second = new Vector2(aimTargetDirection.x, aimTargetDirection.z);
            float angle = Vector2.Angle(first, second);
            bool isRight = Vector2.SignedAngle(first, second) < 0 ? true : false;

            //if (human._IsAttacking || human._Rigidbody.linearVelocity.magnitude >= 0.1f || angle > 25f || human._LastTimeRotated + 0.25f > Time.time)
            RotateToDirection(aimTargetDirection, human, rotationSpeed);

            if (angle > 4f && human._Rigidbody.linearVelocity.magnitude < 0.1f)
                TriggerRotateAnimation(human, isRight);
        }
        else
            RotateToDirection(human._LocomotionSystem.moveDirection, human, rotationSpeed);
    }

    public static void UpdateAnimator(Humanoid human)
    {
        human._LocomotionSystem.UpdateAnimator();
    }
    public static void AttackMove(Humanoid human)
    {
        if (human._HealthSystem.GetHealthState() != HealthState.Healthy) return;

        human._FootIKComponent.SetTargetWeight(0f);
        human._LastTimeDodged = Time.time;
        if (human._AttackReadyTime > human._HeavyAttackThreshold)
            human.ChangeAnimation("AttackMove", 0.1f);
        Vector3 dir = human.transform.forward;
        float attackMoveSpeed = Mathf.Clamp(human._AttackReadyTime < 0.35f ? 0.25f : human._AttackReadyTime, 0f, 0.7f) * 6f;
        human._Rigidbody.linearVelocity = Vector3.ProjectOnPlane(dir, human._LocomotionSystem.groundHit.normal).normalized * attackMoveSpeed;
        GameManager._Instance.CoroutineCall(ref human._AttackMoveCoroutine, human.AttackMoving(), GameManager._Instance);
    }
    public static void UpdateMoveDirection(Humanoid human, Transform referenceTransform)
    {
        if (human._DirectionInput.magnitude <= 0.01 || human._IsInAttackReady)
        {
            human._LocomotionSystem.moveDirection = Vector3.Lerp(human._LocomotionSystem.moveDirection, Vector3.zero, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.deltaTime * 2.5f);
            return;
        }

        NPC npc = human as NPC;
        Vector3 right = referenceTransform.right;
        right.y = 0;
        Vector3 forward = Quaternion.AngleAxis(-90, Vector3.up) * right;

        if (human is Player)
        {
            human._LocomotionSystem.moveDirection = (human._LocomotionSystem.inputSmooth.x * right) + (human._LocomotionSystem.inputSmooth.y * forward);
        }
        else if (npc != null)
        {
            npc.ArrangeMovementCorners();
            npc._LocomotionSystem.moveDirection = (npc._DirectionInput.x * right) + (npc._DirectionInput.y * forward);
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
        if (human._Stamina > 0 && Time.time > 2f + human._WaitForRunLastTriggerTime) return true;
        else if (Time.time > 2f + human._WaitForRunLastTriggerTime)
        {
            human._WaitForRunLastTriggerTime = Time.time;
            return false;
        }
        return false;
    }
    public static void CheckSprint(Humanoid human)
    {
        if (human is Player)
            CheckSprintForPlayer(human._IsInFastWalkMode, human._SprintInput, human as Player);
        else
            CheckSprintForNPC(human._IsInFastWalkMode, human._SprintInput, human as NPC);
    }
    private static void CheckSprintForPlayer(bool runInput, bool sprintInput, Player player)
    {
        bool sprintConditions;
        if (player._MovementState is SwimMoveState)
        {
            player._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            sprintConditions = player._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(player) && IsMovingForward(player) && !player._HealthSystem._IsUnhealthy;
            if ((sprintInput || runInput) && sprintConditions)
                player._IsSprinting = true;
            else
                player._IsSprinting = false;
            return;
        }

        sprintConditions = player._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(player) && player._IsGrounded && IsMovingForward(player) && !player._HealthSystem._IsUnhealthy && player._MovementState is LocomotionState && !(player._HandState is CarryHandState);

        if (!(runInput && sprintConditions) && !player._IsSprinting)
        {
            player._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
        }
        else
        {
            player._LocomotionSystem.FreeMovementSetting.walkByDefault = false;
        }

        bool isVelocityEnoughForSprint = player._Rigidbody.linearVelocity.magnitude > (player._IsStrafing ? player._LocomotionSystem.AimingMovementSetting.runningSpeed : player._LocomotionSystem.FreeMovementSetting.runningSpeed) - 2f;
        if (sprintInput && sprintConditions && isVelocityEnoughForSprint)
        {
            player._IsSprinting = true;
        }
        else
        {
            player._IsSprinting = false;
        }
    }
    private static void CheckSprintForNPC(bool runInput, bool sprintInput, NPC npc)
    {
        bool sprintConditions;
        if (npc._MovementState is SwimMoveState)
        {
            npc._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            sprintConditions = npc._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(npc) && IsMovingForward(npc) && !npc._HealthSystem._IsUnhealthy && new Vector3((npc._LastCornerFromPath - npc.transform.position).x, 0f, (npc._LastCornerFromPath - npc.transform.position).z).magnitude > 0.7f;
            if ((sprintInput || runInput) && sprintConditions)
                npc._IsSprinting = true;
            else
                npc._IsSprinting = false;
            return;
        }

        sprintConditions = npc._DirectionInput.sqrMagnitude > 0.1f && CanRunWithStamina(npc) && npc._IsGrounded && IsMovingForward(npc) && !npc._HealthSystem._IsUnhealthy && npc._MovementState is LocomotionState && !(npc._HandState is CarryHandState) && new Vector3((npc._LastCornerFromPath - npc.transform.position).x, 0f, (npc._LastCornerFromPath - npc.transform.position).z).magnitude > 0.7f;

        if ((sprintInput || runInput) && sprintConditions)
        {
            bool isVelocityEnoughForSprint = npc._Rigidbody.linearVelocity.magnitude > (npc._IsStrafing ? npc._LocomotionSystem.AimingMovementSetting.runningSpeed : npc._LocomotionSystem.FreeMovementSetting.runningSpeed) - 2f;
            if (sprintInput && isVelocityEnoughForSprint)
            {
                npc._LocomotionSystem.FreeMovementSetting.walkByDefault = false;
            }

            if (npc._Rigidbody.linearVelocity.sqrMagnitude > 0.1f && !npc._IsSprinting && sprintInput)
            {
                npc._IsSprinting = true;
            }
            else if (npc._Rigidbody.linearVelocity.sqrMagnitude <= 0.1f && npc._IsSprinting)
            {
                npc._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
                npc._IsSprinting = false;
            }
            else if (npc._IsSprinting && !sprintInput)
            {
                npc._IsSprinting = false;
            }
        }
        else if (npc._IsSprinting)
        {
            npc._LocomotionSystem.FreeMovementSetting.walkByDefault = true;
            npc._IsSprinting = false;
        }
    }

    #region Jump And Dodge Methods
    public static void Stagger(Humanoid human, float second, Vector3 dir)
    {
        //////////////////////////
        human._IsStaggered = true;
        human.StopCombatActions();
        human.ChangeAnimation("Stagger", 0.1f);
        human._Stamina -= 20f;
        GameManager._Instance.CoroutineCall(ref human._StaggerCoroutine, human.Staggering(second), GameManager._Instance);
    }
    public static void CheckDodge(Humanoid human)
    {
        if (human._DodgeInput && DodgeConditions(human))
        {
            if (human is Player)
                GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._DodgeBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._DodgeCoroutine);
            else
                human._DodgeInput = false;

            Dodge(human);
        }
    }
    public static bool DodgeConditions(Humanoid human)
    {
        if (!human._IsInCombatMode || human._HealthSystem._IsUnhealthy || human._IsDodging || human._IsStaggered || !human._IsGrounded || human._LastTimeDodged + 0.5f > Time.time || human._Stamina < 25f) return false;

        return true;
    }
    public static void Dodge(Humanoid human)
    {
        human._FootIKComponent.SetTargetWeight(0f);
        human._Stamina -= 25f;
        human._IsDodging = true;
        human._LastTimeDodged = Time.time;
        human.ChangeAnimation("Dodge", 0.1f);
        Vector3 dir = human._DirectionInput.magnitude > 0.1f ? GameManager._Instance.Vector2ToVector3(human._DirectionInput) : -human.transform.forward;
        human._Rigidbody.linearVelocity = Vector3.ProjectOnPlane(dir, human._LocomotionSystem.groundHit.normal).normalized * human._LocomotionSystem.dodgeSpeed;
        GameManager._Instance.CoroutineCall(ref human._DodgeMoveCoroutine, human.Dodging(), GameManager._Instance);
    }
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
        return human._IsGrounded && !human._HealthSystem._IsUnhealthy && MovementStateMethods.GroundAngle(human) < human._LocomotionSystem.slopeLimit && !human._IsJumping && !human._StopMove && !human._IsInCombatMode && !human._IsDodging && !human._IsStaggered && human._Stamina > 40f;
    }

    private static void Jump(Humanoid human)
    {
        if (human is Player pl) { pl._LastJumpedPosition = pl._LookAtForCam.position; pl._LastJumpedTime = Time.time; }

        human._Stamina -= 30f;
        // trigger jump behaviour
        human._JumpCounter = human._JumpTimer;
        human._IsJumping = true;

        var vel = human._Rigidbody.linearVelocity;
        vel.y = human._LocomotionSystem.jumpHeight;
        human._Rigidbody.linearVelocity = vel;

        // trigger jump animations
        float flatVelMagnitude = new Vector3(human._Rigidbody.linearVelocity.x, 0f, human._Rigidbody.linearVelocity.z).magnitude;
        if (flatVelMagnitude < 0.3f)
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

        //human._IsSprinting = true;

        /*var vel = human._Rigidbody.linearVelocity;
        vel.y = human._LocomotionSystem.jumpHeight;
        human._Rigidbody.linearVelocity = vel;*/
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
        //ControlMaterialPhysics(human);

        if (human._LocomotionSystem.groundDistance <= human._LocomotionSystem.groundMinDistance)
        {
            if (!human._IsGrounded) { human._FootIKComponent.SetTargetWeight(1f); GameManager._Instance.CallForAction(() => { human._MainCollider.height = 2f; }, 0.15f, false); }
            human._IsGrounded = true;
            if (!human._IsJumping && human._LocomotionSystem.groundDistance > 0.05f)
                human._Rigidbody.AddForce(human.transform.up * (human._LocomotionSystem.extraGravity * 2 * Time.fixedDeltaTime), ForceMode.VelocityChange);

            human._LocomotionSystem.heightReached = human.transform.position.y;
        }
        else
        {
            if (human._LocomotionSystem.groundDistance >= human._LocomotionSystem.groundMaxDistance)
            {
                if (human._IsGrounded) { human._MainCollider.height = 1.5f; human._FootIKComponent.SetTargetWeight(0f); }
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
        if (human._LocomotionSystem._defaultCollider != null)
        {
            // radius of the SphereCast
            float radius = human._LocomotionSystem._defaultCollider.radius * 0.9f;
            float dist = 10f;
            // ray for RayCast
            Ray ray2 = new Ray(human.transform.position + new Vector3(0, human._LocomotionSystem.colliderHeight / 2, 0) + human._Rigidbody.linearVelocity.normalized * 0.15f, Vector3.down);
            // raycast for check the ground distance
            if (Physics.Raycast(ray2, out human._LocomotionSystem.forwardGroundHit, (human._LocomotionSystem.colliderHeight / 2) + dist, human._LocomotionSystem.groundLayer) && !human._LocomotionSystem.forwardGroundHit.collider.isTrigger)
                dist = (human.transform.position + human._Rigidbody.linearVelocity.normalized * 0.15f).y - human._LocomotionSystem.forwardGroundHit.point.y;
            // sphere cast around the base of the capsule to check the ground distance
            if (dist >= human._LocomotionSystem.groundMinDistance)
            {
                Vector3 pos = human.transform.position + Vector3.up * (human._LocomotionSystem._defaultCollider.radius) + human._Rigidbody.linearVelocity.normalized * 0.15f;
                Ray ray = new Ray(pos, -Vector3.up);
                if (Physics.SphereCast(ray, radius, out human._LocomotionSystem.forwardGroundHit, human._LocomotionSystem._defaultCollider.radius + human._LocomotionSystem.groundMaxDistance, human._LocomotionSystem.groundLayer) && !human._LocomotionSystem.forwardGroundHit.collider.isTrigger)
                {
                    Physics.Linecast(human._LocomotionSystem.forwardGroundHit.point + (Vector3.up * 0.1f), human._LocomotionSystem.forwardGroundHit.point + Vector3.down * 0.15f, out human._LocomotionSystem.forwardGroundHit, human._LocomotionSystem.groundLayer);
                    float newDist = (human.transform.position + human._Rigidbody.linearVelocity.normalized * 0.15f).y - human._LocomotionSystem.forwardGroundHit.point.y;
                    if (dist > newDist) dist = newDist;
                }
            }
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

        if (human._MovementState is SwimMoveState && human._IsSprinting) targetSpeed *= 0.8f;

        float lerpMultiplier = (human._LocomotionSystem.moveSpeed > targetSpeed) ? 1.5f : 1f;
        human._LocomotionSystem.moveSpeed = Mathf.MoveTowards(human._LocomotionSystem.moveSpeed, targetSpeed, speed.movementSmooth * lerpMultiplier * Time.fixedDeltaTime);
    }

    private static void MoveCharacter(Vector3 direction, Humanoid human)
    {
        if (!human._IsGrounded || human._IsJumping || human._IsDodging || human._IsStaggered || human._IsAttacking) { human._LocomotionSystem.inputSmooth = Vector3.zero; return; }

        human._LocomotionSystem.inputSmooth = Vector3.Lerp(human._LocomotionSystem.inputSmooth, human._DirectionInput, (human._IsStrafing ? human._LocomotionSystem.AimingMovementSetting.movementSmooth : human._LocomotionSystem.FreeMovementSetting.movementSmooth) * Time.fixedDeltaTime);

        direction.x = Mathf.Clamp(direction.x, -1f, 1f);
        direction.z = Mathf.Clamp(direction.z, -1f, 1f);
        if (direction.magnitude > 1f)
            direction.Normalize();

        float speed = human._StopMove ? 0 : human._LocomotionSystem.moveSpeed * human._LocomotionSystem.MovementSpeedMultiplier;
        Vector3 targetVelocity = Vector3.ProjectOnPlane(direction * speed, human._LocomotionSystem.forwardGroundHit.normal).normalized * speed;//(speed + human._SlopeSpeedAdder);
        if (direction.sqrMagnitude > 0.05f)
        {
            if (human._MovementState is SwimMoveState)
                human._Rigidbody.linearVelocity = new Vector3(targetVelocity.x, human._Rigidbody.linearVelocity.y, targetVelocity.z);
            else
                human._Rigidbody.linearVelocity = targetVelocity;
        }
        else if (direction.sqrMagnitude < 0.05f)
            human._Rigidbody.linearVelocity = new Vector3(0f, human._Rigidbody.linearVelocity.y, 0f);
    }

    public static void CheckSlopeLimit(Humanoid human)
    {
        if (human._DirectionInput.sqrMagnitude < 0.1) return;
        //if (human is Player && (human as Player).DirectionInput.sqrMagnitude < 0.1) return;
        //if (human is NPC && (human as NPC).GetVelocity().sqrMagnitude < 0.1) return;

        RaycastHit hitinfo;
        float hitAngle = 0f;

        Vector3 normalizedMoveDir = human._LocomotionSystem.moveDirection;
        normalizedMoveDir.y = 0f;
        normalizedMoveDir.Normalize();
        if (Physics.Linecast(human.transform.position + Vector3.up * (human._LocomotionSystem._defaultCollider.height * 0.5f), human.transform.position + normalizedMoveDir * (human._LocomotionSystem._defaultCollider.radius + 0.2f), out hitinfo, human._LocomotionSystem.groundLayer))
        {
            hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

            Vector3 targetPoint = hitinfo.point + normalizedMoveDir * human._LocomotionSystem._defaultCollider.radius;
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
        if (human._MovementState is ProneMoveState)
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