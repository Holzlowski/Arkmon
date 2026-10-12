using System;
using System.Collections.Generic;
using UnityEngine;

public class StandUpAction : BaseAction
{
    private float remaining;
    public float Progress => isActive ? 1f - Mathf.Clamp01(remaining / 0.35f) : 0f;
    public override string GetActionName() => "Aufstehen";
    public override int GetActionPointCost() => 1;
    public override List<GridPosition> GetValidActionGridPositionList()
        => unit.CanPerformAction(this) ? new List<GridPosition> { unit.GetGridPosition() }
            : new List<GridPosition>();
    public override EnemyAIAction GetEnemyAIAction(GridPosition position)
        => new EnemyAIAction { gridPosition = position, actionValue = 500 };

    public override void TakeAction(GridPosition position, Action completed)
    {
        if (isActive) return;
        if (!IsValidActionGridPosition(position)) { completed?.Invoke(); return; }
        remaining = 0.35f;
        ActionStart(completed);
    }

    private void Update()
    {
        if (!isActive) return;
        if (unit.IsDead) { ActionComplete(); return; }
        remaining -= Time.deltaTime;
        if (remaining > 0f) return;
        unit.SetKnockedDown(false);
        ActionComplete();
    }
}
