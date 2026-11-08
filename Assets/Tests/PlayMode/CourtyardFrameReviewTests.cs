using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Repeatable editor-only comparison. Frame intervals include editor overhead, not a standalone GPU benchmark.
public class CourtyardFrameReviewTests
{
    [Serializable] public class Sample {public string view; public double medianMs,p95Ms;public int frames;}
    [Serializable] public class Report {public string device,unity,scope;public int width=1920,height=1080;public Sample[] samples;}
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest] public IEnumerator FixedRoute_RecordsUncappedFrameIntervals()
    {
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
        var camera=Camera.main;var originalTarget=camera.targetTexture;float originalAspect=camera.aspect,originalTime=Time.timeScale;
        int sync=QualitySettings.vSyncCount,limit=Application.targetFrameRate;
        var target=new RenderTexture(1920,1080,24);target.Create();
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
        var samples=new List<Sample>();
        try
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            camera.targetTexture=target;camera.aspect=16f/9;
            foreach(var canvas in canvases)if(canvas.renderMode==RenderMode.ScreenSpaceOverlay)
            {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.15f;}
            var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();body.isKinematic=true;
            ((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
            var points=new[]{new Vector3(-34,2.4f,8),new Vector3(-35.9f,1,-10.2f),new Vector3(27.3f,1,-15.85f)};
            var names=new[]{"tower","workshop","garden"};
            for(int i=0;i<points.Length;i++)
            {
                Time.timeScale=1;body.position=player.transform.position=points[i];Physics.SyncTransforms();
                yield return new WaitForSeconds(1.5f);Time.timeScale=0;
                for(int w=0;w<45;w++)yield return null;
                var times=new List<double>();double previous=Time.realtimeSinceStartupAsDouble;
                for(int frame=0;frame<240;frame++)
                {
                    yield return null;double now=Time.realtimeSinceStartupAsDouble;
                    times.Add((now-previous)*1000);previous=now;
                }
                times.Sort();samples.Add(new Sample{view=names[i],medianMs=times[times.Count/2],p95Ms=times[(int)(times.Count*.95)],frames=times.Count});
            }
            var report=new Report{device=SystemInfo.graphicsDeviceName,unity=Application.unityVersion,
                scope="Silent PlayMode editor; fixed 1920x1080 target, vsync off, 45 warmup + 240 samples per view; includes editor/OS overhead.",samples=samples.ToArray()};
            string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??"Builds";
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"frame-review.json"),JsonUtility.ToJson(report,true));
            TestContext.WriteLine(JsonUtility.ToJson(report,true));
            Assert.That(samples.All(s=>s.medianMs>0 && !double.IsNaN(s.medianMs)),Is.True);
        }
        finally
        {
            Time.timeScale=originalTime;QualitySettings.vSyncCount=sync;Application.targetFrameRate=limit;
            if(camera!=null){camera.targetTexture=originalTarget;camera.aspect=originalAspect;}
            for(int i=0;i<canvases.Length;i++)if(canvases[i]!=null){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
            target.Release();UnityEngine.Object.Destroy(target);
        }
        LogAssert.NoUnexpectedReceived();
    }
}