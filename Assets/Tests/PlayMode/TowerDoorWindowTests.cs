using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TowerDoorWindowTests
{
    private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static void Capture(string name) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { name });

    [UnityTest]
    public IEnumerator PlayerStopsBeforeLitDoorAndMullionsReachFrame()
    {
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null, null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);
        yield return SceneManager.LoadSceneAsync("TowerRoom"); yield return new WaitForSeconds(.6f);
        var dialogue = T("DialogueBoxController").GetField("instance").GetValue(null);
        dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue, null);
        var player = GameObject.FindWithTag("Player");
        ((Behaviour)player.GetComponent(T("PlayerController"))).enabled = false;
        var body = player.GetComponent<Rigidbody>();
        var door = GameObject.Find("Courtyard door").transform;
        body.position = door.TransformPoint(new Vector3(-2.2f, 1, 0)); body.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        for (int i = 0; i < 70; i++)
        { body.linearVelocity = door.right * 5 + Vector3.up * body.linearVelocity.y; yield return new WaitForFixedUpdate(); }
        body.linearVelocity = Vector3.zero; yield return null;
        Assert.That(door.InverseTransformPoint(body.position).x, Is.LessThan(-.95f), "The body must stop far enough ahead of the illustrated door.");
        var paper = player.GetComponentsInChildren<SpriteRenderer>().First(s => s.gameObject.name == "Paper Visual");
        Bounds bounds = paper.bounds;
        float furthest = float.NegativeInfinity;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i&1)==0?-1:1, (i&2)==0?-1:1, (i&4)==0?-1:1));
            furthest = Mathf.Max(furthest, Vector3.Dot(corner-door.position, door.right));
        }
        bool modelled = door.GetComponent(T("TowerRoomDoor")) != null;
        Assert.That(furthest, Is.LessThan(modelled ? -.37f : -.025f), "The paper silhouette, not only its physics collider, stays in front of the door plane.");
        var surface = door.GetComponentsInChildren<MeshRenderer>().First(r => r.name == "Lit door surface" || r.name == "Door green oak planks");
        Assert.That(surface.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
        Assert.That(surface.sharedMaterial.IsKeywordEnabled("_EMISSION"), Is.True, "Unity must preserve the subtle readability fill on import.");
        Capture("tower-door-collision-overview");
        Camera.main.transform.position = new Vector3(2, 3.3f, -3);
        Camera.main.transform.LookAt(new Vector3(7.2f, 1.8f, 1)); Camera.main.fieldOfView = 45;
        Capture("tower-door-lit-collision");
        var frame = GameObject.Find("Window rounded limestone frame").GetComponent<Renderer>().bounds;
        var sill = GameObject.Find("Polished window sill").GetComponent<Renderer>().bounds;
        var bars = GameObject.Find("Window slender mullions").GetComponentsInChildren<Renderer>();
        var vertical = bars.First(r => r.bounds.size.y > r.bounds.size.x).bounds;
        var horizontal = bars.First(r => r.bounds.size.x > r.bounds.size.y).bounds;
        Assert.That(vertical.min.y, Is.LessThanOrEqualTo(sill.max.y));
        Assert.That(vertical.max.y, Is.GreaterThan(frame.max.y-.18f));
        Assert.That(horizontal.size.x, Is.GreaterThan(2.7f));
        Assert.That(Mathf.Abs(vertical.center.z-frame.center.z), Is.LessThan(.08f));
        Camera.main.transform.position = new Vector3(0, 3.2f, .5f);
        Camera.main.transform.LookAt(new Vector3(0, 3.3f, 6));
        Capture("tower-window-attached-mullions");
        LogAssert.NoUnexpectedReceived();
    }
}
