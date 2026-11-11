using UnityEngine;

public sealed class IceVolleyAction : MeleeAttackAction
{
    private int readyTurn;
    public bool IsRecharging => TurnSystem.Instance != null && TurnSystem.Instance.GetTurnNumber() < readyTurn;
    protected override bool IsFire => false;
    protected override int DefaultDamage => 16;
    public override string GetActionName() => "Eiszapfensalve";

    protected override void OnImpact(Unit target, Vector3 origin, Vector3 destination)
    {
        readyTurn = TurnSystem.Instance.GetTurnNumber() + 4;
        if (target != null) target.ApplyChill();
        CombatStrikeVisual.ShowIce(origin, destination);
    }
}
