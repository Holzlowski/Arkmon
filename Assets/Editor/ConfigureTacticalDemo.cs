using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A repeatable, scoped migration; preserves existing scene systems and authored props.
public static class ConfigureTacticalDemo
{
    [MenuItem("Arkmon/Configure tactical demo")]
    public static void Run()
    {
        Import("Assets/Resources/Prince-Sleeping-Bed.png", 350);
        Sprite fence = Import("Assets/Sprites/Environment/WoodenFence-Paper.png", 600);
        AbilityDefinition ember = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Scriptable Objects/Abilities/Ember.asset");
        if (ember == null) { ember = ScriptableObject.CreateInstance<AbilityDefinition>(); AssetDatabase.CreateAsset(ember, "Assets/Scriptable Objects/Abilities/Ember.asset"); }
        ember.actionName = "Glutfunke"; ember.actionPointCost = 2; ember.damage = 18; ember.range = 1; EditorUtility.SetDirty(ember);
        BattleConfiguration battle = AssetDatabase.LoadAssetAtPath<BattleConfiguration>("Assets/Scriptable Objects/Battles/Default NPC Battle.asset");
        string path = AssetDatabase.GetAssetPath(battle.playerTeam[0]);
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var action = prefab.GetComponent<EmberAction>() ?? prefab.AddComponent<EmberAction>();
            var data = new SerializedObject(prefab.GetComponent<Unit>());
            var attacks = data.FindProperty("attacks");
            if (!Enumerable.Range(0, attacks.arraySize).Any(i => attacks.GetArrayElementAtIndex(i).objectReferenceValue == action))
            { attacks.InsertArrayElementAtIndex(attacks.arraySize); attacks.GetArrayElementAtIndex(attacks.arraySize - 1).objectReferenceValue = action; }
            data.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        MonsterDefinition cat = battle.playerTeam[0].Definition;
        if (!cat.abilities.Contains(ember)) { cat.abilities = cat.abilities.Concat(new[] { ember }).ToArray(); EditorUtility.SetDirty(cat); }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        foreach (BurningObstacle obstacle in Object.FindObjectsByType<BurningObstacle>(FindObjectsSortMode.None))
        {
            bool stone = !obstacle.IsVegetation;
            bool tree = obstacle.name.ToLowerInvariant().Contains("tree");
            obstacle.Configure(stone ? BurningObstacle.MaterialKind.Stone : BurningObstacle.MaterialKind.Vegetation,
                stone ? 45 : tree ? 60 : 30, true, tree);
            if (obstacle.GetComponent<ObstacleConditionVisual>() == null) obstacle.gameObject.AddComponent<ObstacleConditionVisual>();
            EditorUtility.SetDirty(obstacle);
        }
        foreach (DestructibleCrate crate in Object.FindObjectsByType<DestructibleCrate>(FindObjectsSortMode.None))
        {
            var obstacle = crate.GetComponent<BurningObstacle>() ?? crate.gameObject.AddComponent<BurningObstacle>();
            obstacle.Configure(BurningObstacle.MaterialKind.Wood, 45, true);
            if (crate.GetComponent<ObstacleConditionVisual>() == null) crate.gameObject.AddComponent<ObstacleConditionVisual>();
            EditorUtility.SetDirty(obstacle);
        }
        var grid = new SerializedObject(Object.FindFirstObjectByType<LevelGrid>());
        Vector3 origin = grid.FindProperty("origin").vector3Value; float size = grid.FindProperty("cellSize").floatValue;
        var parent = GameObject.Find("Tactical cover props");
        if (parent != null) Object.DestroyImmediate(parent);
        parent = new GameObject("Tactical cover props");
        foreach (GridPosition cell in new[] { new GridPosition(5, 4), new GridPosition(7, 6) })
        {
            Vector3 position = origin + new Vector3(cell.x * size, 0, cell.z * size);
            if (Object.FindObjectsByType<BurningObstacle>(FindObjectsSortMode.None).Any(o => Vector3.Distance(o.transform.position, position) < size * .6f)) continue;
            var obj = new GameObject("Holzzaun"); obj.transform.SetParent(parent.transform); obj.transform.position = position;
            obj.layer = LayerMask.NameToLayer("Obstacles");
            var collider = obj.AddComponent<BoxCollider>(); collider.center = Vector3.up * .65f; collider.size = new Vector3(1.6f, 1.3f, .7f);
            var image = new GameObject("Paper illustration", typeof(SpriteRenderer)); image.transform.SetParent(obj.transform, false);
            image.GetComponent<SpriteRenderer>().sprite = fence;
            image.transform.localScale = Vector3.one * (1.9f / fence.bounds.size.x);
            var prop = obj.AddComponent<PaperPropVisual>(); var visual = new SerializedObject(prop);
            visual.FindProperty("paperPivot").objectReferenceValue = image.transform; visual.ApplyModifiedPropertiesWithoutUndo();
            obj.AddComponent<BurningObstacle>().Configure(BurningObstacle.MaterialKind.Wood, 30, true);
            obj.AddComponent<ObstacleConditionVisual>();
        }
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("Tactical demo configured: waking art, Glutfunke, cover health and wooden fences.");
    }
    private static Sprite Import(string path, float ppu)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu; importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter; settings.spritePivot = new Vector2(.5f, 0);
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
