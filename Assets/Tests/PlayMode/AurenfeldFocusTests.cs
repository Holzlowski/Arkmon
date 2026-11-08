using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public class AurenfeldFocusTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string name)
    {
        var camera=Camera.main;float aspect=camera.aspect;
        var previous=RenderTexture.active;
        var target=new RenderTexture(1920,1080,24);target.Create();
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            camera.aspect=1920f/1080;
            // StandardRequest includes URP's normal volume update; SingleCameraRequest skips it.
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??Path.GetFullPath("Builds");
            Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
        }
        finally{camera.aspect=aspect;RenderTexture.active=previous;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(image);}
    }
    static void AssertMorningDifference()
    {
        string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??Path.GetFullPath("Builds");
        var before=new Texture2D(2,2);var after=new Texture2D(2,2);
        try
        {
            before.LoadImage(File.ReadAllBytes(Path.Combine(folder,"aurenfeld-morning-before.png")));
            after.LoadImage(File.ReadAllBytes(Path.Combine(folder,"aurenfeld-morning-after.png")));
            var a=before.GetPixels32();var b=after.GetPixels32();double difference=0;
            // Lower half is fields: changing only the sky must not satisfy this check.
            int count=a.Length/2;
            for(int i=0;i<count;i++)difference+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);
            TestContext.WriteLine("Morning landscape mean RGB change: "+difference/(count*3));
            Assert.That(difference/(count*3),Is.GreaterThan(2),"Morning shading must visibly affect the fields.");
        }
        finally{UnityEngine.Object.Destroy(before);UnityEngine.Object.Destroy(after);}
    }
    static void SetTilt(Component focus,bool enabled)=>focus.GetType().GetProperty("TiltShiftEnabled").SetValue(focus,enabled);
    static void AssertForegroundOrientation()
    {
        string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??Path.GetFullPath("Builds");
        var on=new Texture2D(2,2);var off=new Texture2D(2,2);
        try
        {
            on.LoadImage(File.ReadAllBytes(Path.Combine(folder,"aurenfeld-foreground-zero.png")));
            off.LoadImage(File.ReadAllBytes(Path.Combine(folder,"aurenfeld-tiltshift-off.png")));
            var a=on.GetPixels32();var b=off.GetPixels32();double difference=0;
            int count=a.Length/4;
            for(int i=0;i<count;i++)difference+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);
            TestContext.WriteLine("Zero foreground lower-quarter RGB difference: "+difference/(count*3));
            Assert.That(difference/(count*3),Is.LessThan(.5),"Foreground control must affect the bottom, not the sky.");
        }
        finally{UnityEngine.Object.Destroy(on);UnityEngine.Object.Destroy(off);}
    }
    static void AssertTiltDifference(float center)
    {
        string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??Path.GetFullPath("Builds");
        var on=new Texture2D(2,2);var off=new Texture2D(2,2);
        try
        {
            on.LoadImage(File.ReadAllBytes(Path.Combine(folder,"aurenfeld-postprocessing-on.png")));
            off.LoadImage(File.ReadAllBytes(Path.Combine(folder,"aurenfeld-tiltshift-off.png")));
            var a=on.GetPixels32();var b=off.GetPixels32();int changed=0;
            double bandDifference=0;int bandPixels=0;
            for(int i=0;i<a.Length;i++)
            {
                int d=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);
                if(d>9)changed++;
                float y=(i/on.width)/(float)on.height;
                if(Mathf.Abs(y-center)<.045f){bandDifference+=d;bandPixels++;}
            }
            TestContext.WriteLine("Tilt-shift changed pixels: "+changed+"; protected band mean difference: "+bandDifference/(bandPixels*3));
            Assert.That(changed,Is.GreaterThan(1000),"The actual camera pass must visibly soften the rendered view.");
            Assert.That(changed,Is.LessThan(a.Length*.65f),"Preserve the main landscape band.");
            Assert.That(bandDifference/(bandPixels*3),Is.LessThan(1),"Keep the farm band sharp.");
        }
        finally{UnityEngine.Object.Destroy(on);UnityEngine.Object.Destroy(off);}
    }
    [UnityTest] public IEnumerator ScenicFocus_ChangesImageAndCleansUpOnReturnDisableAndReload()
    {
        yield return Load();
        var point=GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout"));
        CourtyardGardenTaskTests.MoveTo(point);yield return null;Call(point,"InteractWithPlayer");
        var volume=GameObject.Find("Aurenfeld scenic focus").GetComponent<Volume>();
        Assert.That(volume.enabled,Is.False,"No abrupt blur at entry.");
        float deadline=Time.realtimeSinceStartup+16;
        while((float)point.GetType().GetProperty("Progress").GetValue(point)<.999f && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(volume.enabled,Is.True);
        Assert.That(volume.sharedProfile.TryGet<DepthOfField>(out var depth),Is.True);
        var camera=Camera.main;
        var farm=GameObject.Find("Aurenfeld distant farm");
        var focus=point.GetComponent(T("CourtyardLookoutFocus"));
        float center=Mathf.Clamp(camera.WorldToViewportPoint(farm.transform.position+Vector3.up*3).y,.35f,.65f);
        Assert.That(depth.mode.value,Is.EqualTo(DepthOfFieldMode.Off),"Physical DoF must not blur the paper farm.");
        Assert.That(volume.sharedProfile.TryGet<Bloom>(out var bloom),Is.True);
        Assert.That(volume.sharedProfile.TryGet<ColorAdjustments>(out var colors),Is.True);
        Assert.That(volume.sharedProfile.TryGet<ShadowsMidtonesHighlights>(out var tones),Is.True);
        var canvas=GameObject.Find("Field lookout overlay").GetComponent<Canvas>();
        Assert.That(canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay),"Scenic text stays outside post-processing.");
        // Keep the A/B comparison limited to scenery; gameplay UI remains screen-space overlay.
        var overlays=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        foreach(var overlay in overlays)overlay.enabled=false;
        float previousTimeScale=Time.timeScale;Time.timeScale=0;
        try
        {
            // Freeze world animation and use the complete render pipeline for each comparison.
            yield return null;yield return null;
            Capture("aurenfeld-postprocessing-on");
            SetTilt(focus,false);yield return null;yield return null;
            Capture("aurenfeld-tiltshift-off");
            AssertTiltDifference(center);
            SetTilt(focus,true);focus.GetType().GetProperty("ForegroundStrength").SetValue(focus,0f);
            yield return null;yield return null;
            Capture("aurenfeld-foreground-zero");
            AssertForegroundOrientation();
            focus.GetType().GetProperty("ForegroundStrength").SetValue(focus,.55f);SetTilt(focus,false);
            bloom.active=false;colors.active=false;tones.active=false;
            yield return null;yield return null;
            Capture("aurenfeld-postprocessing-off");
            var morning=(Behaviour)GameObject.Find("Aurenfeld scenic morning").GetComponent(T("CourtyardScenicMorning"));
            var warmSky=RenderSettings.skybox;
            Assert.That(Shader.GetGlobalFloat("_AurenfeldMorning"),Is.EqualTo(1));
            morning.enabled=false;
            Assert.That(Shader.GetGlobalFloat("_AurenfeldMorning"),Is.Zero);
            Assert.That(RenderSettings.skybox,Is.Not.SameAs(warmSky));
            yield return null;yield return null;
            Capture("aurenfeld-morning-before");
            morning.enabled=true;
            Assert.That(RenderSettings.skybox,Is.SameAs(warmSky));
            yield return null;yield return null;
            Capture("aurenfeld-morning-after");
            AssertMorningDifference();
        }
        finally
        {
            Time.timeScale=previousTimeScale;SetTilt(focus,true);focus.GetType().GetProperty("ForegroundStrength").SetValue(focus,.55f);
            bloom.active=true;colors.active=true;tones.active=true;
            foreach(var overlay in overlays)overlay.enabled=true;
        }
        Call(point,"ReturnToCourtyard");deadline=Time.realtimeSinceStartup+6;
        while((bool)T("CourtyardLookout").GetProperty("IsPresenting").GetValue(null) && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(volume.enabled,Is.False,"Returning must release scenic blur.");
        Assert.That((float)focus.GetType().GetProperty("Strength").GetValue(focus),Is.Zero);
        Call(point,"InteractWithPlayer");yield return new WaitForSeconds(.4f);
        ((Behaviour)point).enabled=false;yield return null;
        Assert.That(volume.enabled,Is.False,"Disabling the lookout must release the override.");
        Assert.That((float)focus.GetType().GetProperty("Strength").GetValue(focus),Is.Zero);
        var profile=volume.sharedProfile;
        var oldMorning=GameObject.Find("Aurenfeld scenic morning");
        var oldSky=RenderSettings.skybox;
        yield return Load();yield return null;
        Assert.That(oldMorning==null,Is.True);
        Assert.That(oldSky==null,Is.True,"Runtime sky material is disposed on reload.");
        Assert.That(Shader.GetGlobalFloat("_AurenfeldMorning"),Is.EqualTo(1),"New courtyard owns the light setup.");
        Assert.That(volume==null,Is.True);Assert.That(profile==null,Is.True,"Temporary profile is disposed on scene unload.");
        Assert.That(GameObject.Find("Aurenfeld scenic focus"),Is.Null);
        Capture("aurenfeld-courtyard-morning");
        LogAssert.NoUnexpectedReceived();
    }
}
