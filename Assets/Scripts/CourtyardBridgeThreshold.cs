using UnityEngine;

// The box-shaped player catches even the old five-millimetre meadow lip on the return trip.
// A shallow bevel covers the seam with both ends buried below the adjacent ground.
public sealed class CourtyardBridgeThreshold : MonoBehaviour
{
    private Mesh mesh;
    internal static void Install()
    {
        var deck=GameObject.Find("East drawbridge deck");
        if(deck==null || GameObject.Find("Bevelled bridge threshold")!=null)return;
        var go=new GameObject("Bevelled bridge threshold",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
        go.tag="Ground";
        var threshold=go.AddComponent<CourtyardBridgeThreshold>();
        var vertices=new[]{new Vector3(42,-.025f,-2.8f),new Vector3(42,-.025f,.8f),
            new Vector3(43,.05f,-2.8f),new Vector3(43,.05f,.8f),
            new Vector3(44.3f,-.025f,-2.8f),new Vector3(44.3f,-.025f,.8f)};
        threshold.mesh=new Mesh{name="Gentle bridge threshold"};threshold.mesh.vertices=vertices;
        threshold.mesh.triangles=new[]{0,1,2,1,3,2,2,3,4,3,5,4};
        threshold.mesh.RecalculateNormals();threshold.mesh.RecalculateBounds();
        go.GetComponent<MeshFilter>().sharedMesh=threshold.mesh;go.GetComponent<MeshCollider>().sharedMesh=threshold.mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial=deck.GetComponent<MeshRenderer>().sharedMaterial;
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
