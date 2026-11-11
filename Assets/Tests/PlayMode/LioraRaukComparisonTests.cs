#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class LioraRaukComparisonTests
{
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static object I(string name) => T(name).GetProperty("Instance").GetValue(null);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target,args);
    static object Cell(int x,int z) => Activator.CreateInstance(T("GridPosition"),x,z);
    static int Health(Component unit)
    {
        var health = unit.GetComponent(T("HealthSystem"));
        return (int)health.GetType().GetProperty("Health").GetValue(health);
    }

    static void Place(Component unit,int x,int z)
        => Call(unit,"MoveToWorldPosition",Call(I("LevelGrid"),"GetWorldPosition",Cell(x,z)));

    static IEnumerator Use(Component unit,string command,int x,int z,int? remainingHealth=null)
    {
        var action = unit.GetComponent(T(command));
        var target = Cell(x,z);
        Assert.That(Call(action,"IsValidActionGridPosition",target),Is.True,command);
        Assert.That(Call(unit,"TrySpendActionPointsToTakeAction",action),Is.True,command);
        if (remainingHealth.HasValue) LogAssert.Expect(LogType.Log,remainingHealth.Value.ToString());
        bool finished = false;
        Call(action,"TakeAction",target,(Action)(() => finished = true));
        float deadline = Time.realtimeSinceStartup+6;
        while (!finished && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(finished,Is.True,command);
    }

    static void Capture(string name)
    {
        if (Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") != null)
            typeof(GameIntroTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{name});
    }

    [UnityTest] public IEnumerator LioraAndRaukCanWinWithTheirOwnCommands()
    {
        foreach (string starter in new[]{"Liora","Rauk"})
        {
            UnityEditor.SessionState.SetString("Arkmon.StarterBattle",$"Assets/Scriptable Objects/Battles/{starter} comparison.asset");
            yield return SceneManager.LoadSceneAsync("BattleScene");
            for (int i=0;i<5;i++) yield return null;
            ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled = false;
            var deployment = I("BattleDeployment");
            var cells = (IList)deployment.GetType().GetField("available",Hidden).GetValue(deployment);
            Assert.That(Call(deployment,"TryPlace",cells[0]),Is.True);
            var manager = I("UnitManager");
            var state = manager.GetType().GetProperty("State");
            float deadline = Time.realtimeSinceStartup+15;
            while (state.GetValue(manager).ToString() != "Running" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(state.GetValue(manager).ToString(),Is.EqualTo("Running"));
            yield return null;

            var player = (Component)((IList)Call(manager,"GetFriendlyUnitList"))[0];
            var enemy = (Component)((IList)Call(manager,"GetEnemyUnitList"))[0];
            Assert.That(((UnityEngine.Object)player.GetType().GetProperty("Definition").GetValue(player)).name,Is.EqualTo(starter));
            Assert.That(((UnityEngine.Object)enemy.GetType().GetProperty("Definition").GetValue(enemy)).name,Is.EqualTo("Distelwolle"));
            Assert.That(Health(player),Is.EqualTo(starter=="Liora" ? 95 : 85));
            Assert.That(Health(enemy),Is.EqualTo(50));
            var turns = I("TurnSystem");
            var ui = UnityEngine.Object.FindFirstObjectByType(T("UnitActionSystemUI"));
            Call(ui,"ShowCommandCategory",true);
            yield return null;
            Capture(starter.ToLowerInvariant()+"-gegen-distelwolle");

            if (starter == "Liora")
            {
                Place(player,2,2);
                Place(enemy,5,2);
                Physics.SyncTransforms();
                yield return Use(player,"ThornVineAction",5,2,40);
                Call(turns,"NextTurn");
                Assert.That(Call(enemy,"CanPerformAction",enemy.GetComponent(T("MoveAction"))),Is.False);
                Assert.That(Call(enemy,"CanPerformAction",enemy.GetComponent(T("RamAction"))),Is.False);
                LogAssert.Expect(LogType.Log,"75");
                Call(player,"Damage",20,enemy.transform);
                Call(turns,"NextTurn");
                Assert.That(Call(enemy,"CanPerformAction",enemy.GetComponent(T("MoveAction"))),Is.True);
                yield return Use(player,"RoseLightAction",2,2);
                Assert.That(Health(player),Is.EqualTo(93));

                Place(enemy,4,2);
                Physics.SyncTransforms();
                yield return Use(player,"RoseThrustAction",4,2,28);
                Call(turns,"NextTurn");
                Call(turns,"NextTurn");
                yield return Use(player,"RoseThrustAction",4,2,16);
                yield return Use(player,"RoseThrustAction",4,2,4);
                yield return Use(player,"RoseThrustAction",4,2,0);
            }
            else
            {
                Assert.That(GameObject.Find($"Shadow field {Cell(3,5)}"),Is.Not.Null);
                Assert.That(GameObject.Find($"Shadow field {Cell(2,2)}"),Is.Null);
                Place(player,3,5);
                var pathfinder = I("Pathfinding");
                var args = new object[]{Cell(3,5),Cell(5,5),true,0};
                var route = (IList)pathfinder.GetType().GetMethod("FindMovementPath",Hidden).Invoke(pathfinder,args);
                Assert.That(route.Count,Is.EqualTo(2),"Glide skips the obstacle cell.");
                Assert.That(args[3],Is.EqualTo(30),"The obstacle counts twice and landing costs one step.");
                yield return Use(player,"MoveAction",5,5);
                Assert.That(Call(player,"GetGridPosition"),Is.EqualTo(Cell(5,5)));
                Call(I("UnitActionSystem"),"SetSelectedAction",player.GetComponent(T("ShadowStepAction")));
                yield return null;
                Capture("rauk-schattenfelder");
                yield return Use(player,"ShadowStepAction",3,5);
                Assert.That(Call(player,"GetGridPosition"),Is.EqualTo(Cell(3,5)));
                Call(turns,"NextTurn");
                Call(turns,"NextTurn");

                Place(player,2,2);
                Place(enemy,6,2);
                Physics.SyncTransforms();
                yield return Use(player,"FeatherThrowAction",6,2,42);
                Place(enemy,3,2);
                enemy.transform.forward = Vector3.right;
                Physics.SyncTransforms();
                yield return Use(player,"DaggerStrikeAction",3,2,24);
                Assert.That(Vector3.Dot(enemy.transform.forward,Vector3.left),Is.GreaterThan(.99f));
                yield return Use(player,"DaggerStrikeAction",3,2,12);
                Call(turns,"NextTurn");
                Call(turns,"NextTurn");
                yield return Use(player,"DaggerStrikeAction",3,2,0);
            }
            yield return null;
            Assert.That(state.GetValue(manager).ToString(),Is.EqualTo("Won"));
        }
        LogAssert.NoUnexpectedReceived();
    }
}
#endif
