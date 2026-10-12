using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FireCombatTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private Component cat, enemy, system;
    private ScriptableObject rules;
    private object deployment, grid, turns, manager;
    private static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    private static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, Flags).Invoke(o, a);
    private static object P(object o, string n) => o.GetType().GetProperty(n).GetValue(o);
    private static object Instance(string n) => T(n).GetProperty("Instance").GetValue(null);
    private static object Cell(int x, int z) => Activator.CreateInstance(T("GridPosition"), x, z);
    private static void Set(object o, string n, object v) => o.GetType().GetField(n, Flags).SetValue(o, v);
    [UnitySetUp] public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene"); for (int i = 0; i < 4; i++) yield return null;
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled = false;
        manager = Instance("UnitManager"); ((Behaviour)manager).enabled = false;
        deployment = Instance("BattleDeployment"); grid = Instance("LevelGrid"); turns = Instance("TurnSystem");
        var cells = (IList)deployment.GetType().GetField("available", Flags).GetValue(deployment);
        Assert.That(Call(deployment, "TryPlace", cells[0]), Is.EqualTo(true));
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!(bool)P(deployment, "IsReady") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(P(deployment, "IsReady"), Is.EqualTo(true)); yield return null;
        cat = ((IEnumerable)Call(manager, "GetFriendlyUnitList")).Cast<Component>().First();
        enemy = ((IEnumerable)Call(manager, "GetEnemyUnitList")).Cast<Component>().First();
        Call(cat, "MoveToWorldPosition", Call(grid, "GetWorldPosition", Cell(4, 2)));
        Call(enemy, "MoveToWorldPosition", Call(grid, "GetWorldPosition", Cell(5, 2)));
        system = ((Component)turns).GetComponent(T("BurningSystem"));
        rules = ScriptableObject.CreateInstance(T("FireRules"));
        Set(rules, "monsterHitChance", 1f); Set(rules, "monsterSpreadChance", 0f); Set(rules, "vegetationSpreadChance", 0f);
        Set(system, "rules", rules);
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Scene previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(SceneManager.CreateScene("FireCleanup"));
        yield return SceneManager.UnloadSceneAsync(previous); UnityEngine.Object.Destroy(rules);
        Assert.That(T("BaseAction").GetProperty("IsAnyActionActive").GetValue(null), Is.EqualTo(false));
    }
    private IEnumerator Attack(string name, object cell)
    {
        Component action = cat.GetComponent(T(name)); int completed = 0;
        Assert.That(Call(cat, "TrySpendActionPointsToTakeAction", action), Is.EqualTo(true));
        Call(action, "TakeAction", cell, (Action)(() => completed++));
        float deadline = Time.realtimeSinceStartup + 3f;
        while ((bool)P(action, "IsActive") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(completed, Is.EqualTo(1));
    }
    private void FullRound() { Call(turns, "NextTurn"); Call(turns, "NextTurn"); }

    [UnityTest] public IEnumerator FlameSword_AlwaysIgnitesVegetationAndBurnoutReleasesItsField()
    {
        var root = new GameObject("Test burnable bush"); root.SetActive(false); root.layer = LayerMask.NameToLayer("Obstacles");
        root.transform.position = (Vector3)Call(grid, "GetWorldPosition", Cell(3, 2));
        root.AddComponent<BoxCollider>(); Component obstacle = root.AddComponent(T("BurningObstacle")); root.SetActive(true);
        for (int i = 0; i < 3; i++) yield return null;
        object path = Instance("Pathfinding"); Assert.That(Call(path, "IsWalkableGridPosition", Cell(3, 2)), Is.EqualTo(false));
        yield return Attack("FlameSwordAction", Cell(3, 2));
        Component status = root.GetComponent(T("BurningStatus"));
        Assert.That(P(status, "IsBurning"), Is.EqualTo(true)); Assert.That(P(obstacle, "Health"), Is.EqualTo(15));
        Assert.That(Call(cat, "GetActionPoints"), Is.EqualTo(1));
        FullRound(); Assert.That(P(obstacle, "Health"), Is.EqualTo(10));
        FullRound(); Assert.That(P(obstacle, "Health"), Is.EqualTo(5));
        FullRound(); yield return null;
        Assert.That(root == null, Is.True); Assert.That(Call(path, "IsWalkableGridPosition", Cell(3, 2)), Is.EqualTo(true));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator FlameSword_DamageBurnTicksAndCatImmunityUseTheActualDataAssets()
    {
        LogAssert.Expect(LogType.Log, "25"); yield return Attack("FlameSwordAction", Cell(5, 2));
        Component status = enemy.GetComponent(T("BurningStatus")); Assert.That(P(status, "RemainingRounds"), Is.EqualTo(3));
        Assert.That(enemy.transform.Find("Burning flames"), Is.Not.Null);
        LogAssert.Expect(LogType.Log, "20"); FullRound();
        LogAssert.Expect(LogType.Log, "15"); FullRound();
        LogAssert.Expect(LogType.Log, "10"); FullRound();
        Assert.That(P(status, "IsBurning"), Is.EqualTo(false));
        Assert.That(Call(enemy, "GetHealthNormalized"), Is.EqualTo(.2f).Within(.001f));
        Component catStatus = cat.GetComponent(T("BurningStatus"));
        Assert.That(Call(catStatus, "TryIgnite", 1f, 3, 5), Is.EqualTo(false));
        LogAssert.Expect(LogType.Log, "75"); Call(cat, "Damage", 25, null);
        Assert.That(Call(cat, "GetHealthNormalized"), Is.EqualTo(.75f).Within(.001f));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Claw_IsCheaperWeakerNonElementalAndReplacesTheTestKit()
    {
        Assert.That(cat.GetComponent(T("ShootAction")), Is.Null); Assert.That(cat.GetComponent(T("SwordAction")), Is.Null);
        Assert.That(cat.GetComponent(T("GrenadeAction")), Is.Null);
        LogAssert.Expect(LogType.Log, "35"); yield return Attack("ClawAction", Cell(5, 2));
        Assert.That(Call(cat, "GetActionPoints"), Is.EqualTo(2));
        Assert.That(P(enemy.GetComponent(T("BurningStatus")), "IsBurning"), Is.EqualTo(false));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator LethalBurnDamage_UsesRecallAndVictoryWaitsForBothMonsters()
    {
        Component[] enemies = ((IEnumerable)Call(manager, "GetEnemyUnitList")).Cast<Component>().ToArray();
        foreach (Component unit in enemies)
        {
            LogAssert.Expect(LogType.Log, "5"); Call(unit, "Damage", 45, null);
            Assert.That(Call(unit.GetComponent(T("BurningStatus")), "TryIgnite", 1f, 3, 5), Is.EqualTo(true));
        }
        ((Behaviour)manager).enabled = true;
        LogAssert.Expect(LogType.Log, "0"); LogAssert.Expect(LogType.Log, "0"); FullRound(); yield return null;
        Assert.That(P(manager, "State").ToString(), Is.EqualTo("Won")); Assert.That(P(deployment, "IsRecalling"), Is.EqualTo(true));
        Assert.That(enemies.All(u => u.gameObject.activeSelf), Is.True);
        Assert.That(enemies.All(u => !(bool)P(u.GetComponent(T("BurningStatus")), "IsBurning")), Is.True);
        float deadline = Time.realtimeSinceStartup + 4;
        while ((bool)P(deployment, "IsRecalling") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(P(deployment, "IsRecalling"), Is.EqualTo(false)); Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BattleScene"));
        LogAssert.NoUnexpectedReceived();
    }
}
