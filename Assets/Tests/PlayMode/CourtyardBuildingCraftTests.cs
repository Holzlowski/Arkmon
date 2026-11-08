using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardBuildingCraftTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest] public IEnumerator CraftedSurfaces_PreserveCollisionAndWindows_AndReleaseOwnedGeometry()
    {
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
        var filters=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        var relief=filters.Where(f=>f.name.StartsWith("Craft detail ")).ToArray();
        var bevels=filters.Where(f=>f.name!="Craft timber peg" && f.sharedMesh!=null && f.sharedMesh.name=="Crafted softened edge").ToArray();
        var pegs=filters.Where(f=>f.name=="Craft timber peg").ToArray();
        Assert.That(pegs.Length,Is.GreaterThan(0));
        foreach(string finish in new[]{"Masonry","Roof","Paving"})
            Assert.That(relief.Any(f=>f.name=="Craft detail "+finish),Is.True,"Missing "+finish);
        Assert.That(bevels.Length,Is.GreaterThan(30));
        foreach(var f in relief)
        {
            Assert.That(f.GetComponentsInChildren<Collider>().Length,Is.Zero,"Cosmetic details must not obstruct movement.");
            var mesh=f.sharedMesh;
            Assert.That(mesh.vertexCount,Is.GreaterThan(0));
            Assert.That(mesh.normals.All(n=>!float.IsNaN(n.x) && n.sqrMagnitude>.9f),Is.True,f.transform.parent.name);
            Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
        }
        foreach(string building in new[]{"Castle west wing","Player residence tower","Ida workshop","Stable","Garden cottage"})
            Assert.That(GameObject.Find(building).GetComponentsInChildren<MeshFilter>().Any(f=>f.name=="Craft detail Roof"),Is.True,building);
        Assert.That(GameObject.Find("Castle hall").GetComponentsInChildren<MeshFilter>().Any(f=>f.name=="Craft detail Masonry"),Is.True,"The hall has battlements instead of a pitched roof.");
        var foundation=GameObject.Find("Courtyard grounded foundations");
        Assert.That(foundation.GetComponentsInChildren<Collider>().Length,Is.Zero);
        Assert.That(foundation.GetComponentsInChildren<MeshFilter>().Length,Is.GreaterThanOrEqualTo(4));
        // Surface dressing must conform to the actual ground and remain below ankle height.
        foreach(string label in new[]{"Sandy foundation transitions","Sheltered moss transitions","Resident work scuffs"})
        {
            var detail=GameObject.Find(label);Assert.That(detail,Is.Not.Null);
            var filter=detail.GetComponent<MeshFilter>();
            foreach(var local in filter.sharedMesh.vertices.Where((p,i)=>i%11==0))
            {
                var world=detail.transform.TransformPoint(local);
                var hits=Physics.RaycastAll(new Vector3(world.x,25,world.z),Vector3.down,50,~0,QueryTriggerInteraction.Ignore)
                    .Where(h=>h.collider.CompareTag("Ground") && h.normal.y>.5f).ToArray();
                Assert.That(hits.Length,Is.GreaterThan(0));
                Assert.That(world.y-hits.Max(h=>h.point.y),Is.InRange(.070f,.083f),label+" must follow terrain without floating.");
            }
        }
        foreach(var plant in foundation.GetComponentsInChildren<SpriteRenderer>())
            Assert.That(plant.sprite.name.EndsWith("wind mesh"),Is.True,"Added grass must join the existing breeze.");
        var ridgeParts=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Clay ridge cap").ToArray();
        Assert.That(ridgeParts.Length,Is.GreaterThan(1));
        Assert.That(ridgeParts.Select(t=>Mathf.RoundToInt(t.localEulerAngles.z*100)).Distinct().Count(),Is.GreaterThan(1),"Ridge caps should not be mechanically aligned.");
        var originalColliders=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Select(c=>(c.GetInstanceID(),c.enabled)).OrderBy(c=>c.Item1).ToArray();
        TestContext.WriteLine("Building craft: "+relief.Length+" relief renderers, "+relief.Sum(f=>f.sharedMesh.vertexCount)+" relief vertices, "+bevels.Length+" softened parts.");
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();body.isKinematic=true;
        ((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        var points=new[]{new Vector3(-34,2.4f,8),new Vector3(-35.9f,1,-10.2f),new Vector3(-18,1,-3),new Vector3(12,1,8),new Vector3(27.3f,1,-15.85f),new Vector3(40.8f,1,-1)};
        for(int i=0;i<points.Length;i++)
        {
            body.position=player.transform.position=points[i];Physics.SyncTransforms();yield return new WaitForSeconds(1.4f);
            typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"building-craft-"+i});
        }
        // Reversible teardown must leave authored collision and all original mesh filters intact.
        var meshes=relief.Select(f=>f.sharedMesh).Concat(bevels.Select(f=>f.sharedMesh)).Concat(foundation.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh)).ToArray();
        UnityEngine.Object.Destroy(GameObject.Find("Courtyard crafted buildings"));
        UnityEngine.Object.Destroy(foundation);yield return null;yield return null;
        Assert.That(meshes.All(m=>m==null),Is.True,"Release every generated surface.");
        Assert.That(bevels.All(f=>f!=null && f.sharedMesh.name=="Cube"),Is.True);
        Assert.That(pegs.All(f=>f==null),Is.True,"Scene-owned pegs must be removed with the style.");
        CollectionAssert.AreEqual(originalColliders,UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Select(c=>(c.GetInstanceID(),c.enabled)).OrderBy(c=>c.Item1).ToArray());
        LogAssert.NoUnexpectedReceived();
    }
}