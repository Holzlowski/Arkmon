using UnityEngine;
using UnityEngine.Rendering;

// Preserve the painted shoulder while lifting the forearm/brush onto the horse's back.
public sealed class CourtyardGroomPose : MonoBehaviour
{
    Mesh mesh; Material material; MeshRenderer surface; Sprite last; SpriteRenderer original;
    public void Present(SpriteRenderer source,bool grooming)
    {
        original=source;
        if(surface==null)
        {
            var go=new GameObject("Hannes raised brushing arm",typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(transform,false);
            mesh=new Mesh{name="Stablekeeper articulated paper"};go.GetComponent<MeshFilter>().sharedMesh=mesh;
            surface=go.GetComponent<MeshRenderer>();material=new Material(Shader.Find("Sprites/Default"));
            surface.sharedMaterial=material;surface.sortingOrder=1;surface.shadowCastingMode=ShadowCastingMode.Off;
        }
        source.forceRenderingOff=grooming;surface.enabled=grooming;
        if(!grooming || source.sprite==last)return;
        last=source.sprite; const int cols=32,rows=32;
        var v=new Vector3[(cols+1)*(rows+1)];var uv=new Vector2[v.Length];var t=new int[cols*rows*6];
        var rect=last.rect;var pivot=last.pivot;float ppu=last.pixelsPerUnit;
        for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
        {
            int k=y*(cols+1)+x;float u=x/(float)cols,w=y/(float)rows;
            float arm=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.43f,.75f,u))
                *Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.44f,w))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,.79f,w)));
            v[k]=new Vector3((u*rect.width-pivot.x)/ppu,(w*rect.height-pivot.y)/ppu+arm*.40f,0);
            uv[k]=new Vector2((rect.x+u*rect.width)/last.texture.width,(rect.y+w*rect.height)/last.texture.height);
            if(x==cols || y==rows)continue;
            int i=(y*cols+x)*6;t[i]=k;t[i+1]=k+cols+1;t[i+2]=k+1;t[i+3]=k+1;t[i+4]=k+cols+1;t[i+5]=k+cols+2;
        }
        mesh.Clear();mesh.vertices=v;mesh.uv=uv;mesh.triangles=t;mesh.RecalculateBounds();
        material.mainTexture=last.texture;
    }
    void OnDisable(){if(original!=null)original.forceRenderingOff=false;if(surface!=null)surface.enabled=false;}
    void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
