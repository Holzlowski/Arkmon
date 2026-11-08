using UnityEngine;

// Sparse recorded calls, independent of music and the optional wind audition.
public sealed class CourtyardBirdsong : MonoBehaviour
{
    readonly System.Random timing = new System.Random(9427);
    readonly AudioClip[] calls = new AudioClip[2];
    AudioSource source;
    CourtyardLookout lookout;
    bool wasReady;
    float nextCallAt;
    int callCount;

    internal static void Install(Transform parent, CourtyardLookout lookout)
    {
        if (parent.Find("Aurenfeld quiet birdsong") != null) return;
        var go = new GameObject("Aurenfeld quiet birdsong");
        go.transform.SetParent(parent, false);
        var birdsong = go.AddComponent<CourtyardBirdsong>();
        birdsong.lookout = lookout;
        birdsong.source = go.AddComponent<AudioSource>();
        birdsong.source.playOnAwake = false;
        birdsong.source.loop = false;
        birdsong.source.spatialBlend = 0;
        birdsong.source.priority = 180;
        birdsong.source.volume = 0;
        for (int i = 0; i < birdsong.calls.Length; i++)
            birdsong.calls[i] = Resources.Load<AudioClip>("Audio/aurenfeld-bird-" + (i + 1));
    }

    void Update()
    {
        if (source == null) return;
        bool ready = lookout != null && lookout.isActiveAndEnabled && CourtyardLookout.IsPresenting
            && !lookout.IsReturning && lookout.Progress >= .98f && !SceneLoadManager.IsTransitioning;
        float gain = SoundManager.instance != null ? SoundManager.instance.effectsVolume : .55f;
        float target = ready ? Mathf.Clamp01(gain) * .20f : 0;
        source.volume = gain <= 0 ? 0 : Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * .28f);
        if (!ready)
        {
            wasReady = false;
            if (source.volume <= 0) source.Stop();
            return;
        }
        float now = Time.unscaledTime;
        if (!wasReady)
        {
            // Wait for the arrival and five-second name accent before the first short call.
            nextCallAt = now + 3.5f;
            wasReady = true;
        }
        if (now < nextCallAt) return;
        int index = callCount % calls.Length;
        var clip = calls[index];
        nextCallAt = now + (clip != null ? clip.length : 0) + 10 + (float)timing.NextDouble() * 8;
        if (clip == null || gain <= 0) return;
        source.clip = clip;
        source.panStereo = index == 0 ? -.22f : .24f;
        source.pitch = 1;
        callCount++;
        // Silent tests still exercise scheduling, settings and cleanup.
        if (!Application.isBatchMode) source.Play();
    }

    void OnDisable()
    {
        wasReady = false;
        if (source != null) { source.Stop(); source.volume = 0; }
    }
}
