using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnitManager : MonoBehaviour
{
    public enum BattleState { Initializing, Running, Won, Lost }
    public static UnitManager Instance { get; private set; }
    public event EventHandler OnBattleStateChanged;
    public event EventHandler OnUnitListChanged;
    public BattleState State { get; private set; } = BattleState.Initializing;
    public bool IsBattleRunning => State == BattleState.Running;
    private readonly List<Unit> unitList = new List<Unit>();
    private readonly List<Unit> friendlyUnitList = new List<Unit>();
    private readonly List<Unit> enemyUnitList = new List<Unit>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Unit.OnAnyUnitSpawned += Unit_OnAnyUnitSpawned;
        Unit.OnAnyUnitDead += Unit_OnAnyUnitDead;
    }

    private IEnumerator Start()
    {
        if (Instance != this) yield break;
        // Wait until all scene units have registered in Start.
        yield return null;
        SetState(BattleState.Running);
        CheckBattleEnd();
    }

    private void SetState(BattleState state)
    {
        State = state;
        OnBattleStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CheckBattleEnd()
    {
        BattleState? result = GetBattleResult();
        if (result.HasValue) FinishBattle(result.Value);
    }

    private BattleState? GetBattleResult()
    {
        if (!IsBattleRunning) return null;
        // Defeat takes precedence if both teams are eliminated together.
        if (friendlyUnitList.Count == 0) return BattleState.Lost;
        if (enemyUnitList.Count == 0) return BattleState.Won;
        return null;
    }

    private void LateUpdate()
    {
        CheckBattleEnd();
    }

    private void FinishBattle(BattleState result)
    {
        SetState(result); // Lock before starting the coroutine.
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        while (BaseAction.IsAnyActionActive) yield return null;
        yield return new WaitForSeconds(1f);
        if (SceneLoadManager.instance != null) SceneLoadManager.instance.LoadSceneBeforeBattle();
        else SceneManager.LoadScene(0);
    }

    private void Unit_OnAnyUnitSpawned(object sender, EventArgs e)
    {
        Unit unit = sender as Unit;
        if (unit == null || unitList.Contains(unit)) return;
        unitList.Add(unit);
        (unit.IsEnemy() ? enemyUnitList : friendlyUnitList).Add(unit);
        OnUnitListChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Unit_OnAnyUnitDead(object sender, EventArgs e)
    {
        Unit unit = sender as Unit;
        if (unit == null) return;
        unitList.Remove(unit);
        (unit.IsEnemy() ? enemyUnitList : friendlyUnitList).Remove(unit);
        OnUnitListChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy()
    {
        Unit.OnAnyUnitSpawned -= Unit_OnAnyUnitSpawned;
        Unit.OnAnyUnitDead -= Unit_OnAnyUnitDead;
        if (Instance == this) Instance = null;
    }

    public List<Unit> GetUnitList() => unitList;
    public List<Unit> GetFriendlyUnitList() => friendlyUnitList;
    public List<Unit> GetEnemyUnitList() => enemyUnitList;
}
