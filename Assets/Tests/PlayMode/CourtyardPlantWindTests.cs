using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardPlantWindTests
{
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static IEnumerator Load() => (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
    static Color32[] Render(Camera camera, RenderTexture target, string path = null)
    {
        camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            if (path != null) File.WriteAllBytes(path, texture.EncodeToPNG());
            return texture.GetPixels32();
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.Destroy(texture); }
    }
    static long Difference(Color32[] a, Color32[] b, int width, int fromY, int toY)
    {
        long sum = 0;
        for (int y = fromY; y < toY; y++) for (int x = 0; x < width; x++)
        { int i = y * width + x; sum += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b); }
        return sum;
    }
    [UnityTest] public IEnumerator Wind_BendsVisiblePlants_PreservesRootsTintAndLifecycle()
    {
        yield return Load();
        var owner = GameObject.Find("Courtyard gentle plant wind"); Assert.That(owner, Is.Not.Null);
        var effect = (Behaviour)owner.GetComponent(T("CourtyardPlantWind"));
        var source = Resources.Load<Sprite>("CourtyardStyle/Fern"); int originalVertices = source.vertices.Length;
        var plants = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(r => r.sharedMaterial != null && r.sharedMaterial.shader.name == "Arkmon/Courtyard Plant Wind").ToArray();
        Assert.That(plants.Length, Is.GreaterThan(20));
        Assert.That(plants.Select(r => r.sprite).Distinct().Count(), Is.LessThanOrEqualTo(9), "Share subdivided resources across plants.");
        Assert.That(plants.All(r => r.sharedMaterial.shader.isSupported), Is.True);
        var trees=plants.Where(r=>r.sharedMaterial.GetVector("_PlantShape").w>.5f).ToArray();
        Assert.That(trees.Length,Is.GreaterThan(10),"Tree species must receive wind after tree styling.");
        var fern = plants.First(r => r.sprite.name == "Fern wind mesh");
        var position = fern.transform.position; var scale = fern.transform.localScale;
        var oldSprite = fern.sprite; var oldMaterial = fern.sharedMaterial;
        Assert.That(oldSprite.vertices.Length, Is.GreaterThan(originalVertices));
        var character = GameObject.FindWithTag("Player").GetComponentsInChildren<SpriteRenderer>().First();
        Assert.That(plants.Contains(character), Is.False);
        var vegetables = GameObject.Find("Illustrated vegetable bed").GetComponentsInChildren<SpriteRenderer>();
        Assert.That(vegetables.Any(r => plants.Contains(r)), Is.False);
        var specimen = new GameObject("Wind render specimen", typeof(SpriteRenderer)); specimen.layer = 30;
        var illustration = specimen.GetComponent<SpriteRenderer>(); illustration.sprite = oldSprite; illustration.sharedMaterial = oldMaterial;
        illustration.color = new Color(.82f, .94f, .71f); illustration.flipX = true;
        specimen.transform.localScale = Vector3.one / source.bounds.size.y;
        var cameraObject = new GameObject("Wind render camera", typeof(Camera)); var camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false; camera.cullingMask = 1 << 30; camera.orthographic = true; camera.orthographicSize = .7f;
        camera.transform.position = new Vector3(0, .5f, -5); camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.25f, .31f, .17f);
        var target = new RenderTexture(640, 480, 24); target.Create(); camera.targetTexture = target;
        var still = new Material(oldMaterial); var shape = still.GetVector("_PlantShape"); shape.z = 0; still.SetVector("_PlantShape", shape);
        var directory = Path.GetFullPath("Builds/plant-wind-preview"); Directory.CreateDirectory(directory);
        try
        {
            // Equal tint, flipped UVs and overall silhouette when wind is set to zero.
            illustration.sharedMaterial = still;
            yield return null;
            var reference = Render(camera, target);
            illustration.sprite = source; illustration.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            var originalMaterial = illustration.sharedMaterial;
            var original = Render(camera, target);
            long baselineDifference = Difference(reference, original, 640, 0, 480);
            Assert.That(baselineDifference / (640f * 480 * 3), Is.LessThan(2), "Wind must retain the existing sprite color, UVs and alpha.");
            UnityEngine.Object.Destroy(originalMaterial);
            illustration.sprite = oldSprite; illustration.sharedMaterial = oldMaterial;
            var first = Render(camera, target); long tipChange = 0, rootChange = 0;
            for (int i = 0; i < 24; i++)
            {
                yield return new WaitForSeconds(.12f);
                var frame = Render(camera, target, Path.Combine(directory, i.ToString("D2") + ".png"));
                tipChange = Math.Max(tipChange, Difference(first, frame, 640, 200, 400));
                rootChange = Math.Max(rootChange, Difference(first, frame, 640, 69, 85));
            }
            Assert.That(tipChange, Is.GreaterThan(10000), "The GPU must actually bend the visible upper leaves.");
            Assert.That(rootChange / 16f, Is.LessThan(tipChange / 200f * .2f), "The planted edge should stay visually stable.");
            var oak=trees.First(r=>r.sprite.name=="Oak wind mesh");
            illustration.sprite=oak.sprite;illustration.sharedMaterial=oak.sharedMaterial;
            specimen.transform.localScale=Vector3.one/Resources.Load<Sprite>("CourtyardStyle/Trees/Oak").bounds.size.y;
            var treeFirst=Render(camera,target);long crownChange=0,trunkChange=0;
            for(int i=0;i<12;i++)
            {
                yield return new WaitForSeconds(.15f);
                var frame=Render(camera,target,Path.Combine(directory,"tree-"+i.ToString("D2")+".png"));
                crownChange=Math.Max(crownChange,Difference(treeFirst,frame,640,210,400));
                trunkChange=Math.Max(trunkChange,Difference(treeFirst,frame,640,70,130));
            }
            Assert.That(crownChange,Is.GreaterThan(10000),"Tree crown must visibly move.");
            Assert.That(trunkChange/60f,Is.LessThan(crownChange/190f*.2f),"Tree base must remain planted.");
            Assert.That(fern.transform.position, Is.EqualTo(position)); Assert.That(fern.transform.localScale, Is.EqualTo(scale));
            Assert.That(source.vertices.Length, Is.EqualTo(originalVertices), "Never rewrite the imported sprite.");
        }
        finally
        {
            camera.targetTexture = null; target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(still);
            UnityEngine.Object.Destroy(cameraObject); UnityEngine.Object.Destroy(specimen);
        }
        effect.enabled = false; yield return null;
        Assert.That(fern.sprite, Is.SameAs(source)); Assert.That(fern.sharedMaterial, Is.Not.SameAs(oldMaterial));
        effect.enabled = true; yield return null;
        Assert.That(fern.sprite, Is.SameAs(oldSprite)); Assert.That(fern.sharedMaterial, Is.SameAs(oldMaterial));
        var body = GameObject.FindWithTag("Player").GetComponent<Rigidbody>(); body.position = new Vector3(37, 1, -1); body.transform.position = body.position;
        yield return new WaitForSeconds(1.2f);
        typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "courtyard-plant-wind-gameplay" });
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardPlantWind"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
