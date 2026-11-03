using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuildPaintedTowerDoor
{
    private const string Art = "Assets/Art/Intro/";
    private sealed class Geometry
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<int> t = new List<int>();
        public void Face(params Vector3[] points)
        {
            int n = v.Count; v.AddRange(points);
            for(int i=1;i<points.Length-1;i++)t.AddRange(new[]{n,n+i,n+i+1});
        }
        public void Extrude(Vector2[] polygon,float front,bool curved,float back=.0f)
        {
            float area=0;for(int i=0;i<polygon.Length;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Length];area+=a.x*b.y-b.x*a.y;}
            if(area<0)System.Array.Reverse(polygon);
            var near=new Vector3[polygon.Length];var far=new Vector3[polygon.Length];
            for(int i=0;i<polygon.Length;i++)
            {var p=polygon[i];near[i]=new Vector3(front,p.y,p.x);far[i]=new Vector3(curved?Curve(p.x,7.99f):back,p.y,p.x);}
            Face(near);var reverse=(Vector3[])far.Clone();System.Array.Reverse(reverse);Face(reverse);
            for(int i=0;i<near.Length;i++){int j=(i+1)%near.Length;Face(near[i],far[i],far[j],near[j]);}
        }
    }
    private static float Curve(float z,float radius)=>Mathf.Sqrt(radius*radius-z*z)-7.803f;
    private static float Top(float z)=>2.35f+Mathf.Sqrt(Mathf.Max(0,1.15f*1.15f-z*z));

    public static void Apply()
    {
        var door=GameObject.Find("Courtyard door").transform;
        var old=door.Find("Painted three dimensional door");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Painted three dimensional door").transform;root.SetParent(door,false);
        var source=door.GetComponentInChildren<SpriteRenderer>(true);if(source!=null)source.gameObject.SetActive(false);
        foreach(string name in new[]{"Courtyard door stone recess","Tower curved door backing"})
        {var obj=GameObject.Find(name);if(obj!=null)obj.GetComponent<Renderer>().enabled=false;}
        var stone=GameObject.Find("Round tower visible walls").GetComponent<Renderer>().sharedMaterial;
        var wood=Clone("Door painted green oak","Assets/Art/SceneStyle/Courtyard painted oak.mat",new Color(.18f,.52f,.20f));
        wood.SetTexture("_EmissionMap",wood.GetTexture("_BaseMap"));wood.SetColor("_EmissionColor",new Color(.025f,.22f,.035f));wood.EnableKeyword("_EMISSION");wood.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
        var metal=Clone("Door aged bronze","Assets/Art/SceneStyle/Tower light - Desk aged brass.mat",new Color(.63f,.43f,.19f));
        var dark=Clone("Door shadowed recess","Assets/Art/Intro/Family brown cloth.mat",new Color(.035f,.03f,.025f));
        if(dark==null){dark=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(dark,Art+"Door shadowed recess.mat");dark.SetColor("_BaseColor",new Color(.025f,.02f,.015f));}

        var surround=new Geometry();
        for(int i=0;i<48;i++)
        {
            float a=Mathf.Lerp(-1.65f,1.65f,i/48f),b=Mathf.Lerp(-1.65f,1.65f,(i+1)/48f);
            float ya=Mathf.Abs(a)<1.15f?Top(a)-.025f:0,yb=Mathf.Abs(b)<1.15f?Top(b)-.025f:0;
            surround.Face(new Vector3(Curve(a,7.80f),ya,a),new Vector3(Curve(b,7.80f),yb,b),new Vector3(Curve(b,7.80f),4.15f,b),new Vector3(Curve(a,7.80f),4.15f,a));
        }
        MeshPart(root,"Door curved masonry surround",surround,stone);
        var frame=new Geometry();
        foreach(float sign in new[]{-1f,1f})frame.Extrude(new[]{new Vector2(sign*1.15f,0),new Vector2(sign*1.40f,0),new Vector2(sign*1.40f,2.35f),new Vector2(sign*1.15f,2.35f)},-.5f,true);
        for(int i=0;i<15;i++)
        {
            float a=i*Mathf.PI/15+.006f,b=(i+1)*Mathf.PI/15-.006f;
            frame.Extrude(new[]{new Vector2(Mathf.Cos(a)*1.15f,2.35f+Mathf.Sin(a)*1.15f),new Vector2(Mathf.Cos(b)*1.15f,2.35f+Mathf.Sin(b)*1.15f),new Vector2(Mathf.Cos(b)*1.4f,2.35f+Mathf.Sin(b)*1.4f),new Vector2(Mathf.Cos(a)*1.4f,2.35f+Mathf.Sin(a)*1.4f)},-.5f,true);
        }
        MeshPart(root,"Door carved stone arch",frame,stone);
        Box(root,"Door limestone threshold",new Vector3(-.22f,.04f,0),new Vector3(.8f,.08f,2.7f),stone);
        var recess=new Geometry();
        for(int i=0;i<24;i++)
        {float a=Mathf.Lerp(-1.15f,1.15f,i/24f),b=Mathf.Lerp(-1.15f,1.15f,(i+1)/24f);recess.Extrude(new[]{new Vector2(a,0),new Vector2(b,0),new Vector2(b,Top(b)),new Vector2(a,Top(a))},.12f,false,.18f);}
        MeshPart(root,"Door dark interior",recess,dark);

        var hinge=new GameObject("Door oak hinge").transform;hinge.SetParent(root,false);hinge.localPosition=new Vector3(-.3f,0,-1.15f);
        var planks=new Geometry();
        for(int i=0;i<8;i++)
        {
            float a=-1.14f+i*2.28f/8+.006f,b=-1.14f+(i+1)*2.28f/8-.006f;
            // Local z starts at the hinge; retain a genuinely arched wooden silhouette.
            planks.Extrude(new[]{new Vector2(a+1.15f,.1f),new Vector2(b+1.15f,.1f),new Vector2(b+1.15f,Top(b)-.025f),new Vector2(a+1.15f,Top(a)-.025f)},-.06f,false,.08f);
        }
        MeshPart(hinge,"Door green oak planks",planks,wood);
        foreach(float y in new[]{.65f,2.2f})
        {
            Box(hinge,"Door bronze strap",new Vector3(-.09f,y,1.15f),new Vector3(.07f,.11f,2.06f),metal);
            foreach(float z in new[]{.25f,.65f,1.1f,1.55f,2.0f})
                Box(hinge,"Door bronze rivet",new Vector3(-.14f,y,z),new Vector3(.07f,.07f,.07f),metal,PrimitiveType.Sphere);
        }
        Box(hinge,"Door handle plate",new Vector3(-.105f,1.22f,2.0f),new Vector3(.07f,.38f,.16f),metal);
        Box(hinge,"Door handle stem",new Vector3(-.20f,1.28f,2.0f),new Vector3(.18f,.07f,.07f),metal);
        Box(hinge,"Door handle grip",new Vector3(-.29f,1.18f,2.0f),new Vector3(.07f,.24f,.07f),metal);
        var crestSource=GameObject.Find("Aurenfeld family crest");
        var crest=new GameObject("Aurenfeld carved door crest",typeof(MeshFilter),typeof(MeshRenderer));crest.transform.SetParent(hinge,false);
        crest.transform.localPosition=new Vector3(-.11f,2.78f,1.15f);crest.transform.localRotation=Quaternion.Euler(0,-90,0);crest.transform.localScale=new Vector3(.48f,.60f,1);
        crest.GetComponent<MeshFilter>().sharedMesh=crestSource.GetComponent<MeshFilter>().sharedMesh;crest.GetComponent<Renderer>().sharedMaterial=crestSource.GetComponent<Renderer>().sharedMaterial;
        var animation=door.GetComponent<TowerRoomDoor>();if(animation==null)animation=door.gameObject.AddComponent<TowerRoomDoor>();animation.SetHinge(hinge);animation.SetOpenness(0);
        var blocker=door.Find("Door approach blocker");if(blocker!=null)blocker.localPosition=new Vector3(-.9f,1.9f,0);
    }
    private static Material Clone(string name,string source,Color tint)
    {
        string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){var original=AssetDatabase.LoadAssetAtPath<Material>(source);if(original==null)return null;mat=new Material(original);AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",tint);EditorUtility.SetDirty(mat);return mat;
    }
    private static void Box(Transform parent,string name,Vector3 at,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
    {var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(obj.GetComponent<Collider>());}
    private static void MeshPart(Transform root,string name,Geometry geometry,Material material)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(geometry.v);mesh.SetTriangles(geometry.t,0);
        var uv=new List<Vector2>();foreach(var p in geometry.v)uv.Add(name=="Door green oak planks"?new Vector2(p.y*.45f,p.z*.55f):new Vector2(p.z*.55f,p.y*.45f));mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Art+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);obj.GetComponent<MeshFilter>().sharedMesh=saved;obj.GetComponent<Renderer>().sharedMaterial=material;
    }
}
