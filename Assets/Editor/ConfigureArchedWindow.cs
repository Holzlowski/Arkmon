using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureArchedWindow
{
    const string Art="Assets/Art/SceneStyle/";
    [MenuItem("Arkmon/Build small arched tower window")]
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");Apply();if(GameObject.Find("Round tower shell")!=null)ConfigureRoundTowerRoom.Apply();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static void Box(string name,Vector3 position,Vector3 size,Material material,Transform parent)
    {
        var obj=GameObject.Find(name);
        if(obj==null){obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);}
        obj.transform.position=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void MeshPart(Transform root,string name,List<Vector3> vertices,List<int> triangles,Material material)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        var uv=new List<Vector2>();foreach(var v in vertices)uv.Add(new Vector2(v.x*.4f,v.y*.4f));mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Art+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);
        obj.GetComponent<MeshFilter>().sharedMesh=saved;obj.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {int n=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
    public static void Apply()
    {
        Transform room=GameObject.Find("Tower room interior").transform;
        Material stone=AssetDatabase.LoadAssetAtPath<Material>(Art+"Demo painted castle.mat");
        string trimPath=Art+"Window painted limestone.mat";
        Material trim=AssetDatabase.LoadAssetAtPath<Material>(trimPath);
        if(trim==null){trim=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(trim,trimPath);}
        trim.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"DemoStone.png"));
        trim.SetColor("_BaseColor",new Color(.92f,.87f,.74f));trim.SetFloat("_Smoothness",.03f);EditorUtility.SetDirty(trim);
        foreach(string name in new[]{"Polished window sill","Window left trim","Window right trim","Window lintel","Arched tower window"})
        {var old=GameObject.Find(name);if(old!=null)Object.DestroyImmediate(old);}
        const float radius=1.35f,spring=3.15f,bottom=2f;
        Box("Window wall left",new Vector3(-4.675f,2.5f,6),new Vector3(6.65f,5,.7f),stone,room);
        Box("Window wall right",new Vector3(4.675f,2.5f,6),new Vector3(6.65f,5,.7f),stone,room);
        Box("Window sill",new Vector3(0,1,6),new Vector3(2.7f,2,.7f),stone,room);
        var root=new GameObject("Arched tower window").transform;root.SetParent(room,false);
        var vertices=new List<Vector3>();var triangles=new List<int>();
        const int segments=48;
        for(int i=0;i<segments;i++)
        {
            float x0=Mathf.Lerp(-radius,radius,i/(float)segments),x1=Mathf.Lerp(-radius,radius,(i+1)/(float)segments);
            float y0=spring+Mathf.Sqrt(Mathf.Max(0,radius*radius-x0*x0)),y1=spring+Mathf.Sqrt(Mathf.Max(0,radius*radius-x1*x1));
            Quad(vertices,triangles,new Vector3(x0,y0,5.65f),new Vector3(x0,5,5.65f),new Vector3(x1,5,5.65f),new Vector3(x1,y1,5.65f));
            Quad(vertices,triangles,new Vector3(x1,y1,6.35f),new Vector3(x1,5,6.35f),new Vector3(x0,5,6.35f),new Vector3(x0,y0,6.35f));
            Quad(vertices,triangles,new Vector3(x0,y0,6.35f),new Vector3(x0,y0,5.65f),new Vector3(x1,y1,5.65f),new Vector3(x1,y1,6.35f));
        }
        MeshPart(root,"Window curved stone wall",vertices,triangles,stone);
        vertices=new List<Vector3>();triangles=new List<int>();
        for(int i=0;i<segments;i++)
        {
            float a=i*Mathf.PI/segments,b=(i+1)*Mathf.PI/segments;
            Vector3 inner0=new Vector3(Mathf.Cos(a)*radius,spring+Mathf.Sin(a)*radius,5.48f),inner1=new Vector3(Mathf.Cos(b)*radius,spring+Mathf.Sin(b)*radius,5.48f);
            Vector3 outer0=new Vector3(Mathf.Cos(a)*(radius+.18f),spring+Mathf.Sin(a)*(radius+.18f),5.48f),outer1=new Vector3(Mathf.Cos(b)*(radius+.18f),spring+Mathf.Sin(b)*(radius+.18f),5.48f);
            Quad(vertices,triangles,inner0,inner1,outer1,outer0);
        }
        MeshPart(root,"Window rounded limestone frame",vertices,triangles,trim);
        foreach(float x in new[]{-1.44f,1.44f})Box("Window jamb "+x,new Vector3(x,(spring+bottom)*.5f,5.52f),new Vector3(.18f,spring-bottom,.25f),trim,root);
        Box("Polished window sill",new Vector3(0,1.97f,5.65f),new Vector3(3.1f,.16f,1),trim,root);
        var scenery=GameObject.Find("Painted window scenery");if(scenery!=null)
        {
            ConfigureDemoPolish.PaintedVista(room);
        }
    }
}
