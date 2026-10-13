using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class DemoOpeningDetailsTests
{
    const BindingFlags StaticFlags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Progress(string name)=>T("DemoOpening").GetProperty(name).GetValue(null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static void Reset()=>T("DemoOpening").GetMethod("ResetSession",StaticFlags).Invoke(null,null);
    static void Close()=>Dialogue.GetType().GetMethod("CloseDialogue").Invoke(Dialogue,null);
    [UnityTest]
    public IEnumerator Waking_ShowsSeatedBoyBeforeStandingAndRestoresControl()
    {
        Reset();yield return SceneManager.LoadSceneAsync("TowerRoom");yield return null;
        var bed=GameObject.Find("Canopy bed");var visual=bed.GetComponent(T("PaintedCanopyBed"));
        float deadline=Time.realtimeSinceStartup+6;
        while(!(bool)visual.GetType().GetProperty("IsSeated").GetValue(visual)&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(visual.GetType().GetProperty("IsSeated").GetValue(visual),Is.True);
        Assert.That(Progress("IsPresenting"),Is.True);
        Assert.That(bed.transform.Find("Painted bed geometry").gameObject.activeSelf,Is.True);
        Assert.That(GameObject.FindWithTag("Player").GetComponentsInChildren<SpriteRenderer>().All(s=>!s.enabled),Is.True);
        Assert.That(bed.transform.Find("Boy seated at bed edge").GetComponent<SpriteRenderer>().sprite.name,Is.EqualTo("Prince-Sitting"));
        while(!(bool)Progress("HasAwakened")&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Progress("HasAwakened"),Is.True);
        Assert.That(visual.GetType().GetProperty("IsSeated").GetValue(visual),Is.False);
        Assert.That(GameObject.FindWithTag("Player").GetComponentsInChildren<SpriteRenderer>().Any(s=>s.enabled),Is.True);
        Close();Reset();
    }
    [UnityTest]
    public IEnumerator InteractionFocus_SelectsWardrobeAndHidesDuringDialogueAndDressing()
    {
        Reset();T("DemoOpening").GetField("<HasAwakened>k__BackingField",StaticFlags).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("TowerRoom");yield return new WaitForSeconds(1.4f);Close();
        var player=GameObject.FindWithTag("Player");player.GetComponent(T("PlayerController")).GetType().GetProperty("enabled").SetValue(player.GetComponent(T("PlayerController")),false);
        var focus=player.GetComponent(T("PlayerInteractionFocus"));var refresh=focus.GetType().GetMethod("Refresh");
        player.transform.position=new Vector3(2.7f,1,1);Physics.SyncTransforms();
        object target=refresh.Invoke(focus,null);Assert.That(target,Is.Not.Null);
        Assert.That(target.GetType().GetField("kind").GetValue(target).ToString(),Is.EqualTo("Wardrobe"));
        yield return null;
        var hint = player.GetComponentsInChildren<Canvas>(true).First(c => c.name == "Nearby interaction");
        Assert.That(hint.enabled, Is.True);
        var rect = (RectTransform)hint.transform.Find("Interaction paper");
        Assert.That(rect.position.x, Is.InRange(0f, (float)Screen.width));
        Assert.That(rect.position.y, Is.InRange(0f, (float)Screen.height));
        Dialogue.GetType().GetMethod("ShowMessage").Invoke(Dialogue,new object[]{"Mein Mantel wartet.","Der Junge"});
        Assert.That(refresh.Invoke(focus,null),Is.Null);Close();
        player.transform.position=new Vector3(100,1,100);Physics.SyncTransforms();Assert.That(refresh.Invoke(focus,null),Is.Null);
        player.transform.position=new Vector3(2.7f,1,1);Physics.SyncTransforms();
        T("DemoOpening").GetMethod("TakeMantle").Invoke(null,null);
        Assert.That(refresh.Invoke(focus,null),Is.Null);
        float deadline=Time.realtimeSinceStartup+4;
        while((bool)Progress("IsPresenting")&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Progress("HasMantle"),Is.True);
        Close();Reset();
    }
}
