using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardMorningAccentTests
{
    static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    static IEnumerator Load() => (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
    static void Capture(string n) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { n });
    static float Strength(Renderer r) { var b = new MaterialPropertyBlock(); r.GetPropertyBlock(b); return b.GetFloat("_MorningAccentStrength"); }

    [UnityTest]
    public IEnumerator LocalSunlight_RendersAtLandmarksAndGardenAndRestoresProperties()
    {
        yield return Load();
        var root = GameObject.Find("Courtyard morning accents"); Assert.That(root, Is.Not.Null);
        Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
        Assert.That(root.GetComponentsInChildren<Light>(), Is.Empty);
        var style = (Behaviour)root.GetComponent(T("CourtyardMorningAccents"));
        var targets = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => Strength(r) > 0).ToArray();
        Assert.That(targets.Length, Is.GreaterThan(20));
        Assert.That(targets.All(r => r.sharedMaterial.GetFloat("_MorningAccentStrength") == 0), Is.True, "Shared palettes must stay unchanged.");
        var central = GameObject.Find("Castle hall structure").GetComponent<MeshRenderer>();
        Assert.That(Strength(central), Is.Zero, "The rest of the castle does not receive local entrance highlights.");
        var wall = GameObject.Find("East wall north of gate").GetComponent<MeshRenderer>();
        var block = new MaterialPropertyBlock(); wall.GetPropertyBlock(block);
        var moss = block.GetVector("_FootMoss"); var scale = block.GetVector("_ObjectScale");
        var soil = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed").ToArray();
        Assert.That(soil.Length,Is.EqualTo(3));
        Assert.That(soil.All(r=>Strength(r)==0),Is.True,"Watering owns soil appearance; no sunlight accent overrides it.");
        var plants = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Painted garden ")).ToArray();
        var plantColors = plants.Select(r=>r.color).ToArray();
        var player = GameObject.FindWithTag("Player");
        player.transform.position = new Vector3(8.8f,1,1); player.GetComponent<Rigidbody>().position=player.transform.position;
        ((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        yield return new WaitForSeconds(1.3f); Capture("courtyard-garden-morning-gameplay");
        var rig = UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var positions = new[] { new Vector3(-34,7,-5), new Vector3(-31,7,-3), new Vector3(31,7,-12), new Vector3(3,8,-10) };
        var centers = new[] { new Vector3(-34,5,10), new Vector3(-43,2.6f,7), new Vector3(43,3,-1), new Vector3(3,1,6) };
        var names = new[] { "tower", "breach", "gate", "garden" };
        for (int index=0; index<names.Length; index++)
        {
            T("CourtyardCamera").GetMethod("SetScenicView").Invoke(rig, new object[] { positions[index], Quaternion.LookRotation(centers[index]-positions[index]), 48f });
            yield return null; yield return null;
            string name = "courtyard-morning-accent-"+names[index];
            float zoneX = index==0 ? GameObject.Find("Tower entrance").transform.position.x : index==1 ? -42.5f : index==2 ? 43f : GameObject.Find("Garden potting table").transform.position.x;
            var local = targets.Where(r => { var b=new MaterialPropertyBlock(); r.GetPropertyBlock(b); return Mathf.Abs(b.GetVector("_MorningAccent").x-zoneX)<.01f; }).ToArray();
            Assert.That(local.Length,Is.GreaterThan(0));
            var previous = local.Select(r => Strength(r)).ToArray();
            Capture(name);
            try
            {
                foreach(var r in local) { var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetFloat("_MorningAccentStrength",0);r.SetPropertyBlock(b); }
                Capture(name+"-disabled");
            }
            finally
            {
                for(int n=0;n<local.Length;n++) { var b=new MaterialPropertyBlock();local[n].GetPropertyBlock(b);b.SetFloat("_MorningAccentStrength",previous[n]);local[n].SetPropertyBlock(b); }
            }
            string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","Builds"));
            var on=new Texture2D(2,2); var off=new Texture2D(2,2);
            try
            {
                on.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder,name+".png")));
                off.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder,name+"-disabled.png")));
                var a=on.GetPixels32(); var b=off.GetPixels32(); int brighter=0;
                for(int i=0;i<a.Length;i++) if(a[i].r+a[i].g+a[i].b-b[i].r-b[i].g-b[i].b>12) brighter++;
                Assert.That(brighter,Is.GreaterThan(500),names[index]+" needs a visible, locally lit surface.");
                TestContext.WriteLine(names[index]+" visibly highlighted pixels: "+brighter);
            }
            finally { UnityEngine.Object.Destroy(on); UnityEngine.Object.Destroy(off); }
        }
        // Toggling accents must not change the crop tint or the wet soil color owned by watering.
        Assert.That(plants.Select(r=>r.color).ToArray(),Is.EqualTo(plantColors));
        var wateringPoints = UnityEngine.Object.FindObjectsByType(T("CourtyardGarden"),FindObjectsSortMode.None).Cast<Component>().Where(c=>c.name=="Garden watering point").ToArray();
        var wetMethod = T("CourtyardGarden").GetMethod("SetWetness",BindingFlags.NonPublic|BindingFlags.Instance);
        foreach(var point in wateringPoints) wetMethod.Invoke(point,new object[]{1f});
        var wetColors = soil.Select(r=> {var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);return b.GetColor("_BaseColor");}).ToArray();
        for(int i=0;i<soil.Length;i++) Assert.That(wetColors[i].grayscale,Is.LessThan(soil[i].sharedMaterial.GetColor("_BaseColor").grayscale*.7f));
        var wetPlants=plants.Select(r=>r.color).ToArray();
        Capture("courtyard-garden-morning-wet");
        style.enabled=false;
        for(int i=0;i<soil.Length;i++) {var b=new MaterialPropertyBlock();soil[i].GetPropertyBlock(b);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(wetColors[i]));}
        Assert.That(plants.Select(r=>r.color).ToArray(),Is.EqualTo(wetPlants));
        Assert.That(targets.All(r=>Strength(r)==0),Is.True);
        wall.GetPropertyBlock(block); Assert.That(block.GetVector("_FootMoss"),Is.EqualTo(moss)); Assert.That(block.GetVector("_ObjectScale"),Is.EqualTo(scale));
        block.SetFloat("_Contrast",1.12f);wall.SetPropertyBlock(block);
        style.enabled=true; UnityEngine.Object.Destroy(root);yield return null;
        wall.GetPropertyBlock(block);Assert.That(Strength(wall),Is.Zero);Assert.That(block.GetFloat("_Contrast"),Is.EqualTo(1.12f));
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardMorningAccents"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
