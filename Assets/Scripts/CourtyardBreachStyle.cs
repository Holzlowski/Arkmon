using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Dress the western ruin after the shared castle palette is installed; keep the courtyard enclosed.
[DefaultExecutionOrder(9504)]
public sealed class CourtyardBreachStyle : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> materials=new List<Material>();
    readonly List<Transform> plants=new List<Transform>();
    Material stone,earth;
    readonly List<(MeshRenderer renderer,float world)> mappedWalls=new List<(MeshRenderer,float)>();
    MeadowVegetation exterior;
    readonly List<(MeshFilter filter,Mesh original)> landscape=new List<(MeshFilter,Mesh)>();
    readonly List<(Transform prop,Vector3 point)> exteriorProps=new List<(Transform,Vector3)>();
    internal static void Install()
    {
        var wall=GameObject.Find("West wall near");var boundary=GameObject.Find("Broken west wall base");
        if(wall==null || boundary==null || GameObject.Find("Illustrated west breach")!=null)return;
        var root=new GameObject("Illustrated west breach");var style=root.AddComponent<CourtyardBreachStyle>();
        style.stone=wall.GetComponent<MeshRenderer>().sharedMaterial;
        var oldNames=new HashSet<string>{"Fallen limestone","Broken masonry edge","Moss on fallen stone",
            "Breach foliage","Breach wildflower","Breach grass blade","Ivy along broken masonry"};
        foreach(var item in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if(oldNames.Contains(item.name) || item.name=="Side battlement" && item.position.x<-42 && item.position.z>3.5f && item.position.z<10.5f)
                item.gameObject.SetActive(false);
        // The low wall remains a visible physical boundary, including the gently raised inside terrain.
        boundary.transform.position=new Vector3(-43,.63f,7);
        boundary.transform.localScale=new Vector3(1.5f,1.26f,7);
        var surface=boundary.GetComponent<MeshRenderer>();var properties=new MaterialPropertyBlock();surface.GetPropertyBlock(properties);
        properties.SetVector("_ObjectScale",boundary.transform.lossyScale);surface.SetPropertyBlock(properties);
        foreach(string name in new[]{"West wall near","West wall far","Broken west wall base"})
        {
            var r=GameObject.Find(name)?.GetComponent<MeshRenderer>();if(r==null)continue;
            var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);
            style.mappedWalls.Add((r,b.GetFloat("_WorldMasonry")));
            b.SetFloat("_WorldMasonry",1);r.SetPropertyBlock(b);
        }
        style.BlendExterior();
        style.BrokenEdges();style.Rubble();style.Forecourt();style.ExteriorRubble();style.Vegetation();
        Physics.SyncTransforms();
    }
    // Close the old cut-out terrain edge and let the near meadow rise gradually towards the fields.
    public static Vector3 ExteriorPoint(Vector3 p)
    {
        p=AurenfeldLandscape.Surface(CourtyardEastFieldStyle.ExteriorPoint(p));
        if(p.x<=-74 || p.x>=-36 || Mathf.Abs(p.z-7)>=24)return p;
        float west=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-74,-45,p.x));
        float inside=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-28,-34,p.x));
        float side=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,24,Mathf.Abs(p.z-7)));
        p.y=Mathf.Lerp(p.y,-.05f,west*inside*side);return p;
    }
    void BlendExterior()
    {
        var horizon=GameObject.Find("Castle horizon and golden fields");if(horizon==null)return;
        exterior=horizon.GetComponentInChildren<MeadowVegetation>();
        foreach(var filter in horizon.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null || renderer.sharedMaterial==null || renderer.sharedMaterial.shader.name!="Arkmon/Meadow" || filter.sharedMesh==null)continue;
            var original=filter.sharedMesh;var copy=Instantiate(original);copy.name="Western fields gentle transition";
            var vertices=copy.vertices;var colors=copy.colors;
            for(int i=0;i<vertices.Length;i++)
            {
                var world=ExteriorPoint(filter.transform.TransformPoint(vertices[i]));
                vertices[i]=filter.transform.InverseTransformPoint(world);
                if(colors.Length==vertices.Length)
                    colors[i]=Color.Lerp(colors[i],CourtyardFieldMargins.GoldenGround(world),CourtyardFieldMargins.GoldenFieldWeight(world));
            }
            copy.vertices=vertices;copy.colors=colors;copy.RecalculateNormals();copy.RecalculateBounds();
            landscape.Add((filter,original));meshes.Add(copy);filter.sharedMesh=copy;
            var seamless = new Material(renderer.sharedMaterial) { name = "Aurenfeld wall-aligned landscape" };
            seamless.SetVector("_ClearArea",new Vector4(-43,-17,50,19));
            seamless.SetTexture("_HarvestCanopy",Resources.Load<Texture2D>("CourtyardStyle/PaintedWheatCanopy"));
            seamless.SetFloat("_CanopyStrength",1);
            materials.Add(seamless);renderer.sharedMaterial=seamless;
        }
        if(exterior==null)return;
        foreach(var prop in horizon.GetComponentsInChildren<Transform>())
        {
            if(prop.name!="Meadow tree" && prop.name!="Meadow wildflowers" && prop.name!="Meadow thistles")continue;
            var old=exterior.OriginalSurfacePoint(prop.position);var next=ExteriorPoint(old);
            if(Mathf.Abs(next.y-old.y)<.001f)continue;
            exteriorProps.Add((prop,prop.position));prop.position+=Vector3.up*(next.y-old.y);
        }
    }
    void BrokenEdges()
    {
        // Different heights on either side frame the opening instead of a symmetrical doorway.
        float[] heights={7.65f,5.55f,3.5f,1.8f};
        for(int side=0;side<2;side++)for(int col=0;col<heights.Length;col++)
        {
            float height=heights[col]-(side==1 && col>0?.35f:0);
            float z=side==0?3.62f+col*.59f:10.38f-col*.62f;
            int courses=Mathf.CeilToInt(height/.78f);float course=height/courses;
            for(int row=0;row<courses;row++)
                Block("Breach standing masonry",new Vector3(-43+(row%2)*.035f,course*(row+.5f),z),
                    new Vector3(1.36f,course+.025f,.76f),new Vector3(0,(row%2==0?1:-1)*3,0),side*101+col*19+row,true);
        }
        for(int i=0;i<8;i++)
            Block("Breach irregular wall crown",new Vector3(-43,1.31f+(i%3)*.045f,4.0f+i*.83f),
                new Vector3(1.42f,.30f+(i%2)*.12f,.88f),new Vector3(0,i%2==0?6:-4,0),200+i,false);
    }
    void Rubble()
    {
        var random=new System.Random(381);
        // Two piles at the fractured ends leave a clear viewing place in the middle.
        for(int pile=0;pile<2;pile++)for(int i=0;i<13;i++)
        {
            float x=-42.4f+(float)random.NextDouble()*.45f;
            float z=(pile==0?4.1f:9.5f)+((float)random.NextDouble()-.5f)*1.45f;
            var size=new Vector3(.35f+(float)random.NextDouble()*.55f,.24f+(float)random.NextDouble()*.35f,.30f+(float)random.NextDouble()*.4f);
            var ground=Ground(new Vector3(x,0,z));
            Block("Breach fallen stone",ground+Vector3.up*size.y*.42f,size,new Vector3(-8+i%3*7,i*47,pile==0?-9:8),300+pile*31+i,i%4==0);
        }
    }
    void ExteriorRubble()
    {
        var shader=Resources.Load<Shader>("CourtyardFarmContact");
        Material contact=null;
        if(shader!=null){contact=new Material(shader){name="Breach rubble contact"};materials.Add(contact);}
        var random=new System.Random(672);
        // Low fragments continue the broken courses outside, leaving the centre of the view open.
        for(int side=0;side<2;side++)for(int i=0;i<9;i++)
        {
            float spread=(float)random.NextDouble();
            var p=Ground(new Vector3(-44.1f-spread*2.3f,0,(side==0?3.7f:10.4f)+((float)random.NextDouble()-.5f)*1.6f));
            var size=new Vector3(.32f+(1-spread)*.45f,.16f+(1-spread)*.20f,.27f+(float)random.NextDouble()*.38f);
            if(contact!=null)Patch(p,size.x*.85f,size.z*.85f,contact,"Breach rubble contact");
            Block("Breach exterior stone",p+Vector3.up*size.y*.38f,size,new Vector3(i%3*6-6,i*43,side==0?-7:9),700+side*19+i,false);
        }
    }
    void Block(string name,Vector3 at,Vector3 size,Vector3 angles,int seed,bool solid)
    {
        var random=new System.Random(seed);var corners=new Vector3[8];
        for(int i=0;i<8;i++)corners[i]=new Vector3((i%2==0?-1:1)*(.45f+(float)random.NextDouble()*.05f),
            ((i/2)%2==0?-1:1)*(.43f+(float)random.NextDouble()*.07f),(i<4?-1:1)*(.45f+(float)random.NextDouble()*.05f));
        int[][] faces={new[]{0,2,3,1},new[]{4,5,7,6},new[]{0,4,6,2},new[]{1,3,7,5},new[]{0,1,5,4},new[]{2,6,7,3}};
        var vertices=new List<Vector3>();var triangles=new List<int>();
        foreach(var face in faces){int n=vertices.Count;vertices.AddRange(face.Select(i=>corners[i]));triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        var mesh=new Mesh{name="Weathered breach block"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);
        obj.transform.SetPositionAndRotation(at,Quaternion.Euler(angles));obj.transform.localScale=size;
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=stone;
        var block=new MaterialPropertyBlock();block.SetVector("_ObjectScale",size);block.SetFloat("_WorldMasonry",1);renderer.SetPropertyBlock(block);
        if(solid)obj.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    void Forecourt()
    {
        var source=Resources.Load<Material>("CourtyardStyle/Stable earth");if(source==null)return;
        earth=new Material(source){name="Breach worn golden earth"};materials.Add(earth);
        earth.SetColor("_BaseColor",new Color(.77f,.75f,.60f,.72f));
        // Overlapping soft patches form the short approach without altering the physical ground.
        Patch(new Vector3(-39.0f,0,2.0f),1.0f,1.4f,earth);
        Patch(new Vector3(-40.0f,0,4.1f),.95f,1.5f,earth);
        Patch(new Vector3(-40.6f,0,6.7f),1.30f,1.6f,earth);
        var meadow=new Material(source){name="Breach dry field transition"};materials.Add(meadow);
        meadow.SetColor("_BaseColor",new Color(.80f,.66f,.37f,.38f));
        Patch(new Vector3(-46.0f,0,6.8f),3.0f,4.2f,meadow);
        Patch(new Vector3(-49.1f,0,8.0f),2.9f,3.8f,meadow);
    }
    void Patch(Vector3 center,float rx,float rz,Material material,string label="Breach ground transition")
    {
        const int sides=40,rings=6;var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float angle=side*Mathf.PI*2/sides,r=ring/(float)rings,edge=1+.08f*Mathf.Sin(angle*3)+.035f*Mathf.Cos(angle*7);
            var point=Ground(center+new Vector3(Mathf.Cos(angle)*rx*r*edge,0,Mathf.Sin(angle)*rz*r*edge))+Vector3.up*.025f;
            v.Add(point);c.Add(new Color(r,0,0,1));if(ring==0)continue;
            int n=ring*sides+side,prev=n-sides,next=(side+1)%sides;t.AddRange(new[]{prev,ring*sides+next,n,prev,(ring-1)*sides+next,ring*sides+next});
        }
        var mesh=new Mesh{name="Soft breach ground"};mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var obj=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void Vegetation()
    {
        var random=new System.Random(927);
        // Small islands hug the fractured ends; the approach and camera corridor remain open.
        Vector3[] centers={new Vector3(-41.7f,0,3.5f),new Vector3(-40.6f,0,3.7f),
            new Vector3(-41.8f,0,10.2f),new Vector3(-40.4f,0,10.7f),
            new Vector3(-44.8f,0,3.1f),new Vector3(-46.4f,0,4.0f),
            new Vector3(-45.0f,0,11.1f),new Vector3(-47.4f,0,10.9f),
            new Vector3(-50.0f,0,2.8f),new Vector3(-50.5f,0,12.2f)};
        for(int group=0;group<centers.Length;group++)for(int i=0;i<9;i++)
        {
            float angle=(float)random.NextDouble()*Mathf.PI*2;
            float radius=Mathf.Sqrt((float)random.NextDouble());
            var p=Ground(centers[group]+new Vector3(Mathf.Cos(angle)*radius*.85f,0,Mathf.Sin(angle)*radius*.65f));
            bool outside=group>=4;
            if(!outside && Physics.OverlapSphere(p+Vector3.up*.2f,.24f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.CompareTag("Ground")))continue;
            string kind=outside?"Grass":i%4==0?"Clover":"Grass";
            var color=outside?new Color(.94f,.77f,.43f):new Color(.77f,.80f,.48f);
            Plant(kind,p,.20f+(float)random.NextDouble()*(outside?.24f:.16f),group*9+i,color);
        }
    }
    void Plant(string kind,Vector3 p,float height,int index,Color color)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/"+kind);if(sprite==null)return;
        var obj=new GameObject("Breach illustrated "+kind,typeof(SpriteRenderer));obj.transform.SetParent(transform,false);
        obj.transform.position=p+Vector3.up*.025f;obj.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.flipX=index%2==0;renderer.color=color;
        plants.Add(obj.transform);
    }
    Vector3 Ground(Vector3 p)
    {
        float height=p.x<-44.5f && exterior!=null?exterior.SurfacePoint(p).y:-.05f;
        foreach(var hit in Physics.RaycastAll(new Vector3(p.x,20,p.z),Vector3.down,45,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground") || hit.collider.name=="Meadow beyond the west breach")height=Mathf.Max(height,hit.point.y);
        p.y=height;return p;
    }
    void LateUpdate(){if(Camera.main!=null)foreach(var plant in plants)plant.rotation=Camera.main.transform.rotation;}
    void OnDestroy()
    {
        foreach(var item in mappedWalls)if(item.renderer!=null)
        {var b=new MaterialPropertyBlock();item.renderer.GetPropertyBlock(b);b.SetFloat("_WorldMasonry",item.world);item.renderer.SetPropertyBlock(b);}
        foreach(var item in landscape)if(item.filter!=null)item.filter.sharedMesh=item.original;
        foreach(var item in exteriorProps)if(item.prop!=null)item.prop.position=item.point;
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);
    }
}
