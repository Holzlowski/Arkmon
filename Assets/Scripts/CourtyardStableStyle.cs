using System.Collections.Generic;
using UnityEngine;

// Reuse the workshop's illustrated lighting, with the stable's own dark oak/green roof palette.
public sealed class CourtyardStableStyle : MonoBehaviour
{
    readonly List<Mesh> groundMeshes=new List<Mesh>();
    Material contact;
    internal static void Install(Transform stable)
    {
        if(stable.Find("Illustrated stable setting")!=null)return;
        var oak=Resources.Load<Material>("CourtyardStyle/Stable oak");
        var roof=Resources.Load<Material>("CourtyardStyle/Stable roof");
        var plaster=Resources.Load<Material>("CourtyardStyle/Plaster");
        var stone=Resources.Load<Material>("CourtyardStyle/Stone");
        if(oak==null || roof==null || plaster==null || stone==null)return;
        var obj=new GameObject("Illustrated stable setting");obj.transform.SetParent(stable,false);
        var style=obj.AddComponent<CourtyardStableStyle>();
        foreach(var renderer in stable.GetComponentsInChildren<MeshRenderer>())
        {
            // Horses have their own deforming transparent material; never repaint character meshes.
            if(renderer.GetComponentInParent<CourtyardHorse>()!=null)continue;
            string source=renderer.sharedMaterial!=null?renderer.sharedMaterial.name:"";
            Material selected=renderer.name=="Stable stone footing"?stone:
                source=="Marta painted oak"?oak:source=="Hannes slate shingles"?roof:
                source=="Hannes warm plaster"?plaster:null;
            if(selected==null)continue;
            renderer.sharedMaterial=selected;
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            var scale=renderer.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            block.SetVector("_ObjectScale",scale);block.SetFloat("_GrainAxis",scale.x>scale.y && scale.x>scale.z?0:scale.z>scale.y?2:1);
            var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
            block.SetFloat("_EdgeWidth",mesh!=null && mesh.name=="Cube"?.018f:0);renderer.SetPropertyBlock(block);
        }
        foreach(Transform child in stable)
            if(child.name=="Forecourt scattered straw")child.gameObject.SetActive(false);
        style.Patch("Worn horse care ground",new Vector3(1.05f,0,-4.05f),new Vector2(2.45f,1.4f),"Stable earth",.045f);
        style.Patch("Hay rack scattered straw",new Vector3(-.65f,0,-2.82f),new Vector2(1.10f,.58f),"Stable straw earth",.050f);
        style.Patch("Horse stall straw bedding",new Vector3(-1.55f,.35f,-.8f),new Vector2(1.45f,1.1f),"Stable straw earth",.016f);
        style.Patch("Spare stall straw bedding",new Vector3(1.7f,.35f,-.7f),new Vector2(1.3f,1.0f),"Stable straw earth",.016f);
        style.Patch("Grooming hoof ground",new Vector3(2.05f,0,-3.85f),new Vector2(1.15f,.65f),"Stable earth",.052f);
        style.Patch("Trough worn approach",new Vector3(4.15f,0,-1.65f),new Vector2(.95f,1.1f),"Stable earth",.044f);
        var shader=Resources.Load<Shader>("CourtyardFarmContact");
        if(shader!=null)
        {
            style.contact=new Material(shader){name="Stable soft ground contacts"};
            style.Patch("Trough ground contact",new Vector3(3.8f,0,-.8f),new Vector2(.64f,1.1f),null,.058f);
            foreach(float x in new[]{-1.65f,1.8f})
                style.Patch("Stall gate ground contact",new Vector3(x,0,-2.17f),new Vector2(1.65f,.36f),null,.058f);
            foreach(float x in new[]{-3.4f,.2f,3.4f})
                style.Patch("Stable post ground contact",new Vector3(x,0,-1.9f),new Vector2(.45f,.5f),null,.058f);
            foreach(string name in new[]{"Stable grooming caddy","Stable water bucket","Spare golden hay"})
            {
                var prop=GameObject.Find(name);if(prop==null)continue;
                style.Patch(name+" contact",stable.InverseTransformPoint(prop.transform.position),new Vector2(.4f,.3f),null,.04f);
            }
        }
    }
    void Patch(string name,Vector3 center,Vector2 radius,string resource,float lift)
    {
        var material=resource==null?contact:Resources.Load<Material>("CourtyardStyle/"+resource);if(material==null)return;
        const int segments=40,rings=5;
        var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int ring=0;ring<=rings;ring++)
        for(int side=0;side<segments;side++)
        {
            float a=side*Mathf.PI*2/segments,r=ring/(float)rings;
            float uneven=1+.05f*Mathf.Sin(a*5)+.035f*Mathf.Cos(a*7);
            var local=center+new Vector3(Mathf.Cos(a)*radius.x*r*uneven,0,Mathf.Sin(a)*radius.y*r*uneven);
            var world=transform.TransformPoint(local);
            foreach(var hit in Physics.RaycastAll(world+Vector3.up*12,Vector3.down,24,~0,QueryTriggerInteraction.Ignore))
                if(hit.collider.CompareTag("Ground"))world.y=Mathf.Max(world.y,hit.point.y);
            world.y+=lift;vertices.Add(transform.InverseTransformPoint(world));colors.Add(new Color(r,0,0,1));
            if(ring==0)continue;
            int previous=(ring-1)*segments+side,current=ring*segments+side;
            int pn=(ring-1)*segments+(side+1)%segments,cn=ring*segments+(side+1)%segments;
            triangles.AddRange(new[]{previous,cn,current,previous,pn,cn});
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();groundMeshes.Add(mesh);
        var patch=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(transform,false);
        patch.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=patch.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    void OnDestroy(){foreach(var mesh in groundMeshes)if(mesh!=null)Destroy(mesh);if(contact!=null)Destroy(contact);}
}
