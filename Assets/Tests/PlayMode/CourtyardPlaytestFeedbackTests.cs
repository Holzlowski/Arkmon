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
