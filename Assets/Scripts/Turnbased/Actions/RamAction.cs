using System;
using System.Collections.Generic;
using UnityEngine;

public class RamAction : BaseAction
{
    public static event EventHandler OnAnyRamHit;
    public Unit TargetUnit => IsActive ? targetUnit : null;

    [SerializeField, Min(0.01f)] private float windupDuration = 0.15f;
    [SerializeField, Min(0.1f)] private float chargeSpeed = 14f;
    [SerializeField, Min(0.1f)] private float pushSpeed = 8f;
    private enum Phase { Windup, Charge, Push, Follow }
    private Phase phase;
    private float timer;
    private Unit targetUnit;
    private GridPosition startCell, targetCell, pushCell, approachCell;
    private bool hitApplied;
    private bool blockedPush;
    private int MaxRange => Definition != null ? Definition.range : 3;
    public int DamageAmount => Definition != null ? Definition.damage : 30;
    public bool PreviewPush(GridPosition destination, out GridPosition pushed, out bool knockdown)
    {
        bool valid = TryGetPlan(unit.GetGridPosition(), destination, out _, out _, out pushed);
        knockdown = valid && !IsFree(pushed); return valid;
    }

    public override string GetActionName() => "Rammstoß";
    public override int GetActionPointCost() => Definition != null ? Definition.actionPointCost : 2;

    public override List<GridPosition> GetValidActionGridPositionList()
        => GetValidActionGridPositionList(unit.GetGridPosition());

    // Also used by movement evaluation from a hypothetical future position.
    public List<GridPosition> GetValidActionGridPositionList(GridPosition origin)
    {
        var result = new List<GridPosition>();
        foreach (GridPosition direction in new[] {
            new GridPosition(1, 0), new GridPosition(-1, 0),
            new GridPosition(0, 1), new GridPosition(0, -1) })
            for (int distance = 1; distance <= MaxRange; distance++)
            {
                GridPosition cell = new GridPosition(origin.x + direction.x * distance,
                    origin.z + direction.z * distance);
                if (TryGetPlan(origin, cell, out _, out _, out _)) result.Add(cell);
            }
        return result;
    }

    private bool IsFree(GridPosition cell)
    {
        if (!LevelGrid.Instance.IsValidGridPosition(cell)
            || !Pathfinding.Instance.IsWalkableGridPosition(cell)) return false;
        // The attacker can already occupy a transit cell while charging.
        return !LevelGrid.Instance.HasAnyUnitOnGridPosition(cell)
            || LevelGrid.Instance.GetUnitOnGridPosition(cell) == unit;
    }

    private bool TryGetPlan(GridPosition origin, GridPosition destination,
        out Unit target, out GridPosition direction, out GridPosition pushed)
    {
        target = null; direction = default; pushed = default;
        if (unit == null || !unit.CanPerformAction(this)) return false;
        int dx = destination.x - origin.x, dz = destination.z - origin.z;
        int distance = Mathf.Abs(dx) + Mathf.Abs(dz);
        if (!LevelGrid.Instance.IsValidGridPosition(origin)
            || !LevelGrid.Instance.IsValidGridPosition(destination)
            || (dx != 0 && dz != 0) || distance < 1 || distance > MaxRange) return false;
        target = LevelGrid.Instance.GetUnitOnGridPosition(destination);
        if (target == null || target.IsDead || target.IsEnemy() == unit.IsEnemy()
            || !Pathfinding.Instance.IsWalkableGridPosition(destination)) return false;
        direction = new GridPosition(Math.Sign(dx), Math.Sign(dz));
        for (int step = 1; step < distance; step++)
            if (!IsFree(new GridPosition(origin.x + direction.x * step,
                origin.z + direction.z * step))) return false;
        pushed = destination + direction;
        // An obstacle or board boundary stops the push; another monster blocks the attack.
        return !LevelGrid.Instance.IsValidGridPosition(pushed)
            || !LevelGrid.Instance.HasAnyUnitOnGridPosition(pushed);
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
        => new EnemyAIAction { gridPosition = gridPosition, actionValue = 200 };

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (isActive) return;
        startCell = unit.GetGridPosition();
        hitApplied = false;
        if (!TryGetPlan(startCell, gridPosition, out targetUnit, out GridPosition direction, out pushCell))
        {
            ActionStart(onActionComplete);
            ActionComplete();
            targetUnit = null;
            return;
        }
        blockedPush = !IsFree(pushCell);
        targetCell = gridPosition;
        approachCell = targetCell + new GridPosition(-direction.x, -direction.z);
        phase = Phase.Windup;
        timer = windupDuration;
        ActionStart(onActionComplete);
    }

    private bool Advance(Unit movingUnit, GridPosition destination, float speed)
    {
        Vector3 world = LevelGrid.Instance.GetWorldPosition(destination);
        Vector3 next = Vector3.MoveTowards(movingUnit.transform.position, world, speed * Time.deltaTime);
        movingUnit.MoveToWorldPosition(next);
        return next == world;
    }

    private void Update()
    {
        if (!isActive) return;
        if (unit == null || unit.IsDead)
        {
            SettleInterruptedAction();
            ActionComplete();
            return;
        }
        if (!hitApplied && (!TryGetPlan(startCell, targetCell, out Unit currentTarget, out _, out _)
            || currentTarget != targetUnit))
        {
            SettleInterruptedAction();
            ActionComplete();
            return;
        }
        switch (phase)
        {
            case Phase.Windup:
                timer -= Time.deltaTime;
                if (timer <= 0f) phase = Phase.Charge;
                break;
            case Phase.Charge:
                if (!Advance(unit, approachCell, chargeSpeed)) break;
                blockedPush = !IsFree(pushCell);
                hitApplied = true;
                targetUnit.Damage(DamageAmount, unit.transform);
                OnAnyRamHit?.Invoke(this, EventArgs.Empty);
                if (blockedPush && targetUnit != null && !targetUnit.IsDead)
                {
                    targetUnit.SetKnockedDown(true);
                    targetUnit = null;
                    ActionComplete();
                }
                else phase = Phase.Push;
                break;
            case Phase.Push:
                // A killing blow still lets the attacker claim the vacated field.
                if (targetUnit == null || targetUnit.IsDead || Advance(targetUnit, pushCell, pushSpeed))
                    phase = Phase.Follow;
                break;
            case Phase.Follow:
                if (Advance(unit, targetCell, chargeSpeed))
                {
                    targetUnit = null;
                    ActionComplete();
                }
                break;
        }
    }

    private void SettleInterruptedAction()
    {
        if (LevelGrid.Instance == null) return;
        // End on exact cells and release the action lock even when disabled mid-animation.
        if (hitApplied)
        {
            if (targetUnit != null && !targetUnit.IsDead)
                targetUnit.MoveToWorldPosition(LevelGrid.Instance.GetWorldPosition(blockedPush ? targetCell : pushCell));
            if (unit != null && !unit.IsDead)
                unit.MoveToWorldPosition(LevelGrid.Instance.GetWorldPosition(blockedPush && targetUnit != null && !targetUnit.IsDead ? approachCell : targetCell));
        }
        else if (unit != null && !unit.IsDead)
            unit.MoveToWorldPosition(LevelGrid.Instance.GetWorldPosition(startCell));
        targetUnit = null;
    }

    protected override void OnDisable()
    {
        if (isActive) SettleInterruptedAction();
        base.OnDisable();
    }
}
