using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Soft scenic light shares the moving field-shadow mask, rather than a separate animation.
public sealed class CourtyardCloudSunrays : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<MeshRenderer> renderers=new List<MeshRenderer>();
    Material material;
    float visibility;
    internal static void Install()
    {
        if(GameObject.Find("Aurenfeld cloud-gap sunlight")!=null)return;
        var meadow=GameObject.Find("Castle horizon and golden fields")?.GetComponentInChildren<MeadowVegetation>();
        var shader=Resources.Load<Shader>("AurenfeldCloudSunrays");
        if(meadow==null || shader==null)return;
        var effect=new GameObject("Aurenfeld cloud-gap sunlight").AddComponent<CourtyardCloudSunrays>();
        effect.material=new Material(shader){name="Soft golden cloud-gap light"};
        effect.Build(meadow,-155,90,.95f);
        effect.Build(meadow,-245,118,.72f);
    }
    void Build(MeadowVegetation meadow,float x,float height,float strength)
    {
        const int columns=81;
        var v=new Vector3[columns*2];var uv=new Vector2[v.Length];
        var ground=new Vector3[v.Length];var colors=new Color[v.Length];var indices=new int[(columns-1)*6];
        var lightDirection=RenderSettings.sun!=null?-RenderSettings.sun.transform.forward:new Vector3(-.86f,.31f,-.40f);
        lightDirection/=Mathf.Max(.1f,lightDirection.y);
        for(int i=0;i<columns;i++)
        {
            float t=i/(float)(columns-1);
            var foot=meadow.SurfacePoint(new Vector3(x,0,Mathf.Lerp(-120,190,t)));
            for(int row=0;row<2;row++)
            {
                int n=row*columns+i;
                v[n]=foot+lightDirection*(row==0?1.5f:height);
                uv[n]=new Vector2(t,row);ground[n]=foot;colors[n]=new Color(1,1,1,strength);
            }
            if(i==0)continue;
            int k=(i-1)*6;indices[k]=i-1;indices[k+1]=i;indices[k+2]=i+columns;
            indices[k+3]=i-1;indices[k+4]=i+columns;indices[k+5]=i+columns-1;
        }
        var mesh=new Mesh{name="Diffuse sunlight over fields"};
        mesh.vertices=v;mesh.uv=uv;mesh.SetUVs(1,new List<Vector3>(ground));mesh.colors=colors;mesh.triangles=indices;
        mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sortingOrder=-10;
        renderer.enabled=false;renderers.Add(renderer);
    }
    void LateUpdate()
    {
        var view=Camera.main;
        float outside=view!=null?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-55,-45,view.transform.position.x)):0;
        float target=CourtyardLookout.IsPresenting && !SceneLoadManager.IsTransitioning?outside:0;
        visibility=Mathf.MoveTowards(visibility,target,Time.unscaledDeltaTime*.65f);
        material.SetFloat("_Visibility",visibility);
        foreach(var renderer in renderers)renderer.enabled=visibility>.001f;
    }
    void OnDisable()
    {
        visibility=0;
        if(material!=null)material.SetFloat("_Visibility",0);
        foreach(var renderer in renderers)if(renderer!=null)renderer.enabled=false;
    }
    void OnDestroy()
    {
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        if(material!=null)Destroy(material);
    }
}
