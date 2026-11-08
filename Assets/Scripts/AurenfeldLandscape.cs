using UnityEngine;

// One world-space composition for the western terrain, crops, roads and scenery.
// The playable courtyard and eastern gate retain their existing elevations.
public static class AurenfeldLandscape
{
    public static float Weight(Vector3 p) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-64, -88, p.x));

    static float Hill(Vector3 p, float x, float z, float width, float depth, float height)
        => height * Mathf.Exp(-Mathf.Pow((p.x-x)/width, 2)-Mathf.Pow((p.z-z)/depth, 2));

    public static Vector3 Surface(Vector3 p)
    {
        float distance = Mathf.Max(0, -p.x-80);
        float height = -.8f + distance*.045f;
        height += Hill(p, -150, -55, 42, 85, 6);
        height += Hill(p, -242, 68, 58, 125, 14);
        height += Hill(p, -355, -40, 70, 165, 22);
        height -= Hill(p, -116, 22, 32, 54, 2.4f);
        height += Mathf.Sin(p.z*.024f+p.x*.011f)*.65f;
        // Keep most of the earlier rolling meadow that the playtest preferred.
        float far=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-184,-355,p.x));
        float relief=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-184,-220,p.x));
        float previous=p.y+far*(10+Mathf.Sin(p.z*.019f+p.x*.013f)*2.2f);
        previous+=relief*(Hill(p,-239-Mathf.Sin(p.z*.018f)*14,32,34,118,6)
            -Hill(p,-280,55,30,95,2.2f)+Hill(p,-343,94,43,135,5));
        p.y = Mathf.Lerp(p.y, Mathf.Lerp(previous,height,.25f), Weight(p));
        return p;
    }

    // Bent, irregular parcels share their colour across soil and standing grain.
    public static Color Harvest(Vector3 p)
    {
        var q = new Vector2((-p.x-70)/48, p.z/57);
        q += new Vector2(Mathf.Sin(p.z*.024f)*.25f, Mathf.Sin(p.x*.020f)*.27f);
        var cell = new Vector2(Mathf.Floor(q.x), Mathf.Floor(q.y));
        float first=100, second=100, tone=0, other=0;
        for(int x=-1;x<=1;x++) for(int z=-1;z<=1;z++)
        {
            var id=cell+new Vector2(x,z);
            float seed=Mathf.Repeat(Mathf.Sin(id.x*127.1f+id.y*311.7f)*43758.5453f,1);
            float seed2=Mathf.Repeat(Mathf.Sin(id.x*269.5f+id.y*183.3f)*17358.225f,1);
            float d=(q-id-new Vector2(.2f+seed*.6f,.2f+seed2*.6f)).sqrMagnitude;
            if(d<first){second=first;other=tone;first=d;tone=seed;}
            else if(d<second){second=d;other=seed;}
        }
        float blend=.5f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.065f,second-first)));
        float variety=Mathf.Lerp(tone,other,blend);
        Color ochre=new Color(.72f,.50f,.22f), honey=new Color(.89f,.69f,.31f), straw=new Color(.96f,.83f,.51f);
        return (variety<.5f ? Color.Lerp(ochre,honey,variety*2) : Color.Lerp(honey,straw,(variety-.5f)*2)).linear;
    }
}
