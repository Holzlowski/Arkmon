using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class DialogueEmotionTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static object P(object obj,string name)=>obj.GetType().GetProperty(name).GetValue(obj);
    static object F(object obj,string name)=>obj.GetType().GetField(name,Hidden).GetValue(obj);
    static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name).Invoke(obj,args);
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field).SetValue(obj,value);
    static void Close()=>Call(Dialogue,"CloseDialogue");
    static Component Marta=>GameObject.Find("Courtyard Gardener").GetComponent(T("CourtyardLife"));
    static Component Visual=>GameObject.Find("Courtyard Gardener").GetComponent(T("CourtyardGardenerVisual"));
    static Sprite Portrait=>(Sprite)P(Dialogue,"ActivePortrait");
    static void Capture(string name)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name});

    static IEnumerator Load()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.6f);
        Close();
        T("DialogueBoxController").GetField("charactersPerSecond",Hidden).SetValue(Dialogue,100000f);
        var player=GameObject.FindWithTag("Player");
        ((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;
        var body=player.GetComponent<Rigidbody>();
        body.interpolation=RigidbodyInterpolation.None;body.isKinematic=true;
        player.transform.position=new Vector3(3.2f,1,3.8f);body.position=player.transform.position;
        yield return new WaitForSeconds(.5f);
    }
    static IEnumerator Advance()
    {
        if((bool)F(Dialogue,"typing"))Call(Dialogue,"SkipLine");
        for(int i=0;i<20 && (bool)F(Dialogue,"typing");i++)yield return null;
        Assert.That((bool)F(Dialogue,"typing"),Is.False);
        Call(Dialogue,"SkipLine");yield return null;yield return null;
    }
    static void AssertMarta(string emotion)
    {
        Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo("Marta"));
        Assert.That(P(Dialogue,"ActiveEmotion").ToString(),Is.EqualTo(emotion));
        Assert.That(Portrait.name,Is.EqualTo("Marta-"+emotion.ToLowerInvariant()));
        Assert.That(P(Visual,"CurrentEmotion").ToString(),Is.EqualTo(emotion));
        var drawing=GameObject.Find("Marta illustrated gardener").transform;
        Assert.That(drawing.localScale.y,Is.InRange(.98f,1.02f),"Expression changes must preserve Marta's original height.");
    }

    [UnityTest] public IEnumerator Conversation_ChangesSpeakerAndAllFourExpressions_ThenClears()
    {
        yield return Load();
        Call(Marta,"InteractWithPlayer");
        AssertMarta("Friendly");yield return new WaitForSeconds(.32f);Capture("marta-dialogue-friendly");
        yield return Advance();
        Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo("Elian"));
        Assert.That(Portrait,Is.SameAs(T("PlayerEquipmentAppearance").GetProperty("Portrait").GetValue(null)));
        Assert.That(P(Visual,"CurrentEmotion").ToString(),Is.EqualTo("Neutral"));
        Capture("marta-dialogue-elian");
        yield return Advance();AssertMarta("Surprised");
        yield return new WaitForSeconds(.32f);Capture("marta-dialogue-surprised");
        yield return Advance();AssertMarta("Neutral");Capture("marta-dialogue-neutral");
        yield return Advance();AssertMarta("Worried");
        yield return new WaitForSeconds(.32f);Capture("marta-dialogue-worried");
        yield return Advance();AssertMarta("Friendly");
        yield return Advance();
        Assert.That((bool)P(Dialogue,"IsOpen"),Is.False);
        Assert.That(Portrait,Is.Null);
        Assert.That(P(Dialogue,"ActiveSpeaker"),Is.Empty);
        Assert.That(P(Visual,"CurrentEmotion").ToString(),Is.EqualTo("Neutral"));
        Call(Marta,"InteractWithPlayer");yield return Advance();yield return Advance();
        AssertMarta("Surprised");Close();yield return null;
        Assert.That(P(Visual,"CurrentEmotion").ToString(),Is.EqualTo("Neutral"));
        Assert.That(Portrait,Is.Null);

        // A scene reload must replace subscriptions, never keep the old resident alive.
        yield return Load();
        foreach(var pair in new[]{new[]{"DialogueBoxController","OnLinePresented"},new[]{"CourtyardGarden","OnCompleted"}})
        {
            var subscriptions=(Delegate)T(pair[0]).GetField(pair[1],BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            Assert.That(subscriptions.GetInvocationList().Count(d=>d.Target!=null && d.Target.GetType().Name=="CourtyardGardenerVisual"),Is.EqualTo(1));
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator GardenCompletion_CelebratesOnce_AndOffersPersonalThanks()
    {
        yield return Load();
        var evt=T("CourtyardGarden").GetEvent("OnCompleted");int completions=0;
        Action handler=()=>completions++;evt.AddEventHandler(null,handler);
        try
        {
            var can=GameObject.Find("Courtyard watering can").GetComponent(T("CourtyardGarden"));
            CourtyardGardenTaskTests.MoveTo(can);Call(can,"InteractWithPlayer");yield return CourtyardGardenTaskTests.WaitForPour();Close();
            var beds=UnityEngine.Object.FindObjectsByType(T("CourtyardGarden"),FindObjectsSortMode.None)
                .Cast<Component>().Where(c=>c.name=="Garden watering point").ToArray();
            foreach(var bed in beds){CourtyardGardenTaskTests.MoveTo(bed);Call(bed,"InteractWithPlayer");yield return CourtyardGardenTaskTests.WaitForPour();}
            Assert.That(completions,Is.EqualTo(1));
            Assert.That(P(Visual,"CurrentEmotion").ToString(),Is.EqualTo("Friendly"));
            var player=GameObject.FindWithTag("Player");
            player.transform.position=Marta.transform.position+new Vector3(-1,1,-1);
            player.GetComponent<Rigidbody>().position=player.transform.position;
            yield return new WaitForSeconds(.15f);
            var gardener=GameObject.Find("Marta illustrated gardener").GetComponent<SpriteRenderer>();
            Assert.That(gardener.flipX,Is.False,"Marta first admires the vegetables to her right, even with Elian on her left.");
            yield return new WaitForSeconds(.45f);
            Assert.That(gardener.flipX,Is.True,"After admiring the garden Marta turns back to Elian.");
            Call(beds[0],"InteractWithPlayer");Close();
            Assert.That(completions,Is.EqualTo(1),"Inspecting watered beds must not replay completion.");
            yield return new WaitForSeconds(.85f);
            Assert.That(P(Visual,"CurrentEmotion").ToString(),Is.EqualTo("Neutral"));
            Call(Marta,"InteractWithPlayer");AssertMarta("Surprised");
            yield return Advance();AssertMarta("Friendly");
            Assert.That(((TMPro.TMP_Text)F(Dialogue,"dialogueText")).text,Does.Contain("Vielen Dank"));
            yield return new WaitForSeconds(.32f);Capture("marta-dialogue-thanks");
            Close();
        }
        finally{evt.RemoveEventHandler(null,handler);}
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator LegacyBranchesAndPortraitFallbacks_RemainCompatible()
    {
        yield return Load();
        var style=T("PaperUIStyle").GetMethod("Load").Invoke(null,null);
        var john=(Sprite)T("PaperUIStyle").GetField("hunterPortrait").GetValue(style);
        var sections=Array.CreateInstance(T("DialogueSection"),2);
        var first=Activator.CreateInstance(T("DialogueSection"));
        Set(first,"dialogue",new[]{"Bereit?"});
        var answers=Array.CreateInstance(T("Answer"),1);var answer=Activator.CreateInstance(T("Answer"));
        Set(answer,"answerLabel","Ja");Set(answer,"nextElement",1);answers.SetValue(answer,0);
        var branch=Activator.CreateInstance(T("BranchPoint"));Set(branch,"question","Weiter?");Set(branch,"answers",answers);
        Set(first,"branchPoint",branch);sections.SetValue(first,0);
        var last=Activator.CreateInstance(T("DialogueSection"));Set(last,"dialogue",new[]{"Dann los."});Set(last,"endAfterDialogue",true);sections.SetValue(last,1);
        Call(Dialogue,"ShowConversation",sections,"John","",null);
        Assert.That(Portrait,Is.SameAs(john));yield return Advance();
        Assert.That(((GameObject)F(Dialogue,"answerBox")).activeSelf,Is.True);
        Assert.That(Portrait,Is.SameAs(john));
        Call(Dialogue,"AnswerQuestion",0);yield return null;yield return null;
        Assert.That(P(Dialogue,"ActiveEmotion").ToString(),Is.EqualTo("Neutral"));
        Assert.That(Portrait,Is.SameAs(john));yield return Advance();
        Assert.That((bool)P(Dialogue,"IsOpen"),Is.False);

        Call(Dialogue,"ShowMessage","Ein ruhiger Morgen.","Marta");
        Assert.That(Portrait.name,Is.EqualTo("Marta-neutral"));Close();
        Call(Dialogue,"ShowMessage","Guten Morgen.","Unbekannter Bewohner");
        Assert.That(Portrait,Is.Null);
        Assert.That(((Image)F(Dialogue,"portraitImage")).enabled,Is.False);Close();

        var set=ScriptableObject.CreateInstance(T("DialoguePortraitSet"));
        var tree=ScriptableObject.CreateInstance(T("DialogueTree"));
        try
        {
            Set(set,"neutral",john);
            var worried=Enum.Parse(T("DialogueEmotion"),"Worried");
            Assert.That(Call(set,"For",worried),Is.SameAs(john),"Missing expressions fall back to the same speaker's neutral portrait.");
            var terminal=Array.CreateInstance(T("DialogueSection"),1);terminal.SetValue(last,0);Set(tree,"sections",terminal);
            Call(Dialogue,"StartDialogue",tree,0,"Marta","",null,john);
            Assert.That(Portrait,Is.SameAs(john),"Explicit portraits on legacy callers retain precedence.");
            Close();
        }
        finally{UnityEngine.Object.Destroy(set);UnityEngine.Object.Destroy(tree);}
        LogAssert.NoUnexpectedReceived();
    }
}