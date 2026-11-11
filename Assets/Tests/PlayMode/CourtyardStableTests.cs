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
        yield return ReviewCare();
        var camera=Camera.main;var pos=camera.transform.position;var angle=camera.transform.rotation;
        camera.transform.position=house.position+new Vector3(0,7,-13.3f);
        camera.transform.LookAt(house.position+new Vector3(0,2.2f,-.8f));
        Capture("intro-courtyard-hannes-detail");
        var paper=Hannes.GetComponentInChildren<SpriteRenderer>();var savedSprite=paper.sprite;
        foreach(string frame in new[]{"GroomForward","GroomMiddle","GroomBack"})
        {
            paper.sprite=Resources.Load<Sprite>("Hannes/"+frame);
            Capture("hannes-grooming-"+frame);
        }
        paper.sprite=savedSprite;
        camera.transform.SetPositionAndRotation(pos,angle);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator ConversationPausesWork_ChangesEmotions_AndResumesAfterClosing()
    {
        yield return Load();
        Call(Hannes,"InteractWithPlayer");
        foreach(string emotion in new[]{"Friendly","Neutral","Elian","Friendly","Elian","Friendly"})
        {
            yield return null;
            Assert.That((bool)P(Visual,"IsWorking"),Is.False,"Work must stay paused during Elian's reply too.");
            Assert.That(Hannes.GetComponentInChildren<SpriteRenderer>().sprite,Is.SameAs(Resources.Load<Sprite>("Hannes/GroomIdle")));
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

    [UnityTest] public IEnumerator HorsesStayAtTheirAnchors_AnimateWithPlantedFeet_AndSoundFadesWithPlayerDistance()
    {
        yield return Load();
        var stable=GameObject.Find("Stable").transform;
        var horses=stable.GetComponentsInChildren(T("CourtyardHorse"));
        Assert.That(horses.Length,Is.EqualTo(2));
        Assert.That(horses.Count(h=>(bool)P(h,"IsFeeding")),Is.EqualTo(1));
        var meshList=horses.Select(h=>h.GetComponentsInChildren<MeshFilter>()
            .First(f=>f.sharedMesh.name=="Stable horse breathing mesh").sharedMesh).ToArray();
        var anchors=horses.Select(h=>h.transform.position).ToArray();
        var before=meshList.Select(m=>m.vertices).ToArray();
        foreach(var horse in horses)
        {
            var foley=horse.GetComponent(T("StableLocalFoley"));
            Assert.That((float)P(foley,"Audibility"),Is.GreaterThan(.4f),"Sounds should be audible near the stalls despite the distant camera.");
        }
        yield return new WaitForSeconds(.55f);
        for(int h=0;h<horses.Length;h++)
        {
            Assert.That(Vector3.Distance(anchors[h],horses[h].transform.position),Is.LessThan(.001f));
            var after=meshList[h].vertices;
            Assert.That(after.Where((v,i)=>Vector3.Distance(v,before[h][i])>.001f).Any(),Is.True,"The horse should breathe without sprite-frame popping.");
            for(int i=0;i<after.Length;i++)
                if(before[h][i].y<.08f)
                    Assert.That(Vector3.Distance(before[h][i],after[i]),Is.LessThan(.0001f),"Hoof-level vertices must stay planted.");
        }
        // Exercise the rare hoof lift without waiting half a minute.
        var first=horses[0];var horseType=first.GetType();
        horseType.GetField("hoofAt",Hidden).SetValue(first,Time.time);
        horseType.GetField("hoofPending",Hidden).SetValue(first,true);
        yield return new WaitForSeconds(.2f);
        var lifted=meshList[0].vertices;
        Assert.That(lifted.Where((v,i)=>before[0][i].y<.08f && v.y-before[0][i].y>.005f).Any(),Is.True);
        yield return new WaitForSeconds(.4f);
        var settled=meshList[0].vertices;
        for(int i=0;i<settled.Length;i++)
            if(before[0][i].y<.08f)Assert.That(Vector3.Distance(before[0][i],settled[i]),Is.LessThan(.0001f));
        foreach(string resource in new[]{"horse-snort","saddle-brush","straw-rustle","hoof-shift"})
            Assert.That(Resources.Load<AudioClip>("Audio/Stable/"+resource),Is.Not.Null);
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        camera.transform.position=stable.position+new Vector3(0,6.6f,-12.8f);
        camera.transform.LookAt(stable.position+new Vector3(0,2,-.8f));
        Capture("stable-horses-detail");
        camera.transform.SetPositionAndRotation(position,rotation);
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        player.transform.position=stable.position+new Vector3(0,1,-24);body.position=player.transform.position;
        yield return null;yield return null;
        foreach(var horse in horses)
            Assert.That((float)P(horse.GetComponent(T("StableLocalFoley")),"Audibility"),Is.EqualTo(0));
        LogAssert.NoUnexpectedReceived();
    }

    static IEnumerator ReviewCare()
    {
        var focus=(Behaviour)GameObject.FindWithTag("Player").GetComponent(T("PlayerInteractionFocus"));
        bool showPrompt=focus.enabled;focus.enabled=false;
        var camera=Camera.main;
        var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        bool f=follow!=null && follow.enabled,b=brain!=null && brain.enabled;
        var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        if(follow!=null)follow.enabled=false;if(brain!=null)brain.enabled=false;
        var stable=GameObject.Find("Stable").transform;
        camera.fieldOfView=40;camera.transform.rotation=Quaternion.Euler(25,0,0);
        camera.transform.position=stable.position+new Vector3(.1f,1.35f,-2.3f)-camera.transform.forward*12;
        yield return null;yield return null;
        Capture("stable-care-overview");
        var visual=(Behaviour)Visual;bool active=visual.enabled;visual.enabled=false;
        var paper=Hannes.GetComponentInChildren<SpriteRenderer>();
        foreach(var frame in new[]{"GroomForward","GroomMiddle","GroomBack"})
        {
            paper.sprite=Resources.Load<Sprite>("Hannes/"+frame);yield return null;
            Capture("stable-care-"+frame);
        }
        visual.enabled=active;
        camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;
        if(follow!=null)follow.enabled=f;if(brain!=null)brain.enabled=b;focus.enabled=showPrompt;
    }

    [UnityTest] public IEnumerator HorseGreetings_AreReachable_PauseGrooming_AndCannotHijackDialogue()
    {
        yield return Load();
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var greetings=UnityEngine.Object.FindObjectsByType(T("CourtyardHorseGreeting"),FindObjectsSortMode.None).Cast<Component>().ToArray();
        Assert.That(greetings.Length,Is.EqualTo(2));
        foreach(var point in greetings)
        {
            player.transform.position=point.transform.position+new Vector3(0,1,-.6f);body.position=player.transform.position;
            Physics.SyncTransforms();yield return null;yield return null;
            var blockers=Physics.OverlapBox(player.transform.position,new Vector3(.30f,.65f,.30f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)
                .Where(c=>!c.transform.IsChildOf(player.transform) && c.transform!=player.transform).Select(c=>c.name).ToArray();
            Assert.That(blockers,Is.Empty,"Greeting approach is free.");
            var focus=player.GetComponent(T("PlayerInteractionFocus"));
            Assert.That(Call(focus,"Refresh"),Is.SameAs(point),"The visible prompt selects this horse.");
            Call(point,"InteractWithPlayer");yield return null;
            Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo("Elian"));
            Assert.That((bool)P(Visual,"IsWorking"),Is.False,"Hannes pauses during Elian's first line.");
            var other=greetings.First(g=>g!=point);Call(other,"InteractWithPlayer");
            Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo("Elian"));
            yield return Advance();
            Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo("Hannes"));
            Assert.That(((Sprite)P(Dialogue,"ActivePortrait")).name,Is.EqualTo("Hannes-friendly"));
            Capture(point.name.Contains("chestnut")?"stable-chestnut-greeting":"stable-dun-greeting");
            Call(Dialogue,"CloseDialogue");yield return new WaitForSeconds(.6f);
            Assert.That((bool)P(Visual,"IsWorking"),Is.True);
        }
        player.transform.position+=Vector3.back*20;body.position=player.transform.position;
        Call(greetings[0],"InteractWithPlayer");
        Assert.That((bool)P(Dialogue,"IsOpen"),Is.False,"Remote greetings are rejected.");
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardHorseGreeting"),FindObjectsSortMode.None).Length,Is.EqualTo(2));
        LogAssert.NoUnexpectedReceived();
    }
}
