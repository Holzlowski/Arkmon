using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TowerChapterFinishTests
{
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object P(string name)=>T("DemoOpening").GetProperty(name).GetValue(null);
    static void Capture(string name)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name});
    static void Close(){var d=T("DialogueBoxController").GetField("instance").GetValue(null);d.GetType().GetMethod("CloseDialogue").Invoke(d,null);}
    [UnityTest]public IEnumerator WakeDressDoorArrivalAndTownRemainCoherent()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("TowerRoom");
        var wakingBed=GameObject.Find("Canopy bed").GetComponent(T("PaintedCanopyBed"));
        yield return new WaitForSecondsRealtime(.85f);
        Assert.That(wakingBed.GetType().GetProperty("IsSleeping").GetValue(wakingBed),Is.True);
        var quilt=(GameObject)wakingBed.GetType().GetField("sleepingQuilt",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(wakingBed);
        Assert.That(quilt,Is.Not.Null,"The tower scene must reference the sleeping quilt.");
        Assert.That(quilt.activeInHierarchy,Is.True);
        Capture("tower-wake-covered-sleep");
        float seatedDeadline=Time.realtimeSinceStartup+4;
        while(!(bool)wakingBed.GetType().GetProperty("IsSeated").GetValue(wakingBed)&&Time.realtimeSinceStartup<seatedDeadline)yield return null;
        Assert.That(wakingBed.GetType().GetProperty("IsSeated").GetValue(wakingBed),Is.True);
        yield return new WaitForSecondsRealtime(.24f);
        Capture("tower-wake-bedside-seat");
        var seated=GameObject.Find("Boy seated at bed edge").transform;
        Assert.That(seated.localPosition.x,Is.InRange(1.15f,1.30f),"The seat is aligned with the mattress edge.");
        float deadline=Time.realtimeSinceStartup+8;
        while(!(bool)P("HasAwakened")&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(P("HasAwakened"),Is.True);Close();
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var bed=GameObject.Find("Canopy bed").GetComponent(T("PaintedCanopyBed"));
        var stand=(Vector3)bed.GetType().GetProperty("StandingPosition").GetValue(bed);
        Assert.That(Vector3.Distance(player.transform.position,stand),Is.LessThan(.25f));
        Assert.That(body.useGravity&&body.detectCollisions,Is.True,"Waking restores exploration physics.");
        Assert.That(GameObject.Find("First town beyond the golden fields"),Is.Null);
        Assert.That(GameObject.Find("Distant field hills").GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap").name,
            Is.EqualTo("TowerVista-Fields"),"Keep the village already painted in the original countryside.");
        var door=GameObject.Find("Courtyard door").GetComponent(T("TowerRoomDoor"));
        Assert.That(door,Is.Not.Null);Assert.That(GameObject.Find("Door carved stone arch"),Is.Not.Null);
        Assert.That(GameObject.Find("Door green oak planks"),Is.Not.Null);
        yield return new WaitForSeconds(.3f);
        Capture("tower-intro-finished-morning");
        var camera=Camera.main;Quaternion rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        var window=UnityEngine.Object.FindObjectsByType(T("DemoInteractable"),FindObjectsSortMode.None)
            .Single(item=>item.GetType().GetField("kind").GetValue(item).ToString()=="Window");
        Vector3 roomPosition=camera.transform.position;
        window.GetType().GetMethod("InteractWithPlayer").Invoke(window,null);
        yield return new WaitForSecondsRealtime(1.2f);
        Assert.That(Quaternion.Angle(camera.transform.rotation, Quaternion.identity), Is.LessThan(.2f));
        Assert.That(camera.transform.position.z,Is.GreaterThan(roomPosition.z+4));
        Capture("tower-window-closeup");
        Close();yield return new WaitForSecondsRealtime(1.2f);
        Assert.That(camera.fieldOfView,Is.EqualTo(fov).Within(.15f));
        Assert.That(Vector3.Distance(roomPosition,camera.transform.position),Is.LessThan(.15f));
        foreach(var position in new[]{new Vector3(-4,1,-4),new Vector3(4,1,-4),new Vector3(1,1,1),(Vector3)door.GetType().GetProperty("ApproachPosition").GetValue(door)})
        {
            body.position=position;body.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForSeconds(.35f);
            var viewport=camera.WorldToViewportPoint(player.transform.position+Vector3.up*.25f);
            Assert.That(viewport.x,Is.InRange(.05f,.95f));Assert.That(viewport.y,Is.InRange(.05f,.95f));
            Assert.That(Quaternion.Angle(rotation,camera.transform.rotation),Is.LessThan(.01f));Assert.That(camera.fieldOfView,Is.EqualTo(fov));
        }
        Capture("tower-intro-3d-door");
        var controller=(Behaviour)player.GetComponent(T("PlayerController"));controller.enabled=false;
        body.position=new Vector3(-4,1,-4);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        for(int i=0;i<40;i++){body.linearVelocity=new Vector3(-5,body.linearVelocity.y,0);yield return new WaitForFixedUpdate();}
        body.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.3f);
        Assert.That(camera.WorldToViewportPoint(player.transform.position+Vector3.up*.25f).x,Is.GreaterThan(.05f),"The cutaway boundary keeps the player inside the fixed view.");
        Assert.That(body.position.x,Is.LessThan(-4.1f),"The boundary check actually moved the physics body.");controller.enabled=true;
        var wardrobe=GameObject.Find("Wardrobe");body.position=wardrobe.transform.position+new Vector3(0,1,-1.6f);Physics.SyncTransforms();
        T("DemoOpening").GetMethod("TakeMantle").Invoke(null,null);
        deadline=Time.realtimeSinceStartup+7;
        while((bool)P("IsPresenting")&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(T("IntroChapter").GetProperty("HasFamilyOutfit").GetValue(null),Is.True);
        Assert.That(P("HasLamp"),Is.False);Assert.That(P("HasMantle"),Is.False);Close();
        var hint=wardrobe.GetComponent(T("QuestHint"));yield return null;
        Assert.That(hint.GetType().GetProperty("IsVisible").GetValue(hint),Is.False);
        body.position=(Vector3)door.GetType().GetProperty("ApproachPosition").GetValue(door);Physics.SyncTransforms();
        T("DemoOpening").GetMethod("LeaveRoom").Invoke(null,null);
        yield return new WaitForSeconds(.7f);
        Assert.That((float)door.GetType().GetProperty("Openness").GetValue(door),Is.GreaterThan(.2f));
        deadline=Time.realtimeSinceStartup+8;
        while(SceneManager.GetActiveScene().name!="StartScene"&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("StartScene"));
        while(((bool)P("IsPresenting")||(bool)T("SceneLoadManager").GetProperty("IsTransitioning").GetValue(null))&&Time.realtimeSinceStartup<deadline)yield return null;
        Close();yield return new WaitForSeconds(.2f);
        player=GameObject.FindWithTag("Player");body=player.GetComponent<Rigidbody>();var entrance=GameObject.Find("Tower entrance").transform;
        Assert.That(Mathf.Abs(player.transform.position.x-entrance.position.x),Is.LessThan(.15f));
        Assert.That(entrance.position.z-player.transform.position.z,Is.InRange(1.5f,2.0f));
        Assert.That(body.useGravity&&body.detectCollisions,Is.True,"The arrival restores normal collisions and gravity.");
        Assert.That(player.GetComponent(T("PlayerController")).GetType().GetProperty("IsScriptedMoving").GetValue(player.GetComponent(T("PlayerController"))),Is.False);
        Capture("tower-intro-courtyard-arrival");
        var gardenType=T("CourtyardGarden");
        var points=UnityEngine.Object.FindObjectsByType(gardenType,FindObjectsSortMode.None).Cast<Component>().ToArray();
        var can=points.Single(point=>point.name=="Courtyard watering can");
        var beds=points.Where(point=>point.name=="Garden watering point").ToArray();
        Assert.That(beds.Length,Is.EqualTo(3));
        var label=gardenType.GetProperty("Label");
        var interact=gardenType.GetMethod("InteractWithPlayer");
        interact.Invoke(beds[0],null);Close();
        Assert.That(label.GetValue(beds[0]),Is.EqualTo("Trockenes Beet ansehen"));
        CourtyardGardenTaskTests.MoveTo(can);interact.Invoke(can,null);yield return CourtyardGardenTaskTests.WaitForPour();Close();
        foreach(var gardenBed in beds)
        {
            Assert.That(label.GetValue(gardenBed),Is.EqualTo("Beet gießen"));
            CourtyardGardenTaskTests.MoveTo(gardenBed);interact.Invoke(gardenBed,null);yield return CourtyardGardenTaskTests.WaitForPour();
            Assert.That(label.GetValue(gardenBed),Is.EqualTo("Beet ansehen"));
        }
        LogAssert.NoUnexpectedReceived();
    }
}
