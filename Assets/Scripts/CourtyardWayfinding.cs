using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

// Two restrained wooden direction markers on the verge, never in the walking lane.
public sealed class CourtyardWayfinding : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    GameObject oldGateSign;
    bool oldGateActive;
    internal static void Install()
    {
        if(GameObject.Find("Courtyard route markers")!=null)return;
        var oak=Resources.Load<Material>("CourtyardStyle/Stable oak");if(oak==null)return;
        var style=new GameObject("Courtyard route markers").AddComponent<CourtyardWayfinding>();
        style.oldGateSign=GameObject.Find("Wayfinding - Burgtor");
        if(style.oldGateSign!=null){style.oldGateActive=style.oldGateSign.activeSelf;style.oldGateSign.SetActive(false);}
        style.Marker("Tower landing directions",new Vector3(-28.4f,0,3.8f),"Burgtor","Werkstatt",oak);
        style.Marker("Garden junction directions",new Vector3(32.3f,0,.2f),"Burgtor","Garten",oak);
    }
    static Vector3 Ground(Vector3 p)
    {
        float height=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(p+Vector3.up*20,Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground"))height=Mathf.Max(height,hit.point.y);
        if(!float.IsNegativeInfinity(height))p.y=height;return p;
    }
    void Marker(string name,Vector3 at,string destination,string side,Material wood)
    {
        var root=new GameObject(name).transform;root.SetParent(transform,false);root.position=Ground(at);
        var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Weathered direction post";post.transform.SetParent(root,false);
        post.transform.localPosition=new Vector3(0,.72f,0);post.transform.localScale=new Vector3(.12f,1.44f,.12f);
        var collider=post.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        post.GetComponent<MeshRenderer>().sharedMaterial=wood;
        Board(root,destination,1.25f,1,wood);Board(root,side,.82f,-1,wood);
    }
    void Board(Transform root,string text,float y,int direction,Material wood)
    {
        var shape=new[]{new Vector2(-.82f,-.19f),new Vector2(.62f,-.19f),new Vector2(.94f,0),new Vector2(.62f,.19f),new Vector2(-.82f,.19f)};
        var v=new Vector3[10];for(int i=0;i<5;i++){v[i]=new Vector3(shape[i].x*direction,shape[i].y,-.08f);v[i+5]=new Vector3(shape[i].x*direction,shape[i].y,.04f);}
        var triangles=new List<int>();
        for(int i=1;i<4;i++){triangles.AddRange(new[]{0,i+1,i,5,5+i,6+i});}
        for(int i=0;i<5;i++){int n=(i+1)%5;triangles.AddRange(new[]{i,n,n+5,i,n+5,i+5});}
        if(direction<0)for(int i=0;i<triangles.Count;i+=3){int swap=triangles[i];triangles[i]=triangles[i+1];triangles[i+1]=swap;}
        var mesh=new Mesh{name="Carved directional board"};mesh.vertices=v;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var board=new GameObject("Direction to "+text,typeof(MeshFilter),typeof(MeshRenderer));board.transform.SetParent(root,false);board.transform.localPosition=Vector3.up*y;
        board.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=board.GetComponent<MeshRenderer>();renderer.sharedMaterial=wood;
        var block=new MaterialPropertyBlock();block.SetVector("_ObjectScale",new Vector3(1.76f,.38f,.12f));renderer.SetPropertyBlock(block);
        var label=new GameObject("Painted destination",typeof(TextMeshPro)).GetComponent<TextMeshPro>();label.transform.SetParent(board.transform,false);
        label.transform.localPosition=new Vector3(-.08f*direction,0,-.087f);label.rectTransform.sizeDelta=new Vector2(1.35f,.34f);
        label.font=PaperUIStyle.Load().font;label.fontSize=2.1f;label.alignment=TextAlignmentOptions.Center;
        label.color=new Color(1,.85f,.57f);label.text=text;
    }
    void OnDestroy()
    {
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        if(oldGateSign!=null)oldGateSign.SetActive(oldGateActive);
    }
}
