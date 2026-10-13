using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Cinemachine;

// Session progress survives the courtyard/battle loop, but a new play session starts fresh.
public class DemoOpening : MonoBehaviour
{
    public static bool Started { get; private set; }
    public static bool HasMantle { get; private set; }
    public static bool HasLamp { get; private set; }
    public static bool IsEquipped => HasMantle && HasLamp;
    public static bool EnteredCourtyard { get; private set; }
    public static bool HasAwakened { get; private set; }
    public static bool PassedFirstTrial { get; private set; }
    public static bool TrialIntroductionSeen { get; private set; }
    private static bool firstTrialPending;
    public static void BeginTrial(string speaker)
    {
        firstTrialPending = speaker == "John" && !TrialIntroductionSeen;
        if (firstTrialPending) TrialIntroductionSeen = true;
    }
    public static bool ConsumeTrialGuide()
    {
        bool pending = firstTrialPending; firstTrialPending = false; return pending;
    }
    public static bool IsPresenting => presentation != null && presentation.IsBusy
        || CourtyardArrivalPresentation.IsPresenting || DemoCompletionPanel.IsOpen;
    private static DemoOpeningPresentation presentation;
    public static void RecordTrialVictory() => PassedFirstTrial = true;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Started = HasMantle = HasLamp = EnteredCourtyard = HasAwakened = PassedFirstTrial = TrialIntroductionSeen = firstTrialPending = false; presentation = null; }
    [SerializeField] private bool towerRoom;
    [SerializeField] private Vector3 courtyardArrival = new Vector3(-17, 1, 0);
    private TextMeshProUGUI objective;
    private IEnumerator Start()
    {
        if (towerRoom) Started = true;
        var hud = new GameObject("Demo objective", typeof(Canvas)); hud.transform.SetParent(transform);
        var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        PaperUIFactory.Scale(canvas);
        var style = PaperUIStyle.Load();
        var panel = PaperUIFactory.Image("Objective paper", hud.transform, style.panel,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -102), new Vector2(580, -24));
        objective = PaperUIFactory.Text("Objective", panel.transform, "", 23, Vector2.zero, Vector2.one,
            new Vector2(24, 12), new Vector2(-24, -12));
        yield return null;
        GameObject player = GameObject.FindWithTag("Player");
        ApplyOutfit(player);
        if (towerRoom)
        {
            presentation = gameObject.AddComponent<DemoOpeningPresentation>();
            if (!HasAwakened && !HasMantle)
            { yield return presentation.StartCoroutine(presentation.WakeUp(player)); HasAwakened = true; }
        }
        if (towerRoom && !IsEquipped)
        {
            DialogueBoxController.instance.ShowMessage("Heute ist der große Tag meiner Prüfung. Noch bevor die Arkanmeisterin an den Hof kommt … Meine Lampe steht auf dem Tisch, mein grüner Mantel hängt im Schrank. Beides brauche ich für John.", "Der Junge");
            var camera = FindFirstObjectByType<CinemachineVirtualCamera>();
            var follow = camera != null ? camera.GetCinemachineComponent<CinemachineTransposer>() : null;
            if (follow != null)
            {
                Vector3 overview = follow.m_FollowOffset;
                Vector3 opening = overview + new Vector3(-2, 1, 2);
                for (float time = 0; time < 1.2f; time += Time.deltaTime)
                { follow.m_FollowOffset = Vector3.Lerp(opening, overview, Mathf.SmoothStep(0, 1, time / 1.2f)); yield return null; }
                follow.m_FollowOffset = overview;
            }
            else if (camera != null)
            {
                // Exploration scenes use a FramingTransposer, not the battle camera's Transposer.
                var framing = camera.GetCinemachineComponent<CinemachineFramingTransposer>();
                if (framing != null)
                {
                    float distance = framing.m_CameraDistance;
                    Vector3 focus = framing.m_TrackedObjectOffset;
                    for (float time = 0; time < 1.2f; time += Time.deltaTime)
                    {
                        float progress = Mathf.SmoothStep(0, 1, time / 1.2f);
                        framing.m_CameraDistance = Mathf.Lerp(distance * .9f, distance, progress);
                        framing.m_TrackedObjectOffset = Vector3.Lerp(focus + new Vector3(-1.2f, .3f, .7f), focus, progress);
                        yield return null;
                    }
                    framing.m_CameraDistance = distance; framing.m_TrackedObjectOffset = focus;
                }
            }
        }
        else if (!towerRoom && Started && !EnteredCourtyard)
        {
            EnteredCourtyard = true;
            Transform entrance = GameObject.Find("Tower entrance")?.transform;
            if (player != null) player.transform.position = entrance != null
                ? new Vector3(entrance.position.x, courtyardArrival.y, entrance.position.z - 1.4f)
                : courtyardArrival;
            StartCoroutine(CourtyardGreeting());
        }
    }
    private IEnumerator CourtyardGreeting()
    {
        while (SceneLoadManager.IsTransitioning) yield return null;
        yield return new WaitForSecondsRealtime(.3f);
        var john=FindObjectsByType<NPC>(FindObjectsSortMode.None);
        NPC examiner=System.Array.Find(john,n=>n.npcName=="John");
        var player=GameObject.FindWithTag("Player");
        if(examiner!=null && player!=null)
            yield return gameObject.AddComponent<CourtyardArrivalPresentation>().Reveal(player.transform,examiner.transform);
        if (Started && EnteredCourtyard && DialogueBoxController.instance != null && !DialogueBoxController.instance.IsOpen)
            DialogueBoxController.instance.ShowMessage("Da ist der Hof. John wartet am Übungsplatz … Heute zeigen Kafla und ich, was wir können!", "Der Junge");
    }
    private void Update()
    {
        if (objective == null) return;
        if (IsPresenting)
        { objective.text = !towerRoom ? PassedFirstTrial ? "Prüfung bestanden · Du und Kafla seid bereit!" : "Ankunft im Hof · John wartet am Übungsplatz"
            : IsEquipped ? "Bereit für deine Prüfung" : HasAwakened ? "Vorbereitung · Lampe und Mantel für deine Prüfung" : "Der Morgen deiner Prüfung"; return; }
        objective.text = towerRoom ? (IsEquipped ? "Bereit!  ·  Geh durch die Tür in den Hof" : HasMantle ? "Vorbereitung · Hol deine Lampe vom Tisch" : HasLamp ? "Vorbereitung · Hol deinen Mantel aus dem Schrank" : "Vorbereitung · Lampe vom Tisch und Mantel aus dem Schrank")
            : PassedFirstTrial ? "Neuer Auftrag · Erwarte die Arkanmeisterin. Bei John kannst du weiter üben."
            : "Deine Prüfung  ·  Sprich mit John beim Übungsplatz [E]";

    }
    public static void TakeMantle()
    {
        if (HasMantle || IsPresenting) return;
        if (presentation != null) presentation.StartCoroutine(presentation.Dress(GameObject.FindWithTag("Player")));
        else FinishTakingMantle();
    }
    public static void FinishTakingMantle() { HasMantle = true; ApplyOutfit(GameObject.FindWithTag("Player")); }
    public static void TakeLamp()
    {
        if (HasLamp || IsPresenting) return;
        HasLamp = true;
        ApplyOutfit(GameObject.FindWithTag("Player"));
        foreach (var lamp in FindObjectsByType<CollectiblePlayerLamp>(FindObjectsSortMode.None)) lamp.Refresh();
        DialogueBoxController.instance?.ShowMessage(HasMantle
            ? "Meine Lampe! Kafla wartet darin. Mit Mantel und Lampe bin ich jetzt bereit für John."
            : "Meine Lampe! Kafla wartet darin. Jetzt fehlt noch der grüne Mantel aus dem Schrank.", "Der Junge");
    }
    private static void ApplyOutfit(GameObject player)
    {
        if (player == null || !Started) return;
        var visual = player.GetComponent<PaperCharacterVisual>();
        if (visual != null) visual.SetRestingSprite(PlayerEquipmentAppearance.RestingSprite);
    }
    public static void LeaveRoom()
    {
        if (IsPresenting) return;
        if (!IsEquipped) { DialogueBoxController.instance.ShowMessage(!HasMantle && !HasLamp
            ? "Für die Prüfung brauche ich meinen Mantel aus dem Schrank und meine Lampe vom Tisch."
            : !HasLamp ? "Meine Lampe steht noch auf dem Tisch. Ohne sie kann ich Kafla nicht rufen."
            : "Erst meinen grünen Mantel holen. Er hängt im Kleiderschrank.", "Der Junge"); return; }
        SceneLoadManager.instance.LoadExplorationScene("StartScene");
    }
}
