using UnityEngine;
using TMPro;

public class PaperDamageNumber : MonoBehaviour
{
    private TextMeshProUGUI label;
    private Vector3 origin;
    private float elapsed;
    private Canvas canvas;

    public static void Show(Vector3 position, int damage)
    {
        var root = new GameObject("Paper damage number", typeof(RectTransform), typeof(Canvas), typeof(PaperDamageNumber));
        Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
        PaperUIFactory.Scale(canvas);
        PaperDamageNumber popup = root.GetComponent<PaperDamageNumber>(); popup.origin = position;
        popup.canvas = canvas;
        popup.label = PaperUIFactory.Text("Damage value", root.transform, "−" + damage, 34,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-70, -35), new Vector2(70, 35), TextAlignmentOptions.Center);
        popup.label.fontStyle = FontStyles.Bold;
        popup.label.color = PaperUIStyle.Load() != null ? PaperUIStyle.Load().cream : Color.white;
        popup.label.outlineColor = new Color(.2f, .1f, .06f); popup.label.outlineWidth = .16f;
    }
    private void Update()
    {
        if (label == null) return;
        elapsed += Time.deltaTime;
        Vector3 viewport = Camera.main != null ? Camera.main.WorldToViewportPoint(origin + Vector3.up * (.55f * elapsed / .8f)) : Vector3.zero;
        Rect bounds = ((RectTransform)canvas.transform).rect;
        label.rectTransform.anchoredPosition = new Vector2((viewport.x - .5f) * bounds.width, (viewport.y - .5f) * bounds.height);
        bool visible = viewport.z > 0 && viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
        label.alpha = visible ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, .8f, elapsed)) : 0;
        if (elapsed >= .8f) Destroy(gameObject);
    }
}
