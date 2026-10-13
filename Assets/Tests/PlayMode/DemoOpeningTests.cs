using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class DemoOpeningTests
{
    private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Static(string type, string property) => T(type).GetProperty(property).GetValue(null);
    private static object Dialogue => T("DialogueBoxController").GetField("instance").GetValue(null);
    private static void Call(object target, string method) => target.GetType().GetMethod(method).Invoke(target, null);
    private static IEnumerator WaitForDressing()
    {
        float deadline = Time.realtimeSinceStartup + 4;
        while ((bool)Static("DemoOpening", "IsPresenting") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Static("DemoOpening", "IsPresenting"), Is.False);
    }
    [UnityTest]
    public IEnumerator RightWallDoor_IsReachableThroughPlayerInteractionAndParallaxKeepsRoomFixed()
    {
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return SceneManager.LoadSceneAsync("TowerRoom");
        yield return new WaitForSeconds(4f); Call(Dialogue, "CloseDialogue");
        var player = GameObject.FindWithTag("Player");
        Transform background = GameObject.Find("Painted window vista").transform;
        Transform fields = GameObject.Find("Distant field hills").transform;
        Transform door = GameObject.Find("Courtyard door").transform;
        Vector3 doorStart = door.position, farStart = background.position, fieldsStart = fields.position;
        player.transform.position += Vector3.right * 2;
        yield return new WaitForSeconds(1.5f);
        Assert.That(Mathf.Abs(background.position.x - farStart.x), Is.GreaterThan(.01f));
        Assert.That(Mathf.Abs(fields.position.x - fieldsStart.x), Is.LessThan(Mathf.Abs(background.position.x - farStart.x)));
        Assert.That(door.position, Is.EqualTo(doorStart));
        Assert.That(door.position.x, Is.GreaterThan(7));
        T("DemoOpening").GetMethod("TakeMantle").Invoke(null, null); yield return WaitForDressing(); Call(Dialogue, "CloseDialogue");
        player.transform.position = new Vector3(5.9f, 1, 1);
        Physics.SyncTransforms();
        var controller = player.GetComponent(T("PlayerController"));
        controller.GetType().GetMethod("TryInteract", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
        float deadline = Time.realtimeSinceStartup + 10;
        while ((SceneManager.GetActiveScene().name != "StartScene" || (bool)Static("SceneLoadManager", "IsTransitioning")) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StartScene"));
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
    [UnityTest]
    public IEnumerator Opening_RequiresMantleChangesOutfitAndArrivesInWideCourtyard()
    {
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return SceneManager.LoadSceneAsync("TowerRoom");
        yield return new WaitForSeconds(4f);
        Assert.That(Static("DemoOpening", "Started"), Is.EqualTo(true));
        Assert.That(Static("DemoOpening", "HasMantle"), Is.EqualTo(false));
        var portrait = (UnityEngine.UI.Image)T("DialogueBoxController").GetField("portraitImage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Dialogue);
        Assert.That(portrait.sprite.name, Is.EqualTo("Player room portrait"));
        var player = GameObject.FindWithTag("Player");
        Assert.That(player.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sprite != null && r.sprite.name == "Prince-Room"), Is.True);
        Call(Dialogue, "CloseDialogue");
        T("DemoOpening").GetMethod("LeaveRoom").Invoke(null, null);
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("TowerRoom"));
        Call(Dialogue, "CloseDialogue");
        T("DemoOpening").GetMethod("TakeMantle").Invoke(null, null);
        yield return new WaitForSecondsRealtime(.65f);
        var wardrobe = GameObject.Find("Wardrobe");
        Assert.That(Static("DemoOpening", "HasMantle"), Is.False, "The mantle remains hanging while the doors open.");
        Assert.That(wardrobe.transform.Find("Green mantle on hanger").gameObject.activeSelf, Is.True);
        yield return WaitForDressing();
        Assert.That(wardrobe.transform.Find("Green mantle on hanger").gameObject.activeSelf, Is.False);
        Assert.That(Static("DemoOpening", "HasMantle"), Is.EqualTo(true));
        Assert.That(portrait.sprite.name, Is.EqualTo("Player portrait"));
        Assert.That(GameObject.Find("Wardrobe").GetComponentsInChildren<MeshRenderer>().Length, Is.GreaterThan(10));
        Assert.That(GameObject.Find("Wardrobe").GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(0));
        Assert.That(player.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sprite != null && r.sprite.name == "Prince-Mantle"), Is.True);
        Call(Dialogue, "CloseDialogue");
        T("DemoOpening").GetMethod("LeaveRoom").Invoke(null, null);
        float deadline = Time.realtimeSinceStartup + 10;
        while ((SceneManager.GetActiveScene().name != "StartScene" || (bool)Static("SceneLoadManager", "IsTransitioning")) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StartScene"));
        Assert.That(Static("DemoOpening", "EnteredCourtyard"), Is.EqualTo(true));
        Vector3 arrival = GameObject.FindWithTag("Player").transform.position;
        Vector3 entrance = GameObject.Find("Tower entrance").transform.position;
        Assert.That(arrival.x, Is.EqualTo(entrance.x).Within(.1));
        Assert.That(arrival.z, Is.EqualTo(entrance.z - 1.4f).Within(.1));
        var npc = (Component)UnityEngine.Object.FindFirstObjectByType(T("NPC"));
        Assert.That(npc.transform.position.x, Is.GreaterThan(10));
        Assert.That(GameObject.Find("Courtyard stone floor").transform.localScale.x, Is.GreaterThan(40));
        yield return SceneManager.LoadSceneAsync("StartScene"); yield return null;
        Assert.That(Static("DemoOpening", "HasMantle"), Is.EqualTo(true));
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
    [UnityTest]
    public IEnumerator John_UsesExistingCrystalPoseAndRestoresNormalSprite()
    {
        yield return SceneManager.LoadSceneAsync("StartScene"); yield return null;
        var npc = (Component)UnityEngine.Object.FindFirstObjectByType(T("NPC"));
        var visual = npc.GetComponent(T("PaperCharacterVisual"));
        var body = npc.GetComponentsInChildren<SpriteRenderer>().First(r => r.enabled && r.sprite != null);
        Sprite original = body.sprite;
        visual.GetType().GetMethod("SetSummoningPose").Invoke(visual, new object[] { true });
        Assert.That(body.sprite.name, Is.EqualTo("John-Summoning"));
        Vector3 origin = (Vector3)npc.GetComponent(T("NPCSummoningFocus")).GetType().GetProperty("SummonOrigin").GetValue(npc.GetComponent(T("NPCSummoningFocus")));
        Bounds bounds = body.bounds; bounds.Expand(.2f);
        Assert.That(bounds.Contains(origin), Is.True);
        visual.GetType().GetMethod("SetSummoningPose").Invoke(visual, new object[] { false });
        Assert.That(body.sprite, Is.SameAs(original));
    }
    [UnityTest]
    public IEnumerator WakeUp_HidesStandingPlayerThenRestoresBedAndBlocksEarlyExit()
    {
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return SceneManager.LoadSceneAsync("TowerRoom"); yield return new WaitForSeconds(.8f);
        Assert.That(Static("DemoOpening", "IsPresenting"), Is.EqualTo(true));
        SpriteRenderer bed = GameObject.Find("Canopy bed").GetComponentInChildren<SpriteRenderer>();
        Assert.That(bed.sprite.name, Is.EqualTo("Prince-Sleeping-Bed"));
        Assert.That(GameObject.FindWithTag("Player").GetComponentsInChildren<SpriteRenderer>().All(s => !s.enabled), Is.True);
        T("DemoOpening").GetMethod("LeaveRoom").Invoke(null, null);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("TowerRoom"));
        float deadline = Time.realtimeSinceStartup + 6;
        while ((bool)Static("DemoOpening", "IsPresenting") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Static("DemoOpening", "HasAwakened"), Is.EqualTo(true));
        Assert.That(bed.sprite.name, Is.Not.EqualTo("Prince-Sleeping-Bed"));
        Assert.That(GameObject.FindWithTag("Player").GetComponentsInChildren<SpriteRenderer>().Any(s => s.enabled), Is.True);
        yield return SceneManager.LoadSceneAsync("TowerRoom"); yield return new WaitForSeconds(.8f);
        Assert.That(Static("DemoOpening", "IsPresenting"), Is.EqualTo(false), "Waking only plays once per session.");
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
}
