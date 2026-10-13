using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureRoundTowerRoom
{
    const string Art="Assets/Art/SceneStyle/";
    const float Radius=7.82f, CenterZ=-1.7f;
    [MenuItem("Arkmon/Build round tower room")]
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        ConfigureArchedWindow.Apply();Apply();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static Vector3 Point(float radius,float angle,float y)=>new Vector3(Mathf.Sin(angle)*radius,y,Mathf.Cos(angle)*radius);
    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {int n=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
    static void Segment(List<Vector3> v,List<int> t,float a,float b,float bottom,float top)
    {
        float outer=Radius+.35f;
        Quad(v,t,Point(Radius,a,bottom),Point(Radius,a,top),Point(Radius,b,top),Point(Radius,b,bottom));
        Quad(v,t,Point(outer,a,bottom),Point(outer,b,bottom),Point(outer,b,top),Point(outer,a,top));
        Quad(v,t,Point(Radius,a,top),Point(outer,a,top),Point(outer,b,top),Point(Radius,b,top));
        Quad(v,t,Point(Radius,b,bottom),Point(outer,b,bottom),Point(outer,a,bottom),Point(Radius,a,bottom));
        Quad(v,t,Point(Radius,a,bottom),Point(outer,a,bottom),Point(outer,a,top),Point(Radius,a,top));
        Quad(v,t,Point(Radius,b,top),Point(outer,b,top),Point(outer,b,bottom),Point(Radius,b,bottom));
    }
    static Mesh Save(string name,List<Vector3> vertices,List<int> triangles,bool wall=false)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        var uv=new List<Vector2>();foreach(var p in vertices)uv.Add(wall?new Vector2(Mathf.Atan2(p.x,p.z)*Radius*.3f,p.y*.3f):new Vector2(p.x*.2f,p.z*.2f));
        mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Art+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
    }
    static Mesh Disc(string name,float thickness)
    {
        var v=new List<Vector3>();var t=new List<int>();const int count=96;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
            int n=v.Count;v.AddRange(new[]{Vector3.zero,Point(Radius,a,0),Point(Radius,b,0),new Vector3(0,-thickness,0),Point(Radius,b,-thickness),Point(Radius,a,-thickness)});
            t.AddRange(new[]{n,n+1,n+2,n+3,n+4,n+5});
            Quad(v,t,Point(Radius,a,0),Point(Radius,a,-thickness),Point(Radius,b,-thickness),Point(Radius,b,0));
        }
        return Save(name,v,t);
    }
    static void SetDisc(string objectName,string meshName,float height,float thickness)
    {
        var obj=GameObject.Find(objectName);if(obj==null)return;
        foreach(var col in obj.GetComponents<Collider>())Object.DestroyImmediate(col);
        obj.transform.position=new Vector3(0,height,CenterZ);obj.transform.localScale=Vector3.one;
        Mesh mesh=Disc(meshName,thickness);obj.GetComponent<MeshFilter>().sharedMesh=mesh;
        if(objectName=="Stone floor")obj.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void Wall(Transform root,string name,List<Vector3> v,List<int> t,Material material,bool visible)
    {
        var mesh=Save(name,v,t,true);var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));obj.transform.SetParent(root,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshCollider>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;obj.GetComponent<Renderer>().enabled=visible;
    }
    public static void Apply()
    {
        Transform room=GameObject.Find("Tower room interior").transform;
        Material source=AssetDatabase.LoadAssetAtPath<Material>(Art+"Demo painted castle.mat");
        string materialPath=Art+"Round tower painted stone.mat";
        Material stone=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(stone==null){stone=new Material(source);AssetDatabase.CreateAsset(stone,materialPath);}
        else EditorUtility.CopySerialized(source,stone);
        stone.SetFloat("_MorningStrength",1);EditorUtility.SetDirty(stone);
        var floorMaterial=GameObject.Find("Stone floor").GetComponent<Renderer>().sharedMaterial;floorMaterial.SetFloat("_MorningStrength",1);EditorUtility.SetDirty(floorMaterial);
        var arch=GameObject.Find("Window curved stone wall");if(arch!=null)arch.GetComponent<Renderer>().sharedMaterial=stone;
        foreach(string name in new[]{"West wall","East wall","Window wall left","Window wall right","Window sill","Front invisible boundary","Round tower shell"})
        {var old=GameObject.Find(name);if(old!=null)Object.DestroyImmediate(old);}
        SetDisc("Stone floor","Round tower floor",0,.5f);SetDisc("Tower ceiling","Round tower ceiling",5.18f,.25f);
        var shell=new GameObject("Round tower shell").transform;shell.SetParent(room,false);shell.localPosition=new Vector3(0,0,CenterZ);
        var visible=new List<Vector3>();var visibleTriangles=new List<int>();var hidden=new List<Vector3>();var hiddenTriangles=new List<int>();
        const int count=72;
        for(int i=0;i<count;i++)
        {
            float degrees=i*360f/count,mid=degrees+2.5f;float signed=mid>180?mid-360:mid;
            float a=degrees*Mathf.Deg2Rad,b=(degrees+5)*Mathf.Deg2Rad;
            if(Mathf.Abs(signed)>110){Segment(hidden,hiddenTriangles,a,b,0,5.2f);continue;}
            if(Mathf.Abs(signed)<10)Segment(visible,visibleTriangles,a,b,0,2);
            else if(signed>60&&signed<80)Segment(visible,visibleTriangles,a,b,3.8f,5.2f);
            else Segment(visible,visibleTriangles,a,b,0,5.2f);
        }
        Wall(shell,"Round tower visible walls",visible,visibleTriangles,stone,true);
        Wall(shell,"Round tower cutaway boundary",hidden,hiddenTriangles,stone,false);
        // Physics still closes the high window opening; only the view is open.
        var guard=new GameObject("Window fall protection",typeof(BoxCollider));guard.transform.SetParent(shell,false);guard.transform.localPosition=new Vector3(0,3.3f,7.7f);guard.GetComponent<BoxCollider>().size=new Vector3(2.8f,2.65f,.7f);
        GameObject bed=GameObject.Find("Canopy bed");bed.transform.position=new Vector3(-4.25f,0,1.9f);
        var door=GameObject.Find("Courtyard door");door.transform.position=new Vector3(7.32f,0,1f);door.transform.rotation=Quaternion.Euler(0,-20,0);
        var backing=GameObject.CreatePrimitive(PrimitiveType.Cube);backing.name="Courtyard door stone recess";backing.transform.SetParent(shell,false);
        backing.transform.position=door.transform.TransformPoint(new Vector3(.12f,1.9f,0));backing.transform.rotation=door.transform.rotation;backing.transform.localScale=new Vector3(.1f,3.8f,2.9f);
        // This is an interaction exit, not a walk-through hole to the void.
        backing.GetComponent<Renderer>().sharedMaterial=stone;
        var oldBeams=new List<GameObject>();foreach(Transform child in room)if(child.name.StartsWith("Ceiling beam "))oldBeams.Add(child.gameObject);
        foreach(var old in oldBeams)Object.DestroyImmediate(old);
        var decor=GameObject.Find("Tower ceiling decoration");if(decor!=null)
        {
            var cross=new List<GameObject>();foreach(Transform child in decor.transform)if(child.name=="Oak crossbeam")cross.Add(child.gameObject);
            foreach(var old in cross)Object.DestroyImmediate(old);
        }
        var timbers=new GameObject("Round tower roof timbers").transform;timbers.SetParent(shell,false);
        Material wood=AssetDatabase.LoadAssetAtPath<Material>(Art+"Tower painted oak.mat");
        Mesh beamMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"Tower oak beam box.asset");
        for(int i=0;i<8;i++)
        {
            float angle=i*45f;var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);beam.name="Radial oak roof beam";beam.transform.SetParent(timbers,false);
            beam.transform.localPosition=Point((Radius-.3f)*.5f,angle*Mathf.Deg2Rad,4.94f);beam.transform.localRotation=Quaternion.Euler(0,angle,0);beam.transform.localScale=new Vector3(.2f,.24f,Radius-.3f);
            if(beamMesh!=null)beam.GetComponent<MeshFilter>().sharedMesh=beamMesh;
            beam.GetComponent<Renderer>().sharedMaterial=wood;Object.DestroyImmediate(beam.GetComponent<Collider>());
        }
        var hub=GameObject.CreatePrimitive(PrimitiveType.Cylinder);hub.name="Oak ceiling hub";hub.transform.SetParent(timbers,false);hub.transform.localPosition=new Vector3(0,4.92f,0);hub.transform.localScale=new Vector3(.55f,.12f,.55f);hub.GetComponent<Renderer>().sharedMaterial=wood;Object.DestroyImmediate(hub.GetComponent<Collider>());
        var player=GameObject.FindWithTag("Player");if(player!=null)player.transform.position=bed.GetComponent<PaintedCanopyBed>().BedsidePosition;
    }
}
