using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardStableTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static object P(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static Component Hannes=>GameObject.Find("Courtyard Stablekeeper").GetComponent(T("CourtyardLife"));
    static Component Visual=>Hannes.GetComponent(T("CourtyardStablekeeperVisual"));
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
        player.transform.position=GameObject.Find("Stable").transform.position+new Vector3(0,1,-6.3f);body.position=player.transform.position;
        Physics.SyncTransforms();yield return new WaitForSeconds(.6f);
    }
    static IEnumerator Advance()
    {
        if((bool)T("DialogueBoxController").GetField("typing",Hidden).GetValue(Dialogue))Call(Dialogue,"SkipLine");
        yield return null;yield return null;
        Call(Dialogue,"SkipLine");yield return null;yield return null;
    }
    [UnityTest] public IEnumerator StableApproachAndTackRailRemainAccessible_WorkPosesRemainAnchored()
    {
        yield return Load();
        var house=GameObject.Find("Stable").transform;
        Assert.That(Vector3.Distance(Hannes.transform.position,house.Find("Hannes work spot").position),Is.LessThan(.01f));
        Assert.That(GameObject.Find("Stable tack rail").transform.IsChildOf(house),Is.True);
        Assert.That(Physics.BoxCast(house.position+new Vector3(.5f,1,-5),new Vector3(.32f,.75f,.32f),Vector3.forward,
            out var blocked,Quaternion.identity,2,~0,QueryTriggerInteraction.Ignore),Is.False,
            "Stable forecourt blocked by "+blocked.collider?.name);
        var origin=Hannes.transform.position;var poses=new System.Collections.Generic.HashSet<Sprite>();
        for(int i=0;i<14;i++)
        {
            poses.Add(Hannes.GetComponentInChildren<SpriteRenderer>().sprite);
            yield return new WaitForSeconds(.17f);
        }
        Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        Assert.That(poses.Count,Is.GreaterThanOrEqualTo(3),"The cleaning activity must use distinct working poses.");
        Assert.That(Vector3.Distance(origin,Hannes.transform.position),Is.LessThan(.001f));
        Capture("intro-courtyard-hannes");
        var camera=Camera.main;var pos=camera.transform.position;var angle=camera.transform.rotation;
        camera.transform.position=house.position+new Vector3(0,7,-13.3f);
        camera.transform.LookAt(house.position+new Vector3(0,2.2f,-.8f));
        Capture("intro-courtyard-hannes-detail");
        var paper=Hannes.GetComponentInChildren<SpriteRenderer>();var savedSprite=paper.sprite;
        foreach(string frame in new[]{"WorkForward","WorkMiddle","WorkBack"})
        {
            paper.sprite=Resources.Load<Sprite>("Hannes/"+frame);
            Capture("hannes-saddle-"+frame);
        }
        paper.sprite=savedSprite;
        camera.transform.SetPositionAndRotation(pos,angle);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator ConversationPausesWork_ChangesEmotions_AndResumesAfterClosing()
    {
        yield return Load();
        Call(Hannes,"InteractWithPlayer");
        foreach(string emotion in new[]{"Friendly","Neutral","Worried","Elian","Surprised","Friendly"})
        {
            yield return null;
            Assert.That((bool)P(Visual,"IsWorking"),Is.False,"Work must stay paused during Elian's reply too.");
            Assert.That(Hannes.GetComponentInChildren<SpriteRenderer>().sprite,Is.SameAs(Resources.Load<Sprite>("Hannes/Idle")));
            Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo(emotion=="Elian"?"Elian":"Hannes"));
            if(emotion!="Elian")
            {
                Assert.That(P(Dialogue,"ActiveEmotion").ToString(),Is.EqualTo(emotion));
                Assert.That(((Sprite)P(Dialogue,"ActivePortrait")).name,Is.EqualTo("Hannes-"+emotion.ToLowerInvariant()));
            }
            if(emotion!="Elian"){yield return new WaitForSeconds(.3f);Capture("hannes-dialogue-"+emotion.ToLowerInvariant());}
            yield return Advance();
        }
        Assert.That((bool)P(Dialogue,"IsOpen"),Is.False);
        yield return null;
        Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        Call(Hannes,"InteractWithPlayer");Call(Dialogue,"CloseDialogue");yield return null;
        Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        yield return Load();
        foreach(string evt in new[]{"OnDialogueStarted","OnLinePresented","OnDialogueEnded"})
        {
            var subscriptions=(Delegate)T("DialogueBoxController").GetField(evt,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            Assert.That(subscriptions.GetInvocationList().Count(d=>d.Target!=null&&d.Target.GetType().Name=="CourtyardStablekeeperVisual"),Is.EqualTo(1));
        }
        LogAssert.NoUnexpectedReceived();
    }
}