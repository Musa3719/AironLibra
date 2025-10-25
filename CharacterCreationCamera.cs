using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterCreationCamera : MonoBehaviour
{
    public Transform _Target;
    public Vector3 _FollowOffset;
    public float _CameraDistance { get; private set; }
    private float _customYOffset;
    private float _maxDistance;
    private float _minDistance;

    private float _rad;

    private void Awake()
    {
        _CameraDistance = 2.25f;
        _maxDistance = 3f;
        _minDistance = 0.35f;
        _customYOffset = 0.6f;
    }

    private void Update()
    {
        float zoomInput = M_Input.GetCameraZoomInput();
        if (zoomInput != 0f)
        {
            if (_CameraDistance < 1.5f) zoomInput /= 2f;
            float oldDistance = _CameraDistance;
            _CameraDistance = Mathf.Clamp(_CameraDistance - zoomInput, _minDistance, _maxDistance);
            _customYOffset -= (_CameraDistance - oldDistance) / 1.7f;
            _customYOffset = Mathf.Clamp(_customYOffset, -1f + (2f - _CameraDistance), 1f + (2f - _CameraDistance));
        }

        float verticalInput = M_Input.GetAxis("Vertical");
        float horizontalInput = M_Input.GetAxis("Horizontal");

        if (verticalInput != 0f)
        {
            _customYOffset += verticalInput * (1f + _CameraDistance * 0.75f) * Time.unscaledDeltaTime;
            _customYOffset = Mathf.Clamp(_customYOffset, -1f + (2f - _CameraDistance), 1f + (2f - _CameraDistance));
        }

        if (horizontalInput != 0f)
        {
            float radius = new Vector2(_FollowOffset.x, _FollowOffset.z).magnitude;
            _rad -= Time.deltaTime * 240f * -horizontalInput * Mathf.Deg2Rad;
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
        float lerpSpeed = 8f;
        Vector3 realFollowOffset = new Vector3(_FollowOffset.x, _FollowOffset.y, _FollowOffset.z);
        transform.position = Vector3.Lerp(transform.position, _Target.position + _customYOffset * Vector3.up + realFollowOffset * _CameraDistance, Time.deltaTime * lerpSpeed);
        Vector3 targetAngles = new Vector3(30f, Mathf.Atan2(_FollowOffset.x, _FollowOffset.z) * Mathf.Rad2Deg + 180f, 0f);
        Quaternion targetRotation = Quaternion.Euler(targetAngles.x, targetAngles.y, targetAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed * 0.85f);
    }

}
