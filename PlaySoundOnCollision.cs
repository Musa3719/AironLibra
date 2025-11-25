using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundOnCollision : MonoBehaviour
{
    public AudioClip _SoundClip { get; set; }

    private float _pitch;
    private Rigidbody _rb;
    private float _collisionSpeed;
    private float _lastTimeSoundPlayed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        if (_rb != null)
            _collisionSpeed = _rb.linearVelocity.magnitude;
    }
    private void Update()
    {
        if (enabled && _rb == null)
        {
            _rb = GetComponent<Rigidbody>();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == null) return;

        float volume = 0.1f;
        if (_pitch == 0f)
            _pitch = 1f;

        if (GetComponent<Weapon>() != null)
            volume += Mathf.Clamp(_rb.linearVelocity.magnitude / 100f, 0f, 0.8f);

        if (enabled && _lastTimeSoundPlayed + 0.15f < Time.time && _SoundClip != null && _collisionSpeed > 2f)
        {
            SoundManager._Instance.PlaySound(_SoundClip, transform.position, volume, false, _pitch + Random.Range(-0.1f, 0.1f));
            _lastTimeSoundPlayed = Time.time;
        }
    }
}
