using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AurenfeldCloudShadowTests
{
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static void Capture(string name)=>typeof(AurenfeldFocusTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name});
    static double Difference(string first,string second)
    {
        string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??Path.GetFullPath("Builds");
        var a=new Texture2D(2,2);var b=new Texture2D(2,2);
        try
        {
            a.LoadImage(File.ReadAllBytes(Path.Combine(folder,first+".png")));
            b.LoadImage(File.ReadAllBytes(Path.Combine(folder,second+".png")));
            var x=a.GetPixels32();var y=b.GetPixels32();int count=x.Length/2;double total=0;
            for(int i=0;i<count;i++)total+=Math.Abs(x[i].r-y[i].r)+Math.Abs(x[i].g-y[i].g)+Math.Abs(x[i].b-y[i].b);
            return total/(count*3);
        }
        finally{UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);}
    }
    [UnityTest] public IEnumerator Clouds_ShadeAndMoveAcrossFrozenFields_AndReleaseOnDisable()
    {
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
        var point=GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout"));
        CourtyardGardenTaskTests.MoveTo(point);yield return null;
        point.GetType().GetMethod("InteractWithPlayer").Invoke(point,null);
        float deadline=Time.realtimeSinceStartup+16;
        while((float)point.GetType().GetProperty("Progress").GetValue(point)<.999f && Time.realtimeSinceStartup<deadline)yield return null;
        var style=GameObject.Find("Aurenfeld scenic morning").GetComponent(T("CourtyardScenicMorning"));
        var clock=style.GetType().GetField("weatherTime",BindingFlags.Instance|BindingFlags.NonPublic);
        var strength=style.GetType().GetField("cloudShadowStrength",BindingFlags.Instance|BindingFlags.NonPublic);
        var overlays=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        float previousScale=Time.timeScale;float previousTime=(float)clock.GetValue(style);
        float previousStrength=(float)strength.GetValue(style);
        foreach(var canvas in overlays)canvas.enabled=false;
        Time.timeScale=0;
        try
        {
            clock.SetValue(style,0f);strength.SetValue(style,0f);yield return null;yield return null;
            Capture("aurenfeld-clouds-off");
            strength.SetValue(style,previousStrength);yield return null;yield return null;
            Capture("aurenfeld-clouds-on");
            clock.SetValue(style,30f);yield return null;yield return null;
            Capture("aurenfeld-clouds-after-30s");
            double shade=Difference("aurenfeld-clouds-off","aurenfeld-clouds-on");
            double motion=Difference("aurenfeld-clouds-on","aurenfeld-clouds-after-30s");
            TestContext.WriteLine("Cloud shade RGB change: "+shade+"; 30-second drift change: "+motion);
            Assert.That(shade,Is.GreaterThan(3),"Cloud shade must be visible in the fields.");
            Assert.That(motion,Is.GreaterThan(1),"Clouds must travel while the camera, plants and particles remain frozen.");
            ((Behaviour)style).enabled=false;
            Assert.That(Shader.GetGlobalVector("_AurenfeldClouds"),Is.EqualTo(Vector4.zero));
            ((Behaviour)style).enabled=true;yield return null;
            Assert.That(Shader.GetGlobalVector("_AurenfeldClouds").z,Is.EqualTo(previousStrength));
        }
        finally
        {
            clock.SetValue(style,previousTime);strength.SetValue(style,previousStrength);Time.timeScale=previousScale;
            foreach(var canvas in overlays)if(canvas!=null)canvas.enabled=true;
        }
        point.GetType().GetMethod("ReturnToCourtyard").Invoke(point,null);
        LogAssert.NoUnexpectedReceived();
    }
}
