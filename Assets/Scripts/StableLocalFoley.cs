using UnityEngine;

// Station sounds follow the player. The cinematic carriage listens from the current shot.
public sealed class StableLocalFoley : MonoBehaviour
{
    private AudioSource source;
    private Transform player;
    private Camera view;
    public float Audibility { get; private set; }
    internal bool ListenFromCamera { get; set; }
    private void Awake()
    {
        source=gameObject.AddComponent<AudioSource>();
        source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.dopplerLevel=0;
        source.volume=0;player=GameObject.FindWithTag("Player")?.transform;view=Camera.main;
    }
    private void Update()
    {
        if(player==null){Audibility=0;source.volume=0;return;}
        if(view==null)view=Camera.main;
        Vector3 listener=ListenFromCamera && view!=null ? view.transform.position : player.position;
        Vector3 offset=transform.position-listener;
        float distance=new Vector2(offset.x,offset.z).magnitude;
        Audibility=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f,14,distance));
        bool speaking=DialogueBoxController.instance!=null && DialogueBoxController.instance.IsOpen;
        float gain=SoundManager.instance!=null?SoundManager.instance.effectsVolume:.55f;
        bool scenic = CourtyardLookout.IsPresenting || SceneLoadManager.IsTransitioning;
        float target = scenic ? 0 : Audibility * gain * (speaking ? .24f : .85f);
        source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 1.5f);
        if(view!=null)source.panStereo=Mathf.Clamp(Vector3.Dot(offset,view.transform.right)/12,-.5f,.5f);
    }
    public void Play(AudioClip clip,float gain,float pitch=1)
    {
        // Never produce sound during automated editor runs, including runs without -disableAudio.
        if(CourtyardLookout.IsPresenting || SceneLoadManager.IsTransitioning || Application.isBatchMode || clip==null || Audibility<=.001f || !isActiveAndEnabled)return;
        source.pitch=pitch;source.PlayOneShot(clip,gain);
    }
    public void Stop(){if(source!=null)source.Stop();}
    private void OnDisable(){Stop();}
}