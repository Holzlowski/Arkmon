using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardCastlePaletteTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator SharedStone_PreservesBoundariesAndTowerApproach_ReviewRoute()
    {
        yield return Load();
        var shared=GameObject.Find("Castle hall").transform.Find("Castle hall structure").GetComponent<MeshRenderer>().sharedMaterial;
        foreach(string name in new[]{"Player residence tower","Castle west wing","Aurenfeld upper residence","Tower connecting wing","Aurenfeld raised approach"})
        {
            var root=GameObject.Find(name);Assert.That(root,Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial==shared),Is.True,name+" must use the central castle stone.");
            Assert.That(root.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        }
        var tower=GameObject.Find("Player residence tower");Assert.That(tower.GetComponent<Collider>().enabled,Is.True);
        foreach(string name in new[]{"North castle wall","West wall near","West wall far","East wall north of gate","East wall south of gate","Cutaway near castle wall"})
        {
            var wall=GameObject.Find(name);Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial,Is.SameAs(shared));
            Assert.That(wall.GetComponent<Collider>().enabled,Is.True,name+" must still enclose the courtyard.");
        }
        var towerStone=tower.transform.Find("Octagonal tower").GetComponent<MeshRenderer>();
        var block=new MaterialPropertyBlock();towerStone.GetPropertyBlock(block);
        Assert.That(block.GetVector("_ObjectScale").x,Is.EqualTo(towerStone.transform.lossyScale.x).Within(.001f));
        Assert.That(shared.GetFloat("_FaceAlignedMasonry"),Is.EqualTo(1));
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();var focus=player.GetComponent(T("PlayerInteractionFocus"));
        var entrance=UnityEngine.Object.FindObjectsByType(T("CourtyardEntrance"),FindObjectsSortMode.None).Cast<Component>().Single(e=>e.GetType().GetField("destination").GetValue(e).ToString()=="Tower");
        player.transform.position=entrance.transform.position+new Vector3(0,1,-.6f);body.position=player.transform.position;Physics.SyncTransforms();yield return null;
        Assert.That(focus.GetType().GetMethod("Refresh").Invoke(focus,null),Is.SameAs(entrance));
        ((Behaviour)focus).enabled=false;
        var points=new[]{new Vector3(-34,2.4f,7.8f),new Vector3(-22,1,2),new Vector3(-11,1,-1),new Vector3(11,1,8),new Vector3(37,1,-1)};
        string[] labels={"tower","west-residence","castle-connection","central-castle","east-wall"};
        for(int i=0;i<points.Length;i++)
        {
            player.transform.position=points[i];body.position=points[i];yield return new WaitForSeconds(1.25f);
            Capture("castle-palette-"+labels[i]);
        }
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardCastleFacadeStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(GameObject.Find("North castle wall").GetComponent<MeshRenderer>().sharedMaterial.name,Is.EqualTo("Castle illustrated masonry"));
        LogAssert.NoUnexpectedReceived();
    }
}
