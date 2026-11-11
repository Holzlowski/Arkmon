using System.Collections.Generic;
using UnityEngine;

// Flatten the tower forecourt before paths, decoration and characters sample its height.
public sealed class CourtyardTowerGround : MonoBehaviour
{
    readonly List<(MeshFilter filter,Mesh original,Mesh copy)> changed=new List<(MeshFilter,Mesh,Mesh)>();
    internal static void PrepareTerrain()
    {
        if(GameObject.Find("Tower level forecourt")!=null)return;
        var style=new GameObject("Tower level forecourt").AddComponent<CourtyardTowerGround>();
        foreach(var f in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(f.sharedMesh==null || f.name!="Rolling courtyard meadow" && f.name!="Curved courtyard path")continue;
            var original=f.sharedMesh;var copy=Instantiate(original);var v=copy.vertices;bool touched=false;
            for(int i=0;i<v.Length;i++)
            {
                var w=f.transform.TransformPoint(v[i]);
                float edge=Mathf.Max(Mathf.Abs(w.x+34)-3.8f,Mathf.Abs(w.z-4)-2.8f);
                float weight=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/1.5f));
                float paddockEdge=Mathf.Max(Mathf.Abs(w.x+19.75f)-3.55f,Mathf.Abs(w.z-2.95f)-3.35f);
                weight=Mathf.Max(weight,1-Mathf.SmoothStep(0,1,Mathf.Clamp01(paddockEdge/1.4f)));
                if(weight==0)continue;
                w.y=Mathf.Lerp(w.y,f.name=="Curved courtyard path"?.03f:.005f,weight);
                v[i]=f.transform.InverseTransformPoint(w);touched=true;
            }
            if(!touched){Destroy(copy);continue;}
            copy.name=original.name+" tower approach level";copy.vertices=v;copy.RecalculateNormals();copy.RecalculateBounds();
            f.sharedMesh=copy;var c=f.GetComponent<MeshCollider>();if(c!=null)c.sharedMesh=copy;
            style.changed.Add((f,original,copy));
        }
        Physics.SyncTransforms();
    }
    void OnDestroy()
    {
        foreach(var item in changed)
        {
            if(item.filter!=null && item.filter.sharedMesh==item.copy)
            {item.filter.sharedMesh=item.original;var c=item.filter.GetComponent<MeshCollider>();if(c!=null)c.sharedMesh=item.original;}
            if(item.copy!=null)Destroy(item.copy);
        }
    }
}
