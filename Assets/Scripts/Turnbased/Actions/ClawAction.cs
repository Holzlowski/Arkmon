public class ClawAction : MeleeAttackAction
{
    protected override bool IsFire => false;
    protected override int DefaultDamage => 15;
    public override string GetActionName() => "Krallenhieb";
}
