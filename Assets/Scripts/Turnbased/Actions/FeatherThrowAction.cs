using UnityEngine;

public sealed class FeatherThrowAction : MeleeAttackAction
{
    protected override bool IsFire => false;
    protected override int DefaultDamage => 8;
    public override string GetActionName() => "Federwurf";

    protected override void OnImpact(Unit target, Vector3 origin, Vector3 destination)
        => CombatStrikeVisual.Show(origin,destination,new Color(.70f,.59f,.96f),.075f,"Feather throw");
}
