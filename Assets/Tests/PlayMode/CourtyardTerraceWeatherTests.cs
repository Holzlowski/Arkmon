using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardTerraceWeatherTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(AurenfeldFocusTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static int ChangedAirPixels()
    {
        string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??Path.GetFullPath("Builds");
        var on=new Texture2D(2,2);var off=new Texture2D(2,2);
        try
        {
            on.LoadImage(File.ReadAllBytes(Path.Combine(folder,"aurenfeld-weather-start.png")));
            off.LoadImage(File.ReadAllBytes(Path.Combine(folder,"aurenfeld-weather-no-air-effects.png")));
            var a=on.GetPixels32();var b=off.GetPixels32();int changed=0;
            for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>24)changed++;
            TestContext.WriteLine("Visible dust pixels: "+changed);return changed;
        }
        finally{UnityEngine.Object.Destroy(on);UnityEngine.Object.Destroy(off);}
    }
    [UnityTest] public IEnumerator Gallery_HasContinuousWalkableFloor_AndUnobstructedWindows()
    {
        yield return Load();Physics.SyncTransforms();
        for(float x=-28.5f;x<=-22;x+=1.3f)for(float z=11.5f;z<=15.4f;z+=.65f)
        {
            Assert.That(Physics.Raycast(new Vector3(x,2,z),Vector3.down,out var hit,1.3f,~0,QueryTriggerInteraction.Ignore),Is.True,"Missing terrace floor at "+x+","+z);
            Assert.That(hit.point.y,Is.EqualTo(1.4f).Within(.025f),"Floor must join the existing terrace without a pit.");
        }
        var wing=GameObject.Find("Tower connecting wing").transform;
        var wall=GameObject.Find("Castle west wing structure").GetComponent<Renderer>().bounds;
        foreach(Transform window in wing)
            if(window.name=="Arched residence window")
                foreach(var r in window.GetComponentsInChildren<Renderer>())
                    Assert.That(r.bounds.max.x,Is.LessThan(wall.min.x-.2f),"Gallery window is hidden by the projecting wing.");
        var body=GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        body.isKinematic=false;body.position=new Vector3(-25.5f,2.5f,10.4f);Physics.SyncTransforms();
        yield return new WaitForSeconds(.4f);
        foreach(float target in new[]{14.7f,10.4f})
        {
            float deadline=Time.realtimeSinceStartup+4;
            while(Mathf.Abs(body.position.z-target)>.15f && Time.realtimeSinceStartup<deadline)
            {
                body.linearVelocity=new Vector3(0,body.linearVelocity.y,Mathf.Sign(target-body.position.z)*2);
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocity=Vector3.zero;
            Assert.That(Mathf.Abs(body.position.z-target),Is.LessThan(.2f),"Player must be able to walk into and back out of the former trap.");
            Assert.That(body.position.y,Is.GreaterThan(1.8f));
        }
        yield return new WaitForSeconds(.6f);Capture("terrace-repaired-gameplay");
        LogAssert.NoUnexpectedReceived();
    }
}
