using System.Collections;
using System.Linq;
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
    public static bool EnteredCourtyard { get; private set; }
    public static bool HasAwakened { get; private set; }
    public static bool PassedFirstTrial { get; private set; }
    public static bool IsPresenting => presentation != null && presentation.IsBusy;
    private static DemoOpeningPresentation presentation;
    public static void RecordTrialVictory() => PassedFirstTrial = true;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Started = HasMantle = EnteredCourtyard = HasAwakened = PassedFirstTrial = false; presentation = null; }
    [SerializeField] private bool towerRoom;
    [SerializeField] private Vector3 courtyardArrival = new Vector3(-17, 1, 0);
    private TextMeshProUGUI objective;
    private DemoInteractable[] interactions;
    private Transform playerTransform;
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
        playerTransform = player != null ? player.transform : null;
        interactions = FindObjectsByType<DemoInteractable>(FindObjectsSortMode.None);
        ApplyOutfit(player);
        if (towerRoom)
        {
            presentation = gameObject.AddComponent<DemoOpeningPresentation>();
            if (!HasAwakened && !HasMantle)
            { yield return presentation.StartCoroutine(presentation.WakeUp(player)); HasAwakened = true; }
        }
        if (towerRoom && !HasMantle)
        {
            DialogueBoxController.instance.ShowMessage("Heute ist der große Tag meiner Prüfung. Und die Arkanmeisterin kommt an den Hof … Ich sollte mich vorbereiten.", "Der Junge");
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
        }
    }
    private void Update()
    {
        if (objective == null) return;
        if (IsPresenting)
        { objective.text = HasAwakened ? "Vorbereitung · Du ziehst deinen Mantel an" : "Der Morgen deiner Prüfung"; return; }
        objective.text = towerRoom ? (HasMantle ? "Bereit!  ·  Geh durch die Tür in den Hof" : "Vorbereitung  ·  Hol deinen Mantel aus dem Schrank")
            : PassedFirstTrial ? "Prüfung bestanden  ·  Die Arkanmeisterin wird erwartet. Bei John kannst du weiter üben."
            : "Deine Prüfung  ·  Sprich mit John beim Übungsplatz [E]";
        if (playerTransform != null && interactions != null && !SceneLoadManager.IsTransitioning && DialogueBoxController.instance != null && !DialogueBoxController.instance.IsOpen)
        {
            var closest = interactions.Where(item => item != null)
                .OrderBy(item => (item.transform.position - playerTransform.position).sqrMagnitude).FirstOrDefault();
            if (closest != null && Vector3.Distance(closest.transform.position, playerTransform.position) < 3)
                objective.text += "  ·  [E] " + (closest.kind == DemoInteractable.Kind.Wardrobe ? "Mantel holen"
                    : closest.kind == DemoInteractable.Kind.RoomDoor ? "In den Hof" : closest.kind == DemoInteractable.Kind.Window ? "Aussicht ansehen" : "Ansehen");
        }
    }
    public static void TakeMantle()
    {
        if (HasMantle || IsPresenting) return;
        if (presentation != null) presentation.StartCoroutine(presentation.Dress(GameObject.FindWithTag("Player")));
        else FinishTakingMantle();
    }
    public static void FinishTakingMantle() { HasMantle = true; ApplyOutfit(GameObject.FindWithTag("Player")); }
    private static void ApplyOutfit(GameObject player)
    {
        if (player == null || !Started) return;
        var visual = player.GetComponent<PaperCharacterVisual>();
        if (visual != null) visual.SetRestingSprite(Resources.Load<Sprite>(HasMantle ? "Prince-Mantle" : "Prince-Room"));
    }
    public static void LeaveRoom()
    {
        if (IsPresenting) return;
        if (!HasMantle) { DialogueBoxController.instance.ShowMessage("Erst meinen grünen Mantel holen. Er hängt im Kleiderschrank.", "Der Junge"); return; }
        SceneLoadManager.instance.LoadExplorationScene("StartScene");
    }
}
