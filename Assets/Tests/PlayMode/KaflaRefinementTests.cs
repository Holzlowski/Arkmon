using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class KaflaRefinementTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name).Invoke(obj,args);
    static object I(string name)=>T(name).GetProperty("Instance").GetValue(null);
    static void Capture(string name)
    {
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var enabled=canvases.Select(c=>c.enabled).ToArray();
        foreach(var canvas in canvases)canvas.enabled=false;
        try{typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name});}
        finally{for(int i=0;i<canvases.Length;i++)canvases[i].enabled=enabled[i];}
    }
    [UnityTest]public IEnumerator RefinedBodySurvivesSwordScrapeAndDefeatPoses()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return new CommandNavigationTests().FirstTurn_CommandsAreAvailableAndBackCancelsTargetingInBothCategories();
        var unit=(Component)Call(I("UnitActionSystem"),"GetSelectedUnit");var animation=unit.GetComponent(T("PaperSwordAnimation"));
        var body=(SpriteRenderer)animation.GetType().GetField("bodyRenderer",F).GetValue(animation);
        Assert.That(body.sprite.name,Is.EqualTo("Kafla-Body"));Assert.That(body.sprite.bounds.size.y,Is.EqualTo(1.67f).Within(.01f));
        var shoulder=(Transform)animation.GetType().GetField("shoulder",F).GetValue(animation);
        var joint=shoulder.localPosition;
        Assert.That(joint.x/body.sprite.bounds.size.x,Is.EqualTo(.025f).Within(.001f));
        Assert.That(Resources.Load<ScriptableObject>("Paper UI").GetType().GetField("flacaPortrait").GetValue(Resources.Load<ScriptableObject>("Paper UI")) is Sprite portrait && portrait.name=="Kafla-Refined-Portrait",Is.True);
        var camera=Camera.main;var brain=camera.GetComponent(T("Cinemachine.CinemachineBrain")) as Behaviour;if(brain!=null)brain.enabled=false;
        camera.transform.position=unit.transform.position+new Vector3(0,1.8f,-3.6f);camera.transform.LookAt(unit.transform.position+Vector3.up*.8f);camera.fieldOfView=22;
        var grip=(SpriteRenderer)animation.GetType().GetField("gripRenderer",F).GetValue(animation);
        var forearm=(SpriteRenderer)animation.GetType().GetField("forearmRenderer",F).GetValue(animation);
        var wrist=(Transform)animation.GetType().GetField("sword",F).GetValue(animation);
        Assert.That(grip.transform.parent,Is.SameAs(wrist),"The gripping paw follows the handle rotation.");
        Assert.That(forearm.sortingOrder,Is.GreaterThan(body.sortingOrder),"The visible forearm lies ahead of the torso/cape.");
        yield return null;Capture("kafla-refined-idle");
        var target=unit.transform.position+Vector3.right*2;Call(animation,"BeginSwing",target,true);
        yield return new WaitForSeconds(.3f);Call(animation,"MarkImpact");yield return null;Capture("kafla-refined-flame-sword");
        yield return new WaitForSeconds(1);Assert.That(Call(animation,"GetTipPosition") is Vector3,Is.True);
        var idle=body.sprite;Call(animation,"BeginSparkScrape",target);
        yield return new WaitForSeconds(.45f);Assert.That(body.sprite.name,Is.EqualTo("Kafla-Refined-SparkBody"));
        Assert.That(Vector3.Distance(joint,shoulder.localPosition),Is.LessThan(.001f),"Scraping rotates the arm around an attached shoulder.");Capture("kafla-refined-scrape");
        Call(animation,"MarkSparkRelease");yield return new WaitForSeconds(1);
        Assert.That(body.sprite,Is.SameAs(idle),"The refined resting body returns after Glutfunke.");
        var visual=unit.GetComponent(T("PaperCharacterVisual"));Call(visual,"BeginDefeat");yield return new WaitForSeconds(.5f);
        Assert.That(animation.GetType().GetProperty("IsSwinging").GetValue(animation),Is.False);Assert.That(body.sprite,Is.SameAs(idle));
        Capture("kafla-refined-defeated");LogAssert.NoUnexpectedReceived();
    }
}
