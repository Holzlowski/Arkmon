using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public List<AudioClip> sceneMusicClips = new List<AudioClip>();
    [Range(0, 1)] public float musicVolume = .35f;
    [Range(0, 1)] public float effectsVolume = .55f;
    private AudioSource music, outgoing, effects;
    private Coroutine crossfade;
    private float musicLevel, outgoingLevel, musicDucking = 1f;
    private float conversationGain = 1;
    public float ConversationGain => conversationGain;
    public float MusicDucking => musicDucking;
    private void Update()
    {
        bool conversation = SceneManager.GetActiveScene().name == "StartScene"
            && transitionDestination == null && (DialogueBoxController.instance?.IsOpen ?? false);
        conversationGain = Mathf.MoveTowards(conversationGain, conversation ? .62f : 1f,
            Time.unscaledDeltaTime / (conversation ? .65f : 1.2f));
        ApplyMusicVolumes();
    }
    public void SetMusicDucking(float gain)
    { musicDucking = Mathf.Clamp01(gain); ApplyMusicVolumes(); }
    private void ApplyMusicVolumes()
    {
        if (music != null) music.volume = musicVolume * musicLevel * musicDucking * conversationGain;
        if (outgoing != null) outgoing.volume = musicVolume * outgoingLevel * musicDucking * conversationGain;
    }
    private string transitionDestination;
    public string CurrentMusicName => music != null && music.clip != null ? music.clip.name : "";
    public string LastEffectName { get; private set; }
    public void BeginSceneTransition(string sceneName)
    { transitionDestination = sceneName; CheckSceneSound(); }
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureManager()
    { if (instance == null) new GameObject("Scene audio").AddComponent<SoundManager>(); }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject);
        foreach (AudioSource old in GetComponents<AudioSource>()) { old.Stop(); old.enabled = false; }
        music = CreateSource(true); outgoing = CreateSource(true); effects = CreateSource(false);
        effects.volume = 1f;
        SceneManager.sceneLoaded += SceneLoaded; SceneManager.sceneUnloaded += SceneUnloaded;
        BaseAction.OnAnyActionStarted += ActionStarted;
        MeleeAttackAction.OnAnyMeleeHit += AttackHit; RamAction.OnAnyRamHit += AttackHit;
        CheckSceneSound();
    }
    private AudioSource CreateSource(bool loop)
    {
        var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
        source.loop = loop; source.spatialBlend = 0; source.volume = 0; return source;
    }
    private AudioClip Clip(string name)
    {
        if (!clips.TryGetValue(name, out AudioClip clip)) { clip = Resources.Load<AudioClip>("Audio/" + name); clips[name] = clip; }
        return clip;
    }
    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    { transitionDestination = null; CheckSceneSound(); }
    private void SceneUnloaded(Scene scene) => CheckSceneSound();
    public void CheckSceneSound()
    {
        string scene = transitionDestination ?? SceneManager.GetActiveScene().name;
        bool tower = scene == "TowerRoom" || scene == "TowerRoomPaper";
        AudioClip clip = scene == "BattleScene" ? Clip("battle")
            : scene == "StartScene" ? Clip("courtyard-storybook")
            : tower ? Clip("courtyard") : null;
        if (clip == null)
        {
            if (crossfade != null) StopCoroutine(crossfade);
            music.Stop(); outgoing.Stop(); effects.Stop();
            music.clip = outgoing.clip = null;
            music.enabled = outgoing.enabled = effects.enabled = false;
            musicLevel = outgoingLevel = 0; ApplyMusicVolumes();
            crossfade = null; return;
        }
        music.enabled = outgoing.enabled = effects.enabled = true;
        if (clip == music.clip) return;
        if (crossfade != null) StopCoroutine(crossfade);
        outgoingLevel = musicLevel; musicLevel = 0;
        (music, outgoing) = (outgoing, music);
        ApplyMusicVolumes();
        music.Stop(); music.clip = clip; music.volume = 0;
        if (clip != null) music.Play();
        crossfade = StartCoroutine(FadeMusic(scene == "StartScene" || tower ? 3.2f : .8f));
    }
    private IEnumerator FadeMusic(float duration)
    {
        float oldLevel = outgoingLevel;
        float elapsed = 0;
        while (elapsed < duration)
        {
            // A scene activation hitch must not skip the audible tail of the old track.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (transitionDestination != null && SceneManager.GetActiveScene().name != transitionDestination)
                elapsed = Mathf.Min(elapsed, duration * .45f);
            float t = Mathf.Clamp01(elapsed / duration);
            musicLevel = Mathf.Sin(t * Mathf.PI * .5f);
            outgoingLevel = oldLevel * Mathf.Cos(t * Mathf.PI * .5f);
            ApplyMusicVolumes(); yield return null;
        }
        outgoing.Stop(); outgoingLevel = 0; musicLevel = 1; ApplyMusicVolumes(); crossfade = null;
    }
    public void PlayMusic(int index)
    { if (index >= 0 && index < sceneMusicClips.Count) { music.clip = sceneMusicClips[index]; musicLevel = 1; ApplyMusicVolumes(); music.Play(); } }
    public void PlayEffect(string name) => PlayEffect(name, 1f);

    public void PlayEffect(string name, float gain)
    {
        AudioClip clip = Clip(name);
        if (clip != null && effects.enabled && FindFirstObjectByType<AudioListener>() != null)
        { LastEffectName = name; effects.PlayOneShot(clip, effectsVolume * Mathf.Clamp01(gain)); }
    }
    private void ActionStarted(object sender, EventArgs args)
    {
        string name = sender is FlameSwordAction ? "flame-swing" : sender is MeleeAttackAction ? "claw"
            : sender is RamAction ? "ram" : sender is MoveAction ? "move" : sender is StandUpAction ? "stand" : null;
        if (name != null) PlayEffect(name);
    }
    private void AttackHit(object sender, EventArgs args) => PlayEffect(sender is FlameSwordAction ? "fire-hit" : "hit");
    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneUnloaded -= SceneUnloaded;
        BaseAction.OnAnyActionStarted -= ActionStarted;
        MeleeAttackAction.OnAnyMeleeHit -= AttackHit; RamAction.OnAnyRamHit -= AttackHit; instance = null;
    }
}
