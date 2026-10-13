using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TowerLightingTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest] public IEnumerator TowerUsesScopedLitStoneAndControlledMorningEffects()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("TowerRoom");yield return null;yield return null;
        Assert.That(GameObject.Find("Stone floor").GetComponent<Renderer>().sharedMaterial.shader.name,Is.EqualTo("Arkmon/Tower Painted Light"));
        var sun=GameObject.Find("Window morning light").GetComponent<Light>();Assert.That(sun.shadows,Is.EqualTo(LightShadows.Soft));
        Assert.That(sun.transform.position.z,Is.GreaterThan(6.1f),"Sun shines through the frame from outside.");
        Assert.That(GameObject.Find("Layered morning haze -9").GetComponent<Renderer>().enabled,Is.False);
        Assert.That(GameObject.Find("Golden morning patch").GetComponent<Renderer>().enabled,Is.False);
        var contacts=GameObject.Find("Furniture contact shadows");Assert.That(contacts.GetComponentsInChildren<Renderer>().Length,Is.EqualTo(3));
        Assert.That(contacts.GetComponentsInChildren<Collider>().Length,Is.EqualTo(0));
        Assert.That(GameObject.Find("Window slender mullions").GetComponentsInChildren<Renderer>().Length,Is.EqualTo(2));
        yield return SceneManager.LoadSceneAsync("StartScene");yield return null;
        Assert.That(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterial!=null)
            .Any(r=>r.sharedMaterial.shader.name=="Arkmon/Tower Painted Light"),Is.False,"Courtyard materials stay independent.");
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);LogAssert.NoUnexpectedReceived();
    }
}
