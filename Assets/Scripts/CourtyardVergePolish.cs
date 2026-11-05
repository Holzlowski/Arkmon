using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Bounded illustrated verges from Ida through the vegetable garden. Decoration remains non-blocking.
[DefaultExecutionOrder(9502)]
public sealed class CourtyardVergePolish : MonoBehaviour
{
    const int Across=17;
    readonly List<Transform> plants=new List<Transform>();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> materials=new List<Material>();
    readonly List<(MeshFilter filter,Mesh mesh,MeshRenderer renderer,Material[] material)> originals=
        new List<(MeshFilter,Mesh,MeshRenderer,Material[])>();
    readonly List<(Vector3 center,float width)> roadSections=new List<(Vector3,float)>();
    readonly List<(Vector3 at,Vector3 outside)> edges=new List<(Vector3,Vector3)>();
    IInteractableAsPlayer[] interactions;
    float left,right;
    Bounds[] gardenBeds=new Bounds[0];

    internal static void Install()
    {
        if(GameObject.Find("Illustrated workshop and stable verges")!=null)return;
        var workshop=GameObject.Find("Ida workshop")?.transform;
        var stable=GameObject.Find("Stable")?.transform;
        if(workshop==null || stable==null)return;
        var root=new GameObject("Illustrated workshop and stable verges");
        var polish=root.AddComponent<CourtyardVergePolish>();
        polish.left=workshop.position.x-4.5f;polish.right=stable.position.x+4.8f;
        polish.gardenBeds=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.name=="Garden soil bed").OrderBy(r=>r.transform.position.x).Select(r=>r.bounds).ToArray();
        if(polish.gardenBeds.Length>0)polish.right=Mathf.Max(polish.right,polish.gardenBeds.Max(b=>b.max.x)+1.2f);
        polish.right=Mathf.Max(polish.right,43f);
        polish.interactions=FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IInteractableAsPlayer>().ToArray();
        Physics.SyncTransforms();
        polish.Paths();polish.PlantEdges();
        polish.Foundation(workshop,3.35f,-2.15f);
        polish.Foundation(stable,3.85f,-2.1f);
        polish.ConnectWorkplaces(workshop,stable);
        var cottage=GameObject.Find("Garden cottage")?.transform;
        if(cottage!=null)polish.Foundation(cottage,3.2f,-2.8f);
        polish.GardenEdges();
        if(cottage!=null)polish.CottageMeadow(cottage);
    }
    void Paths()
    {
        var template=Resources.Load<Material>("CourtyardStyle/Soft path");if(template==null)return;
        foreach(var filter in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(filter.name!="Curved courtyard path" || filter.sharedMesh==null)continue;
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null || renderer.bounds.max.x<left || renderer.bounds.min.x>right)continue;
            var original=filter.sharedMesh;var source=renderer.sharedMaterial;
            var vertices=original.vertices;
            // These authored paths consist of 17 vertices across each sampled cross-section.
            if(vertices.Length%Across!=0)continue;
            // Only the authored cottage spur starts at (4,-2); leave the main route and other spurs intact.
            var start=filter.transform.TransformPoint((vertices[0]+vertices[Across-1])*.5f);
            bool gardenSpur=Vector2.Distance(new Vector2(start.x,start.z),new Vector2(4,-2))<.2f;
            if(gardenSpur)
                for(int row=0;row<vertices.Length/Across;row++)
                {
                    int first=row*Across;var center=(vertices[first]+vertices[first+Across-1])*.5f;
                    var across=(vertices[first+Across-1]-vertices[first]).normalized;
                    for(int side=0;side<Across;side++)
                    {
                        var point=center+(vertices[first+side]-center)*.60f+across*(.09f*Mathf.Sin(row*.33f));
                        vertices[first+side]=filter.transform.InverseTransformPoint(Ground(filter.transform.TransformPoint(point))-Vector3.up*.005f);
                    }
                }
            for(int row=0;row<vertices.Length/Across;row++)
            {
                Vector3 a=filter.transform.TransformPoint(vertices[row*Across]);
                Vector3 b=filter.transform.TransformPoint(vertices[row*Across+Across-1]);
                var center=(a+b)*.5f;
                roadSections.Add((center,Vector3.Distance(a,b)*.5f));
                if(row%8==0)
                {
                    edges.Add((a,(a-center).normalized));edges.Add((b,(b-center).normalized));
                }
            }
            var copy=Instantiate(original);copy.name=original.name+" soft verges";meshes.Add(copy);
            copy.vertices=vertices;copy.RecalculateNormals();copy.RecalculateBounds();
            var colors=new Color[vertices.Length];
            for(int i=0;i<colors.Length;i++)colors[i]=new Color(Mathf.Abs(i%Across/8f-1),0,0,1);
            copy.colors=colors;
            var paint=new Material(template);materials.Add(paint);
            paint.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));paint.SetColor("_BaseColor",source.GetColor("_BaseColor"));
            paint.SetVector("_Region",new Vector4(left,right,0,0));
            // Keep all triangles outside this section on their original material.
            var inside=new List<int>();var outside=new List<int>();var triangles=original.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                float x=filter.transform.TransformPoint((vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3).x;
                var list=x>left && x<right?inside:outside;
                list.Add(triangles[i]);list.Add(triangles[i+1]);list.Add(triangles[i+2]);
            }
            copy.subMeshCount=2;copy.SetTriangles(outside,0);copy.SetTriangles(inside,1);
            originals.Add((filter,original,renderer,renderer.sharedMaterials));
            filter.sharedMesh=copy;renderer.sharedMaterials=new[]{source,paint};
        }
    }
    bool Clear(Vector3 point)
    {
        foreach(var bed in gardenBeds)
            if(point.x>bed.min.x-.5f && point.x<bed.max.x+.5f && point.z>bed.min.z-.5f && point.z<bed.max.z+.5f)return false;
        foreach(var obstacle in Physics.OverlapSphere(point+Vector3.up*.18f,.28f,~0,QueryTriggerInteraction.Ignore))
            if(!obstacle.CompareTag("Ground"))return false;
        foreach(var target in interactions)
        {
            var delta=target.GetTransform()-point;delta.y=0;
            if(delta.sqrMagnitude<1.15f*1.15f)return false;
        }
        foreach(var section in roadSections)
        {
            var delta=section.center-point;delta.y=0;
            if(delta.sqrMagnitude<(section.width-.25f)*(section.width-.25f))return false;
        }
        return true;
    }
    void PlantEdges()
    {
        var random=new System.Random(914);
        string[] kinds={"Grass","Clover","Mixed","Grass","Fern","Clover","Flowers","Pebbles"};
        foreach(var edge in edges)
        {
            if(edge.at.x<left+1 || edge.at.x>right-1 || random.NextDouble()<.24)continue;
            var at=edge.at+edge.outside*(.08f+(float)random.NextDouble()*.30f);
            if(!Clear(at))continue;
            Prop(kinds[random.Next(kinds.Length)],Ground(at),1.05f+(float)random.NextDouble()*.45f,random.Next(2)==0);
            int companions=random.Next(1,4);
            for(int i=0;i<companions;i++)
            {
                var nearby=at+edge.outside*(.22f+i*.16f)+new Vector3((i-1)*.28f,0,.1f);
                if(Clear(nearby))Prop(i%2==0?"Clover":"Mixed",Ground(nearby),.7f+(float)random.NextDouble()*.3f,random.Next(2)==0);
            }
        }
    }
    void ConnectWorkplaces(Transform workshop,Transform stable)
    {
        float x=(workshop.position.x+stable.position.x)*.5f;
        float z=(workshop.position.z+stable.position.z)*.5f-3.1f;
        var random=new System.Random(271);
        for(int group=0;group<3;group++)
        {
            var island=new GameObject("Connecting meadow island");island.transform.SetParent(transform,false);
            var center=new Vector3(x+(group-1)*1.25f,0,z+(group%2)*.7f);
            for(int i=0;i<18;i++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble());
                var point=center+new Vector3(Mathf.Cos(a)*1.05f*r,0,Mathf.Sin(a)*.52f*r);
                if(!Clear(point))continue;
                var plant=Prop(i%7==0?"Grass":i%3==0?"Mixed":"Clover",Ground(point),.85f+(float)random.NextDouble()*.45f,i%2==0);
                if(plant!=null)plant.SetParent(island.transform,true);
            }
        }
    }
    void CottageMeadow(Transform cottage)
    {
        // A loose low strip between the stable and cottage; keep the new seat and walking line open.
        var random=new System.Random(418);
        for(int i=0;i<30;i++)
        {
            var at=cottage.TransformPoint(new Vector3(-7.2f+(float)random.NextDouble()*3.2f,0,-5.6f+(float)random.NextDouble()*3));
            if(!Clear(at))continue;
            Prop(i%9==0?"Pebbles":i%5==0?"Grass":"Clover",Ground(at),.70f+(float)random.NextDouble()*.30f,i%2==0);
        }
    }
    void GardenEdges()
    {
        // Low growth at the rear and outer sides; the front remains the watering approach.
        for(int b=0;b<gardenBeds.Length;b++)
        {
            var bed=gardenBeds[b];
            for(int i=0;i<5;i++)
            {
                var point=new Vector3(Mathf.Lerp(bed.min.x,bed.max.x,i/4f),0,bed.max.z+.72f+(i%2)*.12f);
                if(Clear(point))Prop(i%3==0?"Grass":"Clover",Ground(point),.85f,i%2==0);
            }
            if(b!=0 && b!=gardenBeds.Length-1)continue;
            for(int i=0;i<3;i++)
            {
                var point=new Vector3(b==0?bed.min.x-.75f:bed.max.x+.75f,0,bed.center.z+i*.55f);
                if(Clear(point))Prop(i==1?"Flowers":"Mixed",Ground(point),.85f,b==0);
            }
        }
    }
    void Foundation(Transform house,float side,float front)
    {
        // Small growth at the corners ties the foundations to the grass, away from doorways.
        for(int flank=-1;flank<=1;flank+=2)
        for(int i=0;i<5;i++)
        {
            var at=house.TransformPoint(new Vector3(flank*(side+.12f*(i%2)),0,front+i*.56f));
            if(!Clear(at))continue;
            Prop(i==1?"Pebbles":i%2==0?"Mixed":"Fern",Ground(at),.85f+i*.055f,flank<0);
        }
    }
    static Vector3 Ground(Vector3 point)
    {
        float y=point.y;
        foreach(var hit in Physics.RaycastAll(point+Vector3.up*20,Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground"))y=Mathf.Max(y,hit.point.y);
        point.y=y+.035f;return point;
    }
    Transform Prop(string resource,Vector3 at,float scale,bool flip)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/"+resource);if(sprite==null)return null;
        var obj=new GameObject("Verge "+resource,typeof(SpriteRenderer));obj.transform.SetParent(transform,false);
        obj.transform.position=at;obj.transform.localScale=Vector3.one*scale;
        var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.flipX=flip;
        renderer.color=new Color(.88f,.90f,.78f,1);plants.Add(obj.transform);return obj.transform;
    }
    void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var plant in plants)plant.rotation=camera.transform.rotation;
    }
    void OnDestroy()
    {
        foreach(var saved in originals)
        {
            if(saved.filter!=null)saved.filter.sharedMesh=saved.mesh;
            if(saved.renderer!=null)saved.renderer.sharedMaterials=saved.material;
        }
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        foreach(var material in materials)if(material!=null)Destroy(material);
    }
}
