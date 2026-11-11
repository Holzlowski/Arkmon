using System;
using System.Collections.Generic;
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
    public static event EventHandler OnAnyKnockdownChanged;
    public bool IsKnockedDown { get; private set; }
    internal bool IsTrialObjective => trialCells != null;
    private GridPosition[] trialCells;

    internal void SetTrialCells(GridPosition[] cells) => trialCells = cells;

    internal IEnumerable<GridPosition> OccupiedCells()
    {
        if (trialCells == null) yield return gridPosition;
        else
            foreach (var cell in trialCells) yield return cell;
    }

    internal bool IsWithinGridRange(GridPosition origin, int range)
    {
        foreach (var cell in OccupiedCells())
            if (Mathf.Max(Mathf.Abs(origin.x - cell.x), Mathf.Abs(origin.z - cell.z)) <= range)
                return true;
        return false;
    }
    public bool IsGuarding { get; private set; }
    public void SetGuarding(bool guarding) { IsGuarding = guarding && !IsDead && !IsKnockedDown; }
    internal ShieldWatchAction ShieldCover { get; set; }
    private int chillEndsAt, rootEndsAt;
    public bool IsRooted => TurnSystem.Instance != null && TurnSystem.Instance.GetTurnNumber() < rootEndsAt;
    internal void ApplyRoot() => rootEndsAt = TurnSystem.Instance.GetTurnNumber() + 2;
    public bool IsChilled => TurnSystem.Instance != null && TurnSystem.Instance.GetTurnNumber() < chillEndsAt;
    internal void ApplyChill() => chillEndsAt = TurnSystem.Instance.GetTurnNumber() + 2;

    public int PreviewDamage(int damage, bool direct)
    {
        if (direct && ShieldCover != null && ShieldCover.CanIntercept(this))
            return ShieldCover.GetUnit() == this ? Mathf.CeilToInt(Mathf.Max(0, damage) * .5f) : 0;
        return Mathf.Max(0, IsGuarding && direct ? Mathf.CeilToInt(damage * .5f) : damage);
    }

    public bool CanPerformAction(BaseAction action) => !IsTrialObjective && !IsDead && enabled
        && action != null && action.enabled
        && !(action is GuardAction && IsGuarding)
        && !(action is IceVolleyAction volley && volley.IsRecharging)
        && !(IsRooted && (action is MoveAction || action is RamAction || action is ShadowStepAction))
        && !(action is ShieldWatchAction watch && watch.IsProtecting)
        && (IsKnockedDown ? action is StandUpAction : !(action is StandUpAction));

    public void SetKnockedDown(bool knockedDown)
    {
        if (IsTrialObjective || IsDead || IsKnockedDown == knockedDown) return;
        IsKnockedDown = knockedDown;
        if (knockedDown) { isInAttackSelection = false; SetGuarding(false); }
        OnAnyKnockdownChanged?.Invoke(this, EventArgs.Empty);
    }

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
        if (GetComponent<BurningStatus>() == null) gameObject.AddComponent<BurningStatus>();
        actionPoints = MaxActionPoints;
        if (GetComponent<StandUpAction>() == null) gameObject.AddComponent<StandUpAction>();
        if (GetComponent<GuardAction>() == null) gameObject.AddComponent<GuardAction>();
        //findet alle BaseAction Komponenten
        baseActionArray = GetComponents<BaseAction>();
    }

    void Start()
    {
        if (GetComponent<PaperHitFeedback>() == null) gameObject.AddComponent<PaperHitFeedback>();
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        foreach (var cell in OccupiedCells())
            LevelGrid.Instance.AddUnitAtGridPosition(cell, this);

        //Shift - F12(Windows).This will show you all references(within your solution) to that event
        subscribedTurnSystem = TurnSystem.Instance;
        subscribedTurnSystem.OnTurnChanged += TurnSystem_OnTurnChanged;
        healthSystem.OnDead += HealthSystem_OnDead;

        OnAnyUnitSpawned?.Invoke(this, EventArgs.Empty);
    }

    void Update()
    {
        if (!IsTrialObjective && (definition == null || !definition.canGlide || GetAction<MoveAction>()?.IsGliding != true)) SyncGridPosition();
    }

    public void MoveToWorldPosition(Vector3 worldPosition)
    {
        if (IsTrialObjective || IsDead) return;
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
        if (IsKnockedDown) return new BaseAction[] { GetAction<StandUpAction>() };
        if (isEnemy)
        {
            return baseActionArray;
        }
        else if (isInAttackSelection)
        {
            return attacks ?? Array.Empty<BaseAction>();
        }
        else
        {
            var basic = basicActionArray ?? Array.Empty<BaseAction>();
            var result = new BaseAction[basic.Length + 1];
            Array.Copy(basic, result, basic.Length); result[basic.Length] = GetAction<GuardAction>();
            return result;
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
        if (!CanPerformAction(baseAction)) return false;
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
            SetGuarding(false);
            GetAction<ShieldWatchAction>()?.ClearProtection();

            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        }

    }

    internal void RetireDemonstration()
    {
        foreach (var cell in OccupiedCells())
            LevelGrid.Instance.RemoveUnitAtGridPosition(cell, this);
        gameObject.SetActive(false);
        OnAnyUnitDead?.Invoke(this, EventArgs.Empty);
        Destroy(gameObject);
    }

    private void HealthSystem_OnDead(object sender, Transform e)
    {
        foreach (var cell in OccupiedCells())
            LevelGrid.Instance.RemoveUnitAtGridPosition(cell, this);
        actionPoints = 0;
        SetGuarding(false);
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
        if (damage > 0 && damageSource != null && ShieldCover != null && ShieldCover.CanIntercept(this))
        {
            var protector = ShieldCover.GetUnit();
            ShieldCover.ClearProtection();
            protector.healthSystem.Damage(Mathf.CeilToInt(damage * .5f), damageSource);
            return;
        }
        healthSystem.Damage(PreviewDamage(damage, damageSource != null), damageSource);
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
