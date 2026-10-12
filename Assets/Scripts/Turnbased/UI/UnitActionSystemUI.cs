using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
public class UnitActionSystemUI : MonoBehaviour
{
    [SerializeField] private Transform actionButtonPrefab;
    [SerializeField] private Transform actionButtonContainerTransform;
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private Transform attacksUISwitchButtonPrefab;


    private List<ActionButtonUI> actionButtonUIList;
    private GameObject tooltip;
    private TextMeshProUGUI tooltipText;
    private const int CardsPerPage = 4;
    private int commandPage;
    private Button actionsTab, attacksTab, previousPage, nextPage;
    private TextMeshProUGUI pageText;

    public void ConfigureCategoryTabs(Button basic, Button attacks, Button previous, Button next, TextMeshProUGUI page)
    {
        actionsTab = basic; attacksTab = attacks; previousPage = previous; nextPage = next; pageText = page;
        basic.onClick.AddListener(() => ShowCommandCategory(false));
        attacks.onClick.AddListener(() => ShowCommandCategory(true));
        previous.onClick.AddListener(() => ChangePage(-1));
        next.onClick.AddListener(() => ChangePage(1));
    }

    public void ShowCommandCategory(bool attacks)
    {
        Unit selected = actionSystem.GetSelectedUnit();
        if (selected == null || selected.IsDead || selected.IsKnockedDown && attacks
            || BaseAction.IsAnyActionActive || actionSystem.IsBusy) return;
        if (selected.GetIsInAttackSelection() == attacks) return;
        commandPage = 0;
        selected.SwitchAttacksUI();
        BaseAction[] available = selected.GetBaseActionArray();
        actionSystem.SetSelectedAction(available.Length > 0 ? available[0] : null);
    }

    private void ChangePage(int direction)
    {
        if (BaseAction.IsAnyActionActive || actionSystem.IsBusy) return;
        commandPage += direction;
        CreateUnitActionButton();
        Unit selected = actionSystem.GetSelectedUnit();
        BaseAction[] commands = selected != null ? selected.GetBaseActionArray() : Array.Empty<BaseAction>();
        if (commands.Length > 0) actionSystem.SetSelectedAction(commands[commandPage * CardsPerPage]);
        UpdateSelectedVisual();
    }
    public void SetTooltip(GameObject root, TextMeshProUGUI label) { tooltip = root; tooltipText = label; }
    public void ShowTooltip(string value) { if (tooltip == null) return; tooltipText.text = value; tooltip.SetActive(true); }
    public void HideTooltip() { if (tooltip != null) tooltip.SetActive(false); }

    private void Awake()
    {
        actionButtonUIList = new List<ActionButtonUI>();
    }

    private void Start()
    {
        if (PaperBattleLayout.Actions(this, out Transform cards, out TextMeshProUGUI points))
        { actionButtonContainerTransform = cards; actionPointsText = points; }
        actionSystem = UnitActionSystem.Instance;
        turnSystem = TurnSystem.Instance;
        actionSystem.OnSelectedUnitChange += UnitActionSystem_OnSelectedUnitChanged;
        actionSystem.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;
        actionSystem.OnActionStarted += UnitActionSystem_OnActionStarted;
        turnSystem.OnTurnChanged += TurnSystem_OnTurnChanged;
        Unit.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged;
        Unit.OnAttackUISwitch += Unit_OnAttackUISwitch;
        Unit.OnAnyKnockdownChanged += Unit_OnKnockdownChanged;

        UpdateActionPoints();
        CreateUnitActionButton();
        UpdateSelectedVisual();
    }

    private void CreateUnitActionButton()
    {
        foreach (Transform buttonTransform in actionButtonContainerTransform)
        {
            buttonTransform.gameObject.SetActive(false);
            Destroy(buttonTransform.gameObject);
        }

        actionButtonUIList.Clear();

        Unit selectedUnit = actionSystem.GetSelectedUnit();

        if (selectedUnit == null) return;

        BaseAction[] commands = selectedUnit.GetBaseActionArray();
        int pages = Mathf.Max(1, Mathf.CeilToInt(commands.Length / (float)CardsPerPage));
        commandPage = Mathf.Clamp(commandPage, 0, pages - 1);
        RefreshTabs(selectedUnit, pages);
        HideTooltip();
        for (int i = commandPage * CardsPerPage; i < Mathf.Min(commands.Length, (commandPage + 1) * CardsPerPage); i++)
        {
            BaseAction baseAction = commands[i];
            if (baseAction == null) continue;
            Transform actionButtonTransform = Instantiate(actionButtonPrefab, actionButtonContainerTransform);
            ActionButtonUI actionButtonUI = actionButtonTransform.GetComponent<ActionButtonUI>();
            actionButtonUI.SetBaseAction(baseAction);
            actionButtonTransform.GetComponent<LayoutElement>().flexibleWidth = 0;

            actionButtonUIList.Add(actionButtonUI);
        }
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        commandPage = 0;
        CreateUnitActionButton();
        Unit selected = actionSystem.GetSelectedUnit();
        BaseAction[] visible = selected != null ? selected.GetBaseActionArray() : Array.Empty<BaseAction>();
        if (visible != null && visible.Length > 0) actionSystem.SetSelectedAction(visible[0]);
        UpdateSelectedVisual();
        UpdateActionPoints();
    }

    private void UnitActionSystem_OnActionStarted(object sender, EventArgs e)
    {
        UpdateActionPoints();
    }

    private void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateSelectedVisual();
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        UpdateActionPoints();
    }

    private void Unit_OnAnyActionPointsChanged(object sender, EventArgs e)
    {
        UpdateActionPoints();
    }

    private void Unit_OnAttackUISwitch(object sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, actionSystem.GetSelectedUnit())) return;
        commandPage = 0;
        CreateUnitActionButton();
        UpdateSelectedVisual();
    }

    private void Unit_OnKnockdownChanged(object sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, actionSystem.GetSelectedUnit())) return;
        commandPage = 0;
        CreateUnitActionButton();
        UpdateSelectedVisual();
    }

    private void UpdateSelectedVisual()
    {
        foreach (ActionButtonUI actionButtonUI in actionButtonUIList)
        {
            actionButtonUI.UpdateSelectedVisual();
        }
    }

    private void RefreshTabs(Unit unit, int pages)
    {
        if (actionsTab == null) return;
        bool attacks = unit.GetIsInAttackSelection() && !unit.IsKnockedDown;
        PaperUIStyle style = PaperUIStyle.Load();
        actionsTab.GetComponent<Image>().sprite = attacks ? style.card : style.sageButton;
        attacksTab.GetComponent<Image>().sprite = attacks ? style.sageButton : style.card;
        actionsTab.GetComponentInChildren<TextMeshProUGUI>().color = attacks ? style.ink : style.cream;
        attacksTab.GetComponentInChildren<TextMeshProUGUI>().color = attacks ? style.cream : style.ink;
        attacksTab.interactable = !unit.IsKnockedDown;
        previousPage.gameObject.SetActive(pages > 1);
        nextPage.gameObject.SetActive(pages > 1);
        pageText.gameObject.SetActive(pages > 1);
        previousPage.interactable = commandPage > 0;
        nextPage.interactable = commandPage + 1 < pages;
        pageText.text = $"{commandPage + 1} / {pages}";
    }

    private void UpdateActionPoints()
    {
        Unit selectedUnit = actionSystem.GetSelectedUnit();

        actionPointsText.text = "Action Points: " + (selectedUnit != null ? selectedUnit.GetActionPoints() : 0);
    }

    private UnitActionSystem actionSystem;
    private TurnSystem turnSystem;

    private void OnDestroy()
    {
        if (actionSystem != null) actionSystem.OnSelectedUnitChange -= UnitActionSystem_OnSelectedUnitChanged;
        if (actionSystem != null) actionSystem.OnSelectedActionChanged -= UnitActionSystem_OnSelectedActionChanged;
        if (actionSystem != null) actionSystem.OnActionStarted -= UnitActionSystem_OnActionStarted;
        if (turnSystem != null) turnSystem.OnTurnChanged -= TurnSystem_OnTurnChanged;
        Unit.OnAnyActionPointsChanged -= Unit_OnAnyActionPointsChanged;
        Unit.OnAttackUISwitch -= Unit_OnAttackUISwitch;
        Unit.OnAnyKnockdownChanged -= Unit_OnKnockdownChanged;
    }
}
