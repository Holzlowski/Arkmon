using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardPolishTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest]public IEnumerator MainProps_HaveDepthAndKeepGameplaySystems()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("StartScene");for(int i=0;i<5;i++)yield return null;
        foreach(string name in new[]{"Stable","Wheelbarrow","Workbench","Fountain basin","Bench seat","Tower entrance"})
        {
            var prop=GameObject.Find(name);Assert.That(prop,Is.Not.Null,name);
            Assert.That(prop.GetComponentsInChildren<MeshRenderer>().Length,Is.GreaterThan(5),name);
            Assert.That(prop.GetComponentsInChildren<SpriteRenderer>().Length,Is.EqualTo(0),name);
            Assert.That(prop.GetComponent<BoxCollider>(),Is.Not.Null,name);
        }
        Assert.That(UnityEngine.Object.FindFirstObjectByType(T("NPC")),Is.Not.Null);
        Assert.That(GameObject.Find("Castle horizon and golden fields"),Is.Not.Null);
        Assert.That(GameObject.Find("Garden bench seat"),Is.Not.Null);
        var towerStone=GameObject.Find("Octagonal tower").GetComponent<MeshRenderer>().sharedMaterial;
        var hallStone=GameObject.Find("Castle hall structure").GetComponent<MeshRenderer>().sharedMaterial;
        Assert.That(towerStone,Is.SameAs(hallStone),"Tower and castle share stone scale and lighting.");
        Assert.That(hallStone.shader.name,Is.EqualTo("Arkmon/Tower Painted Light"));
        string[] homes={"Stable","West cottage","Garden cottage","East cottage","Cellar storehouse","Ida workshop"};
        string[] castles={"Castle west wing","Aurenfeld upper residence","Tower connecting wing","Castle hall","Castle upper keep","Castle keep west tower","Castle keep east tower"};
        Bounds BoundsOf(string name)
        {
            var renderers=GameObject.Find(name).GetComponentsInChildren<MeshRenderer>();
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        for(int i=0;i<homes.Length;i++)
        foreach(string other in homes.Skip(i+1).Concat(castles))
            Assert.That(BoundsOf(homes[i]).Intersects(BoundsOf(other)),Is.False,homes[i]+" overlaps "+other);
        Assert.That(Physics.Raycast(new Vector3(0,5,-11),Vector3.down,out var hill,10),Is.True);
        Assert.That(hill.point.y,Is.GreaterThan(1.4f),"The meadow relief has real walkable height.");
        Assert.That(hill.collider.name,Is.EqualTo("Rolling courtyard meadow"));
        var footsteps=GameObject.FindWithTag("Player").GetComponent(T("ExplorationFootsteps"));
        var surface=T("ExplorationFootsteps").GetMethod("SurfaceAt",BindingFlags.NonPublic|BindingFlags.Instance)
            .Invoke(footsteps,new object[]{hill});
        Assert.That(surface.ToString(),Is.EqualTo("Grass"));
        Assert.That(Physics.BoxCast(new Vector3(2,1,1),new Vector3(.3f,.75f,.3f),Vector3.forward,
            out var blocked,Quaternion.identity,5,~0,QueryTriggerInteraction.Ignore),Is.False,
            "The cottage approach must stay clear of benches: "+blocked.collider?.name);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]public IEnumerator TowerStairs_CanBeWalkedUpAndDown()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.5f);
        var player=GameObject.FindWithTag("Player");
        ((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;
        var body=player.GetComponent<Rigidbody>();
        body.position=new Vector3(-34,1.1f,4.2f);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        for(int i=0;i<150 && body.position.z<8.1f;i++)
        {body.linearVelocity=new Vector3(0,body.linearVelocity.y,3);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.z,Is.GreaterThan(7.95f),"Elian must reach the tower terrace without catching on stair risers.");
        Assert.That(body.position.y,Is.GreaterThan(2),"The approach must reach the raised terrace.");
        for(int i=0;i<150 && body.position.z>4.2f;i++)
        {body.linearVelocity=new Vector3(0,body.linearVelocity.y,-3);yield return new WaitForFixedUpdate();}
        body.linearVelocity=Vector3.zero;
        Assert.That(body.position.z,Is.LessThan(4.4f),"Elian must return to the courtyard.");
        Assert.That(body.position.y,Is.GreaterThan(-.1f));LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]public IEnumerator MartaCottage_ResidentAndApproachRemainUsable()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.7f);
        var house=GameObject.Find("Garden cottage").transform;
        var anchor=house.Find("Marta work spot");
        var marta=GameObject.Find("Courtyard Gardener");
        Assert.That(Vector3.Distance(marta.transform.position,anchor.position),Is.LessThan(.05f),
            "Marta must spawn at the house's work spot, including after layout changes.");
        Assert.That(marta.GetComponentInChildren<SpriteRenderer>()?.sprite,Is.Not.Null);
        Assert.That(marta.GetComponentInChildren<SpriteRenderer>().sprite.name,Is.EqualTo("Marta-Gardener"));
        Assert.That(GameObject.Find("Garden potting table").transform.IsChildOf(house),Is.True);
        Assert.That(Physics.BoxCast(house.position+new Vector3(0,1,-6.6f),new Vector3(.32f,.75f,.32f),Vector3.forward,
            out var blocked,Quaternion.identity,3.8f,~0,QueryTriggerInteraction.Ignore),Is.False,
            "The door approach must remain free: "+blocked.collider?.name);

        var player=GameObject.FindWithTag("Player");
        ((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;
        var body=player.GetComponent<Rigidbody>();
        body.position=house.position+new Vector3(-1.7f,1,-7.9f);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        yield return new WaitForSeconds(.8f);
        typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static)
            .Invoke(null,new object[]{"intro-courtyard-marta"});
        // An additional close architectural view; gameplay camera settings are restored immediately.
        var camera=Camera.main;var savedPosition=camera.transform.position;var savedAngle=camera.transform.rotation;
        camera.transform.position=house.position+new Vector3(-.2f,8.3f,-14.9f);
        camera.transform.LookAt(house.position+new Vector3(-.2f,2.5f,-1.6f));
        typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static)
            .Invoke(null,new object[]{"intro-courtyard-marta-detail"});
        camera.transform.SetPositionAndRotation(savedPosition,savedAngle);
        var origin=marta.transform.position;
        body.interpolation=RigidbodyInterpolation.None;body.isKinematic=true;
        player.transform.position=origin+new Vector3(-1.5f,1,-1);body.position=player.transform.position;Physics.SyncTransforms();yield return new WaitForSeconds(.15f);
        Assert.That(marta.GetComponentInChildren<SpriteRenderer>().flipX,Is.True,"Marta left-facing: player="+player.transform.position+", Marta="+origin);
        player.transform.position=origin+new Vector3(1.5f,1,-1);body.position=player.transform.position;Physics.SyncTransforms();yield return new WaitForSeconds(.15f);
        Assert.That(marta.GetComponentInChildren<SpriteRenderer>().flipX,Is.False,"Marta right-facing: player="+player.transform.position+", Marta="+origin);
        Assert.That(Vector3.Distance(marta.transform.position,origin),Is.LessThan(.001f));
        var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
        dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        T("CourtyardLife").GetMethod("InteractWithPlayer").Invoke(marta.GetComponent(T("CourtyardLife")),null);
        Assert.That((bool)dialogue.GetType().GetProperty("IsOpen").GetValue(dialogue),Is.True,
            "The illustrated resident must still be interactable.");
        dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        LogAssert.NoUnexpectedReceived();
    }
}
