using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class UnitActionSystemUI : MonoBehaviour
{
    [SerializeField] private Transform actionButtonPrefab;
    [SerializeField] private Transform actionButtonContainerTransform;
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private Transform attacksUISwitchButtonPrefab;


    private List<ActionButtonUI> actionButtonUIList;

    private void Awake()
    {
        actionButtonUIList = new List<ActionButtonUI>();
    }

    private void Start()
    {
        actionSystem = UnitActionSystem.Instance;
        turnSystem = TurnSystem.Instance;
        actionSystem.OnSelectedUnitChange += UnitActionSystem_OnSelectedUnitChanged;
        actionSystem.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;
        actionSystem.OnActionStarted += UnitActionSystem_OnActionStarted;
        turnSystem.OnTurnChanged += TurnSystem_OnTurnChanged;
        Unit.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged;
        Unit.OnAttackUISwitch += Unit_OnAttackUISwitch;

        UpdateActionPoints();
        CreateUnitActionButton();
        UpdateSelectedVisual();
    }

    private void CreateUnitActionButton()
    {
        foreach (Transform buttonTransform in actionButtonContainerTransform)
        {
            Destroy(buttonTransform.gameObject);
        }

        actionButtonUIList.Clear();

        Unit selectedUnit = actionSystem.GetSelectedUnit();

        if (selectedUnit == null) return;

        Transform attacksUISwitchButton = Instantiate(attacksUISwitchButtonPrefab, actionButtonContainerTransform);

        foreach (BaseAction baseAction in selectedUnit.GetBaseActionArray())
        {
            Transform actionButtonTransform = Instantiate(actionButtonPrefab, actionButtonContainerTransform);
            ActionButtonUI actionButtonUI = actionButtonTransform.GetComponent<ActionButtonUI>();
            actionButtonUI.SetBaseAction(baseAction);

            actionButtonUIList.Add(actionButtonUI);
        }
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
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
    }
}
