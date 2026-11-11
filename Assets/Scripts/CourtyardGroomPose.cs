using UnityEngine;
using UnityEngine.Rendering;

// Preserve the painted shoulder while lifting the forearm/brush onto the horse's back.
public sealed class CourtyardGroomPose : MonoBehaviour
{
    Mesh mesh; Material material; MeshRenderer surface; Sprite last; SpriteRenderer original;
    Vector3[] rest,vertices;float[] weights;
    public void Present(SpriteRenderer source,bool grooming,float reach = 0)
    {
        original=source;
        if(surface==null)
        {
            var go=new GameObject("Hannes raised brushing arm",typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(transform,false);
            mesh=new Mesh{name="Stablekeeper articulated paper"};go.GetComponent<MeshFilter>().sharedMesh=mesh;
            surface=go.GetComponent<MeshRenderer>();material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
            material.SetFloat("_Cull",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            surface.sharedMaterial=material;surface.sortingOrder=1;surface.shadowCastingMode=ShadowCastingMode.Off;
        }
        source.forceRenderingOff=grooming;surface.enabled=grooming;
        if(!grooming)return;
        surface.transform.localScale=new Vector3(source.flipX?-1:1,1,1);
        if(last!=source.sprite)
        {
            last=source.sprite;const int cols=32,rows=32;
            rest=new Vector3[(cols+1)*(rows+1)];vertices=new Vector3[rest.Length];weights=new float[rest.Length];
            var uv=new Vector2[rest.Length];var triangles=new int[cols*rows*6];
            var rect=last.rect;var pivot=last.pivot;float ppu=last.pixelsPerUnit;
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
            {
                int k=y*(cols+1)+x;float u=x/(float)cols,w=y/(float)rows;
                weights[k]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,.78f,u))
                    *Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.44f,w))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,.79f,w)));
                rest[k]=new Vector3((u*rect.width-pivot.x)/ppu,(w*rect.height-pivot.y)/ppu,0);
                uv[k]=new Vector2((rect.x+u*rect.width)/last.texture.width,(rect.y+w*rect.height)/last.texture.height);
                if(x==cols || y==rows)continue;
                int i=(y*cols+x)*6;triangles[i]=k;triangles[i+1]=k+cols+1;triangles[i+2]=k+1;
                triangles[i+3]=k+1;triangles[i+4]=k+cols+1;triangles[i+5]=k+cols+2;
            }
            mesh.Clear();mesh.vertices=rest;mesh.uv=uv;mesh.triangles=triangles;
        }
        for(int i=0;i<vertices.Length;i++)vertices[i]=rest[i]+weights[i]*new Vector3(-.13f*(1-reach),.16f,0);
        mesh.vertices=vertices;mesh.RecalculateBounds();
        material.SetTexture("_BaseMap",last.texture);material.SetColor("_BaseColor",source.color);
    }
    void OnDisable(){if(original!=null)original.forceRenderingOff=false;if(surface!=null)surface.enabled=false;}
    void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
