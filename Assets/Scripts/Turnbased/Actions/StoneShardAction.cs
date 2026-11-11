using UnityEngine;

public sealed class StoneShardAction : MeleeAttackAction
{
    protected override bool IsFire => false;
    protected override int DefaultDamage => 12;
    protected override int MinimumRange => 2;
    public override string GetActionName() => "Splitterschuss";

    protected override void OnImpact(Unit target, Vector3 origin, Vector3 destination)
        => CombatStrikeVisual.Show(origin, destination, new Color(.78f, .59f, .39f), .07f, "Stone shard");

    protected override void ApplyUnitHit(Unit target, GridPosition cell)
        => CoverSystem.Apply(unit.GetGridPosition(), target, DamageAt(cell), unit.transform, hitCell: cell);

    public override EnemyAIAction GetEnemyAIAction(GridPosition cell)
    {
        var target = LevelGrid.Instance.GetUnitOnGridPosition(cell);
        return new EnemyAIAction { gridPosition = cell, actionValue = target == null ? 1 : target.IsTrialObjective ? 105 : 80 };
    }
}