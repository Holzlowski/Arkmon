using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager instance;
    public BattleConfiguration PendingBattle { get; private set; }
    public static bool IsTransitioning => instance != null && instance.transitioning;
    private bool transitioning;
    private UnitManager.BattleState? returnResult;
    private string battleSpeaker;
    public void SetBattleSpeaker(string speaker) => battleSpeaker = speaker;
    public void RecordBattleResult(UnitManager.BattleState result) => returnResult = result;
    public void LoadExplorationScene(string sceneName)
    {
        if (transitioning) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName)) { Debug.LogError("Exploration scene missing: " + sceneName); return; }
        StartCoroutine(Transition(sceneName, false));
    }
    public void LoadBattleScene(string sceneName)
    {
        if (transitioning) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        { Debug.LogError("Battle scene is not in the build settings: " + sceneName); return; }
        returnResult = null;
        StartCoroutine(Transition(sceneName, false));
    }

    private IEnumerator Transition(string destination, bool returning)
    {
        transitioning = true;
        string musicScene = returning ? System.IO.Path.GetFileNameWithoutExtension(destination) : destination;
        SoundManager.instance?.BeginSceneTransition(musicScene);
        var overlay = new GameObject("Lamp scene transition", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
        RectTransform rect = PaperUIFactory.Rect("Lamp iris", overlay.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        LampIrisGraphic iris = rect.gameObject.AddComponent<LampIrisGraphic>();
        iris.color = Color.black; iris.raycastTarget = true;
        try
        {
            yield return AnimateIris(iris, 1f, 0f);
            yield return SceneManager.LoadSceneAsync(destination);
            if (returning) OnSceneLoaded(null);
            yield return null;
            yield return AnimateIris(iris, 0f, 1f);
        }
        finally { Destroy(overlay); transitioning = false; }
        if (returning) ShowReturnDialogue();
    }

    private void ShowReturnDialogue()
    {
        if (!returnResult.HasValue || DialogueBoxController.instance == null) return;
        UnitManager.BattleState result = returnResult.Value; returnResult = null;
        NPC speaker = null;
        foreach (NPC npc in FindObjectsByType<NPC>(FindObjectsSortMode.None))
            if (speaker == null || npc.npcName == battleSpeaker) { speaker = npc; if (npc.npcName == battleSpeaker) break; }
        string name = speaker != null ? speaker.npcName : "Dein Prüfer";
        if (speaker != null) speaker.FacePlayer();
        string line = result == UnitManager.BattleState.Won
            ? "Gut gemacht! Du und Kafla habt eure Prüfung bestanden. Die Arkanmeisterin wird bald am Hof erwartet. Bis dahin können wir jederzeit weiter üben."
            : result == UnitManager.BattleState.Lost
            ? "Jeder Kampf lehrt uns etwas. Achte auf Kaflas Zustände und nutze die freien Felder. Versuchen wir es später noch einmal!"
            : "Eine Pause ist völlig in Ordnung. Komm wieder, wenn du und Kafla bereit für die nächste Prüfung seid.";
        DialogueBoxController.instance.ShowMessage(line, name);
        if (result == UnitManager.BattleState.Won && DemoOpening.Started) DemoOpening.RecordTrialVictory();
    }

    private static IEnumerator AnimateIris(LampIrisGraphic iris, float from, float to)
    {
        for (float elapsed = 0; elapsed < .7f; elapsed += Time.unscaledDeltaTime)
        {
            iris.SetOpenness(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / .7f)));
            yield return null;
        }
        iris.SetOpenness(to);
    }
    public void SetPendingBattle(BattleConfiguration configuration) => PendingBattle = configuration;
    // Hier könnten alle Daten gespeichert werden, die zwischen den Szenen ausgetauscht werden sollen
    [SerializeField] Vector3 playerPosition;
    [SerializeField] int sceneIndex;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SavePlayerTransform(Vector3 playerPosition)
    {
        this.playerPosition = playerPosition;
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
    }

    public void SetPlayertransform(Transform playerTransform)
    {
        playerTransform.position = this.playerPosition;
    }

    public int GetSceneIndex()
    {
        return sceneIndex;
    }


    public void LoadSceneBeforeBattle()
    {
        if (transitioning) return;
        PendingBattle = null;
        StartCoroutine(Transition(SceneUtility.GetScenePathByBuildIndex(sceneIndex), true));
    }

    private void OnSceneLoaded(AsyncOperation asyncOperation)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerPosition;
        }
        else
        {
            Debug.LogWarning("Player not found in the loaded scene.");
        }
    }
}
