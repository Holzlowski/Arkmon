using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A scenic access lane. Its sampled centreline also clears grain from the entrance.
public sealed class CourtyardFarmApproach : MonoBehaviour
{
    public static readonly Vector3 Front = new Vector3(69, 0, -20).normalized;
    static readonly Vector3 Right = Vector3.Cross(Vector3.up, -Front);
    public static Vector3 Gate => CourtyardDistantFarm.Site - Right;
    public static Vector3 Forecourt => Gate + Front * 1.8f;
    static readonly Vector3[] Lane = MakeLane();
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<Transform> plants = new List<Transform>();
    Material earth, paper, contact, fenceWood, straw;
    public static Vector3 HarvestCenter => Gate + Right*15 + Front*2;
    public static bool IsHarvested(Vector3 point)
    {
        var delta=point-HarvestCenter;
        return Mathf.Pow(Vector3.Dot(delta,Right)/6.4f,2)+Mathf.Pow(Vector3.Dot(delta,Front)/4.8f,2)<1;
    }
    MeadowVegetation meadow;

    static Vector3[] MakeLane()
    {
        var a = new Vector3(-123, 0, CourtyardFieldMargins.VergeZ(-123));
        var b = new Vector3(-133, 0, a.z - 1);
        var c = Gate + Front * 6;
        var d = Gate - Front * .35f;
        var points = new Vector3[41];
        for(int i=0;i<points.Length;i++)
        {
            float t=i/(float)(points.Length-1),s=1-t;
            points[i]=s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;
        }
        return points;
    }
    public static bool IsBareGround(Vector3 point)
    {
        if(point.x < -149 || point.x > -120 || point.z < 7 || point.z > 30) return false;
        var delta=point-Forecourt;delta.y=0;
        if(Mathf.Pow(Vector3.Dot(delta,Right)/3.4f,2)+Mathf.Pow(Vector3.Dot(delta,Front)/2.7f,2)<1) return true;
        for(int i=1;i<Lane.Length;i++)
        {
            var a=Lane[i-1];var segment=Lane[i]-a;var d=point-a;d.y=0;
            float t=Mathf.Clamp01(Vector3.Dot(d,segment)/segment.sqrMagnitude);
            if((d-segment*t).sqrMagnitude < 1.1f*1.1f) return true;
        }
        return false;
    }
    internal static void Install(Transform farm, MeadowVegetation meadow)
    {
        var source=Resources.Load<Material>("CourtyardStyle/Stable earth");if(source==null)return;
        var root=new GameObject("Farm access and orchard");root.transform.SetParent(farm,false);
        var style=root.AddComponent<CourtyardFarmApproach>();style.meadow=meadow;
        style.earth=new Material(source){name="Farm worn earth"};
        style.earth.SetColor("_BaseColor",new Color(.81f,.78f,.66f,.96f));
        style.earth.renderQueue=2972;style.earth.SetFloat("_WheelRuts",1);
        style.contact=new Material(Resources.Load<Shader>("CourtyardFarmContact")){name="Farm soft ground contacts"};
        style.paper=new Material(Resources.Load<Shader>("AurenfeldScenicPaper")){name="Farm orchard illustrations"};
        style.BuildLane();style.BuildForecourt();style.BuildHarvestYard();
        style.Contact(Gate-Front*.1f,9.4f,1.7f,"Farm building foot contact");
        style.Contact(Gate-Right*10.8f-Front*1.6f,2.8f,1.6f,"Orchard root contact");
        style.Fence(Gate-Right*9+Front*.5f,Gate-Right*10.4f+Front*5.2f);
        style.Fence(Gate+Right*8.6f+Front*.6f,Gate+Right*10.1f+Front*4.4f);
        style.Plant("Trees/Apple",Gate-Right*10.8f-Front*1.6f,7.6f,"Farm apple tree");
        foreach(float side in new[]{-8.4f,-6.6f,-4.5f,4.7f,6.3f,8.1f})
            style.Plant("Mixed",Gate+Right*side+Front*(.5f+Mathf.Abs(side)*.11f),.55f+Mathf.Abs(side)*.045f,"Farm fence growth");
    }
    void BuildHarvestYard()
    {
        straw=new Material(Resources.Load<Material>("CourtyardStyle/Stable straw earth")){name="Farm harvested straw ground"};
        straw.SetColor("_BaseColor",new Color(.92f,.79f,.50f,.92f));
        straw.SetFloat("_WheelRuts",0);straw.SetFloat("_FogStrength",1);
        const int sides=48,rings=8;
        var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)rings;
            v.Add(Ground(HarvestCenter+(Right*Mathf.Cos(a)*6.8f+Front*Mathf.Sin(a)*5.2f)*r*(1+.035f*Mathf.Sin(a*5))));
            c.Add(new Color(r,0,0,1));if(ring==0)continue;
            int n=ring*sides+side,previous=n-sides,next=(side+1)%sides;
            t.AddRange(new[]{previous,ring*sides+next,n,previous,(ring-1)*sides+next,ring*sides+next});
        }
        MeshPart("Farm harvested work patch",v,c,t,straw);
        var hay=Resources.Load<Sprite>("StableCare/Hay");
        if(hay==null)return;
        for(int i=0;i<3;i++)
        {
            var p=HarvestCenter+Right*(i-1)*2.3f+Front*(i==1?-.9f:1.3f);
            Contact(p,1.2f,.75f,"Farm hay contact");
            Illustration(hay,p,1.05f+i*.13f,"Farm tied hay bundle");
        }
    }
    Vector3 Ground(Vector3 p) => meadow.SurfacePoint(p)+Vector3.up*.12f;
    void BuildLane()
    {
        const int columns=9;
        var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int row=0;row<Lane.Length;row++)
        {
            var tangent=Lane[Mathf.Min(row+1,Lane.Length-1)]-Lane[Mathf.Max(0,row-1)];
            var side=Vector3.Cross(Vector3.up,tangent).normalized;
            float width=1.55f+.12f*Mathf.Sin(row*.43f);
            for(int col=0;col<columns;col++)
            {
                float across=col/(float)(columns-1)*2-1;
                v.Add(Ground(Lane[row]+side*across*width));c.Add(new Color(Mathf.Abs(across),(across+1)*.5f,0,1));
                if(row==0||col==0)continue;
                int n=v.Count-1,previous=n-columns;
                t.AddRange(new[]{previous-1,n-1,previous,previous,n-1,n});
            }
        }
        MeshPart("Farm winding access lane",v,c,t);
    }
    void BuildForecourt()
    {
        const int sides=48,rings=7;
        var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)rings;
            var p=Forecourt+(Right*Mathf.Cos(a)*4.1f+Front*Mathf.Sin(a)*3.3f)*r*(1+.05f*Mathf.Sin(a*5));
            v.Add(Ground(p)+Vector3.up*.004f);c.Add(new Color(r,0,0,1));
            if(ring==0)continue;
            int n=ring*sides+side,prev=n-sides,next=(side+1)%sides;
            t.AddRange(new[]{prev,ring*sides+next,n,prev,(ring-1)*sides+next,ring*sides+next});
        }
        MeshPart("Farm trampled forecourt",v,c,t);
    }
    void MeshPart(string label,List<Vector3> v,List<Color> c,List<int> t,Material overrideMaterial=null)
    {
        for(int i=0;i<v.Count;i++)v[i]=transform.InverseTransformPoint(v[i]);
        // Keep every face upward regardless of the centreline direction.
        for(int i=0;i<t.Count;i+=3)
            if(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).y<0)
            {int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
        var mesh=new Mesh{name=label};mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=overrideMaterial!=null?overrideMaterial:earth;renderer.shadowCastingMode=ShadowCastingMode.Off;
        if(label=="Farm trampled forecourt"){var block=new MaterialPropertyBlock();block.SetFloat("_WheelRuts",0);renderer.SetPropertyBlock(block);}
    }
    void Contact(Vector3 center,float width,float depth,string label)
    {
        const int sides=40,rings=6;
        var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)rings;
            v.Add(Ground(center+(Right*Mathf.Cos(a)*width+Front*Mathf.Sin(a)*depth)*r)+Vector3.up*.008f);
            c.Add(new Color(r,0,0,1));if(ring==0)continue;
            int n=ring*sides+side,prev=n-sides,next=(side+1)%sides;
            t.AddRange(new[]{prev,ring*sides+next,n,prev,(ring-1)*sides+next,ring*sides+next});
        }
        MeshPart(label,v,c,t,contact);
    }
    void Fence(Vector3 from,Vector3 to)
    {
        if(fenceWood==null)
        {
            var source=Resources.Load<Material>("CourtyardStyle/Stable oak");if(source==null)return;
            fenceWood=new Material(source){name="Sunlit orchard fence wood"};
            fenceWood.SetColor("_BaseColor",new Color(.95f,.86f,.69f));
        }
        var wood=fenceWood;
        for(int i=0;i<3;i++)
        {
            var at=meadow.SurfacePoint(Vector3.Lerp(from,to,i*.5f));
            Beam(at,at+Vector3.up*1.15f,.16f,wood,"Farm depth fence post");
            if(i==0)continue;
            var previous=meadow.SurfacePoint(Vector3.Lerp(from,to,(i-1)*.5f));
            foreach(float h in new[]{.40f,.88f})Beam(previous+Vector3.up*h,at+Vector3.up*h,.12f,wood,"Farm depth fence rail");
        }
        Contact((from+to)*.5f,1,3,"Farm fence contact");
    }
    void Beam(Vector3 a,Vector3 b,float width,Material wood,string label)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=label;
        Destroy(go.GetComponent<Collider>());go.transform.SetParent(transform,false);
        go.transform.position=(a+b)*.5f;go.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);
        go.transform.localScale=new Vector3(width,Vector3.Distance(a,b),width);
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=wood;
        var block=new MaterialPropertyBlock();block.SetVector("_ObjectScale",go.transform.lossyScale);renderer.SetPropertyBlock(block);
    }
    void Plant(string resource,Vector3 p,float height,string label)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/"+resource);if(sprite==null)return;
        Illustration(sprite,p,height,label);
    }
    void Illustration(Sprite sprite,Vector3 p,float height,string label)
    {
        var go=new GameObject(label,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
        float scale=height/sprite.bounds.size.y;
        go.transform.position=meadow.SurfacePoint(p)+Vector3.up*(.02f-sprite.bounds.min.y*scale);
        go.transform.localScale=Vector3.one*scale;
        var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=paper;
        renderer.color=new Color(.94f,.94f,.83f);plants.Add(go.transform);
    }
    void LateUpdate()
    {
        if(Camera.main==null)return;
        var forward=Camera.main.transform.forward;forward.y=0;
        if(forward.sqrMagnitude<.001f)return;
        var facing=Quaternion.LookRotation(forward);
        foreach(var plant in plants)plant.rotation=facing;
    }
    void OnDestroy()
    {
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        if(straw!=null)Destroy(straw);if(fenceWood!=null)Destroy(fenceWood);if(contact!=null)Destroy(contact);if(earth!=null)Destroy(earth);if(paper!=null)Destroy(paper);
    }
}
