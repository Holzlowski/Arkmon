using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardIllustrationTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardWorkshopTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator IllustratedSection_KeepsInteractionsClear_AndSurvivesReload()
    {
        yield return Load();
        var workshop=GameObject.Find("Ida workshop").transform;
        var stable=GameObject.Find("Stable").transform;
        var roots=UnityEngine.Object.FindObjectsByType(T("CourtyardVergePolish"),FindObjectsSortMode.None);
        Assert.That(roots.Length,Is.EqualTo(1));
        var root=((Component)roots[0]).transform;
        Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Length,Is.GreaterThan(20));
        Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty,"New ground cover must not trap the player.");
        var wall=workshop.Find("Workshop plaster structure").GetComponent<Renderer>();
        Assert.That(wall.sharedMaterial.shader.name,Is.EqualTo("Arkmon/Courtyard Illustrated Surface"));
        Assert.That(wall.sharedMaterial.shader.isSupported,Is.True);
        Assert.That(Physics.BoxCast(workshop.position+new Vector3(1.6f,1,-6.3f),new Vector3(.32f,.75f,.32f),Vector3.forward,
            out var blocked,Quaternion.identity,3.6f,~0,QueryTriggerInteraction.Ignore),Is.False,"Workshop approach blocked by "+blocked.collider?.name);
        Assert.That(Physics.BoxCast(stable.position+new Vector3(.5f,1,-5),new Vector3(.32f,.75f,.32f),Vector3.forward,
            out blocked,Quaternion.identity,2,~0,QueryTriggerInteraction.Ignore),Is.False,"Stable approach blocked by "+blocked.collider?.name);
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var point=GameObject.Find("Inspect Ida workpiece").transform;
        player.transform.position=point.position+new Vector3(0,1,-.5f);body.position=player.transform.position;
        Physics.SyncTransforms();yield return null;
        var focus=player.GetComponent(T("PlayerInteractionFocus"));
        var selected=focus.GetType().GetMethod("Refresh").Invoke(focus,null);
        Assert.That(selected,Is.SameAs(point.GetComponent(T("CourtyardWorkshopWorkpiece"))));
        var editedRoads=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.name=="Curved courtyard path" && r.sharedMaterials.Any(m=>m.shader.name=="Arkmon/Courtyard Soft Path")).ToArray();
        Assert.That(editedRoads.Length,Is.GreaterThan(0));
        Assert.That(editedRoads.All(r=>r.sharedMaterials.All(m=>m.shader.isSupported)),Is.True);
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardVergePolish"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(GameObject.Find("Illustrated workshop and stable verges").GetComponentsInChildren<SpriteRenderer>().Length,Is.GreaterThan(20));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator ReviewWorkshopAndStable_FromGameplayAndDetailCamera()
    {
        yield return Load();
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var prompt=(Behaviour)player.GetComponent(T("PlayerInteractionFocus"));prompt.enabled=false;
        foreach(string name in new[]{"Ida workshop","Stable"})
        {
            player.transform.position=GameObject.Find(name).transform.position+new Vector3(0,1,-6.3f);body.position=player.transform.position;
            yield return new WaitForSeconds(1.3f);
            Capture("courtyard-polish-gameplay-"+name.Replace(" ","-").ToLowerInvariant());
        }
        var camera=Camera.main;
        var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        bool f=follow.enabled,b=brain!=null && brain.enabled;
        var pos=camera.transform.position;var angle=camera.transform.rotation;float fov=camera.fieldOfView;
        follow.enabled=false;if(brain!=null)brain.enabled=false;
        camera.fieldOfView=40;camera.transform.rotation=Quaternion.Euler(25,0,0);
        camera.transform.position=GameObject.Find("Ida workshop").transform.position+new Vector3(0,1.35f,-2.3f)-camera.transform.forward*12;
        yield return null;yield return null;Capture("workshop-illustration-detail");
        camera.transform.position=GameObject.Find("Stable").transform.position+new Vector3(.1f,1.35f,-2.3f)-camera.transform.forward*12;
        yield return null;yield return null;Capture("stable-illustration-detail");
        camera.transform.SetPositionAndRotation(pos,angle);camera.fieldOfView=fov;
        follow.enabled=f;if(brain!=null)brain.enabled=b;prompt.enabled=true;
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator StableSetting_PreservesHorseArtwork_GreetingsAndClearGround()
    {
        yield return Load();
        var stable=GameObject.Find("Stable").transform;
        foreach(var roof in stable.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Stable slate roof"))
            Assert.That(roof.sharedMaterial.name,Is.EqualTo("Stable roof"));
        var setting=stable.Find("Illustrated stable setting");Assert.That(setting,Is.Not.Null);
        Assert.That(setting.GetComponentsInChildren<Collider>(),Is.Empty);
        var contactShadow=stable.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Paper contact shadow");
        foreach(var ground in setting.GetComponentsInChildren<MeshRenderer>())
        {
            Assert.That(ground.sharedMaterial.shader.isSupported,Is.True);
            Assert.That(ground.sharedMaterial.renderQueue,Is.LessThan(contactShadow.sharedMaterial.renderQueue),
                "Ground dressing must render before contact shadows, so the horses remain grounded.");
        }
        foreach(var horse in stable.GetComponentsInChildren(T("CourtyardHorse")))
        {
            var drawing=horse.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.EndsWith("paper horse"));
            Assert.That(drawing.sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Unlit"));
            Assert.That(drawing.sharedMaterial.GetTexture("_BaseMap"),Is.Not.Null);
        }
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
        foreach(var greeting in stable.GetComponentsInChildren(T("CourtyardHorseGreeting")))
        {
            player.transform.position=greeting.transform.position+new Vector3(0,1,-.6f);body.position=player.transform.position;
            Physics.SyncTransforms();yield return null;
            var focus=player.GetComponent(T("PlayerInteractionFocus"));
            Assert.That(focus.GetType().GetMethod("Refresh").Invoke(focus,null),Is.SameAs(greeting));
            greeting.GetType().GetMethod("InteractWithPlayer").Invoke(greeting,null);yield return null;
            Assert.That((bool)dialogue.GetType().GetProperty("IsOpen").GetValue(dialogue),Is.True);
            Assert.That(dialogue.GetType().GetProperty("ActiveSpeaker").GetValue(dialogue),Is.EqualTo("Elian"));
            dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);yield return null;
        }
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardStableStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardHorseGreeting"),FindObjectsSortMode.None).Length,Is.EqualTo(2));
        LogAssert.NoUnexpectedReceived();
    }
}
