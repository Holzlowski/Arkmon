using UnityEngine;

// Quiet, local ambience; unloading the room also stops its source and particles.
public class TowerMorningAtmosphere : MonoBehaviour
{
    private AudioSource ambience;
    private void Start()
    {
        var clip = Resources.Load<AudioClip>("Audio/tower-courtyard-ambience");
        if (clip == null || FindFirstObjectByType<AudioListener>() == null) return;
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.playOnAwake = false; ambience.clip = clip; ambience.loop = true; ambience.spatialBlend = 0; ambience.volume = 0; ambience.Play();
    }
    private void Update()
    {
        if (ambience == null) return;
        float target = SceneLoadManager.IsTransitioning ? 0 : DialogueBoxController.instance != null && DialogueBoxController.instance.IsOpen ? .045f : .075f;
        ambience.volume = Mathf.MoveTowards(ambience.volume, target, Time.unscaledDeltaTime*.12f);
    }
    private void OnDisable() { if (ambience != null) ambience.Stop(); }
}
