using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardEastCottageTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator EastCottage_PreservesApproachGateAndCellarAccess()
    {
        yield return Load();
        var house=GameObject.Find("East cottage");var at=house.transform.position;
        var style=house.transform.Find("Illustrated east cottage");Assert.That(style,Is.Not.Null);
        Assert.That(house.transform.Find("East cottage structure").GetComponent<Collider>().enabled,Is.True);
        Assert.That(house.transform.Find("East cottage structure").GetComponent<MeshRenderer>().sharedMaterial.name,Is.EqualTo("Plaster"));
        Assert.That(style.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        Assert.That(style.GetComponentsInChildren<Collider>().Count(c=>c.enabled),Is.EqualTo(1),"Only the matching closed door adds a solid.");
        Assert.That(Physics.BoxCast(at+new Vector3(0,1,-6),new Vector3(.35f,.75f,.25f),Vector3.forward,out var hit,Quaternion.identity,2.8f,~0,QueryTriggerInteraction.Ignore),Is.False,"Door approach blocked by "+hit.collider?.name);
        Assert.That(Physics.Raycast(at+new Vector3(0,1,-3.5f),Vector3.forward,out hit,2,~0,QueryTriggerInteraction.Ignore),Is.True);
        Assert.That(hit.collider.name,Is.EqualTo("East cottage closed door collision"));
        // The main eastward passage and the gap beside the cellar remain free.
        Assert.That(Physics.BoxCast(new Vector3(29,1,2),new Vector3(.35f,.75f,.3f),Vector3.right,out hit,Quaternion.identity,11,~0,QueryTriggerInteraction.Ignore),Is.False,"East route blocked by "+hit.collider?.name);
        Assert.That(Physics.BoxCastAll(new Vector3(30,1,7),new Vector3(.35f,.75f,.3f),Vector3.forward,Quaternion.identity,5,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.CompareTag("Ground")),Is.False,"Cellar side route must remain free of solid props; the terrain may undulate.");
        foreach(var slab in style.GetComponentsInChildren<Transform>().Where(t=>t.name=="East cottage approach stone"))
        {
            var ground=Physics.RaycastAll(slab.position+Vector3.up*3,Vector3.down,6,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.CompareTag("Ground")).OrderByDescending(h=>h.point.y).First();
            Assert.That(slab.position.y-ground.point.y,Is.EqualTo(.028f).Within(.01f),"Flagstones must follow the meadow instead of disappearing into it.");
        }
        var cap=GameObject.Find("Cellar portal earth retaining cap");Assert.That(cap,Is.Not.Null);Assert.That(cap.GetComponent<Collider>().enabled,Is.True);
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();var focus=player.GetComponent(T("PlayerInteractionFocus"));((Behaviour)focus).enabled=false;
        body.position=at+new Vector3(-1,1,-6.6f);player.transform.position=body.position;yield return new WaitForSeconds(1.4f);Capture("east-cottage-gameplay");
        var camera=Camera.main;var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        follow.enabled=false;if(brain!=null)brain.enabled=false;camera.fieldOfView=42;camera.transform.rotation=Quaternion.Euler(21,-12,0);camera.transform.position=at+new Vector3(0,2.8f,-1)-camera.transform.forward*16;
        yield return null;yield return null;Capture("east-cottage-detail");
        yield return Load();Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardEastCottageStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(GameObject.Find("Illustrated east cottage").GetComponentsInChildren<Transform>().Count(t=>t.name=="East cottage framed window"),Is.EqualTo(3));
        LogAssert.NoUnexpectedReceived();
    }
}
