using UnityEngine;

// Shared placement for the cottage, kitchen garden, paths and their exclusion zones.
public static class CourtyardResidentLayout
{
    public static readonly Vector3 GardenOffset = new Vector3(23, 0, -17.6f);
    public static Vector3 GardenPoint(Vector3 original) => original + GardenOffset;
    public static Vector3 GardenOriginal(Vector3 point) => point - GardenOffset;
    public static Vector3 GardenApproach(float t)
    {
        var points=new[]{new Vector3(35,0,-1.8f),new Vector3(35.4f,0,-12.05f),new Vector3(27.3f,0,-12.05f),new Vector3(24,0,-13.5f)};
        float f=Mathf.Clamp01(t)*3;int i=Mathf.Min(2,Mathf.FloorToInt(f));float u=f-i;
        var a=points[Mathf.Max(0,i-1)];var b=points[i];var c=points[i+1];var d=points[Mathf.Min(3,i+2)];
        return .5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
    }
    public static bool CastleSquare(Vector3 p) => p.x > -4 && p.x < 17 && p.z > 0 && p.z < 12;
    public static bool GardenPlot(Vector3 p) => p.x > 19 && p.x < 33.5f && p.z > -17.4f && p.z < -4.6f;
    public static bool GardenWalkway(Vector3 p)
    {
        var old = GardenOriginal(p);
        return old.x > 3.5f && old.x < 5.0f && old.z > -.2f && old.z < 10.2f
            || p.x > 20 && p.x < 35.5f && p.z > -17.3f && p.z < -14.7f;
    }
}
