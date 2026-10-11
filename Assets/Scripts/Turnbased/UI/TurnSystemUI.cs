using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnSystemUI : MonoBehaviour
{
    [SerializeField] private Button endTurnButton;
    [SerializeField] private TextMeshProUGUI turnNumberText;
    [SerializeField] private GameObject enemyTurnVisualObject;
    private TurnSystem turnSystem;
    private UnitManager unitManager;

    private void Start()
    {
        turnSystem = TurnSystem.Instance;
        unitManager = UnitManager.Instance;
        endTurnButton.onClick.AddListener(EndTurn);
        turnSystem.OnTurnChanged += Refresh;
        unitManager.OnBattleStateChanged += Refresh;
        BaseAction.OnAnyActionStarted += Refresh;
        BaseAction.OnAnyActionCompleted += Refresh;
        Refresh(this, EventArgs.Empty);
    }

    private void Update()
    {
        // Recall completion can release the turn without another action or state event.
        if (turnSystem != null)
            endTurnButton.interactable = turnSystem.IsPlayerTurn() && turnSystem.CanChangeTurn();
    }

    private void EndTurn()
    {
        if (turnSystem.IsPlayerTurn()) turnSystem.NextTurn();
    }

    private void Refresh(object sender, EventArgs e)
    {
        turnNumberText.text = "TURN " + turnSystem.GetTurnNumber();
        enemyTurnVisualObject.SetActive(!turnSystem.IsPlayerTurn() && unitManager.IsBattleRunning);
        endTurnButton.gameObject.SetActive(turnSystem.IsPlayerTurn());
        endTurnButton.interactable = turnSystem.IsPlayerTurn() && turnSystem.CanChangeTurn();
    }

    private void OnDestroy()
    {
        endTurnButton.onClick.RemoveListener(EndTurn);
        if (turnSystem != null) turnSystem.OnTurnChanged -= Refresh;
        if (unitManager != null) unitManager.OnBattleStateChanged -= Refresh;
        BaseAction.OnAnyActionStarted -= Refresh;
        BaseAction.OnAnyActionCompleted -= Refresh;
    }
}
