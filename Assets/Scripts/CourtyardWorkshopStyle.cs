using UnityEngine;

// Local material assignments keep the tower, castle and archived scenes untouched.
public static class CourtyardWorkshopStyle
{
    internal static void Apply(Transform workshop)
    {
        var oak=Resources.Load<Material>("CourtyardStyle/Oak");
        var plaster=Resources.Load<Material>("CourtyardStyle/Plaster");
        var roof=Resources.Load<Material>("CourtyardStyle/Roof");
        var stone=Resources.Load<Material>("CourtyardStyle/Stone");
        if(oak==null || plaster==null || roof==null || stone==null)return;
        foreach(var renderer in workshop.GetComponentsInChildren<MeshRenderer>())
        {
            string name=renderer.name;
            if(name.Contains("dark glass") || name.Contains("recess") || name.Contains("iron") ||
               name.Contains("latch") || name.Contains("ventilation") || name.Contains("emblem"))continue;
            var material=name.Contains("plaster") || name.Contains("solid gables")?plaster:
                name.Contains("roof")?roof:name.Contains("stone") || name.Contains("threshold")?stone:oak;
            renderer.sharedMaterial=material;
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            var scale=renderer.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            block.SetVector("_ObjectScale",scale);
            block.SetFloat("_GrainAxis",scale.x>scale.y && scale.x>scale.z?0:scale.z>scale.y?2:1);
            var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
            block.SetFloat("_EdgeWidth",mesh!=null && mesh.name=="Cube"?.018f:0);
            renderer.SetPropertyBlock(block);
        }
    }
}
