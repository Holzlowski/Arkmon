using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardKaflaTests
{
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Static;
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static object Call(object target,string name) => target.GetType().GetMethod(name).Invoke(target,null);
    static object Read(object target,string name) => target.GetType().GetProperty(name).GetValue(target);

    [UnityTest] public IEnumerator KaflaPractisesWithoutCombatAndGreetsElian()
    {
        T("DemoOpening").GetMethod("ResetSession",Hidden).Invoke(null,null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",Hidden).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("StartScene");
        yield return new WaitForSeconds(.8f);
        var dialogue = T("DialogueBoxController").GetField("instance").GetValue(null);
        Call(dialogue,"CloseDialogue");
        var kafla = GameObject.Find("Courtyard Kafla");
        Assert.That(kafla,Is.Not.Null);
        var yard = GameObject.Find("Guard training yard");
        Assert.That(yard,Is.Not.Null);
        var target = yard.GetComponent(T("CourtyardTrainingYard"));
        Assert.That(yard.transform.Find("Guard training dummy"),Is.Not.Null);
        Assert.That(kafla.GetComponentInChildren(T("Unit"),true),Is.Null);
        var trigger = kafla.GetComponent<SphereCollider>();
        Assert.That(trigger.enabled,Is.False);
        var completion = T("CourtyardStarterSummoning").GetField("<Completed>k__BackingField",Hidden);
        var greeted = T("ArkanmeisterinArrival").GetField("<Greeted>k__BackingField",Hidden);
        completion.SetValue(null,true);
        greeted.SetValue(null,true);
        try
        {
            yield return null;
            Assert.That(trigger.enabled,Is.True);
            var sword = kafla.GetComponentInChildren(T("PaperSwordAnimation"));
            var player = GameObject.FindWithTag("Player");
            ((Behaviour)player.GetComponent(T("PlayerController"))).enabled = false;
            var rb = player.GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
            rb.position = player.transform.position = kafla.transform.position+new Vector3(3.3f,.9f,-.8f);
            Physics.SyncTransforms();
            float practiceDeadline = Time.time + 3f;
            yield return new WaitUntil(() => (bool)Read(target,"IsReacting") || Time.time >= practiceDeadline);
            Assert.That(Read(target,"IsReacting"),Is.True);
            var figure = ((Component)sword).transform;
            Assert.That(figure.localScale.x,Is.LessThan(0));
            Capture("kafla-trainingsplatz");

            rb.position = player.transform.position = kafla.transform.position+new Vector3(1.6f,.9f,-.8f);
            Physics.SyncTransforms();
            yield return null;
            var focus = player.GetComponent(T("PlayerInteractionFocus"));
            var actor = kafla.GetComponent(T("CourtyardKafla"));
            Assert.That(Call(focus,"Refresh"),Is.SameAs(actor));
            Call(actor,"InteractWithPlayer");
            Assert.That(Read(dialogue,"ActiveSpeaker"),Is.EqualTo("Elian"));
            yield return Advance(dialogue);
            yield return new WaitForSeconds(.94f);
            Assert.That(Read(sword,"IsSwinging"),Is.True);
            Assert.That(Read(target,"IsReacting"),Is.True);
            Capture("kafla-uebung");
            Call(dialogue,"CloseDialogue");
            Assert.That(Read(sword,"IsSwinging"),Is.False);

            Call(focus,"Refresh");
            Call(actor,"InteractWithPlayer");
            yield return Advance(dialogue);
            yield return new WaitForSeconds(1.65f);
            Assert.That(Read(dialogue,"ActiveSpeaker"),Is.EqualTo("Kafla"));
            Assert.That(figure.localScale.x,Is.GreaterThan(0));
            Assert.That(Read(dialogue,"ActivePortrait"),Is.Not.Null);
            Assert.That(Read(sword,"IsSwinging"),Is.False);
            Call(dialogue,"SkipLine");
            yield return null;
            Capture("kafla-begruessung");
            Call(dialogue,"CloseDialogue");
        }
        finally { completion.SetValue(null,false); greeted.SetValue(null,false); }
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
        if (Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") != null)
            typeof(GameIntroTests).GetMethod("Capture",Hidden).Invoke(null,new object[] {name});
    }
}
