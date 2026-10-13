using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Selection and the visible prompt use exactly the same collider/range rules.
[DisallowMultipleComponent]
public class PlayerInteractionFocus : MonoBehaviour
{
    public IInteractableAsPlayer Current { get; private set; }
    private PlayerController controller;
    private readonly Collider[] nearby = new Collider[64];
    private Canvas canvas;
    private RectTransform panel;
    private TextMeshProUGUI label;
    private LineRenderer ring;
    private Material ringMaterial;
    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        var hud = new GameObject("Nearby interaction", typeof(Canvas)); hud.transform.SetParent(transform, false);
        canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 19; PaperUIFactory.Scale(canvas);
        panel = PaperUIFactory.Image("Interaction paper", hud.transform, PaperUIStyle.Load().panel,
            Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(290, 48)).rectTransform;
        panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
        label = PaperUIFactory.Text("Interact", panel, "", 22, Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -6));
        label.alignment = TextAlignmentOptions.Center;
        var marker = new GameObject("Interaction ground halo"); marker.transform.SetParent(transform, false);
        ring = marker.AddComponent<LineRenderer>(); ring.useWorldSpace = true; ring.loop = true; ring.positionCount = 48;
        ringMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); ringMaterial.SetColor("_BaseColor", new Color(.95f,.76f,.36f));
        ring.sharedMaterial = ringMaterial; ring.widthMultiplier = .028f; ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Hide();
    }
    public IInteractableAsPlayer Refresh()
    {
        Current = null;
        if (controller == null || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning || DialogueBoxController.instance != null && DialogueBoxController.instance.IsOpen) return null;
        int count = Physics.OverlapSphereNonAlloc(transform.position, controller.interacRange, nearby);
        float best = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var candidate = nearby[i].GetComponentInParent<IInteractableAsPlayer>();
            if (candidate == null) continue;
            float squared = (candidate.GetTransform() - transform.position).sqrMagnitude;
            // A large collider alone must not make an interaction reachable from far away.
            if (squared > controller.interacRange * controller.interacRange || squared >= best) continue;
            Current = candidate; best = squared;
        }
        return Current;
    }
    public static string LabelFor(IInteractableAsPlayer item)
    {
        if (item is NPC npc) return "Mit " + npc.npcName + " sprechen";
        if (item is DemoInteractable target)
            switch (target.kind)
            {
                case DemoInteractable.Kind.Wardrobe: return DemoOpening.HasMantle ? "Schrank ansehen" : "Mantel holen";
                case DemoInteractable.Kind.Lamp: return "Lampe mitnehmen";
                case DemoInteractable.Kind.RoomDoor: return DemoOpening.HasMantle ? "In den Hof" : "Tür ansehen";
                case DemoInteractable.Kind.Window: return "Aussicht ansehen";
                case DemoInteractable.Kind.CourtyardGate: return "Tor ansehen";
            }
        return "Ansehen";
    }
    private void LateUpdate()
    {
        Refresh();
        if (Current == null || Camera.main == null) { Hide(); return; }
        Vector3 point = Current.GetTransform();
        Vector3 screen = Camera.main.WorldToScreenPoint(point + Vector3.up * 1.65f);
        if (screen.z <= 0) { Hide(); return; }
        canvas.enabled = true; ring.enabled = true; label.text = "[E]  " + LabelFor(Current);
        var canvasRect = (RectTransform)canvas.transform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out Vector2 local);
        panel.anchoredPosition = local;
        float pulse = 1 + .035f * Mathf.Sin(Time.unscaledTime * 3);
        for (int i = 0; i < ring.positionCount; i++)
        { float angle = i * Mathf.PI * 2 / ring.positionCount; ring.SetPosition(i, new Vector3(point.x + Mathf.Cos(angle)*.55f*pulse, .035f, point.z + Mathf.Sin(angle)*.55f*pulse)); }
    }
    private void Hide() { if (canvas != null) canvas.enabled = false; if (ring != null) ring.enabled = false; }
    private void OnDisable() { Current = null; Hide(); }
    private void OnDestroy() { if (ringMaterial != null) Destroy(ringMaterial); }
}
