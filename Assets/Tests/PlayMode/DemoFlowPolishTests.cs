using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class DemoFlowPolishTests
{
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object I(string n)=>T(n).GetProperty("Instance").GetValue(null);
    static object P(object obj,string n)=>obj.GetType().GetProperty(n).GetValue(obj);
    static object Call(object obj,string n,params object[] a)=>obj.GetType().GetMethod(n,F).Invoke(obj,a);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static bool Transitioning=>(bool)T("SceneLoadManager").GetProperty("IsTransitioning").GetValue(null);
    static bool Presenting=>(bool)T("DemoOpening").GetProperty("IsPresenting").GetValue(null);
    [UnityTest]public IEnumerator OpeningThroughFirstVictory_EndsDemoAndAllowsPractice()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        T("DemoCompletionPanel").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("TowerRoom");float deadline=Time.realtimeSinceStartup+7;
        while(!(bool)T("DemoOpening").GetProperty("HasAwakened").GetValue(null) && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(T("DemoOpening").GetProperty("HasAwakened").GetValue(null),Is.True);Call(Dialogue,"CloseDialogue");
        T("DemoOpening").GetMethod("TakeMantle").Invoke(null,null);deadline=Time.realtimeSinceStartup+5;
        while(Presenting && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(T("DemoOpening").GetProperty("HasMantle").GetValue(null),Is.True);Call(Dialogue,"CloseDialogue");
        T("DemoOpening").GetMethod("TakeLamp").Invoke(null,null);Call(Dialogue,"CloseDialogue");
        T("DemoOpening").GetMethod("LeaveRoom").Invoke(null,null);deadline=Time.realtimeSinceStartup+10;
        while((SceneManager.GetActiveScene().name!="StartScene" || Transitioning) && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("StartScene"));
        var player=GameObject.FindWithTag("Player");var entrance=GameObject.Find("Tower entrance").transform;
        Assert.That(player.transform.position.x,Is.EqualTo(entrance.position.x).Within(.1f));
        bool sawArrival=false;deadline=Time.realtimeSinceStartup+5;
        while(!(bool)P(Dialogue,"IsOpen") && Time.realtimeSinceStartup<deadline)
        {sawArrival|=(bool)T("CourtyardArrivalPresentation").GetProperty("IsPresenting").GetValue(null);yield return null;}
        Assert.That(sawArrival,Is.True);Assert.That((bool)P(Dialogue,"IsOpen"),Is.True);Assert.That(Presenting,Is.False);Call(Dialogue,"CloseDialogue");
        var john=(Component)UnityEngine.Object.FindObjectsByType(T("NPC"),FindObjectsSortMode.None).First(n=>n.GetType().GetField("npcName").GetValue(n).ToString()=="John");
        player.GetComponent<Rigidbody>().position=john.transform.position+new Vector3(-1.8f,1,0);Physics.SyncTransforms();Call(john,"InteractWithPlayer");
        deadline=Time.realtimeSinceStartup+10;
        while(SceneManager.GetActiveScene().name!="BattleScene" && Time.realtimeSinceStartup<deadline)
        {
            if(Dialogue!=null)
            {
                var answers=(GameObject)Dialogue.GetType().GetField("answerBox",F).GetValue(Dialogue);
                if(answers.activeSelf)Call(Dialogue,"AnswerQuestion",0);else Call(Dialogue,"SkipLine");
            }
            yield return null;
        }
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("BattleScene"));while(Transitioning && Time.realtimeSinceStartup<deadline)yield return null;
        for(int i=0;i<5;i++)yield return null;((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled=false;
        var deployment=I("BattleDeployment");var cells=(IList)deployment.GetType().GetField("available",F).GetValue(deployment);Call(deployment,"TryPlace",cells[0]);
        deadline=Time.realtimeSinceStartup+12;while(!(bool)P(I("UnitManager"),"IsBattleRunning") && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(P(I("UnitManager"),"IsBattleRunning"),Is.True);
        var cat=((IEnumerable)Call(I("UnitManager"),"GetFriendlyUnitList")).Cast<Component>().First();
        var move=cat.GetComponent(T("MoveAction"));
        var reason=T("ActionButtonUI").GetMethod("UnavailableReason");Assert.That(reason.Invoke(null,new[]{move}),Is.EqualTo(""));
        Call(cat,"TrySpendActionPointsToTakeAction",move);Call(cat,"TrySpendActionPointsToTakeAction",move);Call(cat,"TrySpendActionPointsToTakeAction",move);
        Assert.That(reason.Invoke(null,new[]{move}),Is.EqualTo("Zu wenig AP"));
        foreach(Component enemy in ((IEnumerable)Call(I("UnitManager"),"GetEnemyUnitList")).Cast<Component>().ToArray())
        {LogAssert.Expect(LogType.Log,"0");Call(enemy,"Damage",10000,null);}
        deadline=Time.realtimeSinceStartup+8;while(I("BattleResultPanel")==null && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(I("BattleResultPanel"),Is.Not.Null);Call(I("BattleResultPanel"),"Continue");
        deadline=Time.realtimeSinceStartup+8;while((SceneManager.GetActiveScene().name!="StartScene" || Transitioning) && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("StartScene"));Assert.That(T("DemoOpening").GetProperty("PassedFirstTrial").GetValue(null),Is.True);
        Assert.That(T("DemoCompletionPanel").GetProperty("IsOpen").GetValue(null),Is.False,"John's return dialogue comes first.");Call(Dialogue,"CloseDialogue");yield return null;yield return null;
        Assert.That(T("DemoCompletionPanel").GetProperty("IsOpen").GetValue(null),Is.True);Assert.That(Presenting,Is.True);
        var ending=(Component)UnityEngine.Object.FindFirstObjectByType(T("DemoCompletionPanel"));Call(ending,"Close");yield return null;
        john=(Component)UnityEngine.Object.FindFirstObjectByType(T("NPC"));Call(john,"InteractWithPlayer");
        Assert.That(Dialogue.GetType().GetField("temporaryDialogue",F).GetValue(Dialogue),Is.Null,"Practice reuses the normal conversation.");Call(Dialogue,"CloseDialogue");
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);LogAssert.NoUnexpectedReceived();
    }
}
