using System;
using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }

    public event EventHandler OnTurnChanged;

    private int turnNumber = 1;
    private bool isPlayerTurn = true;
    private BurningSystem burningSystem;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("ThereThere's more than one UnitActionSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        burningSystem = GetComponent<BurningSystem>();
        if (burningSystem == null) burningSystem = gameObject.AddComponent<BurningSystem>();
    }

    public void NextTurn()
    {
        if (!CanChangeTurn()) return;
        // A full round ends when the opponent returns control to the player.
        if (!isPlayerTurn) burningSystem.ResolveRound();
        turnNumber++;
        isPlayerTurn = !isPlayerTurn;

        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanChangeTurn()
    {
        return (burningSystem == null || !burningSystem.IsResolving) && !BaseAction.IsAnyActionActive && UnitManager.Instance != null && UnitManager.Instance.IsBattleRunning
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
