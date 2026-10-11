using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RamActionTests
{
    private const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private Component attacker, target, ram;
    private object grid;
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).First(t => t != null);
    private static object Cell(int x, int z) => Activator.CreateInstance(TypeOf("GridPosition"), x, z);
    private static object Call(object obj, string method, params object[] args)
        => obj.GetType().GetMethod(method, Fields).Invoke(obj, args);
    private static bool Busy => (bool)TypeOf("BaseAction").GetProperty("IsAnyActionActive").GetValue(null);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene");
        for (int i = 0; i < 4; i++) yield return null;
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(TypeOf("EnemyAI"))).enabled = false;
        ((Behaviour)TypeOf("UnitManager").GetProperty("Instance").GetValue(null)).enabled = false;
        grid = TypeOf("LevelGrid").GetProperty("Instance").GetValue(null);
        attacker = ((IEnumerable)Call(TypeOf("UnitManager").GetProperty("Instance").GetValue(null),
            "GetEnemyUnitList")).Cast<Component>().First();
        ram = attacker.GetComponent(TypeOf("RamAction"));
        Assert.That(ram, Is.Not.Null);
        Assert.That(attacker.GetComponent(TypeOf("ShootAction")), Is.Null);
        Assert.That(attacker.GetComponent(TypeOf("SwordAction")), Is.Null);
        Assert.That(attacker.GetComponent(TypeOf("GrenadeAction")), Is.Null);
        Call(attacker, "MoveToWorldPosition", Call(grid, "GetWorldPosition", Cell(2, 2)));
        var targetObject = new GameObject("Ram test target");
        targetObject.SetActive(false);
        targetObject.AddComponent(TypeOf("HealthSystem"));
        target = targetObject.AddComponent(TypeOf("Unit"));
        Component move = targetObject.AddComponent(TypeOf("MoveAction"));
        Array basic = Array.CreateInstance(TypeOf("BaseAction"), 1);
        basic.SetValue(move, 0);
        target.GetType().GetField("basicActionArray", Fields).SetValue(target, basic);
        target.GetType().GetField("attacks", Fields).SetValue(target, Array.CreateInstance(TypeOf("BaseAction"), 0));
        targetObject.transform.position = (Vector3)Call(grid, "GetWorldPosition", Cell(5, 2));
        targetObject.SetActive(true);
        yield return null;
        object manager = TypeOf("UnitManager").GetProperty("Instance").GetValue(null);
        Call(manager, "SetState", Enum.Parse(TypeOf("UnitManager").GetNestedType("BattleState"), "Running"));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("RamCleanup"));
        yield return SceneManager.UnloadSceneAsync(previous);
        Assert.That(Busy, Is.False);
    }

    private IEnumerator WaitForAction()
    {
        float deadline = Time.realtimeSinceStartup + 4f;
        while ((bool)ram.GetType().GetProperty("IsActive").GetValue(ram)
            && Time.realtimeSinceStartup < deadline)
        {
            // No two living units may be registered on the same transit field.
            for (int x = 2; x <= 6; x++)
            {
                var units = (ICollection)Call(grid, "GetUnitListAtGridPosition", Cell(x, 2), attacker);
                Assert.That(units.Count, Is.LessThanOrEqualTo(1));
            }
            yield return null;
        }
        Assert.That(Busy, Is.False);
    }

    [UnityTest]
    public IEnumerator Ram_PushesTargetClaimsFieldSpendsDataCostAndCompletesOnce()
    {
        int completed = 0;
        Assert.That(Call(attacker, "TrySpendActionPointsToTakeAction", ram), Is.EqualTo(true));
        LogAssert.Expect(LogType.Log, "70");
        Call(ram, "TakeAction", Cell(5, 2), (Action)(() => completed++));
        Assert.That(Busy, Is.True);
        object turns = TypeOf("TurnSystem").GetProperty("Instance").GetValue(null);
        Assert.That(Call(turns, "CanChangeTurn"), Is.EqualTo(false));
        Call(turns, "NextTurn");
        Assert.That(Call(turns, "IsPlayerTurn"), Is.EqualTo(true));
        yield return WaitForAction();
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(Call(attacker, "GetActionPoints"), Is.EqualTo(1));
        Assert.That(Call(target, "GetHealthNormalized"), Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(Call(attacker, "GetGridPosition"), Is.EqualTo(Cell(5, 2)));
        Assert.That(Call(target, "GetGridPosition"), Is.EqualTo(Cell(6, 2)));
        Assert.That(attacker.transform.position, Is.EqualTo(Call(grid, "GetWorldPosition", Cell(5, 2))));
        Assert.That(target.transform.position, Is.EqualTo(Call(grid, "GetWorldPosition", Cell(6, 2))));
        Assert.That(Call(grid, "GetUnitOnGridPosition", Cell(5, 2)), Is.SameAs(attacker));
        Assert.That(Call(grid, "GetUnitOnGridPosition", Cell(6, 2)), Is.SameAs(target));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Ram_LethalHitStillClaimsTargetFieldAndReleasesBusy()
    {
        LogAssert.Expect(LogType.Log, "20");
        Call(target, "Damage", 80, null);
        int completed = 0;
        LogAssert.Expect(LogType.Log, "0");
        Call(ram, "TakeAction", Cell(5, 2), (Action)(() => completed++));
        yield return WaitForAction();
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(target == null, Is.True);
        Assert.That(Call(grid, "GetUnitOnGridPosition", Cell(5, 2)), Is.SameAs(attacker));
        Assert.That(Call(grid, "HasAnyUnitOnGridPosition", Cell(6, 2)), Is.EqualTo(false));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Ram_DisabledDuringPushSettlesBothUnitsAndCompletesOnce()
    {
        int completed = 0;
        LogAssert.Expect(LogType.Log, "70");
        Call(ram, "TakeAction", Cell(5, 2), (Action)(() => completed++));
        float deadline = Time.realtimeSinceStartup + 4f;
        while ((float)Call(target, "GetHealthNormalized") > 0.7f
            && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That((float)Call(target, "GetHealthNormalized"), Is.EqualTo(0.7f).Within(0.001f));
        ((Behaviour)ram).enabled = false;
        Assert.That(Busy, Is.False);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(Call(attacker, "GetGridPosition"), Is.EqualTo(Cell(5, 2)));
        Assert.That(Call(target, "GetGridPosition"), Is.EqualTo(Cell(6, 2)));
        yield return null;
        Assert.That(completed, Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Ram_ChangedPushObstacleBeforeImpactCancelsWithoutDamage()
    {
        int completed = 0;
        Call(ram, "TakeAction", Cell(5, 2), (Action)(() => completed++));
        object path = TypeOf("Pathfinding").GetProperty("Instance").GetValue(null);
        Call(path, "SetIsWalkableGridPosition", Cell(6, 2), false);
        yield return WaitForAction();
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(Call(attacker, "GetGridPosition"), Is.EqualTo(Cell(2, 2)));
        Assert.That(Call(target, "GetHealthNormalized"), Is.EqualTo(1f));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Ram_FastAnimationCannotOvershootFinalCells()
    {
        ram.GetType().GetField("chargeSpeed", Fields).SetValue(ram, 1000f);
        ram.GetType().GetField("pushSpeed", Fields).SetValue(ram, 1000f);
        LogAssert.Expect(LogType.Log, "70");
        Call(ram, "TakeAction", Cell(5, 2), (Action)(() => { }));
        yield return WaitForAction();
        Assert.That(attacker.transform.position, Is.EqualTo(Call(grid, "GetWorldPosition", Cell(5, 2))));
        Assert.That(target.transform.position, Is.EqualTo(Call(grid, "GetWorldPosition", Cell(6, 2))));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Ram_TargetDiesDuringChargeReturnsAttackerAndReleasesBusy()
    {
        int completed = 0;
        Vector3 origin = attacker.transform.position;
        Call(ram, "TakeAction", Cell(5, 2), (Action)(() => completed++));
        float deadline = Time.realtimeSinceStartup + 4f;
        while (attacker.transform.position == origin && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(attacker.transform.position, Is.Not.EqualTo(origin));
        LogAssert.Expect(LogType.Log, "0");
        Call(target, "Damage", 100, null);
        yield return WaitForAction();
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(attacker.transform.position, Is.EqualTo(origin));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Ram_EnemyTurnAISelectsRamInsteadOfMovingOrUsingTestAttacks()
    {
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(TypeOf("EnemyAI"))).enabled = true;
        LogAssert.Expect(LogType.Log, "Take Enemy AI Action");
        LogAssert.Expect(LogType.Log, "70");
        Call(TypeOf("TurnSystem").GetProperty("Instance").GetValue(null), "NextTurn");
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!(bool)ram.GetType().GetProperty("IsActive").GetValue(ram)
            && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That((bool)ram.GetType().GetProperty("IsActive").GetValue(ram), Is.True);
        yield return WaitForAction();
        Assert.That(Call(attacker, "GetActionPoints"), Is.EqualTo(1));
        Assert.That(Call(attacker, "GetGridPosition"), Is.EqualTo(Cell(5, 2)));
        Assert.That(Call(target, "GetGridPosition"), Is.EqualTo(Cell(6, 2)));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator DistelwolleDeath_RemovesRealPrefabWithoutLegacyRagdoll()
    {
        object cell = Call(attacker, "GetGridPosition");
        object manager = TypeOf("UnitManager").GetProperty("Instance").GetValue(null);
        int count = ((ICollection)Call(manager, "GetEnemyUnitList")).Count;
        int ragdolls = UnityEngine.Object.FindObjectsByType(TypeOf("UnitRagdoll"), FindObjectsSortMode.None).Length;
        // Simulate the legacy reference returning through an old scene/prefab override.
#if UNITY_EDITOR
        Component spawner = attacker.GetComponent(TypeOf("UnitRagdollSpawner"));
        GameObject legacy = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UnitRagdoll.prefab");
        if (legacy == null)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath("9ed608bb4658c144198bfe5a8ed136c2");
            legacy = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }
        Assert.That(legacy, Is.Not.Null);
        spawner.GetType().GetField("ragdollPrefab", Fields).SetValue(spawner, legacy.transform);
#endif
        LogAssert.Expect(LogType.Log, "0");
        Call(attacker, "Damage", 50, null);
        Assert.That((bool)attacker.GetType().GetProperty("IsDead").GetValue(attacker), Is.True);
        Assert.That(attacker.gameObject.activeSelf, Is.False);
        Assert.That(Call(grid, "HasAnyUnitOnGridPosition", cell), Is.EqualTo(false));
        Assert.That(((ICollection)Call(manager, "GetEnemyUnitList")).Count, Is.EqualTo(count - 1));
        Assert.That(Call(attacker, "TrySpendActionPointsToTakeAction", ram), Is.EqualTo(false));
        Assert.That(Call(attacker, "GetActionPoints"), Is.EqualTo(0));
        Assert.That(UnityEngine.Object.FindObjectsByType(TypeOf("UnitRagdoll"), FindObjectsSortMode.None).Length,
            Is.EqualTo(ragdolls));
        yield return null;
        Assert.That(attacker == null, Is.True);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator DistelwolleDeath_DuringMoveStopsPositionAndCompletesOnce()
    {
        Component move = attacker.GetComponent(TypeOf("MoveAction"));
        int completed = 0;
        Vector3 origin = attacker.transform.position;
        Call(move, "TakeAction", Cell(3, 2), (Action)(() => completed++));
        float deadline = Time.realtimeSinceStartup + 2f;
        while (attacker.transform.position == origin && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(attacker.transform.position, Is.Not.EqualTo(origin));
        Vector3 deathPosition = attacker.transform.position;
        object cell = Call(attacker, "GetGridPosition");
        LogAssert.Expect(LogType.Log, "0");
        Call(attacker, "Damage", 50, null);
        Assert.That(Busy, Is.False);
        Assert.That(completed, Is.EqualTo(1));
        Call(attacker, "MoveToWorldPosition", deathPosition + Vector3.right * 2f);
        Assert.That(attacker.transform.position, Is.EqualTo(deathPosition));
        Assert.That(Call(grid, "HasAnyUnitOnGridPosition", cell), Is.EqualTo(false));
        yield return null;
        Assert.That(attacker == null, Is.True);
        Assert.That(completed, Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator DistelwolleDeath_DuringChargeCancelsRamAndDoesNotDamageTarget()
    {
        int completed = 0;
        Vector3 origin = attacker.transform.position;
        Call(ram, "TakeAction", Cell(5, 2), (Action)(() => completed++));
        float deadline = Time.realtimeSinceStartup + 2f;
        while (attacker.transform.position == origin && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(attacker.transform.position, Is.Not.EqualTo(origin));
        LogAssert.Expect(LogType.Log, "0");
        Call(attacker, "Damage", 50, null);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(Busy, Is.False);
        yield return null;
        Assert.That(attacker == null, Is.True);
        Assert.That(Call(target, "GetHealthNormalized"), Is.EqualTo(1f));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator DistelwolleDeath_LastOpponentStillTriggersVictory()
    {
        object manager = TypeOf("UnitManager").GetProperty("Instance").GetValue(null);
        Component[] enemies = ((IEnumerable)Call(manager, "GetEnemyUnitList")).Cast<Component>().ToArray();
        ((Behaviour)manager).enabled = true;
        foreach (Component enemy in enemies)
        {
            LogAssert.Expect(LogType.Log, "0");
            Call(enemy, "Damage", 50, null);
        }
        yield return null;
        Assert.That(((ICollection)Call(manager, "GetEnemyUnitList")).Count, Is.EqualTo(0));
        Assert.That(manager.GetType().GetProperty("State").GetValue(manager).ToString(), Is.EqualTo("Won"));
        Assert.That(Busy, Is.False);
        yield return null;
        LogAssert.NoUnexpectedReceived();
    }
}
