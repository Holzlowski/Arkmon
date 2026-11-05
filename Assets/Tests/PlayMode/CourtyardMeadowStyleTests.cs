using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public class CourtyardMeadowStyleTests
{
    static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    static IEnumerator Load() => (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
    static void Capture(string n) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { n });
    static double Percentile(List<double> values, float p) { var ordered=values.OrderBy(v=>v).ToArray(); return ordered[Mathf.Clamp(Mathf.FloorToInt((ordered.Length-1)*p),0,ordered.Length-1)]; }
    static string Folder => Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") ?? Path.GetFullPath(Path.Combine(Application.dataPath,"..","Builds"));

    [UnityTest] public IEnumerator RootContacts_RenderWithoutSunShadows_AndShareTheGrassLod()
    {
        yield return Load();
        var meadow=GameObject.Find("Castle horizon and golden fields").GetComponentInChildren(T("MeadowVegetation"));
        var toggle=T("MeadowVegetation").GetProperty("NearShadowsEnabled");toggle.SetValue(meadow,false);
        var contacts=meadow.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Grass root contacts").ToArray();
        Assert.That(contacts.Length,Is.InRange(1,100));
        foreach(var contact in contacts)
        {
            Assert.That(contact.sharedMaterial.shader.isSupported,Is.True);
            Assert.That(contact.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));
            var lods=contact.GetComponentInParent<LODGroup>().GetLODs();
            Assert.That(lods[0].renderers.Contains(contact),Is.True);Assert.That(lods[1].renderers.Contains(contact),Is.False);
            Assert.That(contact.GetComponent<Collider>(),Is.Null);
        }
        var follow=UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));var view=T("CourtyardCamera").GetMethod("SetScenicView");
        view.Invoke(follow,new object[]{new Vector3(-76,12,6),Quaternion.LookRotation(new Vector3(-1,-.18f,.38f)),58f});
        yield return new WaitForSeconds(.3f);
        Capture("aurenfeld-grass-contact-on");
        foreach(var contact in contacts)contact.enabled=false;
        Capture("aurenfeld-grass-contact-off");
        var on=new Texture2D(2,2);var off=new Texture2D(2,2);int darker=0;
        try
        {
            on.LoadImage(File.ReadAllBytes(Path.Combine(Folder,"aurenfeld-grass-contact-on.png")));
            off.LoadImage(File.ReadAllBytes(Path.Combine(Folder,"aurenfeld-grass-contact-off.png")));
            var a=on.GetPixels32();var b=off.GetPixels32();
            for(int y=0;y<on.height/2;y++)for(int x=0;x<on.width;x++)
            {int n=y*on.width+x;if(b[n].r+b[n].g+b[n].b-a[n].r-a[n].g-a[n].b>12)darker++;}
        }
        finally {UnityEngine.Object.Destroy(on);UnityEngine.Object.Destroy(off);}
        TestContext.WriteLine("Grass root contact pixels without sun shadows: "+darker);
        Assert.That(darker,Is.GreaterThan(500),"Roots must visibly darken the ground independently of long sun shadows.");
        foreach(var contact in contacts)contact.enabled=true;
        toggle.SetValue(meadow,true);yield return null;yield return null;
        Capture("aurenfeld-grass-contact-combined");
        view.Invoke(follow,new object[]{new Vector3(-61,11,7),Quaternion.LookRotation(new Vector3(-69,0,20)),58f});
        yield return null;yield return null;Capture("aurenfeld-grass-contact-panorama");
        T("CourtyardCamera").GetMethod("ClearScenicView").Invoke(follow,null);
        int count=contacts.Length;yield return Load();
        var fresh=GameObject.Find("Castle horizon and golden fields").GetComponentInChildren(T("MeadowVegetation"));
        Assert.That(fresh.GetComponentsInChildren<MeshRenderer>().Count(r=>r.name=="Grass root contacts"),Is.EqualTo(count));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator Meadow_NearShadowsRenderAndRemainBounded_WithTimingComparison()
    {
        yield return Load();
        var meadow=GameObject.Find("Castle horizon and golden fields").GetComponentInChildren(T("MeadowVegetation"));
        var toggle=T("MeadowVegetation").GetProperty("NearShadowsEnabled");
        var renderers=meadow.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Vegetation LOD")).ToArray();
        Assert.That(renderers.Length,Is.EqualTo(200));
        Assert.That(renderers.All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        Assert.That(renderers.All(r=>r.sharedMaterial.GetFloat("_FieldStyle")>.9f),Is.True);
        var follow=UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var view=T("CourtyardCamera").GetMethod("SetScenicView");
        view.Invoke(follow,new object[]{new Vector3(-61,11,7),Quaternion.LookRotation(new Vector3(-69,0,20)),58f});
        yield return new WaitForSeconds(.3f);Capture("aurenfeld-meadow-panorama");
        // Close field review makes the small grass silhouettes measurable at 1080p.
        var position=new Vector3(-76,12,6);
        view.Invoke(follow,new object[]{position,Quaternion.LookRotation(new Vector3(-1,-.18f,.38f)),58f});
        yield return new WaitForSeconds(.4f);
        var casters=renderers.Where(r=>r.shadowCastingMode!=ShadowCastingMode.Off).ToArray();
        Assert.That(casters.Length,Is.InRange(1,4));
        Assert.That(casters.All(r=>r.name=="Vegetation LOD0" && r.bounds.SqrDistance(position)<36*36),Is.True);
        Capture("aurenfeld-meadow-shadows");
        toggle.SetValue(meadow,false);
        Assert.That(renderers.All(r=>r.shadowCastingMode==ShadowCastingMode.Off),Is.True);
        Capture("aurenfeld-meadow-no-shadows");
        var on=new Texture2D(2,2);var off=new Texture2D(2,2);int darker=0;
        try
        {
            on.LoadImage(File.ReadAllBytes(Path.Combine(Folder,"aurenfeld-meadow-shadows.png")));
            off.LoadImage(File.ReadAllBytes(Path.Combine(Folder,"aurenfeld-meadow-no-shadows.png")));
            var a=on.GetPixels32();var b=off.GetPixels32();
            for(int i=0;i<a.Length;i++) if(b[i].r+b[i].g+b[i].b-a[i].r-a[i].g-a[i].b>12)darker++;
        }
        finally { UnityEngine.Object.Destroy(on);UnityEngine.Object.Destroy(off); }
        TestContext.WriteLine("Visible grass-shadow pixels: "+darker);
        Assert.That(darker,Is.GreaterThan(100),"Near grass shadows must visibly reach the ground, not merely have a render flag.");
        // Fixed-resolution, uncapped editor A/B/B/A sample. This is diagnostic, not a standalone-player benchmark.
        var camera=Camera.main;var previousTarget=camera.targetTexture;float previousAspect=camera.aspect;
        int sync=QualitySettings.vSyncCount,rate=Application.targetFrameRate;
        var target=new RenderTexture(1920,1080,24);target.Create();camera.targetTexture=target;camera.aspect=1920f/1080;
        var enabledTimes=new List<double>();var disabledTimes=new List<double>();
        try
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            foreach(bool enabled in new[]{true,false,false,true})
            {
                toggle.SetValue(meadow,enabled);
                for(int i=0;i<20;i++)yield return null;
                for(int i=0;i<80;i++){yield return null;(enabled?enabledTimes:disabledTimes).Add(Time.unscaledDeltaTime*1000.0);}
            }
        }
        finally
        {
            camera.targetTexture=previousTarget;camera.aspect=previousAspect;target.Release();UnityEngine.Object.Destroy(target);
            QualitySettings.vSyncCount=sync;Application.targetFrameRate=rate;toggle.SetValue(meadow,true);
        }
        string report=$"Unity Editor, 1920x1080 camera target; uncapped A/B/B/A, 160 samples each. GPU: {SystemInfo.graphicsDeviceName}.\n"+
            $"Near shadows ON: median {Percentile(enabledTimes,.5f):F2} ms, p95 {Percentile(enabledTimes,.95f):F2} ms.\n"+
            $"Near shadows OFF: median {Percentile(disabledTimes,.5f):F2} ms, p95 {Percentile(disabledTimes,.95f):F2} ms.\n"+
            $"Shadow-enabled chunks: {casters.Length}; changed dark pixels: {darker}. Editor frame intervals include CPU, GPU and test overhead; no standalone FPS guarantee.\n";
        File.WriteAllText(Path.Combine(Folder,"aurenfeld-meadow-timing.txt"),report);TestContext.WriteLine(report);
        view.Invoke(follow,new object[]{new Vector3(60,6,0),Quaternion.LookRotation(new Vector3(1,-.1f,.2f)),58f});
        yield return null;yield return null;
        Assert.That(casters.All(r=>r.shadowCastingMode==ShadowCastingMode.Off),Is.True,"Western shadow chunks must retire when moving to the east.");
        T("CourtyardCamera").GetMethod("ClearScenicView").Invoke(follow,null);
        yield return Load();
        Assert.That(GameObject.Find("Castle horizon and golden fields").GetComponentInChildren(T("MeadowVegetation")).GetComponentsInChildren<LODGroup>().Length,Is.EqualTo(100));
        LogAssert.NoUnexpectedReceived();
    }
}
