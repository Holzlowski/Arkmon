using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AurenfeldCompositionTests
{
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name).Invoke(o,args);
    static void Capture(string name)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name});
    [UnityTest] public IEnumerator WesternComposition_StaysConnectedThroughTheFlight()
    {
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
        var profile=T("AurenfeldLandscape").GetMethod("Surface");
        foreach(var p in new[]{new Vector3(-43,1,7),new Vector3(53,0,-1),new Vector3(0,0,0)})
            Assert.That(profile.Invoke(null,new object[]{p}),Is.EqualTo(p),"Courtyard/gate elevations stay unchanged.");
        for(float z=-90;z<=100;z+=20)for(float x=-390;x<-68;x+=2)
        {
            var a=(Vector3)profile.Invoke(null,new object[]{new Vector3(x,5,z)});
            var b=(Vector3)profile.Invoke(null,new object[]{new Vector3(x+2,5,z)});
            Assert.That(Mathf.Abs(a.y-b.y),Is.LessThan(1.5f),"No abrupt terrain steps.");
        }
        var meadow=GameObject.Find("Castle horizon and golden fields").GetComponentInChildren(T("MeadowVegetation"));
        var farm=GameObject.Find("Aurenfeld distant farm");
        var ground=(Vector3)Call(meadow,"SurfacePoint",farm.transform.position);
        Assert.That(farm.transform.position.y,Is.EqualTo(ground.y).Within(.05f));
        var point=GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout"));
        CourtyardGardenTaskTests.MoveTo(point);yield return null;
        Call(point,"InteractWithPlayer");
        float deadline=Time.realtimeSinceStartup+20;
        for(int stage=1;stage<=3;stage++)
        {
            while((float)point.GetType().GetProperty("Progress").GetValue(point)<stage/3f-.001f && Time.realtimeSinceStartup<deadline)yield return null;
            Capture("aurenfeld-composition-flight-"+stage);
        }
        Assert.That((float)point.GetType().GetProperty("Progress").GetValue(point),Is.GreaterThan(.99f));
        Call(point,"ReturnToCourtyard");
        deadline=Time.realtimeSinceStartup+10;
        while((bool)T("CourtyardLookout").GetProperty("IsPresenting").GetValue(null) && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That((bool)T("CourtyardLookout").GetProperty("IsPresenting").GetValue(null),Is.False);
        foreach(var component in UnityEngine.Object.FindObjectsByType(T("CourtyardCamera"),FindObjectsSortMode.None).Cast<Behaviour>())component.enabled=false;
        foreach(var component in Camera.main.GetComponents<MonoBehaviour>())component.enabled=false;
        Camera.main.transform.position=new Vector3(-61,11,7);
        Camera.main.transform.rotation=Quaternion.LookRotation(new Vector3(-1,-.06f,.08f));Camera.main.fieldOfView=52;
        yield return null;Capture("aurenfeld-composition-after");
        LogAssert.NoUnexpectedReceived();
    }
}
