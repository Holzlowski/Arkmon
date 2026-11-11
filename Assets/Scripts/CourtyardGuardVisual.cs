using UnityEngine;
using UnityEngine.Rendering;

// Both legs share one continuous stride phase, half a cycle apart; arms counter-swing.
public sealed class CourtyardGuardVisual : MonoBehaviour
{
    internal const float FigureScale = .88f;
    internal Sprite ReviewBody { get; set; }
    private CourtyardGuard guard;
    private SpriteRenderer paper,nearLeg,farLeg,nearArm,farArm;
    private Transform rig,sword;
    private Sprite idle;
    private Camera view;
    private float walkingBlend;
    private Vector2[] nearFootOutline,farFootOutline;
    private void Start()
    {
        guard=GetComponent<CourtyardGuard>();view=Camera.main;idle=Resources.Load<Sprite>("Guard/Idle");
        var child=new GameObject("Guard illustrated figure",typeof(SpriteRenderer));child.transform.SetParent(transform,false);
        paper=child.GetComponent<SpriteRenderer>();paper.sprite=idle;
        rig=new GameObject("Guard paper walking rig",typeof(SortingGroup)).transform;rig.SetParent(paper.transform,false);
        farLeg=Part("FarLeg","Guard far leg",0,new Vector3(.075f,1.12f,.02f));
        farArm=Part("FarArm","Guard far arm",1,new Vector3(.21f,1.73f,.015f));
        nearLeg=Part("NearLeg","Guard near leg",2,new Vector3(-.075f,1.12f,0));
        var body = Part("Body","Guard upper body",3,new Vector3(0,.82f,-.01f));
        if (ReviewBody != null) body.sprite = ReviewBody;
        sword=Part("Sword","Guard sheathed sword",4,new Vector3(.2025f,1.32f,-.02f)).transform;
        nearArm=Part("NearArm","Guard near arm",5,new Vector3(-.18f,1.73f,-.03f));
        farLeg.color=new Color(.86f,.86f,.86f,1);
        nearFootOutline=nearLeg.sprite.vertices;farFootOutline=farLeg.sprite.vertices;
        rig.gameObject.SetActive(false);PaperGroundShadow.Attach(gameObject,paper);
    }
    private SpriteRenderer Part(string resource,string name,int order,Vector3 position)
    {
        var part=new GameObject(name,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        part.transform.SetParent(rig,false);part.transform.localPosition=position;
        part.sprite=Resources.Load<Sprite>("Guard/"+resource);part.sortingOrder=order;
        return part;
    }
    private void LateUpdate()
    {
        if(paper==null || guard==null)return;
        if(view==null)view=Camera.main;
        if(view!=null)paper.transform.rotation=view.transform.rotation;
        walkingBlend=Mathf.MoveTowards(walkingBlend,guard.IsMoving?1:0,Time.deltaTime*7);
        // Keep the same puppet while settling so there is no abrupt jump back to the idle drawing.
        bool walking=true;
        rig.gameObject.SetActive(walking);paper.forceRenderingOff=walking;
        paper.flipX=guard.FacingLeft;
        PresentGait(guard.TravelDistance/(1.45f*FigureScale)*Mathf.PI*2,walkingBlend,guard.FacingLeft);
        float breath=walking?0:Mathf.Sin(Time.time*1.8f)*.003f;
        paper.transform.localScale=new Vector3(1-breath*.25f,1+breath,1)*FigureScale;
    }
    private void PresentGait(float phase,float blend,bool flip)
    {
        float stride=Mathf.Sin(phase)*blend;
        nearLeg.transform.localRotation=Quaternion.Euler(0,0,stride*28);
        farLeg.transform.localRotation=Quaternion.Euler(0,0,-stride*28);
        nearArm.transform.localRotation=Quaternion.Euler(0,0,-stride*11);
        farArm.transform.localRotation=Quaternion.Euler(0,0,stride*11);
        sword.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase-.3f)*blend*2);
        const float rigScale=1.08f;
        rig.localScale=new Vector3(flip?-rigScale:rigScale,rigScale,1);
        // Lift the whole puppet, never individual legs: the hip connections remain intact.
        float lowest=Mathf.Min(FootBottom(nearLeg,nearFootOutline),FootBottom(farLeg,farFootOutline));
        rig.localPosition=Vector3.up*(.015f-lowest*rig.localScale.y);
    }
    private static float FootBottom(SpriteRenderer leg,Vector2[] outline)
    {
        float lowest=float.PositiveInfinity;
        foreach(Vector2 vertex in outline)
            lowest=Mathf.Min(lowest,(leg.transform.localRotation*(Vector3)vertex+leg.transform.localPosition).y);
        return lowest;
    }
}