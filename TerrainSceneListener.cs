using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class TerrainSceneListener : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        if (sceneName.StartsWith("Terrain_"))
        {
            Vector2Int coords;
            if (TryParseTerrainCoordinates(sceneName, out coords))
            {
                Debug.Log($"Terrain loaded: {coords.x}, {coords.y}");

                GameManager._Instance.LoadChunk(coords.x, coords.y);
            }
        }
    }
    private void OnSceneUnloaded(Scene scene)
    {
        string sceneName = scene.name;

        if (sceneName.StartsWith("Terrain_"))
        {
            Vector2Int coords;
            if (TryParseTerrainCoordinates(sceneName, out coords))
            {
                Debug.Log($"Terrain UNLOADED: {coords.x}, {coords.y}");
                GameManager._Instance.UnloadChunk(coords.x, coords.y);
            }
        }
    }


    private bool TryParseTerrainCoordinates(string sceneName, out Vector2Int coords)
    {
        coords = Vector2Int.zero;
        var match = Regex.Match(sceneName, @"Terrain_(\d+)_(\d+)");
        if (match.Success)
        {
            int x = int.Parse(match.Groups[1].Value);
            int y = int.Parse(match.Groups[2].Value);
            coords = new Vector2Int(x, y);
            return true;
        }
        return false;
    }
}