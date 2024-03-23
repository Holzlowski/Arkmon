using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    private AudioSource audioSource;
    // Hier könntest du eine Liste von Soundclips für jede Szene definieren
    public List<AudioClip> sceneMusicClips;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CheckSceneSound();
    }

    public void CheckSceneSound()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            PlayMusic(0);
        }
        else if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            PlayMusic(1);
        }
    }

    public void PlayMusic(int audioClipIndex)
    {
        audioSource.clip = sceneMusicClips[audioClipIndex];
        audioSource.Play();
    }

}
