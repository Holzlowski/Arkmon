using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardGardenTaskTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static object State(string n)=>T("CourtyardGarden").GetProperty(n).GetValue(null);
    static Component Station=>GameObject.Find("Courtyard watering can").GetComponent(T("CourtyardGarden"));
    static Component[] Beds=>UnityEngine.Object.FindObjectsByType(T("CourtyardGarden"),FindObjectsSortMode.None).Cast<Component>().Where(c=>c.name=="Garden watering point").OrderBy(c=>c.transform.position.x).ToArray();
    static GameObject Player=>GameObject.FindWithTag("Player");
    static void Close()=>Call(Dialogue,"CloseDialogue");
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    public static void MoveTo(Component point)
    {
        var p=point.transform.position+Vector3.up;
        Player.transform.position=p;Player.GetComponent<Rigidbody>().position=p;Physics.SyncTransforms();
    }
    static IEnumerator Load(bool reset=true)
    {
        if(reset)
        {
            T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
            T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
            T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        }
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.7f);Close();
        T("DialogueBoxController").GetField("charactersPerSecond",Hidden).SetValue(Dialogue,100000f);
        ((Behaviour)Player.GetComponent(T("PlayerController"))).enabled=false;
        var body=Player.GetComponent<Rigidbody>();body.interpolation=RigidbodyInterpolation.None;body.isKinematic=true;
        MoveTo(Station);yield return new WaitForSeconds(.3f);
    }
    public static IEnumerator WaitForPour()
    {
        float deadline=Time.realtimeSinceStartup+4;
        while((bool)State("IsBusy") && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(State("IsBusy"),Is.False,"The garden animation must finish and release exploration.");
    }
    static Color SoilColor(Component bed)
    {
        var soil=(Renderer)bed.GetType().GetField("soil",Hidden).GetValue(bed);
        var block=new MaterialPropertyBlock();soil.GetPropertyBlock(block);return block.GetColor("_BaseColor");
    }
    [UnityTest] public IEnumerator PourRequiresCompletion_CancelsCleanly_AndSurvivesReload()
    {
        yield return Load();Call(Station,"InteractWithPlayer");yield return WaitForPour();Close();yield return null;
        var can=GameObject.Find("Watering can model");
        Assert.That(can.activeInHierarchy,Is.True);
        Assert.That(Vector3.Distance(can.transform.position,Player.transform.position),Is.LessThan(1));
        MoveTo(Beds[0]);yield return new WaitForSeconds(.7f);Capture("garden-carrying-can");
        var bed= Beds[0];Color dry=SoilColor(bed);
        Call(bed,"InteractWithPlayer");Call(bed,"InteractWithPlayer");
        Assert.That(State("IsWatering"),Is.True);
        Assert.That(T("DemoOpening").GetProperty("IsPresenting").GetValue(null),Is.True);
        Assert.That(State("WateredCount"),Is.EqualTo(0));
        yield return new WaitForSeconds(.7f);Capture("garden-watering-in-progress");
        Assert.That(SoilColor(bed).r,Is.LessThan(dry.r));
        Assert.That(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Garden watering drops"),Is.EqualTo(1));
        Assert.That(bed.GetComponent<AudioSource>().clip,Is.Not.Null);
        Call(bed,"CancelWatering");yield return null;
        Assert.That(State("WateredCount"),Is.EqualTo(0));Assert.That(State("IsWatering"),Is.False);
        Assert.That(T("DemoOpening").GetProperty("IsPresenting").GetValue(null),Is.False);
        Assert.That(SoilColor(bed),Is.EqualTo(dry));
        Assert.That(GameObject.Find("Garden watering drops"),Is.Null);
        Assert.That(bed.GetComponent<AudioSource>(),Is.Null);
        Call(bed,"InteractWithPlayer");yield return WaitForPour();
        Assert.That(State("WateredCount"),Is.EqualTo(1));
        Assert.That(State("ObjectiveText").ToString(),Does.Contain("1/3"));
        MoveTo(Beds[1]);Call(Beds[1],"InteractWithPlayer");yield return new WaitForSeconds(.3f);
        // Unloading halfway must preserve the completed bed, discard the partial one and release the lock.
        yield return Load(false);
        Assert.That(State("IsWatering"),Is.False);Assert.That(State("HasCan"),Is.True);
        Assert.That(State("WateredCount"),Is.EqualTo(1));
        Assert.That(SoilColor(Beds[0]).r,Is.LessThan(SoilColor(Beds[1]).r));
        Assert.That(GameObject.Find("Garden watering drops"),Is.Null);
        yield return Load();
        Assert.That(State("WateredCount"),Is.EqualTo(0));Assert.That(State("HasCan"),Is.False);
        Assert.That(State("ObjectiveText"),Is.Null);LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator ThreeBeds_ThanksAndReturningCan_CloseTheOptionalTask()
    {
        yield return Load();Call(Station,"InteractWithPlayer");yield return WaitForPour();Close();
        var evt=T("CourtyardGarden").GetEvent("OnCompleted");int completed=0;
        Action handler=()=>completed++;evt.AddEventHandler(null,handler);
        try
        {
            foreach(var bed in Beds)
            {
                MoveTo(bed);yield return null;Call(bed,"InteractWithPlayer");yield return WaitForPour();
            }
            Assert.That(completed,Is.EqualTo(1));Assert.That(State("Completed"),Is.True);
            Call(Beds[0],"InteractWithPlayer");Close();Assert.That(completed,Is.EqualTo(1));
            var marta=GameObject.Find("Courtyard Gardener").GetComponent(T("CourtyardLife"));
            Call(marta,"InteractWithPlayer");Assert.That(State("ThanksReceived"),Is.False);
            Close();Assert.That(State("ObjectiveText").ToString(),Does.Contain("Marta"));
            Call(marta,"InteractWithPlayer");yield return new WaitForSeconds(.1f);
            Call(Dialogue,"SkipLine");yield return null;yield return null;
            Assert.That(State("ThanksReceived"),Is.True);Close();
            Assert.That(State("ObjectiveText").ToString(),Does.Contain("zurück"));
            MoveTo(Station);Call(Station,"InteractWithPlayer");yield return WaitForPour();Close();yield return null;
            Assert.That(State("HasCan"),Is.False);Assert.That(State("ObjectiveText"),Is.Null);
            Assert.That(Vector3.Distance(GameObject.Find("Watering can model").transform.position,Station.transform.position),Is.LessThan(.7f));
            Capture("garden-task-complete");
            Call(marta,"InteractWithPlayer");yield return new WaitForSeconds(.1f);
            Assert.That(Dialogue.GetType().GetProperty("ActiveEmotion").GetValue(Dialogue).ToString(),Is.EqualTo("Friendly"));Close();
        }
        finally{evt.RemoveEventHandler(null,handler);}
        LogAssert.NoUnexpectedReceived();
    }
    static IEnumerator CloseUp(string name)
    {
        var camera=Camera.main;var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        var courtyard=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        bool follow=courtyard!=null && courtyard.enabled;
        bool enabled=brain!=null && brain.enabled;Vector3 position=camera.transform.position;
        if(courtyard!=null)courtyard.enabled=false;
        float fov=camera.fieldOfView;
        if(brain!=null)brain.enabled=false;
        camera.transform.position=Player.transform.position+Vector3.up*.1f-camera.transform.forward*5;
        camera.fieldOfView=35;
        yield return null;yield return null;Capture(name);
        camera.transform.position=position;camera.fieldOfView=fov;
        if(courtyard!=null)courtyard.enabled=follow;if(brain!=null)brain.enabled=enabled;
    }
    [UnityTest] public IEnumerator PaperCan_GripMirrors_AndPickUpPutDownCanBeCancelled()
    {
        yield return Load();
        var can=GameObject.Find("Watering can model").GetComponent<SpriteRenderer>();
        Assert.That(can,Is.Not.Null);Assert.That(can.sprite.name,Is.EqualTo("Garden-Can"));
        Assert.That(can.GetComponentsInChildren<MeshRenderer>(),Is.Empty,"The can is a flat painted sprite.");
        var visual=Player.GetComponent(T("PaperCharacterVisual"));
        foreach(float side in new[]{-1f,1f})
        {
            Vector3 p=Station.transform.position+Vector3.up+Camera.main.transform.right*(.65f*side);
            Player.transform.position=p;Player.GetComponent<Rigidbody>().position=p;Physics.SyncTransforms();
            Call(Station,"InteractWithPlayer");
            Assert.That(State("HasCan"),Is.False,"Ownership changes after the animation, not before the reach.");
            yield return new WaitForSeconds(.63f);
            yield return CloseUp(side<0?"garden-pick-up-right":"garden-pick-up-left");
            Assert.That(Vector3.Distance((Vector3)visual.GetType().GetProperty("GardenHandPosition").GetValue(visual),can.transform.position),Is.LessThan(.07f),"The gripping hand follows the handle in either direction.");
            Call(Station,"CancelWatering");yield return null;
            Assert.That(State("HasCan"),Is.False);Assert.That(State("IsBusy"),Is.False);
        }
        Call(Station,"InteractWithPlayer");yield return WaitForPour();
        yield return CloseUp("garden-carry-closeup");
        Call(Station,"InteractWithPlayer");yield return new WaitForSeconds(.65f);
        yield return CloseUp("garden-put-down-closeup");
        Call(Station,"CancelWatering");yield return null;
        Assert.That(State("HasCan"),Is.True,"Cancelling placement restores the carried can.");
        Assert.That(State("IsBusy"),Is.False);
        MoveTo(Beds[0]);Call(Beds[0],"InteractWithPlayer");yield return new WaitForSeconds(.65f);
        yield return CloseUp("garden-pour-closeup");
        Assert.That(Vector3.Distance((Vector3)visual.GetType().GetProperty("GardenHandPosition").GetValue(visual),can.transform.position),Is.LessThan(.07f));
        yield return WaitForPour();
        MoveTo(Station);Call(Station,"InteractWithPlayer");yield return WaitForPour();yield return null;
        Assert.That(State("HasCan"),Is.False);
        Assert.That(Player.GetComponentInChildren<SpriteRenderer>().forceRenderingOff,Is.False,"Normal appearance is restored after putting down the can.");
        Assert.That(Player.GetComponent<Rigidbody>().detectCollisions,Is.True);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator ApproachingCan_RespectsObstacles_AndRestoresPhysicsOnCancel()
    {
        yield return Load();
        var body=Player.GetComponent<Rigidbody>();
        Vector3 p=Station.transform.position+Vector3.up+Vector3.left*2;
        Player.transform.position=p;body.position=p;Physics.SyncTransforms();
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.transform.position=Station.transform.position+Vector3.up+Vector3.left*1.15f;
        blocker.transform.localScale=new Vector3(.3f,2,1);Physics.SyncTransforms();
        try
        {
            Call(Station,"InteractWithPlayer");yield return null;
            Assert.That(State("IsBusy"),Is.False);
            Assert.That(State("HasCan"),Is.False);
            Assert.That(Vector3.Distance(p,Player.transform.position),Is.LessThan(.01f));Close();
        }
        finally{UnityEngine.Object.Destroy(blocker);}
        yield return null;
        bool gravity=body.useGravity, collisions=body.detectCollisions;
        var interpolation=body.interpolation;
        Call(Station,"InteractWithPlayer");yield return new WaitForSeconds(.08f);
        Assert.That(State("IsBusy"),Is.True);
        Call(Station,"CancelWatering");yield return null;
        Assert.That(State("HasCan"),Is.False);Assert.That(State("IsBusy"),Is.False);
        Assert.That(body.useGravity,Is.EqualTo(gravity));Assert.That(body.detectCollisions,Is.EqualTo(collisions));
        Assert.That(body.interpolation,Is.EqualTo(interpolation));
        Assert.That(Player.GetComponent(T("PlayerController")).GetType().GetProperty("IsScriptedMoving").GetValue(Player.GetComponent(T("PlayerController"))),Is.False);
        LogAssert.NoUnexpectedReceived();
    }

}