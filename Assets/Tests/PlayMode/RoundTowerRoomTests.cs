using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class RoundTowerRoomTests
{
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    [UnityTest]
    public IEnumerator NormalMovement_StopsAtVisibleCutawayWindowAndDoorWalls()
    {
        const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        T("DemoOpening").GetMethod("ResetSession",flags).Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",flags).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("TowerRoom");yield return new WaitForSeconds(1.4f);
        object dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        var player=GameObject.FindWithTag("Player");var controller=(Behaviour)player.GetComponent(T("PlayerController"));controller.enabled=false;
        var movement=controller.GetType().GetMethod("ApplyMovement",BindingFlags.NonPublic|BindingFlags.Instance);
        var body=player.GetComponent<Rigidbody>();
        var starts=new[]{new Vector3(-6.5f,1,-1.7f),new Vector3(0,1,-8.2f),new Vector3(0,1,4.6f),new Vector3(5.8f,1,.4f)};
        var inputs=new[]{Vector2.left,Vector2.down,Vector2.up,new Vector2(.94f,.34f)};
        for(int wall=0;wall<starts.Length;wall++)
        {
            body.linearVelocity=Vector3.zero;body.position=starts[wall];Physics.SyncTransforms();
            for(int i=0;i<40;i++)
            {
                movement.Invoke(controller,new object[]{inputs[wall]});yield return new WaitForFixedUpdate();
                Assert.That(Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(0,-1.7f)),Is.LessThan(7.82f),"Movement escaped wall "+wall);
            }
            Assert.That(body.position.y,Is.GreaterThan(.4f));
        }
        T("DemoOpening").GetMethod("ResetSession",flags).Invoke(null,null);
    }
    [UnityTest]
    public IEnumerator CircularFloorAndCutawayBoundaryKeepPlayerInside()
    {
        const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        T("DemoOpening").GetMethod("ResetSession",flags).Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",flags).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("TowerRoom");yield return new WaitForSeconds(1.4f);
        object dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        Assert.That(GameObject.Find("Round tower shell"),Is.Not.Null);
        Assert.That(GameObject.Find("West wall"),Is.Null);
        Assert.That(Physics.Raycast(new Vector3(0,1,-1.7f),Vector3.down,out var floorHit,2),Is.True);
        Assert.That(floorHit.collider.gameObject.name,Is.EqualTo("Stone floor"));
        Assert.That(Physics.Raycast(new Vector3(7,1,6),Vector3.down,2),Is.False,"The old rectangular corner must no longer be floor.");
        var player=GameObject.FindWithTag("Player");((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;
        player.transform.position=new Vector3(0,1,-8.5f);Physics.SyncTransforms();var body=player.GetComponent<Rigidbody>();
        for(int i=0;i<35;i++){body.linearVelocity=new Vector3(0,body.linearVelocity.y,-5);yield return new WaitForFixedUpdate();}
        float radius=Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(0,-1.7f));
        Assert.That(radius,Is.LessThan(7.82f));Assert.That(player.transform.position.y,Is.GreaterThan(.4f));
        T("DemoOpening").GetMethod("ResetSession",flags).Invoke(null,null);
    }
}
