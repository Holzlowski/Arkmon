using UnityEngine;
using UnityEngine.Rendering;

// Reuse the tower-window paintings as distant, non-colliding western scenery.
public sealed class CourtyardLookoutLandscape : MonoBehaviour
{
    private Mesh quad;
    private Material mountainMaterial;
    private Transform clouds;
    private readonly Transform[] cloudPanels=new Transform[3];
    private Renderer[] nearClouds;
    private bool[] nearCloudEnabled;
    private bool hidingNearClouds;
    private Vector3 cloudOrigin;
    private float cloudTime;
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
        // Lift the full mountain range above the rolling fields, keeping its painted proportions.
        var mountains=Layer("Western painted mountains", "Lookout mountains", new Vector3(-195, 80, 30), new Vector2(420, 168), -20);
        var mountainRenderer=mountains.GetComponent<MeshRenderer>();
        mountainMaterial=new Material(mountainRenderer.sharedMaterial){name="Aurenfeld softened mountain foot"};
        mountainMaterial.SetFloat("_Haze",.28f);
        mountainMaterial.SetColor("_HazeColor",new Color(.65f,.67f,.53f));
        mountainRenderer.sharedMaterial=mountainMaterial;
        clouds = Layer("Western drifting clouds", "Lookout clouds", new Vector3(-205, 65, 30), new Vector2(380, 253), -21);
        cloudOrigin = clouds.position;
        cloudPanels[1]=clouds;
        cloudPanels[0]=Layer("Western cloud continuation left", "Lookout clouds", new Vector3(-205,65,30),new Vector2(380,253),-21);
        cloudPanels[2]=Layer("Western cloud continuation right", "Lookout clouds", new Vector3(-205,65,30),new Vector2(380,253),-21);
        var renderers=new System.Collections.Generic.List<Renderer>();
        foreach(var drift in FindObjectsByType<CloudDrift>(FindObjectsSortMode.None))
            if(drift.gameObject.scene==gameObject.scene)renderers.AddRange(drift.GetComponentsInChildren<Renderer>());
        nearClouds=renderers.ToArray();nearCloudEnabled=new bool[nearClouds.Length];
        PositionClouds();
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
        cloudTime += Time.deltaTime;
        PositionClouds();
        bool hide=CourtyardLookout.IsPresenting;
        if(hide!=hidingNearClouds)
        {
            if(hide)
            {
                for(int i=0;i<nearClouds.Length;i++)
                    if(nearClouds[i]!=null){nearCloudEnabled[i]=nearClouds[i].enabled;nearClouds[i].enabled=false;}
            }
            else RestoreNearClouds();
            hidingNearClouds=hide;
        }
    }
    private void PositionClouds()
    {
        if(clouds==null)return;
        float width=clouds.localScale.x;
        // Three adjacent paintings wrap outside the view. Constant drift has no stationary turning point.
        float offset=Mathf.Repeat(cloudTime*2.8125f+width*.5f,width)-width*.5f;
        for(int i=0;i<cloudPanels.Length;i++)
            cloudPanels[i].position=cloudOrigin+Vector3.forward*(offset+(i-1)*width);
    }
    private void RestoreNearClouds()
    {
        if(!hidingNearClouds || nearClouds==null)return;
        for(int i=0;i<nearClouds.Length;i++)if(nearClouds[i]!=null)nearClouds[i].enabled=nearCloudEnabled[i];
        hidingNearClouds=false;
    }
    private void OnDisable()=>RestoreNearClouds();
    private void OnDestroy() { RestoreNearClouds();if (quad != null) Destroy(quad);if(mountainMaterial!=null)Destroy(mountainMaterial); }
}
