using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedGameObject;
    private BaseAction baseAction;
    private Image background;
    private CanvasGroup group;
    private bool hovered;
    private TextMeshProUGUI costLabel;

    public void SetBaseAction(BaseAction action)
    {
        baseAction = action;
        PaperUIStyle style = PaperUIStyle.Load();
        if (style != null)
        {
            PaperUIFactory.ClearChildren(transform);
            RectTransform rect = (RectTransform)transform;
            rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
            var layout = GetComponent<LayoutElement>();
            if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 95; layout.preferredWidth = 166; layout.flexibleWidth = 1;
            background = GetComponent<Image>();
            if (background == null) background = gameObject.AddComponent<Image>();
            background.sprite = style.card; background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 2.5f;
            background.color = Color.white; background.raycastTarget = true;
            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
            button.targetGraphic = background; button.transition = Selectable.Transition.None;
            // The prefab may already be fading to its disabled color during deployment.
            // Switching to Transition.None does not clear that renderer tint or tween.
            background.CrossFadeColor(Color.white, 0f, true, true);
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            Image icon = PaperUIFactory.Image("Action icon", transform, style.Icon(action),
                new Vector2(0.22f, 0.43f), new Vector2(0.78f, 0.83f), Vector2.zero, Vector2.zero, false);
            icon.preserveAspect = true;
            if (action is GuardAction)
            {
                icon.enabled = false;
                var shield = PaperUIFactory.Rect("Guard shield", icon.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                shield.gameObject.AddComponent<PaperGuardIcon>().raycastTarget = false;
            }
            textMeshProUGUI = PaperUIFactory.Text("Action name", transform, PaperUIStyle.ActionName(action), 20,
                new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.44f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            textMeshProUGUI.fontStyle = FontStyles.Bold;
            costLabel = PaperUIFactory.Text("Action cost", transform, action is EscapeAction ? "Rückzug" : $"{action.GetActionPointCost()} AP", 17,
                new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.27f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            costLabel.enableAutoSizing=true;costLabel.fontSizeMin=12;costLabel.fontSizeMax=17;
        }
        else textMeshProUGUI.text = PaperUIStyle.ActionName(action);
        button.onClick.RemoveAllListeners(); button.onClick.AddListener(ChooseAction);
        UpdateSelectedVisual();
    }

    private bool CanChoose()
    {
        return UnavailableReason(baseAction).Length == 0;
    }
    public static string UnavailableReason(BaseAction action)
    {
        if(action==null || action.GetUnit()==null)return "Kein Monster gewählt";
        if(SceneLoadManager.IsTransitioning)return "Szenenwechsel läuft";
        if(UnitManager.Instance==null || !UnitManager.Instance.IsBattleRunning)return "Kampf noch nicht bereit";
        if(TurnSystem.Instance==null || !TurnSystem.Instance.IsPlayerTurn())return "Gegner am Zug";
        if(BaseAction.IsAnyActionActive || UnitActionSystem.Instance==null || UnitActionSystem.Instance.IsBusy)return "Aktion läuft";
        if(BattleDeployment.Instance!=null && BattleDeployment.Instance.IsRecalling)return "Rückruf läuft";
        Unit unit=action.GetUnit();
        if(unit.IsDead)return "Monster besiegt";
        if(unit.IsKnockedDown && !(action is StandUpAction))return "Zuerst aufstehen";
        if(action is GuardAction && unit.IsGuarding)return "Abwehr schon aktiv";
        if(!unit.CanPerformAction(action))return "Aktion nicht verfügbar";
        if(unit.GetActionPoints()<action.GetActionPointCost())return "Zu wenig AP";
        return "";
    }

    private void ChooseAction()
    {
        if (!CanChoose()) return;
        if (baseAction is EscapeAction) UnitManager.Instance.TryEscape();
        else UnitActionSystem.Instance.SetSelectedAction(baseAction);
    }

    private void Update()
    {
        if (baseAction == null) return;
        button.interactable = CanChoose();
        if (group != null) group.alpha = button.interactable ? 1f : 0.48f;
        if(costLabel != null)costLabel.text=button.interactable
            ? baseAction is EscapeAction ? "Rückzug" : $"{baseAction.GetActionPointCost()} AP" : UnavailableReason(baseAction);
        if (hovered) ShowExplanation();
    }

    public void UpdateSelectedVisual()
    {
        bool selected = UnitActionSystem.Instance != null && UnitActionSystem.Instance.GetSelectedAction() == baseAction;
        if (background != null)
        {
            PaperUIStyle style = PaperUIStyle.Load();
            background.sprite = selected ? style.selectedCard : style.card;
            background.color = hovered && button.interactable ? new Color(1f, 0.97f, 0.88f) : Color.white;
        }
        else if (selectedGameObject != null) selectedGameObject.SetActive(selected);
    }

    private void ShowExplanation()
    {
        UnitActionSystemUI ui = GetComponentInParent<UnitActionSystemUI>();
        if (ui == null) return;
        string message = baseAction is FlameSwordAction ? "Feuerschaden. Kann Monster und Pflanzen entzünden."
            : baseAction is ClawAction ? "Ein schneller Krallenhieb ohne Elementarschaden."
            : baseAction is EmberAction ? "Funken treffen 3 Nachbarfelder um Kafla (2 AP, 18 Feuerschaden). Diagonal läuft die Fläche um die Ecke. Auch Verbündete werden getroffen; Holz und Pflanzen entzünden sich."
            : baseAction is GuardAction ? "Wähle dein eigenes Feld: 50 % weniger direkter Schaden bis zum nächsten eigenen Zug. Weitere Aktionen beenden Abwehr."
            : baseAction is StandUpAction ? "Richte dein Monster auf: Wähle sein eigenes Feld."
            : baseAction is MoveAction ? "Wähle ein erreichbares Feld, um dein Monster zu bewegen."
            : baseAction is EscapeAction ? "Rufe dein Team zurück und verlasse den Kampf."
            : "Wähle ein markiertes Zielfeld.";
        string reason=UnavailableReason(baseAction);
        if(reason.Length>0)message=reason+". "+message;
        ui.ShowTooltip(message);
    }
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; UpdateSelectedVisual(); ShowExplanation(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; UpdateSelectedVisual(); GetComponentInParent<UnitActionSystemUI>()?.HideTooltip(); }
    private void OnDisable() { GetComponentInParent<UnitActionSystemUI>()?.HideTooltip(); }
}
