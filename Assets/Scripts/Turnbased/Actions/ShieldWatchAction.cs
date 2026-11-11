using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ShieldWatchAction : BaseAction
{
    private Unit protectedUnit;
    public override string GetActionName() => "Schildwache";
    internal bool IsProtecting => protectedUnit != null && CanIntercept(protectedUnit);

    internal bool CanIntercept(Unit target)
    {
        if (target != protectedUnit || !isActiveAndEnabled || unit.IsDead || unit.IsKnockedDown || target.IsDead) return false;
        return target.IsWithinGridRange(unit.GetGridPosition(), 1);
    }

    internal void ClearProtection()
    {
        if (protectedUnit != null && protectedUnit.ShieldCover == this) protectedUnit.ShieldCover = null;
        protectedUnit = null;
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        var result = new List<GridPosition>();
        if (!unit.CanPerformAction(this) || IsProtecting) return result;
        var allies = unit.IsEnemy() ? UnitManager.Instance.GetEnemyUnitList() : UnitManager.Instance.GetFriendlyUnitList();
        foreach (var ally in allies)
        {
            if (ally.IsDead) continue;
            foreach (var cell in ally.OccupiedCells())
            {
                var offset = cell - unit.GetGridPosition();
                if (Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.z)) <= 1)
                    result.Add(cell);
            }
        }
        return result;
    }

    public override void TakeAction(GridPosition position, Action completed)
    {
        if (!IsValidActionGridPosition(position)) { completed?.Invoke(); return; }
        var target = LevelGrid.Instance.GetUnitOnGridPosition(position);
        if (target == null) { completed?.Invoke(); return; }
        ActionStart(completed);
        ClearProtection();
        target.ShieldCover?.ClearProtection();
        protectedUnit = target;
        target.ShieldCover = this;
        ActionComplete();
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition cell)
        => new EnemyAIAction { gridPosition = cell, actionValue = 1 };

    protected override void OnDisable()
    {
        ClearProtection();
        base.OnDisable();
    }
}
