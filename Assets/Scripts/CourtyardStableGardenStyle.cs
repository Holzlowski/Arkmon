using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// A narrow garden connection and low planting, leaving the horse-care stations unobstructed.
public sealed class CourtyardStableGardenStyle : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Transform> plants=new List<Transform>();
    readonly List<Renderer> hidden=new List<Renderer>();
    readonly List<Vector3> route=new List<Vector3>();
    Material earth;
    IInteractableAsPlayer[] interactions;
    Bounds[] beds;
    internal static void Install()
    {
        if(GameObject.Find("Stable garden connection")!=null || GameObject.Find("Stable")==null)return;
        var source=Resources.Load<Material>("CourtyardStyle/Stable earth");if(source==null)return;
        var style=new GameObject("Stable garden connection").AddComponent<CourtyardStableGardenStyle>();
        style.earth=new Material(source){name="Stable garden footpath earth"};
        style.earth.SetColor("_BaseColor",new Color(.62f,.70f,.79f,.72f));
        style.interactions=FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IInteractableAsPlayer>().ToArray();
        style.beds=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed").Select(r=>r.bounds).ToArray();
        var a=new Vector3(-10.8f,0,.50f);var b=new Vector3(-7.5f,0,-2.0f);
        var c=new Vector3(-8.6f,0,-5.1f);var d=new Vector3(-6.5f,0,-5.9f);
        for(int i=0;i<=56;i++)
        {float t=i/56f,u=1-t;style.route.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}
        foreach(var f in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(f.name!="Curved courtyard path" || f.sharedMesh==null)continue;
            var v=f.sharedMesh.vertices;if(v.Length<17)continue;
            var first=f.transform.TransformPoint((v[0]+v[16])*.5f);
            if(Vector2.Distance(new Vector2(first.x,first.z),new Vector2(-16,-6))>.2f)continue;
            var r=f.GetComponent<Renderer>();if(r!=null && r.enabled){style.hidden.Add(r);r.enabled=false;}
        }
        Physics.SyncTransforms();style.RetireObscuringPlants();style.Footpath();style.PlantMargins();
    }
    static Vector3 Ground(Vector3 p)
    {
        float y=0;
        foreach(var hit in Physics.RaycastAll(new Vector3(p.x,20,p.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground") && hit.normal.y>.5f)y=Mathf.Max(y,hit.point.y);
        p.y=y;return p;
    }
    bool CareOrPath(Vector3 p)
    {
        // Both greeting points and Hannes' brushing silhouette need an open foreground.
        if(p.x> -14.6f && p.x< -7.0f && p.z> -.9f && p.z<3.25f)return true;
        foreach(var center in route){var d=center-p;d.y=0;if(d.sqrMagnitude<1.12f*1.12f)return true;}
        return false;
    }
    void RetireObscuringPlants()
    {
        foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if(!r.enabled)continue;
            bool decorative=r.name.StartsWith("Verge ") || r.name=="Path edge grass" || r.name=="Windblown grass";
            if(decorative && CareOrPath(r.transform.position)){hidden.Add(r);r.enabled=false;}
        }
    }
    bool Clear(Vector3 p)
    {
        if(CareOrPath(p))return false;
        foreach(var target in interactions)
        {var d=target.GetTransform()-p;d.y=0;if(d.sqrMagnitude<1.55f*1.55f)return false;}
        foreach(var bed in beds)
            if(p.x>bed.min.x-.5f && p.x<bed.max.x+.5f && p.z>bed.min.z-.5f && p.z<bed.max.z+.5f)return false;
        // Leave the established main path, cottage approach and castle route legible.
        if(p.z< -2.6f || p.x>0 && p.x<3.1f && p.z>0.9f)return false;
        return !Physics.OverlapSphere(p+Vector3.up*.25f,.35f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.CompareTag("Ground"));
    }
    void Footpath()
    {
        const int across=13;var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int row=0;row<route.Count;row++)
        {
            var along=(route[Mathf.Min(row+1,route.Count-1)]-route[Mathf.Max(0,row-1)]).normalized;
            var side=new Vector3(along.z,0,-along.x);float width=.70f*(1+.07f*Mathf.Sin(row*.38f));
            for(int col=0;col<across;col++)
            {
                float x=col/6f-1;vertices.Add(Ground(route[row]+side*x*width)+Vector3.up*.055f);
                float end=Mathf.Max(1-row/5f,1-(route.Count-1-row)/5f);
                colors.Add(new Color(Mathf.Max(Mathf.Abs(x),end),0,0,1));
                if(row==0 || col==0)continue;int n=vertices.Count-1;
                triangles.AddRange(new[]{n-across-1,n-1,n-across,n-across,n-1,n});
            }
        }
        var mesh=new Mesh{name="Worn stable garden footpath"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=earth;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void PlantMargins()
    {
        var random=new System.Random(1864);
        for(int row=6;row<route.Count-5;row+=6)
        {
            var tangent=(route[row+1]-route[row-1]).normalized;var side=new Vector3(tangent.z,0,-tangent.x);
            for(int flank=-1;flank<=1;flank+=2)for(int i=0;i<4;i++)
            {
                var p=Ground(route[row]+side*flank*(1.3f+(float)random.NextDouble()*.35f)+tangent*((float)random.NextDouble()-.5f)*.65f);
                if(!Clear(p))continue;
                string kind=i==0 && row%12==0?"Flowers":i==1?"Grass":"Clover";
                Plant(kind,p,kind=="Grass"?.32f:.21f,i+row);
            }
        }
        // Small tufts flank the trough, not the horses' faces or the care foreground.
        foreach(var p in new[]{new Vector3(-7,0,4.6f),new Vector3(-6.4f,0,3.9f),new Vector3(-15.4f,0,2.8f)})
            if(Clear(Ground(p)))Plant("Grass",Ground(p),.36f,1);
    }
    void Plant(string kind,Vector3 p,float height,int seed)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/"+kind);if(sprite==null)return;
        var go=new GameObject("Stable garden "+kind,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
        go.transform.position=p+Vector3.up*.03f;go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        var r=go.GetComponent<SpriteRenderer>();r.sprite=sprite;r.flipX=seed%2==0;r.color=new Color(.88f,.95f,.73f);plants.Add(go.transform);
    }
    void LateUpdate(){if(Camera.main!=null)foreach(var plant in plants)plant.rotation=Camera.main.transform.rotation;}
    void OnDestroy()
    {
        foreach(var r in hidden)if(r!=null)r.enabled=true;
        foreach(var m in meshes)if(m!=null)Destroy(m);if(earth!=null)Destroy(earth);
    }
}
