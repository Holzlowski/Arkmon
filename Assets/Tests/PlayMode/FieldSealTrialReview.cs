#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FieldSealTrialReview
{
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object I(string name) => T(name).GetProperty("Instance").GetValue(null);
    static object Get(object obj,string property) => obj.GetType().GetProperty(property).GetValue(obj);
    static object Call(object obj,string method,params object[] args) => obj.GetType().GetMethods()
        .Single(m=>m.Name==method && m.GetParameters().Length==args.Length).Invoke(obj,args);

    [UnityTest] public IEnumerator FourStartersDefendTheSeal()
    {
        UnityEditor.SessionState.SetString("Arkmon.StarterBattle","Assets/Resources/Field Seal Trial.asset");
        yield return SceneManager.LoadSceneAsync("BattleScene");
        for(int i=0;i<6;i++) yield return null;
        var deployment=I("BattleDeployment");
        var available=(IList)deployment.GetType().GetField("available",Hidden).GetValue(deployment);
        for(int i=0;i<4;i++)
        {
            Assert.That(Call(deployment,"TryPlace",available[0]),Is.True);
            float until=Time.realtimeSinceStartup+16;
            while((bool)Get(deployment,"IsSummoning") && Time.realtimeSinceStartup<until) yield return null;
            Assert.That(Get(deployment,"IsSummoning"),Is.False);
        }

        var manager=I("UnitManager");
        yield return null;
        yield return null;
        Assert.That(Get(manager,"IsBattleRunning"),Is.True);
        var friends=((IList)Call(manager,"GetFriendlyUnitList")).Cast<Component>().ToArray();
        var names=friends.Select(unit=>(Get(unit,"Definition") as UnityEngine.Object)?.name).Where(n=>n!=null).ToArray();
        Assert.That(names,Is.EquivalentTo(new[]{"Kafla","Panfrie","Liora","Rauk"}));
        Assert.That(((IList)Call(manager,"GetEnemyUnitList")).Count,Is.EqualTo(2));
        var mentor = (Transform)Get(deployment,"OpponentTrainer");
        Assert.That(mentor.name,Is.EqualTo("Arkanmeisterin"));
        Assert.That(mentor.GetComponentInChildren<SpriteRenderer>().sprite,Is.Not.Null);
        Assert.That(GameObject.Find("Opponent Trainer"),Is.Null);
        var seal=GameObject.Find("Arkanes Übungssiegel").GetComponent(T("HealthSystem"));
        Assert.That(Get(seal,"Health"),Is.EqualTo(120));
        Assert.That(Get(Call(I("UnitActionSystem"),"GetSelectedUnit"),"Definition"),Is.Not.Null);
        if(Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")!=null)
            typeof(GameIntroTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{"feldpruefung-in-unity"});

        var ai=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"));
        Assert.That(ai.isActiveAndEnabled,Is.True);
        var turns=I("TurnSystem");
        Call(turns,"NextTurn");
        float deadline=Time.realtimeSinceStartup+20;
        while(!(bool)Call(turns,"IsPlayerTurn") && Time.realtimeSinceStartup<deadline) yield return null;
        Assert.That(Call(turns,"GetTurnNumber"),Is.EqualTo(3),"The active enemy AI takes its turn and returns control.");
        Assert.That(Get(manager,"IsBattleRunning"),Is.True);

        if ((int)Get(seal,"Health") == 120)
        {
            Call(turns,"NextTurn");
            deadline=Time.realtimeSinceStartup+20;
            while(!(bool)Call(turns,"IsPlayerTurn") && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(Call(turns,"GetTurnNumber"),Is.EqualTo(5));
        }
        Assert.That((int)Get(seal,"Health"),Is.LessThan(120),"Distelwolle must threaten the seal with active AI.");
        ai.enabled=false;
        while((int)Call(turns,"GetTurnNumber")<6) { Call(turns,"NextTurn"); yield return null; }
        Assert.That(Get(manager,"IsBattleRunning"),Is.True,"The third opponent turn must finish.");
        Call(turns,"NextTurn");
        yield return null;
        Assert.That(Get(manager,"State").ToString(),Is.EqualTo("Won"));
    }
}
#endif
