using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public static class HandStateMethods
{
    private static Item FindProjectileType(Humanoid human)
    {
        bool isCrossbow;
        switch (GetCurrentWeaponType(human, human._IsAttackingFromLeftHandWeapon))
        {
            case WeaponType.Bow:
                isCrossbow = false;
                break;
            case WeaponType.Crossbow:
                isCrossbow = true;
                break;
            default:
                Debug.LogError("Weapon Type Wrong For Projectile!");
                return null;
        }

        int length = human._Inventory._Items.Count;
        for (int i = 0; i < length; i++)
        {
            if ((isCrossbow && human._Inventory._Items[i]._ItemDefinition is BoltArrow) || (!isCrossbow && human._Inventory._Items[i]._ItemDefinition is Arrow))
            {
                return human._Inventory._Items[i];
            }
        }

        if (human._BackCarryItemRef == null) return null;
        length = (human._BackCarryItemRef as CarryItem)._Inventory._Items.Count;
        for (int i = 0; i < length; i++)
        {
            if ((isCrossbow && (human._BackCarryItemRef as CarryItem)._Inventory._Items[i]._ItemDefinition is BoltArrow) || (!isCrossbow && (human._BackCarryItemRef as CarryItem)._Inventory._Items[i]._ItemDefinition is Arrow))
            {
                return (human._BackCarryItemRef as CarryItem)._Inventory._Items[i];
            }
        }
        return null;
    }
    public static Item SeperateOneCountForProjectile(Item originalItem)
    {
        if (originalItem == null || originalItem._Count < 1) return null;

        Item seperatedProjectileItem = originalItem.Copy();
        seperatedProjectileItem._Count = 1;
        seperatedProjectileItem._AttachedInventory = null;
        Inventory tempInventory = originalItem._AttachedInventory;
        originalItem._Count -= 1;
        if (originalItem._Count == 0)
            originalItem.DropFrom(false);

        originalItem.SetCurrentCarryCapacityUse(tempInventory);
        if (tempInventory.IsInventoryVisibleInScreen())
            GameManager._Instance.UpdateInventoryUIBuffer();

        return seperatedProjectileItem;
    }
    public static void CheckRangedAttack(Humanoid human)
    {
        if (!human._IsInCombatMode) return;
        if (!(human._HandState is RangedWeaponHandState state) || !human._IsInRangedAim) return;

        if (human._LightAttackInput && IsAttackPossible(human))
        {
            if (!state._RangedWeapon._IsLoaded) return;

            human._LightAttackInput = false;
            RangedAttack(human, GetCurrentWeaponType(human, human._IsAttackingFromLeftHandWeapon));
        }
        else if (human._HeavyAttackInput && IsAttackPossible(human))
        {
            if (!state._RangedWeapon._IsLoaded) return;

            human._HeavyAttackInput = false;
            RangedAttack(human, GetCurrentWeaponType(human, human._IsAttackingFromLeftHandWeapon));
        }
        else if (human._KickInput && IsAttackPossible(human))
        {
            human._KickInput = false;
            KickOrPunchAttack(human, human._HeavyAttackMultiplier, GetCurrentWeaponType(human, human._IsAttackingFromLeftHandWeapon), true);
        }
    }
    private static void RangedAttack(Humanoid human, WeaponType weaponType)
    {
        if (human._IsAttacking)
            AttackIsOver(human, human._LastAttackWeapon);

        Item handEquippedItemRef = human._IsAttackingFromLeftHandWeapon ? human._LeftHandEquippedItemRef : human._RightHandEquippedItemRef;
        if (handEquippedItemRef != null && weaponType != WeaponType.None && handEquippedItemRef is WeaponItem weaponItem)
        {
            var handle = weaponItem._SpawnedHandle;
            if (handle.HasValue && handle.Value.IsValid() && handle.Value.IsDone && handle.Value.Result != null)
            {
                human._FootIKComponent.SetTargetWeight(0f);
                human._Stamina -= 15f;
                human._IsAttacking = true;
                human._IsInAttackReady = false;
                human._LastTimeAttacked = Time.timeAsDouble;
                human._WaitTimeForNextAttack = 1f * GetAttackWaitMultiplierForHumanAgi(human) / (weaponItem._ItemDefinition as IRangedWeapon)._BaseSpeed;
                float attackSpeedMultiplier = 1f;/// human._WaitTimeForNextAttack;
                human.ChangeAnimation(human._LastReadyAnimName, 0.1f, attackSpeedMultiplier, human._IsAttackingFromLeftHandWeapon ? 4 : 3);
                handle.Value.Result.GetComponent<RangedWeapon>().Attack();
                //MovementStateMethods.AttackMove(human);
            }
        }
        else
        {
            KickOrPunchAttack(human, 1f, weaponType);
        }

    }
    public static void ArrangeReload(Humanoid human)
    {
        if (!human._ReloadInput && !human._AimInput) return;
        RangedWeapon rangedWeapon = (human._HandState as RangedWeaponHandState)._RangedWeapon;
        if (rangedWeapon._IsCrossbow && human._ReloadInput && IsReloadPossible(human))
        {
            human._ReloadInput = false;
            (human._HandState as RangedWeaponHandState)._IsReloading = true;
            rangedWeapon.StartReloading(human, FindProjectileType(human));
        }
        else if (!rangedWeapon._IsCrossbow && human._AimInput && IsReloadPossible(human))
        {
            rangedWeapon.StartReloading(human, FindProjectileType(human));
        }
    }
    private static bool IsReloadPossible(Humanoid human)
    {
        if (!human._IsInCombatMode || human._IsAttacking || human._IsJumping || !(human._HandState is RangedWeaponHandState state) || human._IsSprinting || human._IsStaggered || (human._HandState as RangedWeaponHandState)._RangedWeapon != null && (human._HandState as RangedWeaponHandState)._RangedWeapon._IsLoaded) return false;
        Item item = FindProjectileType(human);
        if (item == null || item._Count < 1) return false;

        return true;
    }

    public static void ArrangeRangedAim(Humanoid human)
    {
        if (human._AimInput && IsRangedAimPossible(human))
        {
            if (!human._IsInRangedAim)
                (human._HandState as RangedWeaponHandState)._RangedWeapon.AimStarted(human);
            human._IsInRangedAim = true;
        }
        else
        {
            if (human._IsInRangedAim)
                (human._HandState as RangedWeaponHandState)._RangedWeapon.AimEnded();
            human._IsInRangedAim = false;
        }
    }
    public static bool IsRangedAimPossible(Humanoid human)
    {
        if (!human._IsInCombatMode || human._IsAttacking || human._IsJumping || !(human._HandState is RangedWeaponHandState state) || human._IsSprinting || human._IsStaggered || (human._HandState as RangedWeaponHandState)._RangedWeapon != null && !(human._HandState as RangedWeaponHandState)._RangedWeapon._IsLoaded) return false;

        return true;
    }
    public static RangedWeapon GetRangedWeapon(Humanoid human)
    {
        if (human._RightHandEquippedItemRef?._ItemDefinition is IRangedWeapon && (human._RightHandEquippedItemRef as WeaponItem)._SpawnedHandle.HasValue && (human._RightHandEquippedItemRef as WeaponItem)._SpawnedHandle.Value.Result?.gameObject != null)
            return (human._RightHandEquippedItemRef as WeaponItem)._SpawnedHandle.Value.Result.gameObject.GetComponent<RangedWeapon>();
        if (human._LeftHandEquippedItemRef?._ItemDefinition is IRangedWeapon && (human._LeftHandEquippedItemRef as WeaponItem)._SpawnedHandle.HasValue && (human._LeftHandEquippedItemRef as WeaponItem)._SpawnedHandle.Value.Result?.gameObject != null)
            return (human._LeftHandEquippedItemRef as WeaponItem)._SpawnedHandle.Value.Result.gameObject.GetComponent<RangedWeapon>();
        return null;
    }

    public static void ArrangeLookAtForCamPosition(Player pl, bool isFromRangedWeapon = false)
    {
        float lookForCamDistance = Options._Instance._IsLookForCamDistanceEnabled ? 10f : 1.5f;
        if (!pl._IsStrafing)
        {
            if (CameraController._Instance._IsInCoolAngleMode)
                pl._LookAtForCam.position = Vector3.Lerp(pl._LookAtForCam.position, pl._Transform.position + Vector3.up, Time.deltaTime * 3f);
            else
                pl._LookAtForCam.position = Vector3.Lerp(pl._LookAtForCam.position, pl._Transform.position + Vector3.up + pl.GetVelocity() * lookForCamDistance / 3f * Mathf.Clamp(CameraController._Instance._CameraDistance, 0f, 14f) / 40f, Time.deltaTime * 3f);
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(M_Input.GetMousePosition());
            if (!M_Input.IsLastInputFromGamepadForAim())
            {
                if (Physics.Raycast(ray, out pl._RayForLook, 100f, LayerMask.NameToLayer("PlayerPlane"), QueryTriggerInteraction.Collide))
                {
                    Vector3 distance = pl._RayForLook.point - pl._Transform.position;
                    distance.y = 0f;
                    distance = Vector3.ClampMagnitude(distance, lookForCamDistance);
                    distance = distance * CameraController._Instance._CameraDistance / 40f;
                    if (CameraController._Instance._IsInCoolAngleMode)
                        distance = Vector3.ClampMagnitude(distance, 1.5f);
                    Vector3 targetPos = pl._Transform.position + distance + Vector3.up;
                    pl._LookAtForCam.position = targetPos;
                }
            }
            else
            {
                pl._RayForLook = default;
                Vector2 newVector = M_Input.GetGamepadLookVector();
                Vector3 temp = GameManager._Instance.Vector2ToVector3(newVector);
                temp = Quaternion.Euler(0f, GameManager._Instance._MainCamera.transform.eulerAngles.y, 0f) * temp;
                newVector = new Vector2(temp.x, temp.z);
                if (newVector.magnitude > 0.07f)
                    pl._LastLookVectorForGamepad = newVector;
                if (pl._LastLookVectorForGamepad.magnitude > 1f)
                    pl._LastLookVectorForGamepad.Normalize();
                Vector3 distance = lookForCamDistance * GameManager._Instance.Vector2ToVector3(pl._LastLookVectorForGamepad);
                distance *= CameraController._Instance._CameraDistance / 40f;
                if (CameraController._Instance._IsInCoolAngleMode)
                    distance = Vector3.ClampMagnitude(distance, 1.5f);
                Vector3 targetPos = pl._Transform.position + distance + Vector3.up;
                pl._LookAtForCam.position = targetPos;
            }

            if (isFromRangedWeapon && Physics.Raycast(ray, out pl._RayForLook, 100f, GameManager._Instance._TerrainSolidWaterHumanMask, QueryTriggerInteraction.Collide))
            {
                GameManager._Instance._RangedAimMesh.gameObject.SetActive(true);
                GameManager._Instance._RangedAimMesh.position = pl._RayForLook.collider == null ? GamepadMouse._Instance.GetRangedAimPoint(true) : pl._RayForLook.point;
                GameManager._Instance._RangedAimMesh.forward = pl._RayForLook.collider == null ? GamepadMouse._Instance.GetRangedAimNormal() : pl._RayForLook.normal;
            }
        }

        if (pl._LastJumpedTime + 0.85 > Time.timeAsDouble) { pl._LookAtForCam.position = new Vector3(pl._LookAtForCam.position.x, pl._LastJumpedPosition.y, pl._LookAtForCam.position.z); return; }
    }

    public static void ArrangeBlock(Humanoid human)
    {
        if (human._BlockInput && IsBlockPossible(human))
            human._IsBlocking = true;
        else
            human._IsBlocking = false;
    }
    private static bool IsBlockPossible(Humanoid human)
    {
        if (!human._IsInCombatMode || human._IsAttacking || !(human._HandState is MeleeWeaponHandState) || human._IsJumping || !human._IsGrounded || human._IsSprinting || human._IsStaggered) return false;

        return true;
    }

    public static void AttackIsOver(Humanoid human, Weapon weapon)
    {
        if (weapon != null)
        {
            if (weapon is MeleeWeapon meleeWeapon)
            {
                meleeWeapon._AttackWarning.gameObject.SetActive(false);
                meleeWeapon._AttackCollider.gameObject.SetActive(false);
                meleeWeapon._HeavyAttackMultiplier = 1f;
            }
            weapon._Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        human._IsAttacking = false;
        human._IsInAttackReady = false;
        human._FootIKComponent.SetTargetWeight(1f);
        human.ChangeAnimation("EmptyRight");
        human.ChangeAnimation("EmptyLeft");
        human.ChangeAnimation("EmptyArms");
        human.ChangeAnimation("EmptyFull");
    }
    public static void CheckMeleeAttack(Humanoid human)
    {
        if (!human._IsInCombatMode) return;

        if (human._LightAttackInput && IsAttackPossible(human))
        {
            human._LightAttackInput = false;
            LightOrHeavyAttack(human, human._HeavyAttackMultiplier, GetCurrentWeaponType(human, human._IsAttackingFromLeftHandWeapon));
        }
        else if (human._HeavyAttackInput && IsAttackPossible(human))
        {
            human._HeavyAttackInput = false;
            LightOrHeavyAttack(human, human._HeavyAttackMultiplier, GetCurrentWeaponType(human, human._IsAttackingFromLeftHandWeapon));
        }
        else if (human._KickInput && IsAttackPossible(human))
        {
            human._KickInput = false;
            KickOrPunchAttack(human, human._HeavyAttackMultiplier, GetCurrentWeaponType(human, human._IsAttackingFromLeftHandWeapon), true);
        }
    }
    public static bool IsAttackPossible(Humanoid human)
    {
        if (!human._IsInCombatMode || human._IsBlocking || human._IsStaggered || human._LastTimeAttacked + human._WaitTimeForNextAttack > Time.timeAsDouble || human._Stamina < 15f) return false;
        if (human._HandState is RangedWeaponHandState state && state._LastAimStartedTime + 0.5 > Time.timeAsDouble) return false;

        return true;
    }
    private static void LightOrHeavyAttack(Humanoid human, float heavyAttackMultiplier, WeaponType weaponType)
    {
        if (human._IsAttacking)
            AttackIsOver(human, human._LastAttackWeapon);

        Item handEquippedItemRef = human._IsAttackingFromLeftHandWeapon ? human._LeftHandEquippedItemRef : human._RightHandEquippedItemRef;
        if (handEquippedItemRef != null && weaponType != WeaponType.None && handEquippedItemRef is WeaponItem weaponItem)
        {
            var handle = weaponItem._SpawnedHandle;
            if (handle.HasValue && handle.Value.IsValid() && handle.Value.IsDone && handle.Value.Result != null)
            {
                human._FootIKComponent.SetTargetWeight(0f);
                human._Stamina -= 15f;
                human._IsAttacking = true;
                human._IsInAttackReady = false;
                human._LastTimeAttacked = Time.timeAsDouble;
                human._WaitTimeForNextAttack = 0.65f * heavyAttackMultiplier * GetAttackWaitMultiplierForHumanAgi(human) / (weaponItem._ItemDefinition as IMeleeWeapon)._BaseSpeed;
                float attackSpeedMultiplier = 1f / human._WaitTimeForNextAttack;
                human.ChangeAnimationWithOffset(human._LastReadyAnimName, (heavyAttackMultiplier - 1f) / 3f, 0.1f, attackSpeedMultiplier, human._IsAttackingFromLeftHandWeapon ? 4 : 3);
                handle.Value.Result.GetComponent<MeleeWeapon>().Attack(human._LastReadyAnimName, heavyAttackMultiplier, human._LastAttackDirectionFrom);
                MovementStateMethods.AttackMove(human);
            }
        }
        else
        {
            KickOrPunchAttack(human, heavyAttackMultiplier, weaponType);
        }

    }
    private static float GetAttackWaitMultiplierForHumanAgi(Humanoid human) => 1f - GameManager._Instance.GetAgiLevel(human._Height, human._MuscleLevel, human._FatLevel, human._IsMale) / 40f;
    private static void KickOrPunchAttack(Humanoid human, float heavyAttackMultiplier, WeaponType weaponType, bool isFromKickInput = false)
    {
        if (human._IsAttacking)
            AttackIsOver(human, human._LastAttackWeapon);

        human._FootIKComponent.SetTargetWeight(0f);
        human._Stamina -= 15f;
        human._IsAttacking = true;
        human._IsInAttackReady = false;
        human._LastTimeAttacked = Time.timeAsDouble;
        human._WaitTimeForNextAttack = 0.75f * GetAttackWaitMultiplierForHumanAgi(human) * heavyAttackMultiplier;
        float attackSpeedMultiplier = heavyAttackMultiplier;
        string animName = isFromKickInput ? (Random.Range(0, 2) == 0 ? "Right_Kick" : "Left_Kick") : human._LastReadyAnimName;
        human.ChangeAnimation(animName, 0.05f, attackSpeedMultiplier);
        if (animName.EndsWith("Punch"))
            PunchAttack(human, animName, heavyAttackMultiplier);
        else
            KickAttack(human, animName, heavyAttackMultiplier);
        MovementStateMethods.AttackMove(human);
    }

    private static void PunchAttack(Humanoid human, string animName, float heavyAttackMultiplier)
    {
        if (animName.StartsWith("Left"))
            human._LeftHandPunch.Attack(animName, heavyAttackMultiplier, AttackDirectionFrom.Forward);
        else
            human._RightHandPunch.Attack(animName, heavyAttackMultiplier, AttackDirectionFrom.Forward);
    }
    private static void KickAttack(Humanoid human, string animName, float heavyAttackMultiplier)
    {
        if (animName.StartsWith("Left"))
            human._LeftKick.Attack(animName, heavyAttackMultiplier, AttackDirectionFrom.Forward);
        else
            human._RightKick.Attack(animName, heavyAttackMultiplier, AttackDirectionFrom.Forward);
    }
    public static void TryParry(Humanoid human)
    {
        if (!human._ParryInput || !human._IsInCombatMode || human._IsAttacking || human._IsInAttackReady || !human._IsBlocking || human._LastTimeTriedParry + 0.75 > Time.timeAsDouble) return;

        human._LastTimeTriedParry = Time.timeAsDouble;
        human.ChangeAnimation("Parry");
    }
    public static void ParryFailed(Humanoid human, Vector3 attackDir)
    {
        human.ChangeAnimation("ParryFailed");
        MovementStateMethods.Stagger(human, 1.5f, attackDir);
    }
    public static void AttackGotParried(Humanoid human, Vector3 attackDir)
    {
        human.ChangeAnimation("ParryFailed");
        MovementStateMethods.Stagger(human, 0.75f, -attackDir);
    }
    public static string GetAttackAnimName(Humanoid human, AttackDirectionFrom attackDirectionFrom, WeaponType weaponType)
    {
        if (human._RightHandEquippedItemRef as WeaponItem == null && human._LeftHandEquippedItemRef as WeaponItem == null)
        {
            return Random.Range(0, 2) == 0 ? "Right_Punch" : "Left_Punch";
        }
        else
        {
            string weaponString = GetWeaponStringFromEnum(weaponType);
            string directionString = GetDirectionStringFromEnum(attackDirectionFrom);

            return weaponString + "_" + directionString;
        }
    }
    public static void PlayHitAnimation(Humanoid human, Damage damage)
    {
        float angle = Vector2.Angle(new Vector2(damage._AttackerDirection.x, damage._AttackerDirection.z), new Vector2(human._Transform.forward.x, human._Transform.forward.z));
        if (damage._AttackDirectionFrom == AttackDirectionFrom.Up)
        {
            human.ChangeAnimation("Hit_Up");
        }
        else if (damage._AttackDirectionFrom == AttackDirectionFrom.Forward || damage._AttackDirectionFrom == AttackDirectionFrom.Down)
        {
            if (angle > 90f)
                human.ChangeAnimation("Hit_Forward");
            else
                human.ChangeAnimation("Hit_Back");
        }
        else if (damage._AttackDirectionFrom == AttackDirectionFrom.Right)
        {
            if (angle > 90f)
                human.ChangeAnimation("Hit_Right");
            else
                human.ChangeAnimation("Hit_Left");
        }
        else if (damage._AttackDirectionFrom == AttackDirectionFrom.Left)
        {
            if (angle > 90f)
                human.ChangeAnimation("Hit_Left");
            else
                human.ChangeAnimation("Hit_Right");
        }
    }
    public static void ArrangeIsAttackingFromLeftHand(Humanoid human)
    {
        if (human._LeftHandEquippedItemRef != null && human._LeftHandEquippedItemRef is WeaponItem && GameManager._Instance.RandomPercentageChance(30f))
            human._IsAttackingFromLeftHandWeapon = true;
        else if (human._LeftHandEquippedItemRef != null && human._LeftHandEquippedItemRef is WeaponItem && !(human._RightHandEquippedItemRef != null && human._RightHandEquippedItemRef is WeaponItem))
            human._IsAttackingFromLeftHandWeapon = true;
        else
            human._IsAttackingFromLeftHandWeapon = false;
    }
    public static void ReadyAttackAnimation(Humanoid human, WeaponType weaponType)
    {
        human._IsInAttackReady = true;
        human._LastAttackReadyTime = Time.timeAsDouble;
        AttackDirectionFrom attackDirectionFrom = ChooseAttackDirection(human);
        human._LastAttackDirectionFrom = attackDirectionFrom;
        human._LastReadyAnimName = GetAttackAnimName(human, attackDirectionFrom, weaponType);
        string readyName = human._IsHandsEmpty ? "PunchReady" : (attackDirectionFrom == AttackDirectionFrom.Left ? "AttackReadyLeft" : (attackDirectionFrom == AttackDirectionFrom.Forward ? "AttackReadyForward" : "AttackReady"));
        int layer = human._IsHandsEmpty ? (human._LastReadyAnimName.StartsWith("Left") ? 4 : 3) : (human._IsAttackingFromLeftHandWeapon ? 4 : 3);
        human.ChangeAnimation(readyName, 0.1f, 1f, layer);
    }
    public static AttackDirectionFrom ChooseAttackDirection(Humanoid human)
    {
        AttackDirectionFrom attackDirectionFrom = human._LastAttackDirectionFrom == AttackDirectionFrom.Right ? AttackDirectionFrom.Left : AttackDirectionFrom.Right;
        if (!human._IsGrounded)
            attackDirectionFrom = AttackDirectionFrom.Up;
        return attackDirectionFrom;
    }
    public static void AttackCancelled(Humanoid human)
    {
        AttackIsOver(human, human._LastAttackWeapon);
    }
    public static WeaponType GetCurrentWeaponType(Humanoid human, bool isCheckingLeftHand)
    {
        Item checkItem = isCheckingLeftHand ? human._LeftHandEquippedItemRef : human._RightHandEquippedItemRef;
        if (checkItem != null && checkItem is WeaponItem weaponItem && weaponItem._SpawnedHandle.HasValue && weaponItem._SpawnedHandle.Value.IsValid() && weaponItem._SpawnedHandle.Value.IsDone && weaponItem._SpawnedHandle.Value.Result != null)
            return weaponItem._SpawnedHandle.Value.Result.GetComponent<Weapon>()._WeaponType;

        return WeaponType.None;
    }
    public static WeaponType GetWeaponTypeFromString(string name)
    {
        if (name.StartsWith("BroadSword"))
        {
            return WeaponType.BroadSword;
        }
        else if (name.StartsWith("LongSword"))
        {
            return WeaponType.LongSword;
        }
        else if (name.StartsWith("ColossalSword"))
        {
            return WeaponType.ColossalSword;
        }
        else if (name.StartsWith("Scimitar"))
        {
            return WeaponType.Scimitar;
        }
        else if (name.StartsWith("Katana"))
        {
            return WeaponType.Katana;
        }
        else if (name.StartsWith("Mace"))
        {
            return WeaponType.Mace;
        }
        else if (name.StartsWith("Hammer"))
        {
            return WeaponType.Hammer;
        }
        else if (name.StartsWith("Halberd"))
        {
            return WeaponType.Halberd;
        }
        else if (name.StartsWith("Rapier"))
        {
            return WeaponType.Rapier;
        }
        else if (name.StartsWith("Dagger"))
        {
            return WeaponType.Dagger;
        }
        else if (name.StartsWith("Spear"))
        {
            return WeaponType.Spear;
        }
        else if (name.StartsWith("Bow"))
        {
            return WeaponType.Bow;
        }
        else if (name.StartsWith("Crossbow"))
        {
            return WeaponType.Crossbow;
        }
        else if (name.StartsWith("Talisman"))
        {
            return WeaponType.Talisman;
        }

        Debug.LogError("Weapon Type Not Found! name :" + name);
        return WeaponType.BroadSword;

    }

    private static string GetDirectionStringFromEnum(AttackDirectionFrom getString)
    {
        switch (getString)
        {
            case AttackDirectionFrom.Up:
                return "Up";
            case AttackDirectionFrom.Right:
                return "Right";
            case AttackDirectionFrom.Down:
                return "Down";
            case AttackDirectionFrom.Left:
                return "Left";
            case AttackDirectionFrom.Forward:
                return "Forward";
            default:
                return "Forward";
        }
    }
    private static string GetWeaponStringFromEnum(WeaponType getString)
    {
        switch (getString)
        {
            case WeaponType.BroadSword:
                return "BroadSword";
            case WeaponType.LongSword:
                return "LongSword";
            case WeaponType.ColossalSword:
                return "ColossalSword";
            case WeaponType.Scimitar:
                return "Scimitar";
            case WeaponType.Katana:
                return "Katana";
            case WeaponType.Mace:
                return "Mace";
            case WeaponType.Hammer:
                return "Hammer";
            case WeaponType.Halberd:
                return "Halberd";
            case WeaponType.Rapier:
                return "Rapier";
            case WeaponType.Dagger:
                return "Dagger";
            case WeaponType.Spear:
                return "Spear";
            case WeaponType.Bow:
                return "Bow";
            case WeaponType.Crossbow:
                return "Crossbow";
            default:
                Debug.LogError("type not found! enumName: " + getString);
                return "None";
        }
    }

    public static bool CheckForEmptyState(Humanoid human)
    {
        if (human._MovementState is UnconsciousMoveState) return true;
        if (CheckForMeleeWeaponState(human)) return false;
        if (CheckForRangedWeaponState(human)) return false;
        if (CheckForCarryState(human)) return false;

        return true;
    }
    public static bool CheckForCarryState(Humanoid human)
    {
        return human._RightHandEquippedItemRef != null && human._RightHandEquippedItemRef._ItemDefinition._IsBig && !(human._RightHandEquippedItemRef is WeaponItem);
    }

    public static bool CheckForMeleeWeaponState(Humanoid human)
    {
        if (CheckForCarryState(human)) return false;
        return human._IsInCombatMode && ((human._RightHandEquippedItemRef == null && human._LeftHandEquippedItemRef == null) || (human._RightHandEquippedItemRef != null && human._RightHandEquippedItemRef is WeaponItem weaponItem && !(weaponItem._ItemDefinition is IRangedWeapon)) || (human._LeftHandEquippedItemRef != null && human._LeftHandEquippedItemRef is WeaponItem weaponItem2 && !(weaponItem2._ItemDefinition is IRangedWeapon)));
    }
    public static bool CheckForRangedWeaponState(Humanoid human)
    {
        if (CheckForCarryState(human)) return false;
        return human._IsInCombatMode && ((human._RightHandEquippedItemRef != null && human._RightHandEquippedItemRef._ItemDefinition is IRangedWeapon) || (human._LeftHandEquippedItemRef != null && human._LeftHandEquippedItemRef._ItemDefinition is IRangedWeapon));
    }
}