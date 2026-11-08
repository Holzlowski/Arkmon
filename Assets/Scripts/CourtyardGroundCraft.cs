using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Small terrain-following details at building edges; walking and task areas remain clear.
public sealed class CourtyardGroundCraft : MonoBehaviour
{
    readonly List<Mesh> owned=new List<Mesh>();
    Material stone,wood,soil,moss,wear,paper;
    readonly Batch soilMarks=new Batch(),mossMarks=new Batch(),wearMarks=new Batch(),gravel=new Batch(),litter=new Batch();
    readonly List<Transform> plants=new List<Transform>();
    internal static void Install()
    {
        if(GameObject.Find("Courtyard grounded foundations")!=null)return;
        var style=new GameObject("Courtyard grounded foundations").AddComponent<CourtyardGroundCraft>();
        style.stone=new Material(Resources.Load<Material>("CourtyardStyle/Stone")){name="Buried warm foundation stones"};
        style.stone.SetFloat("_SingleStone",1);style.stone.SetFloat("_StoneMeshUV",1);style.stone.SetFloat("_EdgeWidth",0);
        style.wood=new Material(Resources.Load<Material>("CourtyardStyle/Oak")){name="Weathered root wood"};
        var earth=Resources.Load<Material>("CourtyardStyle/Stable earth");
        style.soil=new Material(earth){name="Settled sandy footing"};
        style.soil.SetColor("_BaseColor",new Color(.76f,.71f,.59f,.40f));style.soil.SetFloat("_SandBlend",.50f);
        style.moss=new Material(earth){name="Sheltered footing moss"};
        style.moss.SetColor("_BaseColor",new Color(.35f,.48f,.23f,.38f));
        style.wear=new Material(earth){name="Local work scuffs"};
        style.wear.SetColor("_BaseColor",new Color(.50f,.47f,.38f,.32f));
        foreach(var paint in new[]{style.soil,style.moss,style.wear})
        {paint.SetFloat("_EdgeStart",.15f);paint.SetFloat("_EdgeBreakup",.12f);paint.renderQueue=2972;}
        style.paper=new Material(Resources.Load<Shader>("AurenfeldScenicPaper")){name="Sheltered courtyard grass"};
        style.Home("Garden cottage",2.65f,2.5f);
        style.Home("East cottage",2.77f,2.5f);
        style.Home("Ida workshop",3.35f,2.15f);
        style.Home("Stable",3.85f,2.1f);
        foreach(var tree in FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID))
            if(tree.name=="Courtyard oak" || tree.name=="Garden apple tree" || tree.name=="Stream willow")style.Roots(tree.position);
        style.WorkAreas();
        style.Finish("Sandy foundation transitions",style.soilMarks,style.soil);
        style.Finish("Sheltered moss transitions",style.mossMarks,style.moss);
        style.Finish("Resident work scuffs",style.wearMarks,style.wear);
        style.Finish("Settled verge gravel",style.gravel,style.stone);
        style.Finish("Resident work remnants",style.litter,style.wood);
    }
    static Vector3 Ground(Vector3 p)
    {
        var hits=Physics.RaycastAll(new Vector3(p.x,25,p.z),Vector3.down,50,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider.CompareTag("Ground") && h.normal.y>.5f).OrderByDescending(h=>h.point.y).ToArray();
        if(hits.Length>0)p.y=hits[0].point.y;
        return p;
    }
    sealed class Batch
    {
        public readonly List<Vector3> v=new List<Vector3>();
        public readonly List<int> t=new List<int>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<Color> colors=new List<Color>();
        public void Face(Vector3[] points,float tone)
        {
            int start=v.Count;
            foreach(var p in points){v.Add(p);colors.Add(new Color(tone,tone,tone,1));}
            for(int i=0;i<points.Length;i++)uv.Add(new Vector2(.64f+(i%2)*.13f,.41f+(i/2%2)*.055f));
            for(int i=1;i<points.Length-1;i++)t.AddRange(new[]{start,start+i,start+i+1});
        }
    }
    void Home(string name,float width,float depth)
    {
        var home=GameObject.Find(name)?.transform;if(home==null)return;
        var batch=new Batch();var random=new System.Random(name.Length*431);
        // Only flank the doorway and hug side walls. No loose stones across the approach.
        for(int side=-1;side<=1;side+=2)
        for(int i=0;i<20;i++)
        {
            float along=(float)random.NextDouble(),outward=.06f+(float)random.NextDouble()*.40f;
            var local=i<8?new Vector3(side*(width-.75f+along*.75f),0,-depth-outward):
                new Vector3(side*(width+outward),0,Mathf.Lerp(-depth,depth,along));
            var at=Ground(home.TransformPoint(local));
            float size=i%5==0?.24f:.07f+(float)random.NextDouble()*.08f;
            Pebble(batch,at+Vector3.down*.02f,new Vector3(size,.04f+size*.36f,size*.73f),random);
        }
        // Narrow deposits follow the actual footing. Moss belongs to sheltered sides,
        // not every doorway; gaps keep these edges from looking like a continuous outline.
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<7;i++)
            {
                float z=Mathf.Lerp(-depth,depth,i/6f);
                var at=home.TransformPoint(new Vector3(side*(width+.16f),0,z));
                Patch(soilMarks,at,.36f,.35f,home.eulerAngles.y+i*17,.074f);
                if(i>1 && i%2==0)Patch(mossMarks,home.TransformPoint(new Vector3(side*(width+.05f),0,z)),.20f,.29f,home.eulerAngles.y,.077f);
                if(i%2==0)
                    for(int chip=0;chip<3;chip++)
                    {
                        var offset=new Vector3((float)random.NextDouble()*.28f-.14f,0,(float)random.NextDouble()*.45f-.225f);
                        Pebble(gravel,Ground(at+offset)+Vector3.down*.008f,new Vector3(.035f,.024f,.055f),random);
                    }
            }
            for(int i=0;i<3;i++)
            {
                var at=home.TransformPoint(new Vector3(side*(width-.45f+i*.18f),0,-depth-.25f));
                Patch(soilMarks,at,.30f,.26f,home.eulerAngles.y,.074f);
            }
            Plant(home.TransformPoint(new Vector3(side*(width+.34f),0,depth*.55f)),.28f+random.Next(3)*.045f);
            Plant(home.TransformPoint(new Vector3(side*(width+.50f),0,depth*.55f+.28f)),.22f);
        }
        Finish(name+" buried footing detail",batch,stone);
    }
    void Plant(Vector3 point,float height)
    {
        // No vegetation in the central square or the garden task corridor.
        if(CourtyardResidentLayout.CastleSquare(point) || CourtyardResidentLayout.GardenWalkway(point))return;
        var sprite=Resources.Load<Sprite>("CourtyardStyle/Grass");if(sprite==null)return;
        var obj=new GameObject("Sheltered footing grass",typeof(SpriteRenderer));obj.transform.SetParent(transform,false);
        var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=paper;
        renderer.color=new Color(.79f,.83f,.56f);
        float scale=height/sprite.bounds.size.y;obj.transform.localScale=Vector3.one*scale;
        obj.transform.position=Ground(point)+Vector3.up*(.025f-sprite.bounds.min.y*scale);
        plants.Add(obj.transform);
    }
    void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var plant in plants)if(plant!=null)plant.rotation=camera.transform.rotation;
    }
    void WorkAreas()
    {
        // Marks stay at actual workplaces rather than scattered across the castle square.
        var workshop=GameObject.Find("Ida workshop")?.transform;
        if(workshop!=null)
        {
            var at=workshop.TransformPoint(new Vector3(-1.85f,0,-4.42f));
            Patch(wearMarks,at,1.1f,.55f,workshop.eulerAngles.y,.078f);
            var random=new System.Random(4807);
            for(int i=0;i<13;i++)
            {
                var p=Ground(at+new Vector3((float)random.NextDouble()*.8f-.4f,0,(float)random.NextDouble()*.32f+.2f))+Vector3.up*.069f;
                float angle=(float)random.NextDouble()*6.28f,length=.08f+(float)random.NextDouble()*.10f;
                var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*length;
                var across=Vector3.Cross(Vector3.up,direction).normalized*.018f;
                litter.Face(new[]{p-direction+across,p-direction-across,p+direction-across,p+direction+across},1);
            }
            Scuffs(workshop,new Vector3(2.0f,0,-3.0f),.65f,false);
        }
        var stable=GameObject.Find("Stable")?.transform;
        if(stable!=null)
        {
            Patch(soilMarks,stable.TransformPoint(new Vector3(.8f,0,-3.6f)),1.35f,.62f,stable.eulerAngles.y,.074f);
            Scuffs(stable,new Vector3(.8f,0,-3.6f),.8f,true);
        }
        var table=GameObject.Find("Garden potting table")?.transform;
        if(table!=null)
        {
            var at=Ground(table.position+Vector3.back*.48f);
            Patch(wearMarks,at,.62f,.34f,12,.078f);
            var random=new System.Random(702);
            for(int i=0;i<7;i++)
                Pebble(gravel,Ground(at+new Vector3(.38f+i*.045f,0,.13f*Mathf.Sin(i*2)))+Vector3.down*.01f,new Vector3(.045f,.025f,.035f),random);
        }
        // Modest plants and small grit at the stair feet, away from the tread itself.
        foreach(var step in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(step.name!="Tower stair tread")continue;
            var at=step.position;
            if(at.y>1)continue;
            float half=step.lossyScale.x*.5f;
            foreach(float side in new[]{-1f,1f})
            {
                var edge=at+step.right*(half+.18f)*side;
                Patch(soilMarks,edge,.40f,.55f,step.eulerAngles.y,.074f);
            }
        }
    }
    void Scuffs(Transform frame,Vector3 local,float spread,bool hoof)
    {
        for(int i=0;i<5;i++)
        {
            var at=frame.TransformPoint(local+new Vector3((i%2==0?-1:1)*spread*.45f,0,(i-2)*.20f));
            Patch(wearMarks,at,hoof?.10f:.115f,hoof?.12f:.21f,frame.eulerAngles.y+(i%2)*19,.079f);
        }
    }
    static void Patch(Batch batch,Vector3 at,float rx,float rz,float turn,float elevation)
    {
        const int rings=4,sides=18;int start=batch.v.Count;
        var rotation=Quaternion.Euler(0,turn,0);
        batch.v.Add(Ground(at)+Vector3.up*elevation);batch.uv.Add(Vector2.zero);batch.colors.Add(new Color(0,.5f,0,1));
        for(int ring=1;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float angle=side*Mathf.PI*2/sides,r=ring/(float)rings;
            float rough=1+.12f*Mathf.Sin(angle*3+at.x)+.08f*Mathf.Cos(angle*5+at.z);
            var p=at+rotation*new Vector3(Mathf.Cos(angle)*rx*r*rough,0,Mathf.Sin(angle)*rz*r*rough);
            batch.v.Add(Ground(p)+Vector3.up*elevation);batch.uv.Add(Vector2.zero);batch.colors.Add(new Color(r,.5f,0,1));
            int current=start+1+(ring-1)*sides+side,next=start+1+(ring-1)*sides+(side+1)%sides;
            if(ring==1)batch.t.AddRange(new[]{start,next,current});
            else batch.t.AddRange(new[]{current-sides,next,current,current-sides,next-sides,next});
        }
    }
    static void Pebble(Batch batch,Vector3 at,Vector3 size,System.Random random)
    {
        var ring=new Vector3[7];var top=new Vector3[7];float turn=(float)random.NextDouble()*6.28f;
        for(int i=0;i<7;i++)
        {
            float a=turn+i*Mathf.PI*2/7,r=.85f+(float)random.NextDouble()*.2f;
            ring[i]=at+new Vector3(Mathf.Cos(a)*size.x*r,0,Mathf.Sin(a)*size.z*r);
            top[i]=at+(ring[i]-at)*.72f+Vector3.up*size.y;
        }
        batch.Face(top.Reverse().ToArray(),1);
        for(int i=0;i<7;i++){int j=(i+1)%7;batch.Face(new[]{ring[i],top[i],top[j],ring[j]},.90f);}
    }
    void Roots(Vector3 origin)
    {
        origin=Ground(origin);var batch=new Batch();
        for(int root=0;root<4;root++)
        {
            float angle=root*1.73f+.37f;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            var across=Vector3.Cross(Vector3.up,direction);Vector3[] previous=null;
            for(int step=0;step<=6;step++)
            {
                float along=step/6f;
                var at=Ground(origin+direction*(.12f+along*.95f)+across*Mathf.Sin(along*4+root)*along*.15f);
                float radius=Mathf.Lerp(.10f,.008f,along);
                at.y-=.018f;
                var ring=new Vector3[6];
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;ring[i]=at+across*(Mathf.Cos(a)*radius)+Vector3.up*(Mathf.Sin(a)*radius);}
                if(previous!=null)for(int i=0;i<6;i++){int j=(i+1)%6;batch.Face(new[]{previous[i],previous[j],ring[j],ring[i]},1);}
                previous=ring;
            }
        }
        Finish("Grounded tree roots",batch,wood);
    }
    void Finish(string name,Batch batch,Material material)
    {
        if(batch.v.Count==0)return;
        var mesh=new Mesh{name=name};mesh.SetVertices(batch.v);mesh.SetUVs(0,batch.uv);mesh.SetColors(batch.colors);
        mesh.SetTriangles(batch.t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        if(material==soil || material==moss || material==wear)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    void OnDestroy()
    {
        foreach(var mesh in owned)if(mesh!=null)Destroy(mesh);
        foreach(var material in new[]{stone,wood,soil,moss,wear,paper})if(material!=null)Destroy(material);
    }
}