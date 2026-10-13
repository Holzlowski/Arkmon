using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TacticalCombatTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private Component cat, enemy;
    private object grid, turns;
    private ScriptableObject rules;
    private static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    private static object I(string n) => T(n).GetProperty("Instance").GetValue(null);
    private static object P(object o, string n) => o.GetType().GetProperty(n).GetValue(o);
    private static object F(object o, string n) => o.GetType().GetField(n, Flags).GetValue(o);
    private static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, Flags).Invoke(o, a);
    private static object Cell(int x, int z) => Activator.CreateInstance(T("GridPosition"), x, z);
    private object Plan(int x, int z, int damage) => T("CoverSystem").GetMethod("Preview").Invoke(null, new[] { Cell(x, z), Call(enemy, "GetGridPosition"), (object)damage, enemy });
    private void Apply(int damage, bool fire = false) => T("CoverSystem").GetMethod("Apply").Invoke(null,
        new[] { Call(cat, "GetGridPosition"), enemy, (object)damage, cat.transform, fire });
    private Component Cover(int x, int z, int health, string material = "Stone", bool high = false)
    {
        var root = new GameObject("Fixture cover"); root.layer = LayerMask.NameToLayer("Obstacles");
        root.transform.position = (Vector3)Call(grid, "GetWorldPosition", Cell(x, z));
        var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up * .65f; collider.size = new Vector3(1.6f, 1.3f, .7f);
        Component obstacle = root.AddComponent(T("BurningObstacle"));
        Call(obstacle, "Configure", Enum.Parse(T("BurningObstacle+MaterialKind"), material), health, true, high);
        return obstacle;
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene"); for (int i = 0; i < 5; i++) yield return null;
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled = false;
        object deployment = I("BattleDeployment"), manager = I("UnitManager"); grid = I("LevelGrid"); turns = I("TurnSystem");
        var available = (IList)F(deployment, "available"); Assert.That(Call(deployment, "TryPlace", available[0]), Is.EqualTo(true));
        float deadline = Time.realtimeSinceStartup + 12;
        while (!(bool)P(manager, "IsBattleRunning") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(P(manager, "IsBattleRunning"), Is.EqualTo(true));
        cat = ((IEnumerable)Call(manager, "GetFriendlyUnitList")).Cast<Component>().First();
        enemy = ((IEnumerable)Call(manager, "GetEnemyUnitList")).Cast<Component>().First();
        foreach (Component obstacle in UnityEngine.Object.FindObjectsByType(T("BurningObstacle"), FindObjectsSortMode.None)) obstacle.gameObject.SetActive(false);
        Physics.SyncTransforms();
        for (int x = 0; x < 11; x++) for (int z = 0; z < 10; z++) Call(I("Pathfinding"), "RefreshWalkability", Cell(x, z));
        Call(cat, "MoveToWorldPosition", Call(grid, "GetWorldPosition", Cell(2, 3)));
        Call(enemy, "MoveToWorldPosition", Call(grid, "GetWorldPosition", Cell(5, 3)));
        Component system = ((Component)turns).GetComponent(T("BurningSystem")); rules = ScriptableObject.CreateInstance(T("FireRules"));
        rules.GetType().GetField("monsterHitChance").SetValue(rules, 0f); system.GetType().GetField("rules", Flags).SetValue(system, rules);
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Scene old = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(SceneManager.CreateScene("TacticalCleanup"));
        yield return SceneManager.UnloadSceneAsync(old); UnityEngine.Object.Destroy(rules);
        Assert.That(T("BaseAction").GetProperty("IsAnyActionActive").GetValue(null), Is.EqualTo(false));
    }
    [UnityTest] public IEnumerator Cover_AbsorbsFromFrontAndFlankingBypassesIt()
    {
        Component cover = Cover(4, 3, 45); yield return null; yield return null; Physics.SyncTransforms();
        object plan = Plan(2, 3, 10);
        Assert.That(F(plan, "unitDamage"), Is.EqualTo(5)); Assert.That(F(plan, "coverDamage"), Is.EqualTo(5));
        Assert.That(F(Plan(5, 1, 10), "unitDamage"), Is.EqualTo(10));
        LogAssert.Expect(LogType.Log, "45"); Apply(10);
        Assert.That(P(cover, "Health"), Is.EqualTo(40)); Assert.That(P(enemy.GetComponent(T("HealthSystem")), "Health"), Is.EqualTo(45));
    }
    [UnityTest] public IEnumerator WeakCover_OnlyAbsorbsRemainingHealthAndOpensItsCell()
    {
        Component cover = Cover(4, 3, 2); yield return null; yield return null; Physics.SyncTransforms();
        Assert.That(F(Plan(2, 3, 10), "unitDamage"), Is.EqualTo(8));
        LogAssert.Expect(LogType.Log, "42"); Apply(10); yield return null;
        Assert.That(cover == null, Is.True); Assert.That(Call(I("Pathfinding"), "IsWalkableGridPosition", Cell(4, 3)), Is.EqualTo(true));
        Assert.That(F(Plan(2, 3, 10), "unitDamage"), Is.EqualTo(10));
    }
    [UnityTest] public IEnumerator TallObstacle_BlocksShotRatherThanReducingDamage()
    {
        Component cover = Cover(4, 3, 60, "Vegetation", true); yield return null; yield return null; Physics.SyncTransforms();
        Assert.That(F(Plan(2, 3, 18), "blocked"), Is.EqualTo(true));
        Call(cat, "MoveToWorldPosition", Call(grid, "GetWorldPosition", Cell(3, 3)));
        Component action = cat.GetComponent(T("EmberAction"));
        Assert.That(((IList)Call(action, "GetImpactGridPositionList", Cell(5, 3))).Contains(Cell(5, 3)), Is.False);
        Assert.That(Call(action, "IsValidActionGridPosition", Cell(4, 3)), Is.EqualTo(true), "The blocker itself can be attacked.");
        Assert.That(P(cover, "Health"), Is.EqualTo(60));
    }
    [UnityTest] public IEnumerator ActualEmberAction_MatchesPreviewSpendsTwoAPAndCannotIgniteStone()
    {
        Component cover = Cover(5, 3, 45); yield return null; yield return null; Physics.SyncTransforms();
        Call(cat, "MoveToWorldPosition", Call(grid, "GetWorldPosition", Cell(4, 3)));
        Component action = cat.GetComponent(T("EmberAction"));
        string preview = (string)T("AttackPreview").GetMethod("Describe").Invoke(null, new[] { action, Cell(5, 3) });
        Assert.That(preview, Does.Contain("−18 LP")); Assert.That(preview, Does.Contain("2 AP"));
        Assert.That(Call(cat, "TrySpendActionPointsToTakeAction", action), Is.EqualTo(true));
        int completed = 0; LogAssert.Expect(LogType.Log, "32");
        Call(action, "TakeAction", Cell(5, 3), (Action)(() => completed++));
        float deadline = Time.realtimeSinceStartup + 3;
        while ((bool)P(action, "IsActive") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(completed, Is.EqualTo(1)); Assert.That(Call(cat, "GetActionPoints"), Is.EqualTo(1));
        Assert.That(P(cover, "Health"), Is.EqualTo(27)); Assert.That(P(cover.GetComponent(T("BurningStatus")), "CanBurn"), Is.EqualTo(false));
    }
    [UnityTest] public IEnumerator FireShot_IgnitesWoodCoverAndBurnDamageKeepsReducingItsHealth()
    {
        Component cover = Cover(4, 3, 30, "Wood"); yield return null; yield return null; Physics.SyncTransforms();
        LogAssert.Expect(LogType.Log, "41"); Apply(18, true);
        Component burning = cover.GetComponent(T("BurningStatus"));
        Assert.That(P(burning, "IsBurning"), Is.EqualTo(true)); Assert.That(P(cover, "Health"), Is.EqualTo(21));
        Call(burning, "Tick"); Assert.That(P(cover, "Health"), Is.EqualTo(16));
    }
    [UnityTest] public IEnumerator Ember_RowHasThreeCellsAndAllowsAnEmptyCentre()
    {
        Component action=cat.GetComponent(T("EmberAction"));
        var horizontal=(IList)Call(action,"GetAffectedGridPositionList",Cell(3,3));
        Assert.That(horizontal.Cast<object>(),Is.EquivalentTo(new[]{Cell(3,2),Cell(3,3),Cell(3,4)}));
        var vertical=(IList)Call(action,"GetAffectedGridPositionList",Cell(2,4));
        Assert.That(vertical.Cast<object>(),Is.EquivalentTo(new[]{Cell(1,4),Cell(2,4),Cell(3,4)}));
        foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1})
        {
            var corner=(IList)Call(action,"GetAffectedGridPositionList",Cell(2+x,3+z));
            Assert.That(corner.Cast<object>(),Is.EquivalentTo(new[]{Cell(2+x,3),Cell(2+x,3+z),Cell(2,3+z)}));
            Assert.That(Call(action,"IsValidActionGridPosition",Cell(2+x,3+z)),Is.EqualTo(true));
        }
        Assert.That(Call(action,"IsValidActionGridPosition",Cell(3,3)),Is.EqualTo(true));
        Assert.That(Call(action,"IsValidActionGridPosition",Cell(5,3)),Is.EqualTo(false));
        Assert.That(((IList)Call(action,"GetAffectedGridPositionList",Cell(5,3))).Count,Is.EqualTo(0));
        Call(cat,"MoveToWorldPosition",Call(grid,"GetWorldPosition",Cell(0,0)));
        Assert.That(Call(action,"IsValidActionGridPosition",Cell(1,0)),Is.EqualTo(false));
        yield return null;
    }
    [UnityTest] public IEnumerator Ember_DamagesAllOccupantsAndIgnitesSurvivingWood()
    {
        Call(cat,"MoveToWorldPosition",Call(grid,"GetWorldPosition",Cell(4,3)));
        // One occupied cell contains two objects; each must be hit exactly once.
        Component wood=Cover(5,4,40,"Wood");Component stone=Cover(5,2,45);
        Component sharedCellWood=Cover(5,3,40,"Wood");
        Component secondSharedWood=Cover(5,3,40,"Wood");
        yield return null;yield return null;Physics.SyncTransforms();
        Component action=cat.GetComponent(T("EmberAction"));
        string preview=(string)T("AttackPreview").GetMethod("Describe").Invoke(null,new[]{action,Cell(5,3)});
        Assert.That(preview,Does.Contain("Entzündet (100 %)"));
        int completed=0;LogAssert.Expect(LogType.Log,"32");
        Call(action,"TakeAction",Cell(5,3),(Action)(()=>completed++));
        yield return new WaitForSeconds(1.6f);
        Assert.That(completed,Is.EqualTo(1));Assert.That(P(wood,"Health"),Is.EqualTo(22));
        Assert.That(P(stone,"Health"),Is.EqualTo(27));
        Assert.That(P(sharedCellWood,"Health"),Is.EqualTo(22));Assert.That(P(secondSharedWood,"Health"),Is.EqualTo(22));
        Assert.That(P(wood.GetComponent(T("BurningStatus")),"IsBurning"),Is.EqualTo(true));
        Assert.That(P(stone.GetComponent(T("BurningStatus")),"IsBurning"),Is.EqualTo(false));
    }
    [UnityTest] public IEnumerator Ember_DeathBeforeReleaseRestoresBodyAndPreventsDamage()
    {
        Call(cat,"MoveToWorldPosition",Call(grid,"GetWorldPosition",Cell(4,3)));
        Component action=cat.GetComponent(T("EmberAction"));Component rig=cat.GetComponent(T("PaperSwordAnimation"));
        var body=(SpriteRenderer)F(rig,"bodyRenderer");Sprite original=body.sprite;
        int completed=0;Call(action,"TakeAction",Cell(5,3),(Action)(()=>completed++));
        yield return new WaitForSeconds(.3f);
        Assert.That(P(rig,"IsScraping"),Is.EqualTo(true));Assert.That(body.sprite,Is.Not.EqualTo(original));
        Assert.That(Vector3.Dot(body.transform.up,Camera.main.transform.up),Is.GreaterThan(.999f));
        LogAssert.Expect(LogType.Log,"0");Call(cat,"Damage",100,null);
        yield return null;
        Assert.That(completed,Is.EqualTo(1));Assert.That(P(rig,"IsScraping"),Is.EqualTo(false));Assert.That(body.sprite,Is.EqualTo(original));
        yield return new WaitForSeconds(1.2f);
        Assert.That(P(enemy.GetComponent(T("HealthSystem")),"Health"),Is.EqualTo(50));
    }
    [UnityTest] public IEnumerator Ember_HitsAlliedMonstersAndWarnsBeforeFiring()
    {
        Call(cat,"MoveToWorldPosition",Call(grid,"GetWorldPosition",Cell(4,3)));
        GameObject clone=UnityEngine.Object.Instantiate(enemy.gameObject,(Vector3)Call(grid,"GetWorldPosition",Cell(5,4)),Quaternion.identity);
        Component ally=clone.GetComponent(T("Unit"));ally.GetType().GetField("isEnemy",Flags).SetValue(ally,false);
        rules.GetType().GetField("monsterHitChance").SetValue(rules,1f);
        yield return null;yield return null;
        Component action=cat.GetComponent(T("EmberAction"));
        string preview=(string)T("AttackPreview").GetMethod("Describe").Invoke(null,new[]{action,Cell(5,3)});
        Assert.That(preview,Does.Contain("Verbündete werden getroffen"));
        Assert.That(F(Call(action,"GetEnemyAIAction",Cell(5,3)),"actionValue"),Is.LessThan(0));
        int completed=0;LogAssert.Expect(LogType.Log,"32");LogAssert.Expect(LogType.Log,"32");
        Call(action,"TakeAction",Cell(5,3),(Action)(()=>completed++));yield return new WaitForSeconds(1.6f);
        Assert.That(completed,Is.EqualTo(1));
        Assert.That(P(ally.GetComponent(T("HealthSystem")),"Health"),Is.EqualTo(32));
        Assert.That(P(ally.GetComponent(T("BurningStatus")),"IsBurning"),Is.EqualTo(true));
        Assert.That(P(enemy.GetComponent(T("BurningStatus")),"IsBurning"),Is.EqualTo(true));
    }
    [UnityTest] public IEnumerator Ember_ScrapeMovesAlongBladeAndMirrorsWithoutTiltingBody()
    {
        Component rig=cat.GetComponent(T("PaperSwordAnimation"));var body=(SpriteRenderer)F(rig,"bodyRenderer");
        Sprite original=body.sprite;
        foreach(bool left in new[]{false,true})
        {
            Call(enemy,"MoveToWorldPosition",Call(grid,"GetWorldPosition",Cell(left?0:5,3)));
            yield return null;
            Call(rig,"BeginSparkScrape",enemy.transform.position);
            yield return new WaitForSeconds(.25f);
            var hand=(SpriteRenderer)F(rig,"clawHand");Vector3 start=hand.transform.localPosition;
            yield return new WaitForSeconds(.25f);
            Assert.That(Vector3.Distance(start,hand.transform.localPosition),Is.GreaterThan(.08f));
            Assert.That(((Transform)F(rig,"mirrorRoot")).localScale.x,Is.EqualTo(left?-1f:1f));
            Assert.That(Vector3.Dot(body.transform.up,Camera.main.transform.up),Is.GreaterThan(.999f));
            Call(rig,"MarkSparkRelease");Assert.That(((ParticleSystem)F(rig,"scrapeSparks")).particleCount,Is.GreaterThan(0));
            Call(rig,"CancelSwing");Assert.That(body.sprite,Is.EqualTo(original));Assert.That(hand.gameObject.activeSelf,Is.False);
        }
    }
    [UnityTest] public IEnumerator Guard_CostsOneAPStacksAfterCoverAndDoesNotReduceBurning()
    {
        Cover(4, 3, 45); yield return null; yield return null; Physics.SyncTransforms();
        Component guard = enemy.GetComponent(T("GuardAction"));
        Assert.That(Call(enemy, "TrySpendActionPointsToTakeAction", guard), Is.EqualTo(true));
        Call(guard, "TakeAction", Cell(5, 3), (Action)(() => {}));
        Assert.That(Call(enemy, "GetActionPoints"), Is.EqualTo(2)); Assert.That(P(enemy, "IsGuarding"), Is.EqualTo(true));
        Assert.That(F(Plan(2, 3, 10), "unitDamage"), Is.EqualTo(3));
        LogAssert.Expect(LogType.Log, "47"); Apply(10);
        Component burning = enemy.GetComponent(T("BurningStatus")); Call(burning, "TryIgnite", 1f, 3, 5);
        LogAssert.Expect(LogType.Log, "42"); Call(burning, "Tick");
        Call(turns, "NextTurn"); Assert.That(P(enemy, "IsGuarding"), Is.EqualTo(false));
        Call(enemy, "SetGuarding", true); Call(enemy, "SetKnockedDown", true);
        Assert.That(P(enemy, "IsGuarding"), Is.EqualTo(false));
    }
    [UnityTest] public IEnumerator CoverHealth_IsHiddenUntilDamageAndHidesAgainAfterFeedback()
    {
        Component cover=Cover(4,3,45);
        Component display=cover.gameObject.AddComponent(T("ObstacleConditionVisual"));
        yield return null;yield return null;
        Assert.That(P(display,"IsHealthVisible"),Is.EqualTo(false));
        Call(cover,"Damage",5);yield return null;
        Assert.That(P(display,"IsHealthVisible"),Is.EqualTo(true));
        yield return new WaitForSeconds(2.65f);
        Assert.That(P(display,"IsHealthVisible"),Is.EqualTo(false));
        Assert.That(P(cover,"Health"),Is.EqualTo(40));
    }
}
