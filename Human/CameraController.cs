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

    public bool _IsInCoolAngleMod { get; private set; }
    private float _rad;
    private float _quitCoolAngleModeCounter;

    private void Awake()
    {
        _Instance = this;
        _realCameraDistance = 10f;
        _CameraDistance = _realCameraDistance;
        _maxDistance = 14f;
        _minDistance = 7f;
    }
    private void Start()
    {
        StartCoroutine(ArrangeViewBlockCoroutine());
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        if (M_Input.GetButton("CoolCamera"))
        {
            if (_quitCoolAngleModeCounter >= 0f)
                _quitCoolAngleModeCounter += Time.deltaTime;
            if (_quitCoolAngleModeCounter > 0.4f)
            {
                _quitCoolAngleModeCounter = -1f;
                if (_IsInCoolAngleMod)
                {
                    DeactivateCoolAngleMod();
                }
                else
                {
                    ActivateCoolAngleMod();
                }
            }
        }
        else
        {
            _quitCoolAngleModeCounter = 0f;
        }

        float zoomInput = M_Input.GetCameraZoomInput();
        if (zoomInput != 0f)
        {
            _realCameraDistance = Mathf.Clamp(_realCameraDistance - zoomInput, _minDistance, _maxDistance);
        }

        if (WorldHandler._Instance._Player._CameraAngleInput)
        {
            float radius = new Vector2(_FollowOffset.x, _FollowOffset.z).magnitude;
            _rad -= Time.deltaTime * 240f * M_Input.GetCameraRotateInput() * Mathf.Deg2Rad;
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

    }
    private void LateUpdate()
    {
        if (GameManager._Instance._IsGameStopped) return;

        float lerpSpeed = 6f;//WorldHandler._Instance._Player._CameraAngleInput ? 6f : 3f;
        WorldHandler._Instance._Player._LookAtForCam.position =
            new Vector3(WorldHandler._Instance._Player._LookAtForCam.position.x, Mathf.Clamp(WorldHandler._Instance._Player._LookAtForCam.position.y, WorldHandler._Instance._SeaLevel, float.MaxValue), WorldHandler._Instance._Player._LookAtForCam.position.z);
        lerpSpeed = (WorldHandler._Instance._Player._LookAtForCam.position - transform.position).magnitude > _CameraDistance * 2f ? 6f : lerpSpeed;
        if (_IsInCoolAngleMod)
            lerpSpeed = 10f;
        Vector3 realFollowOffset = new Vector3(_FollowOffset.x, _IsInCoolAngleMod ? _FollowOffset.y / 1.6f : _FollowOffset.y, _FollowOffset.z);
        transform.position = Vector3.Lerp(transform.position, WorldHandler._Instance._Player._LookAtForCam.position + realFollowOffset * _CameraDistance * (_IsInCoolAngleMod ? 0.6f : 1f), Time.deltaTime * lerpSpeed);
        Vector3 targetAngles = new Vector3(_IsInCoolAngleMod ? 30f : 45f, Mathf.Atan2(_FollowOffset.x, _FollowOffset.z) * Mathf.Rad2Deg + 180f, 0f);
        Quaternion targetRotation = Quaternion.Euler(targetAngles.x, targetAngles.y, targetAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
    }

    private IEnumerator ArrangeViewBlockCoroutine()
    {
        while (true)
        {
            Physics.Raycast(transform.position, (WorldHandler._Instance._Player.transform.position - transform.position).normalized, out RaycastHit hit, 300f, GameManager._Instance._SolidAndHumanMask);
            if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("SolidObject"))
            {
                Physics.Raycast(transform.position, (WorldHandler._Instance._Player.transform.position + Vector3.up * 0.7f - transform.position).normalized, out hit, 300f, GameManager._Instance._SolidAndHumanMask);
                if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("SolidObject"))
                {
                    Physics.Raycast(transform.position, (WorldHandler._Instance._Player.transform.position + Vector3.up * 1.2f - transform.position).normalized, out hit, 300f, GameManager._Instance._SolidAndHumanMask);
                    if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("SolidObject"))
                    {
                        _CameraDistance = Mathf.Clamp(_CameraDistance - 0.5f, 5f, _realCameraDistance);
                    }
                }
            }
            else
            {
                Vector3 targetPos = WorldHandler._Instance._Player._LookAtForCam.position + _FollowOffset * (_CameraDistance + 0.5f);
                Vector3 dir = (WorldHandler._Instance._Player.transform.position - targetPos).normalized;
                Physics.Raycast(targetPos, dir, out hit, 300f, GameManager._Instance._SolidAndHumanMask);
                if (!(hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("SolidObject")))
                    _CameraDistance = Mathf.Clamp(_CameraDistance + 0.5f, 5f, _realCameraDistance);
            }
            yield return null;
        }
    }

    public void ActivateCoolAngleMod()
    {
        _IsInCoolAngleMod = true;
    }
    public void DeactivateCoolAngleMod()
    {
        _IsInCoolAngleMod = false;
    }
}
