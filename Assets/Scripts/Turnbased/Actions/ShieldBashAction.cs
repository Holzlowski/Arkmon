public sealed class ShieldBashAction : MeleeAttackAction
{
    protected override bool IsFire => false;
    protected override int DefaultDamage => 12;
    public override string GetActionName() => "Schildhieb";
}
