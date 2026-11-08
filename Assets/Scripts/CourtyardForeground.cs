using UnityEngine;

// Shared by the backdrop mesh, crop roots and colour masks. Keep the low-camera
// foreground below the courtyard; its former arena hills must not hide the world.
public static class CourtyardForeground
{
    public static bool IsInnerVerge(Vector3 p) => p.x > -43 && p.x < 43 && p.z > -17.5f && p.z < -7;

    static float Across(Vector3 p) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-100, -58, p.x))
        * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(140, 180, p.x)));

    public static float FieldWeight(Vector3 p) => Across(p)
        * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-16, -19, p.z));

    public static Vector3 Surface(Vector3 p)
    {
        float weight = Across(p) * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-7, -11, p.z))
            * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-65, -100, p.z)));
        float distantRise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-42, -65, p.z));
        float height = -.05f + distantRise * (.15f + .25f * Mathf.PerlinNoise(p.x * .035f + 73, p.z * .028f + 41));
        p.y = Mathf.Lerp(p.y, height, weight);
        return p;
    }

    // A ragged, low stubble margin protects the foreground view and keeps its
    // real sun shadows short. Full-height grain begins several metres out.
    public static float CropGrowth(Vector3 p)
    {
        if(p.z >= -17) return 1;
        float edge = 19 + .7f * Mathf.Sin(p.x * .17f) + .3f * Mathf.Sin(p.x * .73f);
        float growth = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(edge, edge + 9, -p.z));
        return Mathf.Lerp(1, growth, Across(p));
    }
}
