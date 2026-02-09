using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using System.Collections.Generic;

public class EntityPhysicObject : EntityCollider
{
    public Humanoid _Human { get; private set; }
    public Transform _CrouchColliderForHuman;
    public Transform _ProneColliderForHuman;
    public bool _IsSettingPositionForTesting;///

    private Transform _transform;
    private Entity _physicsEntity;
    private float _mass = 40f;
    private Vector3 _velocity;
    private float _gravity;
    private bool _isSettingYForThisFrame;

    private PhysicsCollider _collider1;
    private PhysicsCollider _collider2;
    private PhysicsCollider _collider3;
    private void Start()
    {
        _Human = GetComponent<Humanoid>();
        _transform = transform;
        _gravity = 1f;
        CreateColliderEntities(true);
        AddPhysicsBody();
    }
    private void AddPhysicsBody()
    {
        if (_colliderEntities.Count != 1)
            Debug.LogError("Entity Count != 1 for physics entity creation! Count : " + _colliderEntities.Count);

        if (_CrouchColliderForHuman != null && _ProneColliderForHuman != null)
        {
            _collider1 = _em.GetComponentData<PhysicsCollider>(_colliderEntities[0]);
            _collider2 = GetPhysicsColliderFromCapsuleCollider(_CrouchColliderForHuman);
            _collider3 = GetPhysicsColliderFromCapsuleCollider(_ProneColliderForHuman);
        }

        _physicsEntity = _colliderEntities[0];
        if (!_em.HasComponent<PhysicsCollider>(_physicsEntity))
            Debug.LogError("Entity does not have a Collider!");

        var collider = _em.GetComponentData<PhysicsCollider>(_physicsEntity);

        // Velocity
        _em.AddComponentData(_physicsEntity, new PhysicsVelocity
        {
            Linear = float3.zero,
            Angular = float3.zero
        });

        // Mass (dynamic)
        _em.AddComponentData(_physicsEntity,
            PhysicsMass.CreateDynamic(
                collider.Value.Value.MassProperties,
                _mass
            )
        );
        PhysicsMass temp = _em.GetComponentData<PhysicsMass>(_physicsEntity);
        temp.InverseInertia = float3.zero;
        _em.SetComponentData<PhysicsMass>(_physicsEntity, temp);

        // Gravity
        _em.AddComponentData(_physicsEntity, new PhysicsGravityFactor
        {
            Value = 1f
        });
        SetGravity(1f);

        _em.AddComponentData(_physicsEntity, new DesiredVelocity { _Value = 1f });
        _em.AddComponentData(_physicsEntity, new DesiredRotation { _Value = quaternion.identity });
        _em.AddComponentData(_physicsEntity, new DesiredPosition { _Value = transform.position });
        _em.AddComponentData(_physicsEntity, new ActivePositionVelocity { _Pos = transform.position, _Vel = float3.zero });

        EntityManager._Instance._List.Add(this);
        //_velocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized * 5f;
    }
    public void UpdateEntity()
    {
        if (GameManager._Instance._Is5)
        {
            if (_IsSettingPositionForTesting)
                SetPosition();
            //_velocity.y = _em.GetComponentData<ActiveVelocity>(_entity)._Value.y;
            // 1 ms
            _em.SetComponentData(_physicsEntity, new DesiredVelocity { _Value = new float3(_velocity.x, _velocity.y, _velocity.z), _IsSettingY = _isSettingYForThisFrame, _Gravity = GetActualGravity() });
            _isSettingYForThisFrame = false;
            // 1.7 ms
            _em.SetComponentData(_physicsEntity, new DesiredRotation { _Value = _transformForEntityCollider.rotation });

            // 4 ms
            if (!_em.GetComponentData<DesiredPosition>(_physicsEntity)._IsSetting)
                _transform.position = Vector3.Lerp(_transform.position, _em.GetComponentData<ActivePositionVelocity>(_physicsEntity)._Pos, Time.deltaTime * 10f);

        }
    }

    public Vector3 GetVelocity() => _velocity;
    public float GetActualGravity()
    {
        if (_Human == null)
            return _gravity;
        return _gravity * (_Human._IsGrounded ? 0.7f : 1f);
    }
    public void SetActiveCollider(int i)
    {
        switch (i)
        {
            case 1:
                _em.SetComponentData<PhysicsCollider>(_physicsEntity, _collider1);
                break;
            case 2:
                _em.SetComponentData<PhysicsCollider>(_physicsEntity, _collider2);
                break;
            case 3:
                _em.SetComponentData<PhysicsCollider>(_physicsEntity, _collider3);
                break;
        }
    }
    public void SetPosition()
    {
        _em.SetComponentData(_physicsEntity, new DesiredPosition { _IsSetting = true, _Value = _transformForEntityCollider.position });
    }
    public void SetVelocity(Vector3 vel, bool isSettingY)
    {
        if (_isSettingYForThisFrame && !isSettingY) return;
        _velocity = vel;
        _isSettingYForThisFrame = isSettingY;
    }
    public void SetGravity(float value)
    {
        _gravity = value;
    }
    public void Enable()
    {
        _em.SetEnabled(_physicsEntity, true);
        foreach (var item in _colliderEntities)
        {
            _em.SetEnabled(item, true);
        }
        SetPosition();
    }
    public void Disable()
    {
        _em.SetEnabled(_physicsEntity, false);
        foreach (var item in _colliderEntities)
        {
            _em.SetEnabled(item, false);
        }
    }

    private void OnDestroy()
    {
        if (_colliderEntities == null) return;

        OnDestroyCalled();
    }
}
