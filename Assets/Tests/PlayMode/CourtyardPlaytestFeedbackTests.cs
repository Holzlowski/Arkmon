using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

public class CourtyardPlaytestFeedbackTests
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [Test] public void KeyboardMovement_ArrowsDiagonalsAndRelease()
    {
        // Use Unity's isolated input runtime: batch-mode editor updates otherwise
        // route synthetic keyboard edges through unfocused editor state buffers.
        var input=new InputTestFixture();input.Setup();
        try
        {
            var keys=InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W,Key.D));InputSystem.Update();
            var move=(Vector2)T("ExplorationInput").GetProperty("Movement").GetValue(null);
            Assert.That(move.magnitude,Is.EqualTo(1).Within(.001));Assert.That(move.x,Is.GreaterThan(.7));
            InputSystem.QueueStateEvent(keys,new KeyboardState(Key.LeftArrow));InputSystem.Update();
            Assert.That(T("ExplorationInput").GetProperty("Movement").GetValue(null),Is.EqualTo(Vector2.left));
            input.Press(keys.eKey);
            Assert.That(T("ExplorationInput").GetMethod("Pressed").Invoke(null,new object[]{Key.E}),Is.True);
            input.Release(keys.eKey);
            Assert.That(T("ExplorationInput").GetMethod("Released").Invoke(null,new object[]{Key.E}),Is.True);
        }
        finally{input.TearDown();}
    }
    [UnityTest] public IEnumerator GardenBoundary_SidePourReturnAndCourtyardViews()
    {
        yield return Load();
        Assert.That(GameObject.Find("West cottage"),Is.Null);
        Assert.That(UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r=>r.name=="Aurenfeld field-edge green hedge"),Is.GreaterThan(15));
        Assert.That(GameObject.Find("Terrace east return handrail"),Is.Not.Null);
        Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.StandaloneInputModule>(FindObjectsSortMode.None),Is.Empty);
        Assert.That(UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>(),Is.Not.Null);
        var station=GameObject.Find("Courtyard watering can").GetComponent(T("CourtyardGarden"));
        Assert.That(station.transform.position.z,Is.GreaterThan(-13));
        Call(station,"InteractWithPlayer");yield return CourtyardGardenTaskTests.WaitForPour();
        Assert.That(T("CourtyardGarden").GetProperty("HasCan").GetValue(null),Is.True);
        T("CourtyardGarden").GetMethod("FillCan").Invoke(null,null);
        var beds=UnityEngine.Object.FindObjectsByType(T("CourtyardGarden"),FindObjectsSortMode.None).Cast<Component>().Where(c=>c.name=="Garden watering point").OrderBy(c=>c.transform.position.x).ToArray();
        foreach(var bed in beds)
        {
            var soil=(Renderer)bed.GetType().GetField("soil",Hidden).GetValue(bed);
            Assert.That(bed.transform.position.x,Is.LessThan(soil.bounds.min.x-.8f));
            CourtyardGardenTaskTests.MoveTo(bed);yield return null;Call(bed,"InteractWithPlayer");
            Assert.That(T("CourtyardGarden").GetProperty("IsWatering").GetValue(null),Is.True);
            yield return CourtyardGardenTaskTests.WaitForPour();
        }
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var exit=new Vector3(34.4f,1,-12);body.position=exit;player.transform.position=exit;Physics.SyncTransforms();yield return null;yield return null;
        Assert.That(Dialogue.GetType().GetProperty("IsOpen").GetValue(Dialogue),Is.True,"Marta reminds Elian at the outer work-area boundary.");
        Call(Dialogue,"CloseDialogue");yield return null;yield return null;
        Assert.That(Dialogue.GetType().GetProperty("IsOpen").GetValue(Dialogue),Is.False,"The reminder must not reopen every frame.");
        CourtyardGardenTaskTests.MoveTo(station);yield return null;Call(station,"InteractWithPlayer");yield return CourtyardGardenTaskTests.WaitForPour();
        Assert.That(T("CourtyardGarden").GetProperty("HasCan").GetValue(null),Is.False);
        foreach(var component in UnityEngine.Object.FindObjectsByType(T("CourtyardCamera"),FindObjectsSortMode.None).Cast<Behaviour>())component.enabled=false;
        foreach(var component in Camera.main.GetComponents<MonoBehaviour>())component.enabled=false;
        var camera=Camera.main;
        Vector3[] targets={new Vector3(-11,1.3f,1),new Vector3(-11,1.3f,1),new Vector3(27,1,-10),new Vector3(12,4,13),new Vector3(-6,2,11),new Vector3(-38,0,-17),new Vector3(53,0,-1)};
        for(int i=0;i<targets.Length;i++)
        {
            camera.transform.position=targets[i]+(i==6?new Vector3(9,8,-10):new Vector3(i==0?-4:i==1?4:0,5,-11));camera.transform.LookAt(targets[i]);camera.fieldOfView=48;
            yield return new WaitForSeconds(.2f);Capture("feedback-review-"+i);
        }
        // Same scenic composition used for the Aurenfeld review.
        camera.transform.position=new Vector3(-61,11,7);camera.transform.rotation=Quaternion.LookRotation(new Vector3(-1,-.06f,.08f));camera.fieldOfView=52;
        yield return null;Capture("feedback-review-fields");
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator InputOnlyScenes_LoadWithoutLegacyErrors()
    {
        foreach(string scene in new[]{"TowerRoom","TowerRoomPaper","StartScene","BattleScene"})
        {
            #if UNITY_EDITOR
            if(scene=="TowerRoomPaper")
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/TowerRoomPaper.unity",new LoadSceneParameters(LoadSceneMode.Single));
            else
#endif
                yield return SceneManager.LoadSceneAsync(scene);
            yield return new WaitForSeconds(.3f);
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.StandaloneInputModule>(FindObjectsSortMode.None),Is.Empty,scene);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>(),Is.Not.Null,scene);
        }
        LogAssert.NoUnexpectedReceived();
    }
}
