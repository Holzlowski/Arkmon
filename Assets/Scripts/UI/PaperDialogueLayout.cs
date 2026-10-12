using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PaperDialogueLayout
{
    public static bool Build(DialogueBoxController controller, out GameObject box, out GameObject answers,
        out TextMeshProUGUI dialogue, out TextMeshProUGUI speaker, out Button[] choices, out Image portrait)
    {
        box = answers = null; dialogue = speaker = null; choices = null; portrait = null;
        PaperUIStyle style = PaperUIStyle.Load();
        if (style == null) return false;
        PaperUIFactory.Scale(controller.GetComponent<Canvas>());
        Image panel = PaperUIFactory.Image("Paper dialogue panel", controller.transform, style.panel,
            Vector2.zero, new Vector2(1, 0), new Vector2(18, 18), new Vector2(-18, 334));
        panel.raycastTarget = true;
        box = panel.gameObject;
        Image frame = PaperUIFactory.Image("Speaker portrait frame", panel.transform, style.card,
            new Vector2(0.015f, 0.08f), new Vector2(0.235f, 0.98f), Vector2.zero, Vector2.zero);
        portrait = PaperUIFactory.Image("Speaker portrait", frame.transform, style.hunterPortrait,
            new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero, false);
        portrait.preserveAspect = true;
        Image name = PaperUIFactory.Image("Speaker name tab", panel.transform, style.card,
            new Vector2(0.25f, 0.76f), new Vector2(0.47f, 1.03f), Vector2.zero, Vector2.zero);
        speaker = PaperUIFactory.Text("Speaker name", name.transform, "", 25, Vector2.zero, Vector2.one,
            new Vector2(22, 12), new Vector2(-22, -12));
        dialogue = PaperUIFactory.Text("Dialogue text", panel.transform, "", 25,
            new Vector2(0.26f, 0.2f), new Vector2(0.65f, 0.73f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.TopLeft);
        dialogue.overflowMode = TextOverflowModes.Overflow;
        answers = PaperUIFactory.Rect("Dialogue answers", panel.transform,
            new Vector2(0.68f, 0.19f), new Vector2(0.98f, 0.91f), Vector2.zero, Vector2.zero).gameObject;
        var layout = answers.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10; layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandHeight = layout.childForceExpandWidth = true;
        choices = CreateChoices(controller, answers.transform, 3);
        PaperUIFactory.Text("Continue hint", panel.transform, "E · Weiter", 16,
            new Vector2(0.26f, 0.055f), new Vector2(0.5f, 0.17f), Vector2.zero, Vector2.zero);
        Button close = PaperUIFactory.Button("Close dialogue", panel.transform, style.card,
            new Vector2(0.8f, 0.025f), new Vector2(0.975f, 0.16f), Vector2.zero, Vector2.zero);
        PaperUIFactory.Text("Close label", close.transform, "Esc · Schließen", 16,
            Vector2.zero, Vector2.one, new Vector2(12, 3), new Vector2(-12, -3), TextAlignmentOptions.Center);
        close.onClick.AddListener(controller.CloseDialogue);
        answers.SetActive(false); box.SetActive(false);
        return true;
    }

    public static Button[] CreateChoices(DialogueBoxController controller, Transform parent, int count)
    {
        var choices = new Button[count];
        PaperUIStyle style = PaperUIStyle.Load();
        for (int i = 0; i < count; i++)
        {
            Button button = PaperUIFactory.Button("Answer " + (i + 1), parent, style.card,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 38; layout.preferredHeight = 66; layout.flexibleHeight = 1;
            PaperUIFactory.Text("Answer text", button.transform, "", 21, Vector2.zero, Vector2.one,
                new Vector2(24, 12), new Vector2(-24, -12));
            int index = i;
            button.onClick.AddListener(() => controller.AnswerQuestion(index));
            button.gameObject.AddComponent<PaperChoiceVisual>();
            choices[i] = button;
        }
        return choices;
    }
}
