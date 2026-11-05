using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardCastleEntranceTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator Portal_KeepsInspectionReachable_AndMartaKeepsSourceProportions()
    {
        yield return Load();
        var marta=GameObject.Find("Marta illustrated gardener").transform;
        var sprite=marta.GetComponent<SpriteRenderer>().sprite;
        Assert.That(sprite.name,Is.EqualTo("Marta-Gardener"));
        Assert.That(sprite.texture.width,Is.EqualTo(1024));Assert.That(sprite.texture.height,Is.EqualTo(1536));
        Assert.That(sprite.bounds.size.x/sprite.bounds.size.y,Is.EqualTo(2f/3).Within(.001f),"Keep the source drawing's aspect ratio.");
        Assert.That(sprite.bounds.size.y,Is.EqualTo(2.26f).Within(.001f),"Import correction must preserve Marta's original world height.");
        Assert.That(marta.localScale.y,Is.InRange(.98f,1.02f));
        Assert.That(marta.localPosition.y,Is.EqualTo(0).Within(.01f));
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var entrance=UnityEngine.Object.FindObjectsByType(T("CourtyardEntrance"),FindObjectsSortMode.None).Cast<Component>()
            .Single(e=>e.GetType().GetField("destination").GetValue(e).ToString()=="ThroneHall");
        var portal=GameObject.Find("Illustrated castle entrance");Assert.That(portal,Is.Not.Null);
        foreach(string name in new[]{"Castle hall","Castle upper keep","Castle keep west tower","Castle keep east tower"})
        {
            var building=GameObject.Find(name).transform;
            var wall=building.Find(name+" structure");
            Assert.That(wall.GetComponent<Collider>().enabled,Is.True,"Castle solids must remain intact.");
            var renderer=wall.GetComponent<MeshRenderer>();Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("Arkmon/Courtyard Illustrated Surface"));
            Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True);
            foreach(string batch in new[]{"Painted castle window surrounds","Recessed castle arch windows","Castle window glazing bars"})
            {
                var geometry=building.Find(batch);Assert.That(geometry,Is.Not.Null);
                Assert.That(geometry.GetComponentsInChildren<Collider>(),Is.Empty,"Window decoration cannot change walking clearance.");
                Assert.That(geometry.GetComponent<MeshRenderer>().sharedMaterial.shader.isSupported,Is.True);
            }
        }
        Assert.That(GameObject.Find("Garden cottage").transform.Find("Garden cottage structure").GetComponent<MeshRenderer>().sharedMaterial.name,Is.EqualTo("Plaster"),"Facade scope must leave Marta's cottage unchanged.");
        var crest=GameObject.Find("Aurenfeld portal crest");Assert.That(crest,Is.Not.Null);
        Assert.That(crest.transform.localPosition,Is.EqualTo(new Vector3(0,4.06f,-.52f)),"The crest must stay on the door instead of inheriting banner movement.");
        Assert.That(crest.GetComponent(T("CourtyardSceneryMotion")),Is.Null);
        Assert.That(portal.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        var approach=entrance.transform.position+new Vector3(0,1,-.75f);
        Assert.That(Physics.BoxCast(approach+Vector3.back,new Vector3(.3f,.7f,.3f),Vector3.forward,out var hit,Quaternion.identity,1,~0,QueryTriggerInteraction.Ignore),Is.False,"Portal approach blocked by "+hit.collider?.name);
        Assert.That(Physics.Raycast(approach,Vector3.forward,out hit,2,~0,QueryTriggerInteraction.Ignore),Is.True,"The closed portal must still block walking.");
        Assert.That(hit.collider.name,Is.EqualTo("Closed portal collision"));
        player.transform.position=approach;body.position=approach;Physics.SyncTransforms();yield return null;
        var focus=player.GetComponent(T("PlayerInteractionFocus"));
        Assert.That(focus.GetType().GetMethod("Refresh").Invoke(focus,null),Is.SameAs(entrance));
        entrance.GetType().GetMethod("InteractWithPlayer").Invoke(entrance,null);yield return null;
        var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
        Assert.That(dialogue.GetType().GetProperty("IsOpen").GetValue(dialogue),Is.True);
        Assert.That(dialogue.GetType().GetProperty("ActiveSpeaker").GetValue(dialogue),Is.EqualTo("Elian"));
        dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        var prompt=(Behaviour)focus;prompt.enabled=false;
        player.transform.position=entrance.transform.position+new Vector3(-.6f,1,-4.5f);body.position=player.transform.position;
        yield return new WaitForSeconds(1.3f);Capture("castle-entrance-gameplay");
        var camera=Camera.main;var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));
        var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));
        bool f=follow.enabled,b=brain!=null && brain.enabled;var pos=camera.transform.position;var angle=camera.transform.rotation;float fov=camera.fieldOfView;
        try
        {
            follow.enabled=false;if(brain!=null)brain.enabled=false;
            camera.fieldOfView=40;camera.transform.rotation=Quaternion.Euler(18,0,0);
            camera.transform.position=portal.transform.position+new Vector3(0,2.7f,-.5f)-camera.transform.forward*12;
            yield return null;yield return null;Capture("castle-entrance-detail");
            camera.transform.position=marta.parent.position+Vector3.up*1.2f-camera.transform.forward*5;
            yield return null;yield return null;Capture("marta-proportions-detail");
        }
        finally
        {
            camera.transform.SetPositionAndRotation(pos,angle);camera.fieldOfView=fov;follow.enabled=f;
            if(brain!=null)brain.enabled=b;prompt.enabled=true;
        }
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardCastleEntranceStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(GameObject.Find("Marta illustrated gardener").transform.localScale.y,Is.InRange(.98f,1.02f));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardCastleFacadeStyle"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r=>r.name=="Painted castle window surrounds"),Is.EqualTo(4));
        LogAssert.NoUnexpectedReceived();
    }
}
