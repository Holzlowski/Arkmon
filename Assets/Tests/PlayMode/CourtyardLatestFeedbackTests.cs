using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardLatestFeedbackTests
{
    const BindingFlags H=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static object S(string t,string n)=>T(t).GetProperty(n).GetValue(null);
    static GameObject Player=>GameObject.FindWithTag("Player");
    static Component C(string name,string type)=>GameObject.Find(name).GetComponent(T(type));
    static void Close()=>Call(Dialogue,"CloseDialogue");
    static IEnumerator Load(){yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});T("CourtyardWaterTask").GetMethod("OfferHelp").Invoke(null,null);}
    static void Move(Component point)
    {
        var at=(Vector3)Call(point,"GetTransform")+Vector3.up;Player.transform.position=at;Player.GetComponent<Rigidbody>().position=at;Physics.SyncTransforms();
    }
    static IEnumerator Wait()
    {
        float end=Time.realtimeSinceStartup+6;
        while((bool)S("CourtyardWaterTask","IsBusy") && Time.realtimeSinceStartup<end)yield return null;
        Assert.That(S("CourtyardWaterTask","IsBusy"),Is.False);yield return null;
    }
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static IEnumerator View(string name,Vector3 focus,float distance=9)
        =>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("CloseUp",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name,(Vector3?)focus,distance});
    [UnityTest] public IEnumerator Wake_HidesAllPuppetParts_AndIdleFeetAreStaggered()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("TowerRoom");
        bool slept=false,seated=false;float end=Time.realtimeSinceStartup+9;
        while(!(bool)S("DemoOpening","HasAwakened") && Time.realtimeSinceStartup<end)
        {
            yield return null;
            var bed=UnityEngine.Object.FindFirstObjectByType(T("PaintedCanopyBed"));
            if(bed==null || Player==null)continue;
            bool sleeping=(bool)bed.GetType().GetProperty("IsSleeping").GetValue(bed);
            bool sitting=(bool)bed.GetType().GetProperty("IsSeated").GetValue(bed);
            if(sleeping || sitting)
            {
                foreach(var rig in Player.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Elian paper walking rig" || t.name=="Elian garden paper rig"))
                    Assert.That(rig.gameObject.activeInHierarchy,Is.False,"The bed drawing is the only Elian during this pose.");
                var presentation=UnityEngine.Object.FindFirstObjectByType(T("DemoOpeningPresentation"));
                var fade=presentation!=null?presentation.GetType().GetField("fade",H).GetValue(presentation):null;
                bool visible=fade==null || ((Color)fade.GetType().GetProperty("color").GetValue(fade)).a<.05f;
                if(sleeping && !slept && visible){Capture("feedback-wake-sleeping");slept=true;}
                if(sitting && !seated && visible){Capture("feedback-wake-seated");seated=true;}
            }
        }
        Assert.That(slept&&seated,Is.True);Assert.That(S("DemoOpening","HasAwakened"),Is.True);Close();
        float settle=Time.realtimeSinceStartup+4;while((bool)S("DemoOpening","IsPresenting") && Time.realtimeSinceStartup<settle)yield return null;
        yield return new WaitForSeconds(.35f);
        var root=GameObject.Find("Elian paper walking rig");Assert.That(root,Is.Not.Null);
        var near=root.transform.Find("Near leg");var far=root.transform.Find("Far leg");
        Assert.That(near.localPosition.x-far.localPosition.x,Is.InRange(.12f,.16f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(near.localEulerAngles.z,far.localEulerAngles.z)),Is.InRange(14f,18f));
        Capture("feedback-room-idle-feet");LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator EmptyBucket_FillsVisibly_AndFullReturnThanksElian()
    {
        yield return Load();var bucket=C("Stable bucket station","CourtyardWaterTask");var well=C("Courtyard draw well","CourtyardWell");
        var sprite=GameObject.Find("Stable water bucket").GetComponent<SpriteRenderer>();
        Assert.That(sprite.sharedMaterial.GetFloat("_Filled"),Is.Zero);
        Move(bucket);var at=Player.transform.position+Camera.main.transform.right;
        Player.transform.position=at;Player.GetComponent<Rigidbody>().position=at;Physics.SyncTransforms();
        yield return new WaitForSeconds(.2f);yield return View("feedback-empty-bucket",sprite.bounds.center,3);
        Move(bucket);Call(bucket,"InteractWithPlayer");yield return Wait();Move(well);Call(well,"InteractWithPlayer");yield return Wait();
        Assert.That(S("CourtyardWaterTask","BucketFull"),Is.True);Assert.That(sprite.sharedMaterial.GetFloat("_Filled"),Is.EqualTo(1));
        Assert.That(sprite.enabled,Is.False);
        Assert.That(T("CourtyardWaterTask").GetProperty("CarriedItemText").GetValue(null),Is.EqualTo("Voller Eimer"));
        yield return View("feedback-full-bucket",Player.transform.position+Vector3.up*.3f,6);
        Move(bucket);Call(bucket,"InteractWithPlayer");yield return Wait();
        Assert.That(S("CourtyardWaterTask","Completed"),Is.True);Assert.That(S("CourtyardWaterTask","HasBucket"),Is.False);Assert.That(S("CourtyardWaterTask","BucketFull"),Is.False);
        Assert.That(T("DialogueBoxController").GetProperty("ActiveSpeaker").GetValue(Dialogue),Is.EqualTo("Hannes"));
        Assert.That(sprite.sharedMaterial.GetFloat("_Filled"),Is.Zero);Capture("feedback-hannes-full-return");Close();LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator TowerAndStableApproaches_AreClear_AndGuardHasLongerSilhouette()
    {
        yield return Load();
        foreach(float x in new[]{-36f,-34f,-32f})
        {
            var hits=Physics.RaycastAll(new Vector3(x,10,4.1f),Vector3.down,20,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.CompareTag("Ground")).ToArray();
            Assert.That(hits.Length,Is.GreaterThan(0));Assert.That(hits.Max(h=>h.point.y),Is.LessThan(.035f));
        }
        yield return View("feedback-tower-clear-steps",new Vector3(-34,1,5),13);
        bool retired=false;
        foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name=="Curved courtyard path"))
        {
            var v=f.sharedMesh.vertices;if(v.Length<17)continue;var a=f.transform.TransformPoint((v[0]+v[16])*.5f);
            if(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(-16,-6))<.2f){retired=true;Assert.That(f.GetComponent<Renderer>().enabled,Is.False);}
        }
        Assert.That(retired,Is.True);yield return View("feedback-stable-soft-connection",new Vector3(-11,1,0),16);
        var guard=C("Courtyard Guard","CourtyardGuardVisual");((Behaviour)guard).enabled=false;
        var rig=(Transform)guard.GetType().GetField("rig",H).GetValue(guard);rig.gameObject.SetActive(true);
        guard.GetType().GetMethod("PresentGait",H).Invoke(guard,new object[]{Mathf.PI*.5f,1f,false});
        Assert.That(rig.localScale.y,Is.GreaterThan(1.05f));
        yield return View("feedback-guard-proportions",guard.transform.position+Vector3.up*1.2f,9);
        LogAssert.NoUnexpectedReceived();
    }
}
