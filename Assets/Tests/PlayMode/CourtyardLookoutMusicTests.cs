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
        var wind=GameObject.Find("Aurenfeld field atmosphere").GetComponent<AudioSource>();
        Assert.That(wind.clip.name,Is.EqualTo("field-rustle"));
        yield return new WaitForSecondsRealtime(.8f);
        Assert.That(wind.volume,Is.Zero,"Additional wind audio is disabled by default.");
        Assert.That(wind.isPlaying,Is.False);
        var birdObject=GameObject.Find("Aurenfeld quiet birdsong");
        var birds=birdObject.GetComponent(T("CourtyardBirdsong"));
        var birdSource=birdObject.GetComponent<AudioSource>();
        var birdCount=T("CourtyardBirdsong").GetField("callCount",Hidden);
        Assert.That((int)birdCount.GetValue(birds),Is.Zero,"No birds interrupt the name accent.");
        Assert.That(birdSource.volume,Is.Zero);
        T("CourtyardLookout").GetField("progress",Hidden).SetValue(point,.999f);
        yield return null;yield return null;
        var nextCall=T("CourtyardBirdsong").GetField("nextCallAt",Hidden);
        Assert.That((float)nextCall.GetValue(birds),Is.GreaterThan(Time.unscaledTime+3));
        nextCall.SetValue(birds,Time.unscaledTime-1);
        yield return null;yield return null;
        Assert.That((int)birdCount.GetValue(birds),Is.EqualTo(1));
        Assert.That(birdSource.clip.name,Is.EqualTo("aurenfeld-bird-1"));
        Assert.That(birdSource.loop,Is.False);
        Assert.That(birdSource.volume,Is.GreaterThan(0).And.LessThanOrEqualTo(effects*.20f+.001f));
        Assert.That((float)nextCall.GetValue(birds)-Time.unscaledTime-birdSource.clip.length,Is.InRange(9.8f,18.1f));
        nextCall.SetValue(birds,Time.unscaledTime-1);
        yield return null;yield return null;
        Assert.That((int)birdCount.GetValue(birds),Is.EqualTo(2));
        Assert.That(birdSource.clip.name,Is.EqualTo("aurenfeld-bird-2"));
        var effectsField=T("SoundManager").GetField("effectsVolume");
        try
        {
            effectsField.SetValue(Manager,0f);
            nextCall.SetValue(birds,Time.unscaledTime-1);
            yield return null;yield return null;
            Assert.That(birdSource.volume,Is.Zero,"Effects mute must silence birds independently of music.");
            Assert.That((int)birdCount.GetValue(birds),Is.EqualTo(2));
        }
        finally { effectsField.SetValue(Manager,effects); }

        float musicGain=(float)T("SoundManager").GetField("musicVolume").GetValue(Manager);
        Assert.That(sources.Single(s=>s.clip.name=="aurenfeld-title").volume,Is.EqualTo(musicGain).Within(.001f),"Keep the name accent at its approved level.");
        Assert.That(sources.Single(s=>s.clip.name=="aurenfeld-panorama").volume,Is.EqualTo(musicGain*.82f).Within(.001f));
        Assert.That(Music, Is.SameAs(original));
        Call(point, "ReturnToCourtyard");
        yield return new WaitForSecondsRealtime(3.6f);
        Assert.That(Get(Manager, "MusicDucking"), Is.EqualTo(1f));
        Assert.That(sources.All(s => !s.isPlaying && s.volume == 0), Is.True);
        Assert.That(wind.volume,Is.Zero,"Field wind fades out on return.");
        Assert.That(birdSource.volume,Is.Zero);Assert.That(birdSource.isPlaying,Is.False);
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
        Assert.That(birdObject==null,Is.True,"Old scene birds must be released.");
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardBirdsong"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void LookoutClips_KeepExactLoopBoundariesAndUnclippedSamples()
    {
        string[] names = { "aurenfeld-approach", "aurenfeld-panorama", "aurenfeld-title", "field-rustle" };
        float[] seconds = { 8, 53.76f, 5, 48 };
        int[] rates = { 44100, 44100, 44100, 32000 };
        for (int i = 0; i < names.Length; i++)
        {
            var clip = Resources.Load<AudioClip>("Audio/" + names[i]);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.channels, Is.EqualTo(2));
            Assert.That(clip.frequency, Is.EqualTo(rates[i]));
            Assert.That(clip.samples, Is.EqualTo(Mathf.RoundToInt(seconds[i] * rates[i])));
            clip.LoadAudioData();
            var data = new float[clip.samples * 2];
            Assert.That(clip.GetData(data, 0), Is.True);
            Assert.That(data.All(v => !float.IsNaN(v) && !float.IsInfinity(v) && Mathf.Abs(v) < .85f), Is.True);
            if (names[i] != "aurenfeld-title")
                for (int c = 0; c < 2; c++) Assert.That(Mathf.Abs(data[c] - data[data.Length - 2 + c]), Is.LessThan(.003f));
        }
        foreach(string name in new[]{"aurenfeld-bird-1","aurenfeld-bird-2"})
        {
            var clip=Resources.Load<AudioClip>("Audio/"+name);
            Assert.That(clip,Is.Not.Null);Assert.That(clip.channels,Is.EqualTo(1));
            Assert.That(clip.frequency,Is.EqualTo(32000));Assert.That(clip.length,Is.InRange(1.2f,1.6f));
            clip.LoadAudioData();var samples=new float[clip.samples];Assert.That(clip.GetData(samples,0),Is.True);
            Assert.That(samples.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)&&Mathf.Abs(v)<.25f),Is.True);
            Assert.That(Mathf.Abs(samples[0])+Mathf.Abs(samples[samples.Length-1]),Is.LessThan(.002f));
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
