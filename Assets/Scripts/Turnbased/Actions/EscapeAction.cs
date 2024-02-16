using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EscapeAction : BaseAction
{

    private void Update()
    {
        if (!isActive)
        {
            return;
        }
        SceneManager.LoadScene("StartScene");
        ActionComplete();
    }
    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();

        return new List<GridPosition>
        {
            unitGridPosition
        };
    }

    //Gridposition macht hier nichts, aber muss aufgrund der Vererbung dabei sein
    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        //Funktion ClearBusy wird hier im Feld gespeichert
        ActionStart(onActionComplete);
    }

    public override string GetActionName()
    {
        return "Escape";
    }

    public override int GetActionPointCost()
    {
        return 0;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction()
        {
            gridPosition = gridPosition,
            actionValue = 0,
        };
    }
}
