using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Two open flanks and a breakable centre instead of a cluttered obstacle cluster.
public static class ConfigureTrialArena
{
    [MenuItem("Arkmon/Configure John trial arena")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        var grid = new SerializedObject(Object.FindFirstObjectByType<LevelGrid>());
        Vector3 origin = grid.FindProperty("origin").vector3Value;
        float cell = grid.FindProperty("cellSize").floatValue;
        var obstacles = Object.FindObjectsByType<BurningObstacle>(FindObjectsSortMode.None)
            .OrderBy(o => o.name).ThenBy(o => o.transform.position.x).ToArray();
        int trees = 0, rocks = 0, fences = 0, bushes = 0, crates = 0;
        foreach (var obstacle in obstacles)
        {
            string name = obstacle.name.ToLowerInvariant(); GridPosition position;
            if (name.Contains("tree")) { if (trees >= 2) continue; position = trees++ == 0 ? new GridPosition(3, 8) : new GridPosition(9, 0); }
            else if (!obstacle.IsVegetation) { if (rocks >= 2) continue; position = rocks++ == 0 ? new GridPosition(4, 5) : new GridPosition(6, 5); }
            else if (name.Contains("zaun") || name.Contains("fence")) { if (fences >= 2) continue; position = fences++ == 0 ? new GridPosition(5, 4) : new GridPosition(6, 7); }
            else if (name.Contains("crate")) { if (crates++ >= 1) continue; position = new GridPosition(7, 3); }
            else { if (bushes >= 2) continue; position = bushes++ == 0 ? new GridPosition(4, 4) : new GridPosition(6, 6); }
            obstacle.transform.position = origin + new Vector3(position.x * cell, obstacle.transform.position.y - origin.y, position.z * cell);
            EditorUtility.SetDirty(obstacle.transform);
        }
        EditorSceneManager.SaveScene(scene);
    }
}
