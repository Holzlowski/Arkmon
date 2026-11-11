using UnityEditor;
using UnityEditor.SceneManagement;

public static class StarterBattleComparison
{
    private const string Scene = "Assets/Scenes/BattleScene.unity";
    private const string DefaultBattle = "Assets/Scriptable Objects/Battles/Default NPC Battle.asset";
    private const string PanfrieBattle = "Assets/Scriptable Objects/Battles/Panfrie comparison.asset";

    [MenuItem("Arkmon/Kampfprobe/Kafla gegen Distelwolle")]
    private static void PlayKafla() => Play(DefaultBattle);

    [MenuItem("Arkmon/Kampfprobe/Panfrie gegen Distelwolle")]
    private static void PlayPanfrie() => Play(PanfrieBattle);

    [MenuItem("Arkmon/Kampfprobe/Liora gegen Distelwolle")]
    private static void PlayLiora() => Play("Assets/Scriptable Objects/Battles/Liora comparison.asset");

    [MenuItem("Arkmon/Kampfprobe/Rauk gegen Distelwolle")]
    private static void PlayRauk() => Play("Assets/Scriptable Objects/Battles/Rauk comparison.asset");

    [MenuItem("Arkmon/Kampfprobe/Feldprüfung - Haltet das Siegel")]
    private static void PlayFieldTrial() => Play("Assets/Resources/Field Seal Trial.asset");

    [MenuItem("Arkmon/Kampfprobe/Goldene Felder - Arenaprobe")]
    private static void PlayGoldenArena() => Play("Assets/Resources/Golden Field Trial.asset", true, 3);

    [MenuItem("Arkmon/Kampfprobe/Goldene Felder - Vergleich mit vier Golems")]
    private static void PlayGoldenArenaFour() => Play("Assets/Resources/Golden Field Trial.asset", true, 4);

    private static void Play(string configuration, bool goldenArena = false, int golems = 4)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(Scene);
        SessionState.SetString("Arkmon.StarterBattle", configuration);
        SessionState.SetBool("Arkmon.GoldenFieldReview",goldenArena);
        if (goldenArena) SessionState.SetInt("Arkmon.GoldenGolemCount", golems);
        EditorApplication.EnterPlaymode();
    }
}
