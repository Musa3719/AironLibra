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
            if (human._IsStrafing)
            {
                human.LookAt((human as NPC)._AimPosition, human._AimSpeed);
            }
        }
        else if (human is Player)
        {
            if (human._IsStrafing)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out human._RayFoLook, 1000f, GameManager._Instance._TerrainAndWaterMask, QueryTriggerInteraction.Ignore) && human._RayFoLook.collider != null)
                {
                    if (human._Rigidbody.linearVelocity.magnitude > 0.1f)
                        human.LookAt((human as Player)._LookAtForCam.transform.position, human._AimSpeed);
                    human.transform.localEulerAngles = new Vector3(0f, human.transform.localEulerAngles.y, 0f);

                    Vector3 distance = human._RayFoLook.point - human.transform.position;
                    distance.y = 0f;
                    distance = Vector3.ClampMagnitude(distance, 5f);
                    distance = distance * CameraController._Instance._CameraDistance / 12f;
                    Vector3 targetPos = human.transform.position + distance + Vector3.up;
                    (human as Player)._LookAtForCam.transform.position = Vector3.Lerp((human as Player)._LookAtForCam.transform.position, targetPos, Time.deltaTime * 5f);

                    Vector2 first = new Vector2(human.transform.forward.x, human.transform.forward.z);
                    Vector2 second = new Vector2(targetPos.x - human.transform.position.x, targetPos.z - human.transform.position.z);
                    float angle = Vector2.Angle(first, second);
                    bool isRight = Vector2.SignedAngle(first, second) < 0 ? true : false;
                    if (angle > 50f && human._Rigidbody.linearVelocity.magnitude < 0.1f)
                        MovementStateMethods.Rotate60Degrees(human, isRight);
                }
            }
            else
            {
                (human as Player)._LookAtForCam.transform.position = Vector3.Lerp((human as Player)._LookAtForCam.transform.position, human.transform.position + Vector3.up + human._Rigidbody.linearVelocity * 1.75f * CameraController._Instance._CameraDistance / 12f, Time.deltaTime * 5f);
            }
        }
    }
}