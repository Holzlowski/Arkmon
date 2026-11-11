#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class StarterComparisonTests
{
    const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static object I(string name) => T(name).GetProperty("Instance").GetValue(null);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    static object Cell(int x, int z) => Activator.CreateInstance(T("GridPosition"), x, z);
    static object Health(Component unit) => unit.GetComponent(T("HealthSystem"));
    static int LP(Component unit) => (int)Health(unit).GetType().GetProperty("Health").GetValue(Health(unit));

    [UnityTest]
    public IEnumerator SameArenaAndOpponentWithBothStarters()
    {
        object enemyStart = null;
        object playerStart = null;
        int enemyHealth = 0;
        foreach (var starter in new[] { "Kafla", "Panfrie" })
        {
            var config = starter == "Kafla" ? "Default NPC Battle" : "Panfrie comparison";
            UnityEditor.SessionState.SetString("Arkmon.StarterBattle", $"Assets/Scriptable Objects/Battles/{config}.asset");
            yield return SceneManager.LoadSceneAsync("BattleScene");
            for (int i = 0; i < 5; i++) yield return null;
            ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled = false;
            var deployment = I("BattleDeployment");
            var cells = (IList)deployment.GetType().GetField("available", PrivateInstance).GetValue(deployment);
            Assert.That(Call(deployment, "TryPlace", cells[0]), Is.True);
            float deadline = Time.realtimeSinceStartup + 15;
            var manager = I("UnitManager");
            while (!(bool)manager.GetType().GetProperty("IsBattleRunning").GetValue(manager)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(manager.GetType().GetProperty("IsBattleRunning").GetValue(manager), Is.True);
            yield return null;

            var player = (Component)((IList)Call(manager, "GetFriendlyUnitList"))[0];
            var enemy = (Component)((IList)Call(manager, "GetEnemyUnitList"))[0];
            Assert.That(player.GetType().GetProperty("Definition").GetValue(player), Is.InstanceOf<UnityEngine.Object>());
            Assert.That(((UnityEngine.Object)player.GetType().GetProperty("Definition").GetValue(player)).name, Is.EqualTo(starter));
            Assert.That(((UnityEngine.Object)enemy.GetType().GetProperty("Definition").GetValue(enemy)).name, Is.EqualTo("Distelwolle"));
            Assert.That(Call(player, "GetActionPoints"), Is.EqualTo(3));
            if (starter == "Kafla")
            {
                enemyStart = Call(enemy, "GetGridPosition");
                playerStart = Call(player, "GetGridPosition");
                enemyHealth = LP(enemy);
                continue;
            }

            Assert.That(Call(enemy, "GetGridPosition"), Is.EqualTo(enemyStart));
            Assert.That(Call(player, "GetGridPosition"), Is.EqualTo(playerStart));
            Assert.That(LP(enemy), Is.EqualTo(enemyHealth));
            Assert.That(LP(player), Is.EqualTo(115));

            var ui = UnityEngine.Object.FindFirstObjectByType(T("UnitActionSystemUI"));
            Call(ui, "ShowCommandCategory", true);
            yield return null;
            if (Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") != null)
                typeof(GameIntroTests).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { "panfrie-gegen-distelwolle" });

            void Place(Component unit, int x, int z) => Call(unit, "MoveToWorldPosition", Call(I("LevelGrid"), "GetWorldPosition", Cell(x, z)));
            Place(player, 2, 2);
            Place(enemy, 5, 2);
            Physics.SyncTransforms();
            yield return null;
            var move = player.GetComponent(T("MoveAction"));
            var volley = player.GetComponent(T("IceVolleyAction"));
            var watch = player.GetComponent(T("ShieldWatchAction"));
            Assert.That(move.GetType().GetProperty("MaxMoveDistance", PrivateInstance).GetValue(move), Is.EqualTo(2));
            Assert.That(Call(move, "IsValidActionGridPosition", Cell(4, 2)), Is.True);
            Assert.That(Call(move, "IsValidActionGridPosition", Cell(5, 2)), Is.False);
            Assert.That(Call(volley, "GetActionPointCost"), Is.EqualTo(2));

            IEnumerator Use(Component action, object cell)
            {
                Assert.That(Call(action, "IsValidActionGridPosition", cell), Is.True, action.GetType().Name);
                Assert.That(Call(player, "TrySpendActionPointsToTakeAction", action), Is.True);
                bool complete = false;
                Call(action, "TakeAction", cell, (Action)(() => complete = true));
                float end = Time.realtimeSinceStartup + 4;
                while (!complete && Time.realtimeSinceStartup < end) yield return null;
                Assert.That(complete, Is.True);
            }

            LogAssert.Expect(LogType.Log, (enemyHealth - 16).ToString());
            yield return Use(volley, Cell(5, 2));
            Assert.That(LP(enemy), Is.EqualTo(enemyHealth - 16));
            Assert.That(enemy.GetType().GetProperty("IsChilled").GetValue(enemy), Is.True);
            Assert.That(volley.GetType().GetProperty("IsRecharging").GetValue(volley), Is.True);
            yield return Use(watch, Cell(2, 2));
            yield return null;
            Assert.That(player.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sprite != null && r.sprite.name.Contains("Deckung")), Is.True);

            var turns = I("TurnSystem");
            Call(turns, "NextTurn");
            var enemyMove = enemy.GetComponent(T("MoveAction"));
            Assert.That(enemyMove.GetType().GetProperty("MaxMoveDistance", PrivateInstance).GetValue(enemyMove),
                Is.EqualTo(3));
            LogAssert.Expect(LogType.Log, "108");
            Call(player, "Damage", 13, enemy.transform);
            Assert.That(LP(player), Is.EqualTo(108));
            LogAssert.Expect(LogType.Log, "95");
            Call(player, "Damage", 13, enemy.transform);
            Assert.That(LP(player), Is.EqualTo(95));
            Call(turns, "NextTurn");
            Assert.That(enemy.GetType().GetProperty("IsChilled").GetValue(enemy), Is.False);
            Assert.That(Call(player, "CanPerformAction", volley), Is.False);

            Place(enemy, 3, 2);
            Physics.SyncTransforms();
            yield return null;
            LogAssert.Expect(LogType.Log, (enemyHealth - 28).ToString());
            yield return Use(player.GetComponent(T("ShieldBashAction")), Cell(3, 2));
            Assert.That(LP(enemy), Is.EqualTo(enemyHealth - 28));
            Call(turns, "NextTurn");
            Call(turns, "NextTurn");
            Assert.That(Call(player, "CanPerformAction", volley), Is.True);

            var allyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Monster.prefab");
            var allyPosition = (Vector3)Call(I("LevelGrid"), "GetWorldPosition", Cell(2, 3));
            var ally = UnityEngine.Object.Instantiate(allyPrefab, allyPosition, Quaternion.identity).GetComponent(T("Unit"));
            yield return null;
            int allyHealth = LP(ally);
            yield return Use(watch, Cell(2, 3));
            LogAssert.Expect(LogType.Log, "90");
            Call(ally, "Damage", 9, enemy.transform);
            Assert.That(LP(ally), Is.EqualTo(allyHealth));
            Assert.That(LP(player), Is.EqualTo(90));
        }
        LogAssert.NoUnexpectedReceived();
    }
}
#endif
