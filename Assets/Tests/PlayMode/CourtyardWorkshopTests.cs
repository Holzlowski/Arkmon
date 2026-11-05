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
        var spareTimberSpace=Physics.OverlapBox(house.position+new Vector3(3.65f,.75f,.25f),
            new Vector3(.25f,.60f,.25f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
        Assert.That(spareTimberSpace.Select(c=>c.name),Is.Empty,"No invisible remnant of the longer timber rack.");
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
        yield return ReviewWorkshop();
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

    static IEnumerator ReviewWorkshop()
    {
        var focus=(Behaviour)GameObject.FindWithTag("Player").GetComponent(T("PlayerInteractionFocus"));
        bool showPrompt=focus.enabled;focus.enabled=false;
        var camera=Camera.main;
        var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        bool f=follow!=null && follow.enabled,b=brain!=null && brain.enabled;
        var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        if(follow!=null)follow.enabled=false;if(brain!=null)brain.enabled=false;
        camera.fieldOfView=40;camera.transform.rotation=Quaternion.Euler(25,0,0);
        camera.transform.position=GameObject.Find("Ida workshop").transform.position+
            new Vector3(0,1.35f,-2.3f)-camera.transform.forward*12;
        yield return null;yield return null;
        Capture("workshop-details-overview");
        var visual=(Behaviour)Visual;bool active=visual.enabled;visual.enabled=false;
        var paper=Ida.GetComponentInChildren<SpriteRenderer>();
        foreach(var frame in new[]{"WorkForward","WorkMiddle","WorkBack"})
        {
            paper.sprite=Resources.Load<Sprite>("Ida/"+frame);yield return null;
            Capture("workshop-details-"+frame);
        }
        visual.enabled=active;
        camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;
        if(follow!=null)follow.enabled=f;if(brain!=null)brain.enabled=b;focus.enabled=showPrompt;
    }

    [UnityTest] public IEnumerator WorkpieceObservation_IsReachable_PausesFirstLine_AndPreservesGardenProgress()
    {
        yield return Load();
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var point=GameObject.Find("Inspect Ida workpiece").GetComponent(T("CourtyardWorkshopWorkpiece"));
        var garden=T("CourtyardGarden");
        var count=garden.GetProperty("WateredCount").GetValue(null);
        var objective=garden.GetProperty("ObjectiveText").GetValue(null);
        player.transform.position=point.transform.position+new Vector3(0,1,-.5f);body.position=player.transform.position;
        Physics.SyncTransforms();yield return null;
        var blockers=Physics.OverlapBox(player.transform.position,new Vector3(.30f,.65f,.30f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)
            .Where(c=>!c.transform.IsChildOf(player.transform) && c.transform!=player.transform).Select(c=>c.name).ToArray();
        Assert.That(blockers,Is.Empty,"The workpiece must be reachable without entering furniture.");
        Assert.That(Call(player.GetComponent(T("PlayerInteractionFocus")),"Refresh"),Is.SameAs(point));
        Assert.That(P(point,"Label"),Is.EqualTo("Werkstück ansehen"));
        Call(point,"InteractWithPlayer");
        Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo("Elian"));
        Assert.That((bool)P(Visual,"IsCutting"),Is.False);
        yield return null;
        Assert.That((bool)P(Visual,"IsWorking"),Is.False,"Ida pauses before her own first reply.");
        Call(Dialogue,"CloseDialogue");yield return new WaitForSeconds(.6f);
        Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        Call(point,"InteractWithPlayer");
        foreach(string speaker in new[]{"Elian","Ida","Elian","Ida","Elian","Ida"})
        {
            yield return null;
            Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo(speaker));
            Assert.That((bool)P(Visual,"IsWorking"),Is.False);
            var portrait=P(Dialogue,"ActivePortrait");Assert.That(portrait,Is.Not.Null);
            Call(point,"InteractWithPlayer");
            Assert.That(P(Dialogue,"ActivePortrait"),Is.SameAs(portrait),"Repeated interaction must not restart dialogue.");
            if(speaker=="Ida")Capture("workshop-workpiece-dialogue");
            yield return Advance();
        }
        Assert.That((bool)P(Dialogue,"IsOpen"),Is.False);
        Assert.That(garden.GetProperty("WateredCount").GetValue(null),Is.EqualTo(count));
        Assert.That(garden.GetProperty("ObjectiveText").GetValue(null),Is.EqualTo(objective));
        player.transform.position+=Vector3.back*20;body.position=player.transform.position;
        Call(point,"InteractWithPlayer");Assert.That((bool)P(Dialogue,"IsOpen"),Is.False);
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardWorkshopWorkpiece"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator ThreeWorkAreas_UseGameplayCameraForReview()
    {
        yield return Load();
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var focus=(Behaviour)player.GetComponent(T("PlayerInteractionFocus"));focus.enabled=false;
        foreach(string area in new[]{"Ida workshop","Stable","Garden cottage"})
        {
            var house=GameObject.Find(area).transform;
            player.transform.position=house.position+new Vector3(0,1,-6.3f);body.position=player.transform.position;
            Physics.SyncTransforms();yield return new WaitForSeconds(1.3f);
            Assert.That(((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"))).enabled,Is.True);
            var follow=UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
            var configuredFov=(float)follow.GetType().GetField("fieldOfView",Hidden).GetValue(follow);
            Assert.That(Camera.main.fieldOfView,Is.EqualTo(configuredFov).Within(.1f));
            Capture("courtyard-review-"+area.Replace(" ","-").ToLowerInvariant());
        }
        focus.enabled=true;
        foreach(string name in new[]{"Tools","Repair","Offcuts","Planks","Shavings","Caddy"})
            Assert.That(Resources.Load<Sprite>("WorkshopDetails/"+name),Is.Not.Null);
        LogAssert.NoUnexpectedReceived();
    }
}
