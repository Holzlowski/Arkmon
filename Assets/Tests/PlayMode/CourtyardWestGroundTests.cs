using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardWestGroundTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest]public IEnumerator TowerToWorkshop_RemainsWalkable_WithGroundedDressingAndReload()
    {
        yield return Load();
        var details=GameObject.Find("Western courtyard ground details");Assert.That(details,Is.Not.Null);
        Assert.That(details.GetComponentsInChildren<Collider>(),Is.Empty,"Scenic dressing must not create new obstacles.");
        Assert.That(details.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        var contacts=details.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.Contains("contact")).ToArray();
        Assert.That(contacts.Length,Is.GreaterThan(0));
        foreach(var f in contacts)foreach(var v in f.sharedMesh.vertices.Where((v,i)=>i%19==0))
        {
            var p=f.transform.TransformPoint(v);
            var hits=Physics.RaycastAll(p+Vector3.up*15,Vector3.down,30,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.CompareTag("Ground") && h.normal.y>.5f).ToArray();
            Assert.That(hits.Length,Is.GreaterThan(0));
            Assert.That(p.y-hits.Max(h=>h.point.y),Is.InRange(.025f,.065f),"Contacts must conform to the visible terrain, not float above the hill.");
        }
        var westPlants=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.name.StartsWith("Verge ") || r.name.StartsWith("West verge "));
        foreach(var r in westPlants)
        {
            var p=r.transform.position;
            Assert.That(p.x> -37.5f && p.x< -30.5f && p.z>3.7f,Is.False,"No plants on the tower stairs: "+p);
            Assert.That(p.x> -24.4f && p.x< -22.4f && p.z> -1.8f && p.z<2.6f,Is.False,"Ida's doorstep stays visible: "+p);
        }
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        ((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        body.isKinematic=false;body.position=new Vector3(-34,2.4f,8);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        Vector3[] route={new Vector3(-34,0,4.2f),new Vector3(-30,0,1.3f),new Vector3(-26,0,-1.7f),new Vector3(-23.4f,0,-1.6f),new Vector3(-23.4f,0,1.1f)};
        for(int step=0;step<route.Length;step++)
        {
            float distance=100;
            for(int frame=0;frame<220;frame++)
            {
                var d=route[step]-body.position;d.y=0;distance=d.magnitude;if(distance<.18f)break;
                var velocity=d.normalized*3;body.linearVelocity=new Vector3(velocity.x,body.linearVelocity.y,velocity.z);
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocity=Vector3.zero;
            Assert.That(distance,Is.LessThan(.20f),"Blocked walking segment "+step+" at "+body.position);
            Assert.That(body.position.y,Is.GreaterThan(.5f));
            yield return new WaitForSeconds(.8f);Capture("western-route-"+step);
        }
        body.isKinematic=true;
        var old=details;
        yield return Load();Assert.That(old==null,Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardWestGroundStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
