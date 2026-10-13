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
        Call(obstacle, "Configure", Enum.Parse(T("BurningObstacle+MaterialKind"), "Vegetation"), 40, false, false);
        for (int i = 0; i < 3; i++) yield return null;
        object path = Instance("Pathfinding"); Assert.That(Call(path, "IsWalkableGridPosition", Cell(3, 2)), Is.EqualTo(false));
        yield return Attack("FlameSwordAction", Cell(3, 2));
        object audio = T("SoundManager").GetField("instance").GetValue(null);
        Assert.That(P(audio, "LastEffectName"), Is.EqualTo("fire-hit"));
        Assert.That(GameObject.Find("Flame sword impact"), Is.Not.Null);
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
    [UnityTest] public IEnumerator FlameSword_MovesTheSwordBothWaysAndHitsAtContact()
    {
        Set(rules, "monsterHitChance", 0f);
        Component animation = cat.GetComponent(T("PaperSwordAnimation"));
        SpriteRenderer body = cat.GetComponentInChildren<SpriteRenderer>();
        Transform mirror = (Transform)animation.GetType().GetField("mirrorRoot", Flags).GetValue(animation);
        Component action = cat.GetComponent(T("FlameSwordAction"));
        foreach (bool left in new[] { false, true })
        {
            Call(enemy, "MoveToWorldPosition", Call(grid, "GetWorldPosition", Cell(left ? 3 : 5, 2)));
            Set(enemy.GetComponent(T("HealthSystem")), "health", 50);
            yield return null; yield return null;
            Vector3 root = cat.transform.position;
            Vector3 rest = (Vector3)Call(animation, "GetTipPosition");
            int completed = 0;
            Assert.That(Call(cat, "TrySpendActionPointsToTakeAction", action), Is.EqualTo(true));
            LogAssert.Expect(LogType.Log, "25");
            Call(action, "TakeAction", Cell(left ? 3 : 5, 2), (Action)(() => completed++));
            yield return new WaitForSeconds(0.18f);
            Assert.That(Call(enemy, "GetHealthNormalized"), Is.EqualTo(1f), "No damage during windup.");
            Assert.That(P(animation, "IsSwinging"), Is.EqualTo(true));
            Assert.That(mirror.localScale.x, Is.EqualTo(left ? -1f : 1f));
            float motion = 0f, deadline = Time.realtimeSinceStartup + 3;
            while ((float)Call(enemy, "GetHealthNormalized") == 1f && Time.realtimeSinceStartup < deadline)
            {
                motion = Mathf.Max(motion, Vector3.Distance(rest, (Vector3)Call(animation, "GetTipPosition")));
                Assert.That(cat.transform.position, Is.EqualTo(root));
                Assert.That(Vector3.Dot(body.transform.up, Camera.main.transform.up), Is.GreaterThan(.999f));
                yield return null;
            }
            Assert.That(motion, Is.GreaterThan(.05f));
            Assert.That(Call(enemy, "GetHealthNormalized"), Is.EqualTo(.5f));
            Assert.That(P(animation, "IsSwinging"), Is.EqualTo(true));
            Assert.That(completed, Is.EqualTo(0), "Recovery still holds the action lock.");
            while ((bool)P(action, "IsActive") && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            Assert.That(completed, Is.EqualTo(1)); Assert.That(P(animation, "IsSwinging"), Is.EqualTo(false));
            Assert.That(Vector3.Distance(rest, (Vector3)Call(animation, "GetTipPosition")), Is.LessThan(.04f));
            FullRound();
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator FlameSword_ImpactTrailSamplesACurveWhenTheSwingSkipsRenderedFrames()
    {
        Component animation = cat.GetComponent(T("PaperSwordAnimation"));
        Call(animation, "BeginSwing", enemy.transform.position, true);
        // Jump straight from idle to contact, as on a slow frame.
        Call(animation, "MarkImpact");
        TrailRenderer trail = (TrailRenderer)animation.GetType().GetField("fireTrail", Flags).GetValue(animation);
        Assert.That(trail.positionCount, Is.GreaterThan(8));
        var points = new Vector3[trail.positionCount]; trail.GetPositions(points);
        Vector3 chord = points[points.Length - 1] - points[0];
        float curvature = points.Max(p => Vector3.Cross(p - points[0], chord.normalized).magnitude);
        Assert.That(curvature, Is.GreaterThan(.015f));
        Assert.That(trail.sharedMaterial.shader.name, Is.EqualTo("Arkmon/Soft Fire Trail"));
        Call(animation, "CancelSwing");
        Assert.That(trail.positionCount, Is.EqualTo(0));
        yield return null;
    }

    [UnityTest] public IEnumerator FlameSword_DeathCancelsTheSwingAndClearsItsFireTrail()
    {
        Component action = cat.GetComponent(T("FlameSwordAction"));
        Component animation = cat.GetComponent(T("PaperSwordAnimation"));
        int completed = 0; Call(action, "TakeAction", Cell(5, 2), (Action)(() => completed++));
        yield return new WaitForSeconds(.1f);
        Vector3 root = cat.transform.position;
        LogAssert.Expect(LogType.Log, "0"); Call(cat, "Damage", 100, null);
        Assert.That(completed, Is.EqualTo(1)); Assert.That(P(animation, "IsSwinging"), Is.EqualTo(false));
        TrailRenderer trail = (TrailRenderer)animation.GetType().GetField("fireTrail", Flags).GetValue(animation);
        Assert.That(trail.emitting, Is.False); Assert.That(trail.positionCount, Is.EqualTo(0));
        yield return null; Assert.That(cat.transform.position, Is.EqualTo(root));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator LethalBurnDamage_UsesRecallAndVictoryWaitsForBothMonsters()
    {
        // Explicit two-monster fixture: the introductory NPC encounter now uses only one.
        object configuration = deployment.GetType().GetField("configuration", Flags).GetValue(deployment);
        Component prefab = (Component)((Array)T("BattleConfiguration").GetField("enemyTeam").GetValue(configuration)).GetValue(0);
        UnityEngine.Object.Instantiate(prefab.gameObject, (Vector3)Call(grid, "GetWorldPosition", Cell(6, 2)), Quaternion.identity);
        yield return null;
        Component[] enemies = ((IEnumerable)Call(manager, "GetEnemyUnitList")).Cast<Component>().ToArray();
        Assert.That(enemies.Length, Is.EqualTo(2));
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
