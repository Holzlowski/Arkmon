using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardRenderingTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static string Folder=>Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??Path.GetFullPath(Path.Combine(Application.dataPath,"..","Builds"));
    [UnityTest] public IEnumerator InstancedPaint_PreservesRenderedScaleTintLightingAndShadows()
    {
        yield return Load();
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        player.transform.position=new Vector3(-10.8f,1,-.55f);body.position=player.transform.position;Physics.SyncTransforms();
        yield return new WaitForSeconds(1.5f);
        var materials=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Select(r=>r.sharedMaterial).Where(m=>m!=null && m.shader.name=="Arkmon/Courtyard Illustrated Surface").Distinct().ToArray();
        Assert.That(materials.Length,Is.GreaterThan(10));Assert.That(materials.All(m=>m.enableInstancing),Is.True);
        var a=new Texture2D(2,2);var b=new Texture2D(2,2);
        try
        {
            Capture("instancing-warmup");Capture("instancing-on");
            foreach(var m in materials)m.enableInstancing=false;
            Capture("instancing-off-warmup");Capture("instancing-off");
            a.LoadImage(File.ReadAllBytes(Path.Combine(Folder,"instancing-on.png")));
            b.LoadImage(File.ReadAllBytes(Path.Combine(Folder,"instancing-off.png")));
            var pixels=a.GetPixels32();var reference=b.GetPixels32();int changed=0;
            for(int i=0;i<pixels.Length;i++)if(Mathf.Abs(pixels[i].r-reference[i].r)+Mathf.Abs(pixels[i].g-reference[i].g)+Mathf.Abs(pixels[i].b-reference[i].b)>12)changed++;
            TestContext.WriteLine("Instancing changed pixels: "+changed+" / "+pixels.Length);
            Assert.That(changed/(float)pixels.Length,Is.LessThan(.005f),"Batching must preserve the painted scene, including object scale, tint and shadows.");
        }
        finally{foreach(var m in materials)if(m!=null)m.enableInstancing=true;UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);}
        LogAssert.NoUnexpectedReceived();
    }
}
