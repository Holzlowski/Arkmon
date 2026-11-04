using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardJohnGroundingTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest] public IEnumerator JohnKeepsHisFeetOnTheCourtyard_InIdleDialogueAndAfterReload()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        T("IntroChapter").GetMethod("EquipFamilyOutfit").Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        for(int load=0;load<2;load++)
        {
            yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(.5f);
            var john=UnityEngine.Object.FindObjectsByType(T("NPC"),FindObjectsSortMode.None).Cast<Component>().Single(n=>(string)n.GetType().GetField("npcName").GetValue(n)=="John");
            var sprite=john.GetComponentInChildren<SpriteRenderer>();
            var hits=Physics.RaycastAll(john.transform.position+Vector3.up*3,Vector3.down,5,~0,QueryTriggerInteraction.Ignore);
            var ground=hits.Where(h=>h.collider.CompareTag("Ground")).OrderBy(h=>h.distance).First();
            for(int frame=0;frame<24;frame++)
            {
                yield return null;
                float foot=sprite.sprite.vertices.Min(v=>sprite.transform.TransformPoint(v).y);
                Assert.That(foot-ground.point.y,Is.InRange(-.03f,.09f),"John's sprite foot contour should meet the courtyard ground.");
            }
            if(load==0)
            {
                var follow=(Behaviour)UnityEngine.Object.FindFirstObjectByType(T("CourtyardCamera"));follow.enabled=false;
                var camera=Camera.main;var brain=(Behaviour)camera.GetComponent(T("Cinemachine.CinemachineBrain"));if(brain!=null)brain.enabled=false;
                camera.transform.position=john.transform.position+Vector3.up*1.05f-camera.transform.forward*5;camera.fieldOfView=35;
                yield return null;yield return null;
                typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"courtyard-john-grounded"});
                john.GetType().GetMethod("InteractWithPlayer").Invoke(john,null);yield return null;
                var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
                Assert.That(dialogue.GetType().GetProperty("ActiveSpeaker").GetValue(dialogue),Is.EqualTo("John"));
                dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
            }
        }
        LogAssert.NoUnexpectedReceived();
    }
}