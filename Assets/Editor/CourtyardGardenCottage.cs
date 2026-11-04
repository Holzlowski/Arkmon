using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// All cottage details use local coordinates: moving the house keeps its porch,
// planting, furniture and resident anchor together.
public static class CourtyardGardenCottage
{
    const string Art = "Assets/Art/Intro/";
    static Material plaster, wood, tiles, stone, sage, dark, iron, clay, green, soil;
    static readonly Color Cream = new Color(.93f,.84f,.65f);

    public static void Build(Transform parent, Material masonry)
    {
        stone = masonry;
        plaster = Painted("Marta limewash", Cream, "Courtyard earth grain.asset", .35f);
        wood = CopyMaterial("Marta painted oak", Art+"PaintedFurniture/Illustrated furniture oak.mat");
        tiles = Painted("Marta weathered terracotta", new Color(.69f,.34f,.19f), "Courtyard slate courses.asset", .92f);
        sage = Painted("Marta sage shutters", new Color(.35f,.48f,.28f), "Courtyard earth grain.asset", .45f);
        dark = Painted("Marta window recess", new Color(.10f,.14f,.13f), null, 1);
        iron = Painted("Marta iron fittings", new Color(.22f,.19f,.13f), null, 1);
        clay = Painted("Marta clay pots", new Color(.65f,.35f,.20f), null, 1);
        green = Painted("Marta garden leaves", new Color(.30f,.46f,.14f), null, 1);
        soil = Painted("Marta potting soil", new Color(.20f,.12f,.06f), null, 1);

        var house = Root(parent,"Garden cottage",new Vector3(1.0f,0,9.6f));
        Box(house,"Garden cottage structure",new Vector3(0,1.9f,0),new Vector3(5.6f,3.8f,4.6f),plaster,true);
        Box(house,"Cottage stone foundation",new Vector3(0,.24f,0),new Vector3(5.72f,.48f,4.72f),stone);
        for(int i=0;i<9;i++)
            Box(house,"Dressed plinth stone",new Vector3(-2.5f+i*.63f,.23f,-2.38f),new Vector3(.57f,.34f,.18f),stone);

        // Front-facing gable and slightly sagging tile courses make a readable silhouette.
        Gable(house,-2.31f);
        var rear = Root(house,"Rear plaster gable",new Vector3(0,0,2.31f));
        rear.localRotation=Quaternion.Euler(0,180,0);
        Gable(rear,0);
        Roof(house);
        foreach(float x in new[]{-2.68f,2.68f})
        {
            Box(house,"Corner oak post",new Vector3(x,2.0f,-2.4f),new Vector3(.22f,3.6f,.24f),wood);
            Box(house,"Side foundation timber",new Vector3(x,.56f,0),new Vector3(.20f,.20f,4.65f),wood);
        }
        Box(house,"Facade sill beam",new Vector3(0,.61f,-2.4f),new Vector3(5.6f,.20f,.24f),wood);
        Box(house,"Facade eaves beam",new Vector3(0,3.65f,-2.4f),new Vector3(5.7f,.23f,.28f),wood);
        Beam(house,"Left gable frame",new Vector3(-2.75f,3.72f,-2.4f),new Vector3(0,5.65f,-2.4f),.19f,wood);
        Beam(house,"Right gable frame",new Vector3(0,5.65f,-2.4f),new Vector3(2.75f,3.72f,-2.4f),.19f,wood);
        Beam(house,"Gable king post",new Vector3(0,3.65f,-2.4f),new Vector3(0,5.58f,-2.4f),.15f,wood);
        foreach(float side in new[]{-1f,1f})
        {
            Beam(house,"Diagonal half timber",new Vector3(side*2.55f,.72f,-2.42f),new Vector3(side*1.1f,1.57f,-2.42f),.13f,wood);
            Beam(house,"Upper gable strut",new Vector3(side*1.7f,3.72f,-2.42f),new Vector3(side*.5f,4.8f,-2.42f),.12f,wood);
        }

        Door(house);
        Window(house,new Vector3(-1.74f,2.08f,-2.45f),.82f,1.04f);
        Window(house,new Vector3(1.74f,2.08f,-2.45f),.82f,1.04f);
        var attic=Root(house,"Attic window",new Vector3(0,4.5f,-2.45f));
        Box(attic,"Attic dark pane",Vector3.zero,new Vector3(.59f,.69f,.08f),dark);
        foreach(float x in new[]{-.34f,.34f})
            Box(attic,"Attic jamb",new Vector3(x,0,-.08f),new Vector3(.09f,.84f,.12f),wood);
        foreach(float y in new[]{-.38f,.38f})
            Box(attic,"Attic lintel",new Vector3(0,y,-.08f),new Vector3(.74f,.09f,.12f),wood);
        Box(attic,"Attic mullion",new Vector3(0,0,-.09f),new Vector3(.06f,.69f,.10f),wood);

        var canopy=Box(house,"Porch tiled canopy",new Vector3(0,2.91f,-3.0f),new Vector3(2.28f,.16f,1.38f),tiles);
        canopy.localRotation=Quaternion.Euler(-12,0,0);
        Box(house,"Porch fascia",new Vector3(0,2.77f,-3.65f),new Vector3(2.40f,.16f,.16f),wood);
        foreach(float x in new[]{-1.05f,1.05f})
        {
            // Brackets hang from the wall, leaving the path to the door unobstructed.
            Beam(house,"Porch carved bracket",new Vector3(x,2.15f,-2.44f),new Vector3(x,2.77f,-3.37f),.13f,wood);
        }
        Box(house,"Chimney stone shaft",new Vector3(1.65f,4.9f,.80f),new Vector3(.67f,2.5f,.72f),stone);
        Box(house,"Chimney cap",new Vector3(1.65f,6.17f,.80f),new Vector3(.92f,.20f,.96f),stone);
        Box(house,"Chimney dark opening",new Vector3(1.65f,6.28f,.8f),new Vector3(.50f,.025f,.53f),dark);

        Forecourt(house);
        ImportMarta();
    }

    static Transform Root(Transform parent,string name,Vector3 at)
    {
        var obj=new GameObject(name).transform;obj.SetParent(parent,false);obj.localPosition=at;return obj;
    }
    static Material CopyMaterial(string name,string source)
    {
        string path=Art+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(source));AssetDatabase.CreateAsset(mat,path);}
        return mat;
    }
    static Material Painted(string name,Color color,string texture,float detail)
    {
        string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Arkmon/Tower Painted Light"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_TextureScale",.65f);
        mat.SetFloat("_DetailStrength",detail);mat.SetFloat("_Illustration",0);
        mat.SetTexture("_BaseMap",texture==null?Texture2D.whiteTexture:AssetDatabase.LoadAssetAtPath<Texture2D>(Art+texture));
        EditorUtility.SetDirty(mat);return mat;
    }
    static Transform Box(Transform root,string name,Vector3 position,Vector3 size,Material material,bool solid=false)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);
        obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.transform;
    }
    static void Beam(Transform root,string name,Vector3 a,Vector3 b,float thickness,Material material)
    {
        var obj=Box(root,name,(a+b)*.5f,new Vector3(thickness,Vector3.Distance(a,b),thickness),material);
        obj.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void MeshPart(Transform root,string name,List<Vector3> vertices,List<int> triangles,Material material)
    {
        string path=Art+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var obj=Root(root,name,Vector3.zero);obj.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        obj.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    static void Gable(Transform house,float z)
    {
        // Geometry shared by front and rotated rear.
        var root=Root(house,"Plaster gable face",new Vector3(0,0,z));
        MeshPart(root,"Marta plaster gable",new List<Vector3>{new Vector3(-2.8f,3.8f,0),new Vector3(2.8f,3.8f,0),new Vector3(0,5.68f,0)},new List<int>{0,2,1},plaster);
    }
    static float RoofHeight(float x,float z) => 5.72f- Mathf.Abs(x)*.64f-.10f*Mathf.Sin(Mathf.PI*(z+2.8f)/5.6f);
    static void Roof(Transform house)
    {
        // Closed dark under-roof prevents background/light leaks through the small
        // irregular joints of the separate decorative tiles.
        foreach(float side in new[]{-1f,1f})
        {
            var backing=Box(house,"Continuous roof backing",new Vector3(side*1.63f,4.49f,0),
                new Vector3(3.26f/Mathf.Cos(Mathf.Atan(.64f)),.08f,5.62f),wood);
            backing.localRotation=Quaternion.Euler(0,0,-side*Mathf.Atan(.64f)*Mathf.Rad2Deg);
        }
        var vertices=new List<Vector3>();var triangles=new List<int>();
        // Separate tiles retain their small overlap and uneven lower edges.
        foreach(int side in new[]{-1,1})
        for(int row=0;row<7;row++)
        for(int column=0;column<12;column++)
        {
            float x0=side*(row*.46f),x1=side*((row+1)*.46f+.04f);
            float z0=-2.8f+column*(5.6f/12),z1=z0+.46f;
            float lift=.025f+.018f*Mathf.Sin(column*17+row*3);
            int start=vertices.Count;
            vertices.Add(new Vector3(x0,RoofHeight(x0,z0)+lift,z0));
            vertices.Add(new Vector3(x0,RoofHeight(x0,z1)+lift,z1));
            vertices.Add(new Vector3(x1,RoofHeight(x1,z1)+lift,z1));
            vertices.Add(new Vector3(x1,RoofHeight(x1,z0)+lift,z0));
            if(side==1)triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            else triangles.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});
        }
        MeshPart(house,"Marta curved tiled roof",vertices,triangles,tiles);
        foreach(float side in new[]{-1f,1f})
            Box(house,"Dark eaves edging",new Vector3(side*3.22f,3.62f,0),new Vector3(.13f,.20f,5.75f),wood);
        for(int i=0;i<12;i++)
            Box(house,"Clay ridge cap",new Vector3(0,RoofHeight(0,-2.6f+i*.47f)+.06f,-2.6f+i*.47f),new Vector3(.24f,.13f,.48f),tiles);
    }
    static void Door(Transform house)
    {
        Box(house,"Recessed cottage doorway",new Vector3(0,1.39f,-2.34f),new Vector3(1.40f,2.55f,.10f),dark);
        for(int i=0;i<6;i++)
            Box(house,"Sage door plank",new Vector3(-.50f+i*.20f,1.36f,-2.43f),new Vector3(.19f,2.3f,.10f),sage);
        foreach(float x in new[]{-.69f,.69f})
            Box(house,"Door oak jamb",new Vector3(x,1.39f,-2.49f),new Vector3(.16f,2.61f,.23f),wood);
        Box(house,"Carved door head",new Vector3(0,2.66f,-2.49f),new Vector3(1.53f,.20f,.24f),wood);
        foreach(float y in new[]{.65f,2.1f})
            Box(house,"Forged door strap",new Vector3(0,y,-2.51f),new Vector3(1.11f,.10f,.06f),iron);
        Box(house,"Door handle",new Vector3(.38f,1.31f,-2.58f),new Vector3(.055f,.23f,.10f),iron);
        // Visual threshold stays below the walker's clearance; no new tall riser.
        Box(house,"Worn doorway threshold",new Vector3(0,.045f,-2.72f),new Vector3(1.65f,.09f,.67f),stone);
    }
    static void Window(Transform house,Vector3 at,float width,float height)
    {
        var root=Root(house,"Cottage shuttered window",at);
        Box(root,"Deep green glass",Vector3.zero,new Vector3(width,height,.10f),dark);
        foreach(float x in new[]{-width*.5f,width*.5f})
        {
            Box(root,"Oak window jamb",new Vector3(x,0,-.08f),new Vector3(.10f,height+.16f,.17f),wood);
            var shutter=Root(root,"Open sage shutter",new Vector3(x+Mathf.Sign(x)*.24f,0,-.05f));
            shutter.localRotation=Quaternion.Euler(0,Mathf.Sign(x)*-18,0);
            for(int i=0;i<3;i++)Box(shutter,"Shutter slat",new Vector3(-.12f+i*.12f,0,0),new Vector3(.11f,height,.10f),sage);
            foreach(float y in new[]{-.33f,.33f})Box(shutter,"Shutter brace",new Vector3(0,y,-.07f),new Vector3(.37f,.065f,.06f),wood);
        }
        foreach(float y in new[]{-height*.5f,height*.5f})
            Box(root,"Oak window rail",new Vector3(0,y,-.09f),new Vector3(width+.20f,.10f,.20f),wood);
        Box(root,"Window cross upright",new Vector3(0,0,-.10f),new Vector3(.055f,height,.10f),wood);
        Box(root,"Window cross rail",new Vector3(0,0,-.10f),new Vector3(width,.055f,.10f),wood);
        Box(root,"Deep stone sill",new Vector3(0,-height*.5f-.08f,-.16f),new Vector3(width+.30f,.13f,.40f),stone);
        Box(root,"Herb window box",new Vector3(0,-height*.5f-.32f,-.18f),new Vector3(width+.16f,.26f,.37f),wood);
        for(int i=0;i<5;i++)Plant(root,new Vector3(-.32f+i*.16f,-height*.5f-.16f,-.20f),.18f,i);
    }
    static void Forecourt(Transform house)
    {
        var yard=Root(house,"Marta cottage forecourt",Vector3.zero);
        // Pavers are shallow decoration; the existing continuous ground remains walkable.
        for(int i=0;i<5;i++)
        {
            float z=-2.92f-i*.60f,x=Mathf.Sin(i*.8f)*.14f;
            var paver=Box(yard,"Worn approach flagstone",new Vector3(x,.025f,z),new Vector3(1.35f,.045f,.50f),stone);
            paver.localRotation=Quaternion.Euler(0,Mathf.Sin(i*4)*6,0);
        }
        var table=Root(yard,"Garden potting table",new Vector3(2.60f,0,-3.55f));
        Box(table,"Potting tabletop",new Vector3(0,.96f,0),new Vector3(1.40f,.13f,.70f),wood,true);
        foreach(float x in new[]{-.55f,.55f})
        foreach(float z in new[]{-.23f,.23f})
            Box(table,"Potting table leg",new Vector3(x,.46f,z),new Vector3(.12f,.92f,.12f),wood);
        Box(table,"Potting table lower shelf",new Vector3(0,.25f,0),new Vector3(1.25f,.10f,.58f),wood);
        for(int i=0;i<3;i++)Pot(table,new Vector3(-.43f+i*.40f,1.03f,.08f),.15f,true);
        Box(table,"Folded garden cloth",new Vector3(.43f,1.045f,-.19f),new Vector3(.34f,.045f,.22f),plaster);
        Root(house,"Marta work spot",new Vector3(2.55f,0,-4.65f));
        Pot(yard,new Vector3(-2.30f,0,-3.18f),.35f,true);
        Pot(yard,new Vector3(-1.80f,0,-3.50f),.24f,true);
        for(int i=0;i<8;i++)
        {
            float x=i<4?-2.72f:3.35f,z=-2.5f-(i%4)*.52f;
            Plant(yard,new Vector3(x,0,z),.25f,i);
        }
        // A low basket at the work station relates Marta's harvest to the existing beds.
        Box(table,"Harvest crate base",new Vector3(0,.31f,0),new Vector3(.62f,.08f,.45f),wood);
        foreach(float x in new[]{-.30f,.30f})
            Box(table,"Harvest crate side",new Vector3(x,.44f,0),new Vector3(.065f,.28f,.45f),wood);
        foreach(float z in new[]{-.20f,.20f})
            Box(table,"Harvest crate slat",new Vector3(0,.44f,z),new Vector3(.62f,.20f,.06f),wood);
        for(int i=0;i<3;i++)Plant(table,new Vector3(-.20f+i*.20f,.49f,0),.12f,i);
    }
    static void Pot(Transform parent,Vector3 at,float radius,bool plant)
    {
        var root=Root(parent,"Open terracotta pot",at);
        // Eight rim segments around a dark soil surface keep the vessel visibly hollow.
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;
            var wall=Box(root,"Clay pot wall",new Vector3(Mathf.Cos(a)*radius,radius*.63f,Mathf.Sin(a)*radius),new Vector3(radius*.87f,radius*1.25f,.07f),clay);
            wall.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg+90,0);
        }
        Box(root,"Pot soil",new Vector3(0,radius*1.05f,0),new Vector3(radius*1.5f,.04f,radius*1.5f),soil);
        if(plant)Plant(root,new Vector3(0,radius*1.2f,0),radius*.8f,2);
    }
    static void Plant(Transform parent,Vector3 at,float size,int seed)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"Aurenfeld folded leaf.asset");
        if(mesh==null)return;
        var root=Root(parent,"Garden herb cluster",at);
        for(int i=0;i<5;i++)
        {
            var leaf=Root(root,"Painted herb leaf",new Vector3(Mathf.Sin(i*2+seed)*size*.4f,size*.50f,Mathf.Cos(i*2)*size*.4f));
            leaf.localScale=Vector3.one*size;
            leaf.localRotation=Quaternion.Euler(-25+i*12,i*72+seed*17,i*23);
            leaf.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            leaf.gameObject.AddComponent<MeshRenderer>().sharedMaterial=green;
        }
    }
    static void ImportMarta()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Marta-Gardener.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=680;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.015f);
        importer.SetTextureSettings(settings);importer.SaveAndReimport();
    }
}