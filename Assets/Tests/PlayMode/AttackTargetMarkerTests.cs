using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class AttackTargetMarkerTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object I(string n)=>T(n).GetProperty("Instance").GetValue(null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static object Cell(int x,int z)=>Activator.CreateInstance(T("GridPosition"),x,z);
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
    [UnityTest] public IEnumerator Preview_UsesActualFanCellsAndCorrectRamEndPositionAfterKillingHit()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene");for(int i=0;i<5;i++)yield return null;
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled=false;
        var deployment=I("BattleDeployment");var available=(IList)deployment.GetType().GetField("available",F).GetValue(deployment);
        Call(deployment,"TryPlace",available[0]);float deadline=Time.realtimeSinceStartup+12;
        while(!(bool)I("UnitManager").GetType().GetProperty("IsBattleRunning").GetValue(I("UnitManager"))&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
        var cat=((IEnumerable)Call(I("UnitManager"),"GetFriendlyUnitList")).Cast<Component>().First();
        var enemy=((IEnumerable)Call(I("UnitManager"),"GetEnemyUnitList")).Cast<Component>().First();
        foreach(Component prop in UnityEngine.Object.FindObjectsByType(T("BurningObstacle"),FindObjectsSortMode.None))prop.gameObject.SetActive(false);
        Physics.SyncTransforms();
        for(int x=0;x<11;x++)for(int z=0;z<10;z++)Call(I("Pathfinding"),"RefreshWalkability",Cell(x,z));
        Call(cat,"MoveToWorldPosition",Call(I("LevelGrid"),"GetWorldPosition",Cell(4,4)));
        Call(enemy,"MoveToWorldPosition",Call(I("LevelGrid"),"GetWorldPosition",Cell(5,4)));yield return null;
        var fan=cat.GetComponent(T("EmberAction"));var plan=T("AttackTargetMarkers").GetMethod("Plan");
        var marks=(IList)plan.Invoke(null,new[]{fan,Cell(5,5)});
        Assert.That(marks.Count,Is.EqualTo(3));
        var cells=(IList)Call(fan,"GetImpactGridPositionList",Cell(5,5));
        foreach(var mark in marks)Assert.That(cells.Contains(mark.GetType().GetField("cell").GetValue(mark)),Is.True);
        var ram=enemy.GetComponent(T("RamAction"));
        Call(cat,"MoveToWorldPosition",Call(I("LevelGrid"),"GetWorldPosition",Cell(7,4)));
        cat.GetComponent(T("HealthSystem")).GetType().GetField("health",F).SetValue(cat.GetComponent(T("HealthSystem")),1);
        marks=(IList)plan.Invoke(null,new[]{ram,Cell(7,4)});
        var attacker=marks.Cast<object>().Single(m=>m.GetType().GetField("role").GetValue(m).ToString()=="Attacker");
        Assert.That(attacker.GetType().GetField("cell").GetValue(attacker),Is.EqualTo(Cell(7,4)));
        Assert.That(marks.Cast<object>().Any(m=>m.GetType().GetField("role").GetValue(m).ToString()=="Pushed"),Is.False);
        var description=(string)T("AttackPreview").GetMethod("Describe").Invoke(null,new[]{ram,Cell(7,4)});
        Assert.That(description,Does.Contain("Gegner besiegt"));Assert.That(description,Does.Not.Contain("fällt um"));
        Scene old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("PreviewCleanup"));yield return SceneManager.UnloadSceneAsync(old);
    }
}
