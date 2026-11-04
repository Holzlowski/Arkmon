using UnityEngine;

// Distance follows the player, not the elevated courtyard camera.
public sealed class StableLocalFoley : MonoBehaviour
{
    private AudioSource source;
    private Transform player;
    private Camera view;
    public float Audibility { get; private set; }
    private void Awake()
    {
        source=gameObject.AddComponent<AudioSource>();
        source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.dopplerLevel=0;
        source.volume=0;player=GameObject.FindWithTag("Player")?.transform;view=Camera.main;
    }
    private void Update()
    {
        if(player==null){Audibility=0;source.volume=0;return;}
        float distance=Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(transform.position.x,transform.position.z));
        Audibility=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f,14,distance));
        bool speaking=DialogueBoxController.instance!=null && DialogueBoxController.instance.IsOpen;
        float gain=SoundManager.instance!=null?SoundManager.instance.effectsVolume:.55f;
        source.volume=SceneLoadManager.IsTransitioning?0:Audibility*gain*(speaking?.3f:1);
        if(view==null)view=Camera.main;
        if(view!=null)source.panStereo=Mathf.Clamp(Vector3.Dot(transform.position-player.position,view.transform.right)/12,-.5f,.5f);
    }
    public void Play(AudioClip clip,float gain,float pitch=1)
    {
        // Never produce sound during automated editor runs, including runs without -disableAudio.
        if(Application.isBatchMode || clip==null || Audibility<=.001f || !isActiveAndEnabled)return;
        source.pitch=pitch;source.PlayOneShot(clip,gain);
    }
    public void Stop(){if(source!=null)source.Stop();}
    private void OnDisable(){Stop();}
}