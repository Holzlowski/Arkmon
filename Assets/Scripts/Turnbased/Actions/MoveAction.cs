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

        if (Vector3.Distance(transform.position, targetPosittion) > stoppingDistance)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
        else
        {
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

        for (int x = -maxMoveDistance; x <= maxMoveDistance; x++)
        {
            for (int z = -maxMoveDistance; z <= maxMoveDistance; z++)
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

                if (!Pathfinding.Instance.HasPath(unitGridPosition, testGridPosition))
                {
                    continue;
                }

                //Weil Werte bei der Wegeberechnung mit 10 multipliziert werden, muss dies hier auch geschehen
                int pathfindingDistanceMultiplier = 10;
                if (Pathfinding.Instance.GetPathLength(unitGridPosition, testGridPosition) > maxMoveDistance * pathfindingDistanceMultiplier)
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

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int getTargetCountAtGridPosition = unit.GetAction<ShootAction>().GetTargetCountAtPosition(gridPosition);
        return new EnemyAIAction()
        {
            gridPosition = gridPosition,
            actionValue = getTargetCountAtGridPosition * 10,
        };
    }
}
