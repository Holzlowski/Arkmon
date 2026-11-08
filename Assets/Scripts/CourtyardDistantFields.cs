using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Low-detail rolling ground behind the original meadow, for the road into the distance.
public sealed class CourtyardDistantFields : MonoBehaviour
{
    Mesh mesh;
    readonly List<Mesh> transitionMeshes = new List<Mesh>();
    Material material, foliageMaterial;
    readonly List<Transform> foliage = new List<Transform>();

    static Color ParcelColor(Vector3 p) => CourtyardFieldMargins.GoldenGround(p);
    public static Vector3 Surface(MeadowVegetation meadow, Vector3 p) => meadow.SurfacePoint(p);
    internal static void Install()
    {
        if (GameObject.Find("Aurenfeld distant rolling fields") != null) return;
        var horizon = GameObject.Find("Castle horizon and golden fields");
        var meadow = horizon != null ? horizon.GetComponentInChildren<MeadowVegetation>() : null;
        var source = horizon != null ? horizon.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r => r.sharedMaterial != null && r.sharedMaterial.shader.name == "Arkmon/Meadow") : null;
        if (meadow == null || source == null) return;
        var root = new GameObject("Aurenfeld distant rolling fields", typeof(MeshFilter), typeof(MeshRenderer));
        var style = root.AddComponent<CourtyardDistantFields>();
        const int rows = 113, columns = 201;
        var vertices = new Vector3[rows * columns]; var colors = new Color[vertices.Length];
        var triangles = new int[(rows - 1) * (columns - 1) * 6]; int k = 0;
        for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
        {
            float x = Mathf.Lerp(-180, -404, row / (float)(rows - 1));
            float z = Mathf.Lerp(-180, 220, col / (float)(columns - 1)); int n = row * columns + col;
            vertices[n] = Surface(meadow, new Vector3(x, 0, z));
            // Bury the narrow overlap below the existing mesh to avoid z-fighting at its edge.
            vertices[n].y -= .04f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-180, -188, x)));
            colors[n] = ParcelColor(vertices[n]);
            if (row == 0 || col == 0) continue;
            int a = n - columns - 1;
            triangles[k++]=a;triangles[k++]=n-1;triangles[k++]=a+1;
            triangles[k++]=a+1;triangles[k++]=n-1;triangles[k++]=n;
        }
        style.mesh = new Mesh { name = "Distant rolling Aurenfeld fields" };
        style.mesh.vertices=vertices;style.mesh.colors=colors;style.mesh.triangles=triangles;
        style.mesh.RecalculateNormals();style.mesh.RecalculateBounds();root.GetComponent<MeshFilter>().sharedMesh=style.mesh;
        var renderer=root.GetComponent<MeshRenderer>();style.material=new Material(source.sharedMaterial){name="Distant Aurenfeld field haze"};
        style.material.SetFloat("_FogStrength",1f);renderer.sharedMaterial=style.material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        style.AddFieldLandmarks(meadow);
        style.BuildGrainTransition(meadow);
    }
    void BuildGrainTransition(MeadowVegetation meadow)
    {
        // Dense short transition; individual silhouettes disappear before they become isolated bright dots.
        var wind=GameObject.Find("Castle horizon and golden fields")?.GetComponentsInChildren<MeshRenderer>();
        Material source=null;
        if(wind!=null)foreach(var r in wind)if(r.sharedMaterial!=null && r.sharedMaterial.shader.name=="Arkmon/Meadow Wind"){source=r.sharedMaterial;break;}
        // Vegetation may not have rebuilt yet; use its authored material.
        if(source==null)source=meadow.FieldMaterial;
        if(source==null)return;
        var random=new System.Random(1951);
        for(int band=0;band<6;band++)
        {
            var v=new List<Vector3>();var c=new List<Color>();var uv=new List<Vector2>();var tri=new List<int>();
            for(float x=-178-band*18;x>-196-band*18;x-=1.9f)for(float z=-150;z<200;z+=1.9f)
            {
                var p=new Vector3(x+(float)random.NextDouble()*1.8f,0,z+(float)random.NextDouble()*1.8f);
                if(Mathf.Abs(p.z-CourtyardFieldMargins.VergeZ(p.x))<2.4f)continue;
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-180,-320,p.x));
                if(fade<.05f || random.NextDouble()>.94f)continue;
                p=Surface(meadow,p);float height=(.90f+(float)random.NextDouble()*.45f)*fade;
                for(int blade=0;blade<2;blade++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2;var side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.10f;
                    int n=v.Count;v.Add(p-side);v.Add(p+side);v.Add(p+Vector3.up*height+side*.8f);v.Add(p+Vector3.up*(height+.16f*fade));v.Add(p+Vector3.up*height-side*.8f);
                    var tip=AurenfeldLandscape.Harvest(p);var root=tip*.48f;
                    c.AddRange(new[]{root,root,tip,tip,tip});float phase=(float)random.NextDouble();
                    uv.AddRange(new[]{new Vector2(phase,0),new Vector2(phase,0),new Vector2(phase,1),new Vector2(phase,1),new Vector2(phase,1)});
                    tri.AddRange(new[]{n,n+2,n+1,n,n+4,n+2,n+4,n+3,n+2});
                }
            }
            var detail=new Mesh{name="Distant grain transition"};detail.SetVertices(v);detail.SetColors(c);detail.SetUVs(0,uv);detail.SetTriangles(tri,0);detail.RecalculateBounds();
            var bounds=detail.bounds;bounds.Expand(2);detail.bounds=bounds;transitionMeshes.Add(detail);
            var go=new GameObject("Distant grain band "+band,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=detail;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=source;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
    }
    void AddFieldLandmarks(MeadowVegetation meadow)
    {
        foliageMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Distant field illustrations" };
        var oak = Resources.Load<Sprite>("CourtyardStyle/Trees/Oak");
        var birch = Resources.Load<Sprite>("CourtyardStyle/Trees/Birch");
        var hedge = Resources.Load<Sprite>("CourtyardStyle/Mixed");
        var centres = new[] { new Vector3(-195,0,-52), new Vector3(-236,0,85), new Vector3(-308,0,12), new Vector3(-346,0,107) };
        for (int i=0;i<centres.Length;i++)
        {
            var p = centres[i];
            AddPlant(meadow, i == 1 ? birch : oak, p, 10.2f, "Distant field tree");
            AddPlant(meadow, birch, p + new Vector3(-3,0,4), 8.8f, "Distant field tree");
            for(int j=0;j<11;j++)
                AddPlant(meadow, hedge, p + new Vector3(Mathf.Sin(j*.45f)*3,0,(j-5)*2.1f), 1.9f+Mathf.Sin(j*1.1f)*.35f, "Distant field hedge");
        }
        // Short curved hedges separate fields in depth; breaks preserve open views and the road.
        for(int band=0;band<2;band++)for(int j=0;j<17;j++)
        {
            if(j==5 || j==12)continue;
            float z=-20+j*3.7f+band*37;
            float x=-204-band*76+Mathf.Sin(j*.19f+band)*9;
            var p=new Vector3(x,0,z);
            AddPlant(meadow,hedge,p,2.3f+Mathf.Sin(j*.83f)*.35f,"Distant field hedge");
        }
    }
    void AddPlant(MeadowVegetation meadow, Sprite sprite, Vector3 point, float height, string label)
    {
        if(sprite==null || Mathf.Abs(point.z-CourtyardFieldMargins.VergeZ(point.x))<6) return;
        var go = new GameObject(label,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
        go.transform.position=Surface(meadow,point)+Vector3.up*.025f;
        go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        go.transform.position-=Vector3.up*(sprite.bounds.min.y*go.transform.localScale.y);
        var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=foliageMaterial;
        renderer.color=label == "Distant field tree" ? new Color(.78f,.87f,.76f) : new Color(.70f,.80f,.58f);renderer.flipX=foliage.Count%2==0;
        foliage.Add(go.transform);
    }
    void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var plant in foliage)plant.rotation=camera.transform.rotation;
    }
    void OnDestroy(){foreach(var detail in transitionMeshes)if(detail!=null)Destroy(detail);if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(foliageMaterial!=null)Destroy(foliageMaterial);}

}
