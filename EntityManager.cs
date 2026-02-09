using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class EntityManager : MonoBehaviour
{
    public static EntityManager _Instance;
    public List<EntityPhysicObject> _List { get; private set; }
    public Unity.Physics.Material _NoFrictionMaterial;
    private void Awake()
    {
        _Instance = this;
        _List = new List<EntityPhysicObject>();
        _NoFrictionMaterial = new Unity.Physics.Material
        {
            Friction = 0.275f,
            Restitution = 0.25f,
            FrictionCombinePolicy = Unity.Physics.Material.CombinePolicy.Minimum,
            RestitutionCombinePolicy = Unity.Physics.Material.CombinePolicy.Minimum
        };
    }
    private void Update()
    {
        foreach (var item in _List)
        {
            item.UpdateEntity();
        }
    }
}


public struct ActivePositionVelocity : IComponentData
{
    public float3 _Pos;
    public float3 _Vel;
}
public struct DesiredVelocity : IComponentData
{
    public bool _IsSettingY;
    public float3 _Value;
    public float _Gravity;
}

public struct DesiredRotation : IComponentData
{
    public quaternion _Value;
}
public struct DesiredPosition : IComponentData
{
    public bool _IsSetting;
    public float3 _Value;
}


[BurstCompile]
public partial struct EntityPhysicsUpdaterSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (lt, vel, gravity, desiredVel, desiredRot, desiredPos, activePosVel) in
                 SystemAPI.Query<
                     RefRW<LocalTransform>,
                     RefRW<PhysicsVelocity>,
                     RefRW<PhysicsGravityFactor>,
                     RefRO<DesiredVelocity>,
                     RefRO<DesiredRotation>,
                     RefRW<DesiredPosition>,
                     RefRW<ActivePositionVelocity>>())
        {
            //  SET POSITION
            if (desiredPos.ValueRO._IsSetting)
            {
                lt.ValueRW.Position = desiredPos.ValueRO._Value;
                desiredPos.ValueRW._IsSetting = false;
            }

            //  SET ROTATION
            lt.ValueRW.Rotation = desiredRot.ValueRO._Value;

            //  SET VELOCITY
            float tempYVel = vel.ValueRO.Linear.y;
            vel.ValueRW.Linear = desiredVel.ValueRO._Value;
            if (!desiredVel.ValueRO._IsSettingY)
                vel.ValueRW.Linear.y = tempYVel;

            //  SET GRAVITY
            gravity.ValueRW.Value = desiredVel.ValueRO._Gravity * 2f;


            //  GET POSITION
            activePosVel.ValueRW._Pos = lt.ValueRO.Position;

            //  GET VELOCITY
            activePosVel.ValueRW._Vel = vel.ValueRO.Linear;
        }
    }
}