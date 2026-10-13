using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Supplies deterministic input after the legacy input reader without replacing locomotion.
[DefaultExecutionOrder(1000)]
public class ExplorationInputFixture : MonoBehaviour
{
    public Component controller;
    public Vector2 input;
    private void Update(){if(controller!=null)controller.GetType().GetField("moveInput",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(controller,input);}
}
public class ExplorationMovementTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest]public IEnumerator Walking_StopsImmediatelyReversesAndUsesBothOutfitCycles()
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Static;
        T("DemoOpening").GetMethod("ResetSession",flags).Invoke(null,null);
        T("DemoOpening").GetField("<HasAwakened>k__BackingField",flags).SetValue(null,true);
        yield return SceneManager.LoadSceneAsync("TowerRoom");yield return new WaitForSeconds(1.4f);
        object dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);dialogue.GetType().GetMethod("CloseDialogue").Invoke(dialogue,null);
        var player=GameObject.FindWithTag("Player");var driver=player.AddComponent<ExplorationInputFixture>();driver.controller=player.GetComponent(T("PlayerController"));
        var body=player.GetComponent<Rigidbody>();body.position=new Vector3(0,1,-3);Physics.SyncTransforms();
        var visual=player.GetComponent(T("PaperCharacterVisual"));var sprite=(SpriteRenderer)visual.GetType().GetField("spriteRenderer",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(visual);
        foreach(string outfit in new[]{"Room","Mantle","Room-NoLamp","Mantle-NoLamp"})
        {
            var frames=Resources.LoadAll<Sprite>("Prince-"+outfit+"-Walk");Assert.That(frames.Length,Is.EqualTo(8));
            Assert.That(frames.All(s=>Mathf.Abs(s.bounds.size.y-2)<.01f),Is.True,"Frames retain the idle height.");
        }
        driver.input=Vector2.right;yield return new WaitForSeconds(.18f);
        Assert.That(sprite.sprite.name,Does.StartWith("Prince-Room-NoLamp-Walk"));string first=sprite.sprite.name;
        yield return new WaitForSeconds(.12f);Assert.That(sprite.sprite.name,Is.Not.EqualTo(first));
        driver.input=Vector2.left;yield return new WaitForSeconds(.06f);Assert.That(body.linearVelocity.x,Is.LessThan(0));Assert.That(sprite.flipX,Is.True);
        driver.input=Vector2.one.normalized;yield return new WaitForSeconds(.08f);
        float configuredSpeed=(float)driver.controller.GetType().GetField("speed").GetValue(driver.controller);
        Assert.That(new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude,Is.LessThanOrEqualTo(configuredSpeed+.01f));
        driver.input=Vector2.zero;yield return new WaitForSeconds(.06f);Vector3 stopped=body.position;
        Assert.That(new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude,Is.LessThan(.001f));
        yield return new WaitForSeconds(.1f);Assert.That(Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(stopped.x,stopped.z)),Is.LessThan(.01f));
        Assert.That(sprite.sprite.name,Is.EqualTo("Prince-Room-NoLamp"));
        T("DemoOpening").GetMethod("FinishTakingMantle").Invoke(null,null);driver.input=Vector2.right;yield return new WaitForSeconds(.15f);
        Assert.That(sprite.sprite.name,Does.StartWith("Prince-Mantle-NoLamp-Walk"));
        driver.input=Vector2.zero;UnityEngine.Object.Destroy(driver);yield return null;T("DemoOpening").GetMethod("ResetSession",flags).Invoke(null,null);
        LogAssert.NoUnexpectedReceived();
    }
}
