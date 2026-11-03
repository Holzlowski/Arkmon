using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class FinishTowerIntro
{
    private const string Art="Assets/Art/Intro/";
    [MenuItem("Arkmon/Finish playable tower introduction")]
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        BuildPaintedTowerDoor.Apply();FixTowerDoorAndWindow.Apply();
        var camera=new SerializedObject(Object.FindFirstObjectByType<TowerRoomCamera>());
        camera.FindProperty("focus").vector3Value=new Vector3(.85f,1.55f,1.70f);
        camera.FindProperty("angles").vector3Value=new Vector3(11,0,0);
        camera.FindProperty("distance").floatValue=10.8f;camera.FindProperty("fieldOfView").floatValue=46.5f;
        camera.FindProperty("trackingLimits").vector2Value=new Vector2(2.8f,.38f);camera.FindProperty("trackingWeight").floatValue=.54f;camera.FindProperty("smoothing").floatValue=.12f;camera.ApplyModifiedPropertiesWithoutUndo();
        UpperWall();FrameOriginalVista();
        var boundary=GameObject.Find("Tower camera foreground boundary");
        if(boundary==null){boundary=new GameObject("Tower camera foreground boundary",typeof(BoxCollider));boundary.transform.SetParent(GameObject.Find("Tower room interior").transform,false);}
        boundary.transform.position=new Vector3(0,2.5f,-4.8f);boundary.GetComponent<BoxCollider>().size=new Vector3(17,6,.2f);
        Guard("Tower left cutaway guard",new Vector3(-6.9f,0,1),new Vector3(-5.0f,0,-4.8f));
        Guard("Tower right cutaway guard",new Vector3(6.8f,0,1),new Vector3(5.4f,0,-4.8f));
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void Guard(string name,Vector3 start,Vector3 end)
    {
        var obj=GameObject.Find(name);if(obj==null){obj=new GameObject(name,typeof(BoxCollider));obj.transform.SetParent(GameObject.Find("Tower room interior").transform,false);}
        obj.transform.position=(start+end)*.5f+Vector3.up*2.5f;obj.transform.rotation=Quaternion.LookRotation(end-start);
        obj.GetComponent<BoxCollider>().size=new Vector3(.2f,6,Vector3.Distance(start,end)+.2f);
    }
    private static void UpperWall()
    {
        var old=GameObject.Find("Tower upper cutaway masonry");if(old!=null)Object.DestroyImmediate(old);
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<44;i++)
        {
            float a=(-110+i*5)*Mathf.Deg2Rad,b=a+5*Mathf.Deg2Rad;int n=v.Count;
            foreach(var p in new[]{Point(a,5.18f),Point(a,7.5f),Point(b,7.5f),Point(b,5.18f)})v.Add(p);
            t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        var mesh=new Mesh{name="Tower upper cutaway masonry"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Art+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
        var obj=new GameObject("Tower upper cutaway masonry",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(GameObject.Find("Tower room interior").transform,false);
        obj.GetComponent<MeshFilter>().sharedMesh=saved;obj.GetComponent<Renderer>().sharedMaterial=GameObject.Find("Round tower visible walls").GetComponent<Renderer>().sharedMaterial;
    }
    private static Vector3 Point(float angle,float y)=>new Vector3(Mathf.Sin(angle)*7.82f,y,Mathf.Cos(angle)*7.82f-1.7f);
    [MenuItem("Arkmon/Frame original tower vista")]
    public static void RestoreOriginalVista()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        FrameOriginalVista();
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void FrameOriginalVista()
    {
        var extra=GameObject.Find("First town beyond the golden fields");
        if(extra!=null)Object.DestroyImmediate(extra);
        // Restore the countryside composition from before the town framing pass.
        GameObject.Find("Distant field hills").transform.position=new Vector3(0,3.4f,6.9f);
        GameObject.Find("Hills below the tower").transform.position=new Vector3(0,4.65f,6.581f);
        foreach(string name in new[]{"Distant field hills","Hills below the tower"})
            GameObject.Find(name).GetComponent<Renderer>().sharedMaterial=
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/"+name+".mat");
    }
}
