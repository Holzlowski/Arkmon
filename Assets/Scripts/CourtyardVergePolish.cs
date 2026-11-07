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
    Transform workshop;
    readonly List<Renderer> retiredGrowth=new List<Renderer>();
    float left,right;
    Bounds[] gardenBeds=new Bounds[0];
    CourtyardGuardRoute patrol;

    internal static void Install()
    {
        if(GameObject.Find("Illustrated workshop and stable verges")!=null)return;
        var workshop=GameObject.Find("Ida workshop")?.transform;
        var stable=GameObject.Find("Stable")?.transform;
        if(workshop==null || stable==null)return;
        var root=new GameObject("Illustrated workshop and stable verges");
        var polish=root.AddComponent<CourtyardVergePolish>();
        polish.left=-42.5f;polish.right=stable.position.x+4.8f;
        polish.gardenBeds=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.name=="Garden soil bed").OrderBy(r=>r.transform.position.x).Select(r=>r.bounds).ToArray();
        if(polish.gardenBeds.Length>0)polish.right=Mathf.Max(polish.right,polish.gardenBeds.Max(b=>b.max.x)+1.2f);
        polish.right=Mathf.Max(polish.right,43f);
        polish.workshop=workshop;
        polish.interactions=FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IInteractableAsPlayer>().ToArray();
        Physics.SyncTransforms();
        polish.patrol=Resources.Load<CourtyardGuardRoute>("Guard/Patrol");
        polish.Paths();polish.RetirePrimitiveVerge();polish.PlantEdges();
        polish.Foundation(workshop,3.35f,-2.15f);
        polish.Foundation(stable,3.85f,-2.1f);
        polish.ConnectWorkplaces(workshop,stable);
        var cottage=GameObject.Find("Garden cottage")?.transform;
        if(cottage!=null)polish.Foundation(cottage,3.2f,-2.8f);
        polish.GardenEdges();
        if(cottage!=null)polish.CottageMeadow(cottage);
        polish.MeadowIslands();
    }
    void Paths()
    {
        var template=Resources.Load<Material>("CourtyardStyle/Soft path");if(template==null)return;
        foreach(var filter in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(filter.name!="Curved courtyard path" || filter.sharedMesh==null)continue;
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null || renderer.bounds.max.x< -44 || renderer.bounds.min.x>right)continue;
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
                    int first=row*Across;float t=row/(float)(vertices.Length/Across-1);
                    var center=CourtyardResidentLayout.GardenApproach(t);
                    var tangent=(CourtyardResidentLayout.GardenApproach(Mathf.Min(1,t+.005f))-CourtyardResidentLayout.GardenApproach(Mathf.Max(0,t-.005f))).normalized;
                    var across=new Vector3(tangent.z,0,-tangent.x);
                    for(int side=0;side<Across;side++)
                    {
                        var point=center+across*((side/8f-1)*.85f+.08f*Mathf.Sin(row*.33f));
                        vertices[first+side]=filter.transform.InverseTransformPoint(Ground(point)-Vector3.up*.005f);
                    }
                }
            bool castleSpur=Vector2.Distance(new Vector2(start.x,start.z),new Vector2(11,-1))<.2f;
            bool mainRoad=vertices.Length/Across>250;
            if(castleSpur || mainRoad)
                for(int row=0;row<vertices.Length/Across;row++)
                {
                    int first=row*Across;var center=(vertices[first]+vertices[first+Across-1])*.5f;
                    var across=(vertices[first+Across-1]-vertices[first]).normalized;
                    float t=row/(float)(vertices.Length/Across-1);
                    var world=filter.transform.TransformPoint(center);
                    // The castle spur originally stops several metres short of the threshold.
                    float extension=castleSpur?4.0f*Mathf.Pow(t,3):0;
                    float weight=mainRoad?Mathf.Clamp01((world.x+3)/4)*Mathf.Clamp01((17-world.x)/4):1;
                    float width=1+weight*(.10f*Mathf.Sin(row*.16f)+.05f*Mathf.Sin(row*.39f));
                    float drift=weight*.14f*Mathf.Sin(row*.21f);
                    for(int side=0;side<Across;side++)
                    {
                        var point=center+(vertices[first+side]-center)*width+across*drift;
                        world=filter.transform.TransformPoint(point)+Vector3.forward*extension;
                        vertices[first+side]=filter.transform.InverseTransformPoint(Ground(world)-Vector3.up*.005f);
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
            paint.SetVector("_Region",new Vector4(-44,right,0,0));
            // Keep all triangles outside this section on their original material.
            var inside=new List<int>();var outside=new List<int>();var triangles=original.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                float x=filter.transform.TransformPoint((vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3).x;
                var list=x> -44 && x<right?inside:outside;
                list.Add(triangles[i]);list.Add(triangles[i+1]);list.Add(triangles[i+2]);
            }
            copy.subMeshCount=2;copy.SetTriangles(outside,0);copy.SetTriangles(inside,1);
            originals.Add((filter,original,renderer,renderer.sharedMaterials));
            filter.sharedMesh=copy;renderer.sharedMaterials=new[]{source,paint};
        }
    }
    void RetirePrimitiveVerge()
    {
        // Superseded by the illustrated clusters along the actual curved road, not the old straight row.
        foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if(r.enabled && (r.name=="Path edge grass" || r.name=="Path edge pebble"))
            {retiredGrowth.Add(r);r.enabled=false;}
    }
    bool Clear(Vector3 point)
    {
        // Keep the entire western stair flight and Ida's stone doorstep visibly free.
        if(point.x> -37.5f && point.x< -30.5f && point.z>3.7f)return false;
        var local=workshop.InverseTransformPoint(point);
        if(local.x> -3.2f && local.x<2.6f && local.z> -6.8f && local.z< -2.0f)return false;
        // Worn kitchen-garden spine and the three watering approaches remain open.
        if(CourtyardResidentLayout.CastleSquare(point) || CourtyardResidentLayout.GardenWalkway(point))return false;
        foreach(var bed in gardenBeds)
            if(point.x>3.7f+CourtyardResidentLayout.GardenOffset.x && point.x<bed.center.x+.5f && Mathf.Abs(point.z-(bed.center.z-2.05f))<.65f)return false;
        // Preserve the newly opened castle approach and the guard's complete patrol corridor.
        if(point.x>10.55f && point.x<13.45f && point.z>-.8f && point.z<12.3f)return false;
        if(patrol!=null)
            for(int i=1;i<patrol.stops.Length;i++)
            {
                var a=patrol.stops[i-1].position;var b=patrol.stops[i].position;
                a.y=b.y=point.y;var segment=b-a;
                float t=segment.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector3.Dot(point-a,segment)/segment.sqrMagnitude);
                if(Vector3.Distance(point,a+segment*t)<1.1f)return false;
            }
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
            if(edge.at.x<left+1 || edge.at.x>right-1 || random.NextDouble()<.72)continue;
            var at=edge.at+edge.outside*(.08f+(float)random.NextDouble()*.30f);
            if(!Clear(at))continue;
            Prop(kinds[random.Next(kinds.Length)],Ground(at),1.05f+(float)random.NextDouble()*.45f,random.Next(2)==0);
            int companions=random.Next(1,3);
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
    void MeadowIslands()
    {
        var source=Resources.Load<Material>("CourtyardStyle/Stable earth");if(source==null)return;
        var undergrowth=new Material(source){name="Warm courtyard meadow undergrowth"};materials.Add(undergrowth);
        undergrowth.SetTexture("_BaseMap",Texture2D.whiteTexture);
        undergrowth.SetColor("_BaseColor",new Color(.40f,.49f,.19f,.18f));
        undergrowth.SetFloat("_EdgeStart",.36f);undergrowth.SetFloat("_EdgeBreakup",.12f);
        // Broad, low islands flank the main path; leave generous open lawn between them.
        var groups=new[]{
            new Vector4(-30,-4.8f,3.0f,2.0f),new Vector4(-23,0,3.8f,1.9f),new Vector4(-17,-9.0f,5.2f,2.0f),
            new Vector4(-5.5f,-8.5f,4.6f,2.3f),new Vector4(6.5f,-6.8f,3.7f,1.9f),
            new Vector4(15.8f,-7.0f,2.7f,1.7f),new Vector4(18.2f,4.7f,2.5f,1.8f),
            new Vector4(-17.5f,5.6f,2.0f,1.1f),
            // Small, low groups follow the inner foreground wall; keep the open lawn legible.
            new Vector4(-31,-13.5f,2.5f,.85f),new Vector4(-18,-14.3f,2.8f,.9f),
            new Vector4(-3,-13.8f,2.7f,.85f),new Vector4(14,-14.1f,2.8f,.9f),
            new Vector4(39,-13.0f,2.0f,.8f)};
        for(int g=0;g<groups.Length;g++)
        {
            var area=groups[g];var center=new Vector3(area.x,0,area.y);
            var root=new GameObject("Courtyard meadow island "+g).transform;root.SetParent(transform,false);
            MeadowGround(root,center,area.z,area.w,undergrowth,g);
            var random=new System.Random(8017+g*31);
            int count=Mathf.RoundToInt(area.z*area.w*13);
            for(int i=0;i<count;i++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble());
                float edge=1+.12f*Mathf.Sin(angle*3+g)+.06f*Mathf.Cos(angle*5);
                var at=center+new Vector3(Mathf.Cos(angle)*area.z*r*edge,0,Mathf.Sin(angle)*area.w*r*edge);
                float density=Mathf.PerlinNoise((at.x+77)*.68f,(at.z+33)*.68f);
                if(!Clear(at) || random.NextDouble()>Mathf.Lerp(.94f,.24f,r*r)*Mathf.Lerp(.45f,1,Mathf.SmoothStep(.20f,.72f,density)))continue;
                string kind=i%19==0?"Flowers":i%7==0?"Mixed":i%3==0?"Grass":"Clover";
                var sprite=Resources.Load<Sprite>("CourtyardStyle/"+kind);if(sprite==null)continue;
                float height=((kind=="Grass"?.38f:.23f)+(float)random.NextDouble()*.20f)*Mathf.Lerp(1,.62f,r*r);
                var plant=Prop(kind,Ground(at),height/sprite.bounds.size.y,random.Next(2)==0);
                if(plant==null)continue;
                plant.SetParent(root,true);
                plant.GetComponent<SpriteRenderer>().color=Color.Lerp(new Color(.77f,.87f,.64f),new Color(.92f,.96f,.76f),(float)random.NextDouble());
            }
        }
    }
    void MeadowGround(Transform root,Vector3 center,float rx,float rz,Material material,int seed)
    {
        const int sides=40,rings=7;var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float angle=side*Mathf.PI*2/sides,r=ring/(float)rings;
            float edge=1+.12f*Mathf.Sin(angle*3+seed)+.06f*Mathf.Cos(angle*5);
            var at=center+new Vector3(Mathf.Cos(angle)*rx*r*edge,0,Mathf.Sin(angle)*rz*r*edge);
            vertices.Add(Ground(at)-Vector3.up*.015f);
            // Fade the patch out at walking lines and props as well as its irregular outer edge.
            colors.Add(new Color(Clear(at)?r:1,0,0,1));
            if(ring==0)continue;int n=ring*sides+side,previous=n-sides,next=(side+1)%sides;
            triangles.AddRange(new[]{previous,ring*sides+next,n,previous,(ring-1)*sides+next,ring*sides+next});
        }
        var mesh=new Mesh{name="Soft courtyard meadow island"};mesh.SetVertices(vertices);mesh.SetColors(colors);
        mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var obj=new GameObject("Meadow island ground",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
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
        float y=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(point+Vector3.up*20,Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground"))y=Mathf.Max(y,hit.point.y);
        point.y=(float.IsNegativeInfinity(y)?point.y:y)+.035f;return point;
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
        foreach(var r in retiredGrowth)if(r!=null)r.enabled=true;
        foreach(var saved in originals)
        {
            if(saved.filter!=null)saved.filter.sharedMesh=saved.mesh;
            if(saved.renderer!=null)saved.renderer.sharedMaterials=saved.material;
        }
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        foreach(var material in materials)if(material!=null)Destroy(material);
    }
}
