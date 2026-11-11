using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardPanfrieTests
{
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Static;
    static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static object Call(object target,string name) => target.GetType().GetMethod(name).Invoke(target,null);
    static object Read(object target,string property) => target.GetType().GetProperty(property).GetValue(target);

    [UnityTest] public IEnumerator PanfrieAppearsAfterTrialAndReturnsToCoverWhenConversationCloses()
    {
        RuntimeType("DemoOpening").GetMethod("ResetSession",Hidden).Invoke(null,null);
        RuntimeType("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        RuntimeType("DemoOpening").GetField("<HasAwakened>k__BackingField",Hidden).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("StartScene");
        yield return new WaitForSeconds(.75f);
        var dialogue = RuntimeType("DialogueBoxController").GetField("instance").GetValue(null);
        Call(dialogue,"CloseDialogue");
        var panfrie = GameObject.Find("Courtyard Panfrie");
        Assert.That(panfrie,Is.Not.Null);
        var interaction = panfrie.GetComponent(RuntimeType("CourtyardPanfrie"));
        var trigger = panfrie.GetComponent<SphereCollider>();
        var paper = panfrie.GetComponentInChildren<SpriteRenderer>();
        Assert.That(trigger.enabled || paper.enabled,Is.False);

        var completion = RuntimeType("CourtyardStarterSummoning").GetField("<Completed>k__BackingField",Hidden);
        var greeted = RuntimeType("ArkanmeisterinArrival").GetField("<Greeted>k__BackingField",Hidden);
        greeted.SetValue(null,true);
        completion.SetValue(null,true);
        try
        {
            yield return null;
            Assert.That(trigger.enabled && paper.enabled,Is.True);
            var anchor = panfrie.transform.position;
            var stance = paper.transform.parent;
            var hidingSpot = stance.position;
            var well = GameObject.Find("Courtyard draw well").transform;
            Assert.That(hidingSpot.z,Is.GreaterThan(well.position.z+1f));
            var cover = paper.sprite;
            Assert.That(cover.name,Is.EqualTo("Panfrie Deckung"));
            Assert.That(Mathf.Abs(anchor.y),Is.LessThan(1f));
            var player = GameObject.FindWithTag("Player");
            ((Behaviour)player.GetComponent(RuntimeType("PlayerController"))).enabled = false;
            var body = player.GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            body.isKinematic = true;
            var at = anchor + new Vector3(-1.65f,.95f,-.55f);
            body.position = player.transform.position = at;
            Physics.SyncTransforms();
            var focus = player.GetComponent(RuntimeType("PlayerInteractionFocus"));
            yield return new WaitForSeconds(1.4f);
            Assert.That(Call(focus,"Refresh"),Is.SameAs(interaction));
            Assert.That(Read(interaction,"Label"),Is.EqualTo("Panfrie rufen"));
            Capture("panfrie-versteckt");

            Call(interaction,"InteractWithPlayer");
            yield return Advance(dialogue);
            yield return new WaitForSecondsRealtime(.66f);
            Assert.That(Vector3.Distance(stance.position,hidingSpot),Is.GreaterThan(.05f));
            Capture("panfrie-kommt-hervor");
            Call(dialogue,"CloseDialogue");
            Assert.That(stance.position,Is.EqualTo(hidingSpot),"Cancel before landing returns Panfrie to the hiding spot.");

            Call(interaction,"InteractWithPlayer");
            Assert.That(Read(dialogue,"ActiveSpeaker"),Is.EqualTo("Elian"));
            yield return Advance(dialogue);
            Assert.That(paper.sprite,Is.SameAs(cover),"The shield pose holds during the shy pause.");
            yield return new WaitForSecondsRealtime(1.72f);
            Assert.That(Read(dialogue,"ActiveSpeaker"),Is.EqualTo("Panfrie"));
            Assert.That(stance.position,Is.EqualTo(anchor),"Panfrie lands at the conversation spot before replying.");
            Assert.That(paper.sprite.name,Is.EqualTo("Panfrie Hervorlugen"));
            Assert.That(panfrie.transform.position,Is.EqualTo(anchor));
            Assert.That(Read(dialogue,"ActivePortrait"),Is.Not.Null);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(Read(interaction,"Label"),Is.EqualTo("Mit Panfrie sprechen"));
            Capture("panfrie-draussen");
            Call(dialogue,"CloseDialogue");
            Assert.That(paper.sprite,Is.SameAs(cover));

            Call(focus,"Refresh");
            Call(interaction,"InteractWithPlayer");
            yield return Advance(dialogue);
            Call(dialogue,"CloseDialogue");
            yield return new WaitForSecondsRealtime(.75f);
            Assert.That(Read(dialogue,"IsOpen"),Is.False);
            Assert.That(paper.sprite,Is.SameAs(cover),"Cancellation must stop the delayed pose change.");
            Assert.That(stance.position,Is.EqualTo(anchor),"Panfrie stays out after the first completed reveal.");
        }
        finally
        {
            completion.SetValue(null,false);
            greeted.SetValue(null,false);
        }
    }

    static IEnumerator Advance(object dialogue)
    {
        Call(dialogue,"SkipLine");
        yield return null;
        yield return null;
        Call(dialogue,"SkipLine");
        yield return null;
    }

    static void Capture(string name)
    {
        if (Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") == null) return;
        typeof(GameIntroTests).GetMethod("Capture",Hidden).Invoke(null,new object[] {name});
    }
}
