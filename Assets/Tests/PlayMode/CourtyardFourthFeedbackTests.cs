using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardFourthFeedbackTests
{
    const BindingFlags H=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object S(string t,string p)=>T(t).GetProperty(p).GetValue(null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static GameObject Player=>GameObject.FindWithTag("Player");
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static Component C(string n,string t)=>GameObject.Find(n).GetComponent(T(t));
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static IEnumerator View(string n,Vector3 at,float d=7)=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("CloseUp",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n,(Vector3?)at,d});
    static void Move(Component c){var p=(Vector3)Call(c,"GetTransform")+Vector3.up;Player.transform.position=p;Player.GetComponent<Rigidbody>().position=p;Physics.SyncTransforms();}
    static IEnumerator Wait(){float end=Time.realtimeSinceStartup+6;while(((bool)S("CourtyardWaterTask","IsBusy") || (bool)S("CourtyardGarden","IsBusy")) && Time.realtimeSinceStartup<end)yield return null;Assert.That(S("CourtyardWaterTask","IsBusy"),Is.False);Assert.That(S("CourtyardGarden","IsBusy"),Is.False);yield return null;}

    [UnityTest] public IEnumerator Gait_ContinuousPhaseFixedHipsAndRearFootBehind()
    {
        yield return Load();var visual=Player.GetComponent(T("PaperCharacterVisual"));
        var rig=visual.GetType().GetField("playerWalkRig",H).GetValue(visual);
        var root=(GameObject)rig.GetType().GetField("root",H).GetValue(rig);
        var near=(SpriteRenderer)rig.GetType().GetField("nearLeg",H).GetValue(rig);
        var far=(SpriteRenderer)rig.GetType().GetField("farLeg",H).GetValue(rig);
        var update=visual.GetType().GetMethod("UpdateWalkFrame",H);var travelled=visual.GetType().GetField("travelled",H);
        update.Invoke(visual,new object[]{true,.18f});float phase=(float)travelled.GetValue(visual);
        update.Invoke(visual,new object[]{false,0f});Assert.That(travelled.GetValue(visual),Is.EqualTo(phase),"Releasing movement must not reset the step phase.");
        ((Behaviour)visual).enabled=false;((SpriteRenderer)visual.GetType().GetField("spriteRenderer",H).GetValue(visual)).enabled=false;
        Call(rig,"Present",true,true,false,0f,false,0f);
        Assert.That(far.bounds.max.x,Is.LessThan(near.bounds.max.x-.12f),"The rear toe stays behind the front foot.");
        yield return View("fourth-idle-rear-foot",Player.transform.position+Vector3.up*.15f);
        for(int i=0;i<=64;i++)
        {
            float p=i*Mathf.PI/32;Call(rig,"Present",true,true,false,p,true,1f);
            Assert.That(root.transform.localPosition.sqrMagnitude,Is.LessThan(.000001f),"Walking must not lift the whole puppet from boot bounds.");
            Assert.That(near.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(far.transform.localScale,Is.EqualTo(Vector3.one));
            Assert.That(Mathf.Min(near.bounds.min.y,far.bounds.min.y)-root.transform.position.y,Is.InRange(.005f,.025f));
            if(i%16==0)yield return View("fourth-walk-"+i,Player.transform.position+Vector3.up*.15f);
        }
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator BucketIndicator_KeepsNormalFigureAndFirstPickupExplainsTaskOnce()
    {
        yield return Load();var b=C("Stable bucket station","CourtyardWaterTask");Move(b);Call(b,"InteractWithPlayer");yield return Wait();
        Assert.That(S("CourtyardWaterTask","HasBucket"),Is.True);Assert.That(Call(Dialogue,"get_IsOpen"),Is.EqualTo(true));
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Hannes"));Call(Dialogue,"CloseDialogue");
        var visual=Player.GetComponent(T("PaperCharacterVisual"));
        foreach(int side in new[]{1,-1})
        {
            Call(visual,"FaceWorldDirection",Camera.main.transform.right*side);yield return null;yield return null;
            Assert.That(GameObject.Find("Elian garden paper rig"),Is.Null);
            Assert.That(GameObject.Find("Elian paper walking rig"),Is.Not.Null);
            var item=GameObject.Find("Stable water bucket").GetComponent<SpriteRenderer>();
            Assert.That(item.enabled,Is.False);
            Assert.That(S("CourtyardWaterTask","CarriedItemText"),Is.EqualTo("Leerer Eimer"));
            yield return View("fourth-bucket-"+side,Player.transform.position+Vector3.up*.1f);
        }
        Move(b);Call(b,"InteractWithPlayer");yield return Wait();Call(b,"InteractWithPlayer");yield return Wait();
        Assert.That(Call(Dialogue,"get_IsOpen"),Is.EqualTo(false),"Returning and taking again must not repeat the introductory greeting.");
        Move(b);Call(b,"InteractWithPlayer");yield return Wait();
        var can=C("Courtyard watering can","CourtyardGarden");Move(can);Call(can,"InteractWithPlayer");yield return Wait();
        foreach(int side in new[]{1,-1}){Call(visual,"FaceWorldDirection",Camera.main.transform.right*side);yield return null;yield return View("fourth-can-"+side,Player.transform.position+Vector3.up*.1f);}
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Well_FixedLipCrankAssemblyAndTroughPour()
    {
        yield return Load();T("CourtyardWaterTask").GetMethod("OfferHelp").Invoke(null,null);
        var b=C("Stable bucket station","CourtyardWaterTask");var well=C("Courtyard draw well","CourtyardWell");
        var pivot=well.transform.Find("Well crank pivot");var grip=pivot.Find("Well crank grip");
        float radius=Vector3.Distance(grip.position,pivot.position);
        foreach(float p in new[]{0f,.12f,.35f,.50f,.62f,.75f,.90f})
        {
            Call(well,"PresentDraw",p);
            var draw=well.transform.Find("Well drawing bucket");
            if(p<=.50f){Assert.That(draw.localPosition.x,Is.Zero.Within(.001f));Assert.That(draw.localPosition.z,Is.Zero.Within(.001f));}
            if(p>=.50f){var lip=well.transform.InverseTransformPoint((Vector3)Call(well,"get_PourSource"));Assert.That(lip.x,Is.Zero.Within(.001f));Assert.That(lip.z,Is.EqualTo(-.24f).Within(.001f));}
            Assert.That(Vector3.Distance(grip.position,pivot.position),Is.EqualTo(radius).Within(.001f));
            Assert.That(pivot.Find("Well crank arm"),Is.Not.Null);
        }
        Call(well,"PresentDraw",1.15f);Assert.That(Quaternion.Angle(pivot.localRotation,Quaternion.identity),Is.LessThan(.01f),"The crank returns smoothly before the draw rig resets.");
        Call(well,"ResetDraw");Move(b);Call(b,"InteractWithPlayer");yield return Wait();Move(well);Call(well,"InteractWithPlayer");
        for(int i=0;i<6;i++){yield return new WaitForSeconds(.55f);yield return View("fourth-well-"+i,well.transform.position+Vector3.up*.9f,10);}
        yield return Wait();Assert.That(S("CourtyardWaterTask","BucketFull"),Is.True);
        Assert.That(GameObject.Find("Courtyard water transfer"),Is.Null);
        var trough=C("Horse water delivery","CourtyardWaterTask");Move(trough);Call(trough,"InteractWithPlayer");
        bool sawFlow=false;
        for(int i=0;i<80 && (bool)S("CourtyardWaterTask","IsBusy");i++)
        {
            yield return new WaitForSeconds(.04f);
            var transfer=GameObject.Find("Courtyard water transfer");
            var line=transfer!=null?transfer.GetComponentsInChildren<LineRenderer>().FirstOrDefault(l=>l.enabled):null;
            if(line!=null)
            {
                var item=GameObject.Find("Stable water bucket").transform;
                Assert.That(item.TransformPoint(new Vector3(.16f,-.22f,0)).y,Is.LessThan(item.TransformPoint(new Vector3(0,-.22f,0)).y-.10f),"Water must leave the lower lip; the opening tips towards the trough.");
                sawFlow=true;var source=line.GetPosition(0);var end=line.GetPosition(line.positionCount-1);
                var target=GameObject.Find("Stable water trough").transform;
                var inside=target.InverseTransformPoint(end);
                Assert.That(source.y,Is.GreaterThan(end.y+.10f));
                Assert.That(target.InverseTransformPoint(source).y,Is.EqualTo(.83f).Within(.01f),"The scene bucket lip rests at the trough rim.");
                Assert.That(Mathf.Abs(inside.x),Is.LessThan(.3f));Assert.That(Mathf.Abs(inside.z),Is.LessThan(.78f));
                if(i%4==0)yield return View("fourth-trough-flow-"+i,target.position+Vector3.up*.65f,8);
            }
        }
        Assert.That(sawFlow,Is.True,"The bucket visibly pours down into the trough, after reaching its rim.");
        yield return Wait();Assert.That(S("CourtyardWaterTask","Completed"),Is.True);Assert.That(S("CourtyardWaterTask","BucketFull"),Is.False);
        Assert.That(S("CourtyardTaskFeedback","CompletionCount"),Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator WalkingPlayer_StartAndStopDoNotSnapTheBodyOrResetSteps()
    {
        yield return Load();var visual=Player.GetComponent(T("PaperCharacterVisual"));var controller=Player.GetComponent(T("PlayerController"));
        var rig=visual.GetType().GetField("playerWalkRig",H).GetValue(visual);var root=(GameObject)rig.GetType().GetField("root",H).GetValue(rig);
        var torso=root.transform.Find("Stable upper body");var phase=visual.GetType().GetField("travelled",H);
        Player.transform.position=new Vector3(-20,1.12f,2);Player.GetComponent<Rigidbody>().position=Player.transform.position;Physics.SyncTransforms();
        yield return new WaitForSeconds(.4f);float previous=(torso.position-Player.transform.position).y;
        for(int i=0;i<90;i++)
        {
            bool moving=i<35 || i>=50;Call(controller,"SetScriptedMovement",moving);
            if(moving){Player.transform.position+=Vector3.right*.04f;Player.GetComponent<Rigidbody>().position=Player.transform.position;}
            yield return null;
            Assert.That(Mathf.Abs((torso.position-Player.transform.position).y-previous),Is.LessThan(.025f),"The body must not jump at foot changes or when stopping. Frame="+i+" Position="+Player.transform.position);previous=(torso.position-Player.transform.position).y;
            Assert.That(root.transform.localPosition.sqrMagnitude,Is.LessThan(.000001f));
            if(i==35)Assert.That((float)phase.GetValue(visual),Is.GreaterThan(.1f),"Stopping preserves the current stride.");
        }
        Call(controller,"SetScriptedMovement",false);LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Hannes_WholeRearArmAndUnfoldedMeshThroughStroke()
    {
        yield return Load();var h=C("Courtyard Stablekeeper","CourtyardStablekeeperVisual");
        var visual=(Behaviour)h;visual.enabled=false;
        var paper=(Transform)h.GetType().GetField("paper",H).GetValue(h);
        var source=paper.GetComponent<SpriteRenderer>();source.sprite=Resources.Load<Sprite>("Hannes/GroomForward");
        var groom=paper.GetComponent(T("CourtyardGroomPose"));
        foreach(float p in new[]{0f,.25f,.5f,.75f,1f})
        {
            Call(groom,"Present",source,true,p);
            var mesh=paper.GetComponentInChildren<MeshFilter>().sharedMesh;var v=mesh.vertices;var t=mesh.triangles;
            float sign=Mathf.Sign(Vector3.Cross(v[t[1]]-v[t[0]],v[t[2]]-v[t[0]]).z);
            for(int i=0;i<t.Length;i+=3)Assert.That(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).z*sign,Is.GreaterThan(0),"No folded triangles may erase parts of the painted rear arm.");
            yield return View("fourth-hannes-"+p,h.transform.position+Vector3.up,8);
        }
        LogAssert.NoUnexpectedReceived();
    }
}
