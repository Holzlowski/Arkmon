using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoseLightAction : BaseAction
{
    public int Healing => Definition != null ? Definition.damage : 18;
    public override string GetActionName() => "Rosenlicht";

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        var result = new List<GridPosition>();
        if (!unit.CanPerformAction(this)) return result;
        var origin = unit.GetGridPosition();
        int range = Definition != null ? Definition.range : 3;
        var allies = unit.IsEnemy() ? UnitManager.Instance.GetEnemyUnitList() : UnitManager.Instance.GetFriendlyUnitList();
        foreach (var ally in allies)
        {
            if (ally.IsDead) continue;
            var health = ally.GetComponent<HealthSystem>();
            if (health.Health >= health.MaxHealth) continue;
            foreach (var cell in ally.OccupiedCells())
            {
                var offset = cell - origin;
                if (Mathf.Max(Mathf.Abs(offset.x),Mathf.Abs(offset.z)) <= range &&
                    !CoverSystem.Preview(origin,cell,0,ally).blocked)
                    result.Add(cell);
            }
        }        return result;
    }

    public override void TakeAction(GridPosition cell, Action completed)
    {
        if (isActive) return;
        if (!IsValidActionGridPosition(cell)) { completed?.Invoke(); return; }
        var target = LevelGrid.Instance.GetUnitOnGridPosition(cell);
        ActionStart(completed);
        CombatStrikeVisual.Show(unit.transform.position+Vector3.up*.55f,
            target.transform.position+Vector3.up*1.05f,new Color(.98f,.47f,.63f),.14f,"Rose light");
        target.GetComponent<HealthSystem>().Heal(Healing);
        ActionComplete();
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition cell)
        => new EnemyAIAction { gridPosition = cell, actionValue = Healing };
}
