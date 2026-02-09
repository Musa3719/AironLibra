using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class EntityCollider : MonoBehaviour
{
    [SerializeField] protected Transform _transformForEntityCollider;
    public ItemHandleData _ItemHandleData { get; set; }

    protected Unity.Entities.EntityManager _em;
    protected List<Entity> _colliderEntities;


    void Start()
    {
        CreateColliderEntities(false);
    }
    public void CreateColliderEntities(bool isFromPhysicsObject)
    {
        _colliderEntities = new List<Entity>();
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (_transformForEntityCollider == null)
        {
            Transform collidersParent = transform.Find("Colliders");
            if (collidersParent == null)
            {
                Debug.LogWarning("Colliders child not found!");
                return;
            }

            foreach (Transform colTr in collidersParent)
            {
                AddOneEntityForCollider(colTr, isFromPhysicsObject);
            }
        }
        else
        {
            AddOneEntityForCollider(_transformForEntityCollider, isFromPhysicsObject);
        }

    }
    private Entity AddOneEntityForCollider(Transform colTr, bool isFromPhysicsObject, bool isDestroying = false)
    {
        UnityEngine.Collider unityCol = colTr.GetComponent<UnityEngine.Collider>();
        if (!unityCol) return Entity.Null;

        if (!Mathf.Approximately(unityCol.transform.lossyScale.x, unityCol.transform.lossyScale.y) || !Mathf.Approximately(unityCol.transform.lossyScale.y, unityCol.transform.lossyScale.z))
        {
            Debug.LogError("Collider Named : " + unityCol.name + "' Has Non Uniform Scale!");
        }

        Entity e = _em.CreateEntity(
            typeof(LocalTransform),
            typeof(PhysicsCollider)
        );

        // --- TRANSFORM ---
        _em.SetComponentData(e, LocalTransform.FromPositionRotationScale(
            colTr.position,
            colTr.rotation,
            colTr.lossyScale.x // uniform varsayýmý
        ));

        // --- COLLIDER ---
        BlobAssetReference<Unity.Physics.Collider> physicsCollider;

        if (unityCol is UnityEngine.BoxCollider box)
        {
            physicsCollider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Center = box.center,
                Size = box.size,
                Orientation = quaternion.identity
            }, CollisionFilter.Default, isFromPhysicsObject ? EntityManager._Instance._NoFrictionMaterial : Unity.Physics.Material.Default);
        }
        else if (unityCol is UnityEngine.SphereCollider sphere)
        {
            physicsCollider = Unity.Physics.SphereCollider.Create(new SphereGeometry
            {
                Center = sphere.center,
                Radius = sphere.radius
            }, CollisionFilter.Default, isFromPhysicsObject ? EntityManager._Instance._NoFrictionMaterial : Unity.Physics.Material.Default);
        }
        else if (unityCol is UnityEngine.CapsuleCollider capsule)
        {
            physicsCollider = CreateCapsulePhysicsCollider(capsule, isFromPhysicsObject);
        }
        else if (unityCol is UnityEngine.TerrainCollider terrainCol)
        {
            physicsCollider = CreateTerrainPhysicsCollider(terrainCol);
        }
        else
        {
            Debug.LogError("Collider Named : " + unityCol.name + "' Is Another Type of Collider! Type is : " + unityCol.GetType());
            _em.DestroyEntity(e);
            return Entity.Null;
        }

        _em.SetComponentData(e, new PhysicsCollider
        {
            Value = physicsCollider
        });
        _em.AddSharedComponent(e, new PhysicsWorldIndex
        {
            Value = 0
        });

        if (!isDestroying)
            _colliderEntities.Add(e);

        return e;
    }
    protected PhysicsCollider GetPhysicsColliderFromCapsuleCollider(Transform colliderTransform)
    {
        Entity e = AddOneEntityForCollider(colliderTransform, true, true);
        PhysicsCollider physicsCollider = _em.GetComponentData<PhysicsCollider>(e);
        _em.DestroyEntity(e);
        return physicsCollider;
    }
    private void OnDestroy()
    {
        OnDestroyCalled();
    }
    public void OnDestroyCalled()
    {
        if (World.DefaultGameObjectInjectionWorld == null || !World.DefaultGameObjectInjectionWorld.IsCreated)
            return;

        CleanupEntities();
    }
    public void CleanupEntities()
    {
        if (_colliderEntities == null) return;

        foreach (var e in _colliderEntities)
        {
            if (!_em.Exists(e))
                continue;

            if (_em.HasComponent<PhysicsCollider>(e))
            {
                var col = _em.GetComponentData<PhysicsCollider>(e);
                if (col.Value.IsCreated)
                    col.Value.Dispose();
            }

            _em.DestroyEntity(e);
        }

        _colliderEntities.Clear();
    }

    private BlobAssetReference<Unity.Physics.Collider> CreateCapsulePhysicsCollider(UnityEngine.CapsuleCollider capsule, bool isFromPhysicsObject)
    {
        float radius = capsule.radius;
        float height = math.max(capsule.height, radius * 2f);


        float3 up = capsule.direction switch
        {
            0 => new float3(1, 0, 0),
            1 => new float3(0, 1, 0),
            _ => new float3(0, 0, 1)
        };

        float halfCylinder = (height * 0.5f) - radius;

        return Unity.Physics.CapsuleCollider.Create(
            new CapsuleGeometry
            {
                Radius = radius,
                Vertex0 = (float3)capsule.center - up * halfCylinder,
                Vertex1 = (float3)capsule.center + up * halfCylinder
            },
            CollisionFilter.Default,
            isFromPhysicsObject ? EntityManager._Instance._NoFrictionMaterial : Unity.Physics.Material.Default
        );
    }

    private BlobAssetReference<Unity.Physics.Collider> CreateTerrainPhysicsCollider(UnityEngine.TerrainCollider terrainCollider)
    {
        TerrainData data = terrainCollider.terrainData;

        int resolution = data.heightmapResolution;

        float[,] heights2D = data.GetHeights(0, 0, resolution, resolution);

        // HeightFieldCollider 1D float array ister
        NativeArray<float> heights = new NativeArray<float>(
            resolution * resolution,
            Allocator.Persistent
        );

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[z * resolution + x] = heights2D[z, x];
            }
        }

        float3 scale = new float3(data.size.x / (resolution - 1), data.size.y, data.size.z / (resolution - 1));

        var collider = Unity.Physics.TerrainCollider.Create(
            heights,
            new int2(resolution, resolution),
            scale,
            Unity.Physics.TerrainCollider.CollisionMethod.Triangles,
            CollisionFilter.Default,
            Unity.Physics.Material.Default
        );

        return collider;
    }
}