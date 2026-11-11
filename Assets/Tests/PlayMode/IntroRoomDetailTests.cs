using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class IntroRoomDetailTests
{
    private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static void Capture(string name) => typeof(GameIntroTests).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { name });

    [UnityTest]
    public IEnumerator RoomHeraldryGroundAndAlternatingPaperLegs()
    {
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null, null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);
        yield return SceneManager.LoadSceneAsync("TowerRoom");
        yield return new WaitForSeconds(.7f);
        var dialogue = T("DialogueBoxController").GetField("instance").GetValue(null);
        dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue, null);
        var player = GameObject.FindWithTag("Player");
        var body = player.GetComponent<Rigidbody>();
        Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
        ((Behaviour)player.GetComponent(T("PlayerController"))).enabled = false;
        body.position = new Vector3(1.2f, 1, .6f); body.linearVelocity = Vector3.zero;
        Physics.SyncTransforms(); yield return new WaitForSeconds(.6f);
        Assert.That(GameObject.Find("Aurenfeld bed crest"), Is.Not.Null);
        Assert.That(GameObject.Find("Aurenfeld door crest") != null || GameObject.Find("Aurenfeld carved door crest") != null, Is.True);
        Assert.That(GameObject.Find("Tower curved door backing").GetComponent<MeshCollider>(), Is.Not.Null);
        var rug = GameObject.Find("Heirloom rug");
        Assert.That(rug.GetComponent<SpriteRenderer>(), Is.Null, "The rug writes depth as a floor decal instead of sorting over the feet.");
        Assert.That(rug.GetComponent<Renderer>().sharedMaterial.renderQueue, Is.LessThan(2500));
        Capture("intro-room-heraldry");
        var visual = (Behaviour)player.GetComponent(T("PaperCharacterVisual"));
        var update = visual.GetType().GetMethod("UpdateWalkFrame", BindingFlags.NonPublic | BindingFlags.Instance);
        update.Invoke(visual, new object[] { true, .3f });
        Assert.That(visual.GetType().GetProperty("IsPlayingWalkCycle").GetValue(visual), Is.True);
        var rig = visual.GetType().GetField("playerWalkRig", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(visual);
        Assert.That(rig.GetType().GetProperty("Available").GetValue(rig), Is.True);
        var paper = player.GetComponentsInChildren<Transform>().First(t => t.name == "Paper Visual");
        Vector3 displayedScale = paper.localScale;
        visual.enabled = false;
        paper.localScale = displayedScale;
        var present = rig.GetType().GetMethod("Present");
        var near = player.GetComponentsInChildren<Transform>(true).First(t => t.name == "Near leg");
        var far = player.GetComponentsInChildren<Transform>(true).First(t => t.name == "Far leg");
        present.Invoke(rig, new object[] { true, true, false, Mathf.PI / 2 , true, 1f});
        float first = Mathf.DeltaAngle(0, near.localEulerAngles.z);
        Assert.That(first * Mathf.DeltaAngle(0, far.localEulerAngles.z), Is.LessThan(0));
        Capture("elian-walk-near-step");
        present.Invoke(rig, new object[] { true, true, false, Mathf.PI * 1.5f , true, 1f});
        Assert.That(first * Mathf.DeltaAngle(0, near.localEulerAngles.z), Is.LessThan(0), "Opposite half-cycle swaps the leading leg.");
        Capture("elian-walk-far-step");
        for (int i = 0; i < 16; i++)
        {
            present.Invoke(rig, new object[] { true, true, false, i * Mathf.PI / 8 , true, 1f});
            Assert.That(near.GetComponent<Renderer>().bounds.min.y, Is.GreaterThan(.012f));
            Assert.That(far.GetComponent<Renderer>().bounds.min.y, Is.GreaterThan(.012f));
        }
        present.Invoke(rig, new object[] { true, false, true, Mathf.PI / 2 , true, 1f});
        Assert.That(near.parent.localScale.x, Is.LessThan(0));
        Assert.That(player.GetComponentsInChildren<SpriteRenderer>().Any(s => s.sprite != null && s.sprite.name == "Elian-Walk-Room"), Is.True);
        rig.GetType().GetMethod("Hide").Invoke(rig, null);
        Assert.That(near.gameObject.activeInHierarchy, Is.False);
        Camera.main.transform.position = new Vector3(2, 3.3f, -3);
        Camera.main.transform.LookAt(new Vector3(7.2f, 1.8f, 1));
        Camera.main.fieldOfView = 45;
        Capture("intro-door-heraldry");
        LogAssert.NoUnexpectedReceived();
    }
}
