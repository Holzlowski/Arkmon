using System.Linq;
using UnityEngine;

// Small work props stay tied to existing furniture. They add no obstacles or interactions.
[DefaultExecutionOrder(9500)]
public sealed class CourtyardGardenDetails : MonoBehaviour
{
    readonly System.Collections.Generic.List<Transform> billboards = new System.Collections.Generic.List<Transform>();
    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null) return;
        foreach (var prop in billboards)
            if (prop != null) prop.rotation = camera.transform.rotation;
    }

    internal static void Install()
    {
        var house=GameObject.Find("Garden cottage");
        if(house==null || house.transform.Find("Marta garden details")!=null)return;
        var root=new GameObject("Marta garden details");root.transform.SetParent(house.transform,false);
        root.AddComponent<CourtyardGardenDetails>();
        var table=house.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Garden potting table");
        var bench=GameObject.Find("Garden bench seat");
        if(bench!=null)
        {
            var seat=bench.GetComponent<Renderer>().bounds;
            Prop(root.transform,"Marta seed organizer","Seeds",new Vector3(seat.min.x+.49f,seat.max.y+.015f,seat.center.z-.10f),true);
        }
        if(table!=null)
        {
            // Replace two small pots instead of stacking the new tray through existing objects.
            foreach(Transform child in table)
                if(child.name=="Open terracotta pot" && child.localPosition.x>-.1f)child.gameObject.SetActive(false);
            var cloth=table.Find("Folded garden cloth");
            if(cloth!=null)cloth.localPosition=new Vector3(-.43f,1.045f,-.19f);
            Prop(root.transform,"Marta seedling tray","Seedlings",table.TransformPoint(new Vector3(.34f,1.04f,-.15f)),true);
        }
        var tools=Prop(root.transform,"Marta hanging garden tools","Tools",house.transform.TransformPoint(new Vector3(-1.74f,.35f,-2.66f)),false);
        if(tools!=null)tools.transform.rotation=house.transform.rotation;
        var beds=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden soil bed").OrderBy(r=>r.transform.position.x).ToArray();
        string[] labels={"Cabbage","Carrot","Sprout"};
        for(int i=0;i<Mathf.Min(beds.Length,labels.Length);i++)
        {
            var bounds=beds[i].bounds;
            Prop(root.transform,"Marta bed marker "+labels[i],labels[i],new Vector3(bounds.min.x+.22f,bounds.max.y-.02f,bounds.min.z+.30f),true);
        }
    }
    static SpriteRenderer Prop(Transform parent,string name,string resource,Vector3 point,bool faceCamera)
    {
        var sprite=Resources.Load<Sprite>("GardenDetails/"+resource);
        if(sprite==null)return null;
        var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(parent,false);obj.transform.position=point;
        var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=sprite;
        // Props use the same renderer/material convention as Marta, with ordinary world-depth sorting.
        if(faceCamera)
        {
            parent.GetComponent<CourtyardGardenDetails>().billboards.Add(obj.transform);
            if(Camera.main!=null)obj.transform.rotation=Camera.main.transform.rotation;
        }
        return renderer;
    }
}