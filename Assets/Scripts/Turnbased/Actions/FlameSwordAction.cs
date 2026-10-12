public class FlameSwordAction : MeleeAttackAction
{
    protected override bool IsFire => true;
    protected override int DefaultDamage => 25;
    public override string GetActionName() => "Flammenschwert";
    public override int GetActionPointCost() => Definition != null ? Definition.actionPointCost : 2;
}
