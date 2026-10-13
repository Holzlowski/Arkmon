using System;
using System.Collections.Generic;

public class GuardAction : BaseAction
{
    public override string GetActionName() => "Abwehr";
    public override int GetActionPointCost() => 1;
    public override List<GridPosition> GetValidActionGridPositionList()
        => unit.CanPerformAction(this) && !unit.IsGuarding
            ? new List<GridPosition> { unit.GetGridPosition() } : new List<GridPosition>();
    public override EnemyAIAction GetEnemyAIAction(GridPosition position)
        => new EnemyAIAction { gridPosition = position, actionValue = 1 };
    public override void TakeAction(GridPosition position, Action completed)
    {
        if (isActive) return;
        if (!IsValidActionGridPosition(position)) { completed?.Invoke(); return; }
        ActionStart(completed); unit.SetGuarding(true); ActionComplete();
    }
}
