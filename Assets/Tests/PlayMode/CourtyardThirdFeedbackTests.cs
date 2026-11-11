using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardThirdFeedbackTests
{
    const BindingFlags H=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object S(string t,string p)=>T(t).GetProperty(p).GetValue(null);
    static object Call(object o,string m,params object[] a)=>o.GetType().GetMethod(m).Invoke(o,a);
    static GameObject Player=>GameObject.FindWithTag("Player");
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static Component C(string n,string t)=>GameObject.Find(n).GetComponent(T(t));
    static IEnumerator Load(){yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});T("CourtyardWaterTask").GetMethod("OfferHelp").Invoke(null,null);}
    static IEnumerator View(string n,Vector3 at,float d=8)=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("CloseUp",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n,(Vector3?)at,d});
    static void Move(Component c)
    {
        var at=(Vector3)Call(c,"GetTransform")+Vector3.up;Player.transform.position=at;Player.GetComponent<Rigidbody>().position=at;Physics.SyncTransforms();
    }
    static IEnumerator Wait()
    {
        float end=Time.realtimeSinceStartup+7;
        while(((bool)S("CourtyardWaterTask","IsBusy") || (bool)S("CourtyardGarden","IsBusy")) && Time.realtimeSinceStartup<end)yield return null;
        Assert.That(S("CourtyardWaterTask","IsBusy"),Is.False);Assert.That(S("CourtyardGarden","IsBusy"),Is.False);yield return null;
    }
    [UnityTest] public IEnumerator CarriedTools_BothDirectionsUseItemCardAndNormalFigure()
    {
        yield return Load();var can=C("Courtyard watering can","CourtyardGarden");Call(can,"InteractWithPlayer");yield return Wait();
        var visual=Player.GetComponent(T("PaperCharacterVisual"));var sprite=GameObject.Find("Watering can model").GetComponent<SpriteRenderer>();
        foreach(int direction in new[]{1,-1})
        {
            Call(visual,"FaceWorldDirection",Camera.main.transform.right*direction);yield return null;yield return null;
            Assert.That(GameObject.Find("Elian garden paper rig"),Is.Null);
            Assert.That(GameObject.Find("Elian paper walking rig"),Is.Not.Null);
            Assert.That(sprite.enabled,Is.False);
            var card=UnityEngine.Object.FindFirstObjectByType(T("CourtyardItemIndicator"));
            Assert.That(Call(card,"get_ItemText"),Is.EqualTo("Leere Gießkanne"));
            yield return View(direction==1?"third-can-right":"third-can-left",Player.transform.position+Vector3.up*.2f,6);
        }
        Move(can);Call(can,"InteractWithPlayer");yield return Wait();
        var bucket=C("Stable bucket station","CourtyardWaterTask");Move(bucket);Call(bucket,"InteractWithPlayer");yield return Wait();
        foreach(int direction in new[]{1,-1})
        {
            Call(visual,"FaceWorldDirection",Camera.main.transform.right*direction);yield return null;yield return null;
            var b=GameObject.Find("Stable water bucket").GetComponent<SpriteRenderer>();Assert.That(b.enabled,Is.False);
            Assert.That(GameObject.Find("Elian garden paper rig"),Is.Null);Assert.That(S("CourtyardWaterTask","CarriedItemText"),Is.EqualTo("Leerer Eimer"));
            yield return View(direction==1?"third-bucket-right":"third-bucket-left",Player.transform.position+Vector3.up*.2f,6);
        }
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator DrawBucket_NoWaterFromStone_CancelsAndCompletes_WithOneCue()
    {
        yield return Load();var bucket=C("Stable bucket station","CourtyardWaterTask");var well=C("Courtyard draw well","CourtyardWell");
        Move(bucket);Call(bucket,"InteractWithPlayer");yield return Wait();Move(well);Call(well,"InteractWithPlayer");yield return new WaitForSeconds(.7f);
        Assert.That(Call(well,"get_IsDrawing"),Is.EqualTo(true));
        Assert.That(GameObject.Find("Courtyard water transfer").GetComponentsInChildren<LineRenderer>().Any(l=>l.enabled),Is.False,"No stream before the drawing bucket is raised and tipped.");
        yield return View("third-well-drawing",well.transform.position+Vector3.up,11);
        Call(bucket,"Cancel");yield return null;Assert.That(Call(well,"get_IsDrawing"),Is.EqualTo(false));Assert.That(S("CourtyardWaterTask","BucketFull"),Is.False);
        Call(well,"InteractWithPlayer");yield return new WaitForSeconds(2.3f);yield return View("third-well-pouring",well.transform.position+Vector3.up,10);yield return Wait();
        Move(bucket);Call(bucket,"InteractWithPlayer");yield return new WaitForSeconds(1.55f);
        Assert.That(GameObject.Find("Courtyard water transfer").GetComponentsInChildren<LineRenderer>().Any(l=>l.enabled),Is.True);
        yield return View("third-trough-pour",GameObject.Find("Stable water trough").transform.position+Vector3.up*.8f,9);yield return Wait();
        Assert.That(S("CourtyardWaterTask","Completed"),Is.True);Assert.That(S("CourtyardWaterTask","HasBucket"),Is.False);
        Assert.That(S("CourtyardTaskFeedback","CompletionCount"),Is.EqualTo(1));Call(Dialogue,"CloseDialogue");
        T("HannesConversation").GetMethod("Open").Invoke(null,new[]{Dialogue});Call(Dialogue,"CloseDialogue");
        Assert.That(S("CourtyardTaskFeedback","CompletionCount"),Is.EqualTo(1));
        var cue=Resources.Load<AudioClip>("Audio/quest-complete");Assert.That(cue,Is.Not.Null);Assert.That(cue.length,Is.InRange(1f,3f));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator ClearStairs_FlatPaddock_AndSameGuardWhenTalking()
    {
        yield return Load();
        foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name=="Curved courtyard path"))
        {
            foreach(var v in f.sharedMesh.vertices)
            {
                var w=f.transform.TransformPoint(v);
                Assert.That(w.x> -36.7f && w.x< -31.3f && w.z>4.54f && w.z<7.9f,Is.False,"Road must end before visible stairs: "+w);
            }
        }
        foreach(var point in new[]{new Vector3(-20,0,1),new Vector3(-19,0,3),new Vector3(-18,0,4)})
        {
            float height=Physics.RaycastAll(point+Vector3.up*10,Vector3.down,20,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.CompareTag("Ground")).Max(h=>h.point.y);
            Assert.That(height,Is.LessThan(.03f));
        }
        yield return View("third-clear-stairs",new Vector3(-34,1,5),12);
        yield return View("third-flat-horse-yard",new Vector3(-19.5f,.7f,2),16);
        var guard=C("Courtyard Guard","CourtyardGuard");Move(guard);Call(guard,"InteractWithPlayer");yield return new WaitForSeconds(.6f);
        var visual=guard.GetComponent(T("CourtyardGuardVisual"));var rig=(Transform)visual.GetType().GetField("rig",H).GetValue(visual);
        Assert.That(rig.gameObject.activeSelf,Is.True);Assert.That(rig.localScale.y,Is.GreaterThan(1.05f));
        Call(Dialogue,"CloseDialogue");yield return View("third-guard-standing",guard.transform.position+Vector3.up,10);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator HillsideFeetStayOnGround_AndWalkingArmsCounterSwing()
    {
        yield return Load();var at=new Vector3(-20,0,-11);
        float floor=Physics.RaycastAll(at+Vector3.up*10,Vector3.down,20,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.CompareTag("Ground")).Max(h=>h.point.y);
        at.y=floor+1.12f;Player.transform.position=at;Player.GetComponent<Rigidbody>().position=at;Physics.SyncTransforms();yield return new WaitForSeconds(.3f);
        var visual=Player.GetComponent(T("PaperCharacterVisual"));
        var rig=visual.GetType().GetField("playerWalkRig",H).GetValue(visual);
        var root=(GameObject)rig.GetType().GetField("root",H).GetValue(rig);
        var near=(SpriteRenderer)rig.GetType().GetField("nearLeg",H).GetValue(rig);
        var far=(SpriteRenderer)rig.GetType().GetField("farLeg",H).GetValue(rig);
        Assert.That(Mathf.Min(near.bounds.min.y,far.bounds.min.y)-floor,Is.InRange(-.01f,.04f));
        var rb=Player.GetComponent<Rigidbody>();rb.isKinematic=false;rb.linearVelocity=Vector3.up*1.2f;
        visual.GetType().GetMethod("AlignGroundedPaper",H).Invoke(visual,null);
        Assert.That(Mathf.Min(near.bounds.min.y,far.bounds.min.y)-floor,Is.InRange(-.01f,.04f),"Climbing velocity must not disable foot contact.");
        rb.linearVelocity=Vector3.zero;
        var controller=Player.GetComponent(T("PlayerController"));controller.GetType().GetMethod("Jump",H).Invoke(controller,null);
        Assert.That(controller.GetType().GetProperty("HasActiveJump").GetValue(controller),Is.EqualTo(true));
        var pivot=(Transform)visual.GetType().GetField("paperPivot",H).GetValue(visual);var before=pivot.position;pivot.position+=Vector3.up*.2f;
        visual.GetType().GetMethod("AlignGroundedPaper",H).Invoke(visual,null);Assert.That(pivot.position.y,Is.EqualTo(before.y+.2f).Within(.001f),"Actual jumps must not be glued to the hill.");
        pivot.position=before;rb.isKinematic=true;
        yield return View("third-slope-contact",Player.transform.position+Vector3.up*.1f,8);
        ((Behaviour)visual).enabled=false;
        ((SpriteRenderer)visual.GetType().GetField("spriteRenderer",H).GetValue(visual)).enabled=false;
        foreach(float phase in new[]{Mathf.PI*.5f,Mathf.PI*1.5f})
        {
            Call(rig,"Present",true,true,false,phase,true,1f);
            Assert.That(near.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(far.transform.localScale,Is.EqualTo(Vector3.one));
            var arm=(SpriteRenderer)rig.GetType().GetField("nearArm",H).GetValue(rig);
            Assert.That(Mathf.DeltaAngle(0,arm.transform.localEulerAngles.z)*Mathf.DeltaAngle(0,near.transform.localEulerAngles.z),Is.LessThan(0));
            yield return View(phase<Mathf.PI?"third-walk-near-forward":"third-walk-far-forward",Player.transform.position+Vector3.up*.1f,7);
        }
        LogAssert.NoUnexpectedReceived();
    }

}
