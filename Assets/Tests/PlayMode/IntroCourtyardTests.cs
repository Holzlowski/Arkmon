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
        Assert.That(Quaternion.Angle(angle,camera.transform.rotation),Is.LessThan(.01));Assert.That(camera.fieldOfView,Is.EqualTo(fov));
        body.position=new Vector3(40,1,5);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        for(int i=0;i<80;i++){body.linearVelocity=new Vector3(7,body.linearVelocity.y,0);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.x,Is.LessThan(42.7f),"The outer wall stops the player.");Assert.That(body.position.y,Is.GreaterThan(-.5f));body.linearVelocity=Vector3.zero;
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
