using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    public static CameraController _Instance;

    public Vector3 _FollowOffset;
    public float _CameraDistance { get; private set; }
    private float _realCameraDistance;
    private float _maxDistance;
    private float _minDistance;

    private bool _isInCoolAngleMod;
    private float _rad;

    private void Awake()
    {
        _Instance = this;
        _realCameraDistance = 10f;
        _maxDistance = 14f;
        _minDistance = 7f;
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        if (Input.GetButtonDown("CoolCamera"))
        {
            if (_isInCoolAngleMod)
            {
                DeactivateCoolAngleMod();
            }
            else
            {
                ActivateCoolAngleMod();
            }
        }

        if (WorldHandler._Instance._Player._CameraZoomInput)
        {
            _realCameraDistance = Mathf.Clamp(_realCameraDistance - Input.mouseScrollDelta.y * 1f, _minDistance, _maxDistance);
        }

        if (WorldHandler._Instance._Player._CameraAngleInput)
        {
            float radius = new Vector2(_FollowOffset.x, _FollowOffset.z).magnitude;
            _rad -= Time.deltaTime * 240f * Input.mousePositionDelta.normalized.x * Mathf.Deg2Rad;
            _rad = (_rad % (2f * Mathf.PI) + 2f * Mathf.PI) % (2f * Mathf.PI);
            float newX = Mathf.Cos(_rad) * radius;
            float newZ = Mathf.Sin(_rad) * radius;

            Vector3 tempVector = new Vector3(newX, 0f, newZ).normalized;
            tempVector.y = _FollowOffset.y;
            _FollowOffset = tempVector;
        }
        else
        {
            _rad = Mathf.Atan2(_FollowOffset.z, _FollowOffset.x);
        }

        StartCoroutine(ArrangeViewBlock());
    }
    private void LateUpdate()
    {
        if (GameManager._Instance._IsGameStopped) return;

        float lerpSpeed = WorldHandler._Instance._Player._CameraAngleInput ? 4f : 2f;
        lerpSpeed = (WorldHandler._Instance._Player._LookAtForCam.position - transform.position).magnitude > _CameraDistance * 2f ? 6f : lerpSpeed;
        transform.position = Vector3.Lerp(transform.position, WorldHandler._Instance._Player._LookAtForCam.position + _FollowOffset * _CameraDistance, Time.deltaTime * lerpSpeed);
        Vector3 targetAngles = new Vector3(45f, Mathf.Atan2(_FollowOffset.x, _FollowOffset.z) * Mathf.Rad2Deg + 180f, 0f);
        Quaternion targetRotation = Quaternion.Euler(targetAngles.x, targetAngles.y, targetAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 4f);
    }
    private IEnumerator ArrangeViewBlock()
    {
        while (true)
        {
            Physics.Raycast(transform.position, (WorldHandler._Instance._Player.transform.position - transform.position).normalized, out RaycastHit hit, 300f, GameManager._Instance._SolidAndHumanMask);
            if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("SolidObject"))
            {
                _CameraDistance = Mathf.Clamp(_CameraDistance - 0.5f, 2f, _realCameraDistance);
            }
            else
            {
                Vector3 targetPos = WorldHandler._Instance._Player._LookAtForCam.position + _FollowOffset * (_CameraDistance + 0.5f);
                Vector3 dir = (WorldHandler._Instance._Player.transform.position - targetPos).normalized;
                Physics.Raycast(targetPos, dir, out hit, 300f, GameManager._Instance._SolidAndHumanMask);
                if (!(hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("SolidObject")))
                    _CameraDistance = Mathf.Clamp(_CameraDistance + 0.5f, 2f, _realCameraDistance);
            }
            yield return null;
        }
    }
    private bool IsInOpenWorld()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1 && !IsInClosedSpace())
            return true;
        return false;
    }
    private bool IsInClosedSpace()
    {
        return Physics.Raycast(transform.position, Vector3.up, 150f, LayerMask.GetMask("SolidObject"));
    }

    private void ActivateCoolAngleMod()
    {
        _isInCoolAngleMod = true;
        _FollowOffset.y /= 2f;
    }
    private void DeactivateCoolAngleMod()
    {
        _isInCoolAngleMod = false;
        _FollowOffset.y *= 2f;
    }
}
