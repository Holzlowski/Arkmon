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

    [UnityTest] public IEnumerator TowerRampAndTerraceSeam_RemainUsable()
    {
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
        var player = GameObject.FindWithTag("Player");
        var footsteps = player.GetComponent(T("ExplorationFootsteps"));
        var surfaceAt = T("ExplorationFootsteps").GetMethod("SurfaceAt", BindingFlags.NonPublic | BindingFlags.Instance);
        var ramp = GameObject.Find("Tower stairs walkable slope").GetComponent<Collider>();
        Assert.That(ramp.TryGetComponent<Renderer>(out _), Is.False);
        Assert.That(ramp.Raycast(new Ray(new Vector3(-34, 5, 6), Vector3.down), out var hit, 10), Is.True);
        Assert.That(surfaceAt.Invoke(footsteps, new object[] { hit }).ToString(), Is.EqualTo("Stone"), "Invisible collision ramps must resolve footsteps without a Renderer.");

        var terrace = GameObject.Find("Raised tower terrace").GetComponent<Collider>();
        var extension = GameObject.Find("Residence terrace extension").GetComponent<Collider>();
        Assert.That(extension.bounds.min.x, Is.EqualTo(terrace.bounds.max.x).Within(.001f), "Terrace tops must meet without coplanar overlap.");
        foreach(float x in new[] { -29.1f, -29f, -28.9f })
        {
            var hits = Physics.RaycastAll(new Vector3(x, 4, 9), Vector3.down, 5, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(hits.Any(h => h.collider.CompareTag("Ground") && Mathf.Abs(h.point.y - 1.4f) < .002f), Is.True, "The terrace seam must remain supported.");
        }
        LogAssert.NoUnexpectedReceived();
    }
}
