using TMPro;
using UnityEngine;

// Compact world health labels and cracks communicate that cover is consumable.
public class ObstacleConditionVisual : MonoBehaviour
{
    private BurningObstacle obstacle;
    private TextMeshProUGUI label;
    private Transform hud;
    private SpriteRenderer body;
    private Transform cracks;
    private Color original;
    private Material crackMaterial;
    private float visibleUntil;
    public bool IsHealthVisible => hud != null && hud.gameObject.activeSelf;
    public void RevealDamage() => visibleUntil = Time.time + 2.5f;
    private void Start()
    {
        obstacle = GetComponent<BurningObstacle>(); body = GetComponentInChildren<SpriteRenderer>();
        if (body != null) original = body.color;
        var canvas = new GameObject("Cover condition", typeof(Canvas)); canvas.transform.SetParent(transform, false);
        var screen = canvas.GetComponent<Canvas>(); screen.renderMode = RenderMode.WorldSpace;
        var rect = canvas.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(180, 42); rect.localScale = Vector3.one * .012f;
        hud = canvas.transform;
        label = PaperUIFactory.Text("Cover health", hud, "", 22, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        label.color = new Color(.22f, .14f, .09f); Refresh(); hud.gameObject.SetActive(false);
    }
    public void Refresh()
    {
        if (obstacle == null || label == null) return;
        label.text = $"{(obstacle.BlocksSight ? "Sichtblockade" : "Deckung")} · {obstacle.Health}/{obstacle.MaxHealth}";
        if (body == null) return;
        float condition = obstacle.Health / (float)obstacle.MaxHealth;
        body.color = Color.Lerp(new Color(original.r * .65f, original.g * .62f, original.b * .6f, original.a), original, condition);
        if (cracks != null) Destroy(cracks.gameObject);
        if (condition > .7f || body.sprite == null) return;
        cracks = new GameObject("Damage cracks").transform; cracks.SetParent(body.transform, false);
        if (crackMaterial == null) crackMaterial = new Material(Shader.Find("Sprites/Default"));
        Bounds bounds = body.sprite.bounds;
        for (int i = 0; i < (condition > .35f ? 1 : 3); i++)
        {
            var line = new GameObject("Crack", typeof(LineRenderer)).GetComponent<LineRenderer>(); line.transform.SetParent(cracks, false);
            line.useWorldSpace = false; line.sharedMaterial = crackMaterial; line.widthMultiplier = .018f;
            line.startColor = line.endColor = new Color(.2f, .16f, .13f, .9f);
            float x = bounds.center.x + (i - 1) * bounds.size.x * .19f;
            float y = bounds.center.y - bounds.size.y * .2f;
            line.positionCount = 4;
            line.SetPositions(new[] { new Vector3(x, y, -.03f), new Vector3(x + .08f, y + bounds.size.y * .12f, -.03f),
                new Vector3(x - .07f, y + bounds.size.y * .21f, -.03f), new Vector3(x + .02f, y + bounds.size.y * .33f, -.03f) });
        }
    }
    private void LateUpdate()
    {
        if (hud == null || Camera.main == null) return;
        bool hovered = false;
        if (UnityEngine.InputSystem.Mouse.current != null && InputManager.Instance != null
            && !(UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
        {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, LayerMask.GetMask("Obstacles")))
                hovered = hit.collider.GetComponentInParent<BurningObstacle>() == obstacle;
            BaseAction action = UnitActionSystem.Instance?.GetSelectedAction();
            if (action is EmberAction fan && !BaseAction.IsAnyActionActive)
            {
                GridPosition centre = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());
                hovered |= fan.IsValidActionGridPosition(centre) && fan.GetImpactGridPositionList(centre).Contains(obstacle.GetGridPosition());
            }
        }
        hud.gameObject.SetActive(!obstacle.IsDestroyed && (hovered || Time.time < visibleUntil));
        float top = body != null ? body.bounds.max.y : transform.position.y + 1;
        hud.position = new Vector3(transform.position.x, top + .28f, transform.position.z);
        hud.rotation = Camera.main.transform.rotation;
    }
    private void OnDestroy() { if (crackMaterial != null) Destroy(crackMaterial); }
}
