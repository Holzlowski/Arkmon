using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardWallReliefTests
{
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string name)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name});
    [UnityTest] public IEnumerator AllBoundaryFaces_HaveRelief_WithoutChangingCollisions_AndReleaseMeshes()
    {
        yield return Load();
        string[] names={"North castle wall","West wall near","West wall far","East wall north of gate","East wall south of gate","Cutaway near castle wall"};
        var owned=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(f=>f.sharedMesh!=null && f.sharedMesh.name=="Courtyard jointed stone relief").ToArray();
        Assert.That(owned.Length,Is.GreaterThan(6),"Boundary walls and their battlements must use actual relief.");
        foreach(string name in names)
        {
            var wall=GameObject.Find(name);var mesh=wall.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.name,Is.EqualTo("Courtyard jointed stone relief"),name);
            Assert.That(mesh.vertexCount,Is.GreaterThan(100));
            var box=wall.GetComponent<BoxCollider>();Assert.That(box,Is.Not.Null);Assert.That(box.enabled,Is.True);
            Assert.That(box.size.x,Is.EqualTo(1).Within(.001f),"Keep authored boundary collision.");
            Assert.That(wall.GetComponents<Collider>().Length,Is.EqualTo(1),"Individual stones must not add collision clutter.");
            var size=wall.transform.lossyScale;
            for(int axis=0;axis<3;axis++)
            {
                float extra=(mesh.bounds.size[axis]-1)*Mathf.Abs(size[axis]);
                Assert.That(extra,Is.InRange(.015f,.20f),"Relief should project a few centimetres, not obstruct paths: "+name);
            }
            Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
            Assert.That(mesh.normals.All(n=>!float.IsNaN(n.x) && n.sqrMagnitude>.9f),Is.True);
            var block=new MaterialPropertyBlock();wall.GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.That(block.GetFloat("_StoneMeshUV"),Is.EqualTo(1));
        }
        var arch=owned.Single(f=>f.name=="East gate arch masonry").sharedMesh;
        Assert.That(arch.bounds.max.z,Is.GreaterThan(1.52f),"Dress the exposed southern gate end, not only its front.");
        Assert.That(arch.bounds.min.z,Is.LessThan(-3.52f),"Dress the opposite gate end too.");
        TestContext.WriteLine("Wall relief: "+owned.Length+" combined meshes; "+owned.Sum(f=>f.sharedMesh.vertexCount)+" vertices.");
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();body.isKinematic=true;
        ((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        var points=new[]{new Vector3(-39,1,6),new Vector3(-34,2.4f,7.8f),new Vector3(35,1,-1),new Vector3(27,1,-15.8f)};
        for(int i=0;i<points.Length;i++)
        {
            body.position=player.transform.position=points[i];Physics.SyncTransforms();
            yield return new WaitForSeconds(1.3f);Capture("wall-relief-route-"+i);
        }
        var front=GameObject.Find("Cutaway near castle wall").GetComponent<Renderer>();
        var cutaway=new MaterialPropertyBlock();front.GetPropertyBlock(cutaway);
        Assert.That(cutaway.GetVector("_ViewCutaway").w,Is.GreaterThan(.1f),"Near-player reveal must still affect the replacement wall mesh.");
        var meshes=owned.Select(f=>f.sharedMesh).ToArray();
        UnityEngine.Object.Destroy(GameObject.Find("Courtyard wall stone relief"));yield return null;yield return null;
        foreach(var f in owned)Assert.That(f.sharedMesh.name,Is.Not.EqualTo("Courtyard jointed stone relief"));
        Assert.That(meshes.All(m=>m==null),Is.True,"Runtime relief meshes must be released.");
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardWallRelief"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
