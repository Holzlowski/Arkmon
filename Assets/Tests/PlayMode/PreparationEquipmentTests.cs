using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PreparationEquipmentTests
{
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static object Progress(string name) => T("DemoOpening").GetProperty(name).GetValue(null);
    static object Dialogue => T("DialogueBoxController").GetField("instance").GetValue(null);
    static void CloseDialogue() => Dialogue.GetType().GetMethod("CloseDialogue").Invoke(Dialogue, null);
    static void Reset()
    {
        T("DemoOpening").GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);
    }
    static SpriteRenderer PlayerSprite => GameObject.FindWithTag("Player").GetComponentsInChildren<SpriteRenderer>().First(r => r.sprite != null && r.sprite.name.StartsWith("Prince-"));
    static void AssertAppearance(string name)
    {
        Assert.That(PlayerSprite.sprite.name, Is.EqualTo(name));
        var style = Resources.Load("Paper UI");
        var portrait = (Sprite)style.GetType().GetMethod("PortraitFor").Invoke(style, new object[] { "Der Junge" });
        Assert.That(portrait.name, Is.EqualTo(name + "-Portrait"));
        Assert.That(Resources.LoadAll<Sprite>(name + "-Walk").Length, Is.EqualTo(8));
    }
    static IEnumerator Interact(string objectName)
    {
        CloseDialogue(); var item = objectName == "Wardrobe interaction"
            ? ((Component)UnityEngine.Object.FindObjectsByType(T("DemoInteractable"), FindObjectsSortMode.None)
                .First(c => c.GetType().GetField("kind").GetValue(c).ToString() == "Wardrobe")).gameObject
            : GameObject.Find(objectName);
        Assert.That(item, Is.Not.Null);
        var controller = GameObject.FindWithTag("Player").GetComponent(T("PlayerController"));
        var body = controller.GetComponent<Rigidbody>();
        Vector3 position = item.transform.position + new Vector3(0, 0, -.85f); position.y = 1;
        controller.transform.position = position; body.position = position; body.linearVelocity = Vector3.zero;
        Physics.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
        var focus = controller.GetComponent(T("PlayerInteractionFocus"));
        var selected = focus.GetType().GetMethod("Refresh").Invoke(focus, null);
        Assert.That(selected, Is.SameAs(item.GetComponent(T("DemoInteractable"))), "Expected interaction focus at " + objectName + "; got " + (selected as Component)?.name + "; player " + body.position + "; target " + item.transform.position);
        controller.GetType().GetMethod("TryInteract", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller, null);
        float deadline = Time.realtimeSinceStartup + 5;
        while ((bool)Progress("IsPresenting") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Progress("IsPresenting"), Is.False); yield return null;
    }
    static IEnumerator CheckOrder(bool lampFirst)
    {
        Reset(); yield return SceneManager.LoadSceneAsync("TowerRoom"); yield return new WaitForSeconds(1.5f); CloseDialogue();
        AssertAppearance("Prince-Room-NoLamp");
        var lamp = GameObject.Find("Player lamp pickup");
        Assert.That(lamp.transform.IsChildOf(GameObject.Find("Writing desk").transform), Is.True);
        Assert.That(lamp.GetComponent(T("QuestHint")).GetType().GetProperty("IsVisible").GetValue(lamp.GetComponent(T("QuestHint"))), Is.True);
        yield return Interact(lampFirst ? "Player lamp pickup" : "Wardrobe interaction");
        Assert.That(Progress(lampFirst ? "HasLamp" : "HasMantle"), Is.True);
        Assert.That(Progress("IsEquipped"), Is.False);
        AssertAppearance(lampFirst ? "Prince-Room" : "Prince-Mantle-NoLamp");
        CloseDialogue(); T("DemoOpening").GetMethod("LeaveRoom").Invoke(null, null); yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("TowerRoom"));
        yield return Interact(lampFirst ? "Wardrobe interaction" : "Player lamp pickup");
        Assert.That(Progress("IsEquipped"), Is.True); AssertAppearance("Prince-Mantle");
        Assert.That(lamp.transform.Find("Golden player lantern").gameObject.activeSelf, Is.False);
        Assert.That(lamp.GetComponent<Collider>().enabled, Is.False);
        Assert.That(lamp.GetComponent(T("QuestHint")).GetType().GetProperty("IsVisible").GetValue(lamp.GetComponent(T("QuestHint"))), Is.False);
        CloseDialogue(); yield return SceneManager.LoadSceneAsync("TowerRoom"); yield return new WaitForSeconds(.4f);
        AssertAppearance("Prince-Mantle");
        Assert.That(GameObject.Find("Player lamp pickup").GetComponent<Collider>().enabled, Is.False);
        CloseDialogue(); Reset(); LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator LampThenMantle_UpdatesPortraitAndPersists() => CheckOrder(true);
    [UnityTest] public IEnumerator MantleThenLamp_UpdatesPortraitAndPersists() => CheckOrder(false);
}
