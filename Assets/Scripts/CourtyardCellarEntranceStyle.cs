using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// An exterior service stair, not the sealed story gate. Only runtime copies are changed.
public sealed class CourtyardCellarEntranceStyle : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> materials=new List<Material>();
    Material stone,trim,oak,iron,glow;
    PhysicsMaterial stairFriction;
    const float Half=1.4f, Front=-10f, Back=.3f, Sink=3.2f, Landing=-.75f-Sink;
    internal static void Install()
    {
        var entrance=FindObjectsByType<CourtyardEntrance>(FindObjectsSortMode.None).FirstOrDefault(e=>e.destination==CourtyardEntrance.Destination.Cellar);
        var house=GameObject.Find("Cellar storehouse");
        if(entrance==null || house==null || GameObject.Find("Illustrated cellar entrance")!=null)return;
        var source=Resources.Load<Material>("CourtyardStyle/Stone");var wood=Resources.Load<Material>("CourtyardStyle/Stable oak");
        if(source==null || wood==null)return;
        var root=new GameObject("Illustrated cellar entrance");root.transform.SetParent(house.transform.parent,false);
        var oldPortal=new Vector3(house.transform.position.x,0,house.transform.position.z-2.5f);
        root.transform.position=oldPortal+new Vector3(-1.5f,0,3.9f);
        var style=root.AddComponent<CourtyardCellarEntranceStyle>();
        style.stone=style.Paint(source,"Cellar weathered castle stone",new Color(.64f,.70f,.72f),.34f);
        style.trim=style.Paint(Resources.Load<Material>("CourtyardStyle/Plaster"),"Cellar worn limestone",new Color(.66f,.66f,.59f),.45f);
        style.oak=style.Paint(wood,"Cellar aged oak",new Color(.64f,.60f,.53f),.38f);
        style.stone.SetFloat("_AmbientFill",.30f);style.trim.SetFloat("_AmbientFill",.32f);style.oak.SetFloat("_AmbientFill",.25f);
        style.iron=style.Plain("Cellar dark iron",new Color(.11f,.14f,.15f));
        style.glow=style.Plain("Cellar lantern amber",new Color(1,.60f,.18f));style.glow.EnableKeyword("_EMISSION");style.glow.SetColor("_EmissionColor",new Color(1,.36f,.06f)*2);
        foreach(var r in house.GetComponentsInChildren<Renderer>())r.enabled=false;
        // Replace its old tall collision volume as well, so no invisible roof remains.
        foreach(var c in house.GetComponentsInChildren<Collider>())c.enabled=false;
        foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if((r.name=="Closed oak entrance" || r.name=="Doorway jamb" || r.name=="Doorway lintel") && Mathf.Abs(r.transform.position.x-oldPortal.x)<1.7f && Mathf.Abs(r.transform.position.z-oldPortal.z)<.5f)
            {r.enabled=false;var c=r.GetComponent<Collider>();if(c!=null)c.enabled=false;}
        style.Excavate();style.Build();
        entrance.transform.position=root.transform.TransformPoint(new Vector3(0,Landing,-1.1f));
        Physics.SyncTransforms();
    }
    Material Paint(Material source,string name,Color color,float scale)
    {
        var m=new Material(source){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_TextureScale",scale);materials.Add(m);return m;
    }
    Material Plain(string name,Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.16f);materials.Add(m);return m;
    }
    void Build()
    {
        // Assemble the portal, then sink it beneath the courtyard against the castle foundation.
        Box("Cellar rear stone body",new Vector3(0,.5f,2.45f),new Vector3(3.75f,2.5f,4.0f),stone,true);
        foreach(int side in new[]{-1,1})
        {
            Box("Cellar arch pier",new Vector3(side*1.72f,.69f,0),new Vector3(.64f,3.0f,1.1f),stone,true);
            Box("Cellar pier foot",new Vector3(side*1.72f,-.42f,-.12f),new Vector3(.83f,.40f,1.3f),trim);

        }
        const float spring=1.25f,radius=1.38f;
        for(int i=0;i<15;i++)
        {
            float a=i*Mathf.PI/15+.008f,b=(i+1)*Mathf.PI/15-.008f;
            Vector2 P(float r,float t)=>new Vector2(Mathf.Cos(t)*r,spring+Mathf.Sin(t)*r);
            Solid("Cellar arch stone",new[]{P(radius,a),P(radius+.48f,a),P(radius+.48f,b),P(radius,b)},-.60f,1.05f,trim);
            var vault=Solid("Cellar barrel vault",new[]{P(radius,a),P(radius+.43f,a),P(radius+.43f,b),P(radius,b)},.45f,4.0f,stone);
            vault.AddComponent<MeshCollider>().sharedMesh=vault.GetComponent<MeshFilter>().sharedMesh;
        }
        Box("Cellar arch keystone",new Vector3(0,2.95f,-.16f),new Vector3(.32f,.5f,1.18f),trim);

        for(int i=0;i<10;i++)
        {
            float left=-1.34f+i*.268f+.008f,right=left+.252f;
            var points=new List<Vector2>{new Vector2(left,-.74f),new Vector2(right,-.74f)};
            for(int j=0;j<=3;j++){float x=Mathf.Lerp(right,left,j/3f);points.Add(new Vector2(x,spring+Mathf.Sqrt(radius*radius-x*x)));}
            Solid("Cellar oak plank",points.ToArray(),-.28f,.19f,oak);
        }
        Box("Cellar closed door",new Vector3(0,.78f,-.03f),new Vector3(2.76f,3.1f,.32f),iron,true,false);
        foreach(float y in new[]{-.28f,.90f,1.91f})
        {
            Box("Cellar door strap",new Vector3(0,y,-.36f),new Vector3(2.52f,.10f,.06f),iron);
            foreach(float x in new[]{-1.13f,-.67f,.67f,1.13f})Box("Cellar rivet",new Vector3(x,y,-.40f),Vector3.one*.055f,trim);
        }
        Box("Cellar latch",new Vector3(.75f,.55f,-.41f),new Vector3(.28f,.34f,.10f),iron);
        Box("Cellar latch grip",new Vector3(.75f,.55f,-.51f),new Vector3(.08f,.24f,.10f),trim);
        Lantern();Crest();
        foreach(Transform part in transform)part.localPosition+=Vector3.down*Sink;
        // Seal the cutout above the buried arch as well, including oblique views from the eastern house.
        Box("Cellar portal earth retaining cap",new Vector3(0,.035f,0),new Vector3(3.9f,.16f,1.3f),trim,true);
        // Fill the wedge between the curved arch and the level ground cap, including its oblique sides.
        for(int i=0;i<14;i++)
        {
            float left=-Half+i*(2*Half/14),right=-Half+(i+1)*(2*Half/14);
            float Bottom(float x)=>1.25f-Sink+Mathf.Sqrt(1.86f*1.86f-x*x)-.02f;
            Solid("Cellar buried arch backing",new[]{new Vector2(left,Bottom(left)),new Vector2(right,Bottom(right)),new Vector2(right,.04f),new Vector2(left,.04f)},-.58f,.94f,stone);
        }
        // The portal crown stays at courtyard level; the whole door is below ground.
        Box("Cellar castle foundation link",new Vector3(-2.02f,.15f,2.1f),new Vector3(.8f,.3f,4.4f),stone,true);
        foreach(int side in new[]{-1,1})
        {
            float bottom=Landing-.25f,top=.35f,length=Back-Front;
            Box("Cellar retaining cheek",new Vector3(side*1.65f,(bottom+top)*.5f,(Front+Back)*.5f),new Vector3(.5f,top-bottom,length),stone,true);
            const int copingCount=16;
            for(int i=0;i<copingCount;i++)Box("Cellar coping stone",new Vector3(side*1.65f,.40f,Front+(i+.5f)*length/copingCount),new Vector3(.65f,.16f,length/copingCount-.02f),trim);
        }
        const int stepCount=18;
        float tread=(-1.4f-Front)/stepCount;
        for(int i=0;i<stepCount;i++)
            Box("Worn cellar step",new Vector3(0,-.11f+Landing*i/stepCount,Front+(i+.5f)*tread),new Vector3(2.8f,.22f,tread),trim);
        Box("Cellar lower landing",new Vector3(0,Landing-.11f,-.65f),new Vector3(2.8f,.22f,1.5f),trim,true);
        var ramp=new GameObject("Cellar walkable stair slope",typeof(MeshFilter),typeof(MeshRenderer));ramp.transform.SetParent(transform,false);
        // Separate mesh in XZ with a low sealed bottom.
        var mesh=new Mesh{name="Cellar stair collision"};mesh.vertices=new[]{new Vector3(-Half,-.025f,Front-.8f),new Vector3(Half,-.025f,Front-.8f),new Vector3(-Half,.16f,Front-.2f),new Vector3(Half,.16f,Front-.2f),new Vector3(-Half,Landing-.13f,-1.0f),new Vector3(Half,Landing-.13f,-1.0f)};
        var rampVertices=mesh.vertices.ToList();rampVertices.AddRange(mesh.vertices.Select(v=>new Vector3(v.x,Landing-.5f,v.z)));mesh.SetVertices(rampVertices);
        mesh.triangles=new[]{0,2,1,1,2,3,2,4,3,3,4,5,6,7,8,7,9,8,8,9,10,9,11,10,0,1,6,1,7,6,4,10,5,5,10,11,0,6,2,2,6,8,2,8,4,4,8,10,1,3,7,3,9,7,3,5,9,5,11,9};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        ramp.GetComponent<MeshRenderer>().enabled=false;ramp.GetComponent<MeshFilter>().sharedMesh=mesh;var rampCollider=ramp.AddComponent<MeshCollider>();rampCollider.sharedMesh=mesh;rampCollider.convex=true;ramp.tag="Ground";
        stairFriction=new PhysicsMaterial("Cellar smooth walking"){dynamicFriction=0,staticFriction=0,frictionCombine=PhysicsMaterialCombine.Minimum};
        rampCollider.sharedMaterial=stairFriction;transform.Find("Cellar lower landing").GetComponent<Collider>().sharedMaterial=stairFriction;
        Approach();
    }
    void Lantern()
    {
        Box("Lantern iron bracket",new Vector3(-1.18f,2.1f,-.55f),new Vector3(.12f,.12f,.85f),iron);
        Box("Lantern warm glass",new Vector3(-1.18f,1.68f,-.85f),new Vector3(.30f,.43f,.26f),glow);
        foreach(float y in new[]{1.42f,1.94f})Box("Lantern cap",new Vector3(-1.18f,y,-.85f),new Vector3(.43f,.09f,.39f),iron);
        foreach(float x in new[]{-1.35f,-1.01f})foreach(float z in new[]{-.99f,-.71f})Box("Lantern iron upright",new Vector3(x,1.68f,z),new Vector3(.045f,.50f,.045f),iron);
        var obj=new GameObject("Cellar warm lantern",typeof(Light));obj.transform.SetParent(transform,false);obj.transform.localPosition=new Vector3(-.9f,1.7f,-1.15f);
        var light=obj.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.67f,.30f);light.intensity=2.2f;light.range=5;light.shadows=LightShadows.Soft;
    }
    void Crest()
    {
        var source=GameObject.Find("Aurenfeld portal crest");if(source==null)return;
        var crest=new GameObject("Cellar family seal",typeof(MeshFilter),typeof(MeshRenderer));crest.transform.SetParent(transform,false);
        crest.transform.localPosition=new Vector3(0,1.56f,-.41f);crest.transform.localScale=new Vector3(.44f,.52f,1);
        crest.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;crest.GetComponent<MeshRenderer>().sharedMaterial=source.GetComponent<MeshRenderer>().sharedMaterial;
    }
    void Approach()
    {
        var earth=Resources.Load<Material>("CourtyardStyle/Stable earth");
        if(earth!=null)
        {
            const int sides=40;var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            for(int ring=0;ring<=4;ring++)for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,r=ring/4f;float edge=1+.045f*Mathf.Sin(a*5);
                vertices.Add(new Vector3(Mathf.Cos(a)*2.25f*r*edge,.023f,Front-1.9f+Mathf.Sin(a)*1.78f*r*edge));colors.Add(new Color(r,0,0,1));
                if(ring==0)continue;int prev=(ring-1)*sides+i,cur=ring*sides+i,next=(i+1)%sides;
                triangles.AddRange(new[]{prev,ring*sides+next,cur,prev,(ring-1)*sides+next,ring*sides+next});
            }
            var mesh=new Mesh{name="Cellar worn approach earth"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var patch=new GameObject("Cellar worn approach earth",typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(transform,false);patch.GetComponent<MeshFilter>().sharedMesh=mesh;patch.GetComponent<MeshRenderer>().sharedMaterial=earth;patch.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        for(int i=0;i<5;i++)
        {
            var slab=Box("Cellar approach flagstone",new Vector3(.13f*Mathf.Sin(i*2),.035f,Front-.42f-i*.66f),new Vector3(1.9f-i*.13f,.06f,.56f),trim);
            slab.transform.localRotation=Quaternion.Euler(0,i%2==0?4:-6,0);
        }
        // Low planting stays outside the retaining walls and the walking line.
        var sprite=Resources.Load<Sprite>("CourtyardStyle/Fern");
        if(sprite!=null)foreach(int side in new[]{-1,1})for(int i=0;i<3;i++)
        {
            var plant=new GameObject("Cellar verge fern",typeof(SpriteRenderer));plant.transform.SetParent(transform,false);
            plant.transform.localPosition=new Vector3(side*(2.15f+i*.12f),.015f,-2.9f+i*.8f);plant.transform.rotation=Camera.main!=null?Camera.main.transform.rotation:Quaternion.Euler(25,0,0);plant.transform.localScale=Vector3.one*(.48f/sprite.bounds.size.y);
            var r=plant.GetComponent<SpriteRenderer>();r.sprite=sprite;r.color=new Color(.73f,.82f,.66f);
        }
    }
    GameObject Box(string name,Vector3 at,Vector3 size,Material mat,bool collision=false,bool visible=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform,false);go.transform.localPosition=at;go.transform.localScale=size;
        var c=go.GetComponent<Collider>();if(!collision){c.enabled=false;Destroy(c);}else if(name.Contains("landing"))go.tag="Ground";
        Finish(go,mat,size);go.GetComponent<MeshRenderer>().enabled=visible;return go;
    }
    void Finish(GameObject go,Material mat,Vector3 scale)
    {
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;
        if(!mat.HasProperty("_ObjectScale"))return;
        var b=new MaterialPropertyBlock();b.SetVector("_ObjectScale",scale);b.SetFloat("_EdgeWidth",go.GetComponent<MeshFilter>().sharedMesh.name=="Cube"?.012f:0);b.SetFloat("_GrainAxis",1);if(go.name=="Cellar oak plank")b.SetVector("_ObjectScale",new Vector3(1,.22f,1));r.SetPropertyBlock(b);
    }
    GameObject Solid(string name,Vector2[] p,float front,float depth,Material mat)
    {
        var v=new List<Vector3>();var t=new List<int>();
        void Tri(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.AddRange(new[]{a,b,c});t.AddRange(new[]{n,n+1,n+2});}
        Vector3 P(int i,float z)=>new Vector3(p[i].x,p[i].y,z);
        for(int i=1;i<p.Length-1;i++){Tri(P(0,front),P(i+1,front),P(i,front));Tri(P(0,front+depth),P(i,front+depth),P(i+1,front+depth));}
        for(int i=0;i<p.Length;i++){int j=(i+1)%p.Length;Tri(P(i,front),P(j,front),P(j,front+depth));Tri(P(i,front),P(j,front+depth),P(i,front+depth));}
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;Finish(go,mat,Vector3.one);return go;
    }
    void Excavate()
    {
        var center=transform.position;
        foreach(var f in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(f.name!="Courtyard walkable ground" && f.name!="Rolling courtyard meadow" && f.name!="Winding courtyard path")continue;
            var bounds=f.GetComponent<Renderer>().bounds;
            if(bounds.max.x<center.x-Half || bounds.min.x>center.x+Half || bounds.max.z<center.z+Front || bounds.min.z>center.z+Back)continue;
            Cut(f,center);
        }
    }
    struct Vertex
    {
        public Vector3 p,n;public Vector2 uv;public Color color;
        public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t),color=Color.Lerp(a.color,b.color,t)};
    }
    void Cut(MeshFilter filter,Vector3 center)
    {
        var original=filter.sharedMesh;var p=original.vertices;var normals=original.normals;var uv=original.uv;var colors=original.colors;
        var vertices=new List<Vector3>();var ns=new List<Vector3>();var uvs=new List<Vector2>();var cs=new List<Color>();var triangles=new List<int>();
        float[] edges={center.x-Half,center.x+Half,center.z+Front,center.z+Back};
        float Distance(Vertex v,int edge){var world=filter.transform.TransformPoint(v.p);return (edge<2?world.x:world.z)-edges[edge];}
        List<Vertex> Clip(List<Vertex> polygon,int edge,bool inside)
        {
            var result=new List<Vertex>();if(polygon.Count==0)return result;
            float Sign(Vertex v)=>Distance(v,edge)*(edge%2==0?1:-1)*(inside?1:-1);
            var a=polygon[polygon.Count-1];float da=Sign(a);
            foreach(var b in polygon){float db=Sign(b);if((da>=0)!=(db>=0))result.Add(Vertex.Lerp(a,b,da/(da-db)));if(db>=0)result.Add(b);a=b;da=db;}
            return result;
        }
        void Emit(List<Vertex> polygon)
        {
            for(int j=1;j<polygon.Count-1;j++)foreach(var v in new[]{polygon[0],polygon[j],polygon[j+1]})
            {triangles.Add(vertices.Count);vertices.Add(v.p);ns.Add(v.n);uvs.Add(v.uv);cs.Add(v.color);}
        }
        var indices=original.triangles;
        for(int i=0;i<indices.Length;i+=3)
        {
            var polygon=new List<Vertex>();for(int j=0;j<3;j++){int k=indices[i+j];polygon.Add(new Vertex{p=p[k],n=normals.Length==p.Length?normals[k]:Vector3.up,uv=uv.Length==p.Length?uv[k]:Vector2.zero,color=colors.Length==p.Length?colors[k]:Color.white});}
            for(int edge=0;edge<4 && polygon.Count>0;edge++){Emit(Clip(polygon,edge,false));polygon=Clip(polygon,edge,true);}
        }
        var mesh=new Mesh{name=original.name+" cellar opening",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(ns);mesh.SetUVs(0,uvs);mesh.SetColors(cs);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();meshes.Add(mesh);filter.sharedMesh=mesh;
        var collider=filter.GetComponent<Collider>();
        if(collider!=null){collider.enabled=false;var replacement=filter.gameObject.AddComponent<MeshCollider>();replacement.sharedMesh=mesh;}
    }
    void OnDestroy(){if(stairFriction!=null)Destroy(stairFriction);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);}
}
