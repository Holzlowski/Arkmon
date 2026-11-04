using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardWorkshopTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static object P(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static Component Ida=>GameObject.Find("Courtyard Carpenter").GetComponent(T("CourtyardLife"));
    static Component Visual=>Ida.GetComponent(T("CourtyardCarpenterVisual"));
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static IEnumerator Load()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.7f);
        Call(Dialogue,"CloseDialogue");
        T("DialogueBoxController").GetField("charactersPerSecond",Hidden).SetValue(Dialogue,100000f);
        var player=GameObject.FindWithTag("Player");
        ((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;
        var body=player.GetComponent<Rigidbody>();body.interpolation=RigidbodyInterpolation.None;body.isKinematic=true;
        player.transform.position=GameObject.Find("Ida workshop").transform.position+new Vector3(0,1,-6.3f);body.position=player.transform.position;
        Physics.SyncTransforms();yield return new WaitForSeconds(.6f);
    }
    static IEnumerator Advance()
    {
        if((bool)T("DialogueBoxController").GetField("typing",Hidden).GetValue(Dialogue))Call(Dialogue,"SkipLine");
        yield return null;yield return null;
        Call(Dialogue,"SkipLine");yield return null;yield return null;
    }
    [UnityTest] public IEnumerator WorkbenchAndDoorAreAccessible_WorkPosesRemainAnchored()
    {
        yield return Load();
        var house=GameObject.Find("Ida workshop").transform;
        Assert.That(Vector3.Distance(Ida.transform.position,house.Find("Ida work spot").position),Is.LessThan(.01f));
        Assert.That(GameObject.Find("Workbench").transform.IsChildOf(house),Is.True);
        Assert.That(Physics.BoxCast(house.position+new Vector3(1.6f,1,-6.3f),new Vector3(.32f,.75f,.32f),Vector3.forward,
            out var blocked,Quaternion.identity,3.6f,~0,QueryTriggerInteraction.Ignore),Is.False,
            "Workshop door approach blocked by "+blocked.collider?.name);
        var origin=Ida.transform.position;var poses=new System.Collections.Generic.HashSet<Sprite>();
        for(int i=0;i<14;i++)
        {
            poses.Add(Ida.GetComponentInChildren<SpriteRenderer>().sprite);
            yield return new WaitForSeconds(.17f);
        }
        Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        Assert.That(poses.Count,Is.GreaterThanOrEqualTo(3),"The hand plane must move through distinct working poses.");
        Assert.That(Vector3.Distance(origin,Ida.transform.position),Is.LessThan(.001f));
        Capture("intro-courtyard-ida");
        var camera=Camera.main;var pos=camera.transform.position;var angle=camera.transform.rotation;
        camera.transform.position=house.position+new Vector3(0,7,-13.3f);
        camera.transform.LookAt(house.position+new Vector3(0,2.2f,-.8f));
        Capture("intro-courtyard-ida-detail");
        var paper=Ida.GetComponentInChildren<SpriteRenderer>();var savedSprite=paper.sprite;
        foreach(string frame in new[]{"WorkForward","WorkMiddle","WorkBack"})
        {
            paper.sprite=Resources.Load<Sprite>("Ida/"+frame);
            Capture("ida-planing-"+frame);
        }
        paper.sprite=savedSprite;
        camera.transform.SetPositionAndRotation(pos,angle);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator ConversationPausesWork_ChangesEmotions_AndResumesAfterClosing()
    {
        yield return Load();
        Call(Ida,"InteractWithPlayer");
        foreach(string emotion in new[]{"Friendly","Neutral","Worried","Elian","Surprised","Friendly"})
        {
            yield return null;
            Assert.That((bool)P(Visual,"IsWorking"),Is.False,"Work must stay paused during Elian's reply too.");
            Assert.That(Ida.GetComponentInChildren<SpriteRenderer>().sprite,Is.SameAs(Resources.Load<Sprite>("Ida/Idle")));
            Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo(emotion=="Elian"?"Elian":"Ida"));
            if(emotion!="Elian")
            {
                Assert.That(P(Dialogue,"ActiveEmotion").ToString(),Is.EqualTo(emotion));
                Assert.That(((Sprite)P(Dialogue,"ActivePortrait")).name,Is.EqualTo("Ida-"+emotion.ToLowerInvariant()));
            }
            if(emotion!="Elian"){yield return new WaitForSeconds(.3f);Capture("ida-dialogue-"+emotion.ToLowerInvariant());}
            yield return Advance();
        }
        Assert.That((bool)P(Dialogue,"IsOpen"),Is.False);
        yield return null;
        Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        Call(Ida,"InteractWithPlayer");Call(Dialogue,"CloseDialogue");yield return null;
        Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        yield return Load();
        foreach(string evt in new[]{"OnDialogueStarted","OnLinePresented","OnDialogueEnded"})
        {
            var subscriptions=(Delegate)T("DialogueBoxController").GetField(evt,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            Assert.That(subscriptions.GetInvocationList().Count(d=>d.Target!=null&&d.Target.GetType().Name=="CourtyardCarpenterVisual"),Is.EqualTo(1));
        }
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator PlaningSoundIsLocal_AndWorkSettlesAfterInterruption()
    {
        yield return Load();
        foreach(string name in new[]{"plane-stroke-1","plane-stroke-2"})
        {
            var clip=Resources.Load<AudioClip>("Audio/Workshop/"+name);
            Assert.That(clip,Is.Not.Null);
            Assert.That(clip.length,Is.InRange(.45f,.55f));
            Assert.That(clip.channels,Is.EqualTo(1));
        }
        var foley=Ida.GetComponent(T("StableLocalFoley"));
        Assert.That(foley,Is.Not.Null);
        Assert.That((float)P(foley,"Audibility"),Is.GreaterThan(.5f));
        var clock=Visual.GetType().GetField("workTime",Hidden);
        float deadline=Time.time+5;
        while(!(bool)P(Visual,"IsCutting") && Time.time<deadline)yield return null;
        Assert.That((bool)P(Visual,"IsCutting"),Is.True);
        Call(Ida,"InteractWithPlayer");
        Assert.That((bool)P(Visual,"IsCutting"),Is.False,"Speaking must stop the cut immediately.");
        yield return new WaitForSeconds(.2f);
        Assert.That((float)clock.GetValue(Visual),Is.Zero);
        Assert.That(Ida.GetComponents<AudioSource>().All(source=>!source.isPlaying),Is.True);
        Call(Dialogue,"CloseDialogue");
        yield return new WaitForSeconds(.2f);
        Assert.That((float)clock.GetValue(Visual),Is.Zero,"Resume only after a settling pause.");
        Assert.That((bool)P(Visual,"IsCutting"),Is.False);
        yield return new WaitForSeconds(.4f);
        Assert.That((float)clock.GetValue(Visual),Is.GreaterThan(0));
        Assert.That((bool)P(Visual,"IsCutting"),Is.True);
        ((Behaviour)Visual).enabled=false;
        Assert.That((bool)P(Visual,"IsCutting"),Is.False);
        Assert.That((bool)P(Visual,"IsWorking"),Is.False);
        ((Behaviour)Visual).enabled=true;
        yield return new WaitForSeconds(.6f);
        Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        Assert.That(Ida.GetComponents(T("StableLocalFoley")).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}