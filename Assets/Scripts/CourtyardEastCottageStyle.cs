using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Local dressing of the eastern cottage; its authored solid and neighboring routes remain intact.
public sealed class CourtyardEastCottageStyle : MonoBehaviour
{
    readonly List<Material> ownedMaterials=new List<Material>();
    readonly List<Transform> herbs=new List<Transform>();
    Material oak,plaster,stone,roof,sage,glass,iron;
    Mesh earthMesh;

    internal static void Install()
    {
        var house=GameObject.Find("East cottage")?.transform;
        if(house==null || house.Find("Illustrated east cottage")!=null)return;
        var oak=Resources.Load<Material>("CourtyardStyle/Oak");
        var plaster=Resources.Load<Material>("CourtyardStyle/Plaster");
        var stone=Resources.Load<Material>("CourtyardStyle/Stone");
        var roof=Resources.Load<Material>("CourtyardStyle/Stable roof");
        var sage=Resources.Load<Material>("CourtyardStyle/Garden sage");
        if(oak==null || plaster==null || stone==null || roof==null || sage==null)return;
        var holder=new GameObject("Illustrated east cottage");holder.transform.SetParent(house,false);
        var style=holder.AddComponent<CourtyardEastCottageStyle>();
        style.oak=oak;style.plaster=plaster;style.stone=stone;style.roof=roof;style.sage=sage;
        style.glass=style.Plain("East cottage recessed glass",new Color(.075f,.13f,.15f));
        style.iron=style.Plain("East cottage aged iron",new Color(.17f,.15f,.12f));
        foreach(var renderer in house.GetComponentsInChildren<MeshRenderer>())
        {
            string n=renderer.name;
            var material=n=="East cottage structure" || n=="Painted solid gable"?plaster:n=="Painted roof slope"?roof:oak;
            Assign(renderer,material);
        }
        // Legacy facade details are direct courtyard children, not children of their house.
        var courtyard=house.parent;
        foreach(Transform part in courtyard)
        {
            if(part.name.StartsWith("Cottage ") && Mathf.Abs(part.position.x-house.position.x)<3 && Mathf.Abs(part.position.z-(house.position.z-2.5f))<.8f
                || part.name.StartsWith("East cottage stone "))
            {
                foreach(var r in part.GetComponentsInChildren<Renderer>())r.enabled=false;
                foreach(var c in part.GetComponentsInChildren<Collider>())c.enabled=false;
            }
            if(part.name=="East cottage chimney")
            {
                part.position=new Vector3(part.position.x,house.position.y+6.1f,part.position.z);
                part.localScale=new Vector3(.63f,2.1f,.67f);Assign(part.GetComponent<MeshRenderer>(),stone);
                style.Box("East cottage chimney cap",house.InverseTransformPoint(part.position)+Vector3.up*1.1f,new Vector3(.85f,.16f,.87f),stone);
            }
        }
        style.Build();Physics.SyncTransforms();
    }
    Material Plain(string name,Color color)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.18f);ownedMaterials.Add(material);return material;
    }
    static void Assign(MeshRenderer renderer,Material material)
    {
        renderer.sharedMaterial=material;
        if(!material.HasProperty("_ObjectScale"))return;
        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
        var scale=renderer.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        block.SetVector("_ObjectScale",scale);block.SetFloat("_GrainAxis",scale.x>scale.y && scale.x>scale.z?0:scale.z>scale.y?2:1);
        block.SetFloat("_EdgeWidth",renderer.GetComponent<MeshFilter>().sharedMesh.name=="Cube"?.016f:0);
        if(renderer.name=="Painted roof slope")block.SetFloat("_TextureScale",.48f);
        renderer.SetPropertyBlock(block);
    }
    void Build()
    {
        Box("East cottage front stone footing",new Vector3(0,.24f,-2.54f),new Vector3(5.6f,.48f,.20f),stone);
        foreach(int side in new[]{-1,1})
        {
            Box("East cottage side stone footing",new Vector3(side*2.77f,.24f,0),new Vector3(.20f,.48f,5.05f),stone);
            Box("East cottage eave beam",new Vector3(0,3.78f,side*2.67f),new Vector3(5.9f,.17f,.22f),oak);
            Box("East cottage side timber",new Vector3(side*2.79f,1.92f,0),new Vector3(.17f,3.8f,.16f),oak);
            Window(new Vector3(side*1.65f,1.82f,-2.56f),Quaternion.identity);
        }
        Window(new Vector3(-2.79f,1.85f,.15f),Quaternion.Euler(0,90,0));
        Box("East cottage door recess",new Vector3(0,1.18f,-2.56f),new Vector3(1.4f,2.36f,.13f),glass);
        for(int i=0;i<7;i++)Box("East cottage oak door plank",new Vector3(-.57f+i*.19f,1.18f,-2.65f),new Vector3(.18f,2.26f,.12f),oak);
        foreach(int side in new[]{-1,1})Box("East cottage carved door jamb",new Vector3(side*.74f,1.22f,-2.67f),new Vector3(.18f,2.44f,.34f),stone);
        Box("East cottage carved lintel",new Vector3(0,2.45f,-2.68f),new Vector3(1.68f,.20f,.36f),stone);
        foreach(float y in new[]{.42f,1.95f})Box("East cottage forged hinge",new Vector3(0,y,-2.735f),new Vector3(1.16f,.08f,.04f),iron);
        Box("East cottage latch plate",new Vector3(.40f,1.13f,-2.75f),new Vector3(.12f,.24f,.05f),iron);
        Box("East cottage door handle",new Vector3(.40f,1.13f,-2.80f),new Vector3(.055f,.16f,.05f),stone);
        // The visible closed door has a matching solid, with a clear level approach.
        Box("East cottage closed door collision",new Vector3(0,1.18f,-2.62f),new Vector3(1.4f,2.36f,.24f),oak,true,false);
        Box("East cottage low threshold",new Vector3(0,.035f,-2.96f),new Vector3(1.65f,.07f,.55f),stone);
        var canopy=Box("East cottage shingled canopy",new Vector3(0,2.73f,-3.02f),new Vector3(2.05f,.13f,.98f),roof);
        canopy.transform.localRotation=Quaternion.Euler(-13,0,0);
        foreach(int side in new[]{-1,1})
        {
            var bracket=Box("East cottage canopy bracket",new Vector3(side*.79f,2.48f,-2.91f),new Vector3(.10f,.54f,.10f),oak);
            bracket.transform.localRotation=Quaternion.Euler(-38,0,0);
        }
        Earth();
        for(int i=0;i<7;i++)
        {
            var point=OnGround(new Vector3(.15f*Mathf.Sin(i*1.5f),0,-3.35f-i*.57f)) + Vector3.up*.028f;
            var step=Box("East cottage approach stone",point,new Vector3(1.32f-i*.035f,.055f,.47f),stone);
            float dx=(OnGround(point+Vector3.right*.2f).y-OnGround(point-Vector3.right*.2f).y)/.4f;
            float dz=(OnGround(point+Vector3.forward*.2f).y-OnGround(point-Vector3.forward*.2f).y)/.4f;
            step.transform.localRotation=Quaternion.FromToRotation(Vector3.up,new Vector3(-dx,1,-dz).normalized)*Quaternion.Euler(0,i%2==0?6:-8,0);
        }
        foreach(int side in new[]{-1,1})
        {
            Plant(new Vector3(side*2.72f,.025f,-2.85f),.64f,"Fern");
            Plant(new Vector3(side*2.98f,.025f,-2.35f),.4f,"Clover");
            Plant(new Vector3(side*2.42f,.025f,-3.03f),.3f,"Grass");
        }
    }
    void Window(Vector3 at,Quaternion rotation)
    {
        var holder=new GameObject("East cottage framed window").transform;holder.SetParent(transform,false);holder.localPosition=at;holder.localRotation=rotation;
        void Part(string name,Vector3 position,Vector3 size,Material material)
        {
            var part=Box(name,position,size,material);part.transform.SetParent(holder,false);
        }
        Part("East cottage dark window pane",new Vector3(0,0,-.03f),new Vector3(.80f,1.07f,.06f),glass);
        foreach(int side in new[]{-1,1})
        {
            Part("East cottage window frame",new Vector3(side*.46f,0,-.13f),new Vector3(.12f,1.24f,.26f),oak);
            Part("East cottage sage shutter",new Vector3(side*.70f,0,-.12f),new Vector3(.34f,1.14f,.14f),sage);
            foreach(float y in new[]{-.37f,.37f})Part("East cottage shutter brace",new Vector3(side*.70f,y,-.21f),new Vector3(.30f,.065f,.055f),oak);
        }
        Part("East cottage window head",new Vector3(0,.61f,-.13f),new Vector3(1.05f,.14f,.26f),oak);
        Part("East cottage deep window sill",new Vector3(0,-.63f,-.20f),new Vector3(1.15f,.16f,.47f),stone);
        Part("East cottage window mullion",new Vector3(0,0,-.10f),new Vector3(.055f,1.08f,.09f),oak);
        Part("East cottage window crossbar",new Vector3(0,.08f,-.10f),new Vector3(.81f,.055f,.09f),oak);
    }
    void Plant(Vector3 at,float height,string kind)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/"+kind);if(sprite==null)return;
        var plant=new GameObject("East cottage verge "+kind,typeof(SpriteRenderer));plant.transform.SetParent(transform,false);plant.transform.localPosition=OnGround(at)+Vector3.up*.02f;
        plant.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);plant.GetComponent<SpriteRenderer>().sprite=sprite;herbs.Add(plant.transform);
    }
    void Earth()
    {
        var material=Resources.Load<Material>("CourtyardStyle/Stable earth");if(material==null)return;
        const int sides=40;var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int ring=0;ring<=4;ring++)for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides,r=ring/4f,edge=1+.05f*Mathf.Sin(a*5);
            vertices.Add(OnGround(new Vector3(Mathf.Cos(a)*1.9f*r*edge,0,-4.8f+Mathf.Sin(a)*2.25f*r*edge))+Vector3.up*.019f);colors.Add(new Color(r,0,0,1));
            if(ring==0)continue;int prev=(ring-1)*sides+i,cur=ring*sides+i,next=(i+1)%sides;
            triangles.AddRange(new[]{prev,ring*sides+next,cur,prev,(ring-1)*sides+next,ring*sides+next});
        }
        earthMesh=new Mesh{name="East cottage worn forecourt"};earthMesh.SetVertices(vertices);earthMesh.SetColors(colors);earthMesh.SetTriangles(triangles,0);earthMesh.RecalculateNormals();earthMesh.RecalculateBounds();
        var patch=new GameObject("East cottage worn forecourt",typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(transform,false);patch.GetComponent<MeshFilter>().sharedMesh=earthMesh;
        var renderer=patch.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    Vector3 OnGround(Vector3 local)
    {
        var world=transform.TransformPoint(local);float height=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(world.x,20,world.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground"))height=Mathf.Max(height,hit.point.y);
        if(!float.IsNegativeInfinity(height))world.y=height;
        return transform.InverseTransformPoint(world);
    }
    GameObject Box(string name,Vector3 at,Vector3 size,Material material,bool solid=false,bool visible=true)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(transform,false);part.transform.localPosition=at;part.transform.localScale=size;
        if(!solid){var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);}
        Assign(part.GetComponent<MeshRenderer>(),material);part.GetComponent<MeshRenderer>().enabled=visible;return part;
    }
    void LateUpdate(){if(Camera.main!=null)foreach(var herb in herbs)herb.rotation=Camera.main.transform.rotation;}
    void OnDestroy(){if(earthMesh!=null)Destroy(earthMesh);foreach(var material in ownedMaterials)if(material!=null)Destroy(material);}
}
