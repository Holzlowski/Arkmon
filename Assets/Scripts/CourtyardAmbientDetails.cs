using UnityEngine;
using UnityEngine.Rendering;

// Small scene-owned effects. Audio distance follows Elian rather than the elevated camera.
public sealed class CourtyardAmbientDetails : MonoBehaviour
{
    static CourtyardAmbientDetails active;
    ParticleSystem dust,leaves,oakLeaves;
    Material dustMaterial,leafMaterial;
    AudioSource stream;
    Transform player;
    float oldVolume,oldBlend,oldPan;
    bool wasPlaying;
    Vector3 leafOrigin;
    StableLocalFoley gardenWind;
    AudioClip gardenRustle;
    float nextGardenGust;
    internal static void Install()
    {
        if(GameObject.Find("Courtyard quiet details")!=null)return;
        var shader=Resources.Load<Shader>("CourtyardSmallParticles");if(shader==null)return;
        var style=new GameObject("Courtyard quiet details").AddComponent<CourtyardAmbientDetails>();active=style;
        style.dustMaterial=new Material(shader){name="Soft courtyard footfall dust"};
        style.leafMaterial=new Material(shader){name="Small painted drifting leaves"};style.leafMaterial.SetFloat("_Leaf",1);
        style.player=GameObject.FindWithTag("Player")?.transform;
        style.dust=style.Create("Courtyard footfall dust",style.dustMaterial,24,false);style.dust.Play();
        style.leaves=style.Create("Courtyard drifting leaves",style.leafMaterial,14,true);
        style.oakLeaves = style.Create("Rauk oak falling leaves",style.leafMaterial,12,true);
        style.oakLeaves.GetComponent<ParticleSystemRenderer>().maxParticleSize = .027f;
        var oakEmission = style.oakLeaves.emission;
        oakEmission.enabled = false;
        var oakLeavesMain = style.oakLeaves.main;
        oakLeavesMain.gravityModifier = .18f;
        var oakSpin = style.oakLeaves.rotationOverLifetime;
        oakSpin.enabled = true;
        oakSpin.z = new ParticleSystem.MinMaxCurve(-2.5f,2.5f);
        style.oakLeaves.Play();

        var home=GameObject.Find("Garden apple tree");
        style.leafOrigin=(home!=null?home.transform.position:Vector3.zero)+new Vector3(0,3.4f,0);
        style.leaves.transform.position=style.leafOrigin;
        var wind=new GameObject("Garden orchard breeze");wind.transform.SetParent(style.transform,false);
        wind.transform.position=style.leafOrigin-Vector3.up*3.4f;
        style.gardenWind=wind.AddComponent<StableLocalFoley>();
        style.gardenRustle=Resources.Load<AudioClip>("Audio/field-rustle");style.nextGardenGust=Time.time+9;
        var shape=style.leaves.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(3.5f,1,2);
        var velocity=style.leaves.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=.15f;velocity.y=-.48f;velocity.z=.08f;
        var spin=style.leaves.rotationOverLifetime;spin.enabled=true;spin.z=new ParticleSystem.MinMaxCurve(-1.3f,1.3f);
        style.stream=GameObject.Find("Spatial stream ambience")?.GetComponent<AudioSource>();
        if(style.stream!=null)
        {
            style.oldVolume=style.stream.volume;style.oldBlend=style.stream.spatialBlend;style.oldPan=style.stream.panStereo;style.wasPlaying=style.stream.isPlaying;
            style.stream.Stop();style.stream.spatialBlend=0;style.stream.volume=0;
        }
    }
    ParticleSystem Create(string name,Material material,int cap,bool leaf)
    {
        var go=new GameObject(name,typeof(ParticleSystem));go.transform.SetParent(transform,false);
        var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.maxParticles=cap;main.startSpeed=0;main.startLifetime=leaf?7:.6f;
        main.startSize=new ParticleSystem.MinMaxCurve(leaf?.10f:.12f,leaf?.18f:.26f);
        main.startColor=leaf?new Color(.77f,.58f,.22f,.65f):new Color(.70f,.53f,.30f,.24f);
        var emission=ps.emission;emission.rateOverTime=leaf?.6f:0;
        var shape=ps.shape;shape.enabled=false;
        var fade=ps.colorOverLifetime;fade.enabled=true;
        var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});fade.color=g;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.maxParticleSize=.012f;return ps;
    }
    internal static void ShakeOakLeaves(Vector3 branch, Quaternion orientation, bool landing)
    {
        if (active == null || !active.isActiveAndEnabled || active.oakLeaves == null) return;
        int count = landing ? 3 : 2;
        for (int i = 0; i < count; i++)
        {
            var leaf = new ParticleSystem.EmitParams
            {
                position = branch + orientation*new Vector3(-.39f+.29f*i,.08f+.06f*i,-.72f),
                velocity = orientation*new Vector3(-.58f+.43f*i,landing ? .25f : .10f,-.42f),
                startLifetime = 1.50f+.14f*i,
                startSize = .26f+.025f*i,
                startColor = i == 1 ? new Color(.70f,.73f,.28f,.96f) : new Color(.98f,.74f,.24f,.96f),
                rotation = -35f+54f*i
            };
            active.oakLeaves.Emit(leaf,1);
        }
    }

    public static void Footfall(Vector3 at)
    {
        if(active==null || !active.isActiveAndEnabled || SceneLoadManager.IsTransitioning)return;
        var p=new ParticleSystem.EmitParams{position=at+Vector3.up*.07f,velocity=new Vector3(.08f,.16f,.03f)};
        active.dust.Emit(p,3);
    }
    void Update()
    {
        if(player==null)return;
        bool quiet=SceneLoadManager.IsTransitioning || CourtyardLookout.IsPresenting;
        bool near=!quiet && (player.position-leafOrigin).sqrMagnitude<22*22;
        if(near && !leaves.isPlaying)leaves.Play();
        else if(!near && leaves.isPlaying)leaves.Stop(true,ParticleSystemStopBehavior.StopEmitting);
        if(!quiet && gardenWind!=null && gardenRustle!=null && Time.time>=nextGardenGust)
        {
            gardenWind.Play(gardenRustle,.10f,.98f);
            nextGardenGust=Time.time+Mathf.Max(32,gardenRustle.length+12);
        }
        if(quiet && gardenWind!=null)gardenWind.Stop();
        if(stream==null)return;
        var point=new Vector3(47,player.position.y,Mathf.Clamp(player.position.z,-24,70));
        float d=Vector3.Distance(player.position,point);
        float gain=SoundManager.instance!=null?SoundManager.instance.effectsVolume:.55f;
        bool dialogue=DialogueBoxController.instance!=null && DialogueBoxController.instance.IsOpen;
        float target=quiet?0:(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,15,d)))*gain*(dialogue?.10f:.25f);
        stream.volume=Mathf.MoveTowards(stream.volume,target,Time.unscaledDeltaTime*.2f);
        var camera=Camera.main;if(camera!=null)stream.panStereo=Mathf.Clamp(Vector3.Dot(point-player.position,camera.transform.right)/12,-.55f,.55f);
        if(!Application.isBatchMode && target>0 && !stream.isPlaying && stream.clip!=null)stream.Play();
        if(target<=0 && stream.volume<=0 && stream.isPlaying)stream.Stop();
    }
    void OnDisable()
    {
        if(dust!=null)dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(leaves!=null)leaves.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(oakLeaves!=null)oakLeaves.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(stream!=null)stream.Stop();
        if(gardenWind!=null)gardenWind.Stop();
    }
    void OnDestroy()
    {
        if(active==this)active=null;
        if(stream!=null){stream.volume=oldVolume;stream.spatialBlend=oldBlend;stream.panStereo=oldPan;if(wasPlaying && !Application.isBatchMode)stream.Play();}
        if(dustMaterial!=null)Destroy(dustMaterial);if(leafMaterial!=null)Destroy(leafMaterial);
    }
}
