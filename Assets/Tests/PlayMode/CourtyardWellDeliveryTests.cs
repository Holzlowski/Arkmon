using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardWellDeliveryTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static bool State(string n)=>(bool)T("CourtyardHandleDelivery").GetProperty(n).GetValue(null);
    static void Close()=>T("DialogueBoxController").GetMethod("CloseDialogue").Invoke(Dialogue,null);
    static void Open(string who)=>T(who+"Conversation").GetMethod("Open").Invoke(null,new[]{Dialogue});
    static IEnumerator Load(bool reset)
    {
        if(reset)
        {
            T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
            T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
            T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        }
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.8f);Close();
        T("DialogueBoxController").GetField("charactersPerSecond",Hidden).SetValue(Dialogue,100000f);
    }
    static IEnumerator Finish()
    {
        for(int i=0;i<20 && (bool)T("DialogueBoxController").GetProperty("IsOpen").GetValue(Dialogue);i++)
        {
            T("DialogueBoxController").GetMethod("SkipLine").Invoke(Dialogue,null);
            yield return null;yield return null;
        }
        Assert.That((bool)T("DialogueBoxController").GetProperty("IsOpen").GetValue(Dialogue),Is.False);
    }
    [UnityTest] public IEnumerator Delivery_CancellationReloadAndGardenRemainIndependent()
    {
        yield return Load(true);
        Open("Ida");Close();Assert.That(State("HasHandle"),Is.False);
        Open("Marta");yield return Finish();Assert.That(State("Completed"),Is.False);
        Open("Ida");yield return Finish();Assert.That(State("Offered"),Is.True);Assert.That(State("HasHandle"),Is.False);
        var point=GameObject.Find("Inspect Ida workpiece").GetComponent(T("CourtyardWorkshopWorkpiece"));
        var player=GameObject.FindWithTag("Player");((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;player.GetComponent<Rigidbody>().isKinematic=true;
        player.transform.position=point.transform.position+Vector3.up;player.GetComponent<Rigidbody>().position=player.transform.position;Physics.SyncTransforms();
        Assert.That(player.GetComponent(T("PlayerInteractionFocus")).GetType().GetMethod("Refresh").Invoke(player.GetComponent(T("PlayerInteractionFocus")),null),Is.SameAs(point));
        point.GetType().GetMethod("InteractWithPlayer").Invoke(point,null);Close();Assert.That(State("HasHandle"),Is.True);
        yield return Load(false);Assert.That(State("HasHandle"),Is.True);
        Open("Marta");Close();Assert.That(State("HasHandle"),Is.True);
        Open("Marta");yield return Finish();Assert.That(State("Completed"),Is.True);
        Assert.That(T("CourtyardTaskFeedback").GetProperty("CompletionCount").GetValue(null),Is.EqualTo(1));Assert.That(State("HasHandle"),Is.False);
        Assert.That((bool)T("CourtyardGarden").GetProperty("Completed").GetValue(null),Is.False);
        Open("Ida");yield return Finish();Assert.That(State("HasHandle"),Is.False);
        yield return Load(false);Assert.That(State("Completed"),Is.True);
        T("IntroChapter").GetMethod("ResetProgress").Invoke(null,null);Assert.That(State("Completed"),Is.False);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Well_IsSolidAccessibleAndSceneOwned()
    {
        yield return Load(true);
        var well=GameObject.Find("Courtyard draw well");Assert.That(well,Is.Not.Null);
        Assert.That(well.transform.position.x,Is.InRange(-6f,19f));
        var parts=well.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
        Assert.That(parts.Length,Is.EqualTo(62));
        var point=(Vector3)T("CourtyardWell").GetMethod("GetTransform").Invoke(well.GetComponent(T("CourtyardWell")),null);
        var obstacles=Physics.OverlapBox(point+Vector3.up*.8f,new Vector3(.3f,.7f,.3f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
        Assert.That(obstacles.Where(c=>!c.CompareTag("Ground")&&!c.CompareTag("Player")).Select(c=>c.name),Is.Empty);
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();body.position=point+Vector3.up;player.transform.position=body.position;
        Physics.SyncTransforms();yield return new WaitForSeconds(.8f);
        T("CourtyardWell").GetMethod("InteractWithPlayer").Invoke(well.GetComponent(T("CourtyardWell")),null);
        Assert.That((bool)T("DialogueBoxController").GetProperty("IsOpen").GetValue(Dialogue),Is.True);
        Close();yield return null;
        typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"courtyard-well"});
        Close();yield return Load(false);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardWell"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
