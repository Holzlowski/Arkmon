using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardFurnitureShadowTests
{
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static IEnumerator Load() => (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
    static void Capture(string name) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { name });

    [UnityTest]
    public IEnumerator FurnitureContacts_RenderFollowVisibilityAndReloadWithoutDuplicates()
    {
        yield return Load();
        var root = GameObject.Find("Courtyard furniture contacts"); Assert.That(root, Is.Not.Null);
        Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        Assert.That(renderers.Length, Is.EqualTo(4), "Both benches, the potting table and Ida's workbench need contacts.");
        Assert.That(renderers.All(r => r.sharedMaterial.shader.isSupported), Is.True);
        var bench = GameObject.Find("Marta side bench");
        bench.SetActive(false); yield return null;
        Assert.That(renderers.Single(r => r.name == "Marta side bench soft contact").enabled, Is.False);
        bench.SetActive(true); yield return null;
        Assert.That(renderers.All(r => r.enabled), Is.True);
        var camera = Camera.main;
        Capture("courtyard-contacts-gameplay");
        var cameraRig = UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var center = bench.GetComponentsInChildren<MeshRenderer>().First().bounds.center;
        var position = center + new Vector3(2, 5, -7);
        T("CourtyardCamera").GetMethod("SetScenicView").Invoke(cameraRig, new object[] { position, Quaternion.LookRotation(center - position), 42f });
        yield return null;
        Capture("courtyard-furniture-contacts");
        foreach (var renderer in renderers) renderer.enabled = false;
        Capture("courtyard-furniture-contacts-disabled");
        foreach (var renderer in renderers) renderer.enabled = true;
        string folder = Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Builds"));
        var on = new Texture2D(2,2); var off = new Texture2D(2,2);
        try
        {
            on.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder, "courtyard-furniture-contacts.png")));
            off.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder, "courtyard-furniture-contacts-disabled.png")));
            var a = on.GetPixels32(); var b = off.GetPixels32(); int darker = 0;
            for(int i=0;i<a.Length;i++) if(b[i].r+b[i].g+b[i].b-a[i].r-a[i].g-a[i].b>12) darker++;
            Assert.That(darker, Is.GreaterThan(500), "The added contact must visibly darken the furniture's ground.");
        }
        finally { UnityEngine.Object.Destroy(on); UnityEngine.Object.Destroy(off); }
        var style = (Behaviour)root.GetComponent(T("CourtyardFurnitureShadows"));
        style.enabled = false; yield return null; Assert.That(renderers.All(r => !r.enabled), Is.True);
        style.enabled = true; yield return null; Assert.That(renderers.All(r => r.enabled), Is.True);
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardFurnitureShadows"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
