using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardFoothillTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(AurenfeldFocusTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator Foothills_JoinFieldsWithoutCoveringRoad_AndReleaseResources()
    {
        yield return Load();
        var root=GameObject.Find("Aurenfeld mountain foothills");Assert.That(root,Is.Not.Null);
        Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
        var filters=root.GetComponentsInChildren<MeshFilter>();Assert.That(filters.Length,Is.EqualTo(1));
        Assert.That(filters.All(f=>f.sharedMesh.normals.All(n=>n.y>0)),Is.True,"Hill meshes must face upwards.");
        Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Length,Is.InRange(28,65));
        Assert.That(root.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        var style=root.GetComponent(T("CourtyardFoothills"));
        var meadow=GameObject.Find("Castle horizon and golden fields").GetComponentInChildren(T("MeadowVegetation"));
        var surface=style.GetType().GetMethod("SurfacePoint");var ground=meadow.GetType().GetMethod("SurfacePoint");
        var road=T("CourtyardFieldMargins").GetMethod("VergeZ",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
        for(float x=-310;x>=-405;x-=5)
        {
            float z=(float)road.Invoke(null,new object[]{x});
            foreach(float side in new[]{-3f,0,3f})
            {
                var p=new Vector3(x,0,z+side);
                float delta=((Vector3)surface.Invoke(style,new object[]{p})).y-((Vector3)ground.Invoke(meadow,new object[]{p})).y;
                Assert.That(delta,Is.LessThan(.001f),"Leave the distant road corridor open.");
            }
        }
        var mountain=GameObject.Find("Western painted mountains").GetComponent<MeshRenderer>();
        // Probe actual triangles across the formerly uncovered valleys and side margins.
        // This collider exists only for the regression check, never in the scenic runtime.
        var probeObject=new GameObject("Foothill coverage probe");
        try
        {
            var probe=probeObject.AddComponent<MeshCollider>();probe.sharedMesh=filters[0].sharedMesh;
            for(float x=-180;x>=-418;x-=7)for(float z=-298;z<=338;z+=7)
                Assert.That(probe.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out _,1000),Is.True,
                    "Continuous ground is missing at "+x+", "+z);
            Assert.That(filters[0].sharedMesh.bounds.min.x,Is.LessThan(mountain.transform.position.x),
                "Ground must extend beneath the painted mountain plane.");
        }
        finally{UnityEngine.Object.Destroy(probeObject);}
        var ownedMaterial=mountain.sharedMaterial;var source=Resources.Load<Material>("CourtyardStyle/Lookout mountains");
        Assert.That(ownedMaterial,Is.Not.SameAs(source));Assert.That(ownedMaterial.GetFloat("_Haze"),Is.GreaterThan(source.GetFloat("_Haze")));
        var point=GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout"));
        CourtyardGardenTaskTests.MoveTo(point);yield return null;
        point.GetType().GetMethod("InteractWithPlayer").Invoke(point,null);
        float deadline=Time.realtimeSinceStartup+18;bool middle=false;
        while((float)point.GetType().GetProperty("Progress").GetValue(point)<.999f && Time.realtimeSinceStartup<deadline)
        {
            if(!middle && (float)point.GetType().GetProperty("Progress").GetValue(point)>.65f){Capture("aurenfeld-foothills-flight");middle=true;}
            yield return null;
        }
        Assert.That((float)point.GetType().GetProperty("Progress").GetValue(point),Is.GreaterThan(.99f));
        yield return new WaitForSeconds(.6f);
        try
        {
            root.SetActive(false);mountain.sharedMaterial=source;Capture("aurenfeld-foothills-before");
        }
        finally{root.SetActive(true);mountain.sharedMaterial=ownedMaterial;}
        Capture("aurenfeld-foothills-after");
        var meshes=filters.Select(f=>f.sharedMesh).ToArray();var material=filters[0].GetComponent<Renderer>().sharedMaterial;
        point.GetType().GetMethod("ReturnToCourtyard").Invoke(point,null);yield return new WaitForSeconds(4);
        yield return Load();
        Assert.That(root==null && ownedMaterial==null && material==null && meshes.All(m=>m==null),Is.True,"Scene reload must release owned geometry/materials.");
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardFoothills"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
