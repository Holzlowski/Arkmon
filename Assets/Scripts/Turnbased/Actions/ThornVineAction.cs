using UnityEngine;

public sealed class ThornVineAction : MeleeAttackAction
{
    protected override bool IsFire => false;
    protected override int DefaultDamage => 10;
    public override string GetActionName() => "Dornenranke";

    protected override void OnImpact(Unit target, Vector3 origin, Vector3 destination)
    {
        if (target != null) target.ApplyRoot();
        CombatStrikeVisual.Show(origin, destination, new Color(.47f,.78f,.31f),.13f,"Thorn vine");
    }
}
