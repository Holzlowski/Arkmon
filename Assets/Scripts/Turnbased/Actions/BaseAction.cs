using System;
using System.Collections.Generic;
using UnityEngine;

//abstract, damit keine Instanzen von BaseAction erzeugt werden k�nnen
public abstract class BaseAction : MonoBehaviour
{

    public static event EventHandler OnAnyActionStarted;
    public static event EventHandler OnAnyActionCompleted;

    //protected, kann nicht von au�en zugegriffen werden, aber von Klassen, die von dieser vererben
    protected Unit unit;
    protected bool isActive;
    public bool IsActive => isActive;
    private static readonly HashSet<BaseAction> activeActions = new HashSet<BaseAction>();
    public static bool IsAnyActionActive => activeActions.Count > 0;
    //delegates sind Funktionen, die in Variabeln gespeichert sind
    protected Action onActionComplete;

    protected virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }

    //abstract, damit jede Klase, die erbt diese Funktion aufweisen muss
    public abstract string GetActionName();

    public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete);

    public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> validGridPositionList = GetValidActionGridPositionList();
        return validGridPositionList.Contains(gridPosition);
    }

    public abstract List<GridPosition> GetValidActionGridPositionList();

    public virtual int GetActionPointCost()
    {
        return Definition != null ? Definition.actionPointCost : 1;
    }

    protected AbilityDefinition Definition => unit != null && unit.Definition != null
        ? unit.Definition.GetAbility(GetActionName()) : null;

    protected void ActionStart(Action onActionComplete)
    {
        isActive = true;
        activeActions.Add(this);
        this.onActionComplete = onActionComplete;

        OnAnyActionStarted?.Invoke(this, EventArgs.Empty);
    }

    protected void ActionComplete()
    {
        if (!isActive) return;
        isActive = false;
        activeActions.Remove(this);
        Action callback = onActionComplete;
        onActionComplete = null;
        callback?.Invoke();

        OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveActions() => activeActions.Clear();

    protected virtual void OnDisable()
    {
        ActionComplete();
    }

    public Unit GetUnit()
    {
        return unit;
    }

    public EnemyAIAction GetBestEnemyAIAction()
    {
        List<EnemyAIAction> enemyAIActionList = new List<EnemyAIAction>();
        List<GridPosition> validActionGridPositionList = GetValidActionGridPositionList();

        foreach (GridPosition gridPosition in validActionGridPositionList)
        {
            EnemyAIAction enemyAIAction = GetEnemyAIAction(gridPosition);
            enemyAIActionList.Add(enemyAIAction);
        }

        if (enemyAIActionList.Count > 0)
        {
            enemyAIActionList.Sort((EnemyAIAction a, EnemyAIAction b) => b.actionValue - a.actionValue);
            return enemyAIActionList[0];
        }
        else
        {
            //No possible Enemmy AI Actions
            return null;
        }
    }

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);
}
