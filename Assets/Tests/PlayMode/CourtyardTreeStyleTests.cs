using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardTreeStyleTests
{
    static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    static IEnumerator Load() => (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
    static void Capture(string n) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { n });
    [UnityTest] public IEnumerator TreeVarieties_RenderAtTheirHabitatsAndSurviveReload()
    {
        yield return Load();
        var sprites = new[] { "Oak", "Birch", "Apple", "Willow" }.Select(n => Resources.Load<Sprite>("CourtyardStyle/Trees/" + n)).ToArray();
        Assert.That(sprites.All(s => s != null), Is.True);
        Assert.That(sprites.Select(s => s.texture).Distinct().Count(), Is.EqualTo(4));
        var trees = GameObject.Find("Western field margins").GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "Western field tree").ToArray();
        Assert.That(trees.Length, Is.EqualTo(12));
        Assert.That(trees.All(r => sprites.Contains(r.sprite)), Is.True);
        Assert.That(trees.Select(r => r.sprite).Distinct().Count(), Is.EqualTo(2));
        var casters = GameObject.Find("Western grove morning shadows").GetComponentsInChildren<MeshRenderer>()
            .Where(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);
        Assert.That(casters.All(r => sprites.Any(s => r.sharedMaterial.GetTexture("_BaseMap") == s.texture)), Is.True,
            "Shadow silhouettes must use the new tree textures.");
        var root = GameObject.Find("Aurenfeld tree varieties");
        Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
        Assert.That(GameObject.Find("Garden apple tree").GetComponent<SpriteRenderer>().sprite, Is.EqualTo(sprites[2]));
        Assert.That(GameObject.Find("Stream willow").GetComponent<SpriteRenderer>().sprite, Is.EqualTo(sprites[3]));
        var follow = UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var view = T("CourtyardCamera").GetMethod("SetScenicView");
        var angle = Quaternion.Euler(27, 0, 0);
        view.Invoke(follow, new object[] { new Vector3(6, 2, 10) - angle * Vector3.forward * 22, angle, 45f });
        yield return new WaitForSeconds(.2f); Capture("aurenfeld-trees-garden");
        view.Invoke(follow, new object[] { new Vector3(46, 2, 5) - angle * Vector3.forward * 28, angle, 45f });
        yield return new WaitForSeconds(.2f); Capture("aurenfeld-trees-stream");
        T("CourtyardCamera").GetMethod("ClearScenicView").Invoke(follow, null);
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardTreeStyle"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(GameObject.Find("Aurenfeld tree varieties").GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "Garden apple tree"), Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
