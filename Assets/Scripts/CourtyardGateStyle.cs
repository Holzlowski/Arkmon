using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Dress the existing eastern crossing; keep the authored walkable deck and approach intact.
[DefaultExecutionOrder(9506)]
public sealed class CourtyardGateStyle : MonoBehaviour
{
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Material> materials = new List<Material>();
    private readonly List<Transform> plants = new List<Transform>();
    private Material oak, iron, stone, bank;
    private readonly List<(MeshRenderer renderer,Vector4 original)> cutaway=new List<(MeshRenderer,Vector4)>();
    private MaterialPropertyBlock cutawayBlock;
    private Transform player;
    private static readonly int Cutaway=Shader.PropertyToID("_ViewCutaway");
    internal static void Install()
    {
        if (GameObject.Find("Illustrated east crossing") != null || GameObject.Find("East drawbridge deck") == null) return;
        var root = new GameObject("Illustrated east crossing");
        root.AddComponent<CourtyardGateStyle>().Build();
    }
    private Material Paint(string resource, string name, Color tint)
    {
        var material = new Material(Resources.Load<Material>("CourtyardStyle/" + resource)) { name = name };
        material.SetColor("_BaseColor", tint); materials.Add(material); return material;
    }
    private void Build()
    {
        cutawayBlock=new MaterialPropertyBlock();
        oak = Paint("Stable oak", "Drawbridge illustrated oak", new Color(.60f, .69f, .78f));
        oak.SetFloat("_TextureScale", .24f);
        stone = Paint("Plaster", "Gate dressed limestone", new Color(.76f, .73f, .66f));
        bank = Paint("Plaster", "Stream mossy bank", new Color(.43f, .39f, .23f));
        iron = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Drawbridge wrought iron" };
        iron.SetColor("_BaseColor", new Color(.22f, .19f, .13f)); iron.SetFloat("_Metallic", .25f); iron.SetFloat("_Smoothness", .24f); materials.Add(iron);
        foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            switch (renderer.name)
            {
                case "East drawbridge deck": case "Drawbridge plank": case "Drawbridge edge beam": Surface(renderer, oak); break;
                case "Drawbridge iron strap": case "Gate chain anchor": Surface(renderer, iron); break;
                case "Drawbridge chain link": case "Stream bank rushes": case "Rounded stream stone": case "Stream surface glint":
                    renderer.gameObject.SetActive(false); break;
                case "Exterior stream": case "Stream bank below the grass": renderer.enabled = false; break;
            }
        }
        Arch();
        foreach (float z in new[] { -3.2f, 1.2f })
        {
            Box("Gate carved pier foot", new Vector3(43.4f,.2f,z), new Vector3(2.5f,.4f,.85f),stone);
            Box("Gate carved spring cap", new Vector3(43.4f,3.25f,z),new Vector3(2.5f,.23f,.85f),stone);
            Chain(z + (z < 0 ? .1f : -.1f));
        }
        Box("Gate arch keystone", new Vector3(43.45f,5.5f,-1),new Vector3(2.5f,.7f,.46f),stone);
        foreach (float x in new[] {43.45f,50.48f})
            for (int i=0;i<9;i++) Box("Drawbridge strap rivet",new Vector3(x,.08f,-2.85f+i*.46f),new Vector3(.07f,.04f,.07f),iron);
        Stream(); BankPlants(); BankStones(); BridgeWear();
        player=GameObject.FindWithTag("Player")?.transform;
        foreach(var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            bool gatePart=renderer.transform.IsChildOf(transform) || renderer.name=="Gate stone pier"
                || renderer.name=="East wall south of gate" || renderer.name=="East wall north of gate";
            if(!gatePart || renderer.sharedMaterial==null || !renderer.sharedMaterial.HasProperty(Cutaway))continue;
            renderer.GetPropertyBlock(cutawayBlock);
            cutaway.Add((renderer,cutawayBlock.HasVector(Cutaway)?cutawayBlock.GetVector(Cutaway):Vector4.zero));
        }
    }
    private void Arch()
    {
        var lintel=GameObject.Find("East gate lintel").GetComponent<MeshRenderer>();
        var masonry=lintel.sharedMaterial;lintel.enabled=false;
        foreach(var old in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if(old.name=="Gate arch voussoir")old.enabled=false;
        const float inner=1.85f,outer=2.23f,spring=3.25f;
        for(int i=0;i<19;i++)
        {
            float a=i*Mathf.PI/19+.004f,b=(i+1)*Mathf.PI/19-.004f;
            Extrude("East gate carved arch",new[]{
                new Vector2(-1+Mathf.Cos(a)*inner,spring+Mathf.Sin(a)*inner),
                new Vector2(-1+Mathf.Cos(b)*inner,spring+Mathf.Sin(b)*inner),
                new Vector2(-1+Mathf.Cos(b)*outer,spring+Mathf.Sin(b)*outer),
                new Vector2(-1+Mathf.Cos(a)*outer,spring+Mathf.Sin(a)*outer)},42.18f,44.62f,stone);
        }
        // Fill above the curve, replacing the old rectangular lintel's cut-through silhouette.
        for(int i=0;i<48;i++)
        {
            float a=-2.5f+i*5f/48,b=-2.5f+(i+1)*5f/48;
            float ya=spring+Mathf.Sqrt(Mathf.Max(0,outer*outer-a*a));
            float yb=spring+Mathf.Sqrt(Mathf.Max(0,outer*outer-b*b));
            Extrude("East gate arch masonry",new[]{new Vector2(a-1,ya),new Vector2(b-1,yb),new Vector2(b-1,8),new Vector2(a-1,8)},42,44,masonry);
        }
    }
    private void Extrude(string name,Vector2[] polygon,float front,float back,Material material)
    {
        float area=0;
        for(int i=0;i<polygon.Length;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Length];area+=a.x*b.y-b.x*a.y;}
        if(area<0)System.Array.Reverse(polygon);
        var v=new List<Vector3>();var t=new List<int>();
        Vector3 P(int i,float x)=>new Vector3(x,polygon[i].y,polygon[i].x);
        void Tri(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.AddRange(new[]{a,b,c});t.AddRange(new[]{n,n+1,n+2});}
        for(int i=1;i<polygon.Length-1;i++){Tri(P(0,front),P(i,front),P(i+1,front));Tri(P(0,back),P(i+1,back),P(i,back));}
        for(int i=0;i<polygon.Length;i++){int j=(i+1)%polygon.Length;Tri(P(i,front),P(j,back),P(j,front));Tri(P(i,front),P(i,back),P(j,back));}
        Emit(name,v,t,null,material);
    }
    private void Surface(MeshRenderer renderer, Material material)
    {
        renderer.sharedMaterial=material;
        if (!material.HasProperty("_ObjectScale")) return;
        var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
        block.SetVector("_ObjectScale",renderer.transform.lossyScale); block.SetFloat("_GrainAxis",renderer.name=="Drawbridge plank"?2:0);
        block.SetFloat("_EdgeWidth",.012f); renderer.SetPropertyBlock(block);
    }
    private void Box(string name, Vector3 at, Vector3 size, Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(transform,false);
        go.transform.position=at; go.transform.localScale=size;
        var collider=go.GetComponent<Collider>(); collider.enabled=false; Destroy(collider);
        Surface(go.GetComponent<MeshRenderer>(),material);
    }
    private void Chain(float z)
    {
        var from=new Vector3(44.55f,3.9f,z); var to=new Vector3(50.45f,.25f,z);
        var along=(to-from).normalized; var sideways=Vector3.forward; var other=Vector3.Cross(along,sideways).normalized;
        var v=new List<Vector3>(); var t=new List<int>();
        const int links=26,around=16,tube=6;
        for(int link=0;link<links;link++)
        {
            Vector3 centre=Vector3.Lerp(from,to,link/(float)(links-1));
            var side=link%2==0?sideways:other;
            var normal=Vector3.Cross(along,side).normalized;
            int start=v.Count;
            for(int a=0;a<around;a++)
            {
                float angle=a*Mathf.PI*2/around;
                var radial=(along*Mathf.Cos(angle)+side*Mathf.Sin(angle)).normalized;
                var ring=centre+along*(Mathf.Cos(angle)*.20f)+side*(Mathf.Sin(angle)*.095f);
                for(int b=0;b<tube;b++)
                {
                    float q=b*Mathf.PI*2/tube;
                    v.Add(ring+(radial*Mathf.Cos(q)+normal*Mathf.Sin(q))*.025f);
                    int n=start+a*tube+b, next=start+((a+1)%around)*tube+b;
                    int nb=start+a*tube+(b+1)%tube, nextb=start+((a+1)%around)*tube+(b+1)%tube;
                    t.AddRange(new[]{n,next,nb,nb,next,nextb});
                }
            }
        }
        Emit("Drawbridge linked chain",v,t,null,iron);
    }
    private static float Centre(float z) => 47.0f + Mathf.Sin((z+1)*.16f)*.42f + Mathf.Sin(z*.43f)*.12f;
    private static float HalfWidth(float z) => 1.80f + Mathf.Sin(z*.27f)*.18f + Mathf.Cos(z*.61f)*.08f;
    private static float FarLip(float z) => 51.0f + Mathf.Sin(z*.31f)*.48f + Mathf.Cos(z*.74f)*.18f;
    private void Stream()
    {
        var water=new Material(Resources.Load<Shader>("CourtyardStream")){name="Flowing courtyard stream"};materials.Add(water);
        const int rows=121;
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int row=0;row<rows;row++)
        {
            float z=-27+row*100f/(rows-1);
            for(int col=0;col<2;col++)
            {
                v.Add(new Vector3(Centre(z)+(col==0?-1:1)*HalfWidth(z),-.755f,z));uv.Add(new Vector2(col,z));
                if(row>0 && col==1){int n=v.Count-1;t.AddRange(new[]{n-3,n-1,n-2,n-2,n-1,n});}
            }
        }
        Emit("Flowing exterior stream",v,t,uv,water);
        for(int side=-1;side<=1;side+=2)
        {
            v=new List<Vector3>();t=new List<int>();
            for(int row=0;row<rows;row++)
            {
                float z=-27+row*100f/(rows-1),edge=Centre(z)+side*HalfWidth(z);
                // Fold the far slope over the solid bank's straight face, then blend onto its top.
                // The deck and bank colliders keep their established heights and footprint.
                float outer=side<0?43.62f:FarLip(z);
                float crest=side<0?43.72f:49.88f+.09f*Mathf.Sin(z*.55f);
                v.Add(new Vector3(edge,-.81f,z));
                v.Add(new Vector3(Mathf.Lerp(edge,crest,.58f),-.26f+.035f*Mathf.Sin(z*.7f),z));
                v.Add(new Vector3(crest,.025f,z));v.Add(new Vector3(outer,.008f,z));
                if(row==0)continue;
                for(int c=1;c<4;c++)
                {
                    int n=row*4+c,p=n-4;
                    if(side>0)t.AddRange(new[]{p-1,n-1,p,p,n-1,n});else t.AddRange(new[]{p-1,p,n-1,p,n,n-1});
                }
            }
            Emit("Stream sloping bank",v,t,null,bank);
        }
    }
    private void BankStones()
    {
        for(int i=0;i<16;i++)
        {
            float z=-16+i*3.5f;
            if(z>-5 && z<3)continue;
            float side=i%2==0?1:-1;
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Stream worn bank stone";
            go.transform.SetParent(transform,false);
            go.transform.position=new Vector3(Centre(z)+side*(HalfWidth(z)+.22f),-.55f,z);
            go.transform.localScale=new Vector3(.6f+(i%3)*.17f,.35f,.48f+(i%2)*.15f);
            go.transform.rotation=Quaternion.Euler(8,i*47,6);
            var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);
            Surface(go.GetComponent<MeshRenderer>(),stone);
        }
    }
    private void BridgeWear()
    {
        var worn=Paint("Stable oak","Drawbridge pale worn wood",new Color(.76f,.76f,.69f));
        worn.SetFloat("_TextureScale",.32f);
        // Thin scuffs and fastening plates are visual only: the deck remains a single reliable surface.
        for(int i=0;i<12;i++)
        {
            float x=43.6f+i*.58f;
            for(int side=-1;side<=1;side+=2)
                Box("Drawbridge worn footfall",new Vector3(x,.040f,-1+side*.62f+.10f*Mathf.Sin(i*2)),
                    new Vector3(.17f+(i%3)*.04f,.008f,.36f+(i%2)*.18f),worn);
        }
        foreach(float x in new[]{43.5f,50.45f})foreach(float z in new[]{-2.85f,.85f})
        {
            Box("Drawbridge iron end shoe",new Vector3(x,.04f,z),new Vector3(.28f,.06f,.36f),iron);
            Box("Drawbridge shoe bolt",new Vector3(x,.082f,z),new Vector3(.075f,.025f,.075f),stone);
        }
    }
    private void Emit(string name,List<Vector3> vertices,List<int> triangles,List<Vector2> uv,Material material)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);if(uv!=null)mesh.SetUVs(0,uv);
        mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    private void BankPlants()
    {
        var random=new System.Random(480);
        // Small, separated riparian groups; the bridge and its future arrival space stay open.
        for(int group=0;group<17;group++)
        {
            float z=-19+group*2.6f+(float)random.NextDouble()*.9f;
            if(z>-5.2f && z<3.2f)continue;
            bool far=group%3!=0;
            for(int i=0;i<4;i++)
            {
                float dz=z+((float)random.NextDouble()-.5f)*1.4f;
                float x=far?FarLip(dz)+(float)random.NextDouble()*.48f:43.67f+(float)random.NextDouble()*.07f;
                string kind=i==0 && far?"Pebbles":i==1?"Fern":"Grass";
                var sprite=Resources.Load<Sprite>("CourtyardStyle/"+kind);if(sprite==null)continue;
                var go=new GameObject("Illustrated streamside plant",typeof(SpriteRenderer));go.transform.SetParent(transform,false);
                go.transform.position=new Vector3(x,far?.025f:.015f,dz);
                float height=kind=="Pebbles"?.23f:.30f+(float)random.NextDouble()*.25f;
                go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
                var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.flipX=i%2==0;
                renderer.color=kind=="Pebbles"?new Color(.91f,.88f,.77f):new Color(.86f,.94f,.72f);plants.Add(go.transform);
            }
        }
    }
    private void LateUpdate()
    {
        if(Camera.main!=null)foreach(var plant in plants)plant.rotation=Camera.main.transform.rotation;
        if(player==null)return;
        var center=player.position+Vector3.up*.15f;
        float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(40,42,center.x))
            *(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(51,54,center.x)));
        var value=new Vector4(center.x,center.y,center.z,1.35f*blend);
        foreach(var part in cutaway)
        {
            if(part.renderer==null)continue;
            part.renderer.GetPropertyBlock(cutawayBlock);cutawayBlock.SetVector(Cutaway,value);part.renderer.SetPropertyBlock(cutawayBlock);
        }
    }
    private void OnDisable()
    {
        foreach(var part in cutaway)
        {
            if(part.renderer==null)continue;
            part.renderer.GetPropertyBlock(cutawayBlock);cutawayBlock.SetVector(Cutaway,part.original);part.renderer.SetPropertyBlock(cutawayBlock);
        }
    }
    private void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);}
}
