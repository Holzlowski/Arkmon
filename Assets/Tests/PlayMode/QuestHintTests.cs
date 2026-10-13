using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class QuestHintTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object P(object obj,string name)=>obj.GetType().GetProperty(name).GetValue(obj);
    static void Capture(string name)
    {
        var helper=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("PaperUIReviewTests")).FirstOrDefault(t=>t!=null);
        if(helper==null)return;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
        try{helper.GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name,1920,1080});}
        finally{for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}}
    }
    [UnityTest]public IEnumerator Wardrobe_DimWhenDistantBrightWhenInteractableAndOffAfterMantle()
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Static;
        T("DemoOpening").GetMethod("ResetSession",flags).Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",flags).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("TowerRoom");yield return new WaitForSeconds(1.4f);
        var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        var wardrobe=GameObject.Find("Wardrobe");var hint=wardrobe.GetComponent(T("QuestHint"));Assert.That(hint,Is.Not.Null);
        var outlines=wardrobe.GetComponentsInChildren<LineRenderer>();
        Assert.That(outlines.Length,Is.EqualTo(4),"Each door retains its subdued core and glow.");
        var sparkles=wardrobe.GetComponentsInChildren<ParticleSystem>().Where(p=>p.name=="Door quest sparkles").ToArray();
        Assert.That(sparkles.Length,Is.EqualTo(2));
        Assert.That(sparkles.All(p=>p.particleCount==72 && p.main.simulationSpace==ParticleSystemSimulationSpace.Local),Is.True);
        Assert.That(wardrobe.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name=="Quest shimmer surface"),Is.False,"The door faces must not receive a full-surface shimmer.");
        var player=GameObject.FindWithTag("Player");var focus=player.GetComponent(T("PlayerInteractionFocus"));
        foreach(Behaviour camera in UnityEngine.Object.FindObjectsByType(T("Cinemachine.CinemachineVirtualCamera"),FindObjectsSortMode.None))camera.enabled=false;
        Camera.main.transform.position=wardrobe.transform.position+new Vector3(-4,3,-5);Camera.main.transform.LookAt(wardrobe.transform.position+Vector3.up*1.6f);Camera.main.fieldOfView=40;
        player.GetComponent<Rigidbody>().position=new Vector3(0,1,-3);Physics.SyncTransforms();yield return new WaitForSeconds(.5f);
        Assert.That(P(hint,"IsVisible"),Is.True);Assert.That(P(hint,"IsBright"),Is.False);float dim=(float)P(hint,"Strength");Capture("quest-wardrobe-dim");
        player.GetComponent<Rigidbody>().position=wardrobe.transform.position+new Vector3(0,1,-1.6f);Physics.SyncTransforms();yield return new WaitForSeconds(.5f);
        Assert.That(P(focus,"Current"),Is.Not.Null);
        Assert.That(P(focus,"Current").GetType().GetField("kind").GetValue(P(focus,"Current")).ToString(),Is.EqualTo("Wardrobe"));
        Assert.That(P(hint,"IsBright"),Is.True);Assert.That((float)P(hint,"Strength"),Is.GreaterThan(dim));Capture("quest-wardrobe-bright");
        var particleDoor=sparkles[0].transform;Vector3 corner=particleDoor.TransformPoint(new Vector3(.4f,1,0));
        var animation=wardrobe.GetComponent(T("PaintedWardrobe"));animation.GetType().GetMethod("OpenForPreview").Invoke(animation,new object[]{true});yield return new WaitForSeconds(.55f);
        Assert.That(Vector3.Distance(corner,particleDoor.TransformPoint(new Vector3(.4f,1,0))),Is.GreaterThan(.25f),"The outline follows the door hinge.");
        animation.GetType().GetMethod("OpenForPreview").Invoke(animation,new object[]{false});yield return new WaitForSeconds(.55f);
        dialogue.GetType().GetMethod("ShowMessage").Invoke(dialogue,new object[]{"Test","Der Junge"});yield return null;yield return null;
        Assert.That(P(hint,"IsBright"),Is.False);dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        T("DemoOpening").GetMethod("FinishTakingMantle").Invoke(null,null);yield return null;yield return null;
        Assert.That(P(hint,"IsVisible"),Is.False);Assert.That(P(hint,"Strength"),Is.EqualTo(0));
        Assert.That(sparkles.All(p=>p.particleCount==0),Is.True,"Taking the mantle removes all quest particles immediately.");
        Assert.That(outlines.All(l=>!l.enabled),Is.True);
        Assert.That(wardrobe.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Quest shimmer surface").All(r=>!r.enabled),Is.True);Capture("quest-wardrobe-complete");
        T("DemoOpening").GetMethod("ResetSession",flags).Invoke(null,null);LogAssert.NoUnexpectedReceived();
    }
}
