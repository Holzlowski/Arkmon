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

    [UnityTest] public IEnumerator SunraysAndWayfinding_AreBoundedAndSurviveReload()
    {
        yield return Load();
        var root=GameObject.Find("Breach golden sunrays");Assert.That(root,Is.Not.Null);
        var rays=root.GetComponentsInChildren<MeshRenderer>();Assert.That(rays.Length,Is.EqualTo(9));
        Assert.That(rays.All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
        Assert.That(root.GetComponentsInChildren<Light>(),Is.Empty);
        var motes=root.GetComponentInChildren<ParticleSystem>();Assert.That(motes.main.maxParticles,Is.EqualTo(64));
        var markers=GameObject.Find("Courtyard route markers");Assert.That(markers,Is.Not.Null);
        Assert.That(markers.GetComponentsInChildren<Collider>(),Is.Empty);
        Assert.That(markers.transform.childCount,Is.EqualTo(2));
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        CourtyardGardenTaskTests.MoveTo(GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout")));
        var follow=UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var position=new Vector3(-31,7,-3);
        Call(follow,"SetScenicView",position,Quaternion.LookRotation(new Vector3(-42,2.6f,7)-position),48f);
        yield return new WaitForSeconds(.5f);
        Assert.That(rays.All(r=>r.enabled),Is.True);
        Capture("courtyard-breach-godrays-on");
        var style=(Behaviour)root.GetComponent(T("CourtyardBreachSunrays"));style.enabled=false;yield return null;
        Assert.That(rays.All(r=>!r.enabled),Is.True);Assert.That(motes.particleCount,Is.Zero);
        Capture("courtyard-breach-godrays-off");style.enabled=true;yield return null;
        Call(follow,"SetScenicView",new Vector3(-61,11,7),Quaternion.LookRotation(new Vector3(-69,0,20)),58f);
        yield return null;yield return null;
        Assert.That(rays.All(r=>!r.enabled),Is.True,"No veil over the finished panorama.");
        Call(follow,"ClearScenicView");
        foreach(var point in new[]{new Vector3(-29,1,1),new Vector3(32,1,-3)})
        {
            body.position=player.transform.position=point;Physics.SyncTransforms();yield return new WaitForSeconds(1);
            Capture(point.x<0?"courtyard-tower-directions":"courtyard-garden-directions");
        }
        yield return Load();Assert.That(root==null,Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardBreachSunrays"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardWayfinding"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
