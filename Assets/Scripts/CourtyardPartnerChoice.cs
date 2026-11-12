using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

internal sealed class CourtyardPartnerChoice : MonoBehaviour
{
    private static readonly CourtyardStarterMeetings.Starter[] Starters = {
        CourtyardStarterMeetings.Starter.Kafla, CourtyardStarterMeetings.Starter.Panfrie,
        CourtyardStarterMeetings.Starter.Liora, CourtyardStarterMeetings.Starter.Rauk
    };
    private static readonly string[] Details = {
        "Mutig und höflich. Kafla kämpft am liebsten aus der Nähe und setzt mit seinen Flammen mehrere Felder unter Druck.",
        "Zurückhaltend und beschützend. Panfrie fängt mit seinem Schild Treffer ab und bremst Gegner mit Eis.",
        "Neugierig und geheimnisvoll. Liora hält Gegner mit Dornen fest und heilt ihre Verbündeten.",
        "Schnell und selbstsicher. Rauk gleitet über Hindernisse und sucht den richtigen Moment für einen Angriff von hinten."
    };

    private GameObject stage, canvas, inspectPanel, confirmPanel;
    private Material paper;
    private Sprite liora, rauk;
    private PaperCharacterVisual elian;
    private readonly Transform[] figures = new Transform[4];
    private readonly Button[] choices = new Button[4];
    private Button choose, accept, back;
    private TMP_Text heading, details, question;
    private int selected = -1;

    internal IEnumerator Play(Transform player, CourtyardCamera follow, DialogueBoxController dialogue, bool congratulate, SpriteRenderer mentor, Sprite portrait)
    {
        try
        {
            BuildStage(player, follow);
            if (congratulate)
            {
                ArkanmeisterinConversation.SpeakAsMentor(dialogue, portrait, "Gut gemacht, Elian. Ihr habt das Siegel gemeinsam beschützt. Schaut Euch die vier noch einmal an. Mit wem möchtet Ihr von nun an Seite an Seite gehen?");
                yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            }

            if (!CourtyardStarterMeetings.ChosenPartner.HasValue) BuildPanel();
            while (!CourtyardStarterMeetings.ChosenPartner.HasValue)
            {
                if (confirmPanel.activeSelf && ExplorationInput.Pressed(Key.Escape)) ReturnToInspection();
                yield return null;
            }
            if (canvas != null) canvas.SetActive(false);
            int partnerIndex = System.Array.IndexOf(Starters, CourtyardStarterMeetings.ChosenPartner.Value);

            yield return gameObject.AddComponent<CourtyardPartnerRitual>()
                .Play(player, follow, dialogue, mentor, portrait, figures, partnerIndex, Starters[partnerIndex]);
        }
        finally { Clear(); }
    }

    private void BuildStage(Transform player, CourtyardCamera follow)
    {
        stage = new GameObject("Arkmon am Prüfungsfeld");
        var view = Quaternion.Euler(14, -8, 0);
        var midpoint = (transform.position + player.position) * .5f;
        var lineup = midpoint + new Vector3(0, 0, 2.35f);
        var focus = midpoint + new Vector3(0, .65f, 1.45f);
        float distance = 10.8f * Mathf.Max(1, 1.6f / Camera.main.aspect);
        follow.SetScenicView(focus - view * Vector3.forward * distance, view, 43);
        elian = player.GetComponent<PaperCharacterVisual>();
        paper = new Material(Resources.Load<Shader>("AurenfeldScenicPaper"));
        liora = CourtyardStarterSummoning.LoadArtwork("StarterSummoning/Liora", 1.35f);
        rauk = CourtyardStarterSummoning.LoadArtwork("StarterSummoning/Rauk", 1.38f);
        var sprites = new[] { null, Resources.Load<Sprite>("Panfrie/Panfrie Hervorlugen"), liora, rauk };
        var kafla = Resources.Load<GameObject>("Kafla courtyard figure");
        float[] positions = { -4.1f, -2.35f, 2.35f, 4.1f };
        for (int i = 0; i < Starters.Length; i++)
        {
            var actor = i == 0 && kafla != null ? Instantiate(kafla, stage.transform)
                : new GameObject(Starters[i].ToString(), typeof(SpriteRenderer));
            actor.name = Starters[i].ToString();
            actor.transform.SetParent(stage.transform, false);
            var mark = lineup + view * Vector3.right * positions[i];
            actor.transform.SetPositionAndRotation(CourtyardGuard.Grounded(mark), view);
            actor.transform.localScale = Vector3.one * (i == 0 ? .78f : i == 1 ? .66f : 1f);
            if (i > 0) actor.GetComponent<SpriteRenderer>().sprite = sprites[i];
            foreach (var part in actor.GetComponentsInChildren<SpriteRenderer>())
            {
                part.sharedMaterial = paper;
                if (i >= 2) part.flipX = true;
            }
            PaperGroundShadow.Attach(actor, actor.GetComponentInChildren<SpriteRenderer>());
            figures[i] = actor.transform;
        }
    }

    private void BuildPanel()
    {
        var root = new GameObject("Partnerwahl", typeof(Canvas), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        root.renderMode = RenderMode.ScreenSpaceOverlay;
        root.sortingOrder = 45;
        PaperUIFactory.Scale(root);
        canvas = root.gameObject;
        var style = PaperUIStyle.Load();
        inspectPanel = MakePanel("Arkmon ansehen", root.transform, style.panel);
        heading = Label("Auswahl", inspectPanel.transform, "Wen möchtet Ihr noch einmal ansehen?", 25, 20, 147, 720, 183);
        details = Label("Beschreibung", inspectPanel.transform, "Wählt einen Namen, um das Arkmon noch einmal anzusehen.", 22, 20, 75, 735, 138);
        for (int i = 0; i < Starters.Length; i++)
        {
            int index = i;
            int left = 20 + i * 188;
            choices[i] = AddButton(inspectPanel.transform, style.card, Starters[i].ToString(), left, 19, left + 170, 65);
            choices[i].onClick.AddListener(() => Inspect(index));
        }

        choose = AddButton(inspectPanel.transform, style.card, "Weiter", 777, 19, 990, 65);
        choose.onClick.AddListener(AskConfirmation);
        choose.interactable = false;
        confirmPanel = MakePanel("Partner bestätigen", root.transform, style.panel);
        question = Label("Frage", confirmPanel.transform, "", 27, 23, 94, 982, 173);
        back = AddButton(confirmPanel.transform, style.card, "Zurück", 521, 19, 710, 66);
        back.onClick.AddListener(ReturnToInspection);
        accept = AddButton(confirmPanel.transform, style.card, "Wahl bestätigen", 730, 19, 987, 66);
        accept.onClick.AddListener(Confirm);
        confirmPanel.SetActive(false);
        Focus(choices[0]);
    }

    private void Inspect(int index)
    {
        if (!inspectPanel.activeSelf || index < 0 || index >= Starters.Length) return;
        selected = index;
        heading.text = Starters[index].ToString();
        details.text = Details[index];
        elian?.SetTrialLookTarget(figures[index]);
        choose.interactable = true;
    }

    private void AskConfirmation()
    {
        if (selected < 0 || !inspectPanel.activeSelf) return;
        question.text = $"Möchtet Ihr {Starters[selected]} als Partner wählen?";
        inspectPanel.SetActive(false);
        confirmPanel.SetActive(true);
        Focus(back);
    }

    private void ReturnToInspection()
    {
        confirmPanel.SetActive(false);
        inspectPanel.SetActive(true);
        Focus(choices[selected]);
    }

    private void Confirm()
    {
        if (selected >= 0 && confirmPanel.activeSelf)
            CourtyardStarterMeetings.ConfirmPartner(Starters[selected]);
    }

    private static GameObject MakePanel(string name, Transform parent, Sprite sprite)
        => PaperUIFactory.Image(name, parent, sprite, new Vector2(.5f, 0), new Vector2(.5f, 0),
            new Vector2(-505, 18), new Vector2(505, 213)).gameObject;

    private static TMP_Text Label(string name, Transform parent, string value, float size, int x, int y, int right, int top)
        => PaperUIFactory.Text(name, parent, value, size, Vector2.zero, Vector2.zero,
            new Vector2(x, y), new Vector2(right, top));

    private static Button AddButton(Transform parent, Sprite sprite, string caption, int x, int y, int right, int top)
    {
        var button = PaperUIFactory.Button(caption, parent, sprite, Vector2.zero, Vector2.zero,
            new Vector2(x, y), new Vector2(right, top));
        PaperUIFactory.Text("Beschriftung", button.transform, caption, 22, Vector2.zero, Vector2.one,
            new Vector2(5, 0), new Vector2(-5, 0), TextAlignmentOptions.Center);
        return button;
    }

    private static void Focus(Button button) => EventSystem.current?.SetSelectedGameObject(button.gameObject);

    private void OnDestroy() => Clear();

    private void Clear()
    {
        elian?.SetTrialLookTarget(null);
        if (stage != null) Destroy(stage);
        if (canvas != null) Destroy(canvas);
        if (paper != null) Destroy(paper);
        if (liora != null) Destroy(liora);
        if (rauk != null) Destroy(rauk);
        stage = canvas = null;
        paper = null;
        liora = rauk = null;
    }
}