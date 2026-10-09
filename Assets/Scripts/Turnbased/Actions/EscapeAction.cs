using System;
using System.Collections.Generic;

public class EscapeAction : BaseAction
{

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
        // Request escape before registering this action, so it does not block itself.
        UnitManager.Instance?.TryEscape();
        ActionStart(onActionComplete);
        ActionComplete();
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
