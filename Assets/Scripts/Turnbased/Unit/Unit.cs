using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private const int ACTION_POINTS_MAX = 3;
    [SerializeField] private MonsterDefinition definition;
    public MonsterDefinition Definition => definition;
    [SerializeField] private Color summonColor = new Color(1f, 0.42f, 0.1f, 1f);
    public Color GetSummonColor() => definition != null ? definition.summonColor : summonColor;
    private int MaxActionPoints => definition != null ? definition.maxActionPoints : ACTION_POINTS_MAX;

    public static event EventHandler OnAnyActionPointsChanged;
    public static event EventHandler OnAttackUISwitch;
    public static event EventHandler OnAnyUnitSpawned;
    public static event EventHandler OnAnyUnitDead;

    [SerializeField] private bool isEnemy;
    [SerializeField] private bool isInAttackSelection = false;

    private GridPosition gridPosition;
    private HealthSystem healthSystem;
    private TurnSystem subscribedTurnSystem;
    public bool IsDead => healthSystem.IsDead;

    [SerializeField] private BaseAction[] basicActionArray;
    [SerializeField] private BaseAction[] attacks;
    private BaseAction[] baseActionArray;
    private int actionPoints = ACTION_POINTS_MAX;

    void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        actionPoints = MaxActionPoints;
        //findet alle BaseAction Komponenten
        baseActionArray = GetComponents<BaseAction>();
    }

    void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);

        //Shift - F12(Windows).This will show you all references(within your solution) to that event
        subscribedTurnSystem = TurnSystem.Instance;
        subscribedTurnSystem.OnTurnChanged += TurnSystem_OnTurnChanged;
        healthSystem.OnDead += HealthSystem_OnDead;

        OnAnyUnitSpawned?.Invoke(this, EventArgs.Empty);
    }

    void Update()
    {
        SyncGridPosition();
    }

    public void MoveToWorldPosition(Vector3 worldPosition)
    {
        if (IsDead) return;
        transform.position = worldPosition;
        SyncGridPosition();
    }

    private void SyncGridPosition()
    {
        if (IsDead) return;
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            //Unit ändert ihre Gridposition
            GridPosition oldGridPosition = gridPosition;
            gridPosition = newGridPosition;

            LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
        }
    }

    public T GetAction<T>() where T : BaseAction
    {
        foreach (BaseAction baseAction in baseActionArray)
        {
            if (baseAction is T)
            {
                return (T)baseAction;
            }
        }
        return null;
    }

    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public BaseAction[] GetBaseActionArray()
    {
        if (IsDead) return Array.Empty<BaseAction>();
        if (isEnemy)
        {
            return baseActionArray;
        }
        else if (isInAttackSelection)
        {
            return attacks;
        }
        else
        {
            return basicActionArray;
        }

    }

    public void SwitchAttacksUI()
    {
        isInAttackSelection = !isInAttackSelection;

        OnAttackUISwitch?.Invoke(this, EventArgs.Empty);
    }

    public bool GetIsInAttackSelection()
    {
        return isInAttackSelection;
    }

    public bool IsEnemy()
    {
        return isEnemy;
    }

    public bool TrySpendActionPointsToTakeAction(BaseAction baseAction)
    {
        if (CanSpendActionPointsToTakeAction(baseAction))
        {
            SpendActionPoints(baseAction.GetActionPointCost());
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CanSpendActionPointsToTakeAction(BaseAction baseAction)
    {
        if (IsDead || !enabled || baseAction == null || !baseAction.enabled) return false;
        if (actionPoints >= baseAction.GetActionPointCost())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void SpendActionPoints(int amount)
    {
        actionPoints -= amount;

        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetActionPoints()
    {
        return actionPoints;
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if (IsDead) return;
        if ((IsEnemy() && !TurnSystem.Instance.IsPlayerTurn()) ||
                (!IsEnemy() && TurnSystem.Instance.IsPlayerTurn()))
        {
            actionPoints = MaxActionPoints;

            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        }

    }

    private void HealthSystem_OnDead(object sender, Transform e)
    {
        LevelGrid.Instance.RemoveUnitAtGridPosition(gridPosition, this);
        actionPoints = 0;
        foreach (BaseAction action in baseActionArray) action.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
        foreach (Canvas canvas in GetComponentsInChildren<Canvas>()) canvas.enabled = false;
        UnitAnimator animator = GetComponent<UnitAnimator>();
        if (animator != null) animator.enabled = false;
        enabled = false; // Gameplay stops immediately; the paper child can still show defeat.
        bool recalling = BattleDeployment.Instance != null && BattleDeployment.Instance.TryRecall(this);
        if (!recalling)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        OnAnyUnitDead?.Invoke(this, EventArgs.Empty);
    }

    public void Damage(int damage, Transform damageSource = null)
    {
        healthSystem.Damage(damage, damageSource);
    }

    private void OnDestroy()
    {
        if (subscribedTurnSystem != null)
            subscribedTurnSystem.OnTurnChanged -= TurnSystem_OnTurnChanged;
        if (healthSystem != null) healthSystem.OnDead -= HealthSystem_OnDead;
    }

    public float GetHealthNormalized()
    {
        return healthSystem.GetHealthNormalized();
    }

}
