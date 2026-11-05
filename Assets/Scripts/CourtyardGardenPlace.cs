using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Local, reversible scene dressing. Keep the authored fountain available for a later courtyard square.
public sealed class CourtyardGardenPlace : MonoBehaviour
{
    Mesh groundMesh;
    internal static void Install()
    {
        var house=GameObject.Find("Garden cottage")?.transform;
        if(house==null || house.Find("Marta garden place")!=null)return;
        var oak=Resources.Load<Material>("CourtyardStyle/Oak");if(oak==null)return;
        var root=new GameObject("Marta garden place");root.transform.SetParent(house,false);
        var place=root.AddComponent<CourtyardGardenPlace>();
        GameObject.Find("Fountain basin")?.SetActive(false);
        var original=GameObject.Find("Bench seat");
        if(original!=null)
        {
            var seat=Instantiate(original,root.transform);seat.name="Marta side bench";
            original.SetActive(false);
            seat.transform.localPosition=new Vector3(-3.65f,0,-4.15f);
            seat.transform.localRotation=Quaternion.Euler(0,-65,0);
            seat.transform.localScale=new Vector3(.60f,.90f,.85f);
            var ground=Ground(seat.transform.position);seat.transform.position=ground;
            foreach(var renderer in seat.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial=oak;
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                var scale=renderer.transform.lossyScale;
                block.SetVector("_ObjectScale",new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));
                block.SetFloat("_GrainAxis",scale.x>scale.y && scale.x>scale.z?0:scale.z>scale.y?2:1);
                block.SetFloat("_EdgeWidth",.018f);renderer.SetPropertyBlock(block);
            }
        }
        var steps=house.GetComponentsInChildren<Transform>().Where(t=>t.name=="Worn approach flagstone").OrderByDescending(t=>t.localPosition.z).ToArray();
        for(int i=0;i<steps.Length;i++)
        {
            var step=steps[i];var at=step.localPosition;at.x=.12f*Mathf.Sin(i*1.8f);step.localPosition=at;
            step.localRotation=Quaternion.Euler(0,i%2==0?-5:7,0);
            var scale=step.localScale;scale.x*=.78f;scale.z*=.9f;step.localScale=scale;
        }
        Physics.SyncTransforms();
        place.WorkingGround(house.TransformPoint(new Vector3(2.6f,0,-4.15f)));
    }
    static Vector3 Ground(Vector3 at)
    {
        foreach(var hit in Physics.RaycastAll(at+Vector3.up*15,Vector3.down,30,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground"))at.y=Mathf.Max(at.y,hit.point.y);
        return at;
    }
    void WorkingGround(Vector3 center)
    {
        var material=Resources.Load<Material>("CourtyardStyle/Stable earth");if(material==null)return;
        const int sides=40,rings=5;
        var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int ring=0;ring<=rings;ring++)
        for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)rings;
            float edge=1+.06f*Mathf.Sin(a*5)+.03f*Mathf.Cos(a*7);
            var point=Ground(center+new Vector3(Mathf.Cos(a)*1.25f*r*edge,0,Mathf.Sin(a)*.95f*r*edge))+Vector3.up*.045f;
            vertices.Add(transform.InverseTransformPoint(point));colors.Add(new Color(r,0,0,1));
            if(ring==0)continue;
            int prev=(ring-1)*sides+side,cur=ring*sides+side,next=(side+1)%sides;
            triangles.AddRange(new[]{prev,ring*sides+next,cur,prev,(ring-1)*sides+next,ring*sides+next});
        }
        groundMesh=new Mesh{name="Marta worn work ground"};groundMesh.SetVertices(vertices);groundMesh.SetColors(colors);
        groundMesh.SetTriangles(triangles,0);groundMesh.RecalculateNormals();groundMesh.RecalculateBounds();
        var patch=new GameObject("Marta worn work ground",typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(transform,false);
        patch.GetComponent<MeshFilter>().sharedMesh=groundMesh;
        var renderer=patch.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    void OnDestroy(){if(groundMesh!=null)Destroy(groundMesh);}
}
