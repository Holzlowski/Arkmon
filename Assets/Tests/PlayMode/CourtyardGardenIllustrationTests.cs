using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardGardenIllustrationTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator GardenStyle_PreservesWateringApproaches_AndReloadsCleanly()
    {
        yield return Load();
        var house=GameObject.Find("Garden cottage").transform;
        Assert.That(GameObject.Find("Fountain basin"),Is.Null,"The fountain must no longer render or collide in the garden.");
        Assert.That(GameObject.Find("Bench seat"),Is.Null,"The large front bench is retired.");
        var seat=GameObject.Find("Marta side bench");Assert.That(seat,Is.Not.Null);
        foreach(var own in seat.GetComponentsInChildren<Collider>())
        foreach(var other in Physics.OverlapBox(own.bounds.center,own.bounds.extents,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
        {
            if(other.transform.IsChildOf(seat.transform) || other.CompareTag("Ground"))continue;
            Assert.That(Physics.ComputePenetration(own,own.transform.position,own.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out _),Is.False,"Seat overlaps "+other.name);
        }
        Assert.That(Physics.BoxCast(house.position+new Vector3(0,1,-6.2f),new Vector3(.3f,.7f,.3f),Vector3.forward,
            out var doorHit,Quaternion.identity,2.8f,~0,QueryTriggerInteraction.Ignore),Is.False,"Cottage approach blocked by "+doorHit.collider?.name);
        var ground=GameObject.Find("Marta worn work ground");Assert.That(ground,Is.Not.Null);
        Assert.That(ground.GetComponentsInChildren<Collider>(),Is.Empty);
        var decoration=house.Find("Illustrated garden cottage");Assert.That(decoration,Is.Not.Null);
        Assert.That(decoration.GetComponentsInChildren<Collider>(),Is.Empty);
        var roof=house.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Marta curved tiled roof");
        Assert.That(roof.sharedMaterial.shader.name,Is.EqualTo("Arkmon/Courtyard Illustrated Surface"));
        Assert.That(roof.sharedMaterial.shader.isSupported,Is.True);
        Assert.That(house.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name=="Illustrated garden herbs"),Is.GreaterThan(10));
        var soil=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed").ToArray();
        Assert.That(soil.Length,Is.EqualTo(3));
        Assert.That(soil.All(r=>r.sharedMaterial.shader.name!="Arkmon/Courtyard Illustrated Surface"),Is.True,"Watering retains ownership of soil shading.");
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        foreach(var station in UnityEngine.Object.FindObjectsByType(T("CourtyardGarden"),FindObjectsSortMode.None).Cast<Component>())
        {
            var approach=station.transform.position+new Vector3(0,1,-.6f);
            Assert.That(Physics.BoxCast(approach+Vector3.back,new Vector3(.28f,.65f,.28f),Vector3.forward,
                out var hit,Quaternion.identity,1,~0,QueryTriggerInteraction.Ignore),Is.False,"Garden approach blocked by "+hit.collider?.name);
            player.transform.position=approach;body.position=approach;Physics.SyncTransforms();yield return null;
            var focus=player.GetComponent(T("PlayerInteractionFocus"));
            Assert.That(focus.GetType().GetMethod("Refresh").Invoke(focus,null),Is.SameAs(station));
        }
        var prompt=(Behaviour)player.GetComponent(T("PlayerInteractionFocus"));prompt.enabled=false;
        player.transform.position=house.position+new Vector3(0,1,-6.3f);body.position=player.transform.position;
        yield return new WaitForSeconds(1.3f);Capture("garden-illustration-gameplay");
        var camera=Camera.main;var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        bool f=follow.enabled,b=brain!=null && brain.enabled;var pos=camera.transform.position;var angle=camera.transform.rotation;float fov=camera.fieldOfView;
        try
        {
            follow.enabled=false;if(brain!=null)brain.enabled=false;
            camera.fieldOfView=40;camera.transform.rotation=Quaternion.Euler(25,0,0);
            camera.transform.position=house.position+new Vector3(0,1.35f,-2.3f)-camera.transform.forward*12;
            yield return null;yield return null;Capture("garden-illustration-detail");
            camera.transform.position=new Vector3(13.4f,.5f,4)-camera.transform.forward*19;
            yield return null;yield return null;Capture("garden-illustration-beds");
        }
        finally
        {
            camera.transform.SetPositionAndRotation(pos,angle);camera.fieldOfView=fov;
            follow.enabled=f;if(brain!=null)brain.enabled=b;prompt.enabled=true;
        }
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardGardenStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardGardenPlace"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(GameObject.Find("Fountain basin"),Is.Null);
        Assert.That(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Marta side bench"),Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
