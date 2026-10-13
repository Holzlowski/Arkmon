using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EmberAction : BaseAction
{
    public int DamageAmount => Definition != null ? Definition.damage : 18;
    public int Range => 1;
    public Unit TargetUnit { get; private set; }
    public Vector3? TargetPosition => isActive ? LevelGrid.Instance.GetWorldPosition(targetCell) : (Vector3?)null;
    private GridPosition targetCell;
    private float elapsed;
    private bool released, hit;
    private PaperSwordAnimation rig;
    private EmberFanVisual fan;
    public override string GetActionName() => "Glutfunke";
    public override int GetActionPointCost() => Definition != null ? Definition.actionPointCost : 2;
    private List<GridPosition> Row(GridPosition origin, GridPosition centre)
    {
        if (Mathf.Max(Mathf.Abs(centre.x - origin.x), Mathf.Abs(centre.z - origin.z)) != 1)
            return new List<GridPosition>();
        int x = Math.Sign(centre.x - origin.x), z = Math.Sign(centre.z - origin.z);
        if (x != 0 && z != 0)
            return new List<GridPosition> { origin + new GridPosition(x, 0), origin + new GridPosition(x, z), origin + new GridPosition(0, z) };
        GridPosition d = x != 0 ? new GridPosition(0, 1) : new GridPosition(1, 0);
        return new List<GridPosition> { centre + new GridPosition(-d.x, -d.z), centre, centre + d };
    }
    public List<GridPosition> GetAffectedGridPositionList(GridPosition centre) => Row(unit.GetGridPosition(), centre);
    private List<GridPosition> Impact(GridPosition origin, GridPosition centre)
        => Row(origin,centre).Where(c => LevelGrid.Instance.IsValidGridPosition(c) && !CoverSystem.Preview(origin,c,0).blocked).ToList();
    public List<GridPosition> GetImpactGridPositionList(GridPosition centre) => Impact(unit.GetGridPosition(),centre);
    private bool IsValidFrom(GridPosition origin, GridPosition centre)
    {
        int x = Mathf.Abs(centre.x - origin.x), z = Mathf.Abs(centre.z - origin.z);
        if (x > 1 || z > 1 || x + z == 0) return false;
        List<GridPosition> row = Row(origin, centre);
        return row.All(c => LevelGrid.Instance.IsValidGridPosition(c))
            && row.Any(c => !CoverSystem.Preview(origin, c, 0).blocked);
    }
    public override bool IsValidActionGridPosition(GridPosition centre)
        => unit.CanPerformAction(this) && IsValidFrom(unit.GetGridPosition(), centre);
    public override List<GridPosition> GetValidActionGridPositionList() => GetValidActionGridPositionList(unit.GetGridPosition());
    public List<GridPosition> GetValidActionGridPositionList(GridPosition origin)
    {
        var result = new List<GridPosition>();
        if (!unit.CanPerformAction(this)) return result;
        for (int x=-Range;x<=Range;x++) for(int z=-Range;z<=Range;z++)
        {
            GridPosition centre=origin+new GridPosition(x,z);
            if (IsValidFrom(origin, centre)) result.Add(centre);
        }
        return result;
    }
    public int GetTargetCountAtPosition(GridPosition origin)
    {
        var reachable = new HashSet<GridPosition>();
        foreach (GridPosition centre in GetValidActionGridPositionList(origin))
            foreach (GridPosition cell in Impact(origin, centre)) reachable.Add(cell);
        return (unit.IsEnemy() ? UnitManager.Instance.GetFriendlyUnitList() : UnitManager.Instance.GetEnemyUnitList())
            .Count(target => target != null && !target.IsDead && reachable.Contains(target.GetGridPosition()));
    }
    public override EnemyAIAction GetEnemyAIAction(GridPosition cell)
    {
        int score=0; var seen=new HashSet<Unit>();
        foreach(GridPosition affected in GetImpactGridPositionList(cell))
        {
            foreach(Unit target in LevelGrid.Instance.GetUnitListAtGridPosition(affected,unit))
            {
                if (target == null || target.IsDead || !seen.Add(target)) continue;
                int damage = Mathf.Min(target.PreviewDamage(DamageAmount, true), target.GetComponent<HealthSystem>().Health);
                score += target.IsEnemy() == unit.IsEnemy() ? -200 - damage * 3 : 120 + damage;
            }
        }
        return new EnemyAIAction{gridPosition=cell,actionValue=score};
    }
    public override void TakeAction(GridPosition cell,Action completed)
    {
        if(isActive)return;
        if(!IsValidActionGridPosition(cell)){completed?.Invoke();return;}
        targetCell=cell;TargetUnit=LevelGrid.Instance.GetUnitOnGridPosition(cell);
        elapsed=0;released=hit=false;ActionStart(completed);
        rig=GetComponent<PaperSwordAnimation>();rig?.BeginSparkScrape(LevelGrid.Instance.GetWorldPosition(cell));
    }
    private void Update()
    {
        if(!isActive)return;
        if(unit.IsDead){Finish();return;}
        elapsed+=Time.deltaTime;
        if(!released && elapsed>=PaperSwordAnimation.SparkReleaseTime)
        {
            released=true;rig?.MarkSparkRelease();
            Vector3 origin=rig!=null?rig.GetTipPosition():transform.position+Vector3.up*.6f;
            fan=EmberFanVisual.Play(origin,GetAffectedGridPositionList(targetCell));
        }
        if(!hit && elapsed>=PaperSwordAnimation.SparkReleaseTime+EmberFanVisual.TravelTime)
        {
            hit=true;
            // Snapshot before destroyed obstacles can open other sight lines in this same hit.
            List<GridPosition> cells=GetImpactGridPositionList(targetCell);
            var targets=new HashSet<Unit>();var obstacles=new HashSet<BurningObstacle>();
            foreach(GridPosition cell in cells)
            {
                foreach(Unit target in LevelGrid.Instance.GetUnitListAtGridPosition(cell,unit))
                    if(target!=null && !target.IsDead)targets.Add(target);
                foreach(BurningObstacle obstacle in CoverSystem.ObstaclesAt(cell))obstacles.Add(obstacle);
                FlameSwordImpactVisual.Play(LevelGrid.Instance.GetWorldPosition(cell)+Vector3.up*.2f);
            }
            foreach (Unit target in targets)
            {
                target.Damage(DamageAmount, unit.transform);
                if (!target.IsDead) CoverSystem.Ignite(target.GetComponent<BurningStatus>(), false);
            }
            foreach (BurningObstacle obstacle in obstacles)
            {
                obstacle.Damage(DamageAmount);
                if (!obstacle.IsDestroyed) CoverSystem.Ignite(obstacle.GetComponent<BurningStatus>(), true);
            }
            SoundManager.instance?.PlayEffect("fire-hit");
        }
        if(elapsed>=PaperSwordAnimation.SparkDuration)Finish();
    }
    private void Cleanup()
    {
        TargetUnit = null;
        rig?.CancelSwing();
        if (fan != null) Destroy(fan.gameObject);
        fan = null;
    }
    private void Finish()
    {
        Cleanup();
        ActionComplete();
    }
    protected override void OnDisable()
    {
        Cleanup();
        base.OnDisable();
    }
}
