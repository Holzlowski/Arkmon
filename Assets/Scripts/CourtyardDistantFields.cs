using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Low-detail rolling ground behind the original meadow, for the road into the distance.
public sealed class CourtyardDistantFields : MonoBehaviour
{
    Mesh mesh;
    Material material, foliageMaterial;
    readonly List<Transform> foliage = new List<Transform>();

    // Broad, gently bent parcels: small green boundaries separate predominantly golden crops.
    static Color ParcelColor(Vector3 p)
    {
        float u = -p.x - 178 + Mathf.Sin(p.z * .035f) * 6;
        float v = p.z + Mathf.Sin(p.x * .021f) * 9 + (-p.x - 178) * .12f;
        int row = Mathf.FloorToInt(u / 66);
        v += (row % 2 == 0 ? 0 : 23);
        int column = Mathf.FloorToInt(v / 78);
        float a = Mathf.Repeat(u, 66), b = Mathf.Repeat(v, 78);
        float edge = Mathf.Min(Mathf.Min(a, 66 - a), Mathf.Min(b, 78 - b));
        float strip = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.2f, 3.8f, edge));
        float variety = Mathf.Repeat(row * .381f + column * .617f, 1);
        Color crop = Color.Lerp(new Color(.65f,.47f,.20f), new Color(.87f,.71f,.36f), variety).linear;
        Color grass = new Color(.40f,.47f,.20f).linear;
        var color = Color.Lerp(crop, grass, strip);
        color *= Mathf.Lerp(.96f, 1.04f, Mathf.PerlinNoise(p.x * .07f + 70, p.z * .07f + 50));
        return Color.Lerp(CourtyardFieldMargins.GoldenGround(p), color,
            Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-184, -206, p.x)));
    }
    public static Vector3 Surface(MeadowVegetation meadow, Vector3 p)
    {
        var point = meadow.SurfacePoint(p);
        float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-184, -355, p.x));
        point.y += t * (12 + Mathf.Sin(p.z * .024f + p.x * .017f) * 3);
        return point;
    }
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
    }
    void AddFieldLandmarks(MeadowVegetation meadow)
    {
        foliageMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Distant field illustrations" };
        var oak = Resources.Load<Sprite>("CourtyardStyle/Trees/Oak");
        var birch = Resources.Load<Sprite>("CourtyardStyle/Trees/Birch");
        var hedge = Resources.Load<Sprite>("CourtyardStyle/Mixed");
        var centres = new[] { new Vector3(-242,0,-20), new Vector3(-272,0,116), new Vector3(-337,0,57) };
        for (int i=0;i<centres.Length;i++)
        {
            var p = centres[i];
            AddPlant(meadow, i == 1 ? birch : oak, p, 7.2f, "Distant field tree");
            AddPlant(meadow, birch, p + new Vector3(-3,0,4), 5.8f, "Distant field tree");
            for(int j=0;j<7;j++)
                AddPlant(meadow, hedge, p + new Vector3(0,0,(j-3)*2.2f), 1.5f+(j%3)*.2f, "Distant field hedge");
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
        renderer.color=new Color(.85f,.84f,.69f);renderer.flipX=foliage.Count%2==0;
        foliage.Add(go.transform);
    }
    void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var plant in foliage)plant.rotation=camera.transform.rotation;
    }
    void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(foliageMaterial!=null)Destroy(foliageMaterial);}

}
