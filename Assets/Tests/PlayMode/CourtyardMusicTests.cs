using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardMusicTests
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static Component Audio => (Component)T("SoundManager").GetField("instance").GetValue(null);
    static AudioSource Music => (AudioSource)T("SoundManager").GetField("music", Fields).GetValue(Audio);
    static bool Transitioning => (bool)T("SceneLoadManager").GetProperty("IsTransitioning").GetValue(null);

    [UnityTest]
    public IEnumerator ImportedLoopAndTowerToCourtyardFade_PreserveSamplesAndSceneMusic()
    {
        var clip = Resources.Load<AudioClip>("Audio/courtyard-storybook");
        Assert.That(clip, Is.Not.Null);
        clip.LoadAudioData();
        float deadline = Time.realtimeSinceStartup + 10;
        while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(clip.loadState, Is.EqualTo(AudioDataLoadState.Loaded));
        Assert.That(clip.channels, Is.EqualTo(2));
        Assert.That(clip.frequency, Is.EqualTo(44100));
        Assert.That(clip.samples, Is.EqualTo(2948644), "PCM import must preserve the exact loop period.");
        var first = new float[128]; var last = new float[128];
        Assert.That(clip.GetData(first, 0), Is.True);
        Assert.That(clip.GetData(last, clip.samples - 64), Is.True);
        for (int channel = 0; channel < 2; channel++)
            Assert.That(Mathf.Abs(first[channel] - last[126 + channel]), Is.LessThan(.001f), "Imported repeat boundary must stay continuous.");

        yield return SceneManager.LoadSceneAsync("TowerRoom");
        yield return new WaitForSecondsRealtime(1);
        Assert.That(Music.clip.name, Is.EqualTo("courtyard"), "The tower keeps its existing music.");
        var oldSource = Music;
        var sceneManager = T("SceneLoadManager").GetField("instance").GetValue(null);
        Assert.That(sceneManager, Is.Not.Null);
        T("SceneLoadManager").GetMethod("LoadExplorationScene").Invoke(sceneManager, new object[] { "StartScene" });
        var courtyardSource = Music;
        Assert.That(courtyardSource, Is.Not.SameAs(oldSource));
        Assert.That(courtyardSource.clip, Is.SameAs(clip));
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(oldSource.volume,Is.GreaterThan(0),"The tower track must continue into the passage.");
        Assert.That(courtyardSource.volume,Is.GreaterThan(0),"The courtyard must fade in under the tower track.");
        Assert.That(courtyardSource.loop, Is.True);
        Assert.That(courtyardSource.spatialBlend, Is.Zero);
        deadline = Time.realtimeSinceStartup + 20;
        while (Transitioning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Transitioning, Is.False);
        yield return new WaitForSecondsRealtime(3.5f);
        // The arrival greeting intentionally ducks the music; close it before checking full gain.
        var dialogue=T("DialogueBoxController").GetField("instance").GetValue(null);
        T("DialogueBoxController").GetMethod("CloseDialogue").Invoke(dialogue,null);
        yield return new WaitForSecondsRealtime(1.3f);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StartScene"));
        Assert.That(Music, Is.SameAs(courtyardSource), "Scene load must not restart the source selected at transition start.");
        Assert.That(oldSource.volume, Is.Zero.Within(.001f));
        float targetVolume = (float)T("SoundManager").GetField("musicVolume").GetValue(Audio);
        Assert.That(Music.volume, Is.EqualTo(targetVolume).Within(.001f));
        T("SoundManager").GetMethod("CheckSceneSound").Invoke(Audio, null);
        Assert.That(Music, Is.SameAs(courtyardSource));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("SoundManager"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        var previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("CourtyardMusicCleanup" + Guid.NewGuid()));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}
