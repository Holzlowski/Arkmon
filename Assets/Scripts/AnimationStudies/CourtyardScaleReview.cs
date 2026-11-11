using TMPro;
using UnityEngine;

public sealed class CourtyardScaleReview : MonoBehaviour
{
    [Tooltip("Optionaler Sprite zum Vergleich mit den fünf Hoffiguren.")]
    [SerializeField] private Sprite comparisonSprite;
    [Min(.05f)]
    [SerializeField] private float comparisonScale = 1;
    [Tooltip("Korrektur der Sohlenhöhe für Bilder mit transparentem Rand.")]
    [SerializeField] private float comparisonFootOffset;

    [SerializeField] private Sprite guardBodyVariant;

    const float ChartLeft = -5.18f;
    const float ChartRight = 4.95f;
    Camera reviewCamera;
    PaperPlayerWalkRig elianRig;
    Sprite mentorArt;
    Material gridInk, baselineInk;
    float leftEdge;
    float rightEdge;
    float centerX;

    private void Awake()
    {
        bool compareGuards = guardBodyVariant != null;
        leftEdge = compareGuards ? -2.35f : ChartLeft;
        rightEdge = compareGuards ? 2.35f : (comparisonSprite != null ? 6.70f : ChartRight);
        centerX = (leftEdge + rightEdge)*.5f;
        BuildCamera();
        var ground = Anchor("Bodenlinie",Vector3.zero).gameObject;
        ground.tag = "Ground";
        var floor = ground.AddComponent<BoxCollider>();
        floor.center = new Vector3(0,-.08f,0);
        floor.size = new Vector3(20,.16f,6);

        gridInk = Ink(new Color(.67f,.70f,.67f));
        baselineInk = Ink(new Color(.23f,.32f,.30f));
        if (compareGuards)
        {
            BuildRuler("ARKMON  /  BURGWACHE","Gleiche Pose, gleiche Größe",5);
            AddGuard("Burgwache · Original",-1.02f);
            AddGuard("Burgwache · Variante",1.02f).ReviewBody = guardBodyVariant;
            Caption("A · Original",-1.02f,"Aktuelle Spielfigur");
            Caption("B · Variante",1.02f,"Schmalere Wangen und Kiefer");
            FitCamera();
            return;
        }

        BuildRuler("ARKMON  /  GRÖSSENPROBE","Gleiche Kamera · gemeinsame Bodenlinie · Unity-Einheiten",6);

        var elian = Anchor("Elian",new Vector3(-4.15f,0,0));
        var original = elian.gameObject.AddComponent<SpriteRenderer>();
        original.sprite = Resources.Load<Sprite>("Prince-Family");
        elianRig = new PaperPlayerWalkRig(elian,original);
        elianRig.Present(true,true,false,0,false);

        AddGuard("Burgwache",-2.45f);

        var mentor = Anchor("Arkanmeisterin",new Vector3(-.62f,0,0));
        var texture = Resources.Load<Texture2D>("Arkanmeisterin poses");
        mentorArt = ArkanmeisterinArrival.CreateFigureSprite(texture,false);
        mentor.gameObject.AddComponent<SpriteRenderer>().sprite = mentorArt;

        var team = Anchor("Kutschgespann",Vector3.zero).gameObject.AddComponent<ArrivalCarriage>();
        team.BuildScaleReference(new Vector3(1.63f,0,0),4.05f);

        Caption("Elian",-4.15f,"stehend");
        Caption("Burgwache",-2.45f,"stehend");
        Caption("Arkanmeisterin",-.62f,"stehend");
        Caption("Zugpferd",1.63f,"am Boden");
        Caption("Kutscher",4.05f,"auf dem Kutschbock");

        if(comparisonSprite != null)
        {
            var candidate = Anchor("Vergleichsfigur",new Vector3(5.79f,
                comparisonFootOffset-comparisonSprite.bounds.min.y*comparisonScale,0));
            candidate.localScale = Vector3.one*comparisonScale;
            candidate.gameObject.AddComponent<SpriteRenderer>().sprite = comparisonSprite;
            Caption("Vergleichsfigur",5.79f,"eigener Sprite");
        }
        FitCamera();
    }

    private CourtyardGuardVisual AddGuard(string name,float x)
    {
        var guard = Anchor(name,new Vector3(x,0,0)).gameObject;
        guard.AddComponent<CourtyardGuard>();
        return guard.AddComponent<CourtyardGuardVisual>();
    }

    private void BuildCamera()
    {
        var cameraObject = Anchor("Größenkamera",new Vector3(centerX,1.41f,-10)).gameObject;
        cameraObject.tag = "MainCamera";
        reviewCamera = cameraObject.AddComponent<Camera>();
        reviewCamera.orthographic = true;
        reviewCamera.clearFlags = CameraClearFlags.SolidColor;
        reviewCamera.backgroundColor = new Color(.90f,.90f,.85f);
        reviewCamera.farClipPlane = 25;
        var light = Anchor("Studiolicht",Vector3.zero).gameObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.transform.rotation = Quaternion.Euler(30,-35,0);
    }

    private void LateUpdate() => FitCamera();

    private void FitCamera()
    {
        float width = rightEdge - leftEdge + .90f;
        float minimumSize = guardBodyVariant != null ? 2.12f : 2.56f;
        reviewCamera.orthographicSize = Mathf.Max(minimumSize,width/(2*Mathf.Max(.1f,reviewCamera.aspect)));
    }

    private void BuildRuler(string title,string subtitle,int halfSteps)
    {
        float top = halfSteps*.5f;
        Label(title,new Vector3(centerX,top+.65f,-1),3.3f,8);
        Label(subtitle,new Vector3(centerX,top+.30f,-1),1.65f,9);
        for(int i=0;i<=halfSteps;i++)
        {
            float y=i*.5f;
            Line("Maßlinie",new Vector3(leftEdge,y,.48f),new Vector3(rightEdge,y,.48f),
                i==0?.022f:.011f,i==0?baselineInk:gridInk);
            if(i%2==0)Label((i/2).ToString(),new Vector3(leftEdge-.16f,y,-.4f),1.45f,.30f);
        }
        Line("Maßstab",new Vector3(leftEdge,0,.48f),new Vector3(leftEdge,top,.48f),.014f,baselineInk);
    }

    private void Caption(string name,float x,string pose)
    {
        Label(name,new Vector3(x,-.28f,-1),1.85f,1.76f);
        Label(pose,new Vector3(x,-.51f,-1),1.32f,1.76f);
    }

    private void Label(string value,Vector3 at,float size,float width)
    {
        var label=Anchor(value,at).gameObject.AddComponent<TextMeshPro>();
        label.font=PaperUIStyle.Load()?.font ?? TMP_Settings.defaultFontAsset;
        label.fontSize=size;
        label.color=new Color(.15f,.24f,.23f);
        label.alignment=TextAlignmentOptions.Center;
        label.textWrappingMode=TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta=new Vector2(width,.32f);
        label.text=value;
    }

    private void Line(string name,Vector3 a,Vector3 b,float width,Material material)
    {
        var line=Anchor(name,Vector3.zero).gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace=false;
        line.sharedMaterial=material;
        line.widthMultiplier=width;
        line.positionCount=2;
        line.SetPosition(0,a);
        line.SetPosition(1,b);
        line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private Transform Anchor(string name,Vector3 position)
    {
        var anchor=new GameObject(name).transform;
        anchor.SetParent(transform,false);
        anchor.localPosition=position;
        return anchor;
    }

    private static Material Ink(Color color)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor",color);
        return material;
    }

    private void OnDestroy()
    {
        elianRig?.Dispose();
        Destroy(mentorArt);
        Destroy(gridInk);
        Destroy(baselineInk);
    }
}
