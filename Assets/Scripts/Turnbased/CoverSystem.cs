using System.Linq;
using UnityEngine;

// Preview and actual impact use the same deterministic rules. Low cover only protects
// an adjacent target; tall obstacles block the entire sight line.
public static class CoverSystem
{
    public struct DamagePlan
    {
        public bool blocked;
        public BurningObstacle cover;
        public int coverDamage, damageBeforeGuard, unitDamage;
    }

    public static System.Collections.Generic.IEnumerable<BurningObstacle> ObstaclesAt(GridPosition cell)
        => BurningStatus.Active.Select(s => s != null ? s.Obstacle : null)
            .Where(o => o != null && !o.IsDestroyed && o.isActiveAndEnabled && o.GetGridPosition() == cell).Distinct();
    public static BurningObstacle ObstacleAt(GridPosition cell) => ObstaclesAt(cell).FirstOrDefault();

    public static DamagePlan Preview(GridPosition origin, GridPosition destination, int damage, Unit target = null)
    {
        var plan = new DamagePlan { damageBeforeGuard = Mathf.Max(0, damage) };
        Vector3 start = LevelGrid.Instance.GetWorldPosition(origin) + Vector3.up * .65f;
        Vector3 end = LevelGrid.Instance.GetWorldPosition(destination) + Vector3.up * .65f;
        float distance = Vector3.Distance(start, end);
        if (distance < .01f) { plan.unitDamage = target != null ? target.PreviewDamage(damage, true) : damage; return plan; }
        BurningObstacle directTarget = target == null ? ObstacleAt(destination) : null;
        var hits = Physics.RaycastAll(start, (end - start).normalized, distance,
            LayerMask.GetMask("Obstacles"), QueryTriggerInteraction.Ignore).OrderBy(h => h.distance);
        foreach (RaycastHit hit in hits)
        {
            BurningObstacle obstacle = hit.collider.GetComponentInParent<BurningObstacle>();
            if (obstacle != null && (obstacle == directTarget || target == null && obstacle.GetGridPosition() == destination || obstacle.IsDestroyed)) continue;
            if (obstacle == null || obstacle.BlocksSight)
            { plan.blocked = true; plan.damageBeforeGuard = plan.unitDamage = 0; return plan; }
            GridPosition position = obstacle.GetGridPosition();
            if (target == null || !obstacle.ProvidesCover
                || Mathf.Max(Mathf.Abs(position.x - destination.x), Mathf.Abs(position.z - destination.z)) > 1) continue;
            // Choose the closest cover to the target, never stack multiple reductions.
            plan.cover = obstacle;
        }
        if (plan.cover != null)
        {
            plan.coverDamage = Mathf.Min(plan.cover.Health, Mathf.CeilToInt(damage * plan.cover.Absorption));
            plan.damageBeforeGuard -= plan.coverDamage;
        }
        plan.unitDamage = target != null ? target.PreviewDamage(plan.damageBeforeGuard, true) : plan.damageBeforeGuard;
        return plan;
    }

    public static bool Apply(GridPosition origin, Unit target, int damage, Transform source, bool fire = false)
    {
        if (target == null || target.IsDead) return false;
        DamagePlan plan = Preview(origin, target.GetGridPosition(), damage, target);
        if (plan.blocked) return false;
        if (plan.cover != null)
        {
            if (fire) Ignite(plan.cover.GetComponent<BurningStatus>(), true);
            plan.cover.Damage(plan.coverDamage);
        }
        target.Damage(plan.damageBeforeGuard, source);
        if (fire && plan.damageBeforeGuard > 0) Ignite(target.GetComponent<BurningStatus>(), false);
        return true;
    }

    public static void Ignite(BurningStatus status, bool guaranteed)
    {
        BurningSystem system = TurnSystem.Instance != null ? TurnSystem.Instance.GetComponent<BurningSystem>() : null;
        if (status == null || system == null) return;
        FireRules rules = system.Rules;
        status.TryIgnite(guaranteed ? 1f : rules.monsterHitChance, rules.durationRounds, rules.damagePerRound);
    }
}
