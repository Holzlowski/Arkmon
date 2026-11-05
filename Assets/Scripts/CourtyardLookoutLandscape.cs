using UnityEngine;
using UnityEngine.Rendering;

// Reuse the tower-window paintings as distant, non-colliding western scenery.
public sealed class CourtyardLookoutLandscape : MonoBehaviour
{
    private Mesh quad;
    private Transform clouds;
    private Vector3 cloudOrigin;
    internal static void Install()
    {
        if (GameObject.Find("Western painted horizon") != null) return;
        var root = new GameObject("Western painted horizon");
        var landscape = root.AddComponent<CourtyardLookoutLandscape>();
        landscape.Build();
    }
    private void Build()
    {
        quad = new Mesh { name = "Western horizon plane" };
        quad.vertices = new[] { new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0) };
        quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        quad.triangles = new[] { 0,2,1,0,3,2 }; quad.RecalculateNormals(); quad.RecalculateBounds();
        Layer("Western painted mountains", "Lookout mountains", new Vector3(-195, 50, 30), new Vector2(420, 168), -20);
        clouds = Layer("Western drifting clouds", "Lookout clouds", new Vector3(-205, 65, 30), new Vector2(380, 253), -21);
        cloudOrigin = clouds.position;
    }
    private Transform Layer(string name, string materialName, Vector3 position, Vector2 size, int order)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
        // Place the painted backdrop beyond the extended hills while retaining its composition
        // from the final lookout camera (uniform scaling about that camera preserves the rays).
        var eye = new Vector3(-61, 11, 7);
        const float depthScale = 2.65f;
        position = eye + (position - eye) * depthScale; size *= depthScale;
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, -90, 0));
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        go.GetComponent<MeshFilter>().sharedMesh = quad;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = Resources.Load<Material>("CourtyardStyle/" + materialName);
        renderer.sortingOrder = order; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        return go.transform;
    }
    private void Update()
    {
        if (clouds != null) clouds.position = cloudOrigin + Vector3.forward * (Mathf.Sin(Time.timeSinceLevelLoad * .018f) * 6);
    }
    private void OnDestroy() { if (quad != null) Destroy(quad); }
}
