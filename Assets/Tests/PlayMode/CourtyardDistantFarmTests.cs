using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardDistantFarmTests
{
    static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    static IEnumerator Load() => (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
    static void Capture(string n) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { n });
    [UnityTest] public IEnumerator DistantFarm_HasVisibleRisingSmokeAndReloadsOnce()
    {
        yield return Load();
        var farm = GameObject.Find("Aurenfeld distant farm"); Assert.That(farm, Is.Not.Null);
        Assert.That(farm.transform.position.x, Is.LessThan(-100));
        Assert.That(farm.GetComponentsInChildren<Collider>(), Is.Empty, "This is distant scenery, not a new playable area.");
        Assert.That(farm.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterial != null && r.sharedMaterial.shader.isSupported), Is.True);
        var approach=GameObject.Find("Farm access and orchard");Assert.That(approach,Is.Not.Null);
        Assert.That(approach.GetComponentsInChildren<Collider>(),Is.Empty);
        Assert.That(approach.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh.normals.All(n=>n.y>0)),Is.True);
        Assert.That(GameObject.Find("Farm apple tree"),Is.Not.Null);
        var art = farm.transform.Find("Painted Aurenfeld farm").GetComponent<SpriteRenderer>(); Assert.That(art, Is.Not.Null);
        Assert.That(art.sprite, Is.EqualTo(Resources.Load<Sprite>("CourtyardStyle/Farm/AurenfeldFarm")));
        var road = GameObject.Find("Western winding field path").GetComponent<MeshFilter>().sharedMesh;
        Assert.That(road.bounds.min.x, Is.LessThan(-370), "The road must continue beyond the nearby farm.");
        Assert.That(road.normals.All(n => n.y > 0), Is.True);
        var hills = GameObject.Find("Aurenfeld distant rolling fields"); Assert.That(hills, Is.Not.Null);
        Assert.That(hills.GetComponentsInChildren<Collider>(), Is.Empty);
        Assert.That(hills.GetComponent<MeshFilter>().sharedMesh.normals.All(n => n.y > 0), Is.True);
        var smoke = farm.GetComponentInChildren<ParticleSystem>(); Assert.That(smoke, Is.Not.Null);
        Assert.That(smoke.main.maxParticles, Is.LessThanOrEqualTo(32));
        var follow = UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        T("CourtyardCamera").GetMethod("SetScenicView").Invoke(follow, new object[] { new Vector3(-61,11,7), Quaternion.LookRotation(new Vector3(-69,0,20)), 58f });
        yield return new WaitForSeconds(.4f);
        Assert.That(smoke.particleCount, Is.GreaterThan(0));
        var chimney = Camera.main.WorldToViewportPoint(smoke.transform.position);
        Assert.That(chimney.z, Is.GreaterThan(0)); Assert.That(chimney.x, Is.InRange(.1f,.9f)); Assert.That(chimney.y, Is.InRange(.2f,.85f));
        Capture("aurenfeld-farm-panorama");
        Capture("aurenfeld-clear-distance");
        Capture("aurenfeld-golden-fields");
        Capture("aurenfeld-parcels-after");
        // Visual diagnostic: compare the old strong distance veil at this identical camera.
        float fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance;
        try
        {
            RenderSettings.fogStartDistance=65;RenderSettings.fogEndDistance=340;
            Capture("aurenfeld-old-distance-fog");
        }
        finally { RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd; }

        // Compare the distant road in the identical frame; foreground grass/wind cannot fake visibility.
        var roadRenderer = GameObject.Find("Western winding field path").GetComponent<MeshRenderer>();
        roadRenderer.enabled = false; Capture("aurenfeld-road-hidden"); roadRenderer.enabled = true;
        var near = new Texture2D(2,2); var without = new Texture2D(2,2); int distantPixels = 0;
        try
        {
            string folder = Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","Builds"));
            near.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder,"aurenfeld-farm-panorama.png")));
            without.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder,"aurenfeld-road-hidden.png")));
            var a=near.GetPixels32();var b=without.GetPixels32();
            for(int y=near.height/2-20;y<near.height*3/5;y++) for(int x=near.width/3;x<near.width*2/3;x++)
            {
                int i=y*near.width+x;
                if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>15)distantPixels++;
            }
        }
        finally { UnityEngine.Object.Destroy(near);UnityEngine.Object.Destroy(without); }
        TestContext.WriteLine("Distant road pixels near the horizon: "+distantPixels);

        var particles = new ParticleSystem.Particle[32]; int count = smoke.GetParticles(particles);
        Assert.That(particles.Take(count).Any(p => p.position.y > smoke.transform.position.y + 1), Is.True, "Smoke must rise above the actual chimney.");
        smoke.Pause(); smoke.Simulate(.8f, true, false, false);
        int afterCount = smoke.GetParticles(particles);
        Assert.That(afterCount, Is.GreaterThan(0)); smoke.Play();
        var view = T("CourtyardCamera").GetMethod("SetScenicView");
        var focus = farm.transform.position + Vector3.up * 6;
        var position = focus + new Vector3(35,9,-15);
        view.Invoke(follow, new object[] { position, Quaternion.LookRotation(focus-position), 48f });
        yield return new WaitForSeconds(.2f); Capture("aurenfeld-farm-detail");
        var distantFocus = new Vector3(-270, 17, 50);
        var distantEye = new Vector3(-190, 35, 28);
        view.Invoke(follow, new object[] { distantEye, Quaternion.LookRotation(distantFocus-distantEye), 58f });
        yield return null;yield return null;Capture("aurenfeld-far-road-detail");
        Assert.That(distantPixels,Is.GreaterThan(40),"The extended road must actually remain visible near the horizon.");
        T("CourtyardCamera").GetMethod("ClearScenicView").Invoke(follow, null);
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardDistantFarm"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(GameObject.Find("Aurenfeld distant farm").GetComponentsInChildren<ParticleSystem>().Length, Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
