using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MeleeAttackAction : BaseAction
{
    public static event EventHandler OnAnyMeleeHit;
    protected abstract bool IsFire { get; }
    protected abstract int DefaultDamage { get; }
    public Unit TargetUnit { get; private set; }
    public Vector3? TargetPosition => IsActive && LevelGrid.Instance != null
        ? LevelGrid.Instance.GetWorldPosition(targetCell) : (Vector3?)null;
    private BurningObstacle obstacle;
    private GridPosition targetCell;
    private float timer;
    private bool hitApplied;
    private PaperSwordAnimation swordAnimation;
    public int Range => Definition != null ? Definition.range : 1;

    public override List<GridPosition> GetValidActionGridPositionList()
        => GetValidActionGridPositionList(unit.GetGridPosition());

    public List<GridPosition> GetValidActionGridPositionList(GridPosition origin)
    {
        var result = new List<GridPosition>();
        if (!unit.CanPerformAction(this)) return result;
        for (int x = -Range; x <= Range; x++)
            for (int z = -Range; z <= Range; z++)
            {
                GridPosition cell = origin + new GridPosition(x, z);
                if (cell == origin || !LevelGrid.Instance.IsValidGridPosition(cell)) continue;
                if (FindTarget(cell, out _, out _)) result.Add(cell);
            }
        return result;
    }

    private bool FindTarget(GridPosition cell, out Unit target, out BurningObstacle prop)
    {
        target = LevelGrid.Instance.GetUnitOnGridPosition(cell);
        prop = null;
        if (target != null) return !target.IsDead && target.IsEnemy() != unit.IsEnemy();
        if (!IsFire) return false;
        foreach (BurningStatus status in BurningStatus.Active)
            if (status != null && status.Obstacle != null && status.CanBurn && status.Position == cell)
            { prop = status.Obstacle; return true; }
        return false;
    }

    public override void TakeAction(GridPosition cell, Action completed)
    {
        if (isActive) return;
        if (unit.IsDead || !isActiveAndEnabled || !IsValidActionGridPosition(cell)
            || !FindTarget(cell, out Unit target, out BurningObstacle prop))
        { completed?.Invoke(); return; }
        TargetUnit = target; obstacle = prop; targetCell = cell;
        swordAnimation = IsFire ? unit.GetComponent<PaperSwordAnimation>() : null;
        hitApplied = false; timer = swordAnimation != null ? PaperSwordAnimation.HitTime : 0.3f;
        if (swordAnimation != null) swordAnimation.BeginSwing(LevelGrid.Instance.GetWorldPosition(cell), true);
        ActionStart(completed);
    }

    private void Update()
    {
        if (!isActive) return;
        if (unit.IsDead) { Finish(); return; }
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        if (hitApplied) { Finish(); return; }
        if (!IsValidActionGridPosition(targetCell)
            || !FindTarget(targetCell, out Unit current, out BurningObstacle currentProp)
            || current != TargetUnit || currentProp != obstacle)
        { Finish(); return; }
        if (swordAnimation != null) swordAnimation.MarkImpact();
        hitApplied = true;
        timer = swordAnimation != null ? PaperSwordAnimation.Duration - PaperSwordAnimation.HitTime : 0.3f;
        Vector3 destination = LevelGrid.Instance.GetWorldPosition(targetCell) + Vector3.up * 0.5f;
        PaperCharacterVisual visual = unit.GetComponent<PaperCharacterVisual>();
        Vector3 origin = visual != null ? visual.GetAttackOrigin() : unit.transform.position + Vector3.up * 0.6f;
        if (swordAnimation == null) CombatStrikeVisual.Show(origin, destination, IsFire);
        OnAnyMeleeHit?.Invoke(this, EventArgs.Empty);
        if (TargetUnit != null) TargetUnit.Damage(Definition != null ? Definition.damage : DefaultDamage, unit.transform);
        if (IsFire)
        {
            BurningStatus status = TargetUnit != null ? TargetUnit.GetComponent<BurningStatus>()
                : obstacle != null ? obstacle.GetComponent<BurningStatus>() : null;
            BurningSystem system = TurnSystem.Instance != null ? TurnSystem.Instance.GetComponent<BurningSystem>() : null;
            if (status != null && system != null)
            {
                FireRules rules = system.Rules;
                status.TryIgnite(TargetUnit != null ? rules.monsterHitChance : 1f,
                    rules.durationRounds, rules.damagePerRound);
            }
        }
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition cell)
        => new EnemyAIAction { gridPosition = cell, actionValue = 180 + (IsFire ? 20 : 0) };
    private void Finish()
    {
        if (swordAnimation != null) swordAnimation.CancelSwing();
        TargetUnit = null; obstacle = null; ActionComplete();
    }
    protected override void OnDisable()
    {
        if (swordAnimation != null) swordAnimation.CancelSwing();
        TargetUnit = null; obstacle = null; base.OnDisable();
    }
}
