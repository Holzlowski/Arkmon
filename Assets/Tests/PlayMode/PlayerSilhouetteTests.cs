using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PlayerSilhouetteTests
{
    private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    private static void Capture(string name) => typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name});
    [UnityTest] public IEnumerator PaperHeadClearsWardrobeAndRoundWallsAndArmsAlternate()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("TowerRoom"); yield return new WaitForSeconds(.7f);
        var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
        dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        var player=GameObject.FindWithTag("Player");var rb=player.GetComponent<Rigidbody>();
        ((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;
        var wardrobe=GameObject.Find("Wardrobe").transform;
        rb.position=wardrobe.TransformPoint(new Vector3(0,1,-2.7f));rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        for(int i=0;i<65;i++){rb.linearVelocity=wardrobe.forward*5+Vector3.up*rb.linearVelocity.y;yield return new WaitForFixedUpdate();}
        rb.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.25f);
        float front=-wardrobe.GetComponent<BoxCollider>().size.z*.5f;
        foreach(var renderer in player.GetComponentsInChildren<SpriteRenderer>())
        {
            if(!renderer.enabled||renderer.forceRenderingOff||renderer.sprite==null)continue;
            foreach(var vertex in renderer.sprite.vertices)
                Assert.That(wardrobe.InverseTransformPoint(renderer.transform.TransformPoint(vertex)).z,Is.LessThan(front+.025f),"The visible paper silhouette stays in front of the wardrobe.");
        }
        Capture("player-wardrobe-clearance");
        rb.position=new Vector3(4.1f,1,3.6f);rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        Vector3 outward=new Vector3(4.1f,0,5.3f).normalized;
        for(int i=0;i<65;i++){rb.linearVelocity=outward*5+Vector3.up*rb.linearVelocity.y;yield return new WaitForFixedUpdate();}
        rb.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.25f);
        foreach(var renderer in player.GetComponentsInChildren<SpriteRenderer>())
        {
            if(!renderer.enabled||renderer.forceRenderingOff||renderer.sprite==null)continue;
            foreach(var vertex in renderer.sprite.vertices)
            {
                var point=renderer.transform.TransformPoint(vertex);
                Assert.That(new Vector2(point.x,point.z+1.7f).magnitude,Is.LessThan(7.84f),"Head and shoulders remain inside the curved masonry.");
            }
        }
        Capture("player-round-wall-clearance");
        rb.position=new Vector3(0,1,0);rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return null;
        var visual=player.GetComponent(T("PaperCharacterVisual"));
        var rig=visual.GetType().GetField("playerWalkRig",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(visual);
        var present=rig.GetType().GetMethod("Present");
        ((Behaviour)visual).enabled=false;
        foreach(bool family in new[]{false,true})
        {
            present.Invoke(rig,new object[]{true,family,false,Mathf.PI*.5f, true, 1f});
            var near=player.GetComponentsInChildren<Transform>().First(t=>t.name=="Near arm");
            var far=player.GetComponentsInChildren<Transform>().First(t=>t.name=="Far arm");
            float angle=Mathf.DeltaAngle(0,near.localEulerAngles.z);
            Assert.That(angle*Mathf.DeltaAngle(0,far.localEulerAngles.z),Is.LessThan(0));
            Capture(family?"elian-family-arm-step":"elian-room-arm-step");
            present.Invoke(rig,new object[]{true,family,true,Mathf.PI*1.5f, true, 1f});
            Assert.That(angle*Mathf.DeltaAngle(0,near.localEulerAngles.z),Is.LessThan(0));
            Assert.That(near.parent.localScale.x,Is.LessThan(0));
        }
        rig.GetType().GetMethod("Hide").Invoke(rig,null);
        LogAssert.NoUnexpectedReceived();
    }
}
