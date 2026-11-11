using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Die Klasse `MoveAction` ist verantwortlich für die Bewegung und Animation einer Einheit in einer rasterbasierten Spielumgebung.
/// </summary>
public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;

    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float rotationSpeed = 0f;
    [SerializeField] private int maxMoveDistance = 4;
    private int MaxMoveDistance => Mathf.Max(1, (Definition != null ? Definition.range : maxMoveDistance) - (unit != null && unit.IsChilled ? 1 : 0));

    private List<Vector3> positionList;
    internal bool IsGliding => IsActive && unit.Definition != null && unit.Definition.canGlide;
    private int currentPositionIndex;
    float stoppingDistance = .1f;


    void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (unit.IsDead)
        {
            OnStopMoving?.Invoke(this, EventArgs.Empty);
            ActionComplete();
            return;
        }

        Vector3 targetPosittion = positionList[currentPositionIndex];
        Vector3 moveDirection = targetPosittion-transform.position;
        moveDirection.y = 0;
        moveDirection.Normalize();

        if (moveDirection.sqrMagnitude > .001f)
            transform.forward = Vector3.Lerp(transform.forward, moveDirection, rotationSpeed * Time.deltaTime);

        transform.position = Vector3.MoveTowards(transform.position, targetPosittion, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPosittion) <= stoppingDistance)
        {
            transform.position = targetPosittion;
            currentPositionIndex++;
            if (currentPositionIndex >= positionList.Count)
            {
                //Animatorskript setzt Walkinganimation auf false
                OnStopMoving?.Invoke(this, EventArgs.Empty);

                //in der BaseActionklasse gespeichert, Clearbusy() wird aufgerufen
                if (IsGliding) unit.MoveToWorldPosition(targetPosittion);
                ActionComplete();
            }

        }
    }

    /// <summary>
    /// Diese Methode ermöglicht externem Code, eine neue Zielposition für die Einheit festzulegen, auf die sie sich zubewegen soll.
    /// </summary>
    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (isActive) return;
        if (!IsValidActionGridPosition(gridPosition))
        {
            onActionComplete?.Invoke();
            return;
        }
        List<GridPosition> pathGridPositionList = Pathfinding.Instance.FindMovementPath(unit.GetGridPosition(), gridPosition, unit.Definition != null && unit.Definition.canGlide, out _);

        if (pathGridPositionList == null || pathGridPositionList.Count == 0)
        {
            ActionStart(onActionComplete);
            ActionComplete();
            return;
        }

        currentPositionIndex = 0;
        positionList = new List<Vector3>();

        var grid = LevelGrid.Instance;
        for (int i = 0; i < pathGridPositionList.Count; i++)
        {
            var cell = pathGridPositionList[i];
            var world = grid.GetWorldPosition(cell);
            if (i > 0 && Pathfinding.Instance.CalculateDistance(pathGridPositionList[i-1],cell) > 14)
                positionList.Add((grid.GetWorldPosition(pathGridPositionList[i-1])+world)*.5f+Vector3.up*.9f);
            positionList.Add(world);
        }

        //Animatorskript setzt Walkinganimation auf true
        OnStartMoving?.Invoke(this, EventArgs.Empty);

        ActionStart(onActionComplete);
    }

    /// <summary>
    /// Generiert eine Liste gültiger Rasterpositionen, zu denen die Einheit sich bewegen kann.
    /// </summary>
    /// <returns>Liste gültiger Rasterpositionen.</returns>
    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        if (!unit.CanPerformAction(this)) return validGridPositionList;

        GridPosition unitGridPosition = unit.GetGridPosition();

        for (int x = -MaxMoveDistance; x <= MaxMoveDistance; x++)
        {
            for (int z = -MaxMoveDistance; z <= MaxMoveDistance; z++)
            {
                GridPosition offsetGridPostion = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPostion;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                if (unitGridPosition == testGridPosition)
                {
                    // Gleiche Rasterposition, auf der sich die Einheit bereits befindet
                    continue;
                }

                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
                {
                    // Rasterposition bereits von einer anderen Einheit besetzt
                    continue;
                }

                if (!Pathfinding.Instance.IsWalkableGridPosition(testGridPosition))
                {
                    continue;
                }

                List<GridPosition> path = Pathfinding.Instance.FindMovementPath(unitGridPosition, testGridPosition, unit.Definition != null && unit.Definition.canGlide, out int pathLength);
                if (path == null)
                {
                    continue;
                }

                //Weil Werte bei der Wegeberechnung mit 10 multipliziert werden, muss dies hier auch geschehen
                int pathfindingDistanceMultiplier = 10;
                if (pathLength > MaxMoveDistance * pathfindingDistanceMultiplier)
                {
                    //Weg ist zu lang
                    continue;
                }

                validGridPositionList.Add(testGridPosition);
            }
        }
        return validGridPositionList;
    }

    public override string GetActionName()
    {
        return "Move";
    }

    protected override void OnDisable()
    {
        if (isActive) OnStopMoving?.Invoke(this, EventArgs.Empty);
        base.OnDisable();
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        ShootAction shootAction = unit.GetAction<ShootAction>();
        SwordAction swordAction = unit.GetAction<SwordAction>();
        RamAction ramAction = unit.GetAction<RamAction>();
        EmberAction emberAction = unit.GetAction<EmberAction>();
        int targetCount = shootAction != null ? shootAction.GetTargetCountAtPosition(gridPosition) : 0;
        if (swordAction != null) targetCount += swordAction.GetValidActionGridPositionList(gridPosition).Count;
        if (ramAction != null) targetCount += ramAction.GetValidActionGridPositionList(gridPosition)
            .Select(LevelGrid.Instance.GetUnitOnGridPosition).Distinct().Count();
        if (emberAction != null) targetCount += emberAction.GetTargetCountAtPosition(gridPosition);
        MeleeAttackAction[] meleeActions = unit.GetComponents<MeleeAttackAction>();
        foreach (MeleeAttackAction melee in meleeActions) targetCount += melee.GetValidActionGridPositionList(gridPosition)
            .Select(LevelGrid.Instance.GetUnitOnGridPosition).Where(target => target != null).Distinct().Count();
        int approachValue = 0;
        if (shootAction == null && (swordAction != null || ramAction != null || meleeActions.Length > 0))
        {
            int currentDistance = int.MaxValue;
            int candidateDistance = int.MaxValue;
            foreach (Unit target in UnitManager.Instance.GetUnitList())
            {
                if (target.IsEnemy() == unit.IsEnemy() || target.IsDead) continue;
                GridPosition position = target.GetGridPosition();
                GridPosition current = unit.GetGridPosition();
                int currentX = Mathf.Abs(current.x - position.x), currentZ = Mathf.Abs(current.z - position.z);
                int candidateX = Mathf.Abs(gridPosition.x - position.x), candidateZ = Mathf.Abs(gridPosition.z - position.z);
                currentDistance = Mathf.Min(currentDistance, ramAction != null
                    ? currentX + currentZ : Mathf.Max(currentX, currentZ));
                candidateDistance = Mathf.Min(candidateDistance, ramAction != null
                    ? candidateX + candidateZ : Mathf.Max(candidateX, candidateZ));
            }
            if (candidateDistance < currentDistance) approachValue = 1;
        }
        if (unit.IsEnemy() && BattleDeployment.Instance?.SealTrial != null)
        {
            foreach (var target in UnitManager.Instance.GetFriendlyUnitList())
            {
                if (!target.IsTrialObjective || target.IsDead) continue;
                var pathfinding = Pathfinding.Instance;
                int before = target.OccupiedCells().Min(cell => pathfinding.CalculateDistance(unit.GetGridPosition(), cell));
                int after = target.OccupiedCells().Min(cell => pathfinding.CalculateDistance(gridPosition, cell));
                approachValue += Mathf.Max(0,before-after);
                break;
            }
        }
        int coverValue = 0;
        if (targetCount > 0)
            foreach (Unit opponent in UnitManager.Instance.GetUnitList())
                if (!opponent.IsDead && opponent.IsEnemy() != unit.IsEnemy()
                    && (opponent.GetAction<EmberAction>() != null || opponent.GetAction<ShootAction>() != null))
                {
                    var plan = CoverSystem.Preview(opponent.GetGridPosition(), gridPosition, 10, unit);
                    if (plan.cover != null) coverValue = 3;
                }
        return new EnemyAIAction()
        {
            gridPosition = gridPosition,
            actionValue = targetCount * 10 + approachValue + coverValue,
        };
    }
}
