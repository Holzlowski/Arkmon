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
    private Button backButton;
    private bool categoryOpen;
    private GameObject landing;
    private TextMeshProUGUI categoryTitle;
    public bool IsCategoryOpen => categoryOpen;
    public void ConfigureMenuPresentation(GameObject root, TextMeshProUGUI title) { landing=root; categoryTitle=title; }
    public bool IsShowingExplanation => tooltip != null && tooltip.activeInHierarchy;
    public void ConfigureBack(Button back)
    {
        backButton = back; back.onClick.AddListener(BackToCategories);
    }
    public void BackToCategories()
    {
        if (actionSystem == null || actionSystem.IsBusy || BaseAction.IsAnyActionActive) return;
        if (actionSystem.GetSelectedAction() != null)
        { actionSystem.SetSelectedAction(null); HideTooltip(); RefreshTabs(actionSystem.GetSelectedUnit(), Mathf.Max(1, Mathf.CeilToInt(actionSystem.GetSelectedUnit().GetBaseActionArray().Length / (float)CardsPerPage))); return; }
        categoryOpen = false; commandPage = 0; HideTooltip();
        if (landing != null) landing.SetActive(true);
        if (categoryTitle != null) categoryTitle.text="1 · Wähle einen Bereich";
        actionSystem.SetSelectedAction(null);
        actionButtonContainerTransform.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
        previousPage.gameObject.SetActive(false); nextPage.gameObject.SetActive(false); pageText.gameObject.SetActive(false);
    }

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
        categoryOpen = true; actionButtonContainerTransform.gameObject.SetActive(true);
        commandPage = 0;
        if (selected.GetIsInAttackSelection() != attacks) selected.SwitchAttacksUI();
        else CreateUnitActionButton();
        actionSystem.SetSelectedAction(null);
        RefreshTabs(selected, Mathf.Max(1, Mathf.CeilToInt(selected.GetBaseActionArray().Length / (float)CardsPerPage)));
    }

    private void ChangePage(int direction)
    {
        if (BaseAction.IsAnyActionActive || actionSystem.IsBusy) return;
        commandPage += direction;
        CreateUnitActionButton();
        Unit selected = actionSystem.GetSelectedUnit();
        BaseAction[] commands = selected != null ? selected.GetBaseActionArray() : Array.Empty<BaseAction>();
        actionSystem.SetSelectedAction(null);
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
        UnitManager.Instance.OnBattleStateChanged += BattleStateChanged;
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
        categoryOpen = false;
        actionSystem.SetSelectedAction(null);
        CreateUnitActionButton();
        UpdateSelectedVisual();
        UpdateActionPoints();
    }

    private void UnitActionSystem_OnActionStarted(object sender, EventArgs e)
    {
        UpdateActionPoints();
    }

    private void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        if (actionSystem.GetSelectedUnit() != null) RefreshTabs(actionSystem.GetSelectedUnit(), Mathf.Max(1, Mathf.CeilToInt(actionSystem.GetSelectedUnit().GetBaseActionArray().Length / (float)CardsPerPage)));
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
        categoryOpen = false;
        actionSystem.SetSelectedAction(null);
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
        if (actionsTab == null || unit == null) return;
        actionButtonContainerTransform.gameObject.SetActive(categoryOpen);
        if (landing != null) landing.SetActive(!categoryOpen);
        if (backButton != null) backButton.gameObject.SetActive(categoryOpen);
        bool attacks = unit.GetIsInAttackSelection() && !unit.IsKnockedDown;
        if (categoryTitle != null) categoryTitle.text = !categoryOpen ? "1 · Wähle einen Bereich" : actionSystem.GetSelectedAction() == null ? "2 · " + (attacks ? "Attacke" : "Aktion") + " wählen" : "3 · " + PaperUIStyle.ActionName(actionSystem.GetSelectedAction()) + " · Ziel wählen";
        if (backButton != null) backButton.GetComponentInChildren<TextMeshProUGUI>().text = actionSystem.GetSelectedAction() == null ? "Zurück" : "Abbrechen";
        PaperUIStyle style = PaperUIStyle.Load();
        actionsTab.GetComponent<Image>().sprite = !categoryOpen || attacks ? style.card : style.sageButton;
        attacksTab.GetComponent<Image>().sprite = categoryOpen && attacks ? style.sageButton : style.card;
        actionsTab.GetComponentInChildren<TextMeshProUGUI>().color = !categoryOpen || attacks ? style.ink : style.cream;
        attacksTab.GetComponentInChildren<TextMeshProUGUI>().color = categoryOpen && attacks ? style.cream : style.ink;
        attacksTab.interactable = !unit.IsKnockedDown;
        previousPage.gameObject.SetActive(categoryOpen && pages > 1);
        nextPage.gameObject.SetActive(categoryOpen && pages > 1);
        pageText.gameObject.SetActive(categoryOpen && pages > 1);
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
    private void LateUpdate()
    {
        if (actionSystem == null || actionsTab == null || turnSystem == null) return;
        bool ready = UnitManager.Instance != null && UnitManager.Instance.IsBattleRunning && turnSystem.IsPlayerTurn()
            && !actionSystem.IsBusy && !BaseAction.IsAnyActionActive && !SceneLoadManager.IsTransitioning;
        var unit = actionSystem.GetSelectedUnit();
        actionsTab.interactable = ready && unit != null && !unit.IsDead;
        attacksTab.interactable = actionsTab.interactable && !unit.IsKnockedDown;
        if (backButton != null) backButton.interactable = ready;
        if (ready && categoryOpen && Input.GetKeyDown(KeyCode.Escape)) BackToCategories();
    }
    private void BattleStateChanged(object sender, EventArgs e)
    {
        if (!UnitManager.Instance.IsBattleRunning) return;
        // Rebind after deployment: never keep cards pointing at a retired scene template.
        categoryOpen = false; actionSystem.SetSelectedAction(null);
        CreateUnitActionButton(); UpdateActionPoints(); UpdateSelectedVisual();
    }

    private void OnDestroy()
    {
        if (UnitManager.Instance != null) UnitManager.Instance.OnBattleStateChanged -= BattleStateChanged;
        if (actionSystem != null) actionSystem.OnSelectedUnitChange -= UnitActionSystem_OnSelectedUnitChanged;
        if (actionSystem != null) actionSystem.OnSelectedActionChanged -= UnitActionSystem_OnSelectedActionChanged;
        if (actionSystem != null) actionSystem.OnActionStarted -= UnitActionSystem_OnActionStarted;
        if (turnSystem != null) turnSystem.OnTurnChanged -= TurnSystem_OnTurnChanged;
        Unit.OnAnyActionPointsChanged -= Unit_OnAnyActionPointsChanged;
        Unit.OnAttackUISwitch -= Unit_OnAttackUISwitch;
        Unit.OnAnyKnockdownChanged -= Unit_OnKnockdownChanged;
    }
}
