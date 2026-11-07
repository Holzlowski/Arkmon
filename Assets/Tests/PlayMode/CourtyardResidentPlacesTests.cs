using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardResidentPlacesTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator RelocatedGardenAndWorkingYards_AreReachableAndReloadCleanly()
    {
        yield return Load();
        var house=GameObject.Find("Garden cottage").transform;
        Assert.That(house.position.x,Is.EqualTo(24).Within(.01f));Assert.That(house.position.z,Is.EqualTo(-8).Within(.01f));
        var beds=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed").ToArray();
        Assert.That(beds.Length,Is.EqualTo(3));
        Assert.That(beds.All(b=>b.bounds.min.x>27 && b.bounds.max.z< -4.5f),Is.True);
        var homeBody=house.GetComponentsInChildren<Collider>().Where(c=>c.enabled && !c.isTrigger).ToArray();
        foreach(var bed in beds)foreach(var solid in homeBody)
            Assert.That(solid.bounds.Intersects(bed.bounds),Is.False,"House and vegetables must not overlap.");
        Assert.That(GameObject.Find("Ida working yard"),Is.Not.Null);Assert.That(GameObject.Find("Hannes small paddock"),Is.Not.Null);
        Assert.That(GameObject.Find("Marta kitchen garden fence"),Is.Not.Null);
        Assert.That(Vector3.Distance(GameObject.Find("Courtyard Gardener").transform.position,house.Find("Marta work spot").position),Is.LessThan(.1f));
        Assert.That(Physics.OverlapBox(new Vector3(4,1.2f,6),new Vector3(6,.7f,3),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)
            .Any(c=>!c.CompareTag("Ground")),Is.False,"The former cottage and garden site must be an open castle forecourt.");
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        ((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        body.isKinematic=false;body.position=new Vector3(12,1,7);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        yield return new WaitForSeconds(.8f);Capture("resident-places-castle-square");
        var route=new[]{new Vector3(12,0,-1),new Vector3(21,0,-2.9f),new Vector3(35,0,-2),new Vector3(35.4f,0,-12.05f),new Vector3(27.3f,0,-12.05f),new Vector3(27.3f,0,-15.85f)};
        float speed=Time.timeScale;Time.timeScale=3;
        try
        {
            foreach(var target in route)
            {
                float distance=100;
                for(int frame=0;frame<330;frame++)
                {
                    var d=target-body.position;d.y=0;distance=d.magnitude;if(distance<.18f)break;
                    var velocity=d.normalized*3;body.linearVelocity=new Vector3(velocity.x,body.linearVelocity.y,velocity.z);
                    yield return new WaitForFixedUpdate();
                }
                body.linearVelocity=Vector3.zero;
                Assert.That(distance,Is.LessThan(.20f),"Blocked garden route to "+target+" at "+body.position);
            }
        }
        finally{Time.timeScale=speed;body.linearVelocity=Vector3.zero;body.isKinematic=true;}
        yield return new WaitForSeconds(1);Capture("resident-places-marta-garden");
        foreach(var pair in new[]{(new Vector3(-35.9f,1,-10.3f),"ida"),(new Vector3(-18,1,-4),"hannes"),(new Vector3(12,1,-8),"overview")})
        {
            body.position=player.transform.position=pair.Item1;Physics.SyncTransforms();yield return new WaitForSeconds(1);Capture("resident-places-"+pair.Item2);
        }
        var old=GameObject.Find("Resident working yards");yield return Load();Assert.That(old==null,Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardResidentPlaces"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(GameObject.Find("Garden cottage").transform.position,Is.EqualTo(house==null?new Vector3(24,0,-8):house.position));
        LogAssert.NoUnexpectedReceived();
    }
}
