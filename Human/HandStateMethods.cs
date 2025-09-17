using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HandStateMethods
{
    public static void ArrangeAimRotation(Humanoid human)
    {
        float beforeY = human.transform.localEulerAngles.y;
        if (human is NPC)
        {

        }
        else if (human is Player)
        {
            if (human._IsStrafing)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out human._RayFoLook, 1000f, GameManager._Instance._TerrainAndWaterMask, QueryTriggerInteraction.Ignore) && human._RayFoLook.collider != null)
                {
                    Vector3 distance = human._RayFoLook.point - human.transform.position;
                    distance.y = 0f;
                    distance = Vector3.ClampMagnitude(distance, 5f);
                    distance = distance * CameraController._Instance._CameraDistance / 12f;
                    Vector3 targetPos = human.transform.position + distance + Vector3.up;
                    (human as Player)._LookAtForCam.transform.position = Vector3.Lerp((human as Player)._LookAtForCam.transform.position, targetPos, Time.deltaTime * 5f);
                }
            }
            else
            {
                (human as Player)._LookAtForCam.transform.position = Vector3.Lerp((human as Player)._LookAtForCam.transform.position, human.transform.position + Vector3.up + human._Rigidbody.linearVelocity * 1.75f * CameraController._Instance._CameraDistance / 12f, Time.deltaTime * 5f);
            }
        }
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
}