using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardCellarEntranceTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest]public IEnumerator Cellar_StairsAreWalkable_ClosedDoorAndInspectionSurviveReload()
    {
        yield return Load();
        var root=GameObject.Find("Illustrated cellar entrance");Assert.That(root,Is.Not.Null);
        var at=root.transform.position;
        Assert.That(GameObject.Find("Cellar storehouse").GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True,"No invisible old storehouse collision should remain.");
        Assert.That(Physics.Raycast(at+new Vector3(0,6,2),Vector3.down,out var roof,7,~0,QueryTriggerInteraction.Ignore),Is.True);
        Assert.That(roof.point.y,Is.GreaterThanOrEqualTo(-.05f),"The buried vault must remain covered by courtyard ground.");
        Assert.That(root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Cellar oak plank").Max(r=>r.bounds.max.y),Is.LessThan(0),"The full door must lie below courtyard level.");
        Assert.That(at.x,Is.EqualTo(25.8f).Within(.01f));Assert.That(at.z,Is.EqualTo(16.9f).Within(.01f));
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var entrance=UnityEngine.Object.FindObjectsByType(T("CourtyardEntrance"),FindObjectsSortMode.None).Cast<Component>().Single(e=>e.GetType().GetField("destination").GetValue(e).ToString()=="Cellar");
        Assert.That(root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).All(r=>r.sharedMaterial!=null && r.sharedMaterial.shader.isSupported),Is.True);
        Assert.That(Physics.Raycast(at+new Vector3(0,3,-.9f),Vector3.down,out var floor,8,~0,QueryTriggerInteraction.Ignore),Is.True);
        Assert.That(floor.collider.name,Is.EqualTo("Cellar lower landing"),"Old ground must not cover the excavated stair.");
        Assert.That(floor.point.y,Is.EqualTo(-3.95f).Within(.02f));
        Assert.That(Physics.Raycast(at+new Vector3(2.4f,3,-2.8f),Vector3.down,out var outside,6,~0,QueryTriggerInteraction.Ignore),Is.True);
        Assert.That(outside.point.y,Is.GreaterThanOrEqualTo(-.01f),"Excavation must preserve the neighboring courtyard.");
        body.isKinematic=false;body.position=at+new Vector3(0,1,-10.9f);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        for(int i=0;i<350 && body.position.z<at.z-1;i++){body.linearVelocity=new Vector3(0,body.linearVelocity.y,2);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.z,Is.GreaterThan(at.z-1.3f),"Player must reach the bottom without catching a riser.");
        body.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.25f);
        Assert.That(body.position.y,Is.LessThan(-2.9f),"Elian must physically descend below the courtyard.");
        var focus=player.GetComponent(T("PlayerInteractionFocus"));
        Assert.That(focus.GetType().GetMethod("Refresh").Invoke(focus,null),Is.SameAs(entrance));
        yield return null;
        var ring=(LineRenderer)focus.GetType().GetField("ring",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(focus);
        Assert.That(ring.GetPosition(0).y,Is.EqualTo(-3.915f).Within(.02f),"The interaction marker belongs on the underground landing.");
        yield return new WaitForSeconds(1.2f);Capture("cellar-underground-bottom-gameplay");
        entrance.GetType().GetMethod("InteractWithPlayer").Invoke(entrance,null);yield return null;
        var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
        Assert.That(dialogue.GetType().GetProperty("IsOpen").GetValue(dialogue),Is.True);
        Assert.That(dialogue.GetType().GetProperty("ActiveSpeaker").GetValue(dialogue),Is.EqualTo("Elian"));
        dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        for(int i=0;i<55;i++){body.linearVelocity=new Vector3(0,body.linearVelocity.y,2);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.z,Is.LessThan(at.z-.25f),"Closed door must block further movement.");
        for(int i=0;i<350 && body.position.z>at.z-10.9f;i++){body.linearVelocity=new Vector3(0,body.linearVelocity.y,-2);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.z,Is.LessThan(at.z-10.7f),"Player must be able to leave the stairwell.");
        Assert.That(body.position.y,Is.GreaterThan(-.1f));
        body.linearVelocity=Vector3.zero;body.isKinematic=true;((Behaviour)focus).enabled=false;
        body.position=at+new Vector3(-1,1,-10.9f);player.transform.position=body.position;yield return new WaitForSeconds(1.4f);Capture("cellar-underground-gameplay");
        var camera=Camera.main;var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        follow.enabled=false;if(brain!=null)brain.enabled=false;camera.fieldOfView=43;camera.transform.rotation=Quaternion.Euler(34,-6,0);camera.transform.position=at+new Vector3(0,-1,-4)-camera.transform.forward*18;
        yield return null;yield return null;Capture("cellar-underground-detail");
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardCellarEntranceStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(Physics.Raycast(at+new Vector3(0,3,-.9f),Vector3.down,out floor,8,~0,QueryTriggerInteraction.Ignore),Is.True);
        Assert.That(floor.collider.name,Is.EqualTo("Cellar lower landing"));
        LogAssert.NoUnexpectedReceived();
    }
}
