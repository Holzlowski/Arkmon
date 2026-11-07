using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardCameraTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static object P(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static Component Rig=>(Component)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static void Place(Vector3 position)
    {
        var player=GameObject.FindWithTag("Player");
        float ground=0;
        foreach(var hit in Physics.RaycastAll(position+Vector3.up*20,Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground"))ground=Mathf.Max(ground,hit.point.y);
        position.y=ground+1;
        player.transform.position=player.GetComponent<Rigidbody>().position=position;Physics.SyncTransforms();
    }

    [UnityTest] public IEnumerator CloserExploration_ConversationFramesPair_AndReturnsSmoothly()
    {
        yield return Load();
        var rig=Rig;var view=Camera.main;
        Assert.That((float)P(rig,"CurrentDistance"),Is.EqualTo(19).Within(.01f));
        foreach(string name in new[]{"Courtyard Carpenter","Courtyard Stablekeeper","Courtyard Gardener","Courtyard Guard"})
        {
            var npc=GameObject.Find(name);Assert.That(npc,Is.Not.Null);
            Place(npc.transform.position+new Vector3(2.3f,0,-1.5f));
            yield return new WaitForSecondsRealtime(1.3f);
            var target=npc.GetComponent(T(name=="Courtyard Guard"?"CourtyardGuard":"CourtyardLife"));
            Vector3 before=view.transform.position;
            Call(target,"InteractWithPlayer");
            Assert.That(Vector3.Distance(before,view.transform.position),Is.LessThan(.01f),"Starting dialogue must not snap the camera.");
            yield return new WaitForSecondsRealtime(2.6f);
            Assert.That((float)P(rig,"ConversationBlend"),Is.GreaterThan(.97f));
            Assert.That((float)P(rig,"CurrentDistance"),Is.InRange(14,14.2f));
            Assert.That(P(rig,"ConversationPartner"),Is.SameAs(npc.transform));
            foreach(var point in new[]{npc.transform.position+Vector3.up,GameObject.FindWithTag("Player").transform.position})
            {
                var uv=view.WorldToViewportPoint(point);
                Assert.That(uv.z,Is.GreaterThan(0));Assert.That(uv.x,Is.InRange(.10f,.90f));
                Assert.That(uv.y,Is.InRange(.35f,.85f),"Both figures stay above the dialogue panel.");
            }
            Capture("courtyard-close-dialogue-"+name.Replace("Courtyard ",""));
            // Elian's reply must not switch the locked framing partner.
            T("CourtyardCamera").GetMethod("PresentLine",Hidden).Invoke(rig,new[]{(object)"Elian",Enum.Parse(T("DialogueEmotion"),"Neutral")});
            Assert.That(P(rig,"ConversationPartner"),Is.SameAs(npc.transform));
            before=view.transform.position;Call(Dialogue,"CloseDialogue");
            Assert.That(Vector3.Distance(before,view.transform.position),Is.LessThan(.01f));
            yield return new WaitForSecondsRealtime(3.6f);
            Assert.That((float)P(rig,"ConversationBlend"),Is.LessThan(.004f));
            Assert.That((float)P(rig,"CurrentDistance"),Is.EqualTo(19).Within(.02f));
            if(name=="Courtyard Gardener")Capture("courtyard-close-exploration-garden");
        }
        // The western limit follows Elian now instead of holding the view at x=-30.
        Place(new Vector3(-40,0,7));yield return new WaitForSecondsRealtime(1.5f);
        var playerView=view.WorldToViewportPoint(GameObject.FindWithTag("Player").transform.position);
        Assert.That(playerView.x,Is.InRange(.3f,.7f));
        Capture("courtyard-close-exploration-breach");
        var scenic=new Vector3(-61,11,7);var rotation=Quaternion.LookRotation(new Vector3(-69,0,20));
        Call(rig,"SetScenicView",scenic,rotation,58f);yield return null;yield return null;
        Assert.That(view.transform.position,Is.EqualTo(scenic));Assert.That(view.fieldOfView,Is.EqualTo(58));
        Call(rig,"ClearScenicView");Assert.That(view.fieldOfView,Is.EqualTo(48));
        yield return Load();
        var subscribers=(Delegate)T("DialogueBoxController").GetField("OnDialogueStarted",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        Assert.That(subscribers.GetInvocationList().Count(d=>d.Target!=null && d.Target.GetType().Name=="CourtyardCamera"),Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
