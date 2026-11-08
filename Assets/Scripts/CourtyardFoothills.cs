using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A scenic middle distance between the cultivated fields and the painted mountain range.
// No colliders: this remains beyond the playable courtyard.
public sealed class CourtyardFoothills : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Transform> groves=new List<Transform>();
    Material hills,foliage;
    MeadowVegetation meadow;
    internal static void Install()
    {
        if(GameObject.Find("Aurenfeld mountain foothills")!=null)return;
        var meadow=GameObject.Find("Castle horizon and golden fields")?.GetComponentInChildren<MeadowVegetation>();
        var shader=Resources.Load<Shader>("AurenfeldFoothills");
        var paper=Resources.Load<Shader>("AurenfeldScenicPaper");
        if(meadow==null || shader==null || paper==null)return;
        var style=new GameObject("Aurenfeld mountain foothills").AddComponent<CourtyardFoothills>();
        style.meadow=meadow;
        style.hills=new Material(shader){name="Soft olive-gold foothill paint"};
        style.hills.SetTexture("_HarvestCanopy",Resources.Load<Texture2D>("CourtyardStyle/PaintedWheatCanopy"));
        style.foliage=new Material(paper){name="Foothill grove illustrations"};
        style.BuildTerrain();style.BuildGroves();
    }
    static float Rise(int band,float x,float z)
    {
        float front=band==0?-307:-350,back=band==0?-389:-413;
        float t=Mathf.InverseLerp(front,back,x);
        if(x>front || x<back)return 0;
        // Vary the ridge axis and its height; the two bands never share the same profile.
        float curve=Mathf.Sin(z*(band==0?.012f:.009f)+band*1.9f)*.10f;
        float profile=Mathf.Max(0,Mathf.Sin(Mathf.Clamp01(t+curve*Mathf.Sin(t*Mathf.PI))*Mathf.PI));
        float amplitude=band==0?4+7*Mathf.Pow(Mathf.Sin(z*.010f+.8f),2):9+9*Mathf.Pow(Mathf.Sin(z*.008f-1),2);
        // Break the long left ridge into lower, staggered shoulders matching the painted foothills.
        float left=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-65,40,z));
        float shoulders=.52f+.22f*Mathf.Sin(z*.043f+1.1f)+.26f*Mathf.PerlinNoise(z*.035f+30,band*9+4);
        amplitude*=Mathf.Lerp(1,shoulders,left*(band==1?1:.4f));
        float roadDistance=Mathf.Abs(z-CourtyardFieldMargins.VergeZ(x));
        float pass=Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,65,roadDistance));
        float edge=Mathf.SmoothStep(0,1,Mathf.Min(z+300,340-z)/55);
        return profile*profile*amplitude*pass*edge;
    }
    public Vector3 SurfacePoint(Vector3 p)
    {
        var ground=meadow.SurfacePoint(p);
        ground.y+=Mathf.Max(Rise(0,p.x,p.z),Rise(1,p.x,p.z));
        return ground;
    }
    void BuildTerrain()
    {
        const int rows=122,columns=161;
        var vertices=new Vector3[rows*columns];var colors=new Color[vertices.Length];
        var triangles=new int[(rows-1)*(columns-1)*6];int k=0;
        for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
        {
            // One connected surface reaches beneath the mountain painting. The old separate
            // hill strips left their valleys and outer edges without underlying ground.
            float x=Mathf.Lerp(-178,-420,row/(float)(rows-1));
            float z=Mathf.Lerp(-300,340,col/(float)(columns-1));int n=row*columns+col;
            var p=meadow.SurfacePoint(new Vector3(x,0,z));
            float near=Rise(0,x,z),far=Rise(1,x,z),rise=Mathf.Max(near,far);
            int band=far>near?1:0;
            p.y+=rise-.16f;vertices[n]=p;
            Color low=band==0?new Color(.70f,.59f,.31f):new Color(.51f,.56f,.42f);
            Color high=band==0?new Color(.58f,.57f,.31f):new Color(.63f,.65f,.48f);
            float variation=Mathf.PerlinNoise(z*.016f+band*14,x*.009f)*.7f+Mathf.Clamp01(rise/30)*.3f;
            var paint=Color.Lerp(low,high,variation).linear;
            float left=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-65,40,z));
            float backdrop=Mathf.Lerp(.55f,1,left)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-345,-410,x));
            paint=Color.Lerp(paint,new Color(.45f,.60f,.58f).linear,backdrop*.82f);
            // Fade colour into the existing harvest ground at the buried toes.
            colors[n]=Color.Lerp(CourtyardFieldMargins.GoldenGround(p),paint,Mathf.SmoothStep(0,1,rise/4));
            if(row==0 || col==0)continue;
            int a=n-columns-1;
            triangles[k++]=a;triangles[k++]=n-1;triangles[k++]=a+1;
            triangles[k++]=a+1;triangles[k++]=n-1;triangles[k++]=n;
        }
        var mesh=new Mesh{name="Continuous field-to-mountain ground"};
        mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=hills;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    void BuildGroves()
    {
        var oak=Resources.Load<Sprite>("CourtyardStyle/Trees/Oak");
        var birch=Resources.Load<Sprite>("CourtyardStyle/Trees/Birch");
        var hedge=Resources.Load<Sprite>("CourtyardStyle/Mixed");
        var random=new System.Random(8317);
        var centres=new[]{new Vector2(-315,-113),new Vector2(-330,-57),new Vector2(-342,5),
            new Vector2(-321,75),new Vector2(-346,128),new Vector2(-333,191)};
        for(int group=0;group<centres.Length;group++)
        {
            var c=centres[group];int count=3+group%3;
            for(int i=0;i<count;i++)
            {
                var p=new Vector3(c.x+(float)random.NextDouble()*7-3.5f,0,c.y+(i-(count-1)*.5f)*4+(float)random.NextDouble()*2);
                Plant(hedge,p,2.2f+(float)random.NextDouble()*2.2f,random,"Foothill shrub group");
            }
            int trees=group%3==1?1:2;
            for(int i=0;i<trees;i++)
                Plant((i+group)%3==0?birch:oak,new Vector3(c.x-2-i*2,0,c.y+i*6),6+(float)random.NextDouble()*3,random,"Foothill tree group");
        }
        var shoulders=new[]{new Vector2(-374,-160),new Vector2(-382,-111),new Vector2(-378,-48),
            new Vector2(-392,10),new Vector2(-383,116),new Vector2(-389,187)};
        for(int group=0;group<shoulders.Length;group++)
        {
            int count=3+group%2;
            for(int i=0;i<count;i++)
            {
                var center=shoulders[group];
                var p=new Vector3(center.x+(float)random.NextDouble()*6,0,center.y+i*4.2f+(float)random.NextDouble()*2);
                Plant(i==1?birch:oak,p,3.2f+(float)random.NextDouble()*2.2f,random,"Foothill transition woodland");
            }
        }
    }

    void Plant(Sprite sprite,Vector3 p,float height,System.Random random,string name)
    {
        if(sprite==null || Mathf.Abs(p.z-CourtyardFieldMargins.VergeZ(p.x))<14 || CourtyardFarmApproach.IsBareGround(p))return;
        var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
        float scale=height/sprite.bounds.size.y;
        go.transform.position=SurfacePoint(p)+Vector3.up*(.025f-sprite.bounds.min.y*scale);
        go.transform.localScale=new Vector3(scale*(.85f+(float)random.NextDouble()*.3f),scale,scale);
        var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=foliage;
        renderer.color=Color.Lerp(new Color(.65f,.73f,.50f),new Color(.87f,.87f,.65f),(float)random.NextDouble());
        if(name=="Foothill transition woodland")renderer.color=Color.Lerp(new Color(.53f,.66f,.58f),new Color(.67f,.74f,.61f),(float)random.NextDouble());
        renderer.flipX=random.Next(2)==0;groves.Add(go.transform);
    }
    void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var grove in groves)grove.rotation=camera.transform.rotation;
    }
    void OnDestroy()
    {
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        if(hills!=null)Destroy(hills);if(foliage!=null)Destroy(foliage);
    }
}
