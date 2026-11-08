using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardCloudRayTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static void Capture(string n)=>typeof(AurenfeldFocusTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static Color32[] Pixels(string n)
    {
        var texture=new Texture2D(2,2);
        try
        {
            var folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??Path.GetFullPath("Builds");
            texture.LoadImage(File.ReadAllBytes(Path.Combine(folder,n+".png")));return texture.GetPixels32();
        }
        finally{UnityEngine.Object.Destroy(texture);}
    }
    static int[] Difference(string on,string off)
    {
        var a=Pixels(on);var b=Pixels(off);var delta=new int[a.Length];
        for(int i=0;i<a.Length;i++)delta[i]=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);
        return delta;
    }
    static bool Covered(MeshFilter surface,Ray ray,float far)
    {
        var v=surface.sharedMesh.vertices.Select(surface.transform.TransformPoint).ToArray();
        var indices=surface.sharedMesh.triangles;
        for(int i=0;i<indices.Length;i+=3)
        {
            var a=v[indices[i]];var b=v[indices[i+1]];var c=v[indices[i+2]];
            var normal=Vector3.Cross(b-a,c-a);
            var plane=new Plane(a,b,c);
            if(!plane.Raycast(ray,out float distance) || distance>far)continue;
            var hit=ray.GetPoint(distance);
            if(Vector3.Dot(Vector3.Cross(b-a,hit-a),normal)>=-.01f &&
               Vector3.Dot(Vector3.Cross(c-b,hit-b),normal)>=-.01f &&
               Vector3.Dot(Vector3.Cross(a-c,hit-c),normal)>=-.01f)return true;
        }
        return false;
    }
    [UnityTest] public IEnumerator CloudGapLight_IsVisibleMovesAndCleansUp()
    {
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
        var root=GameObject.Find("Aurenfeld cloud-gap sunlight");Assert.That(root,Is.Not.Null);
        var effect=(Behaviour)root.GetComponent(T("CourtyardCloudSunrays"));
        var renderers=root.GetComponentsInChildren<MeshRenderer>();Assert.That(renderers.Length,Is.EqualTo(2));
        Assert.That(renderers.All(r=>!r.enabled && r.sharedMaterial.shader.isSupported),Is.True);
        var material=renderers[0].sharedMaterial;var meshes=root.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).ToArray();
        var point=GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout"));
        CourtyardGardenTaskTests.MoveTo(point);yield return new WaitForSeconds(2);
        Capture("flight-00-courtyard");
        point.GetType().GetMethod("InteractWithPlayer").Invoke(point,null);
        float deadline=Time.realtimeSinceStartup+18;
        int stage=1;
        while((float)point.GetType().GetProperty("Progress").GetValue(point)<.999f && Time.realtimeSinceStartup<deadline)
        {
            if((float)point.GetType().GetProperty("Progress").GetValue(point)>stage*.12f && stage<=7)
            { Capture("flight-"+stage.ToString("D2")); stage++; }
            yield return null;
        }
        Assert.That((float)point.GetType().GetProperty("Progress").GetValue(point),Is.GreaterThan(.99f));
        yield return new WaitForSeconds(2);
        // Check the actual camera rays at both display edges, including ultrawide.
        var camera=Camera.main;
        var mountain=GameObject.Find("Western painted mountains").GetComponent<MeshFilter>();
        float aspect=camera.aspect;
        try
        {
            foreach(float ratio in new[]{16f/9f,21f/9f})
            {
                camera.aspect=ratio;
                foreach(float edge in new[]{0f,1f})
                    Assert.That(Covered(mountain,camera.ViewportPointToRay(new Vector3(edge,.5f,0)),camera.farClipPlane),Is.True,"Mountain scenery must cover both frame edges.");
            }
            // The turning camera can face any direction between courtyard and lookout.
            foreach(var origin in new[]{new Vector3(-40,30,0),new Vector3(-61,30,7)})
                for(int yaw=0;yaw<360;yaw+=5)
                    Assert.That(Covered(mountain,new Ray(origin,Quaternion.Euler(0,yaw,0)*Vector3.forward),camera.farClipPlane),Is.True,"Missing horizon during turn at "+yaw);
        }
        finally{camera.aspect=aspect;}
        var weather=GameObject.Find("Aurenfeld scenic morning").GetComponent(T("CourtyardScenicMorning"));
        var clock=weather.GetType().GetField("weatherTime",BindingFlags.NonPublic|BindingFlags.Instance);
        float previous=Time.timeScale;Time.timeScale=0;
        try
        {
            clock.SetValue(weather,0f);yield return null;yield return null;
            Capture("aurenfeld-cloud-rays-on");foreach(var r in renderers)r.enabled=false;Capture("aurenfeld-cloud-rays-off");foreach(var r in renderers)r.enabled=true;
            var start=Difference("aurenfeld-cloud-rays-on","aurenfeld-cloud-rays-off");
            int visible=start.Count(d=>d>9);TestContext.WriteLine("Visible ray pixels: "+visible);
            Assert.That(visible,Is.InRange(10000,(int)(start.Length*.65f)),"Light must be visible without washing out the whole panorama.");
            clock.SetValue(weather,45f);yield return null;yield return null;
            Capture("aurenfeld-cloud-rays-after-45s");foreach(var r in renderers)r.enabled=false;Capture("aurenfeld-cloud-rays-off-45s");foreach(var r in renderers)r.enabled=true;
            var end=Difference("aurenfeld-cloud-rays-after-45s","aurenfeld-cloud-rays-off-45s");
            Assert.That(start.Where((value,i)=>Math.Abs(value-end[i])>9).Count(),Is.GreaterThan(1000),"Ray illumination must follow the changing weather mask.");
            effect.enabled=false;Assert.That(renderers.All(r=>!r.enabled),Is.True);effect.enabled=true;
        }
        finally{Time.timeScale=previous;}
        point.GetType().GetMethod("ReturnToCourtyard").Invoke(point,null);yield return new WaitForSeconds(5);
        Assert.That(renderers.All(r=>!r.enabled),Is.True,"No lingering scenic light after return.");
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
        Assert.That(material==null && meshes.All(m=>m==null),Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardCloudSunrays"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
