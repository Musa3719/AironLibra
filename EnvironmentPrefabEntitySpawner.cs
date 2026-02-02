using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class EnvironmentPrefabEntitySpawner : MonoBehaviour
{
    public GameObject entityPrefab; // Authoring prefab

    void Start()
    {
        /*var world = World.DefaultGameObjectInjectionWorld;
        world.EntityManager.Instantiate(entity);

        Entity prefabEntity =
            world.GetExistingSystemManaged<>()
            .GetPrimaryEntity(entityPrefab); // */
    }
}