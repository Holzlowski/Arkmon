public sealed class RoseThrustAction : MeleeAttackAction
{
    protected override bool IsFire => false;
    protected override int DefaultDamage => 12;
    public override string GetActionName() => "Rosenstich";
}
