using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PresentationPolishTests
{
    private static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [UnityTest] public IEnumerator Dialogue_UsesActualSpeakerPortraitAndReadableFantasyFont()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("StartScene");yield return null;yield return null;
        object controller=T("DialogueBoxController").GetField("instance").GetValue(null);
        var portrait=(Image)T("DialogueBoxController").GetField("portraitImage",Flags).GetValue(controller);
        var text=(TextMeshProUGUI)T("DialogueBoxController").GetField("dialogueText",Flags).GetValue(controller);
        object style=T("PaperUIStyle").GetMethod("Load").Invoke(null,null);
        foreach(string speaker in new[]{"Der Junge","John","Unbekannt","Der Junge"})
        {
            T("DialogueBoxController").GetMethod("ShowMessage").Invoke(controller,new object[]{"Äußere Prüfung – Kafla −18 LP …",speaker});
            yield return null;
            Sprite expected=(Sprite)T("PaperUIStyle").GetMethod("PortraitFor").Invoke(style,new object[]{speaker});
            Assert.That(portrait.sprite,Is.EqualTo(expected));Assert.That(portrait.enabled,Is.EqualTo(expected!=null));
            Assert.That(text.font.name,Does.Contain("Alegreya"));
            foreach(char c in "ÄäÖöÜüß−–…")Assert.That(text.font.characterLookupTable.ContainsKey(c),Is.True,$"Missing glyph: {c}");
            T("DialogueBoxController").GetMethod("CloseDialogue").Invoke(controller,null);
        }
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Scene previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("PresentationCleanup"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}
