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

    public bool _IsInCoolAngleMode { get; private set; }
    private float _rad;
    private float _coolAngleModeCounter;
    private float _xAngleForThirdPersonMode;

    private void Awake()
    {
        _Instance = this;
        _realCameraDistance = 14f;
        _CameraDistance = _realCameraDistance;
        _maxDistance = 17f;
        _minDistance = 7f;
        _xAngleForThirdPersonMode = 15f;
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
            if (_coolAngleModeCounter >= 0f)
                _coolAngleModeCounter += Time.deltaTime;
            if (_coolAngleModeCounter > 0.4f)
            {
                _coolAngleModeCounter = -1f;
                if (_IsInCoolAngleMode)
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
            if (_IsInCoolAngleMode && WorldHandler._Instance._Player._DirectionInput.magnitude > 0.1f)
                _coolAngleModeCounter += Time.deltaTime;
            else
                _coolAngleModeCounter = 0f;
            if (_IsInCoolAngleMode && _coolAngleModeCounter > 0.4f)
                DeactivateCoolAngleMod();
        }

        float zoomInput = M_Input.GetCameraZoomInput();
        if (zoomInput != 0f)
        {
            _realCameraDistance = Mathf.Clamp(_realCameraDistance - zoomInput, _minDistance, _maxDistance);
        }

        if (WorldHandler._Instance._Player._CameraAngleInput || _IsInCoolAngleMode)
        {
            float radius = new Vector2(_FollowOffset.x, _FollowOffset.z).magnitude;
            _rad -= Time.deltaTime * 240f * M_Input.GetCameraRotateInput() * Mathf.Deg2Rad;
            _rad = (_rad % (2f * Mathf.PI) + 2f * Mathf.PI) % (2f * Mathf.PI);
            float newX = Mathf.Cos(_rad) * radius;
            float newZ = Mathf.Sin(_rad) * radius;

            Vector3 tempVector = new Vector3(newX, 0f, newZ);
            tempVector.y = _FollowOffset.y;
            _FollowOffset = tempVector;

            if (_IsInCoolAngleMode)
            {
                _xAngleForThirdPersonMode += M_Input.GetCameraRotateUpwardsInput();
                _xAngleForThirdPersonMode = Mathf.Clamp(_xAngleForThirdPersonMode, 0f, 35f);
            }
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
        lerpSpeed = (WorldHandler._Instance._Player._LookAtForCam.position - transform.position).magnitude > _CameraDistance * 2.5f ? 7f : lerpSpeed;
        Vector3 realFollowOffset = new Vector3(_FollowOffset.x, _IsInCoolAngleMode ? 0.25f : _FollowOffset.y, _FollowOffset.z);
        Vector3 targetPos = WorldHandler._Instance._Player._LookAtForCam.position + realFollowOffset * (_IsInCoolAngleMode ? _CameraDistance / 2f : _CameraDistance);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);

        if (_IsInCoolAngleMode)
        {
            float tempX = transform.localEulerAngles.x;
            Quaternion targetRotation = Quaternion.LookRotation((WorldHandler._Instance._Player.transform.position - transform.position).normalized, Vector3.up);
            transform.rotation = targetRotation;
            transform.localEulerAngles = new Vector3(Mathf.Lerp(tempX, _xAngleForThirdPersonMode, Time.deltaTime * 8f), transform.localEulerAngles.y, 0f);
        }
        else
        {
            Vector3 targetAngles = new Vector3(55f, Mathf.Atan2(_FollowOffset.x, _FollowOffset.z) * Mathf.Rad2Deg + 180f, 0f);
            Quaternion targetRotation = Quaternion.Euler(targetAngles);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
        }

    }

    private IEnumerator ArrangeViewBlockCoroutine()
    {
        while (true)
        {
            //Physics.Raycast(transform.position, (WorldHandler._Instance._Player.transform.position - transform.position).normalized, out RaycastHit hit, 300f, GameManager._Instance._SolidAndHumanMask);
            Physics.Raycast(transform.position, (WorldHandler._Instance._Player.transform.position + Vector3.up * 0.7f - transform.position).normalized, out RaycastHit hit2, 300f, GameManager._Instance._SolidHumanMask);
            Physics.Raycast(transform.position, (WorldHandler._Instance._Player.transform.position + Vector3.up * 1.4f - transform.position).normalized, out RaycastHit hit3, 300f, GameManager._Instance._SolidHumanMask);
            if (CheckRaycastHitForSolidObj(hit2) || CheckRaycastHitForSolidObj(hit3))
            {
                _CameraDistance = Mathf.Clamp(_CameraDistance - 0.5f, 3f, _realCameraDistance);
            }
            else if (_CameraDistance != _realCameraDistance)
            {
                Vector3 targetPos = WorldHandler._Instance._Player._LookAtForCam.position + _FollowOffset * (_CameraDistance + 0.5f);
                //Vector3 dir = (WorldHandler._Instance._Player.transform.position - targetPos).normalized;
                Vector3 dir2 = (WorldHandler._Instance._Player.transform.position + Vector3.up * 0.7f - targetPos).normalized;
                Vector3 dir3 = (WorldHandler._Instance._Player.transform.position + Vector3.up * 1.4f - targetPos).normalized;
                //Physics.Raycast(targetPos, dir, out hit, 300f, GameManager._Instance._SolidAndHumanMask);
                Physics.Raycast(targetPos, dir2, out hit2, 300f, GameManager._Instance._SolidHumanMask);
                Physics.Raycast(targetPos, dir3, out hit3, 300f, GameManager._Instance._SolidHumanMask);
                if (!(CheckRaycastHitForSolidObj(hit2) || CheckRaycastHitForSolidObj(hit3)))
                    _CameraDistance = Mathf.Clamp(_CameraDistance + 0.5f, 3f, _realCameraDistance);
            }
            yield return null;
        }
    }
    private bool CheckRaycastHitForSolidObj(RaycastHit hit)
    {
        return hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("SolidObject");
    }

    public void ActivateCoolAngleMod()
    {
        if (WorldHandler._Instance._Player._IsInCombatMode) return;
        _IsInCoolAngleMode = true;
    }
    public void DeactivateCoolAngleMod()
    {
        _IsInCoolAngleMode = false;
    }
}
