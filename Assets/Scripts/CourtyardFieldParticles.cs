using UnityEngine;
using UnityEngine.Rendering;

// A small world-space seed cloud for the western lookout, never a full-screen overlay.
public sealed class CourtyardFieldParticles : MonoBehaviour
{
    ParticleSystem seeds;
    Material material;
    internal static void Install()
    {
        if(GameObject.Find("Western sunlit seeds")!=null)return;
        var meadow=GameObject.Find("Castle horizon and golden fields")?.GetComponentInChildren<MeadowVegetation>();
        var shader=Resources.Load<Shader>("CourtyardFieldSeed");
        if(meadow==null || shader==null)return;
        var root=new GameObject("Western sunlit seeds",typeof(ParticleSystem));
        root.transform.position=meadow.SurfacePoint(new Vector3(-80,0,17))+Vector3.up*7.5f;
        var style=root.AddComponent<CourtyardFieldParticles>();
        style.Build(shader);
    }
    void Build(Shader shader)
    {
        material=new Material(shader){name="Soft sunlit field seeds"};
        seeds=GetComponent<ParticleSystem>();seeds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        seeds.useAutoRandomSeed=false;seeds.randomSeed=2591;
        var main=seeds.main;main.playOnAwake=false;main.loop=true;main.duration=12;main.prewarm=true;
        main.startSpeed=0;main.startLifetime=new ParticleSystem.MinMaxCurve(8,12);
        main.startSize=new ParticleSystem.MinMaxCurve(.12f,.25f);
        main.startRotation=new ParticleSystem.MinMaxCurve(-.7f,.7f);
        main.startColor=new ParticleSystem.MinMaxGradient(new Color(2.1f,1.45f,.55f,.7f),new Color(3.0f,2.3f,1.1f,.95f));
        main.maxParticles=128;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.cullingMode=ParticleSystemCullingMode.PauseAndCatchup;
        var shape=seeds.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(28,8,36);
        var emission=seeds.emission;emission.rateOverTime=11f;
        var velocity=seeds.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=.45f;velocity.y=.07f;velocity.z=.8f;
        var noise=seeds.noise;noise.enabled=true;noise.strength=.16f;noise.frequency=.22f;noise.scrollSpeed=.12f;noise.quality=ParticleSystemNoiseQuality.Low;
        var fade=seeds.colorOverLifetime;fade.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(.8f,.75f),new GradientAlphaKey(0,1)});fade.color=gradient;
        var renderer=GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.014f;
    }
    void Update()
    {
        if(seeds==null)return;
        bool show=CourtyardLookout.IsPresenting && !SceneLoadManager.IsTransitioning;
        if(show && !seeds.isPlaying)seeds.Play();
        else if(!show && !seeds.isStopped)seeds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    void OnDisable(){if(seeds!=null)seeds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    void OnDestroy(){if(material!=null)Destroy(material);}
}
