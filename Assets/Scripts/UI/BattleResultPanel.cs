using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleResultPanel : MonoBehaviour
{
    public static BattleResultPanel Instance { get; private set; }
    public bool IsOpen { get; private set; }
    public UnitManager.BattleState Result { get; private set; }
    private bool continued;
    private CameraController controls;
    private bool previousCameraLock;
    public void Continue() { if (IsOpen) continued = true; }

    public static IEnumerator Show(UnitManager.BattleState result)
    {
        var root = new GameObject("Battle result", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(BattleResultPanel));
        BattleResultPanel panel = root.GetComponent<BattleResultPanel>();
        panel.Build(result);
        try { while (!panel.continued) yield return null; }
        finally { if (root != null) Destroy(root); }
    }

    private void Build(UnitManager.BattleState result)
    {
        Instance = this; IsOpen = true; Result = result;
        controls = FindFirstObjectByType<CameraController>();
        if (controls != null) { previousCameraLock = controls.IsPresentationLocked; controls.IsPresentationLocked = true; }
        PaperUIStyle style = PaperUIStyle.Load();
        Canvas canvas = GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        PaperUIFactory.Scale(canvas);
        Image shade = PaperUIFactory.Image("Result backdrop", transform, null,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        shade.color = new Color(.12f, .09f, .07f, .32f); shade.raycastTarget = true;
        Image sheet = PaperUIFactory.Image("Result paper", transform, style != null ? style.panel : null,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-330, -190), new Vector2(330, 190));
        string heading = result == UnitManager.BattleState.Won ? "Prüfung bestanden"
            : result == UnitManager.BattleState.Lost ? "Heute war der Gegner stärker" : "Kampf verlassen";
        string message = result == UnitManager.BattleState.Won ? "Kafla und du haben die Prüfung gemeistert."
            : result == UnitManager.BattleState.Lost ? "Dein Team braucht eine Pause. Im Hof könnt ihr euch neu sammeln."
            : "Ihr zieht euch zurück. Im Hof könnt ihr jederzeit weiterüben.";
        PaperUIFactory.Text("Result heading", sheet.transform, heading, 34,
            new Vector2(.08f, .72f), new Vector2(.92f, .9f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        Image portrait = PaperUIFactory.Image("Result portrait", sheet.transform, style != null ? style.flacaPortrait : null,
            new Vector2(.08f, .31f), new Vector2(.31f, .7f), Vector2.zero, Vector2.zero, false);
        portrait.preserveAspect = true;
        PaperUIFactory.Text("Result explanation", sheet.transform, message, 24,
            new Vector2(.34f, .32f), new Vector2(.89f, .67f), Vector2.zero, Vector2.zero);
        Button button = PaperUIFactory.Button("Return to courtyard", sheet.transform, style != null ? style.sageButton : null,
            new Vector2(.3f, .1f), new Vector2(.7f, .27f), Vector2.zero, Vector2.zero);
        button.onClick.AddListener(Continue);
        var label = PaperUIFactory.Text("Return label", button.transform, "Zurück zum Hof", 23,
            Vector2.zero, Vector2.one, new Vector2(14, 5), new Vector2(-14, -5), TextAlignmentOptions.Center);
        label.color = style != null ? style.cream : Color.white;
        EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }
    private void Update()
    {
        if (IsOpen && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))) Continue();
    }
    private void OnDisable()
    {
        continued = true; IsOpen = false;
        if (controls != null) controls.IsPresentationLocked = previousCameraLock;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
