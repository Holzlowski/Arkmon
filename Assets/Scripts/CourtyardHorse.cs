using UnityEngine;
using UnityEngine.Rendering;

// Animate a single illustration continuously; the hooves and world anchor stay fixed.
public sealed class CourtyardHorse : MonoBehaviour
{
    [SerializeField] private Sprite artwork;
    [SerializeField] private bool feeding;
    [SerializeField] private float phaseOffset;
    private Transform paper;
    private Mesh mesh;
    private Material material;
    private Vector3[] rest, animated;
    private Vector2[] coordinates;
    private Camera view;
    private StableLocalFoley foley;
    private AudioClip snort, rustle;
    private float nextSnort, nextRustle, snortAt=-100;
    private System.Random random;
    public bool IsFeeding => feeding;
    public float MotionTime { get; private set; }

    public void Configure(Sprite sprite,bool eats,float offset)
    { artwork=sprite;feeding=eats;phaseOffset=offset; }

    private void Start()
    {
        if(artwork==null){enabled=false;return;}
        view=Camera.main;random=new System.Random(feeding?218:591);
        paper=new GameObject(feeding?"Chestnut paper horse":"Dun paper horse",typeof(MeshFilter),typeof(MeshRenderer)).transform;
        paper.SetParent(transform,false);
        material=new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name="Stable paper horse" };
        material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
        material.SetFloat("_Cull",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.25f);material.EnableKeyword("_ALPHATEST_ON");
        material.SetTexture("_BaseMap",artwork.texture);material.renderQueue=3000;
        var renderer=paper.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        BuildMesh();paper.GetComponent<MeshFilter>().sharedMesh=mesh;
        PaperGroundShadow.Attach(gameObject,renderer);
        foley=gameObject.AddComponent<StableLocalFoley>();
        snort=Resources.Load<AudioClip>("Audio/Stable/horse-snort");
        rustle=Resources.Load<AudioClip>("Audio/Stable/straw-rustle");
        nextSnort=Time.time+(feeding?8:19);nextRustle=Time.time+(feeding?4:12);
    }

    private void BuildMesh()
    {
        const int columns=36,rows=32;
        int count=(columns+1)*(rows+1);
        rest=new Vector3[count];animated=new Vector3[count];coordinates=new Vector2[count];
        var uv=new Vector2[count];var indices=new int[columns*rows*6];
        Vector2 size=artwork.rect.size/artwork.pixelsPerUnit;
        Vector2 pivot=artwork.pivot/artwork.rect.size;
        for(int y=0;y<=rows;y++)
        for(int x=0;x<=columns;x++)
        {
            int k=y*(columns+1)+x;var p=new Vector2(x/(float)columns,y/(float)rows);
            coordinates[k]=p;rest[k]=Vector2.Scale(p-pivot,size);
            Vector2 pixel=artwork.rect.position+Vector2.Scale(p,artwork.rect.size);
            uv[k]=new Vector2(pixel.x/artwork.texture.width,pixel.y/artwork.texture.height);
            if(x==columns||y==rows)continue;
            int t=(y*columns+x)*6;
            indices[t]=k;indices[t+1]=k+columns+1;indices[t+2]=k+columns+2;
            indices[t+3]=k;indices[t+4]=k+columns+2;indices[t+5]=k+1;
        }
        mesh=new Mesh { name="Stable horse breathing mesh" };mesh.MarkDynamic();
        mesh.vertices=rest;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateBounds();
    }

    private void LateUpdate()
    {
        if(paper==null)return;
        if(view==null)view=Camera.main;
        if(view!=null)paper.rotation=view.transform.rotation;
        MotionTime=Time.time+phaseOffset;
        float t=MotionTime;
        float breath=Mathf.Sin(t*1.35f)*.009f;
        float neck=Mathf.Sin(t*(feeding?.8f:.49f))*(feeding?.035f:.016f);
        float tailWindow=Pulse(t,7.8f,1.8f);
        float tail=Mathf.Sin(t*5.2f)*tailWindow*.075f;
        float ear=Mathf.Sin(t*15)*Pulse(t+2.7f,9.4f,.48f)*.018f;
        float snortMotion=Mathf.Sin(Mathf.Clamp01((Time.time-snortAt)/.65f)*Mathf.PI)*.015f;
        for(int i=0;i<rest.Length;i++)
        {
            var p=coordinates[i];var v=rest[i];
            float torso=Smooth(.27f,.55f,p.y)*(1-Smooth(.61f,.76f,p.x));
            // Tail stays attached at its root; legs are outside this narrow left strip.
            float tailWeight=(1-Smooth(.12f,.225f,p.x))*Smooth(.14f,.25f,p.y)*(1-Smooth(.68f,.80f,p.y));
            float neckWeight=Smooth(.60f,.81f,p.x)*Smooth(.29f,.53f,p.y);
            float earHeight=feeding?.56f:.82f;
            float earWeight=Smooth(.73f,.87f,p.x)*Smooth(earHeight,earHeight+.13f,p.y);
            v.y+=breath*torso+(neck+snortMotion)*neckWeight;
            v.x+=tail*tailWeight+ear*earWeight;
            animated[i]=v;
        }
        mesh.vertices=animated;mesh.RecalculateBounds();
        if(Time.time>=nextSnort)
        {
            foley.Play(snort,.26f,feeding?.96f:1.02f);snortAt=Time.time;
            nextSnort=Time.time+24+(float)random.NextDouble()*17;
        }
        if(Time.time>=nextRustle)
        {
            foley.Play(rustle,feeding?.11f:.065f,.85f+(float)random.NextDouble()*.12f);
            nextRustle=Time.time+(feeding?7:15)+(float)random.NextDouble()*9;
        }
    }
    private static float Smooth(float a,float b,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
    private static float Pulse(float t,float period,float duration)
    {
        float phase=Mathf.Repeat(t,period);
        return phase<duration?Mathf.Sin(phase/duration*Mathf.PI):0;
    }
    private void OnDisable(){if(foley!=null)foley.Stop();}
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}