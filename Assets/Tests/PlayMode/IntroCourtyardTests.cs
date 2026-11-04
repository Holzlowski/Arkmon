using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class IntroCourtyardTests
{
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static void Capture(string name)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name});
    [UnityTest]public IEnumerator CourtyardRoutesBoundariesCameraAndTowerReturn()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.7f);
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var controller=(Behaviour)player.GetComponent(T("PlayerController"));controller.enabled=false;
        Assert.That(GameObject.Find("Courtyard walkable ground").GetComponent<Collider>().bounds.size.x,Is.GreaterThan(85));
        var entrances=UnityEngine.Object.FindObjectsByType(T("CourtyardEntrance"),FindObjectsSortMode.None);
        Assert.That(entrances.Length,Is.EqualTo(4));
        Assert.That(Physics.BoxCast(new Vector3(-38,1,-1),new Vector3(.35f,.8f,.35f),Vector3.right,out var hit,Quaternion.identity,76,~0,QueryTriggerInteraction.Ignore),Is.False,"The cross-courtyard route must stay open: "+hit.collider?.name);
        var camera=Camera.main;Quaternion angle=camera.transform.rotation;float fov=camera.fieldOfView;
        body.position=new Vector3(-34,2.4f,8);Physics.SyncTransforms();yield return new WaitForSeconds(.8f);Capture("intro-courtyard-west");
        body.position=new Vector3(9,1,1);Physics.SyncTransforms();yield return new WaitForSeconds(.8f);Capture("intro-courtyard-centre");
        body.position=new Vector3(41,1,-1);Physics.SyncTransforms();yield return new WaitForSeconds(.8f);Capture("intro-courtyard-east");
        // Review the complete architecture and terrain without changing the gameplay camera setup.
        var savedPosition=camera.transform.position;
        camera.transform.position=new Vector3(-3,20,-59);
        camera.transform.LookAt(new Vector3(-3,6,9));camera.fieldOfView=64;
        Capture("intro-courtyard-architecture-overview");
        camera.transform.SetPositionAndRotation(savedPosition,angle);camera.fieldOfView=fov;
        Assert.That(Quaternion.Angle(angle,camera.transform.rotation),Is.LessThan(.01));Assert.That(camera.fieldOfView,Is.EqualTo(fov));
        body.position=new Vector3(40,1,5);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        for(int i=0;i<80;i++){body.linearVelocity=new Vector3(7,body.linearVelocity.y,0);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.x,Is.LessThan(42.7f),"The outer wall stops the player.");Assert.That(body.position.y,Is.GreaterThan(-.5f));body.linearVelocity=Vector3.zero;
        var gardenType=T("CourtyardGarden");
        var can=GameObject.Find("Courtyard watering can").GetComponent(gardenType);
        var closeDialogue=T("DialogueBoxController").GetMethod("CloseDialogue");
        var dialogueController=T("DialogueBoxController").GetField("instance").GetValue(null);
        closeDialogue.Invoke(dialogueController,null);
        CourtyardGardenTaskTests.MoveTo(can);
        gardenType.GetMethod("InteractWithPlayer").Invoke(can,null);
        yield return CourtyardGardenTaskTests.WaitForPour();
        closeDialogue.Invoke(dialogueController,null);
        var beds=UnityEngine.Object.FindObjectsByType(gardenType,FindObjectsSortMode.None)
            .Cast<Component>().Where(point=>point.name=="Garden watering point").ToArray();
        Assert.That(beds.Length,Is.EqualTo(3));
        foreach(var bed in beds)
        {
            CourtyardGardenTaskTests.MoveTo(bed);
            gardenType.GetMethod("InteractWithPlayer").Invoke(bed,null);
            yield return CourtyardGardenTaskTests.WaitForPour();
        }
        Assert.That(gardenType.GetProperty("Completed").GetValue(null),Is.True);
        body.position=new Vector3(9,1,1);Physics.SyncTransforms();
        yield return new WaitForSeconds(.8f);Capture("intro-courtyard-watered");
        body.position=new Vector3(47,1,-1);Physics.SyncTransforms();
        yield return new WaitForSeconds(.8f);Capture("intro-courtyard-bridge");
        body.position=new Vector3(54,1,-1);Physics.SyncTransforms();
        yield return new WaitForSeconds(.8f);Capture("intro-courtyard-far-bank");
        var stream=GameObject.Find("Spatial stream ambience").GetComponent<AudioSource>();
        Assert.That(stream.clip,Is.Not.Null);
        Assert.That(stream.loop,Is.True);
        Assert.That(stream.spatialBlend,Is.EqualTo(1f));
        var tower=entrances.First(e=>e.GetType().GetField("destination").GetValue(e).ToString()=="Tower");
        var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        tower.GetType().GetMethod("InteractWithPlayer").Invoke(tower,null);
        float deadline=Time.realtimeSinceStartup+8;
        while(SceneManager.GetActiveScene().name!="TowerRoom" && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("TowerRoom"));yield return new WaitForSeconds(.8f);
        var door=UnityEngine.Object.FindObjectsByType(T("DemoInteractable"),FindObjectsSortMode.None).Cast<Component>().First(d=>d.GetType().GetField("kind").GetValue(d).ToString()=="RoomDoor");
        Assert.That(Vector3.Distance(GameObject.FindWithTag("Player").transform.position,door.transform.position),Is.LessThan(2.2f));
        Assert.That(T("IntroChapter").GetProperty("HasFamilyOutfit").GetValue(null),Is.True);LogAssert.NoUnexpectedReceived();
    }
}
