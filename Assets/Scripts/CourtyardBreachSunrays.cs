using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bounded illustrated light shafts, not a full-screen fog or a volumetric renderer.
public sealed class CourtyardBreachSunrays : MonoBehaviour
{
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<MeshRenderer> rays = new List<MeshRenderer>();
    readonly List<Vector4> beamStarts=new List<Vector4>();
    readonly List<Vector4> beamEnds=new List<Vector4>();
    Material sunlight, dustMaterial;
    ParticleSystem dust;
    Transform player;
    Camera view;
    private float presentationGlow=1;
    public float Visibility { get; private set; }
    internal static void Install()
    {
        if(GameObject.Find("Breach golden sunrays")!=null || GameObject.Find("Illustrated west breach")==null)return;
        var shader=Resources.Load<Shader>("CourtyardSunShaft");if(shader==null)return;
        var style=new GameObject("Breach golden sunrays").AddComponent<CourtyardBreachSunrays>();
        style.sunlight=new Material(shader){name="Golden breach light"};
        style.player=GameObject.FindWithTag("Player")?.transform;style.view=Camera.main;
        // Three depth banks with three shafts each: steep descending light, not a flat stripe.
        // Authored length/width pairs avoid a repeating staircase pattern or random changes on reload.
        var proportions=new[]{new Vector2(.58f,1.25f),new Vector2(1f,.55f),new Vector2(.78f,.9f),
            new Vector2(.90f,.65f),new Vector2(.52f,1.4f),new Vector2(.74f,.8f),
            new Vector2(.63f,.5f),new Vector2(.85f,1.1f),new Vector2(1f,.7f)};
        for(int bank=0;bank<3;bank++)
            for(int shaft=0;shaft<3;shaft++)
            {
                float z=5.0f+bank*1.65f+shaft*.42f;
                var start=new Vector3(-43.3f+shaft*.20f,7.1f+shaft*.48f+bank*.18f,z);
                var end=new Vector3(-38.7f+shaft*.66f+bank*.18f,.85f+shaft*.22f,z+.65f+bank*.48f);
                var proportion=proportions[bank*3+shaft];
                // Enlarge the authored beam, but keep its lower endpoint above the ground.
                end=Vector3.LerpUnclamped(start,end,proportion.x*1.2f);
                end.y=Mathf.Max(.4f,end.y);
                style.Ray(start,end,(.12f+shaft*.025f)*proportion.y*1.35f,(.48f+shaft*.10f)*proportion.y*1.35f,.78f+bank*.10f);
            }
        style.Dust();
    }
    void Ray(Vector3 start,Vector3 end,float narrow,float wide,float strength)
    {
        // Broad glow, a bright fine core and an angled inner sheet remain one draw per shaft.
        beamStarts.Add(new Vector4(start.x,start.y,start.z,narrow));
        beamEnds.Add(new Vector4(end.x,end.y,end.z,wide));
        var axis=(end-start).normalized;
        var side=Vector3.Cross(axis,Vector3.up).normalized;
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
        for(int layer=0;layer<3;layer++)
        {
            float width=layer==0?1:layer==1?.34f:.64f;
            var across=layer==2?Quaternion.AngleAxis(64,axis)*side:side;
            int first=vertices.Count;
            vertices.AddRange(new[]{start-across*narrow*width,start+across*narrow*width,end+across*wide*width,end-across*wide*width});
            uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
            var color=new Color(1,1,1,strength*(layer==0?.72f:layer==1?.82f:.40f));
            for(int i=0;i<4;i++)colors.Add(color);
            triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
        }
        var mesh=new Mesh{name="Layered golden breach shaft"};meshes.Add(mesh);
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        var go=new GameObject("Golden sunlight through breach",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();
        r.sharedMaterial=sunlight;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;rays.Add(r);
        var properties=new MaterialPropertyBlock();properties.SetVector("_BeamOrigin",start);r.SetPropertyBlock(properties);
    }
    void Dust()
    {
        var shader=Resources.Load<Shader>("CourtyardSunDust");if(shader==null)return;
        dustMaterial=new Material(shader){name="Breach floating golden motes"};
        dustMaterial.SetVectorArray("_BeamStarts",beamStarts);
        dustMaterial.SetVectorArray("_BeamEnds",beamEnds);
        var go=new GameObject("Breach sunlit motes",typeof(ParticleSystem));go.transform.SetParent(transform,false);
        go.transform.position=new Vector3(-40.5f,4.1f,7.7f);dust=go.GetComponent<ParticleSystem>();
        dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);dust.useAutoRandomSeed=false;dust.randomSeed=4307;
        var main=dust.main;main.playOnAwake=false;main.loop=true;main.duration=10;main.prewarm=true;main.maxParticles=64;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(6,10);
        main.startSpeed=.025f;main.startSize=new ParticleSystem.MinMaxCurve(.035f,.065f);
        main.startColor=new Color(1.6f,.94f,.26f,.9f);
        var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(4.7f,5.1f,4.8f);
        var emission=dust.emission;emission.rateOverTime=7;
        var velocity=dust.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=.06f;velocity.y=.025f;velocity.z=.025f;
        var noise=dust.noise;noise.enabled=true;noise.strength=.10f;noise.frequency=.35f;noise.scrollSpeed=.12f;noise.damping=true;
        var fade=dust.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(1,.7f),new GradientAlphaKey(0,1)});fade.color=gradient;
        var r=dust.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=dustMaterial;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
    }
    void LateUpdate()
    {
        if(view==null)view=Camera.main;
        float distance=player!=null?Vector3.Distance(player.position,new Vector3(-40,player.position.y,7)):100;
        float proximity=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(22,36,distance));
        // Once outside, the beams remain behind the camera; no luminous screen wash over Aurenfeld.
        float inside=view!=null?Mathf.SmoothStep(0,1,Mathf.InverseLerp(-48,-43.8f,view.transform.position.x)):0;
        Visibility=SceneLoadManager.IsTransitioning?0:proximity*inside;
        presentationGlow=Mathf.MoveTowards(presentationGlow,CourtyardLookout.IsPresenting?1.15f:1,Time.unscaledDeltaTime*.2f);
        sunlight.SetColor("_BaseColor",new Color(1.3f,.67f,.14f,.30f*Visibility*presentationGlow));
        foreach(var r in rays)r.enabled=Visibility>.001f;
        if(dust!=null)
        {
            dustMaterial.SetFloat("_Visibility",Visibility);
            if(Visibility>.05f && !dust.isPlaying)dust.Play();
            else if(Visibility<=.05f && !dust.isStopped)dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    void OnDisable()
    {
        Visibility=0;foreach(var r in rays)if(r!=null)r.enabled=false;
        if(dust!=null)dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    void OnDestroy()
    {
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        if(sunlight!=null)Destroy(sunlight);if(dustMaterial!=null)Destroy(dustMaterial);
    }
}
