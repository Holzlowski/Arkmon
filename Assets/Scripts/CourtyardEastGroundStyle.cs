using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// A bounded eastern landscape pass. Heights are prepared before furnishings sample the ground.
[DefaultExecutionOrder(9503)]
public sealed class CourtyardEastGroundStyle : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> materials=new List<Material>();
    readonly List<Transform> plants=new List<Transform>();
    readonly List<(Vector3 at,float radius)> paths=new List<(Vector3,float)>();
    readonly List<Vector3[]> routes=new List<Vector3[]>();
    Bounds[] beds;
    CourtyardGuardRoute patrol;
    IInteractableAsPlayer[] interactions;
    MeshFilter meadow;Mesh originalMeadow;Mesh terrainCopy;
    Material earth,green;
    bool dressed;

    internal static void PrepareTerrain()
    {
        if(GameObject.Find("Eastern courtyard landscape")!=null)return;
        var meadow=GameObject.Find("Rolling courtyard meadow")?.GetComponent<MeshFilter>();if(meadow==null)return;
        var root=new GameObject("Eastern courtyard landscape");var style=root.AddComponent<CourtyardEastGroundStyle>();
        style.meadow=meadow;style.originalMeadow=meadow.sharedMesh;
        style.patrol=Resources.Load<CourtyardGuardRoute>("Guard/Patrol");
        style.beds=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed").Select(r=>r.bounds).ToArray();
        foreach(var filter in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(filter.name!="Curved courtyard path" || filter.sharedMesh==null)continue;
            var vertices=filter.sharedMesh.vertices;if(vertices.Length%17!=0)continue;
            for(int i=0;i<vertices.Length;i+=17)
            {
                var a=filter.transform.TransformPoint(vertices[i]);var b=filter.transform.TransformPoint(vertices[i+16]);
                style.paths.Add(((a+b)*.5f,Vector3.Distance(a,b)*.5f));
            }
        }
        style.Route(new Vector3(30,0,-3.9f),new Vector3(29,0,1.5f),new Vector3(25.8f,0,6.1f),1.0f);
        style.Route(new Vector3(37.8f,0,-2.5f),new Vector3(34.5f,0,1.6f),new Vector3(35,0,6.55f),.95f);
        style.Route(new Vector3(22.4f,0,1.45f),new Vector3(25,0,1.4f),new Vector3(28.7f,0,-.2f),.8f);
        var copy=Instantiate(style.originalMeadow);copy.name="Eastern meadow gentle relief";
        var v=copy.vertices;
        for(int i=0;i<v.Length;i++)
        {
            var world=meadow.transform.TransformPoint(v[i]);float change=style.Relief(world);
            v[i]=meadow.transform.InverseTransformPoint(world+Vector3.up*change);
        }
        copy.vertices=v;copy.RecalculateNormals();copy.RecalculateBounds();style.meshes.Add(copy);style.terrainCopy=copy;
        meadow.sharedMesh=copy;meadow.GetComponent<MeshCollider>().sharedMesh=copy;Physics.SyncTransforms();
    }
    void Route(Vector3 a,Vector3 b,Vector3 c,float halfWidth)
    {
        const int samples=41;var route=new Vector3[samples];
        for(int i=0;i<samples;i++){float t=i/(float)(samples-1);route[i]=(1-t)*(1-t)*a+2*(1-t)*t*b+t*t*c;paths.Add((route[i],halfWidth));}
        routes.Add(route);
    }
    float Relief(Vector3 p)
    {
        if(p.x<27 || p.x>42 || p.z>16 || p.z<-11)return 0;
        float Hill(float x,float z,float rx,float rz,float height)
        {float r=Mathf.Pow((p.x-x)/rx,2)+Mathf.Pow((p.z-z)/rz,2);return height*Mathf.Pow(Mathf.Max(0,1-r),2);}
        float height=Hill(30.3f,8.1f,2.6f,2.4f,.26f)+Hill(40.1f,10.5f,1.7f,3.2f,.22f)+Hill(30.5f,-8.1f,3.2f,2.5f,.24f);
        float clear=1;
        foreach(var path in paths)clear=Mathf.Min(clear,Mathf.SmoothStep(0,1,Mathf.Clamp01((Flat(p,path.at)-path.radius-.5f)/1.3f)));
        if(patrol!=null)for(int i=1;i<patrol.stops.Length;i++)
            clear=Mathf.Min(clear,Mathf.SmoothStep(0,1,Mathf.Clamp01((Segment(p,patrol.stops[i-1].position,patrol.stops[i].position)-1.2f)/1.1f)));
        // Foundations and the excavated cellar are fixed reference heights.
        if(p.x>31.4f && p.x<38.6f && p.z>9.1f || p.x<28.6f && p.z>5.3f)return 0;
        return height*clear;
    }
    internal static void Install()
    {
        var style=GameObject.Find("Eastern courtyard landscape")?.GetComponent<CourtyardEastGroundStyle>();if(style==null || style.dressed)return;
        var source=Resources.Load<Material>("CourtyardStyle/Stable earth");if(source==null)return;
        style.dressed=true;style.earth=new Material(source){name="Eastern courtyard worn earth"};style.materials.Add(style.earth);
        style.earth.SetColor("_BaseColor",new Color(.68f,.70f,.65f,.86f));
        style.green=new Material(source){name="Eastern courtyard moss and clover ground"};style.materials.Add(style.green);
        style.green.SetTexture("_BaseMap",Texture2D.whiteTexture);style.green.SetColor("_BaseColor",new Color(.35f,.47f,.16f,.32f));
        style.interactions=FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IInteractableAsPlayer>().ToArray();Physics.SyncTransforms();
        for(int i=0;i<style.routes.Count;i++)style.Ribbon(style.routes[i],i==2?.8f:i==1?.95f:1f);
        style.Groups();style.FoundationEdges();
    }
    void Ribbon(Vector3[] route,float width)
    {
        const int across=13;var v=new List<Vector3>();var colors=new List<Color>();var t=new List<int>();
        for(int row=0;row<route.Length;row++)
        {
            var tangent=(route[Mathf.Min(row+1,route.Length-1)]-route[Mathf.Max(row-1,0)]).normalized;
            var side=new Vector3(tangent.z,0,-tangent.x);float w=width*(1+.07f*Mathf.Sin(row*.43f));
            for(int col=0;col<across;col++)
            {
                float x=col/(float)(across-1)*2-1;v.Add(Ground(route[row]+side*(x*w),.028f));float end=Mathf.Max(1-row/5f,1-(route.Length-1-row)/5f);colors.Add(new Color(Mathf.Max(Mathf.Abs(x),end),0,0,1));
                if(row==0 || col==0)continue;int k=v.Count-1;int prev=k-across;t.AddRange(new[]{prev-1,k-1,prev,prev,k-1,k});
            }
        }
        Emit("Eastern footpath connection",v,colors,t,earth);
    }
    void Groups()
    {
        Island(new Vector3(30.2f,0,8.5f),2.0f,2.1f,48,31);
        Island(new Vector3(39.7f,0,9.5f),1.3f,3.0f,46,47);
        Island(new Vector3(30.5f,0,-8.2f),3.2f,1.6f,60,61);
        Island(new Vector3(31.6f,0,3.4f),1.65f,1.2f,36,79);
        Island(new Vector3(40.4f,0,3.6f),1.2f,1.5f,32,91);
        Island(new Vector3(23.5f,0,-7.0f),1.8f,1.4f,30,113);
    }
    void Island(Vector3 center,float rx,float rz,int count,int seed)
    {
        Patch(center,rx,rz,green);
        var random=new System.Random(seed);
        for(int i=0;i<count;i++)
        {
            float angle=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble());
            var point=center+new Vector3(Mathf.Cos(angle)*rx*r,0,Mathf.Sin(angle)*rz*r);
            if(!Clear(point))continue;
            string kind=i%11==0?"Flowers":i%5==0?"Mixed":i%3==0?"Grass":"Clover";
            Plant(kind,point,.23f+(float)random.NextDouble()*.23f,i%2==0);
        }
    }
    void FoundationEdges()
    {
        // Low earth and growth hug the outer edges, never the staircase opening or front doors.
        foreach(float x in new[]{23.2f,28.45f})
        {
            Patch(new Vector3(x,0,11.5f),.52f,4.8f,earth);
            for(int i=0;i<12;i++){var at=new Vector3(x+(i%2)*.08f,0,7.5f+i*.7f);if(Clear(at))Plant(i%4==0?"Pebbles":i%3==0?"Fern":"Clover",at,.22f+(i%3)*.06f,i%2==0);}
        }
        foreach(float x in new[]{31.9f,38.1f})
        {
            Patch(new Vector3(x,0,12.8f),.65f,2.7f,earth);
            for(int i=0;i<9;i++){var at=new Vector3(x,0,10.3f+i*.55f);if(Clear(at))Plant(i%4==0?"Fern":"Clover",at,.25f+(i%3)*.06f,i%2==0);}
        }
        Patch(new Vector3(41.7f,0,7.5f),.55f,5f,earth);
        for(int i=0;i<12;i++){var at=new Vector3(41.6f,0,2.5f+i*.85f);if(Clear(at))Plant(i%4==0?"Pebbles":"Grass",at,.25f,i%2==0);}
    }
    bool Clear(Vector3 p)
    {
        // The stair opening is excluded in XZ, irrespective of its negative floor height.
        if(p.x>23.45f && p.x<28.15f && p.z>6.0f && p.z<17.7f)return false;
        foreach(var bed in beds)if(p.x>bed.min.x-.55f && p.x<bed.max.x+.55f && p.z>bed.min.z-.55f && p.z<bed.max.z+.55f)return false;
        foreach(var path in paths)if(Flat(p,path.at)<path.radius+.28f)return false;
        if(patrol!=null)for(int i=1;i<patrol.stops.Length;i++)if(Segment(p,patrol.stops[i-1].position,patrol.stops[i].position)<.95f)return false;
        foreach(var interaction in interactions)if(Flat(p,interaction.GetTransform())<1.3f)return false;
        var ground=Ground(p,0);
        return !Physics.OverlapSphere(ground+Vector3.up*.22f,.30f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.CompareTag("Ground"));
    }
    void Patch(Vector3 center,float rx,float rz,Material material)
    {
        const int sides=48,rings=7;var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides,r=ring/(float)rings,edge=1+.06f*Mathf.Sin(a*5)+.04f*Mathf.Cos(a*3);
            var p=center+new Vector3(Mathf.Cos(a)*rx*r*edge,0,Mathf.Sin(a)*rz*r*edge);
            v.Add(Ground(p,.018f));c.Add(new Color(r,0,0,1));
            if(ring==0)continue;int k=ring*sides+i,prev=k-sides,next=(i+1)%sides;t.AddRange(new[]{prev,ring*sides+next,k,prev,(ring-1)*sides+next,ring*sides+next});
        }
        Emit(material==green?"Eastern meadow ground variation":"Eastern foundation earth",v,c,t,material);
    }
    void Emit(string name,List<Vector3> v,List<Color> c,List<int> t,Material material)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void Plant(string kind,Vector3 p,float height,bool flip)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/"+kind);if(sprite==null)return;
        var obj=new GameObject("Eastern meadow "+kind,typeof(SpriteRenderer));obj.transform.SetParent(transform,false);obj.transform.position=Ground(p,.025f);obj.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.flipX=flip;renderer.color=new Color(.70f,.88f,.63f);plants.Add(obj.transform);
    }
    static Vector3 Ground(Vector3 p,float lift)
    {
        float height=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(p.x,20,p.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore))if(hit.collider.CompareTag("Ground"))height=Mathf.Max(height,hit.point.y);
        if(!float.IsNegativeInfinity(height))p.y=height;p.y+=lift;return p;
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
    static float Segment(Vector3 p,Vector3 a,Vector3 b)
    {p.y=a.y=b.y=0;var d=b-a;float t=d.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);return Vector3.Distance(p,a+t*d);}
    void LateUpdate(){if(Camera.main!=null)foreach(var plant in plants)plant.rotation=Camera.main.transform.rotation;}
    void OnDestroy()
    {
        if(meadow!=null && meadow.sharedMesh==terrainCopy){meadow.sharedMesh=originalMeadow;var collider=meadow.GetComponent<MeshCollider>();if(collider!=null)collider.sharedMesh=originalMeadow;}
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);
    }
}
