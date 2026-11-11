using UnityEngine;

public sealed class DaggerStrikeAction : MeleeAttackAction
{
    protected override bool IsFire => false;
    protected override int DefaultDamage => 12;
    public override string GetActionName() => "Dolchhieb";

    private static bool IsBehind(Unit target, Vector3 direction)
        => Vector3.Dot(target.transform.forward,direction) <= -.7f;

    protected override void ApplyUnitHit(Unit target, GridPosition cell)
    {
        var offset = unit.GetGridPosition()-cell;
        var direction = new Vector3(offset.x,0,offset.z).normalized;
        bool behind = IsBehind(target,direction);
        var health = target.GetComponent<HealthSystem>();
        int before = health.Health;
        base.ApplyUnitHit(target,cell);
        if (behind && !target.IsDead && health.Health < before)
            target.transform.forward = direction;
    }

    public override int DamageAt(GridPosition cell)
    {
        var target = LevelGrid.Instance.GetUnitOnGridPosition(cell);
        if (target == null) return DamageAmount;
        var offset = unit.GetGridPosition()-cell;
        var direction = new Vector3(offset.x,0,offset.z).normalized;
        var opposite = new GridPosition(cell.x-offset.x,cell.z-offset.z);
        Unit ally = LevelGrid.Instance.IsValidGridPosition(opposite)
            ? LevelGrid.Instance.GetUnitOnGridPosition(opposite) : null;
        bool behind = IsBehind(target,direction);
        bool flanked = ally != null && !ally.IsDead && ally.IsEnemy() == unit.IsEnemy();
        return DamageAmount + (behind || flanked ? 6 : 0);
    }
}
