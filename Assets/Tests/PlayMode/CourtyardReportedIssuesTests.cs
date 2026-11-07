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

    [UnityTest] public IEnumerator TowerRampTerraceAndGateForeground_RemainUsable()
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
        var body = player.GetComponent<Rigidbody>();
        player.transform.position = body.position = new Vector3(-33, 2.4f, 8.5f); Physics.SyncTransforms();
        yield return new WaitForSeconds(1.2f); Capture("reported-tower-terrace-fixed");

        var meadow = GameObject.Find("Castle horizon and golden fields").GetComponentInChildren(T("MeadowVegetation"));
        var surface = T("MeadowVegetation").GetMethod("SurfacePoint");
        var gold = T("CourtyardFieldMargins").GetMethod("GoldenFieldWeight");
        foreach(float x in new[] { -42f, -34f, -12f, 0f, 18f, 28f, 38f, 48f })
            foreach(float z in new[] { -24f, -32f, -40f })
            {
                var point = new Vector3(x, 0, z);
                var actual = (Vector3)surface.Invoke(meadow, new object[] { point });
                Assert.That(actual.y, Is.EqualTo(-.05f).Within(.002f), "Legacy foreground mound must not obscure the courtyard: " + point);
                Assert.That((float)gold.Invoke(null, new object[] { point }), Is.EqualTo(1).Within(.001f));
            }
        var isInner=T("CourtyardForeground").GetMethod("IsInnerVerge");
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Meadow color patch"))
            if((bool)isInner.Invoke(null,new object[]{r.transform.position})) Assert.That(r.enabled,Is.False,"Legacy round foreground patches should be retired.");
        var oldGrass=GameObject.Find("Courtyard decorative grass").GetComponent<MeshFilter>();
        var v=oldGrass.sharedMesh.vertices;var indices=oldGrass.sharedMesh.triangles;
        for(int i=0;i<indices.Length;i+=3)
        {
            var center=oldGrass.transform.TransformPoint((v[indices[i]]+v[indices[i+1]]+v[indices[i+2]])/3);
            Assert.That((bool)isInner.Invoke(null,new object[]{center}),Is.False,"No old triangular blades should remain in the front lawn.");
        }
        // Preserve the western lookout's existing terrain outside the southern foreground.
        var untouched = new Vector3(-61, 4, 7);
        Assert.That((Vector3)T("CourtyardEastFieldStyle").GetMethod("ExteriorPoint").Invoke(null, new object[] { untouched }), Is.EqualTo(untouched));
        foreach(var point in new[] { new Vector3(-35.9f, 1, -8.8f), new Vector3(-12, 1, -9), new Vector3(12, 1, -9), new Vector3(39, 1, -8), new Vector3(46, 1, -1), new Vector3(53, 1, -1) })
        {
            player.transform.position = body.position = point; Physics.SyncTransforms();
            yield return new WaitForSeconds(1.5f); Capture("courtyard-full-foreground-" + point.x);
        }
        LogAssert.NoUnexpectedReceived();
    }
}
