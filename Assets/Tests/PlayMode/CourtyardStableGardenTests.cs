using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardStableGardenTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest]public IEnumerator CareToMainStreet_IsWalkable_PreservesBedsAndGroundContacts_OnReload()
    {
        yield return Load();
        var root=GameObject.Find("Stable garden connection");Assert.That(root,Is.Not.Null);
        Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
        Assert.That(root.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        var stable=GameObject.Find("Stable");
        var contacts=stable.transform.Find("Illustrated stable setting").GetComponentsInChildren<MeshFilter>().Where(f=>f.name.Contains("contact")).ToArray();
        Assert.That(contacts.Length,Is.GreaterThan(0));
        foreach(var f in contacts)foreach(var v in f.sharedMesh.vertices.Where((v,i)=>i%37==0))
        {
            var p=f.transform.TransformPoint(v);
            var heights=Physics.RaycastAll(p+Vector3.up*15,Vector3.down,30,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.CompareTag("Ground")).Select(h=>h.point.y).ToArray();
            Assert.That(heights.Length,Is.GreaterThan(0));
            Assert.That(p.y-heights.Max(),Is.InRange(.02f,.10f),"Ground contact must stay near the floor: "+f.name+" at "+p);
        }
        var beds=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed").ToArray();
        Assert.That(beds.Length,Is.EqualTo(3));
        Assert.That(beds.All(r=>r.bounds.min.x>27 && r.bounds.max.z< -4.5f),Is.True,"The kitchen beds stay clear of the castle approach at x=12.");
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var focus=(Behaviour)player.GetComponent(T("PlayerInteractionFocus"));focus.enabled=false;
        body.isKinematic=false;body.position=new Vector3(-10.8f,1,-.55f);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        yield return new WaitForSeconds(.8f);Capture("stable-garden-route-0");
        Vector3[] route={new Vector3(-7,0,-1.15f),new Vector3(-5.5f,0,-3.2f),new Vector3(-3,0,-3.5f)};
        for(int step=0;step<route.Length;step++)
        {
            float distance=100;
            for(int frame=0;frame<190;frame++)
            {
                var d=route[step]-body.position;d.y=0;distance=d.magnitude;if(distance<.18f)break;
                var velocity=d.normalized*3;body.linearVelocity=new Vector3(velocity.x,body.linearVelocity.y,velocity.z);
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocity=Vector3.zero;
            Assert.That(distance,Is.LessThan(.20f),"Blocked garden connection segment "+step+" at "+body.position);
            Assert.That(body.position.y,Is.GreaterThan(.5f));
            yield return new WaitForSeconds(.8f);Capture("stable-garden-route-"+(step+1));
        }
        body.isKinematic=true;focus.enabled=true;yield return null;
        Assert.That(Vector3.Distance(player.transform.position,GameObject.Find("Courtyard watering can").transform.position),Is.GreaterThan(20),"The old shortcut ends on the main street, not at a stale watering-can anchor.");
        var old=root;yield return Load();Assert.That(old==null,Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardStableGardenStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
