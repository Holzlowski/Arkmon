using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardBucketIndicatorTests
{
    const BindingFlags H=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object S(string p)=>T("CourtyardWaterTask").GetProperty(p).GetValue(null);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n).Invoke(o,a);
    static Component C(string n,string t)=>GameObject.Find(n).GetComponent(T(t));
    static Component Station=>C("Stable bucket station","CourtyardWaterTask");
    static Component Well=>C("Courtyard draw well","CourtyardWell");
    static Component Trough=>C("Horse water delivery","CourtyardWaterTask");
    static GameObject Player=>GameObject.FindWithTag("Player");
    static SpriteRenderer Model=>GameObject.Find("Stable water bucket").GetComponent<SpriteRenderer>();
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static object Indicator=>UnityEngine.Object.FindFirstObjectByType(T("CourtyardItemIndicator"));
    static IEnumerator Load(bool reset=true)=>(IEnumerator)typeof(CourtyardWaterTaskTests).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{reset});
    static IEnumerator View(string n,Vector3 at,float distance=8)=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("CloseUp",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{n,(Vector3?)at,distance});
    static void Move(Component point){var at=(Vector3)Call(point,"GetTransform")+Vector3.up;Player.transform.position=at;Player.GetComponent<Rigidbody>().position=at;Physics.SyncTransforms();}
    static void CheckFigure()
    {
        Assert.That(GameObject.Find("Elian garden paper rig"),Is.Null,"The bucket action must not switch to the old carrying puppet.");
        Assert.That(GameObject.Find("Elian paper walking rig"),Is.Not.Null);
        var visual=Player.GetComponent(T("PaperCharacterVisual"));Assert.That(visual.GetType().GetField("gardenCan",H).GetValue(visual),Is.Null);
        Assert.That(Model.transform.IsChildOf(Player.transform),Is.False);
    }
    static void CheckCard(string expected,bool full)
    {
        Assert.That(Call(Indicator,"get_IsVisible"),Is.EqualTo(expected!=null));
        Assert.That(S("CarriedItemText"),Is.EqualTo(expected));
        if(expected==null)return;
        Assert.That(Call(Indicator,"get_ItemText"),Is.EqualTo(expected));
        var material=(Material)Indicator.GetType().GetField("iconMaterial",H).GetValue(Indicator);
        Assert.That(material.GetFloat("_Filled"),Is.EqualTo(full?1:0));
    }
    static IEnumerator Wait()
    {
        float until=Time.realtimeSinceStartup+7;
        while((bool)S("IsBusy")&&Time.realtimeSinceStartup<until){yield return null;CheckFigure();}
        Assert.That(S("IsBusy"),Is.False);yield return null;CheckFigure();
    }
    static IEnumerator Flow()
    {
        float until=Time.realtimeSinceStartup+6;
        while(Time.realtimeSinceStartup<until)
        {
            yield return null;CheckFigure();
            var effect=GameObject.Find("Courtyard water transfer");
            if(effect!=null&&effect.GetComponentsInChildren<LineRenderer>().Any(l=>l.enabled))yield break;
        }
        Assert.Fail("The water action must show its stream.");
    }
    [UnityTest] public IEnumerator IndicatorAndOrdinaryFigureCoverFillCancelDeliverReturnAndThanks()
    {
        yield return Load();CheckCard(null,false);
        T("HannesConversation").GetMethod("Open").Invoke(null,new[]{Dialogue});Call(Dialogue,"CloseDialogue");
        Move(Station);Call(Station,"InteractWithPlayer");yield return Wait();
        Assert.That(Model.enabled,Is.False);CheckCard("Leerer Eimer",false);
        yield return View("hannes-item-empty",Player.transform.position+Vector3.up*.2f,7);
        var controller=Player.GetComponent(T("PlayerController"));var visual=Player.GetComponent(T("PaperCharacterVisual"));
        foreach(int side in new[]{1,-1})
        {
            Call(controller,"SetScriptedMovement",true);Call(visual,"FaceWorldDirection",Camera.main.transform.right*side);
            Player.transform.position+=Camera.main.transform.right*(side*.12f);Player.GetComponent<Rigidbody>().position=Player.transform.position;
            yield return null;yield return null;CheckFigure();Assert.That(Model.enabled,Is.False);CheckCard("Leerer Eimer",false);
            Assert.That(Call(visual,"get_IsPlayingWalkCycle"),Is.EqualTo(true));Call(controller,"SetScriptedMovement",false);
        }
        Move(Well);Call(Well,"InteractWithPlayer");yield return Flow();
        Assert.That(Model.enabled,Is.True);Assert.That(S("BucketFull"),Is.False);
        yield return View("hannes-bucket-at-well",Well.transform.position+Vector3.up*.6f,9);
        yield return Wait();CheckCard("Voller Eimer",true);Assert.That(Model.enabled,Is.False);
        yield return View("hannes-item-full",Player.transform.position+Vector3.up*.2f,7);
        yield return Load(false);CheckCard("Voller Eimer",true);Assert.That(Model.enabled,Is.False);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardItemIndicator"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Move(Trough);Call(Trough,"InteractWithPlayer");yield return Flow();
        var lip=Model.transform.TransformPoint(new Vector3(.16f,-.22f,0));
        yield return null;Assert.That(Vector3.Distance(lip,Model.transform.TransformPoint(new Vector3(.16f,-.22f,0))),Is.LessThan(.001f),"The scene prop has a stable lip while pouring.");
        Call(Trough,"Cancel");yield return null;
        Assert.That(S("BucketFull"),Is.True);Assert.That(S("Completed"),Is.False);CheckCard("Voller Eimer",true);Assert.That(Model.enabled,Is.False);
        Assert.That(GameObject.Find("Courtyard water transfer"),Is.Null);
        Call(Trough,"InteractWithPlayer");yield return Flow();
        yield return View("hannes-bucket-at-trough",GameObject.Find("Stable water trough").transform.position+Vector3.up*.5f,8);
        yield return Wait();Assert.That(S("Completed"),Is.True);CheckCard("Leerer Eimer",false);
        Assert.That(T("CourtyardTaskFeedback").GetProperty("CompletionCount").GetValue(null),Is.EqualTo(1));
        Move(Station);Call(Station,"InteractWithPlayer");yield return Wait();CheckCard(null,false);Assert.That(Model.enabled,Is.True);
        Assert.That(Model.sharedMaterial.GetFloat("_Filled"),Is.Zero);
        T("HannesConversation").GetMethod("Open").Invoke(null,new[]{Dialogue});Assert.That(S("ThanksReceived"),Is.True);Call(Dialogue,"CloseDialogue");
        Assert.That(T("CourtyardTaskFeedback").GetProperty("CompletionCount").GetValue(null),Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator CancelledPickupPutdownAndSceneUnloadKeepTheItemState()
    {
        yield return Load();Move(Station);Call(Station,"InteractWithPlayer");yield return new WaitForSeconds(.06f);Call(Station,"Cancel");yield return null;
        Assert.That(S("HasBucket"),Is.False);Assert.That(Model.enabled,Is.True);CheckCard(null,false);
        Call(Station,"InteractWithPlayer");yield return Wait();Call(Station,"InteractWithPlayer");yield return new WaitForSeconds(.06f);Call(Station,"Cancel");yield return null;
        Assert.That(S("HasBucket"),Is.True);Assert.That(Model.enabled,Is.False);CheckCard("Leerer Eimer",false);
        Move(Well);Call(Well,"InteractWithPlayer");yield return new WaitForSeconds(.4f);yield return Load(false);
        Assert.That(S("HasBucket"),Is.True);Assert.That(S("BucketFull"),Is.False);Assert.That(S("IsBusy"),Is.False);CheckCard("Leerer Eimer",false);
        Assert.That(GameObject.Find("Courtyard water transfer"),Is.Null);Assert.That(Model.enabled,Is.False);CheckFigure();LogAssert.NoUnexpectedReceived();
    }
}