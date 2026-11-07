using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardFinishingTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static object P(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);

    [UnityTest] public IEnumerator ResidentsPauseForWorkAndWatering_MusicReturnsAfterConversation()
    {
        yield return Load();
        var ida=GameObject.Find("Courtyard Carpenter").GetComponent(T("CourtyardCarpenterVisual"));
        var hannes=GameObject.Find("Courtyard Stablekeeper").GetComponent(T("CourtyardStablekeeperVisual"));
        var marta=GameObject.Find("Courtyard Gardener").GetComponent(T("CourtyardGardenerVisual"));
        var anchors=new[]{ida.transform.position,hannes.transform.position,marta.transform.position};
        T("CourtyardCarpenterVisual").GetField("workTime",Hidden).SetValue(ida,18.2f);
        T("CourtyardStablekeeperVisual").GetField("workTime",Hidden).SetValue(hannes,11.2f);
        yield return null;yield return null;
        Assert.That(P(ida,"IsInspecting"),Is.True);Assert.That(P(ida,"IsCutting"),Is.False);
        Assert.That(P(hannes,"IsCheckingHorse"),Is.True);
        CourtyardGardenTaskTests.MoveTo(ida);
        Call(ida.GetComponent(T("CourtyardLife")),"InteractWithPlayer");
        yield return new WaitForSecondsRealtime(.8f);
        var manager=T("SoundManager").GetField("instance").GetValue(null);
        Assert.That(P(manager,"ConversationGain"),Is.EqualTo(.62f).Within(.001f));
        Assert.That(P(ida,"IsWorking"),Is.False);Assert.That(P(hannes,"IsWorking"),Is.False);
        Call(Dialogue,"CloseDialogue");yield return new WaitForSecondsRealtime(1.3f);
        Assert.That(P(manager,"ConversationGain"),Is.EqualTo(1f).Within(.001f));
        Assert.That(P(ida,"IsWorking"),Is.True);
        var can=GameObject.Find("Courtyard watering can").GetComponent(T("CourtyardGarden"));
        CourtyardGardenTaskTests.MoveTo(can);Call(can,"InteractWithPlayer");yield return CourtyardGardenTaskTests.WaitForPour();Call(Dialogue,"CloseDialogue");
        var bed=UnityEngine.Object.FindObjectsByType(T("CourtyardGarden"),FindObjectsSortMode.None).Cast<Component>().First(c=>c.name=="Garden watering point");
        CourtyardGardenTaskTests.MoveTo(bed);Call(bed,"InteractWithPlayer");
        yield return new WaitForSeconds(.4f);
        Assert.That(T("CourtyardGarden").GetProperty("IsWatering").GetValue(null),Is.True);
        Assert.That(P(marta,"IsWatchingWatering"),Is.True);
        Assert.That(P(marta,"IsTending"),Is.False);
        Call(bed,"CancelWatering");yield return null;yield return null;
        Assert.That(P(marta,"IsWatchingWatering"),Is.False);
        Assert.That(T("CourtyardGarden").GetProperty("WateredCount").GetValue(null),Is.EqualTo(0));
        Assert.That(ida.transform.position,Is.EqualTo(anchors[0]));Assert.That(hannes.transform.position,Is.EqualTo(anchors[1]));Assert.That(marta.transform.position,Is.EqualTo(anchors[2]));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator PlaningAfterLongPause_PlaysOnlyOncePerCut()
    {
        yield return Load();
        var ida=GameObject.Find("Courtyard Carpenter").GetComponent(T("CourtyardCarpenterVisual"));
        var clock=ida.GetType().GetField("workTime",Hidden);
        var last=ida.GetType().GetField("lastStroke",Hidden);
        last.SetValue(ida,-1);
        clock.SetValue(ida,22.48f);
        ida.GetType().GetMethod("LateUpdate",Hidden).Invoke(ida,null);
        Assert.That(P(ida,"IsCutting"),Is.True);
        int first=(int)last.GetValue(ida);
        Assert.That(first,Is.GreaterThanOrEqualTo(0));
        clock.SetValue(ida,22.60f);
        ida.GetType().GetMethod("LateUpdate",Hidden).Invoke(ida,null);
        Assert.That(P(ida,"IsCutting"),Is.True);
        Assert.That((int)last.GetValue(ida),Is.EqualTo(first),"The same forward cut cannot retrigger audio after the long inspection pause.");
        LogAssert.NoUnexpectedReceived();
    }

}
