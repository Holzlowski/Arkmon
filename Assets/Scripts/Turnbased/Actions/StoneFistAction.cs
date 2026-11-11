using UnityEngine;

public sealed class StoneFistAction : MeleeAttackAction
{
    protected override bool IsFire => false;
    protected override int DefaultDamage => 22;
    public override string GetActionName() => "Steinfaust";

    protected override void OnImpact(Unit target, Vector3 origin, Vector3 destination)
        => CombatStrikeVisual.Show(origin, destination, new Color(.90f, .65f, .37f), .13f, "Stone fist");

    public override EnemyAIAction GetEnemyAIAction(GridPosition cell)
    {
        var target = LevelGrid.Instance.GetUnitOnGridPosition(cell);
        return new EnemyAIAction { gridPosition = cell, actionValue = target == null ? 1 : target.IsTrialObjective ? 230 : 180 };
    }
}