using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardEastGroundTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest]public IEnumerator EasternLandscape_LeavesWalkingAndGardenClear_AfterReload()
    {
        yield return Load();
        var root=GameObject.Find("Eastern courtyard landscape");Assert.That(root,Is.Not.Null);
        Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty,"Landscape decoration must not add walking blockers.");
        Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Length,Is.GreaterThan(80));
        Assert.That(root.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        foreach(var plant in root.GetComponentsInChildren<SpriteRenderer>())
        {
            var p=plant.transform.position;
            Assert.That(p.x>24.0f && p.x<27.6f && p.z>6.4f && p.z<17.4f,Is.False,"No vegetation may obscure the cellar opening.");
            foreach(var bed in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed"))
                Assert.That(p.x>bed.bounds.min.x-.3f && p.x<bed.bounds.max.x+.3f && p.z>bed.bounds.min.z-.3f && p.z<bed.bounds.max.z+.3f,Is.False);
        }
        Assert.That(Physics.Raycast(new Vector3(30.3f,5,8.1f),Vector3.down,out var hill,10,~0,QueryTriggerInteraction.Ignore),Is.True);
        Assert.That(hill.collider.CompareTag("Ground"),Is.True);Assert.That(hill.point.y,Is.GreaterThan(.15f),"Meadow islands must have real, gentle relief.");
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        body.isKinematic=false;body.position=new Vector3(30,1.2f,7);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        for(int i=0;i<170 && body.position.z<12;i++){body.linearVelocity=new Vector3(0,body.linearVelocity.y,2.5f);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.z,Is.GreaterThan(11.8f),"The physical player must cross the gentle relief beside the cellar.");
        Assert.That(body.position.y,Is.GreaterThan(.7f));body.linearVelocity=Vector3.zero;body.isKinematic=true;
        var points=new[]{new Vector3(23,1,1),new Vector3(31,1,2),new Vector3(34,1,6.4f),new Vector3(40,1,-1)};
        var names=new[]{"garden-link","overview","house","gate"};
        for(int i=0;i<points.Length;i++){body.position=points[i];player.transform.position=points[i];yield return new WaitForSeconds(1.3f);Capture("east-landscape-"+names[i]);}
        yield return Load();Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardEastGroundStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(Physics.Raycast(new Vector3(30.3f,5,8.1f),Vector3.down,out hill,10,~0,QueryTriggerInteraction.Ignore),Is.True);
        Assert.That(hill.point.y,Is.InRange(.15f,.30f),"Reload must not accumulate terrain height.");
        LogAssert.NoUnexpectedReceived();
    }
}
