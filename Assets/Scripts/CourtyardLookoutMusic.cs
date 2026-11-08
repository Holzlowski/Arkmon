using UnityEngine;

// The caption and its musical accent share one clock, including after frame hitches.
// Scene-owned sources leave the courtyard song playing silently at its current position.
public sealed class CourtyardLookoutMusic : MonoBehaviour
{
    private AudioSource approach, panorama, title;
    private SoundManager manager;
    private bool presenting, ownsMix, revealed;
    private float blend;
    public bool IsAvailable { get; private set; }
    public int RevealCount { get; private set; }
    public double RevealTime { get; private set; }
    private static double Clock => Application.isBatchMode ? Time.unscaledTimeAsDouble : AudioSettings.dspTime;
    public float CaptionAlpha => !revealed ? 0 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((float)(Clock - RevealTime) / 2.2f));

    private AudioSource Source(string clipName, bool loop)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = loop; source.spatialBlend = 0; source.volume = 0;
        source.clip = Resources.Load<AudioClip>("Audio/" + clipName);
        return source;
    }

    public void Begin()
    {
        StopImmediately();
        if (approach == null)
        {
            approach = Source("aurenfeld-approach", true);
            panorama = Source("aurenfeld-panorama", true);
            title = Source("aurenfeld-title", false);
        }
        manager = SoundManager.instance;
        IsAvailable = manager != null && approach.clip != null && panorama.clip != null && title.clip != null;
        RevealCount = 0; revealed = false; RevealTime = 0;
        if (!IsAvailable) return;
        presenting = ownsMix = true;
        if (!Application.isBatchMode) approach.Play();
    }

    public void RevealCaption()
    {
        if (!presenting || !IsAvailable || revealed) return;
        revealed = true; RevealCount++;
        // Preloaded PCM sources get a small scheduling lead; the caption waits for that same instant.
        RevealTime = Clock + .05;
        if (!Application.isBatchMode)
        {
            panorama.PlayScheduled(RevealTime);
            title.PlayScheduled(RevealTime);
        }
        ApplyMix();
    }

    public void End() => presenting = false;

    private void Update()
    {
        if (!ownsMix) return;
        if (manager == null || SceneLoadManager.IsTransitioning) { StopImmediately(); return; }
        blend = Mathf.MoveTowards(blend, presenting ? 1 : 0, Time.unscaledDeltaTime / (presenting ? .8f : 1f));
        ApplyMix();
        if (!presenting && blend <= 0) StopImmediately();
    }

    private void ApplyMix()
    {
        if (manager == null) return;
        float themeBlend = revealed ? Mathf.Clamp01((float)(Clock - RevealTime) / .65f) : 0;
        float volume = manager.musicVolume * blend;
        approach.volume = volume * .82f * (1 - themeBlend);
        panorama.volume = volume * .82f * themeBlend;
        title.volume = volume;
        if (themeBlend >= 1 && approach.isPlaying) approach.Stop();
        manager.SetMusicDucking(1 - blend);
    }

    public void StopImmediately()
    {
        if (approach != null) approach.Stop();
        if (panorama != null) panorama.Stop();
        if (title != null) title.Stop();
        if (ownsMix && manager != null) manager.SetMusicDucking(1);
        if (approach != null) approach.volume = panorama.volume = title.volume = 0;
        blend = 0; ownsMix = presenting = false;
    }
    private void OnDisable() => StopImmediately();
}
