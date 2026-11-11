using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardWaterTaskTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static object S(string type,string prop)=>T(type).GetProperty(prop).GetValue(null);
    static Component C(string label,string type)=>GameObject.Find(label).GetComponent(T(type));
    static Component Bucket=>C("Stable bucket station","CourtyardWaterTask");
    static Component Well=>C("Courtyard draw well","CourtyardWell");
    static Component Can=>C("Courtyard watering can","CourtyardGarden");
    static Component Trough=>C("Horse water delivery","CourtyardWaterTask");
    static GameObject Player=>GameObject.FindWithTag("Player");
    static void Close()=>Call(Dialogue,"CloseDialogue");
    static void Move(Component target)
    {
        var p=(Vector3)Call(target,"GetTransform")+Vector3.up;
        Player.transform.position=p;Player.GetComponent<Rigidbody>().position=p;Physics.SyncTransforms();
    }
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static IEnumerator Wait()
    {
        float end=Time.realtimeSinceStartup+5;
        while(((bool)S("CourtyardWaterTask","IsBusy") || (bool)S("CourtyardGarden","IsBusy")) && Time.realtimeSinceStartup<end)yield return null;
        Assert.That(S("CourtyardWaterTask","IsBusy"),Is.False);Assert.That(S("CourtyardGarden","IsBusy"),Is.False);
        yield return null;
    }
    static IEnumerator Load(bool reset=true)
    {
        if(reset)
        {
            T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
            T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
            T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        }
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.8f);Close();T("CourtyardWaterTask").GetMethod("OfferHelp").Invoke(null,null);
        ((Behaviour)Player.GetComponent(T("PlayerController"))).enabled=false;
        Player.GetComponent<Rigidbody>().isKinematic=true;Player.GetComponent<Rigidbody>().interpolation=RigidbodyInterpolation.None;
    }
    [UnityTest] public IEnumerator Bucket_FillCancelReloadDeliverReturnAndThanks()
    {
        yield return Load();T("HannesConversation").GetMethod("Open").Invoke(null,new[]{Dialogue});Close();
        Assert.That(S("CourtyardWaterTask","ObjectiveText").ToString(),Does.Contain("Eimer"));
        Move(Bucket);Call(Bucket,"InteractWithPlayer");yield return Wait();Assert.That(S("CourtyardWaterTask","HasBucket"),Is.True);
        Move(Can);Call(Can,"InteractWithPlayer");Close();Assert.That(S("CourtyardGarden","HasCan"),Is.False);
        Move(Trough);Call(Trough,"InteractWithPlayer");Close();Assert.That(S("CourtyardWaterTask","Completed"),Is.False);
        Move(Well);Call(Well,"InteractWithPlayer");yield return new WaitForSeconds(.45f);
        Capture("water-bucket-filling");Call(Bucket,"Cancel");yield return null;Assert.That(S("CourtyardWaterTask","BucketFull"),Is.False);
        Assert.That(GameObject.Find("Courtyard water transfer"),Is.Null);
        Call(Well,"InteractWithPlayer");yield return Wait();Assert.That(S("CourtyardWaterTask","BucketFull"),Is.True);
        yield return Load(false);Assert.That(S("CourtyardWaterTask","HasBucket"),Is.True);Assert.That(S("CourtyardWaterTask","BucketFull"),Is.True);
        var exit=UnityEngine.Object.FindObjectsByType(T("CourtyardEntrance"),FindObjectsSortMode.None).Cast<Component>().First();
        Call(exit,"InteractWithPlayer");Assert.That((bool)T("DialogueBoxController").GetProperty("IsOpen").GetValue(Dialogue),Is.True);Close();
        Move(Trough);Call(Trough,"InteractWithPlayer");yield return new WaitForSeconds(.4f);Call(Trough,"Cancel");
        Assert.That(S("CourtyardWaterTask","BucketFull"),Is.True);Assert.That(S("CourtyardWaterTask","Completed"),Is.False);
        Call(Trough,"InteractWithPlayer");yield return new WaitForSeconds(.7f);Capture("water-horse-trough-pour");yield return Wait();
        Assert.That(S("CourtyardWaterTask","Completed"),Is.True);Assert.That(S("CourtyardWaterTask","BucketFull"),Is.False);
        Move(Bucket);Call(Bucket,"InteractWithPlayer");yield return Wait();Assert.That(S("CourtyardWaterTask","HasBucket"),Is.False);
        T("HannesConversation").GetMethod("Open").Invoke(null,new[]{Dialogue});Close();Assert.That(S("CourtyardWaterTask","ThanksReceived"),Is.True);
        yield return Load(false);Assert.That(S("CourtyardWaterTask","Completed"),Is.True);
        T("IntroChapter").GetMethod("ResetProgress").Invoke(null,null);Assert.That(S("CourtyardWaterTask","Completed"),Is.False);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Can_RequiresWell_ThreeBedsUseOneFilling()
    {
        yield return Load();Move(Can);Call(Can,"InteractWithPlayer");yield return Wait();Assert.That(S("CourtyardGarden","WaterCharges"),Is.EqualTo(0));
        var beds=UnityEngine.Object.FindObjectsByType(T("CourtyardGarden"),FindObjectsSortMode.None).Cast<Component>().Where(c=>c.name=="Garden watering point").OrderBy(c=>c.transform.position.x).ToArray();
        Move(beds[0]);Call(beds[0],"InteractWithPlayer");Assert.That(S("CourtyardGarden","IsBusy"),Is.False);Close();
        Move(Well);yield return null;Assert.That((bool)T("DialogueBoxController").GetProperty("IsOpen").GetValue(Dialogue),Is.False);
        Call(Well,"InteractWithPlayer");yield return new WaitForSeconds(.3f);Call(Bucket,"Cancel");Assert.That(S("CourtyardGarden","WaterCharges"),Is.EqualTo(0));
        Call(Well,"InteractWithPlayer");yield return new WaitForSeconds(.6f);Capture("water-can-at-well");yield return Wait();
        Assert.That(S("CourtyardGarden","WaterCharges"),Is.EqualTo(3));Call(Well,"InteractWithPlayer");Close();Assert.That(S("CourtyardWaterTask","IsBusy"),Is.False);
        Move(Bucket);Call(Bucket,"InteractWithPlayer");Close();Assert.That(S("CourtyardWaterTask","HasBucket"),Is.False);
        Move(beds[0]);Call(beds[0],"InteractWithPlayer");yield return new WaitForSeconds(.4f);Call(beds[0],"CancelWatering");Assert.That(S("CourtyardGarden","WaterCharges"),Is.EqualTo(3));
        for(int i=0;i<3;i++){Move(beds[i]);Call(beds[i],"InteractWithPlayer");yield return Wait();Assert.That(S("CourtyardGarden","WaterCharges"),Is.EqualTo(2-i));}
        Assert.That(S("CourtyardGarden","Completed"),Is.True);Assert.That(S("CourtyardWaterTask","Completed"),Is.False);
        Move(Can);Call(Can,"InteractWithPlayer");yield return Wait();Assert.That(S("CourtyardGarden","HasCan"),Is.False);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator FillingSceneUnload_DiscardsPartialWaterAndReleasesLock()
    {
        yield return Load();Move(Bucket);Call(Bucket,"InteractWithPlayer");yield return Wait();Move(Well);Call(Well,"InteractWithPlayer");yield return new WaitForSeconds(.3f);
        yield return Load(false);Assert.That(S("CourtyardWaterTask","IsBusy"),Is.False);Assert.That(S("CourtyardWaterTask","BucketFull"),Is.False);Assert.That(S("CourtyardWaterTask","HasBucket"),Is.True);
        Assert.That(GameObject.Find("Courtyard water transfer"),Is.Null);LogAssert.NoUnexpectedReceived();
    }
    static Vector3 Limit(Vector3 p,Vector3 v)=>(Vector3)T("CourtyardBorrowedTools").GetMethod("ConstrainMovement").Invoke(null,new object[]{p,v,.02f});
    static bool Inside(Vector3 p)=>(bool)T("CourtyardBorrowedTools").GetMethod("InsideArea").Invoke(null,new object[]{p});
    [UnityTest] public IEnumerator BorrowedTools_StayNearOwnerAndWell_AndReleaseAfterReturn()
    {
        yield return Load();
        Assert.That(Limit(new Vector3(40,0,0),Vector3.right*5),Is.EqualTo(Vector3.right*5));
        Move(Can);Call(Can,"InteractWithPlayer");yield return Wait();
        Assert.That(Inside(new Vector3(10,0,-9.8f)),Is.True,"The can reaches the well.");
        Assert.That(Inside(new Vector3(7.5f,0,-9)),Is.True,"A little room beyond the well remains.");
        Assert.That(Inside(new Vector3(-5,0,-9)),Is.False);
        Assert.That(Limit(new Vector3(7,0,-9),Vector3.left*5).x,Is.Zero);
        Assert.That(Limit(new Vector3(7,0,-9),Vector3.right*5).x,Is.GreaterThan(0));
        Assert.That(Limit(new Vector3(7,0,-9),Vector3.forward*5).z,Is.EqualTo(5));
        Player.transform.position=new Vector3(7.2f,1,-9);Player.GetComponent<Rigidbody>().position=Player.transform.position;yield return null;yield return null;
        Assert.That((bool)T("DialogueBoxController").GetProperty("IsOpen").GetValue(Dialogue),Is.True);Close();yield return null;yield return null;
        Assert.That((bool)T("DialogueBoxController").GetProperty("IsOpen").GetValue(Dialogue),Is.False);
        Move(Can);Call(Can,"InteractWithPlayer");yield return Wait();
        Assert.That(Limit(new Vector3(7,0,-9),Vector3.left*5).x,Is.EqualTo(-5));
        Move(Bucket);Call(Bucket,"InteractWithPlayer");yield return Wait();
        Assert.That(Inside(new Vector3(10,0,-9.8f)),Is.True);Assert.That(Inside(new Vector3(12.5f,0,-9)),Is.True);
        Assert.That(Inside(new Vector3(27,0,-12)),Is.False);
        Assert.That(Limit(new Vector3(13,0,-9),Vector3.right*5).x,Is.Zero);
        Assert.That(Limit(new Vector3(14,0,-9),Vector3.left*5).x,Is.LessThan(0),"Displacement must never trap a returning player.");
        Move(Bucket);Call(Bucket,"InteractWithPlayer");yield return Wait();
        Assert.That(Limit(new Vector3(14,0,-9),Vector3.right*5).x,Is.EqualTo(5));
        LogAssert.NoUnexpectedReceived();
    }

}
