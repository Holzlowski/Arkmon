using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardStarterMeetingsTests
{
    const BindingFlags HiddenStatic = BindingFlags.NonPublic | BindingFlags.Static;
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Call(object target,string name) => target.GetType().GetMethod(name).Invoke(target,null);
    static object Dialogue => T("DialogueBoxController").GetField("instance").GetValue(null);
    static string Text => ((TMP_Text)Dialogue.GetType().GetField("dialogueText",BindingFlags.Instance|BindingFlags.NonPublic)
        .GetValue(Dialogue)).text;

    [UnityTest] public IEnumerator RaukReturnsToTheBranchAndFourMeetingsUnlockTheBriefing()
    {
        T("DemoOpening").GetMethod("ResetSession",HiddenStatic).Invoke(null,null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",HiddenStatic).SetValue(null,true);
        var arrival = T("ArkanmeisterinArrival");
        arrival.GetField("<Arrived>k__BackingField",HiddenStatic).SetValue(null,true);
        arrival.GetField("<Greeted>k__BackingField",HiddenStatic).SetValue(null,true);
        var completed = T("CourtyardStarterSummoning").GetField("<Completed>k__BackingField",HiddenStatic);
        var vessel = T("CourtyardVesselTrial").GetField("<Completed>k__BackingField",HiddenStatic);
        var meetings = T("CourtyardStarterMeetings");
        var ready = meetings.GetProperty("ReadyForBriefing",HiddenStatic);
        void Meet(string name) => meetings.GetMethod("Meet",HiddenStatic)
            .Invoke(null,new[] { Enum.Parse(meetings.GetNestedType("Starter",BindingFlags.NonPublic),name) });
        void Capture(string name)
        {
            if (Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT") != null)
                typeof(GameIntroTests).GetMethod("Capture",HiddenStatic).Invoke(null,new object[] {name});
        }
        yield return SceneManager.LoadSceneAsync("StartScene");
        yield return new WaitForSeconds(.8f);
        Call(Dialogue,"CloseDialogue");
        var rauk = GameObject.Find("Courtyard Rauk");
        var liora = GameObject.Find("Courtyard Liora");
        Assert.That(rauk,Is.Not.Null);
        Assert.That(liora,Is.Not.Null);
        var tree = GameObject.Find("Rauk's oak").GetComponent<SpriteRenderer>();
        var wall = GameObject.Find("East wall north of gate").GetComponent<Renderer>();
        Assert.That(tree.bounds.max.x,Is.LessThan(wall.bounds.min.x-.35f));
        var raukTrigger = rauk.GetComponent<SphereCollider>();
        var lioraTrigger = liora.GetComponent<SphereCollider>();
        Assert.That(raukTrigger.enabled || lioraTrigger.enabled,Is.False);

        vessel.SetValue(null,true);
        completed.SetValue(null,true);
        try
        {
            yield return null;
            Assert.That(raukTrigger.enabled && lioraTrigger.enabled,Is.True);
            var bird = rauk.GetComponentInChildren<SpriteRenderer>();
            Assert.That(bird.sprite.name,Is.EqualTo("Rauk perched"));
            var player = GameObject.FindWithTag("Player");
            ((Behaviour)player.GetComponent(T("PlayerController"))).enabled = false;
            var rb = player.GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
            void Move(Vector3 position)
            {
                rb.position=player.transform.position=position;
                Physics.SyncTransforms();
            }

            var mentor = GameObject.Find("Arkanmeisterin").GetComponent(arrival);
            Call(mentor,"InteractWithPlayer");
            Call(Dialogue,"SkipLine");
            yield return null;
            Assert.That(Text,Does.Contain("allen vier"));
            Call(Dialogue,"CloseDialogue");

            Move(liora.transform.position+new Vector3(1.13f,.88f,-.25f));
            yield return new WaitForSeconds(.6f);
            var focus = player.GetComponent(T("PlayerInteractionFocus"));
            Assert.That(Call(focus,"Refresh"),Is.SameAs(liora.GetComponent(T("CourtyardLiora"))));
            Call(liora.GetComponent(T("CourtyardLiora")),"InteractWithPlayer");
            Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Liora"));
            yield return new WaitForSeconds(.66f);
            Call(Dialogue,"SkipLine");
            yield return null;
            Capture("liora-am-garten");
            Call(Dialogue,"CloseDialogue");

            Meet("Kafla");
            Meet("Panfrie");
            Assert.That(ready.GetValue(null),Is.False);
            Move(rauk.transform.position+new Vector3(.46f,.88f,-.45f));
            yield return new WaitForSeconds(.7f);
            var actor = rauk.GetComponent(T("CourtyardRauk"));
            Assert.That(Call(focus,"Refresh"),Is.SameAs(actor));
            Capture("rauk-auf-dem-ast");
            Call(actor,"InteractWithPlayer");
            yield return new WaitForSeconds(.94f);
            Assert.That(T("CourtyardRauk").GetProperty("IsPresenting").GetValue(null),Is.True);
            Assert.That(Call(focus,"Refresh"),Is.Null);
            Assert.That(bird.sprite.name,Is.EqualTo("Rauk landing"));
            Assert.That(bird.transform.position.y,Is.LessThan(2.65f));
            Capture("rauk-fliegt-herunter");

            float deadline=Time.time+2;
            while (!(bool)Call(Dialogue,"get_IsOpen") && Time.time<deadline) yield return null;
            Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Rauk"));
            Assert.That(bird.sprite.name,Is.EqualTo("Rauk"));
            Assert.That(ready.GetValue(null),Is.True);
            yield return new WaitForSeconds(.35f);
            Call(Dialogue,"SkipLine");
            yield return null;
            Capture("rauk-gespraech");
            Call(Dialogue,"CloseDialogue");
            yield return new WaitForSeconds(.44f);
            Assert.That(bird.sprite.name,Is.EqualTo("Rauk landing"));
            Capture("rauk-fliegt-zurueck");
            var isPresenting = T("CourtyardRauk").GetProperty("IsPresenting");
            deadline = Time.time+1.5f;
            while ((bool)isPresenting.GetValue(null) && Time.time<deadline) yield return null;
            yield return null;
            Assert.That(isPresenting.GetValue(null),Is.False);
            Assert.That(bird.sprite.name,Is.EqualTo("Rauk perched"));
            Assert.That(raukTrigger.enabled,Is.True);

            Call(mentor,"InteractWithPlayer");
            Call(Dialogue,"SkipLine");
            yield return null;
            Assert.That(Text,Does.Contain("draußen auf dem Feld"));
            Call(Dialogue,"CloseDialogue");

            yield return SceneManager.LoadSceneAsync("StartScene");
            yield return new WaitForSeconds(.65f);
            Assert.That(ready.GetValue(null),Is.True);
            Assert.That(GameObject.Find("Courtyard Rauk").GetComponent<SphereCollider>().enabled,Is.True);
        }
        finally { T("DemoOpening").GetMethod("ResetSession",HiddenStatic).Invoke(null,null); }
    }
}
