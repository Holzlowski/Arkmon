using UnityEngine;

// A small rose-shaped spray, not one solid hose. Owned by the pour action.
public sealed class GardenWaterEffect : MonoBehaviour
{
    readonly LineRenderer[] strands = new LineRenderer[3];
    readonly Transform[] drops = new Transform[12];
    Material material;
    bool singleStream;
    public void SetSingleStream()
    {
        singleStream=true;
        strands[0].startWidth=.026f;strands[0].endWidth=.016f;
    }

    void Awake()
    {
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", new Color(.61f,.84f,.93f));
        for (int i = 0; i < strands.Length; i++)
        {
            var obj = new GameObject("Fine garden water stream", typeof(LineRenderer));
            obj.transform.SetParent(transform,false);
            var line = obj.GetComponent<LineRenderer>(); strands[i] = line;
            line.sharedMaterial = material; line.positionCount = 14; line.useWorldSpace = true;
            line.startWidth = .010f; line.endWidth = .004f;
            line.numCapVertices = 2; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;
        }
        for (int i = 0; i < drops.Length; i++)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = i < 8 ? "Falling garden drop" : "Leaf splash";
            obj.transform.SetParent(transform,false);
            var collider = obj.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.transform.localScale = Vector3.one * .022f; obj.SetActive(false);
            drops[i] = obj.transform;
        }
    }

    static Vector3 Arc(Vector3 source, Vector3 end, float u)
    {
        // Falling parabola retains a gentle initial slope from the watering rose.
        Vector3 point = Vector3.Lerp(source,end,u);
        point.y = Mathf.Lerp(source.y,end.y,u*u);
        return point;
    }

    public void Present(Vector3 source, Vector3 target, float time, float strength)
    {
        bool pouring = strength > .5f;
        for (int i = 0; i < strands.Length; i++)
        {
            var line = strands[i]; line.enabled = pouring && (!singleStream || i==0);
            var spread = singleStream ? Vector3.zero : new Vector3((i-1)*.032f,0,((i*2)%3-1)*.025f);
            for (int j = 0; j < line.positionCount; j++)
                line.SetPosition(j,Arc(source + spread*.06f,target+spread,j/(float)(line.positionCount-1)));
        }
        for (int i = 0; i < drops.Length; i++)
        {
            drops[i].gameObject.SetActive(pouring);
            float phase = Mathf.Repeat(time*2.8f+i*.137f,1);
            var spread = new Vector3((i%5-2)*.07f,0,(i%3-1)*.075f);
            if (i < 8)
            {
                drops[i].position = Arc(source,target+spread,phase);
                drops[i].localScale = new Vector3(.012f,.024f,.012f);
            }
            else
            {
                float angle = i*2.4f;
                var outward = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.16f;
                drops[i].position = target + spread*.5f + outward*phase + Vector3.up*(.14f*Mathf.Sin(phase*Mathf.PI));
                drops[i].localScale = Vector3.one * Mathf.Lerp(.017f,.004f,phase);
            }
        }
    }

    void OnDestroy() { if (material != null) Destroy(material); }
}
