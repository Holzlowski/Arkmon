using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// An isolated, seekable study. It shares the real painted rig but owns no quest state.
public sealed class ElianBucketMotionStudy : MonoBehaviour
{
    public const float Duration = 14f;
    public bool autoPlay = true;
    public bool mirrored;
    [Range(.1f, 1.5f)] public float speed = 1;
    [Range(0, Duration)] public float playhead;
    public string Phase { get; private set; }
    public bool IsAttached { get; private set; }
    public Camera StudyCamera { get; private set; }
    public Vector3 HandPosition => rig.HandPosition;
    public Vector3 HandlePosition => bucket.transform.position;
    public float WaterAmount { get; private set; }
    public float SupportWeight { get; private set; }
    public Vector3 SupportContactPosition { get; private set; }
    public Vector3 SupportHandPosition => rig.SupportHandPosition;
    Transform stage, actor, water;
    SpriteRenderer bucket;
    Sprite bucketSprite;
    Material paper, contents, streamMaterial;
    PaperPlayerGardenRig rig;
    GardenWaterEffect stream;
    TMP_Text caption, direction;
    readonly List<Mesh> meshes = new List<Mesh>();
    SpriteRenderer nearFoot,farFoot; Vector3[] nearSole,farSole;
    readonly Vector3 start = new Vector3(-2.1f, 0, 0);
    const float RigScale = 2f / 2.07f;
    Vector3 groundedHandle;

    void Awake()
    {
        paper = PaperPlayerWalkRig.CreatePaperMaterial();
        paper.SetTexture("_BaseMap", Texture2D.whiteTexture);
        stage = new GameObject("Quiet animation stage").transform;
        stage.SetParent(transform, false);
        var cameraObject = new GameObject("Study camera", typeof(Camera));
        cameraObject.transform.SetParent(transform, false);
        StudyCamera = cameraObject.GetComponent<Camera>();
        StudyCamera.tag = "MainCamera";
        StudyCamera.orthographic = true; StudyCamera.orthographicSize = 1.95f;
        StudyCamera.transform.position = new Vector3(0, 1.45f, -10);
        StudyCamera.clearFlags = CameraClearFlags.SolidColor;
        StudyCamera.backgroundColor = new Color(.83f, .81f, .75f);
        StudyCamera.nearClipPlane = .1f; StudyCamera.farClipPlane = 30;
        Polygon("Ground", new[]{new Vector2(-8,-2),new Vector2(8,-2),new Vector2(8,0),new Vector2(-8,0)}, new Color(.71f,.70f,.63f), -30);
        Polygon("Ground contact", new[]{new Vector2(-8,-.008f),new Vector2(8,-.008f),new Vector2(8,.008f),new Vector2(-8,.008f)}, new Color(.50f,.49f,.44f), -29);
        Ellipse("Foot shadow", start + new Vector3(0,.025f,0), .35f,.035f, new Color(.48f,.47f,.41f,.35f), -20, out var shadow);
        shadow.name = "Study foot shadow";
        CreateTrough();
        actor = new GameObject("Elian study actor").transform; actor.SetParent(stage,false);
        var original = actor.gameObject.AddComponent<SpriteRenderer>(); original.sharedMaterial = paper;
        original.sprite = Resources.Load<Sprite>("Elian-Walk-Family");
        rig = new PaperPlayerGardenRig(actor, original);
        var source = Resources.Load<Sprite>("StableCare/Bucket");
        bucketSprite = Sprite.Create(source.texture,source.rect,new Vector2(.5f,.94f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
        bucketSprite.name = "Study bucket handle";
        var item = new GameObject("Study bucket",typeof(SpriteRenderer)); item.transform.SetParent(stage,false);
        bucket = item.GetComponent<SpriteRenderer>(); bucket.sprite = bucketSprite;
        contents = new Material(Resources.Load<Shader>("CourtyardBucketContents")); bucket.sharedMaterial = contents;
        groundedHandle = start + new Vector3(.46f,-bucketSprite.bounds.min.y * RigScale,-.015f);
        var effect = new GameObject("Study pour stream"); effect.transform.SetParent(stage,false);
        stream = effect.AddComponent<GardenWaterEffect>(); stream.SetSingleStream();
        streamMaterial = PaperPlayerWalkRig.CreatePaperMaterial();
        streamMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);streamMaterial.SetColor("_BaseColor",new Color(.61f,.84f,.93f));
        foreach(var renderer in effect.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=streamMaterial;renderer.sortingOrder=12;}
        Label("ELIAN / BEWEGUNGSPROBE", new Vector3(-3.1f,3.14f,-1), .22f);
        caption=Label("", new Vector3(-3.1f,2.82f,-1), .18f);
        direction=Label("", new Vector3(-3.1f,2.54f,-1), .14f);
        nearFoot=actor.Find("Elian garden paper rig/Near garden leg").GetComponent<SpriteRenderer>();
        farFoot=actor.Find("Elian garden paper rig/Far garden leg").GetComponent<SpriteRenderer>();
        nearSole=VisibleSole(nearFoot.sprite);farSole=VisibleSole(farFoot.sprite);
        Evaluate(0);
    }
    static float Ease(float t,float from,float to) => Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,t));
    public void Evaluate(float time)
    {
        playhead = Mathf.Clamp(time,0,Duration);
        float t = playhead;
        stage.localScale = new Vector3(mirrored ? -1 : 1,1,1);
        float travel = Ease(t,3.8f,6.4f);
        actor.localPosition = Vector3.Lerp(start,Vector3.zero,travel);
        stage.Find("Study foot shadow").localPosition = actor.localPosition+new Vector3(0,.025f,0);
        bool attached = t >= 2.2f;
        IsAttached = attached;
        if(!attached)
        {
            rig.Hide();
            bucket.transform.SetParent(stage,false);
            bucket.transform.localScale = Vector3.one*RigScale;
            bucket.transform.localRotation = Quaternion.identity;
            bucket.transform.localPosition = groundedHandle;
        }
        float bend = 1.55f * (t<2.2f ? Ease(t,1,2.2f) : 1-Ease(t,2.2f,3.3f));
        float lift = Ease(t,6.9f,7.9f)*(1-Ease(t,11.7f,12.8f));
        float tip = Ease(t,8.1f,8.9f)*(1-Ease(t,10.6f,11.5f));
        if(t>=6.9f)bend=.65f*lift*(1-tip);
        float walkWeight = Ease(t,3.8f,4.15f)*(1-Ease(t,6.05f,6.4f));
        float phase = travel*2.1f/2.4f*Mathf.PI*2;
        Vector3 contact=actor.TransformPoint(groundedHandle-start);
        rig.Present(true,false,walkWeight>0,phase,bend,70*tip,Ease(t,1,2.2f),bucket,attached,contact,weight:walkWeight,support:lift,freeArmSwing:-.07f*Mathf.Sin(phase)*walkWeight,
            liftedHandle:new Vector3(Mathf.Lerp(.32f,.46f,tip),Mathf.Lerp(1.28f,1.36f,tip),-.015f),
            lowerSupport:Vector3.Lerp(new Vector3(-.05f,-.43f,0),PaperPlayerGardenRig.BucketSupportPoint,tip));
        PlaceVisibleSole(nearFoot,nearSole,phase,walkWeight);PlaceVisibleSole(farFoot,farSole,phase+Mathf.PI,walkWeight);
        SupportWeight=lift;
        SupportContactPosition=bucket.transform.TransformPoint(Vector3.Lerp(new Vector3(-.05f,-.43f,0),PaperPlayerGardenRig.BucketSupportPoint,tip));
        WaterAmount=1-Ease(t,8.95f,10.55f);
        contents.SetFloat("_Filled",WaterAmount);
        water.localScale=new Vector3(1,Mathf.Lerp(.2f,1,1-WaterAmount),1);
        water.localPosition=new Vector3(0,Mathf.Lerp(.40f,.60f,1-WaterAmount),0);
        var sourcePoint=bucket.transform.TransformPoint(new Vector3(.16f,-.22f,0));
        stream.Present(sourcePoint,stage.TransformPoint(new Vector3(.91f,Mathf.Lerp(.40f,.60f,1-WaterAmount),0)),t,t>=8.95f && t<10.55f?1:0);
        Phase=t<1?"Ausgangsposition":t<2.2f?"1 / Zum Henkel greifen":t<3.3f?"2 / Eimer anheben":t<3.8f?"Griff halten":t<6.4f?"3 / Eimer tragen":t<6.9f?"Anhalten":t<7.9f?"4 / Zweite Hand und Anheben":t<8.9f?"5 / Gemeinsam kippen":t<10.6f?"6 / In den Trog ausgießen":t<11.7f?"7 / Eimer aufrichten":t<12.8f?"8 / Zurück in die Tragehaltung":"Endposition";
        caption.text=Phase;
        direction.text=$"{(mirrored?"Nach links":"Nach rechts")}   |   {playhead:0.00} / {Duration:0} s   |   Voller Eimer als Ausgangspunkt";
    }
    void Update()
    {
        var keys=Keyboard.current;
        if(keys!=null)
        {
            if(keys.spaceKey.wasPressedThisFrame)autoPlay=!autoPlay;
            if(keys.rKey.wasPressedThisFrame)playhead=0;
            if(keys.fKey.wasPressedThisFrame)mirrored=!mirrored;
            if(keys.leftArrowKey.wasPressedThisFrame){autoPlay=false;playhead-=1f/24;}
            if(keys.rightArrowKey.wasPressedThisFrame){autoPlay=false;playhead+=1f/24;}
        }
        if(autoPlay){playhead+=Time.unscaledDeltaTime*speed;if(playhead>Duration)playhead=0;}
        Evaluate(playhead);
    }
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(16,Screen.height-84,Mathf.Min(Screen.width-32,700),80),GUI.skin.box);
        GUILayout.BeginHorizontal();
        if(GUILayout.Button(autoPlay?"Pause":"Abspielen",GUILayout.Width(100)))autoPlay=!autoPlay;
        if(GUILayout.Button("Neustart",GUILayout.Width(90)))playhead=0;
        if(GUILayout.Button(mirrored?"Richtung: links":"Richtung: rechts",GUILayout.Width(135)))mirrored=!mirrored;
        if(GUILayout.Button(speed<.5f?"Tempo: 0,25×":"Tempo: 1×",GUILayout.Width(110)))speed=speed<.5f?1:.25f;
        GUILayout.EndHorizontal();
        float selected=GUILayout.HorizontalSlider(playhead,0,Duration);
        if(Mathf.Abs(selected-playhead)>.001f){autoPlay=false;playhead=selected;Evaluate(playhead);}
        GUILayout.Label("Leertaste: Pause  ·  ← / →: Einzelbild  ·  F: Richtung  ·  R: Neustart");
        GUILayout.EndArea();
    }
    TMP_Text Label(string text,Vector3 position,float size)
    {
        var go=new GameObject("Study caption",typeof(TextMeshPro));go.transform.SetParent(transform,false);go.transform.localPosition=position;
        var label=go.GetComponent<TextMeshPro>();label.font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        label.text=text;label.fontSize=size*10;label.color=new Color(.20f,.23f,.22f);label.alignment=TextAlignmentOptions.TopLeft;
        label.rectTransform.sizeDelta=new Vector2(7,.35f);label.rectTransform.pivot=new Vector2(0,1);label.GetComponent<MeshRenderer>().sortingOrder=50;
        return label;
    }
    void CreateTrough()
    {
        var dark=new Color(.25f,.20f,.15f);var wood=new Color(.49f,.36f,.23f);
        Polygon("Trough back",new[]{new Vector2(.55f,.18f),new Vector2(2.6f,.18f),new Vector2(2.68f,.79f),new Vector2(.48f,.79f)},dark,-10);
        var w=Polygon("Trough rising water",new[]{new Vector2(.61f,0),new Vector2(2.53f,0),new Vector2(2.53f,.1f),new Vector2(.61f,.1f)},new Color(.36f,.58f,.62f),-9);
        water=w.transform;
        Polygon("Trough front outline",new[]{new Vector2(.62f,.12f),new Vector2(2.52f,.12f),new Vector2(2.62f,.51f),new Vector2(.52f,.51f)},dark,15);
        Polygon("Trough front wood",new[]{new Vector2(.66f,.16f),new Vector2(2.48f,.16f),new Vector2(2.56f,.47f),new Vector2(.58f,.47f)},wood,16);
        Polygon("Trough wood grain",new[]{new Vector2(.65f,.30f),new Vector2(2.49f,.30f),new Vector2(2.49f,.315f),new Vector2(.65f,.315f)},new Color(.37f,.27f,.18f),17);
        foreach(float x in new[]{.85f,2.23f})
            Polygon("Trough leg",new[]{new Vector2(x,0),new Vector2(x+.16f,0),new Vector2(x+.16f,.18f),new Vector2(x,.18f)},dark,14);
    }
    GameObject Polygon(string name,Vector2[] points,Color color,int order)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(stage,false);
        var vertices=new Vector3[points.Length];for(int i=0;i<points.Length;i++)vertices[i]=points[i];
        var indices=new int[(points.Length-2)*3];for(int i=0;i<points.Length-2;i++){indices[i*3]=0;indices[i*3+1]=i+1;indices[i*3+2]=i+2;}
        var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=indices;mesh.uv=new Vector2[points.Length];mesh.RecalculateBounds();meshes.Add(mesh);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=paper;renderer.sortingOrder=order;
        var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",color);renderer.SetPropertyBlock(properties);return go;
    }
    void Ellipse(string name,Vector3 at,float width,float height,Color color,int order,out Transform result)
    {
        var points=new Vector2[32];for(int i=0;i<points.Length;i++){float angle=i*Mathf.PI*2/points.Length;points[i]=new Vector2(Mathf.Cos(angle)*width,Mathf.Sin(angle)*height);}
        result=Polygon(name,points,color,order).transform;result.localPosition=at;
    }
    // The quad includes transparent atlas margins. Ground the visible boot outline instead.
    static Vector3[] VisibleSole(Sprite sprite)
    {
        var source=sprite.texture;var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
        var before=RenderTexture.active;Graphics.Blit(source,target);RenderTexture.active=target;
        var readable=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
        readable.ReadPixels(new Rect(0,0,source.width,source.height),0,0);readable.Apply();
        var pixels=readable.GetPixels32();RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);Destroy(readable);
        int width=(int)sprite.rect.width,height=(int)sprite.rect.height;var visited=new bool[width*height];var largest=new List<int>();
        bool Opaque(int at)=>pixels[((int)sprite.rect.y+at/width)*source.width+(int)sprite.rect.x+at%width].a>230;
        for(int at=0;at<visited.Length;at++)
        {
            if(visited[at]||!Opaque(at))continue;
            var part=new List<int>{at};visited[at]=true;
            for(int k=0;k<part.Count;k++)
            {
                int p=part[k],x=p%width,y=p/width;
                foreach(var d in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                {
                    int nx=x+d.x,ny=y+d.y,n=ny*width+nx;
                    if(nx<0||nx>=width||ny<0||ny>=height||visited[n]||!Opaque(n))continue;
                    visited[n]=true;part.Add(n);
                }
            }
            if(part.Count>largest.Count)largest=part;
        }
        var set=new HashSet<int>(largest);var outline=new List<Vector3>();
        foreach(int p in largest)
            if(!set.Contains(p-1)||!set.Contains(p+1)||!set.Contains(p-width)||!set.Contains(p+width))
                outline.Add((new Vector2(p%width+.5f,p/width+.5f)-sprite.pivot)/sprite.pixelsPerUnit);
        return outline.ToArray();
    }
    static void PlaceVisibleSole(SpriteRenderer foot,Vector3[] outline,float phase,float weight)
    {
        float lowest=float.PositiveInfinity;
        foreach(var point in outline)lowest=Mathf.Min(lowest,foot.transform.TransformPoint(point).y);
        float floor=.012f+.055f*Mathf.Max(0,Mathf.Sin(phase))*weight*RigScale;
        foot.transform.position+=Vector3.up*(floor-lowest);
    }
    void OnDestroy()
    {
        rig?.Dispose();foreach(var mesh in meshes)Destroy(mesh);Destroy(bucketSprite);Destroy(contents);Destroy(paper);Destroy(streamMaterial);
    }
}