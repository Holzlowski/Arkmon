using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardBreachTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest]public IEnumerator Ruin_KeepsViewingApproachOpen_AndPreventsLeavingCourtyard()
    {
        yield return Load();
        var ruin=GameObject.Find("Illustrated west breach");Assert.That(ruin,Is.Not.Null);
        Assert.That(ruin.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        Assert.That(GameObject.Find("Breach grass blade"),Is.Null,"Retire the primitive foliage instead of layering more props over it.");
        Assert.That(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="Side battlement" && t.position.x<-42 && t.position.z>3.5f && t.position.z<10.5f),Is.False,"No floating battlements above the opening.");
        var meadow=(Component)UnityEngine.Object.FindFirstObjectByType(T("MeadowVegetation"));
        var near=(Vector3)meadow.GetType().GetMethod("SurfacePoint").Invoke(meadow,new object[]{new Vector3(-45.5f,0,7)});
        Assert.That(near.y,Is.InRange(-.06f,.05f),"The exterior field must meet the lower meadow, not start as a floating cliff.");
        var ground=GameObject.Find("Castle horizon and golden fields").GetComponentsInChildren<MeshFilter>()
            .First(f=>f.sharedMesh!=null && f.sharedMesh.name=="Western fields gentle transition");
        Assert.That(ground.sharedMesh.vertices.Select(v=>ground.transform.TransformPoint(v)).Where(v=>v.x>-47 && v.x<-42 && v.z>2 && v.z<12)
            .All(v=>v.y<.15f),Is.True,"Flatten the clipped ground edge and its neighboring vertices consistently.");
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        ((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        body.isKinematic=false;body.position=new Vector3(-40.3f,1.4f,1.5f);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        for(int i=0;i<160 && body.position.z<6.8f;i++) {body.linearVelocity=new Vector3(0,body.linearVelocity.y,3);yield return new WaitForFixedUpdate();}
        Capture("west-breach-walking-approach");
        Assert.That(body.position.z,Is.GreaterThan(6.6f),"The player must reach the viewing place without catching rubble.");
        for(int i=0;i<100;i++){body.linearVelocity=new Vector3(-3,body.linearVelocity.y,0);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.x,Is.GreaterThan(-42.7f),"The visible low wall must keep Elian inside the courtyard.");
        Assert.That(body.position.y,Is.GreaterThan(.7f));body.linearVelocity=Vector3.zero;body.isKinematic=true;
        yield return new WaitForSeconds(1.3f);Capture("west-breach-gameplay");
        body.position=new Vector3(-34,2.4f,7.8f);player.transform.position=body.position;
        yield return new WaitForSeconds(1.3f);Capture("west-breach-tower-approach");
        var camera=Camera.main;var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        bool f=follow.enabled,b=brain!=null && brain.enabled;var pos=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        try
        {
            follow.enabled=false;if(brain!=null)brain.enabled=false;
            camera.fieldOfView=48;camera.transform.rotation=Quaternion.Euler(19,-48,0);
            camera.transform.position=new Vector3(-42.8f,2.4f,7)-camera.transform.forward*14;
            yield return null;yield return null;Capture("west-breach-detail");
        }
        finally{camera.transform.SetPositionAndRotation(pos,rotation);camera.fieldOfView=fov;follow.enabled=f;if(brain!=null)brain.enabled=b;}
        yield return Load();Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardBreachStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(GameObject.Find("Broken west wall base").GetComponent<Collider>().enabled,Is.True);
        LogAssert.NoUnexpectedReceived();
    }
}
