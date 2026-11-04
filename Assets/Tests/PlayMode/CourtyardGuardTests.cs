using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardGuardTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static object P(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static Component Guard=>GameObject.Find("Courtyard Guard").GetComponent(T("CourtyardGuard"));
    static GameObject Player=>GameObject.FindWithTag("Player");
    static void MovePlayer(Vector3 p){Player.transform.position=p;Player.GetComponent<Rigidbody>().position=p;Physics.SyncTransforms();}
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static IEnumerator Load()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.7f);
        Call(Dialogue,"CloseDialogue");
        T("DialogueBoxController").GetField("charactersPerSecond",Hidden).SetValue(Dialogue,100000f);
        ((Behaviour)Player.GetComponent(T("PlayerController"))).enabled=false;
        var body=Player.GetComponent<Rigidbody>();body.interpolation=RigidbodyInterpolation.None;body.isKinematic=true;
        MovePlayer(new Vector3(18,1,-8));yield return new WaitForSeconds(.2f);
        Assert.That(Guard,Is.Not.Null);Assert.That(Guard.GetComponentInChildren<SpriteRenderer>().sprite,Is.Not.Null);
    }
    static IEnumerator Advance()
    {
        if((bool)T("DialogueBoxController").GetField("typing",Hidden).GetValue(Dialogue))Call(Dialogue,"SkipLine");
        yield return null;yield return null;Call(Dialogue,"SkipLine");yield return null;yield return null;
    }
    [UnityTest] public IEnumerator CompletePatrolReturnsToCastle_WithoutCrossingProps()
    {
        yield return Load();
        var origin=Guard.transform.position;var angles=new HashSet<int>();var targets=new HashSet<int>();
        float until=Time.time+95,waiting=0;bool reachedGate=false,captured=false;
        float scale=Time.timeScale;Time.timeScale=4;
        try
        {
            while(Time.time<until)
            {
                targets.Add((int)P(Guard,"TargetIndex"));
                if((bool)P(Guard,"IsMoving")){var leg=Guard.transform.Find("Guard illustrated figure/Guard paper walking rig/Guard near leg");angles.Add(Mathf.RoundToInt(Mathf.DeltaAngle(0,leg.localEulerAngles.z)/5));}
                if((bool)P(Guard,"IsWaitingForObstacle"))waiting+=Time.deltaTime;else waiting=0;
                Assert.That(waiting,Is.LessThan(2),"Patrol blocked near "+Guard.transform.position+" / "+string.Join(", ",Physics.OverlapSphere(Guard.transform.position+Vector3.up,2).Where(c=>!c.isTrigger && !c.CompareTag("Ground")).Select(c=>c.name+" "+c.bounds)));
                Assert.That(Guard.transform.position.y,Is.GreaterThanOrEqualTo(0));
                foreach(var collider in Physics.OverlapCapsule(Guard.transform.position+Vector3.up*.45f,
                    Guard.transform.position+Vector3.up*1.7f,.28f,~0,QueryTriggerInteraction.Ignore))
                    Assert.That(collider.transform==Guard.transform || collider.CompareTag("Ground"),Is.True,
                        "Guard overlaps "+collider.name+" at "+Guard.transform.position);
                if(Guard.transform.position.x>38.8f)reachedGate=true;
                if(!captured && Guard.transform.position.x>20)
                {
                    MovePlayer(new Vector3(23,1,-3));
                    yield return new WaitForSeconds(1.2f);
                    Capture("intro-courtyard-guard-patrol");captured=true;
                    MovePlayer(new Vector3(18,1,-8));
                }
                if(reachedGate && Vector3.Distance(origin,Guard.transform.position)<.08f && !(bool)P(Guard,"IsMoving") && (int)P(Guard,"TargetIndex")==1)break;
                yield return null;
            }
            Assert.That(reachedGate,Is.True);Assert.That(Vector3.Distance(origin,Guard.transform.position),Is.LessThan(.08f));
            Assert.That(targets.Count,Is.EqualTo(8));Assert.That(angles.Count,Is.GreaterThanOrEqualTo(6));
            var atRest=Guard.GetComponent<Rigidbody>().position;yield return new WaitForSeconds(1);
            Assert.That(Vector3.Distance(atRest,Guard.GetComponent<Rigidbody>().position),Is.LessThan(.02f),"Castle watch pause missing.");
        }
        finally{Time.timeScale=scale;}
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator DialogueStopsPatrol_ShowsOwnPortraits_AndResumes()
    {
        yield return Load();
        MovePlayer(Guard.transform.position+new Vector3(-1.4f,1,-.6f));
        var focus=Player.GetComponent(T("PlayerInteractionFocus"));
        Assert.That(Call(focus,"Refresh"),Is.SameAs(Guard),"Guard must be reachable with the normal interaction selector.");
        Call(Guard,"InteractWithPlayer");var atRest=Guard.transform.position;
        string[] names={"Burgwache","Elian","Burgwache","Elian","Burgwache","Elian","Burgwache"};
        string[] moods={"Friendly","Neutral","Neutral","Worried","Friendly","Friendly","Friendly"};
        for(int i=0;i<names.Length;i++)
        {
            yield return new WaitForSeconds(.15f);
            Assert.That(P(Dialogue,"ActiveSpeaker"),Is.EqualTo(names[i]));
            Assert.That(P(Dialogue,"ActiveEmotion").ToString(),Is.EqualTo(moods[i]));
            Assert.That(Vector3.Distance(atRest,Guard.transform.position),Is.LessThan(.02f));
            Assert.That((bool)P(Guard,"IsMoving"),Is.False);
            Assert.That((bool)P(Guard,"FacingLeft"),Is.True);
            if(names[i]=="Burgwache")Assert.That(((Sprite)P(Dialogue,"ActivePortrait")).name,Is.EqualTo("Guard-"+moods[i].ToLowerInvariant()));
            else Assert.That(((Sprite)P(Dialogue,"ActivePortrait")).name,Does.Not.StartWith("Guard-"));
            if(i==0)Capture("intro-courtyard-guard-dialogue");
            yield return Advance();
        }
        Assert.That((bool)P(Dialogue,"IsOpen"),Is.False);
        MovePlayer(new Vector3(18,1,-8));
        yield return new WaitForSeconds(.3f);
        Assert.That(Vector3.Distance(atRest,Guard.transform.position),Is.LessThan(.02f));
        yield return new WaitForSeconds(5);
        Assert.That(Vector3.Distance(atRest,Guard.transform.position),Is.GreaterThan(.2f));
        Call(Guard,"InteractWithPlayer");Call(Dialogue,"CloseDialogue");
        yield return new WaitForSeconds(.9f);Assert.That((bool)P(Guard,"IsMoving"),Is.True);
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardGuard"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        foreach(string evt in new[]{"OnDialogueStarted","OnDialogueEnded","OnLinePresented"})
        {
            var subscriptions=(Delegate)T("DialogueBoxController").GetField(evt,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            Assert.That(subscriptions.GetInvocationList().Count(d=>d.Target is Component c && c.GetType()==T("CourtyardGuard")),Is.EqualTo(1));
        }
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator PlayerAndPropBlockersPauseMovement_ThenReleaseIt()
    {
        yield return Load();yield return new WaitForSeconds(4.5f);
        Assert.That((bool)P(Guard,"IsMoving"),Is.True);
        Vector3 position=Guard.GetComponent<Rigidbody>().position;
        var data=Resources.Load("Guard/Patrol");
        var stops=(Array)data.GetType().GetField("stops").GetValue(data);
        var nextStop=stops.GetValue((int)P(Guard,"TargetIndex"));
        Vector3 heading=(Vector3)nextStop.GetType().GetField("position").GetValue(nextStop)-position;heading.y=0;heading.Normalize();
        MovePlayer(position+heading+Vector3.up);
        yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        Assert.That((bool)P(Guard,"IsWaitingForObstacle"),Is.True);
        var hold=Guard.transform.position;float travelled=(float)P(Guard,"TravelDistance");
        yield return new WaitForSeconds(.5f);
        Assert.That(Vector3.Distance(hold,Guard.transform.position),Is.LessThan(.04f));
        Assert.That((float)P(Guard,"TravelDistance"),Is.EqualTo(travelled));
        Assert.That(Guard.GetComponentInChildren<SpriteRenderer>().sprite,Is.SameAs(Resources.Load<Sprite>("Guard/Idle")));
        MovePlayer(new Vector3(18,1,-8));
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="Guard test blocker";
        obstacle.transform.position=Guard.transform.position+heading*.8f+Vector3.up;
        obstacle.transform.localScale=new Vector3(.6f,2,.6f);Physics.SyncTransforms();
        yield return new WaitForSeconds(.6f);
        Assert.That((bool)P(Guard,"IsWaitingForObstacle"),Is.True);
        UnityEngine.Object.Destroy(obstacle);yield return new WaitForSeconds(.4f);
        Assert.That((bool)P(Guard,"IsMoving"),Is.True);
        Call(Dialogue,"ShowMessage","Ein kurzer Blick über den Hof.","Elian");
        var before=Guard.GetComponent<Rigidbody>().position;yield return new WaitForSeconds(.5f);
        Assert.That(Vector3.Distance(before,Guard.GetComponent<Rigidbody>().position),Is.LessThan(.02f));
        Call(Dialogue,"CloseDialogue");yield return new WaitForSeconds(.9f);
        Assert.That((bool)P(Guard,"IsMoving"),Is.True);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale=1;
        Scene previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("GuardCleanup"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}