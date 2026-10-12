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
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            Image icon = PaperUIFactory.Image("Action icon", transform, style.Icon(action),
                new Vector2(0.22f, 0.43f), new Vector2(0.78f, 0.83f), Vector2.zero, Vector2.zero, false);
            icon.preserveAspect = true;
            textMeshProUGUI = PaperUIFactory.Text("Action name", transform, PaperUIStyle.ActionName(action), 20,
                new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.44f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            textMeshProUGUI.fontStyle = FontStyles.Bold;
            PaperUIFactory.Text("Action cost", transform, action is EscapeAction ? "Rückzug" : $"{action.GetActionPointCost()} AP", 17,
                new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.27f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        }
        else textMeshProUGUI.text = PaperUIStyle.ActionName(action);
        button.onClick.RemoveAllListeners(); button.onClick.AddListener(ChooseAction);
        UpdateSelectedVisual();
    }

    private bool CanChoose()
    {
        if (baseAction == null || baseAction.GetUnit() == null) return false;
        return TurnSystem.Instance != null && TurnSystem.Instance.IsPlayerTurn()
            && UnitManager.Instance != null && UnitManager.Instance.IsBattleRunning
            && !BaseAction.IsAnyActionActive
            && UnitActionSystem.Instance != null && !UnitActionSystem.Instance.IsBusy
            && (BattleDeployment.Instance == null || !BattleDeployment.Instance.IsRecalling)
            && baseAction.GetUnit().CanSpendActionPointsToTakeAction(baseAction);
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
            : baseAction is StandUpAction ? "Richte dein Monster auf: Wähle sein eigenes Feld."
            : baseAction is MoveAction ? "Wähle ein erreichbares Feld, um dein Monster zu bewegen."
            : baseAction is EscapeAction ? "Rufe dein Team zurück und verlasse den Kampf."
            : "Wähle ein markiertes Zielfeld.";
        if (!baseAction.GetUnit().CanSpendActionPointsToTakeAction(baseAction)) message = "Nicht genügend Aktionspunkte oder Aktion im aktuellen Zustand gesperrt.";
        ui.ShowTooltip(message);
    }
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; UpdateSelectedVisual(); ShowExplanation(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; UpdateSelectedVisual(); GetComponentInParent<UnitActionSystemUI>()?.HideTooltip(); }
    private void OnDisable() { GetComponentInParent<UnitActionSystemUI>()?.HideTooltip(); }
}
