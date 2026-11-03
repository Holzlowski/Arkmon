using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardPolishTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest]public IEnumerator MainProps_HaveDepthAndKeepGameplaySystems()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("StartScene");for(int i=0;i<5;i++)yield return null;
        foreach(string name in new[]{"Stable","Wheelbarrow","Workbench","Fountain basin","Bench seat","Tower entrance"})
        {
            var prop=GameObject.Find(name);Assert.That(prop,Is.Not.Null,name);
            Assert.That(prop.GetComponentsInChildren<MeshRenderer>().Length,Is.GreaterThan(5),name);
            Assert.That(prop.GetComponentsInChildren<SpriteRenderer>().Length,Is.EqualTo(0),name);
            Assert.That(prop.GetComponent<BoxCollider>(),Is.Not.Null,name);
        }
        Assert.That(UnityEngine.Object.FindFirstObjectByType(T("NPC")),Is.Not.Null);
        Assert.That(GameObject.Find("Castle horizon and golden fields"),Is.Not.Null);
        Assert.That(GameObject.Find("Garden bench seat"),Is.Not.Null);
        var towerStone=GameObject.Find("Octagonal tower").GetComponent<MeshRenderer>().sharedMaterial;
        var hallStone=GameObject.Find("Castle hall structure").GetComponent<MeshRenderer>().sharedMaterial;
        Assert.That(towerStone,Is.SameAs(hallStone),"Tower and castle share stone scale and lighting.");
        Assert.That(hallStone.shader.name,Is.EqualTo("Arkmon/Tower Painted Light"));
        string[] homes={"Stable","West cottage","Garden cottage","East cottage","Cellar storehouse"};
        string[] castles={"Castle west wing","Castle hall","Castle upper keep","Castle keep west tower","Castle keep east tower"};
        Bounds BoundsOf(string name)
        {
            var renderers=GameObject.Find(name).GetComponentsInChildren<MeshRenderer>();
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        for(int i=0;i<homes.Length;i++)
        foreach(string other in homes.Skip(i+1).Concat(castles))
            Assert.That(BoundsOf(homes[i]).Intersects(BoundsOf(other)),Is.False,homes[i]+" overlaps "+other);
        Assert.That(Physics.Raycast(new Vector3(0,5,-11),Vector3.down,out var hill,10),Is.True);
        Assert.That(hill.point.y,Is.GreaterThan(1.4f),"The meadow relief has real walkable height.");
        Assert.That(hill.collider.name,Is.EqualTo("Rolling courtyard meadow"));
        Assert.That(Physics.BoxCast(new Vector3(2,1,1),new Vector3(.3f,.75f,.3f),Vector3.forward,
            out var blocked,Quaternion.identity,5,~0,QueryTriggerInteraction.Ignore),Is.False,
            "The cottage approach must stay clear of benches: "+blocked.collider?.name);
        LogAssert.NoUnexpectedReceived();
    }
}
