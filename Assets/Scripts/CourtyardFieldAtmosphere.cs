using UnityEngine;
using UnityEngine.Rendering;

// A bounded scenic flock and quiet field rustle; owned entirely by StartScene.
public sealed class CourtyardFieldAtmosphere : MonoBehaviour
{
    readonly Transform[] birds = new Transform[3];
    readonly Mesh[] wings = new Mesh[3];
    readonly Vector3[] vertices = new Vector3[9];
    Material ink;
    AudioSource rustle;
    float age;

    internal static void Install()
    {
        if (GameObject.Find("Aurenfeld field atmosphere") != null) return;
        var style = new GameObject("Aurenfeld field atmosphere").AddComponent<CourtyardFieldAtmosphere>();
        style.Build();
    }
    void Build()
    {
        ink = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Distant bird ink" };
        ink.SetColor("_BaseColor",new Color(.26f,.24f,.23f));
        ink.SetFloat("_Cull",0);
        for (int i=0;i<birds.Length;i++)
        {
            var bird=new GameObject("Distant field bird",typeof(MeshFilter),typeof(MeshRenderer));
            birds[i]=bird.transform;birds[i].SetParent(transform,false);
            var mesh=new Mesh {name="Scenic bird wings"};mesh.MarkDynamic();
            mesh.vertices=vertices;mesh.triangles=new[]{0,1,2,3,4,5,6,7,8};
            mesh.bounds=new Bounds(Vector3.zero,new Vector3(3,2,1));
            wings[i]=mesh;bird.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=bird.GetComponent<MeshRenderer>();renderer.sharedMaterial=ink;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            bird.SetActive(false);
        }
        rustle=gameObject.AddComponent<AudioSource>();
        rustle.playOnAwake=false;rustle.loop=true;rustle.spatialBlend=0;rustle.volume=0;
        rustle.clip=Resources.Load<AudioClip>("Audio/field-rustle");
        if (!Application.isBatchMode && rustle.clip!=null) rustle.Play();
    }
    void Update()
    {
        age+=Time.deltaTime;
        var camera=Camera.main;
        for(int i=0;i<birds.Length;i++)
        {
            if(birds[i]==null)continue;
            // A short crossing followed by a long quiet gap. Birds enter and leave beyond the view.
            float flight=Mathf.Repeat(age+5-i*.85f,44);
            bool visible=flight<18 && camera!=null;
            birds[i].gameObject.SetActive(visible);
            if(!visible)continue;
            float t=flight/18;
            birds[i].position=new Vector3(-216-i*3,32+i*1.4f+Mathf.Sin(t*Mathf.PI)*3,Mathf.Lerp(-100,170,t)+i*2);
            birds[i].rotation=camera.transform.rotation;
            float flap=Mathf.Sin(age*4.1f+i*.9f)*.38f;
            vertices[0]=new Vector3(0,0,0);vertices[1]=new Vector3(-1.15f,.25f+flap,0);vertices[2]=new Vector3(-.47f,-.10f+flap*.4f,0);
            vertices[3]=Vector3.zero;vertices[4]=new Vector3(.47f,-.10f+flap*.4f,0);vertices[5]=new Vector3(1.15f,.25f+flap,0);
            vertices[6]=new Vector3(-.12f,.04f,0);vertices[7]=new Vector3(.12f,.04f,0);vertices[8]=new Vector3(0,-.28f,0);
            wings[i].vertices=vertices;
        }
        if(rustle==null)return;
        bool presenting=CourtyardLookout.IsPresenting && !SceneLoadManager.IsTransitioning;
        float gain=SoundManager.instance!=null?SoundManager.instance.effectsVolume:.55f;
        float target = presenting ? .24f * gain : 0;
        rustle.volume=Mathf.MoveTowards(rustle.volume,target,Time.unscaledDeltaTime*.10f);
        if(!Application.isBatchMode && presenting && !rustle.isPlaying && rustle.clip!=null)rustle.Play();
        if(!presenting && rustle.volume<=0 && rustle.isPlaying)rustle.Stop();
    }
    void OnDisable()
    {
        if(rustle!=null){rustle.volume=0;rustle.Stop();}
        foreach(var bird in birds)if(bird!=null)bird.gameObject.SetActive(false);
    }
    void OnDestroy()
    {
        foreach(var mesh in wings)if(mesh!=null)Destroy(mesh);
        if(ink!=null)Destroy(ink);
    }
}
