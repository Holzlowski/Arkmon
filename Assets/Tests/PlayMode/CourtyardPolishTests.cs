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
        foreach(string name in new[]{"Stable","Wheelbarrow","Workbench","Fountain basin","Bench seat","Tower entrance","Paper supply crate"})
        {
            var prop=GameObject.Find(name);Assert.That(prop,Is.Not.Null,name);
            Assert.That(prop.GetComponentsInChildren<MeshRenderer>().Length,Is.GreaterThan(5),name);
            Assert.That(prop.GetComponentsInChildren<SpriteRenderer>().Length,Is.EqualTo(0),name);
            Assert.That(prop.GetComponent<BoxCollider>(),Is.Not.Null,name);
        }
        Assert.That(UnityEngine.Object.FindFirstObjectByType(T("NPC")),Is.Not.Null);
        Assert.That(GameObject.Find("Castle horizon and golden fields"),Is.Not.Null);
        Assert.That(GameObject.Find("Gate forecourt"),Is.Not.Null);
        LogAssert.NoUnexpectedReceived();
    }
}
