using UnityEngine;

// Scene-owned draw well and shared filling point for courtyard tools.
public sealed class CourtyardWell : MonoBehaviour, IInteractableAsPlayer
{
    Material waterMaterial;
    Transform drawingBucket,drawWater,rope,crank;
    public bool IsDrawing => drawingBucket!=null && drawingBucket.gameObject.activeSelf;
    public Vector3 PourSource => drawingBucket.TransformPoint(new Vector3(0,.14f,-.24f));
    public Vector3 ChannelInlet => transform.TransformPoint(new Vector3(0,1.16f,-.28f));
    public Vector3 ChannelOutlet => transform.TransformPoint(new Vector3(.8f,1.02f,-1.70f));
    public Vector3 ReceivingGrip => ChannelOutlet - Vector3.up * .03f + Vector3.back * .12f;
    public float Flow { get; private set; }
    public void PresentDraw(float progress)
    {
        drawingBucket.gameObject.SetActive(true);
        float raised = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.50f,progress));
        float lowered = Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.18f,progress));
        float height = Mathf.Lerp(Mathf.Lerp(.42f,-.28f,lowered),1.30f-.14f,raised);
        height=Mathf.Lerp(height,.42f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,1.15f,progress)));
        float tip = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.50f,.62f,progress))
            * (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.87f,.98f,progress)));
        var rotation=Quaternion.Euler(-65*tip,0,0);
        // The bucket lip rests on a fixed cradle above the inlet, rather than floating forward.
        var lip=new Vector3(0,.14f,-.24f);
        drawingBucket.localRotation=rotation;
        drawingBucket.localPosition=new Vector3(0,height,0)+lip-rotation*lip;
        drawWater.gameObject.SetActive(progress>.18f && progress<.87f);
        var a=transform.TransformPoint(new Vector3(0,1.85f,0));var b=drawingBucket.TransformPoint(Vector3.up*.52f);
        rope.position=(a+b)*.5f;rope.rotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        rope.localScale=new Vector3(.035f,Vector3.Distance(a,b)*.5f,.035f);
        // Axle, crank arm and grip turn as one assembly around the actual horizontal axle.
        crank.localRotation=Quaternion.Euler((height-.42f)/.09f*Mathf.Rad2Deg,0,0);
        Flow=progress>=.62f && progress<.87f ? Mathf.Clamp01((progress-.62f)/.04f)*Mathf.Clamp01((.87f-progress)/.04f) : 0;
    }
    public void ResetDraw()
    {
        Flow=0;
        if(drawingBucket!=null)drawingBucket.gameObject.SetActive(false);
        if(rope!=null){rope.localPosition=new Vector3(0,1.13f,0);rope.localRotation=Quaternion.identity;rope.localScale=new Vector3(.035f,.72f,.035f);}
        if(crank!=null)crank.localRotation=Quaternion.identity;
    }
    void BuildDrawingBucket(Material oak)
    {
        rope=transform.Find("Well hanging rope");
        crank=new GameObject("Well crank pivot").transform;crank.SetParent(transform,false);
        crank.localPosition=new Vector3(1.76f,1.85f,0);
        foreach(string name in new[]{"Well crank arm","Well crank grip","Well winding axle"})
            transform.Find(name).SetParent(crank,true);
        // A shallow, sloping U channel catches the tipped bucket and conducts water to the front.
        var start=new Vector3(0,1.16f,-.28f);var end=new Vector3(.8f,1.02f,-1.70f);
        var direction=end-start;var channelRotation=Quaternion.LookRotation(direction);
        var center=(start+end)*.5f;
        var channel=Part("Well pouring channel",PrimitiveType.Cube,center-Vector3.up*.03f,new Vector3(.28f,.055f,direction.magnitude),oak,false);
        channel.localRotation=channelRotation;
        foreach(float side in new[]{-.16f,.16f})
        {
            var rail=Part("Well channel rim",PrimitiveType.Cube,center+channelRotation*Vector3.right*side+Vector3.up*.03f,new Vector3(.045f,.12f,direction.magnitude),oak,false);
            rail.localRotation=channelRotation;
        }
        Part("Well tipping cradle",PrimitiveType.Cube,new Vector3(0,1.24f,-.24f),new Vector3(.59f,.10f,.09f),oak,false);
        drawingBucket=new GameObject("Well drawing bucket").transform;drawingBucket.SetParent(transform,false);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;
            var part=Part("Draw bucket oak stave",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.225f,0,Mathf.Cos(a)*.225f),new Vector3(.12f,.42f,.055f),oak,false);
            part.SetParent(drawingBucket,false);part.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
        }
        foreach(float x in new[]{-.23f,.23f})
        {
            var side=Part("Draw bucket handle side",PrimitiveType.Cube,new Vector3(x,.32f,0),new Vector3(.035f,.38f,.035f),oak,false);side.SetParent(drawingBucket,false);
        }
        var handle=Part("Draw bucket handle grip",PrimitiveType.Cube,new Vector3(0,.51f,0),new Vector3(.49f,.045f,.045f),oak,false);handle.SetParent(drawingBucket,false);
        drawWater=Part("Draw bucket water",PrimitiveType.Cylinder,new Vector3(0,.13f,0),new Vector3(.41f,.01f,.41f),waterMaterial,false);drawWater.SetParent(drawingBucket,false);
        ResetDraw();
    }
    void OnDestroy() { if (waterMaterial != null) Destroy(waterMaterial); }
    public string Label => CourtyardWaterTask.IsBusy ? "Wasser schöpfen …"
        : CourtyardGarden.HasCan ? CourtyardGarden.WaterCharges < 3 ? "Gießkanne füllen" : "Gießkanne ist voll"
        : CourtyardWaterTask.HasBucket ? CourtyardWaterTask.BucketFull ? "Eimer ist voll" : "Eimer füllen" : "Brunnen ansehen";
    public Vector3 GetTransform() => transform.position + new Vector3(0,0,-1.8f);
    internal static void Install()
    {
        if (GameObject.Find("Courtyard draw well") != null) return;
        var root = new GameObject("Courtyard draw well").AddComponent<CourtyardWell>();
        var p = new Vector3(10,0,-8);
        foreach (var hit in Physics.RaycastAll(p + Vector3.up*20, Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if (hit.collider.CompareTag("Ground") && hit.normal.y > .5f) p.y = Mathf.Max(p.y,hit.point.y);
        root.transform.position = p;
        root.Build();
    }
    void Build()
    {
        var stone=Resources.Load<Material>("CourtyardStyle/Stone");
        var oak=Resources.Load<Material>("CourtyardStyle/Oak");
        var roof=Resources.Load<Material>("CourtyardStyle/Stable roof");
        waterMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Draw well cool water"};
        waterMaterial.SetColor("_BaseColor",new Color(.13f,.32f,.34f));
        waterMaterial.SetFloat("_Smoothness",.84f);
        for(int row=0;row<3;row++) for(int i=0;i<20;i++)
        {
            float a=(i+(row%2)*.5f)*Mathf.PI/10;
            var block=Part("Well limestone course",PrimitiveType.Cube,
                new Vector3(Mathf.Sin(a)*1.12f,.20f+row*.32f,Mathf.Cos(a)*1.12f),
                new Vector3(.355f,.31f,.34f),stone,true);
            block.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
        }
        Part("Well still water",PrimitiveType.Cylinder,new Vector3(0,.36f,0),new Vector3(1.86f,.025f,1.86f),waterMaterial,false);
        foreach(float side in new[]{-1f,1f})
        {
            Part("Well oak upright",PrimitiveType.Cube,new Vector3(side*1.48f,1.6f,0),new Vector3(.22f,3.2f,.25f),oak,true);
            var slope=Part("Well pitched roof",PrimitiveType.Cube,new Vector3(0,3.28f,side*.57f),new Vector3(3.65f,.16f,1.42f),roof,false);
            slope.localRotation=Quaternion.Euler(side*27,0,0);
        }
        Part("Well roof ridge",PrimitiveType.Cube,new Vector3(0,3.62f,0),new Vector3(3.8f,.17f,.20f),oak,false);
        var axle=Part("Well winding axle",PrimitiveType.Cylinder,new Vector3(0,1.85f,0),new Vector3(.18f,1.6f,.18f),oak,false);
        axle.localRotation=Quaternion.Euler(0,0,90);
        Part("Well hanging rope",PrimitiveType.Cylinder,new Vector3(0,1.13f,0),new Vector3(.035f,.72f,.035f),oak,false);
        Part("Well crank arm",PrimitiveType.Cube,new Vector3(1.76f,1.65f,0),new Vector3(.12f,.48f,.12f),oak,false);
        Part("Well crank grip",PrimitiveType.Cube,new Vector3(1.92f,1.42f,0),new Vector3(.38f,.1f,.1f),oak,false);
        BuildDrawingBucket(oak);
        var focus=new GameObject("Well approach",typeof(SphereCollider));focus.transform.SetParent(transform,false);
        focus.transform.localPosition=new Vector3(0,.6f,-1.8f);
        var trigger=focus.GetComponent<SphereCollider>();trigger.radius=.55f;trigger.isTrigger=true;
    }
    Transform Part(string label,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool solid)
    {
        var go=GameObject.CreatePrimitive(type);go.name=label;go.transform.SetParent(transform,false);
        go.transform.localPosition=position;go.transform.localScale=scale;
        var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;
        var block=new MaterialPropertyBlock();block.SetVector("_ObjectScale",scale);
        block.SetFloat("_EdgeWidth",.012f);block.SetFloat("_GrainAxis",1);
        renderer.SetPropertyBlock(block);
        if(!solid){var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);}
        return go.transform;
    }
    public void InteractWithPlayer()
    {
        var dialogue=DialogueBoxController.instance;
        var player=GameObject.FindWithTag("Player");
        if(dialogue==null || player==null || dialogue.IsOpen || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning)return;
        var controller=player.GetComponent<PlayerController>();
        if(controller==null || Vector3.Distance(player.transform.position,GetTransform())>controller.interacRange)return;
        if (CourtyardGarden.HasCan || CourtyardWaterTask.HasBucket)
        {
            if (CourtyardGarden.HasCan && CourtyardGarden.WaterCharges == 3 || CourtyardWaterTask.HasBucket && CourtyardWaterTask.BucketFull)
                dialogue.ShowMessage("Das Gefäß ist schon voll. Ich bringe das Wasser dorthin, wo es gebraucht wird.",IntroChapter.PlayerName);
            else CourtyardWaterTask.BeginFill(this);
            return;
        }
        dialogue.ShowMessage("Kühles Wasser glitzert unten im Brunnen. Die Kurbel und das Seil sehen gut gepflegt aus.",IntroChapter.PlayerName);
    }
}
