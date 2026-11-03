using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixTowerSilhouetteCollision
{
    [MenuItem("Arkmon/Fit tower player silhouette collision")]
    public static void Run()
    {
        ImportElianWalkRig.Run();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        var player = GameObject.FindWithTag("Player");
        // The billboard leans back with the camera: its head is behind its feet.
        // Keep the original floor contact while enclosing that visible depth and walking arms.
        var body = player.GetComponent<BoxCollider>();
        body.size = new Vector3(1.30f, 1.93f, 1.25f);
        body.center = new Vector3(-.06f, 0, .40f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}
