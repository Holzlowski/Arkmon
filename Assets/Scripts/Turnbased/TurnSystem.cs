using System;
using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }

    public event EventHandler OnTurnChanged;

    private int turnNumber = 1;
    private bool isPlayerTurn = true;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("ThereThere's more than one UnitActionSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void NextTurn()
    {
        if (!CanChangeTurn()) return;
        turnNumber++;
        isPlayerTurn = !isPlayerTurn;

        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanChangeTurn()
    {
        return !BaseAction.IsAnyActionActive && UnitManager.Instance != null && UnitManager.Instance.IsBattleRunning
            && (BattleDeployment.Instance == null || !BattleDeployment.Instance.IsRecalling);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetTurnNumber()
    {
        return turnNumber;
    }

    public bool IsPlayerTurn()
    {
        return isPlayerTurn;
    }
}
