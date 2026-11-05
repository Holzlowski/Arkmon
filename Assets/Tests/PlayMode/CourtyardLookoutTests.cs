using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CourtyardLookoutTests
{
    static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n).Invoke(o, args);
    static bool Busy => (bool)T("CourtyardLookout").GetProperty("IsPresenting").GetValue(null);
    static IEnumerator Load() => (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });
    static void Capture(string n) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { n });
    static Component Point => GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout"));
    static IEnumerator Return(Component point)
    {
        Call(point, "ReturnToCourtyard");
        float deadline = Time.realtimeSinceStartup + 5;
        while (Busy && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Busy, Is.False, "The view must release the exploration lock.");
    }

    [UnityTest]
    public IEnumerator Lookout_CancelsRepeatsAndRestoresCameraInputAndHud()
    {
        yield return Load();
        var point = Point;
        var fieldAtmosphere = GameObject.Find("Aurenfeld field atmosphere");
        var fieldAudio = fieldAtmosphere.GetComponent<AudioSource>();
        Assert.That(fieldAudio.clip, Is.Not.Null);
        Assert.That(fieldAudio.volume, Is.Zero, "The field loop must not play over courtyard conversations.");
        var margins = GameObject.Find("Western field margins");
        Assert.That(margins, Is.Not.Null);
        Assert.That(margins.GetComponentsInChildren<Collider>(), Is.Empty, "Distant field dressing must not introduce movement obstacles.");
        var trees = margins.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "Western field tree").ToArray();
        Assert.That(trees.Length, Is.EqualTo(12));
        var shadows = GameObject.Find("Western grove morning shadows"); Assert.That(shadows, Is.Not.Null);
        Assert.That(shadows.GetComponentsInChildren<Collider>(), Is.Empty);
        var shadowRenderers = shadows.GetComponentsInChildren<MeshRenderer>();
        Assert.That(shadowRenderers.Length, Is.EqualTo(trees.Length * 2));
        Assert.That(shadowRenderers.All(r=>r.sharedMaterial.shader.isSupported), Is.True);
        var casters=shadowRenderers.Where(r=>r.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly).ToArray();
        Assert.That(casters.Length,Is.EqualTo(trees.Length),"Each illustrated tree must cast a real shadow onto the grass.");
        Assert.That(casters.All(r=>r.sharedMaterial.IsKeywordEnabled("_ALPHATEST_ON")),Is.True,"Only the opaque tree silhouette casts shadows, never the rectangular card.");
        var contacts=shadows.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Tree root contact shadow").ToArray();
        Assert.That(contacts.All(f=>f.sharedMesh.vertices.Max(v=>v.y)-f.sharedMesh.vertices.Min(v=>v.y)>.001f),Is.True,"Contacts follow the sloping ground.");
        foreach(var tree in trees)tree.enabled=false;yield return null;yield return null;
        Assert.That(shadowRenderers.All(r=>!r.enabled),Is.True,"Hidden tree illustrations must not leave ghost shadows.");
        foreach(var tree in trees)tree.enabled=true;yield return null;yield return null;
        Assert.That(shadowRenderers.All(r=>r.enabled),Is.True);
        var mountains = GameObject.Find("Western painted mountains").GetComponent<Renderer>();
        Assert.That(trees.All(r => r.sortingOrder > mountains.sortingOrder), Is.True, "Distant painted mountains must never draw over nearer tree crowns.");
        Assert.That(trees.All(r => r.transform.position.x < -45 && r.sprite != null), Is.True, "All groves stay outside the courtyard.");
        var ribbon = GameObject.Find("Western winding field path").GetComponent<MeshFilter>();
        Assert.That(ribbon.sharedMesh.normals.All(n => n.y > 0), Is.True, "Path surfaces must face upwards.");
        var scenery = GameObject.Find("Western painted horizon");
        Assert.That(scenery.GetComponentsInChildren<Collider>(), Is.Empty);
        Assert.That(scenery.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterial != null && r.sharedMaterial.shader.isSupported), Is.True);
        var clouds = GameObject.Find("Western drifting clouds").transform;
        var cloudStart = clouds.position;
        Call(point, "InteractWithPlayer"); Assert.That(Busy, Is.False, "Cannot start from the distant garden.");
        CourtyardGardenTaskTests.MoveTo(point);
        var player = GameObject.FindWithTag("Player");
        var body = player.GetComponent<Rigidbody>(); body.isKinematic = false;
        var controller = (Behaviour)player.GetComponent(T("PlayerController")); controller.enabled = true;
        yield return new WaitForSeconds(1.5f);
        var focus = player.GetComponent(T("PlayerInteractionFocus"));
        Assert.That(Call(focus, "Refresh"), Is.EqualTo(point), "The actual E target must resolve to the lookout.");
        Capture("lookout-interaction");
        var camera = Camera.main; float fov = camera.fieldOfView;
        var position = camera.transform.position; var rotation = camera.transform.rotation;
        var objective = GameObject.Find("Demo objective").GetComponent<Canvas>();
        var playerPosition = player.transform.position;
        Call(point, "InteractWithPlayer"); Call(point, "InteractWithPlayer");
        Assert.That(Busy, Is.True); Assert.That(objective.enabled, Is.False);
        Assert.That(Call(focus, "Refresh"), Is.Null);
        Assert.That(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Count(c => c.name == "Field lookout overlay"), Is.EqualTo(1));
        // A queued movement input must be suppressed by the shared presentation lock.
        T("PlayerController").GetField("moveInput", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, Vector2.right);
        yield return new WaitForFixedUpdate();
        Assert.That(Mathf.Abs(body.linearVelocity.x), Is.LessThan(.01f));
        yield return new WaitForSeconds(.7f);
        yield return Return(point);
        Assert.That(camera.fieldOfView, Is.EqualTo(fov).Within(.01f));
        Assert.That(Vector3.Distance(camera.transform.position, position), Is.LessThan(.12f));
        Assert.That(Quaternion.Angle(camera.transform.rotation, rotation), Is.LessThan(.1f));
        Assert.That(objective.enabled, Is.True);
        Assert.That((bool)T("DemoOpening").GetProperty("IsPresenting").GetValue(null), Is.False);
        Call(point, "InteractWithPlayer");
        for(int stage=1;stage<=10;stage++)
        {
            float target=stage/10f;
            while((float)point.GetType().GetProperty("Progress").GetValue(point)<target-.001f)yield return null;
            yield return null;Capture("lookout-flight-"+stage.ToString("00"));
        }
        Capture("lookout-fields-panorama");
        Assert.That(fieldAudio.volume, Is.GreaterThan(0), "The scenic view should fade its ambient bed in.");
        if(Application.isBatchMode) Assert.That(fieldAudio.isPlaying, Is.False, "Batch tests must remain silent.");
        // Render both states in one frame so wind/cloud motion cannot fake a shadow difference.
        Capture("lookout-tree-shadows");
        foreach (var caster in casters) caster.enabled = false;
        Capture("lookout-tree-shadows-disabled");
        foreach (var caster in casters) caster.enabled = true;
        string captureFolder = Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Builds"));
        var shadowImage = new Texture2D(2, 2); var clearImage = new Texture2D(2, 2);
        int darkerPixels = 0;
        try
        {
            shadowImage.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(captureFolder, "lookout-tree-shadows.png")));
            clearImage.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(captureFolder, "lookout-tree-shadows-disabled.png")));
            var dark = shadowImage.GetPixels32(); var clear = clearImage.GetPixels32();
            for (int i = 0; i < dark.Length; i++)
                if ((clear[i].r + clear[i].g + clear[i].b) - (dark[i].r + dark[i].g + dark[i].b) > 18) darkerPixels++;
        }
        finally { UnityEngine.Object.Destroy(shadowImage); UnityEngine.Object.Destroy(clearImage); }
        TestContext.WriteLine("Tree shadow pixels visibly darkened: " + darkerPixels);
        Assert.That(darkerPixels, Is.GreaterThan(1000), "Tree casters must visibly darken the rendered meadow, not merely exist in the hierarchy.");
        Assert.That((float)point.GetType().GetProperty("Progress").GetValue(point), Is.EqualTo(1).Within(.001f));
        Assert.That(Vector3.Distance(clouds.position, cloudStart), Is.GreaterThan(.1f));
        Assert.That(Vector3.Distance(playerPosition, player.transform.position), Is.LessThan(.15f));
        GameObject.Find("Return to courtyard").GetComponent<Button>().onClick.Invoke();
        yield return Return(point);
        yield return new WaitForSeconds(1.6f);
        Assert.That(fieldAudio.volume, Is.Zero, "Ambient audio must fade away after leaving the lookout.");
        Assert.That(Call(focus, "Refresh"), Is.EqualTo(point));
        Assert.That(((Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"))).enabled, Is.False);
        Call(point, "InteractWithPlayer"); yield return new WaitForSeconds(.3f);
        ((Behaviour)point).enabled = false; yield return null;
        Assert.That(Busy, Is.False); Assert.That(objective.enabled, Is.True);
        ((Behaviour)point).enabled = true; Call(point, "InteractWithPlayer");
        yield return new WaitForSeconds(.2f); yield return Load();
        Assert.That(fieldAudio == null, Is.True, "Unloading the scene must dispose the old loop source.");
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardFieldAtmosphere"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(Busy, Is.False);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardLookout"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(GameObject.Find("Field lookout overlay"), Is.Null);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardTreeShadows"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardFieldMargins"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(GameObject.Find("Demo objective").GetComponent<Canvas>().enabled, Is.True);
        LogAssert.NoUnexpectedReceived();
    }
}
