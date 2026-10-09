using System;
using System.Collections.Generic;
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
    private int MaxMoveDistance => Definition != null ? Definition.range : maxMoveDistance;

    private List<Vector3> positionList;
    private int currentPositionIndex;
    float stoppingDistance = .1f;


    void Update()
    {
        if (!isActive)
        {
            return;
        }

        Vector3 targetPosittion = positionList[currentPositionIndex];
        Vector3 moveDirection = (targetPosittion - transform.position).normalized;

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
                ActionComplete();
            }

        }
    }

    /// <summary>
    /// Diese Methode ermöglicht externem Code, eine neue Zielposition für die Einheit festzulegen, auf die sie sich zubewegen soll.
    /// </summary>
    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        List<GridPosition> pathGridPositionList = Pathfinding.Instance.FindPath(unit.GetGridPosition(), gridPosition, out int pathLength);

        if (pathGridPositionList == null || pathGridPositionList.Count == 0)
        {
            ActionStart(onActionComplete);
            ActionComplete();
            return;
        }

        currentPositionIndex = 0;
        positionList = new List<Vector3>();

        foreach (GridPosition pathGridPosition in pathGridPositionList)
        {
            positionList.Add(LevelGrid.Instance.GetWorldPosition(pathGridPosition));
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

                List<GridPosition> path = Pathfinding.Instance.FindPath(unitGridPosition, testGridPosition, out int pathLength);
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
        int targetCount = shootAction != null ? shootAction.GetTargetCountAtPosition(gridPosition) : 0;
        if (swordAction != null) targetCount += swordAction.GetValidActionGridPositionList(gridPosition).Count;
        int approachValue = 0;
        if (shootAction == null && swordAction != null)
        {
            int currentDistance = int.MaxValue;
            int candidateDistance = int.MaxValue;
            foreach (Unit target in UnitManager.Instance.GetUnitList())
            {
                if (target.IsEnemy() == unit.IsEnemy() || target.IsDead) continue;
                GridPosition position = target.GetGridPosition();
                GridPosition current = unit.GetGridPosition();
                currentDistance = Mathf.Min(currentDistance, Mathf.Max(Mathf.Abs(current.x - position.x), Mathf.Abs(current.z - position.z)));
                candidateDistance = Mathf.Min(candidateDistance, Mathf.Max(Mathf.Abs(gridPosition.x - position.x), Mathf.Abs(gridPosition.z - position.z)));
            }
            if (candidateDistance < currentDistance) approachValue = 1;
        }
        return new EnemyAIAction()
        {
            gridPosition = gridPosition,
            actionValue = targetCount * 10 + approachValue,
        };
    }
}
