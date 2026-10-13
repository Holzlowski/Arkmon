using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ConfigureCourtyard
{
    [MenuItem("Arkmon/Configure castle courtyard")]
    public static void Run()
    {
        Scene battle = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity", OpenSceneMode.Single);
        GameObject meadow = GameObject.Find("Idyllic meadow");
        if (meadow == null) throw new System.InvalidOperationException("Battle meadow missing");
        Scene start = EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity", OpenSceneMode.Additive);
        SceneManager.SetActiveScene(start);
        var prior = FindRoot(start, "Castle horizon and golden fields");
        if (prior != null) Object.DestroyImmediate(prior);
        var landscape = Object.Instantiate(meadow);
        SceneManager.MoveGameObjectToScene(landscape, start);
        landscape.name = "Castle horizon and golden fields";
        landscape.transform.position += new Vector3(-18, 0, -12);
        // The courtyard floor stays clear; landscape remains decorative and collision-free.
        foreach (Collider collider in landscape.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
        SceneManager.SetActiveScene(battle);
        Material sky = RenderSettings.skybox;
        Color ambient = RenderSettings.ambientLight, fog = RenderSettings.fogColor;
        float fogDensity = RenderSettings.fogDensity;
        bool fogEnabled = RenderSettings.fog;
        Volume battleVolume = null;
        foreach (GameObject root in battle.GetRootGameObjects())
            if (root.GetComponentInChildren<Volume>() != null) { battleVolume = root.GetComponentInChildren<Volume>(); break; }
        VolumeProfile profile = battleVolume != null ? battleVolume.sharedProfile : null;
        SceneManager.SetActiveScene(start);
        RenderSettings.skybox = sky; RenderSettings.ambientLight = ambient;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.fog = fogEnabled; RenderSettings.fogColor = fog; RenderSettings.fogDensity = fogDensity;
        foreach (var root in start.GetRootGameObjects())
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>())
            { camera.clearFlags = CameraClearFlags.Skybox; camera.farClipPlane = Mathf.Max(camera.farClipPlane, 500); camera.fieldOfView = 70; }
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour == null || behaviour.GetType().Name != "CinemachineVirtualCamera") continue;
                var serialized = new SerializedObject(behaviour);
                var lens = serialized.FindProperty("m_Lens.FieldOfView");
                if (lens != null) { lens.floatValue = 70; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            }
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                if (renderer.name == "Roof tier" || renderer.name == "Rounded crown"
                    || renderer.name == "Crown cluster" || renderer.name == "Tree trunk") renderer.enabled = false;
            foreach (var volume in root.GetComponentsInChildren<Volume>()) if (profile != null) volume.sharedProfile = profile;
            foreach (var light in root.GetComponentsInChildren<Light>())
                if (light.type == LightType.Directional) { light.color = new Color(1f, .93f, .78f); light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(40, -35, 0); }
        }
        var oldWalls = FindRoot(start, "Castle courtyard enclosure");
        if (oldWalls != null) Object.DestroyImmediate(oldWalls);
        var walls = new GameObject("Castle courtyard enclosure");
        Material stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Warm limestone.mat");
        Material cap = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Light limestone.mat");
        Block(walls.transform, "Keep battlement roof", new Vector3(0, 5.4f, 11), new Vector3(10, .5f, 4.2f), stone);
        for (int x = -4; x <= 4; x += 2)
            foreach (int z in new[] { 9, 13 })
                Block(walls.transform, "Keep merlon", new Vector3(x, 6, z), new Vector3(1, .8f, .8f), cap);
        foreach (Vector3 position in new[] { new Vector3(-7, 0, 4), new Vector3(8, 0, 5), new Vector3(-7, 0, -3), new Vector3(9, 0, -3) })
            PaperTree(walls.transform, position);
        Block(walls.transform, "West rampart", new Vector3(-12, 1.3f, 1), new Vector3(1, 2.6f, 24), stone);
        Block(walls.transform, "East rampart", new Vector3(12, 1.3f, 1), new Vector3(1, 2.6f, 24), stone);
        Block(walls.transform, "North rampart", new Vector3(0, 1.3f, 13), new Vector3(24, 2.6f, 1), stone);
        foreach (int side in new[] { -1, 1 })
        {
            Block(walls.transform, "Gate wall", new Vector3(side * 7.5f, 1.3f, -11), new Vector3(9, 2.6f, 1), stone);
            Block(walls.transform, "Gate pillar", new Vector3(side * 3, 2.1f, -11), new Vector3(1.3f, 4.2f, 1.4f), cap);
            foreach (int end in new[] { -1, 1 })
            {
                Vector3 corner = new Vector3(side * 12, 2.75f, end == -1 ? -11 : 13);
                Block(walls.transform, "Courtyard tower", corner, new Vector3(3, 5.5f, 3), stone);
                Block(walls.transform, "Tower cornice", corner + Vector3.up * 2.8f, new Vector3(3.3f, .35f, 3.3f), cap);
            }
        }
        Block(walls.transform, "Gate arch lintel", new Vector3(0, 3.9f, -11), new Vector3(7.2f, .7f, 1.4f), cap);
        for (int z = -10; z <= 12; z += 2)
            foreach (int side in new[] { -1, 1 })
                Block(walls.transform, "Rampart merlon", new Vector3(side * 12, 2.95f, z), new Vector3(1.15f, .7f, 1), cap);
        for (int x = -10; x <= 10; x += 2)
            Block(walls.transform, "Rampart merlon", new Vector3(x, 2.95f, 13), new Vector3(1, .7f, 1.15f), cap);
        EditorSceneManager.SaveScene(start);
        EditorSceneManager.CloseScene(battle, true);
        AssetDatabase.SaveAssets();
        Debug.Log("Castle courtyard, matching sky and golden field horizon configured.");
    }
    private static GameObject FindRoot(Scene scene, string name)
    { foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root; return null; }
    private static void Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = name;
        block.transform.SetParent(parent, false); block.transform.position = position; block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
    }
    private static void PaperTree(Transform parent, Vector3 position)
    {
        var root = new GameObject("Courtyard paper tree"); root.transform.SetParent(parent, false); root.transform.position = position;
        var illustration = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(illustration.GetComponent<Collider>());
        illustration.name = "Tree illustration"; illustration.transform.SetParent(root.transform, false);
        illustration.transform.localPosition = Vector3.up * 2.6f; illustration.transform.localScale = new Vector3(4.5f, 5.5f, 1);
        illustration.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MeadowTree-Paper.mat");
        var visual = root.AddComponent<PaperPropVisual>(); var serialized = new SerializedObject(visual);
        serialized.FindProperty("paperPivot").objectReferenceValue = illustration.transform; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
