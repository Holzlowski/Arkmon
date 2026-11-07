using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A reversible optional view; it never moves the player or advances intro progress.
[DefaultExecutionOrder(9010)]
public sealed class CourtyardLookout : MonoBehaviour, IInteractableAsPlayer
{
    private static CourtyardLookout active;
    public static bool IsPresenting => active != null;
    public bool IsReturning => returning;
    public float Progress => progress;
    public string Label => "Über die Felder schauen";
    private CourtyardCamera follow;
    private Camera view;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float startFov, progress;
    private bool returning, objectiveWasEnabled;
    private Canvas objective, canvas;
    private CanvasGroup caption;
    private int startFrame;
    private CourtyardLookoutMusic scenicMusic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => active = null;

    internal static void Install()
    {
        if (GameObject.Find("Western field lookout") != null) return;
        var point = new Vector3(-40.9f, 0, 7);
        foreach (var hit in Physics.RaycastAll(point + Vector3.up * 20, Vector3.down, 40))
            if (hit.collider.CompareTag("Ground")) point.y = Mathf.Max(point.y, hit.point.y);
        var root = new GameObject("Western field lookout", typeof(SphereCollider), typeof(CourtyardLookout));
        root.transform.position = point;
        var trigger = root.GetComponent<SphereCollider>();
        trigger.isTrigger = true; trigger.center = Vector3.up * .8f; trigger.radius = .8f;
    }

    public Vector3 GetTransform() => transform.position;

    public void InteractWithPlayer()
    {
        if (active != null || !isActiveAndEnabled || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning
            || DialogueBoxController.instance != null && DialogueBoxController.instance.IsOpen) return;
        var player = GameObject.FindWithTag("Player");
        var controller = player != null ? player.GetComponent<PlayerController>() : null;
        if (controller == null || Vector3.Distance(player.transform.position, transform.position) > controller.interacRange) return;
        follow = FindFirstObjectByType<CourtyardCamera>(); view = Camera.main;
        if (follow == null || !follow.isActiveAndEnabled || view == null) return;
        startPosition = view.transform.position; startRotation = view.transform.rotation; startFov = view.fieldOfView;
        progress = 0; returning = false; startFrame = Time.frameCount; active = this;
        var body = player.GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic) body.linearVelocity = new Vector3(0, body.linearVelocity.y, 0);
        objective = GameObject.Find("Demo objective")?.GetComponent<Canvas>();
        if (objective != null) { objectiveWasEnabled = objective.enabled; objective.enabled = false; }
        BuildOverlay();
        scenicMusic = GetComponent<CourtyardLookoutMusic>() ?? gameObject.AddComponent<CourtyardLookoutMusic>();
        scenicMusic.Begin();
    }

    public void ReturnToCourtyard()
    {
        if (active == this) { returning = true; scenicMusic?.End(); }
    }

    private void Update()
    {
        if (active != this) return;
        if (SceneLoadManager.IsTransitioning || follow == null || !follow.isActiveAndEnabled || view == null) { Finish(); return; }
        // Ignore the E release which opened this interaction, regardless of script Update order.
        if (Time.frameCount > startFrame && (Input.GetKeyUp(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))) ReturnToCourtyard();
    }

    private void LateUpdate()
    {
        if (active != this || follow == null) return;
        progress = Mathf.MoveTowards(progress, returning ? 0 : 1, Time.unscaledDeltaTime / (returning ? 3.5f : 10f));
        if (returning && progress <= 0) { Finish(); return; }
        // First approach the opening from inside; then glide through its clear centre.
        var gateway = new Vector3(-40.2f, 3.7f, 7);
        var gatewayRotation = Quaternion.LookRotation(new Vector3(-1, .04f, .10f));
        Vector3 position; Quaternion rotation; float fov;
        if (progress < .3f)
        {
            float t = Mathf.SmoothStep(0, 1, progress / .3f);
            position = Vector3.Lerp(startPosition, gateway, t);
            rotation = Quaternion.Slerp(startRotation, gatewayRotation, t);
            fov = Mathf.Lerp(startFov, 53, t);
        }
        else
        {
            float t = Mathf.SmoothStep(0, 1, (progress - .3f) / .7f);
            position = Vector3.Lerp(gateway, new Vector3(-61, 11, 7), t);
            rotation = Quaternion.Slerp(gatewayRotation, Quaternion.LookRotation(new Vector3(-69, 0, 20)), t);
            fov = Mathf.Lerp(53, 58, t);
        }
        follow.SetScenicView(position, rotation, fov);
        if (!returning && progress >= .68f) scenicMusic?.RevealCaption();
        caption.alpha = returning ? 0 : scenicMusic != null && scenicMusic.IsAvailable
            ? scenicMusic.CaptionAlpha : Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.68f, .9f, progress));
    }

    private void BuildOverlay()
    {
        var root = new GameObject("Field lookout overlay", typeof(Canvas)); root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 25;
        PaperUIFactory.Scale(canvas); root.AddComponent<GraphicRaycaster>();
        var style = PaperUIStyle.Load();
        var panel = PaperUIFactory.Image("Landscape caption", root.transform, style.panel,
            new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-350, 35), new Vector2(350, 132));
        caption = panel.gameObject.AddComponent<CanvasGroup>(); caption.alpha = 0;
        PaperUIFactory.Text("Region", panel.transform, "Aurenfeld", 28, Vector2.zero, Vector2.one,
            new Vector2(20, 42), new Vector2(-20, -10), TextAlignmentOptions.Center);
        PaperUIFactory.Text("Fields", panel.transform, "Goldene Felder im warmen Morgenlicht.", 22, Vector2.zero, Vector2.one,
            new Vector2(20, 9), new Vector2(-20, -45), TextAlignmentOptions.Center);
        var button = PaperUIFactory.Button("Return to courtyard", root.transform, style.panel,
            Vector2.one, Vector2.one, new Vector2(-275, -78), new Vector2(-24, -24));
        PaperUIFactory.Text("Return label", button.transform, "Zurück · E / Esc", 22, Vector2.zero, Vector2.one,
            new Vector2(12, 6), new Vector2(-12, -6), TextAlignmentOptions.Center);
        button.onClick.AddListener(ReturnToCourtyard);
    }

    private void Finish()
    {
        if (active != this) return;
        scenicMusic?.StopImmediately();
        follow?.ClearScenicView();
        if (objective != null) objective.enabled = objectiveWasEnabled;
        if (canvas != null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); }
        canvas = null; caption = null; progress = 0; returning = false; active = null;
    }
    private void OnDisable() => Finish();
}
