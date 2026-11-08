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
        root.transform.position=meadow.SurfacePoint(new Vector3(-70,0,7))+Vector3.up*4.2f;
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
        main.startSize=new ParticleSystem.MinMaxCurve(.065f,.13f);
        main.startRotation=new ParticleSystem.MinMaxCurve(-.7f,.7f);
        main.startColor=new ParticleSystem.MinMaxGradient(new Color(.92f,.78f,.48f,.5f),new Color(1,.93f,.72f,.7f));
        main.maxParticles=24;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.cullingMode=ParticleSystemCullingMode.PauseAndCatchup;
        var shape=seeds.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(22,5,16);
        var emission=seeds.emission;emission.rateOverTime=1.5f;
        var velocity=seeds.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=.20f;velocity.y=.045f;velocity.z=.09f;
        var noise=seeds.noise;noise.enabled=true;noise.strength=.16f;noise.frequency=.22f;noise.scrollSpeed=.12f;noise.quality=ParticleSystemNoiseQuality.Low;
        var fade=seeds.colorOverLifetime;fade.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(.8f,.75f),new GradientAlphaKey(0,1)});fade.color=gradient;
        var renderer=GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.008f;
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
