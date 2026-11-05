using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(9501)]
public sealed class CourtyardGardenStyle : MonoBehaviour
{
    readonly List<Transform> herbs=new List<Transform>();
    internal static void Install()
    {
        var house=GameObject.Find("Garden cottage")?.transform;
        if(house==null || house.Find("Illustrated garden cottage")!=null)return;
        var oak=Resources.Load<Material>("CourtyardStyle/Oak");
        var roof=Resources.Load<Material>("CourtyardStyle/Roof");
        var plaster=Resources.Load<Material>("CourtyardStyle/Plaster");
        var stone=Resources.Load<Material>("CourtyardStyle/Stone");
        var sage=Resources.Load<Material>("CourtyardStyle/Garden sage");
        var clay=Resources.Load<Material>("CourtyardStyle/Garden clay");
        if(oak==null || roof==null || plaster==null || stone==null || sage==null || clay==null)return;
        var holder=new GameObject("Illustrated garden cottage");holder.transform.SetParent(house,false);
        var style=holder.AddComponent<CourtyardGardenStyle>();
        foreach(var renderer in house.GetComponentsInChildren<MeshRenderer>())
        {
            string source=renderer.sharedMaterial!=null?renderer.sharedMaterial.name:"";
            Material selected=source=="Marta painted oak"?oak:source=="Marta weathered terracotta"?roof:
                source=="Marta limewash"?plaster:source=="Marta sage shutters"?sage:source=="Marta clay pots"?clay:
                renderer.name.Contains("stone") || renderer.name=="Chimney cap" || renderer.name=="Worn approach flagstone"?stone:null;
            if(selected!=null)Assign(renderer,selected);
        }
        // Borders, bean supports and the nearby seat join the same palette. Soil is never repainted:
        // its material/property block belongs to the watering and wetness systems.
        foreach(var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if(renderer.name=="Garden border" || renderer.name.StartsWith("Bean trellis") || renderer.name=="Garden bench seat")Assign(renderer,oak);
            if(renderer.name=="Garden bench stone foot")Assign(renderer,stone);
        }
        foreach(var cluster in house.GetComponentsInChildren<Transform>().Where(t=>t.name=="Garden herb cluster").ToArray())
            style.Herb(cluster);
    }
    static void Assign(MeshRenderer renderer,Material material)
    {
        renderer.sharedMaterial=material;
        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
        var scale=renderer.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        block.SetVector("_ObjectScale",scale);block.SetFloat("_GrainAxis",scale.x>scale.y && scale.x>scale.z?0:scale.z>scale.y?2:1);
        var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
        block.SetFloat("_EdgeWidth",mesh!=null && mesh.name=="Cube"?.018f:0);
        if(renderer.name=="Marta curved tiled roof")block.SetFloat("_TextureScale",.45f);
        renderer.SetPropertyBlock(block);
    }
    void Herb(Transform cluster)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/Fern");if(sprite==null)return;
        var old=cluster.GetComponentsInChildren<MeshRenderer>();if(old.Length==0)return;
        var bounds=old[0].bounds;foreach(var renderer in old)bounds.Encapsulate(renderer.bounds);
        float height=Mathf.Clamp(bounds.size.y*1.35f,.16f,.42f);
        var paper=new GameObject("Illustrated garden herbs",typeof(SpriteRenderer));paper.transform.SetParent(cluster,false);
        paper.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        var art=paper.GetComponent<SpriteRenderer>();art.sprite=sprite;art.color=new Color(.85f,.94f,.80f);
        herbs.Add(paper.transform);foreach(var renderer in old)renderer.enabled=false;
    }
    void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var herb in herbs)herb.rotation=camera.transform.rotation;
    }
}
