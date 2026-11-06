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
    [UnityTest] public IEnumerator LocalPalette_RestoresResourcesAndLeavesSoilAndCollisionAlone()
    {
        yield return Load();
        var stone=Resources.Load<Material>("CourtyardStyle/Stone");var plaster=Resources.Load<Material>("CourtyardStyle/Plaster");
        float scale=stone.GetFloat("_TextureScale");Color color=plaster.GetColor("_BaseColor");
        var root=GameObject.Find("Courtyard coherent architecture");var palette=(Behaviour)root.GetComponent(T("CourtyardArchitecturePalette"));
        var targets=GameObject.Find("Garden cottage").GetComponentsInChildren<MeshRenderer>();
        var replacement=targets.First(r=>r.sharedMaterial.name=="Resident local limestone");
        var owned=replacement.sharedMaterial;
        Assert.That(owned.GetFloat("_TextureScale"),Is.EqualTo(.58f).Within(.001f));
        var soil=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed").ToArray();
        var soilMaterials=soil.Select(r=>r.sharedMaterial).ToArray();
        var colliders=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();
        palette.enabled=false;Assert.That(replacement.sharedMaterial,Is.SameAs(stone));
        palette.enabled=true;Assert.That(replacement.sharedMaterial,Is.SameAs(owned));
        Assert.That(soil.Select(r=>r.sharedMaterial),Is.EqualTo(soilMaterials));Assert.That(colliders.All(c=>c.enabled),Is.True);
        Assert.That(stone.GetFloat("_TextureScale"),Is.EqualTo(scale));Assert.That(plaster.GetColor("_BaseColor"),Is.EqualTo(color));
        yield return Load();Assert.That(owned==null,Is.True);Assert.That(root==null,Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardArchitecturePalette"),FindObjectsSortMode.None).Length,Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
    }
}
