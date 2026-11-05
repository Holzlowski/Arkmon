using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardCrossingStyleTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator Crossing_RemainsWalkableBothWays_WithExteriorWaterAndClearApproach()
    {
        yield return Load();
        var style=GameObject.Find("Illustrated east crossing"); Assert.That(style,Is.Not.Null);
        Assert.That(style.GetComponentsInChildren<Collider>(),Is.Empty,"Decoration must not change bridge or gate collision.");
        Assert.That(style.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial!=null && r.sharedMaterial.shader.isSupported),Is.True);
        var arches=style.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="East gate carved arch").ToArray();
        Assert.That(arches.Length,Is.EqualTo(19));
        Assert.That(arches.All(f=>f.sharedMesh.normals[0].x<-.9f),Is.True,"The carved arch faces must be visible from inside the courtyard.");
        var water=GameObject.Find("Flowing exterior stream").GetComponent<MeshFilter>();
        Assert.That(water.sharedMesh.vertices.All(v=>v.x>43.6f && v.y<-.7f),Is.True,"The stream stays outside the wall and below the deck.");
        Assert.That(style.GetComponentsInChildren<SpriteRenderer>().All(r=>r.transform.position.z< -4.3f || r.transform.position.z>2.3f),Is.True);
        Assert.That(GameObject.Find("Drawbridge chain link"),Is.Null,"Retire the disconnected placeholder rods.");
        var fields=GameObject.Find("Eastern field approach"); Assert.That(fields,Is.Not.Null);
        Assert.That(fields.GetComponentsInChildren<Collider>(),Is.Empty);
        var road=GameObject.Find("Eastern winding field path").GetComponent<MeshFilter>();
        Assert.That(road.sharedMesh.normals.All(n=>n.y>.65f),Is.True,"The path must have upward faces and no sudden height jumps at the bank.");
        Assert.That(road.sharedMesh.bounds.max.x,Is.GreaterThan(145));
        var meadow=GameObject.Find("Castle horizon and golden fields").GetComponentInChildren(T("MeadowVegetation"));
        var surface=T("MeadowVegetation").GetMethod("SurfacePoint");
        foreach(float x in new[]{62f,80f,100f})
            Assert.That(((Vector3)surface.Invoke(meadow,new object[]{new Vector3(x,0,-1)})).y,Is.EqualTo(-.035f).Within(.002f));
        var body=GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        ((Behaviour)body.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        body.isKinematic=false;body.position=new Vector3(39.5f,1.2f,-1);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        yield return new WaitForSeconds(.5f);
        for(int i=0;i<360 && body.position.x<70;i++)
        {
            body.linearVelocity=new Vector3(5,body.linearVelocity.y,0);yield return new WaitForFixedUpdate();
            Assert.That(body.position.y,Is.GreaterThan(.45f),"No fall through the bridge or bank seam.");
        }
        Assert.That(body.position.x,Is.GreaterThan(69.7f));
        for(int i=0;i<360 && body.position.x>39.5f;i++)
        {body.linearVelocity=new Vector3(-5,body.linearVelocity.y,0);yield return new WaitForFixedUpdate();}
        string nearby=string.Join("; ",Physics.OverlapSphere(body.position,2,~0,QueryTriggerInteraction.Ignore).Select(c=>c.name+" bounds="+c.bounds));
        Assert.That(body.position.x,Is.LessThan(39.8f),"Return position="+body.position+" Nearby: "+nearby);Assert.That(body.position.y,Is.GreaterThan(.45f));
        body.linearVelocity=Vector3.zero;body.isKinematic=true;
        yield return new WaitForSeconds(1.2f);Capture("east-crossing-gameplay");
        var camera=Camera.main;var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        bool f=follow.enabled,b=brain!=null && brain.enabled;var pos=camera.transform.position;var rot=camera.transform.rotation;float fov=camera.fieldOfView;
        try
        {
            follow.enabled=false;if(brain!=null)brain.enabled=false;
            camera.fieldOfView=48;camera.transform.rotation=Quaternion.Euler(23,55,0);
            camera.transform.position=new Vector3(46,1.6f,-1)-camera.transform.forward*17;
            yield return null;yield return null;Capture("east-crossing-detail");
        }
        finally{camera.transform.SetPositionAndRotation(pos,rot);camera.fieldOfView=fov;follow.enabled=f;if(brain!=null)brain.enabled=b;}
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardEastFieldStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardGateStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(GameObject.Find("East drawbridge deck").GetComponent<Collider>().enabled,Is.True);
        Assert.That(Physics.Raycast(new Vector3(53,2,-1),Vector3.down,out var hit,4,~0,QueryTriggerInteraction.Ignore),Is.True);
        Assert.That(hit.collider.CompareTag("Ground"),Is.True);LogAssert.NoUnexpectedReceived();
    }
}
