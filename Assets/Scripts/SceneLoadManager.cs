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
        bool roomPassage = IntroChapter.Active && !DemoOpening.HasLamp
            && (destination == "StartScene" || destination == IntroChapter.TowerScene)
            && (SceneManager.GetActiveScene().name == IntroChapter.TowerScene || SceneManager.GetActiveScene().name == "StartScene");
        LampIrisGraphic iris = roomPassage ? null : rect.gameObject.AddComponent<LampIrisGraphic>();
        Image black = roomPassage ? rect.gameObject.AddComponent<Image>() : null;
        if(iris != null){iris.color = Color.black; iris.raycastTarget = true;}
        if(black != null){black.color = new Color(0,0,0,0);black.raycastTarget=true;}
        try
        {
            if(roomPassage)yield return AnimateBlack(black,0,1);else yield return AnimateIris(iris, 1f, 0f);
            yield return SceneManager.LoadSceneAsync(destination);
            if (returning) OnSceneLoaded(null);
            yield return null;
            if(roomPassage)yield return AnimateBlack(black,1,0);else yield return AnimateIris(iris, 0f, 1f);
        }
        finally { Destroy(overlay); transitioning = false; }
        if (returning) ShowReturnDialogue();
    }
    private static IEnumerator AnimateBlack(Image image,float from,float to)
    {
        for(float t=0;t<.4f;t+=Time.unscaledDeltaTime)
        {image.color=new Color(0,0,0,Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,t/.4f)));yield return null;}
        image.color=new Color(0,0,0,to);
    }

    private void ShowReturnDialogue()
    {
        if (!returnResult.HasValue || DialogueBoxController.instance == null) return;
        UnitManager.BattleState result = returnResult.Value; returnResult = null;
        if (battleSpeaker == ArkanmeisterinConversation.Speaker)
        {
            CourtyardStarterMeetings.FieldTrialWon = result == UnitManager.BattleState.Won;
            DialogueBoxController.instance.ShowMessage(CourtyardStarterMeetings.FieldTrialWon
                ? "Gut gemacht, Elian. Gemeinsam habt Ihr das Siegel beschützt."
                : "Sammelt Euch kurz. Wenn Ihr bereit seid, versuchen wir es erneut.",battleSpeaker);
            return;
        }
        NPC speaker = null;
        foreach (NPC npc in FindObjectsByType<NPC>(FindObjectsSortMode.None))
            if (speaker == null || npc.npcName == battleSpeaker) { speaker = npc; if (npc.npcName == battleSpeaker) break; }
        string name = speaker != null ? speaker.npcName : "Dein Prüfer";
        if (speaker != null) speaker.FacePlayer();
        string line = result == UnitManager.BattleState.Won
            ? DemoOpening.PassedFirstTrial ? "Gut gekämpft! Kafla und du werdet ein eingespieltes Team. Für eine weitere Übungsrunde bin ich jederzeit bereit."
              : "Prüfung bestanden! Du und Kafla seid bereit. Als Nächstes erwarten wir die Arkanmeisterin am Hof. Bis dahin können wir jederzeit weiter üben."
            : result == UnitManager.BattleState.Lost
            ? "Jeder Kampf lehrt uns etwas. Achte auf Kaflas Zustände und nutze die freien Felder. Versuchen wir es später noch einmal!"
            : "Eine Pause ist völlig in Ordnung. Komm wieder, wenn du und Kafla bereit für die nächste Prüfung seid.";
        bool firstVictory=result==UnitManager.BattleState.Won && battleSpeaker=="John" && !DemoOpening.PassedFirstTrial;
        DialogueBoxController.instance.ShowMessage(line, name);
        if (result == UnitManager.BattleState.Won && battleSpeaker == "John") DemoOpening.RecordTrialVictory();
        if(firstVictory)DemoCompletionPanel.Queue();
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
