using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public class CourtyardAmbientDetailsTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    [UnityTest] public IEnumerator LocalEffects_ExpireFollowPlayerDistanceAndUnloadCleanly()
    {
        yield return Load();var root=GameObject.Find("Courtyard quiet details");Assert.That(root,Is.Not.Null);
        Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
        var dust=GameObject.Find("Courtyard footfall dust").GetComponent<ParticleSystem>();
        for(int i=0;i<30;i++)T("CourtyardAmbientDetails").GetMethod("Footfall").Invoke(null,new object[]{new Vector3(4,.1f,1)});
        Assert.That(dust.particleCount,Is.InRange(1,24));yield return new WaitForSeconds(1);Assert.That(dust.particleCount,Is.Zero);
        var stream=GameObject.Find("Spatial stream ambience").GetComponent<AudioSource>();
        var body=GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        body.position=new Vector3(47,1,-1);Physics.SyncTransforms();yield return new WaitForSeconds(1);Assert.That(stream.volume,Is.GreaterThan(.05f));
        body.position=new Vector3(-20,1,0);Physics.SyncTransforms();yield return new WaitForSeconds(1.5f);Assert.That(stream.volume,Is.Zero);
        Assert.That(stream.isPlaying,Is.False,"Batch review must remain silent.");
        var previous=root;yield return Load();Assert.That(previous==null,Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardAmbientDetails"),FindObjectsSortMode.None).Length,Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
    }
}
