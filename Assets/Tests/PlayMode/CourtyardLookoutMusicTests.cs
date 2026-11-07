using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardLookoutMusicTests
{
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n).Invoke(o, args);
    static object Get(object o, string n) => o.GetType().GetProperty(n).GetValue(o);
    static Component Manager => (Component)T("SoundManager").GetField("instance").GetValue(null);
    static AudioSource Music => (AudioSource)T("SoundManager").GetField("music", Hidden).GetValue(Manager);
    static Component Point => GameObject.Find("Western field lookout").GetComponent(T("CourtyardLookout"));
    static IEnumerator Load() => (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { true });

    [UnityTest]
    public IEnumerator CaptionCue_CancelsRepeatsAndRestoresMusicOnUnload()
    {
        yield return Load();
        var point = Point;
        CourtyardGardenTaskTests.MoveTo(point);
        yield return null;
        var original = Music;
        float effects = (float)T("SoundManager").GetField("effectsVolume").GetValue(Manager);
        Call(point, "InteractWithPlayer");
        var cue = point.GetComponent(T("CourtyardLookoutMusic"));
        Assert.That(cue, Is.Not.Null);
        Assert.That(Get(cue, "IsAvailable"), Is.True);
        yield return new WaitForSecondsRealtime(.25f);
        Call(point, "ReturnToCourtyard");
        yield return new WaitForSecondsRealtime(.6f);
        Assert.That(Get(cue, "RevealCount"), Is.EqualTo(0), "Early exit never plays the title accent.");
        Assert.That(Get(Manager, "MusicDucking"), Is.EqualTo(1f));
        Assert.That(Music, Is.SameAs(original));

        Call(point, "InteractWithPlayer");
        yield return new WaitForSecondsRealtime(1);
        Assert.That(Get(Manager, "MusicDucking"), Is.EqualTo(0f));
        Assert.That(Music.volume, Is.Zero);
        Assert.That((float)T("SoundManager").GetField("effectsVolume").GetValue(Manager), Is.EqualTo(effects));
        // Skip only the already-tested camera travel; exercise the real reveal threshold on a later frame.
        T("CourtyardLookout").GetField("progress", Hidden).SetValue(point, .681f);
        yield return null;
        yield return null;
        Assert.That(Get(cue, "RevealCount"), Is.EqualTo(1));
        double scheduled = (double)Get(cue, "RevealTime");
        Assert.That(scheduled, Is.GreaterThan(0));
        yield return new WaitForSecondsRealtime(.25f);
        var caption = GameObject.Find("Landscape caption").GetComponent<CanvasGroup>();
        double now = Application.isBatchMode ? Time.unscaledTimeAsDouble : AudioSettings.dspTime;
        float expected = Mathf.SmoothStep(0, 1, Mathf.Clamp01((float)(now - scheduled) / 2.2f));
        Assert.That(caption.alpha, Is.EqualTo(expected).Within(.03f), "Text fades on the scheduled music clock.");
        Assert.That(caption.alpha, Is.GreaterThan(0));
        Call(point, "InteractWithPlayer");
        Assert.That(Get(cue, "RevealCount"), Is.EqualTo(1), "Repeated interaction cannot restart the cue.");
        var sources = point.GetComponents<AudioSource>();
        Assert.That(sources.Length, Is.EqualTo(3));
        Assert.That(sources.Single(s => s.clip.name == "aurenfeld-title").loop, Is.False);
        Assert.That(sources.Where(s => s.clip.name != "aurenfeld-title").All(s => s.loop), Is.True);
        Assert.That(sources.All(s => s.spatialBlend == 0), Is.True);
        Assert.That(Music, Is.SameAs(original));
        Call(point, "ReturnToCourtyard");
        yield return new WaitForSecondsRealtime(3.6f);
        Assert.That(Get(Manager, "MusicDucking"), Is.EqualTo(1f));
        Assert.That(sources.All(s => !s.isPlaying && s.volume == 0), Is.True);
        Assert.That(Music.volume, Is.EqualTo((float)T("SoundManager").GetField("musicVolume").GetValue(Manager)).Within(.001f));

        Call(point, "InteractWithPlayer");
        yield return new WaitForSecondsRealtime(.3f);
        ((Behaviour)point).enabled = false;
        Assert.That(Get(Manager, "MusicDucking"), Is.EqualTo(1f));
        ((Behaviour)point).enabled = true;
        Call(point, "InteractWithPlayer");
        yield return new WaitForSecondsRealtime(.3f);
        yield return SceneManager.LoadSceneAsync("StartScene");
        yield return new WaitForSecondsRealtime(1);
        Assert.That(Get(Manager, "MusicDucking"), Is.EqualTo(1f));
        Assert.That(Get(Manager, "CurrentMusicName"), Is.EqualTo("courtyard-storybook"));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardLookoutMusic"), FindObjectsSortMode.None), Is.Empty);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void LookoutClips_KeepExactLoopBoundariesAndUnclippedSamples()
    {
        string[] names = { "aurenfeld-approach", "aurenfeld-panorama", "aurenfeld-title" };
        int[] seconds = { 8, 32, 5 };
        for (int i = 0; i < names.Length; i++)
        {
            var clip = Resources.Load<AudioClip>("Audio/" + names[i]);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.channels, Is.EqualTo(2));
            Assert.That(clip.frequency, Is.EqualTo(44100));
            Assert.That(clip.samples, Is.EqualTo(seconds[i] * 44100));
            clip.LoadAudioData();
            var data = new float[clip.samples * 2];
            Assert.That(clip.GetData(data, 0), Is.True);
            Assert.That(data.All(v => !float.IsNaN(v) && !float.IsInfinity(v) && Mathf.Abs(v) < .85f), Is.True);
            if (i < 2)
                for (int c = 0; c < 2; c++) Assert.That(Mathf.Abs(data[c] - data[data.Length - 2 + c]), Is.LessThan(.003f));
        }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        var previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("LookoutMusicCleanup" + Guid.NewGuid()));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}
