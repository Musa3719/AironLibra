using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshSurfaceHandler : MonoBehaviour
{
    private NavMeshDataInstance _instance;

    void OnEnable()
    {
        if (TryGetComponent(out NavMeshSurface surface))
            _instance = NavMesh.AddNavMeshData(surface.navMeshData);
    }

    void OnDisable()
    {
        if (_instance.valid)
            _instance.Remove();
    }
}