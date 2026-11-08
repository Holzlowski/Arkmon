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
    [UnityTest] public IEnumerator Weather_DriftsContinuously_AndDustReleasesAfterLookout()
    {
        yield return Load();
        var oldClouds=UnityEngine.Object.FindObjectsByType(T("CloudDrift"),FindObjectsSortMode.None).Cast<Component>()
            .SelectMany(c=>c.GetComponentsInChildren<Renderer>()).Select(r=>(renderer:r,enabled:r.enabled)).ToArray();
        var point=GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout"));
        CourtyardGardenTaskTests.MoveTo(point);yield return null;
        point.GetType().GetMethod("InteractWithPlayer").Invoke(point,null);
        float deadline=Time.realtimeSinceStartup+16;
        while((float)point.GetType().GetProperty("Progress").GetValue(point)<.999f && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That((float)point.GetType().GetProperty("Progress").GetValue(point),Is.GreaterThan(.99f));
        Assert.That(oldClouds.All(c=>!c.renderer.enabled),Is.True,"The old foreground cloud must not cover the painted panorama.");
        var seeds=GameObject.Find("Western sunlit seeds").GetComponent<ParticleSystem>();
        Assert.That(seeds.particleCount,Is.GreaterThan(30),"The lookout needs a visible population of sunlit motes.");
        var sky=GameObject.Find("Western painted horizon").GetComponent(T("CourtyardLookoutLandscape"));
        var cloudClock=sky.GetType().GetField("cloudTime",BindingFlags.NonPublic|BindingFlags.Instance);
        var cloud=GameObject.Find("Western drifting clouds").transform;
        var weather=GameObject.Find("Aurenfeld scenic morning").GetComponent(T("CourtyardScenicMorning"));
        var weatherClock=weather.GetType().GetField("weatherTime",BindingFlags.NonPublic|BindingFlags.Instance);
        Assert.That(GameObject.Find("Aurenfeld wind strokes"),Is.Null,"Drawn wind ribbons were removed.");
        float old=Time.timeScale;Time.timeScale=0;
        try
        {
            cloudClock.SetValue(sky,0f);weatherClock.SetValue(weather,0f);
            yield return null;yield return null;
            Vector3 start=cloud.position;Vector4 shade=Shader.GetGlobalVector("_AurenfeldClouds");
            Capture("aurenfeld-weather-start");
            var seedRenderer=seeds.GetComponent<ParticleSystemRenderer>();seedRenderer.enabled=false;
            Capture("aurenfeld-weather-no-air-effects");
            seedRenderer.enabled=true;
            Assert.That(ChangedAirPixels(),Is.GreaterThan(500),"Luminous dust must be visibly rendered, not merely an active component.");
            cloudClock.SetValue(sky,45f);weatherClock.SetValue(weather,45f);
            yield return null;yield return null;Capture("aurenfeld-weather-after-45s");
            Assert.That(Vector3.Distance(start,cloud.position),Is.InRange(110f,145f),"Painted clouds need visible but gentle drift over 45 seconds.");
            var motion=Shader.GetGlobalVector("_AurenfeldClouds")-shade;
            Assert.That(new Vector2(motion.x,motion.y).magnitude,Is.InRange(45f,65f),"Field shadows must drift slowly rather than race across crops.");
            cloudClock.SetValue(sky,300f);yield return null;
            var fiveMinutes=Camera.main.WorldToScreenPoint(cloud.position);
            cloudClock.SetValue(sky,310f);yield return null;
            float pixels=(Camera.main.WorldToScreenPoint(cloud.position)-fiveMinutes).magnitude;
            Assert.That(pixels,Is.GreaterThan(10),"Background cloud movement must remain visible after several minutes, without a stationary turnaround.");
        }
        finally{Time.timeScale=old;}
        point.GetType().GetMethod("ReturnToCourtyard").Invoke(point,null);yield return new WaitForSeconds(4);
        Assert.That(oldClouds.All(c=>c.renderer.enabled==c.enabled),Is.True,"Return must restore the previous courtyard clouds.");
        Assert.That(seeds.particleCount,Is.Zero);
        yield return Load();
        Assert.That(GameObject.Find("Aurenfeld wind strokes"),Is.Null);
        LogAssert.NoUnexpectedReceived();
    }
}
