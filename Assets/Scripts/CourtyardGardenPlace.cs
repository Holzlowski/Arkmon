using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Local, reversible scene dressing. Keep the authored fountain available for a later courtyard square.
public sealed class CourtyardGardenPlace : MonoBehaviour
{
    readonly List<Mesh> groundMeshes=new List<Mesh>();
    readonly List<Renderer> hiddenHerbs=new List<Renderer>();
    Material trailEarth;
    // Run before ground cover, crop visuals and interaction anchors sample the authored beds.
    internal static void PrepareBeds()
    {
        if (GameObject.Find("Kitchen garden layout") != null) return;
        var beds = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.name == "Garden soil bed").OrderBy(r => r.transform.position.x).ToArray();
        if (beds.Length != 3 || GameObject.Find("Garden cottage") == null) return;
        GameObject.Find("Garden cottage").transform.position = CourtyardResidentLayout.GardenPoint(new Vector3(.2f, 0, 9.6f));
        var centers = beds.Select(r => new Vector3(r.transform.position.x, 0, r.transform.position.z)).ToArray();
        var parts = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t =>
            t.name == "Garden soil bed" || t.name == "Garden border" || t.name.StartsWith("Bean trellis") ||
            t.name == "Lettuce" || t.name == "Carrot greens" || t.name == "Carrot shoulders" || t.name == "Bean leaves").ToArray();
        var layout = new GameObject("Kitchen garden layout").transform;
        layout.SetParent(beds[0].transform.parent, false);
        const float size = .72f;
        for (int i = 0; i < beds.Length; i++)
        {
            var group = new GameObject("Kitchen garden bed " + i).transform;
            group.SetParent(layout, false);
            group.position = CourtyardResidentLayout.GardenPoint(new Vector3(6.6f + i * .25f, 0, 3.7f + i * 3.7f));
            foreach (var part in parts)
            {
                var nearest = Enumerable.Range(0, centers.Length).OrderBy(n =>
                    Vector2.SqrMagnitude(new Vector2(part.position.x - centers[n].x, part.position.z - centers[n].z))).First();
                // Parts already moved belong to their new group and must not be selected again.
                if (part.IsChildOf(layout) || nearest != i) continue;
                var point = group.position + (part.position - centers[i]) * size;
                part.SetParent(group, true); part.position = point; part.localScale *= size;
            }
        }
        // Keep the seed bench beside Marta, outside the new first bed and the house approach.
        foreach (var part in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (part.name == "Garden bench seat" || part.name == "Garden bench stone foot")
                part.position += new Vector3(-2.35f, 0, -.3f) + CourtyardResidentLayout.GardenOffset;
        var cottage=GameObject.Find("Garden cottage").transform;
        foreach(var part in cottage.GetComponentsInChildren<Transform>())
            if(part.name=="Garden potting table" || part.name=="Marta work spot")part.position+=Vector3.left*.45f;
        Physics.SyncTransforms();
    }
    internal static void Install()
    {
        var house=GameObject.Find("Garden cottage")?.transform;
        if(house==null || house.Find("Marta garden place")!=null)return;
        var oak=Resources.Load<Material>("CourtyardStyle/Oak");if(oak==null)return;
        var root=new GameObject("Marta garden place");root.transform.SetParent(house,false);
        var place=root.AddComponent<CourtyardGardenPlace>();
        GameObject.Find("Fountain basin")?.SetActive(false);
        var original=GameObject.Find("Bench seat");
        if(original!=null)
        {
            var seat=Instantiate(original,root.transform);seat.name="Marta side bench";
            original.SetActive(false);
            seat.transform.localPosition=new Vector3(-3.65f,0,-4.15f);
            seat.transform.localRotation=Quaternion.Euler(0,-65,0);
            seat.transform.localScale=new Vector3(.60f,.90f,.85f);
            var ground=Ground(seat.transform.position);seat.transform.position=ground;
            foreach(var renderer in seat.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial=oak;
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                var scale=renderer.transform.lossyScale;
                block.SetVector("_ObjectScale",new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));
                block.SetFloat("_GrainAxis",scale.x>scale.y && scale.x>scale.z?0:scale.z>scale.y?2:1);
                block.SetFloat("_EdgeWidth",.018f);renderer.SetPropertyBlock(block);
            }
        }
        var steps=house.GetComponentsInChildren<Transform>().Where(t=>t.name=="Worn approach flagstone").OrderByDescending(t=>t.localPosition.z).ToArray();
        for(int i=0;i<steps.Length;i++)
        {
            var step=steps[i];var at=step.localPosition;at.x=.12f*Mathf.Sin(i*1.8f);step.localPosition=at;
            step.localRotation=Quaternion.Euler(0,i%2==0?-5:7,0);
            var scale=step.localScale;scale.x*=.78f;scale.z*=.9f;step.localScale=scale;
        }
        Physics.SyncTransforms();
        place.WorkingGround(house.TransformPoint(new Vector3(2.15f,0,-4.15f)));
        var earth=Resources.Load<Material>("CourtyardStyle/Stable earth");
        if(earth!=null)
        {
            place.trailEarth=new Material(earth){name="Subtle worn garden paths"};
            place.trailEarth.SetColor("_BaseColor",new Color(.62f,.70f,.79f,.63f));
        }
        foreach(var herb in house.GetComponentsInChildren<SpriteRenderer>())
            if(herb.name=="Illustrated garden herbs" && CourtyardResidentLayout.GardenOriginal(herb.transform.position).x>3.85f && CourtyardResidentLayout.GardenOriginal(herb.transform.position).z<10)
            {place.hiddenHerbs.Add(herb);herb.enabled=false;}
        // One narrow spine serves every bed; short branches mark the actual watering positions.
        place.Trail("Garden worn spine",new Vector3(3.95f,0,.35f),new Vector3(4.35f,0,4.9f),new Vector3(4.55f,0,9.6f),.53f);
        foreach(var bed in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed"))
        {
            float z=bed.transform.position.z-CourtyardResidentLayout.GardenOffset.z;
            place.Trail("Garden watering approach",new Vector3(4.35f,0,z),new Vector3(5.2f,0,z-.10f),new Vector3(bed.bounds.min.x-CourtyardResidentLayout.GardenOffset.x-1f,0,z),.48f);
        }
        place.Trail("Garden worktable connection",new Vector3(3.1f,0,5.35f),new Vector3(3.7f,0,5.05f),new Vector3(4.45f,0,5.15f),.48f);
    }
    static Vector3 Ground(Vector3 at)
    {
        foreach(var hit in Physics.RaycastAll(at+Vector3.up*15,Vector3.down,30,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground"))at.y=Mathf.Max(at.y,hit.point.y);
        return at;
    }
    void WorkingGround(Vector3 center)
    {
        var material=Resources.Load<Material>("CourtyardStyle/Stable earth");if(material==null)return;
        const int sides=40,rings=5;
        var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int ring=0;ring<=rings;ring++)
        for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)rings;
            float edge=1+.06f*Mathf.Sin(a*5)+.03f*Mathf.Cos(a*7);
            var point=Ground(center+new Vector3(Mathf.Cos(a)*1.25f*r*edge,0,Mathf.Sin(a)*.95f*r*edge))+Vector3.up*.045f;
            vertices.Add(transform.InverseTransformPoint(point));colors.Add(new Color(r,0,0,1));
            if(ring==0)continue;
            int prev=(ring-1)*sides+side,cur=ring*sides+side,next=(side+1)%sides;
            triangles.AddRange(new[]{prev,ring*sides+next,cur,prev,(ring-1)*sides+next,ring*sides+next});
        }
        var groundMesh=new Mesh{name="Marta worn work ground"};groundMeshes.Add(groundMesh);groundMesh.SetVertices(vertices);groundMesh.SetColors(colors);
        groundMesh.SetTriangles(triangles,0);groundMesh.RecalculateNormals();groundMesh.RecalculateBounds();
        var patch=new GameObject("Marta worn work ground",typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(transform,false);
        patch.GetComponent<MeshFilter>().sharedMesh=groundMesh;
        var renderer=patch.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    void Trail(string name,Vector3 a,Vector3 b,Vector3 c,float width)
    {
        var material=trailEarth;if(material==null)return;
        a=CourtyardResidentLayout.GardenPoint(a);b=CourtyardResidentLayout.GardenPoint(b);c=CourtyardResidentLayout.GardenPoint(c);
        const int rows=40,across=9;var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int row=0;row<=rows;row++)
        {
            float t=row/(float)rows,u=1-t;var center=u*u*a+2*u*t*b+t*t*c;
            var tangent=(2*u*(b-a)+2*t*(c-b)).normalized;var side=new Vector3(tangent.z,0,-tangent.x);
            for(int col=0;col<across;col++)
            {
                float x=col/4f-1;
                var at=Ground(center+side*x*width*(.55f+.45f*Mathf.Sin(t*Mathf.PI))*(1+.10f*Mathf.Sin(row*.49f)))+Vector3.up*.052f;
                vertices.Add(transform.InverseTransformPoint(at));
                colors.Add(new Color(Mathf.Max(Mathf.Abs(x),Mathf.Max(1-row/9f,1-(rows-row)/9f)),0,0,1));
                if(row==0 || col==0)continue;int n=vertices.Count-1;
                triangles.AddRange(new[]{n-across-1,n-1,n-across,n-across,n-1,n});
            }
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();groundMeshes.Add(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    void OnDestroy()
    {
        foreach(var r in hiddenHerbs)if(r!=null)r.enabled=true;
        foreach(var mesh in groundMeshes)if(mesh!=null)Destroy(mesh);
        if(trailEarth!=null)Destroy(trailEarth);
    }
}
