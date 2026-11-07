using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardReportedIssuesTests
{
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static void Capture(string name) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { name });

    [UnityTest] public IEnumerator RendererFreeTowerRamp_ResolvesStoneFootsteps()
    {
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
        var player = GameObject.FindWithTag("Player");
        var footsteps = player.GetComponent(T("ExplorationFootsteps"));
        var surfaceAt = T("ExplorationFootsteps").GetMethod("SurfaceAt", BindingFlags.NonPublic | BindingFlags.Instance);
        var ramp = GameObject.Find("Tower stairs walkable slope").GetComponent<Collider>();
        Assert.That(ramp.TryGetComponent<Renderer>(out _), Is.False);
        Assert.That(ramp.Raycast(new Ray(new Vector3(-34, 5, 6), Vector3.down), out var hit, 10), Is.True);
        Assert.That(surfaceAt.Invoke(footsteps, new object[] { hit }).ToString(), Is.EqualTo("Stone"), "Invisible collision ramps must resolve footsteps without a Renderer.");

        LogAssert.NoUnexpectedReceived();
    }
}
