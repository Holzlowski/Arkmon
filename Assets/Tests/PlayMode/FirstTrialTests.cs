using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FirstTrialTests
{
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static void Reset() => T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
    [Test] public void AcceptedTrial_OffersGuideOnceAndResetsForNewSession()
    {
        Reset(); var opening=T("DemoOpening");
        opening.GetMethod("BeginTrial").Invoke(null,new object[]{"Another NPC"});
        Assert.That(opening.GetMethod("ConsumeTrialGuide").Invoke(null,null),Is.False);
        opening.GetMethod("BeginTrial").Invoke(null,new object[]{"John"});
        Assert.That(opening.GetProperty("TrialIntroductionSeen").GetValue(null),Is.True);
        Assert.That(opening.GetMethod("ConsumeTrialGuide").Invoke(null,null),Is.True);
        Assert.That(opening.GetMethod("ConsumeTrialGuide").Invoke(null,null),Is.False);
        opening.GetMethod("BeginTrial").Invoke(null,new object[]{"John"});
        Assert.That(opening.GetMethod("ConsumeTrialGuide").Invoke(null,null),Is.False);
        Reset(); Assert.That(opening.GetProperty("TrialIntroductionSeen").GetValue(null),Is.False);
    }
    [Test] public void Guide_FollowsActionsWithoutRequiringMovement()
    {
        var hint=T("FirstTrialGuide").GetMethod("Hint"); var state=T("UnitManager+BattleState");
        Assert.That(hint.Invoke(null,new[]{Enum.Parse(state,"Deploying"),(object)false,false,false,true}),Does.Contain("leuchtenden Felder"));
        Assert.That(hint.Invoke(null,new[]{Enum.Parse(state,"Running"),(object)false,true,false,true}),Does.Contain("Zug beenden"));
        Assert.That(hint.Invoke(null,new[]{Enum.Parse(state,"Running"),(object)false,false,true,false}),Does.Contain("Rammstoß"));
    }
    [UnityTest] public IEnumerator John_CancelledIntroductionRemainsAvailableAndPracticeUsesOriginalDialogue()
    {
        Reset(); yield return SceneManager.LoadSceneAsync("StartScene"); for(int i=0;i<4;i++)yield return null;
        var npc=(Component)UnityEngine.Object.FindObjectsByType(T("NPC"),FindObjectsSortMode.None).First(n=>n.GetType().GetField("npcName").GetValue(n).ToString()=="John");
        object dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
        npc.GetType().GetMethod("InteractWithPlayer").Invoke(npc,null);
        var temporary=T("DialogueBoxController").GetField("temporaryDialogue",BindingFlags.NonPublic|BindingFlags.Instance);
        Assert.That(temporary.GetValue(dialogue),Is.Not.Null);
        Assert.That(((Array)T("DialogueTree").GetField("sections").GetValue(temporary.GetValue(dialogue))).Length,Is.EqualTo(3));
        T("DialogueBoxController").GetMethod("CloseDialogue").Invoke(dialogue,null);
        Assert.That(T("DemoOpening").GetProperty("TrialIntroductionSeen").GetValue(null),Is.False);
        T("DemoOpening").GetMethod("BeginTrial").Invoke(null,new object[]{"John"});
        npc.GetType().GetMethod("InteractWithPlayer").Invoke(npc,null);
        Assert.That(temporary.GetValue(dialogue),Is.Null);
        T("DialogueBoxController").GetMethod("CloseDialogue").Invoke(dialogue,null); Reset();
    }
    [UnityTest] public IEnumerator AcceptingJohnDialogue_StartsBattleWithOptionalGuide()
    {
        Reset();yield return SceneManager.LoadSceneAsync("StartScene");for(int i=0;i<4;i++)yield return null;
        var npc=(Component)UnityEngine.Object.FindObjectsByType(T("NPC"),FindObjectsSortMode.None).First(n=>n.GetType().GetField("npcName").GetValue(n).ToString()=="John");
        object dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
        npc.GetType().GetMethod("InteractWithPlayer").Invoke(npc,null);
        var answers=T("DialogueBoxController").GetField("answerBox",BindingFlags.NonPublic|BindingFlags.Instance);
        float deadline=Time.realtimeSinceStartup+5;
        while(!((GameObject)answers.GetValue(dialogue)).activeSelf && Time.realtimeSinceStartup<deadline)
        {T("DialogueBoxController").GetMethod("SkipLine").Invoke(dialogue,null);yield return null;}
        Assert.That(((GameObject)answers.GetValue(dialogue)).activeSelf,Is.True);
        T("DialogueBoxController").GetMethod("AnswerQuestion").Invoke(dialogue,new object[]{0});yield return null;
        deadline=Time.realtimeSinceStartup+10;
        while(SceneManager.GetActiveScene().name!="BattleScene" && Time.realtimeSinceStartup<deadline)
        {if((UnityEngine.Object)dialogue!=null)T("DialogueBoxController").GetMethod("SkipLine").Invoke(dialogue,null);yield return null;}
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("BattleScene"));
        for(int i=0;i<5;i++)yield return null;
        Assert.That(UnityEngine.Object.FindFirstObjectByType(T("FirstTrialGuide")),Is.Not.Null);
        while((bool)T("SceneLoadManager").GetProperty("IsTransitioning").GetValue(null)&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(T("DemoOpening").GetProperty("TrialIntroductionSeen").GetValue(null),Is.True);
        Reset();
    }
}
