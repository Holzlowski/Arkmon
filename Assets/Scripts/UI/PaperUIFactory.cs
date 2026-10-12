using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared construction helpers; the same style asset supplies battle and dialogue.
public static class PaperUIFactory
{
    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        return rect;
    }

    public static Image Image(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax, bool sliced = true)
    {
        RectTransform rect = Rect(name, parent, min, max, offsetMin, offsetMax);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite; image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 2.5f;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, string value, float size,
        Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var label = Rect(name, parent, min, max, offsetMin, offsetMax).gameObject.AddComponent<TextMeshProUGUI>();
        PaperUIStyle style = PaperUIStyle.Load();
        label.font = style != null ? style.font : TMP_Settings.defaultFontAsset;
        label.color = style != null ? style.ink : Color.black;
        label.text = value; label.fontSize = size; label.alignment = alignment;
        label.raycastTarget = false; label.enableAutoSizing = true;
        label.fontSizeMin = size * 0.75f; label.fontSizeMax = size;
        label.overflowMode = TextOverflowModes.Truncate;
        // TMP initialization can resize a newly added component; enforce the requested box last.
        label.rectTransform.anchorMin = min; label.rectTransform.anchorMax = max;
        label.rectTransform.offsetMin = offsetMin; label.rectTransform.offsetMax = offsetMax;
        return label;
    }

    public static Button Button(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        Image image = Image(name, parent, sprite, min, max, offsetMin, offsetMax);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.96f, 0.87f);
        colors.pressedColor = new Color(0.83f, 0.78f, 0.65f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.55f);
        button.colors = colors;
        return button;
    }

    public static void Scale(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
    }

    public static void HideChildren(Transform parent)
    { foreach (Transform child in parent) child.gameObject.SetActive(false); }

    public static void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        { child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
    }

    public static void Fill(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
}
