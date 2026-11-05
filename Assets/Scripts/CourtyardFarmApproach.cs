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
    Material earth, paper;
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
        style.earth.renderQueue=2972;
        style.paper=new Material(Shader.Find("Sprites/Default")){name="Farm orchard illustrations"};
        style.BuildLane();style.BuildForecourt();
        style.Plant("Trees/Apple",Gate-Right*10.8f-Front*1.6f,7.6f,"Farm apple tree");
        foreach(float side in new[]{-8.4f,-6.6f,-4.5f,4.7f,6.3f,8.1f})
            style.Plant("Mixed",Gate+Right*side+Front*.25f,.55f+Mathf.Abs(side)*.045f,"Farm fence growth");
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
                v.Add(Ground(Lane[row]+side*across*width));c.Add(new Color(Mathf.Abs(across),0,0,1));
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
    void MeshPart(string label,List<Vector3> v,List<Color> c,List<int> t)
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
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=earth;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void Plant(string resource,Vector3 p,float height,string label)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/"+resource);if(sprite==null)return;
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
        if(earth!=null)Destroy(earth);if(paper!=null)Destroy(paper);
    }
}
