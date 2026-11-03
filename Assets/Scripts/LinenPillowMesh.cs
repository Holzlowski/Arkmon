using UnityEngine;

internal static class LinenPillowMesh
{
    private static float SoftSquare(float value) =>
        Mathf.Sign(value) * Mathf.Pow(Mathf.Abs(value), .48f);

    public static Mesh Create()
    {
        const int rings = 24, sides = 48;
        var vertices = new Vector3[(rings + 1) * (sides + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[rings * sides * 6];
        int k = 0;
        for (int r = 0; r <= rings; r++)
        {
            float lat = Mathf.Lerp(-Mathf.PI / 2, Mathf.PI / 2, r / (float)rings);
            float radius = Mathf.Cos(lat);
            for (int c = 0; c <= sides; c++)
            {
                float angle = c * Mathf.PI * 2 / sides;
                int i = r * (sides + 1) + c;
                float x = SoftSquare(Mathf.Cos(angle)), z = SoftSquare(Mathf.Sin(angle));
                float loft = 1 - .38f * Mathf.Abs(x * z) * radius;
                vertices[i] = new Vector3(x * radius, Mathf.Sin(lat) * loft, z * radius) * .5f;
                uv[i] = new Vector2(c / (float)sides, r / (float)rings);
                if (r == rings || c == sides) continue;
                int next = i + sides + 1;
                triangles[k++] = i; triangles[k++] = next; triangles[k++] = i + 1;
                triangles[k++] = i + 1; triangles[k++] = next; triangles[k++] = next + 1;
            }
        }
        var mesh = new Mesh { name = "Linen pillow cushion", vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
