using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The body, seated driver and hitch share the carriage transform along the bridge.
public sealed class ArrivalCarriage : MonoBehaviour
{
    const float WheelRadius = .62f;
    static readonly Vector3 CoachmanSeat = new Vector3(-1.72f,1.46f,-.46f);
    readonly Transform[] wheels = new Transform[4];
    readonly List<Mesh> meshes = new List<Mesh>();
    Transform door;
    SpriteRenderer horse, driver;
    Sprite horseArt, driverArt;
    LineRenderer reins;
    StableLocalFoley foley;
    AudioClip hoof;
    Material cabin, canopy, brass, leather, iron, oak;
    BoxCollider parkedBody;
    float nextHoofDistance;
    internal bool FaceAlongRoad { get; set; }

    public void Build()
    {
        PrepareSeatMaterials();
        canopy = Finish("Charcoal plum coach roof",new Color(.13f,.10f,.19f),.21f);
        brass = Finish("Warm brass fittings",new Color(.78f,.52f,.21f),.38f);

        iron = Finish("Burnished iron tires",new Color(.19f,.16f,.14f),.26f);
        BuildCabin();

        var tire = BuildTire();
        int index=0;
        foreach (float x in new[]{-1.17f,1.18f})
        foreach (float z in new[]{-1.03f,1.03f})
            wheels[index++] = BuildWheel(new Vector3(x,WheelRadius,z),tire);
        foreach (float z in new[]{-.47f,.47f})
            Beam(transform,"Carriage shaft",new Vector3(-1.37f,.77f,z),new Vector3(-3.91f,1.43f,z),.076f,oak);

        BuildTeam();
        parkedBody = gameObject.AddComponent<BoxCollider>();
        parkedBody.center = new Vector3(.12f,1.35f,0);
        parkedBody.size = new Vector3(2.75f,2.70f,1.90f);
        parkedBody.enabled = false;
    }

    private void BuildCabin()
    {
        Box(transform,"Coach chassis",new Vector3(-.22f,.72f,0),new Vector3(3.93f,.26f,1.62f),oak);
        Box(transform,"Cabin floor",new Vector3(.07f,.96f,0),new Vector3(2.33f,.26f,1.77f),cabin);
        Box(transform,"Far cabin wall",new Vector3(.08f,1.76f,.89f),new Vector3(2.52f,1.55f,.12f),cabin);
        foreach (float sign in new[]{-1f,1f})
        {
            float end = .08f + sign*1.23f;
            var wall = Box(transform,"Canted cabin end",new Vector3(end,1.76f,0),new Vector3(.15f,1.61f,1.88f),cabin);
            wall.localRotation = Quaternion.Euler(0,0,-sign*8);
            float jamb = .08f + sign*.94f;
            Box(transform,"Door side panel",new Vector3(jamb,1.82f,-.90f),new Vector3(.69f,1.42f,.15f),cabin);
            Beam(transform,"Brass cabin corner",new Vector3(end-sign*.12f,1.04f,-.987f),
                new Vector3(end+sign*.08f,2.56f,-.987f),.052f,brass);
            Box(transform,"Door panel inlay",new Vector3(jamb,1.35f,-.984f),new Vector3(.44f,.35f,.034f),canopy);
            AddCoachLamp(new Vector3(jamb,2.02f,-1.03f));
        }
        Box(transform,"Cabin belt rail",new Vector3(.08f,1.08f,-.966f),new Vector3(2.40f,.07f,.09f),brass);
        Box(transform,"Roof cornice",new Vector3(.08f,2.57f,0),new Vector3(2.69f,.12f,2.10f),brass);
        BuildCanopy();

        door = Anchor(transform,"Carriage door hinge",new Vector3(-.55f,1.12f,-.976f));
        Box(door,"Carriage door panel",new Vector3(.61f,.31f,0),new Vector3(1.22f,.62f,.12f),cabin);
        Box(door,"Inset door field",new Vector3(.61f,.32f,-.069f),new Vector3(1.04f,.47f,.04f),canopy);
        Box(door,"Door window rail",new Vector3(.61f,.63f,-.058f),new Vector3(1.23f,.058f,.10f),brass);
        foreach (float x in new[]{.035f,1.187f})
        {
            float inset = x < .6f ? .11f : -.11f;
            Beam(door,"Window side",new Vector3(x,.64f,0),new Vector3(x,1.15f,0),.085f,cabin);
            Beam(door,"Window chamfer",new Vector3(x,1.15f,0),new Vector3(x+inset,1.30f,0),.085f,cabin);
            Beam(door,"Window brass inset",new Vector3(x,.66f,-.050f),new Vector3(x,1.15f,-.050f),.022f,brass);
            Beam(door,"Window brass inset",new Vector3(x,1.15f,-.050f),new Vector3(x+inset,1.30f,-.050f),.022f,brass);
            Beam(door,"Brass door edge",new Vector3(x,.035f,-.075f),new Vector3(x,.61f,-.075f),.031f,brass);
        }
        Box(door,"Arched window top",new Vector3(.61f,1.30f,0),new Vector3(.98f,.10f,.13f),cabin);
        Box(door,"Window brass inset",new Vector3(.61f,1.30f,-.073f),new Vector3(.94f,.024f,.025f),brass);
        Box(door,"Door latch",new Vector3(1.02f,.49f,-.10f),new Vector3(.19f,.055f,.12f),brass);
        AddDoorCrest();

        Box(transform,"Carriage step",new Vector3(.29f,.48f,-1.04f),new Vector3(.88f,.10f,.47f),oak);
        Box(transform,"Brass step lip",new Vector3(.29f,.50f,-1.282f),new Vector3(.89f,.07f,.05f),brass);
        BuildSeat(transform);
    }

    private void PrepareSeatMaterials()
    {
        oak = Resources.Load<Material>("CourtyardStyle/Oak");
        cabin = Finish("Indigo carriage lacquer",new Color(.24f,.23f,.39f),.33f);
        leather = Finish("Dark bridle leather",new Color(.13f,.073f,.046f),.16f);
    }

    private void BuildSeat(Transform parent)
    {
        Box(parent,"Coachman's footboard",new Vector3(-2.23f,.780f,0),new Vector3(.84f,.13f,1.47f),oak);
        Box(parent,"Coachman's seat",new Vector3(-1.65f,1.37f,0),new Vector3(.76f,.18f,1.45f),leather);
        Box(parent,"Coachman's backrest",new Vector3(-1.32f,1.61f,0),new Vector3(.12f,.39f,1.44f),cabin);
    }

    internal void BuildScaleReference(Vector3 horsePosition, float coachmanX)
    {
        PrepareSeatMaterials();
        BuildHorse(horsePosition);
        var stand = Anchor(transform,"Kutschbock der Größenprobe",
            new Vector3(coachmanX-CoachmanSeat.x,0,-CoachmanSeat.z));
        Box(stand,"Standstütze",new Vector3(-1.66f,.65f,.02f),new Vector3(.54f,1.30f,.86f),oak);
        Box(stand,"Fußbrettstütze",new Vector3(-2.13f,.35f,.02f),new Vector3(.16f,.70f,.80f),oak);
        BuildSeat(stand);
        BuildDriver(stand);
    }

    private void AddDoorCrest()
    {
        var seal = Cylinder(door,"Carriage seal",new Vector3(.59f,.33f,-.094f),.165f,.025f,brass);
        seal.localRotation = Quaternion.Euler(90,0,0);
        var field = Cylinder(door,"Enamel medallion",new Vector3(.59f,.33f,-.112f),.135f,.018f,cabin);
        field.localRotation = seal.localRotation;
        for(int ray=0;ray<4;ray++)
        {
            float a=ray*Mathf.PI/4;
            var offset=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*(ray%2==0?.098f:.069f);
            var center=new Vector3(.59f,.33f,-.125f);
            Beam(door,"Brass compass rose",center-offset,center+offset,ray%2==0?.022f:.014f,brass);
        }
    }

    private void AddCoachLamp(Vector3 point)
    {
        var lamp = Anchor(transform,"Carriage lantern",point);
        Box(lamp,"Lamp bracket",new Vector3(0,0,.015f),new Vector3(.08f,.08f,.19f),iron);
        Box(lamp,"Lamp glass",new Vector3(0,0,-.11f),new Vector3(.16f,.21f,.16f),brass);
        Box(lamp,"Lamp base",new Vector3(0,-.12f,-.11f),new Vector3(.19f,.047f,.19f),iron);
        Box(lamp,"Lamp cap",new Vector3(0,.12f,-.11f),new Vector3(.19f,.047f,.19f),iron);
    }

    private void BuildCanopy()
    {
        const int divisions = 12;
        var vertices=new List<Vector3>();
        var triangles=new List<int>();
        for(int i=0;i<divisions;i++)
        {
            var left=RoofPoint(i,divisions,-1);
            var right=RoofPoint(i,divisions,1);
            var nextLeft=RoofPoint(i+1,divisions,-1);
            var nextRight=RoofPoint(i+1,divisions,1);
            Quad(vertices,triangles,left,nextLeft,nextRight,right);
            Quad(vertices,triangles,left,new Vector3(left.x,2.60f,left.z),new Vector3(nextLeft.x,2.60f,nextLeft.z),nextLeft);
            Quad(vertices,triangles,right,nextRight,new Vector3(nextRight.x,2.60f,nextRight.z),new Vector3(right.x,2.60f,right.z));
            Beam(transform,"Canopy edge piping",left,nextLeft,.028f,brass);
            Beam(transform,"Canopy edge piping",right,nextRight,.028f,brass);
        }
        MeshPart(transform,"Curved carriage canopy",MakeMesh("Coach canopy",vertices,triangles),canopy);
    }

    private static Vector3 RoofPoint(int band, int divisions, float end)
    {
        float t=band/(float)divisions;
        float crown=Mathf.Sin(t*Mathf.PI);
        return new Vector3(.08f+end*(1.30f+.058f*crown),2.61f+.34f*crown,Mathf.Lerp(-1.04f,1.04f,t));
    }

    private void BuildTeam()
    {
        var horseRoot = BuildHorse(new Vector3(-3.86f,0,0));
        foley = horseRoot.gameObject.AddComponent<StableLocalFoley>();
        foley.ListenFromCamera = true;
        hoof = Resources.Load<AudioClip>("Audio/Stable/hoof-shift");

        BuildDriver(transform);
        reins = new GameObject("Coachman's reins",typeof(LineRenderer)).GetComponent<LineRenderer>();
        reins.transform.SetParent(transform,false);
        reins.sharedMaterial=leather;
        reins.startWidth=.024f;reins.endWidth=.019f;
        reins.numCapVertices=2;
        reins.positionCount=3;
        reins.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private Transform BuildHorse(Vector3 position)
    {
        var horseRoot = Anchor(transform,"Carriage horse",position);
        horseArt = PaperArt("Carriage Draft Horse",new Vector2(.5f,.017f));
        horse = Paper(horseRoot,"Carriage horse illustration",horseArt,2.58f);
        PaperGroundShadow.Attach(horseRoot.gameObject,horse);
        return horseRoot;
    }

    private void BuildDriver(Transform parent)
    {
        var driverRoot = Anchor(parent,"Coachman",CoachmanSeat);
        driverArt = PaperArt("Carriage Coachman",new Vector2(.60f,.315f));
        driver = Paper(driverRoot,"Coachman illustration",driverArt,2.23f);
    }

    private static Sprite PaperArt(string resource, Vector2 pivot)
    {
        var texture=Resources.Load<Texture2D>(resource);
        var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),pivot,100);
        sprite.name=resource;
        return sprite;
    }

    private static SpriteRenderer Paper(Transform root, string name, Sprite art, float height)
    {
        var paper=Anchor(root,name,Vector3.zero).gameObject.AddComponent<SpriteRenderer>();
        paper.sprite=art;
        paper.transform.localScale=Vector3.one*(height/art.bounds.size.y);
        return paper;
    }

    public void PresentTravel(float distance, bool rolling)
    {
        float angle=distance/WheelRadius*Mathf.Rad2Deg;
        foreach(var wheel in wheels)wheel.localRotation=Quaternion.Euler(0,0,angle);
        horse.transform.localPosition=Vector3.up*(rolling?.025f*Mathf.Abs(Mathf.Sin(distance*9)):0);
        if(rolling && distance>=nextHoofDistance)
        {
            nextHoofDistance=distance+1.06f;
            foley.Play(hoof,.62f,1.05f);
        }
    }

    public void PresentDoor(float progress) => door.localRotation=Quaternion.Euler(0,102*progress,0);

    internal void BeginApproach()
    {
        parkedBody.enabled = false;
        nextHoofDistance = 0;
        PresentDoor(0);
        PresentTravel(0,false);
    }

    public void Park()
    {
        parkedBody.enabled=true;
        foley.Stop();
    }

    internal IEnumerator DriveToStable(Vector3[] route)
    {
        var startRotation = transform.rotation;
        for (float elapsed = 0; elapsed < .65f; elapsed += Time.deltaTime)
        {
            PresentDoor(1 - Mathf.SmoothStep(0, 1, elapsed / .65f));
            yield return null;
        }
        BeginApproach();
        FaceAlongRoad = true;

        const float duration = 17f;
        float distance = 0;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.SmoothStep(0, 1, elapsed / duration);
            float x = Mathf.Lerp(route[0].x, route[route.Length - 1].x, progress);
            Vector3 point = CarriageGround(new Vector3(x, 0, RouteDepth(route, x)));
            float depthSlope = (RouteDepth(route, x + .12f) - RouteDepth(route, x - .12f)) / .24f;
            var heading = Quaternion.Euler(0, -Mathf.Atan(depthSlope) * Mathf.Rad2Deg, 0);
            distance += Vector3.Distance(transform.position, point);
            transform.SetPositionAndRotation(point, Quaternion.Slerp(startRotation, heading, Mathf.Clamp01(elapsed)));
            PresentTravel(distance, true);
            yield return null;
        }
        PresentTravel(distance, false);
        ParkAtStable(route[route.Length - 1]);
    }

    internal void ParkAtStable(Vector3 spot)
    {
        transform.SetPositionAndRotation(CarriageGround(spot), Quaternion.identity);
        FaceAlongRoad = true;
        PresentDoor(0);
        Park();
    }

    private static Vector3 CarriageGround(Vector3 point) => CourtyardGuard.Grounded(point) + Vector3.up * .02f;

    private static float RouteDepth(Vector3[] route, float x)
    {
        if (x >= route[0].x) return route[0].z;
        for (int i = 1; i < route.Length; i++)
        {
            var from = route[i - 1];
            var to = route[i];
            if (x < to.x) continue;
            var before = route[Mathf.Max(0, i - 2)];
            var after = route[Mathf.Min(route.Length - 1, i + 1)];
            float span = to.x - from.x;
            float entry = i == 1 ? 0 : (to.z - before.z) / (to.x - before.x) * span;
            float exit = i == route.Length - 1 ? 0 : (after.z - from.z) / (after.x - from.x) * span;
            float t = (x - from.x) / span;
            float t2 = t * t;
            return Mathf.SmoothStep(from.z, to.z, t) + t * (1 - t) * (1 - t) * entry + (t2 * t - t2) * exit;
        }
        return route[route.Length - 1].z;
    }

    private void LateUpdate()
    {
        var view=Camera.main;
        if(horse==null || view==null)return;
        var paperAngle=FaceAlongRoad ? transform.rotation : view.transform.rotation;
        horse.transform.rotation=paperAngle;
        driver.transform.rotation=paperAngle;
        if(reins==null)return;
        var hands=ArtPoint(driver,new Vector2(.32f,.493f));
        var bit=ArtPoint(horse,new Vector2(.246f,.710f));
        reins.SetPosition(0,hands);
        reins.SetPosition(1,Vector3.Lerp(hands,bit,.52f)-Vector3.up*.18f);
        reins.SetPosition(2,bit);
    }

    private static Vector3 ArtPoint(SpriteRenderer paper, Vector2 point)
        => paper.transform.TransformPoint((point*paper.sprite.rect.size-paper.sprite.pivot)/paper.sprite.pixelsPerUnit);

    private Transform BuildWheel(Vector3 center, Mesh tire)
    {
        var wheel=Anchor(transform,"Carriage wheel",center);
        MeshPart(wheel,"Iron tire",tire,iron);
        for(int i=0;i<10;i++)
        {
            float angle=i*Mathf.PI/5;
            var direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            Beam(wheel,"Wheel oak spoke",direction*.09f,direction*.558f,.064f,oak);
        }
        var hub=Cylinder(wheel,"Brass wheel hub",Vector3.zero,.115f,.154f,brass);
        hub.localRotation=Quaternion.Euler(90,0,0);
        return wheel;
    }

    private Mesh BuildTire()
    {
        const int sides=36;
        const float inside=.545f, halfDepth=.060f;
        var vertices=new List<Vector3>();
        var triangles=new List<int>();
        for(int i=0;i<sides;i++)
        {
            float a=i*2*Mathf.PI/sides, b=(i+1)*2*Mathf.PI/sides;
            var u=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
            var v=new Vector3(Mathf.Cos(b),Mathf.Sin(b),0);
            var depth=Vector3.forward*halfDepth;
            var outerA=u*WheelRadius;var outerB=v*WheelRadius;
            var innerA=u*inside;var innerB=v*inside;
            Quad(vertices,triangles,outerA-depth,outerB-depth,outerB+depth,outerA+depth);
            Quad(vertices,triangles,innerB-depth,innerA-depth,innerA+depth,innerB+depth);
            Quad(vertices,triangles,innerA+depth,outerA+depth,outerB+depth,innerB+depth);
            Quad(vertices,triangles,innerB-depth,outerB-depth,outerA-depth,innerA-depth);
        }
        return MakeMesh("Shared round carriage tire",vertices,triangles);
    }

    private static void Quad(List<Vector3> vertices,List<int> triangles,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {
        int i=vertices.Count;
        vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
        triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);
        triangles.Add(i);triangles.Add(i+2);triangles.Add(i+3);
    }

    private Mesh MakeMesh(string name,List<Vector3> vertices,List<int> triangles)
    {
        var mesh=new Mesh{name=name};
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
        meshes.Add(mesh);
        return mesh;
    }

    private static void MeshPart(Transform parent,string name,Mesh mesh,Material material)
    {
        var part=Anchor(parent,name,Vector3.zero);
        part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
    }

    private static Transform Anchor(Transform parent,string name,Vector3 position)
    {
        var part=new GameObject(name).transform;
        part.SetParent(parent,false);
        part.localPosition=position;
        return part;
    }

    private static Transform Beam(Transform parent,string name,Vector3 from,Vector3 to,float width,Material material)
    {
        var part=Box(parent,name,(from+to)*.5f,new Vector3(width,Vector3.Distance(from,to),width),material);
        part.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);
        return part;
    }

    private static Transform Box(Transform parent,string name,Vector3 at,Vector3 size,Material material)
        => Primitive(parent,name,PrimitiveType.Cube,at,size,material);

    private static Transform Cylinder(Transform parent,string name,Vector3 at,float radius,float thickness,Material material)
        => Primitive(parent,name,PrimitiveType.Cylinder,at,new Vector3(radius*2,thickness*.5f,radius*2),material);

    private static Transform Primitive(Transform parent,string name,PrimitiveType type,Vector3 at,Vector3 size,Material material)
    {
        var part=GameObject.CreatePrimitive(type);
        part.name=name;part.transform.SetParent(parent,false);
        part.transform.localPosition=at;part.transform.localScale=size;
        var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        var renderer=part.GetComponent<Renderer>();renderer.sharedMaterial=material;
        if(material.HasProperty("_ObjectScale"))
        {
            var block=new MaterialPropertyBlock();
            block.SetVector("_ObjectScale",part.transform.lossyScale);
            block.SetFloat("_GrainAxis",1);block.SetFloat("_EdgeWidth",.012f);
            renderer.SetPropertyBlock(block);
        }
        return part.transform;
    }

    private static Material Finish(string name,Color color,float smoothness)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
        material.SetFloat("_Smoothness",smoothness);
        return material;
    }

    private void OnDestroy()
    {
        Destroy(cabin);Destroy(canopy);Destroy(brass);Destroy(leather);Destroy(iron);
        Destroy(horseArt);Destroy(driverArt);
        foreach(var mesh in meshes)Destroy(mesh);
    }
}
