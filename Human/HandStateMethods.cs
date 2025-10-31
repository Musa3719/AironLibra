using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HandStateMethods
{
    public static void ArrangeLookAtForCamPosition(Player pl)
    {
        if (!pl._IsStrafing)
        {
            if (CameraController._Instance._IsInCoolAngleMod)
                pl._LookAtForCam.transform.position = pl.transform.position + Vector3.up;
            else
                pl._LookAtForCam.transform.position = Vector3.Lerp(pl._LookAtForCam.transform.position, pl.transform.position + Vector3.up + pl._Rigidbody.linearVelocity * CameraController._Instance._CameraDistance / 40f, Time.deltaTime * 5f);
        }
        else
        {
            if (!M_Input.IsLastInputFromGamepadForAim())
            {
                Ray ray = Camera.main.ScreenPointToRay(M_Input.GetMousePosition());
                if (Physics.Raycast(ray, out pl._RayFoLook, 1000f, GameManager._Instance._TerrainAndWaterMask, QueryTriggerInteraction.Ignore) && pl._RayFoLook.collider != null)
                {
                    Vector3 distance = pl._RayFoLook.point - pl.transform.position;
                    distance.y = 0f;
                    distance = Vector3.ClampMagnitude(distance, 2f);
                    distance = distance * CameraController._Instance._CameraDistance / 40f;
                    if (CameraController._Instance._IsInCoolAngleMod)
                        distance = Vector3.ClampMagnitude(distance, 1.5f);
                    Vector3 targetPos = pl.transform.position + distance + Vector3.up;
                    pl._LookAtForCam.transform.position = targetPos;
                }
            }
            else
            {
                Vector2 newVector = M_Input.GetGamepadLookVector();
                if (newVector.magnitude > 0.07f)
                    pl._LastLookVectorForGamepad = newVector;
                if (pl._LastLookVectorForGamepad.magnitude > 1f)
                    pl._LastLookVectorForGamepad.Normalize();
                Vector3 distance = 2f * GameManager._Instance.Vector2ToVector3(pl._LastLookVectorForGamepad);
                distance *= CameraController._Instance._CameraDistance / 40f;
                if (CameraController._Instance._IsInCoolAngleMod)
                    distance = Vector3.ClampMagnitude(distance, 1.5f);
                Vector3 targetPos = pl.transform.position + distance + Vector3.up;
                pl._LookAtForCam.transform.position = targetPos;
            }
        }

        if (pl._LastJumpedTime + 0.85f > Time.time) { pl._LookAtForCam.transform.position = new Vector3(pl._LookAtForCam.position.x, pl._LastJumpedPosition.y, pl._LookAtForCam.position.z); return; }
    }

    public static void CheckAttack(Humanoid human)
    {
        if (!human._IsInCombatMode) return;

        if (human._AttackInput && AttackConditions(human))
        {
            if (human is Player)
                GameManager._Instance.BufferActivated(ref WorldHandler._Instance._Player._AttackBuffer, WorldHandler._Instance._Player, ref WorldHandler._Instance._Player._AttackCoroutine);
            Attack(human);
        }
    }
    private static bool AttackConditions(Humanoid human)
    {
        return false;
    }
    private static void Attack(Humanoid human)
    {

    }

    public static bool CheckForEmptyState(Humanoid human)
    {
        return !human._IsInCombatMode && human._RightHandEquippedItemRef == null && human._LeftHandEquippedItemRef == null;
    }
    public static bool CheckForCarryState(Humanoid human)
    {
        return (human._RightHandEquippedItemRef != null && human._RightHandEquippedItemRef._ItemDefinition._IsBig && !(human._RightHandEquippedItemRef is WeaponItem)) ||
            (human._LeftHandEquippedItemRef != null && human._LeftHandEquippedItemRef._ItemDefinition._IsBig && !(human._LeftHandEquippedItemRef is WeaponItem));
    }

    public static bool CheckForWeaponState(Humanoid human)
    {
        if (CheckForCarryState(human)) return false;
        return human._IsInCombatMode && ((human._RightHandEquippedItemRef != null && human._RightHandEquippedItemRef is WeaponItem) || (human._LeftHandEquippedItemRef != null && human._LeftHandEquippedItemRef is WeaponItem));
    }
}