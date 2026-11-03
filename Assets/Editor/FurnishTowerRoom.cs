using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Authored scene furniture: no runtime spawning, random layout or extra gameplay state.
public static class FurnishTowerRoom
{
    private const string Art = "Assets/Art/Intro/";
    private static Material wood, linen, green, gold, paper, red, ink;
    [MenuItem("Arkmon/Furnish Elian's tower room")]
    public static void Run()
    {
        FinishTowerIntro.RestoreOriginalVista();
        var scene = EditorSceneManager.GetActiveScene();
        var old = GameObject.Find("Elian personal room details");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Elian personal room details").transform;
        wood = Mat("Bed painted oak"); linen = Mat("Bed painted linen");
        green = Mat("Bed painted green velvet"); gold = Mat("Desk aged brass");
        paper = Mat("Desk parchment"); red = Mat("Desk crimson book"); ink = Mat("Desk ink");
        BedCorner(root); StudyCorner(root); WindowCorner(root);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }
    private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(
        "Assets/Art/SceneStyle/Tower light - " + name + ".mat");
    private static Transform Group(Transform parent, string name, Vector3 position, float yaw = 0)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false);
        t.localPosition = position; t.localRotation = Quaternion.Euler(0, yaw, 0); return t;
    }
    private static Transform Part(Transform parent, string name, Vector3 position, Vector3 scale,
        Material mat, PrimitiveType primitive = PrimitiveType.Cube)
    {
        var obj = GameObject.CreatePrimitive(primitive); obj.name = name;
        obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
        obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj.transform;
    }
    private static void Block(Transform parent, Vector3 center, Vector3 size)
    {
        var box = parent.gameObject.AddComponent<BoxCollider>(); box.center = center; box.size = size;
    }
    private static void BedCorner(Transform root)
    {
        var table = Group(root, "Bedside water table", new Vector3(-5.85f, 0, 2.85f), 12);
        Part(table, "Rounded oak tabletop", new Vector3(0,.94f,0), new Vector3(.54f,.065f,.54f), wood, PrimitiveType.Cylinder);
        foreach (float x in new[]{-.32f,.32f}) foreach(float z in new[]{-.32f,.32f})
            Part(table,"Turned table leg",new Vector3(x,.45f,z),new Vector3(.09f,.45f,.09f),wood,PrimitiveType.Cylinder);
        Part(table,"Lower shelf",new Vector3(0,.24f,0),new Vector3(.8f,.07f,.8f),wood);
        Block(table,new Vector3(0,.5f,0),new Vector3(1.05f,1,1.05f));
        Vessel(table,"Glazed water jug",new Vector3(-.12f,1.01f,.08f),.42f,true);
        Vessel(table,"Bedside drinking cup",new Vector3(.27f,1.01f,-.17f),.19f,false);
        Book(table,new Vector3(0,.32f,0),new Vector3(.48f,.09f,.36f),red,-8);
        var shoes=Group(root,"Bedside slippers",new Vector3(-5.25f,0,.15f),-12);
        foreach(float x in new[]{-.15f,.15f})
        {
            Part(shoes,"Soft leather slipper",new Vector3(x,.075f,0),new Vector3(.22f,.13f,.44f),wood,PrimitiveType.Sphere);
            Part(shoes,"Slipper opening",new Vector3(x,.12f,.085f),new Vector3(.15f,.035f,.19f),ink,PrimitiveType.Sphere);
        }
        // A folded throw on the existing chest, outside the waking side of the bed.
        var throwRoot=Group(root,"Folded bedside throw",new Vector3(-5.55f,.93f,.25f),-4);
        for(int i=0;i<3;i++)Part(throwRoot,"Linen folds",new Vector3(0,i*.035f,0),new Vector3(.64f,.05f,.5f),linen);
        for(int i=0;i<7;i++)Part(throwRoot,"Throw fringe",new Vector3(-.27f+i*.09f,.015f,-.28f),new Vector3(.025f,.035f,.12f),linen);
    }
    private static void StudyCorner(Transform root)
    {
        var shelf=Group(root,"Elian study wall shelf",new Vector3(-4.45f,3.8f,4.55f),-35);
        Part(shelf,"Carved shelf",Vector3.zero,new Vector3(1.65f,.11f,.48f),wood);
        foreach(float x in new[]{-.58f,.58f})
        {
            Part(shelf,"Shelf wall bracket",new Vector3(x,-.22f,.12f),new Vector3(.1f,.45f,.12f),wood);
            var brace=Part(shelf,"Shelf diagonal support",new Vector3(x,-.16f,-.01f),new Vector3(.08f,.38f,.08f),wood);
            brace.localRotation=Quaternion.Euler(-38,0,0);
        }
        for(int i=0;i<6;i++)
        {
            float h=.43f+(i%3)*.07f;
            var book=Group(shelf,"Favourite bound volume",new Vector3(-.65f+i*.17f,.06f+h*.5f,0));
            Part(book,"Book binding",Vector3.zero,new Vector3(.13f,h,.3f),i%2==0?red:green);
            foreach(float y in new[]{-.13f,.13f})Part(book,"Gilt spine band",new Vector3(0,y,-.154f),new Vector3(.13f,.025f,.015f),gold);
        }
        var keepsake=Group(shelf,"Keepsake miniature frame",new Vector3(.5f,.31f,-.02f));
        Part(keepsake,"Brass frame",Vector3.zero,new Vector3(.38f,.48f,.055f),gold);
        Part(keepsake,"Pressed flower backing",new Vector3(0,0,-.033f),new Vector3(.31f,.41f,.015f),paper);
        Part(keepsake,"Pressed flower stem",new Vector3(0,-.04f,-.045f),new Vector3(.015f,.23f,.012f),green);
        for(int i=0;i<5;i++)
        {
            float a=i*Mathf.PI*2/5;
            Part(keepsake,"Pressed flower petal",new Vector3(Mathf.Cos(a)*.045f,.08f+Mathf.Sin(a)*.045f,-.05f),new Vector3(.06f,.07f,.018f),gold,PrimitiveType.Sphere);
        }
        var desk=Group(root,"Elian writing materials",new Vector3(-1.1f,0,4.7f));
        var bookRoot=Group(desk,"Open study book",new Vector3(-.25f,1.25f,-.15f),-12);
        foreach(float side in new[]{-1f,1f})
        {
            var page=Part(bookRoot,"Open cream page",new Vector3(side*.18f,.025f,0),new Vector3(.35f,.035f,.46f),paper);
            page.localRotation=Quaternion.Euler(0,0,side*5);
            for(int line=0;line<6;line++)Part(bookRoot,"Handwritten book line",new Vector3(side*.18f,.05f,-.16f+line*.055f),new Vector3(.22f-line%2*.04f,.003f,.009f),wood);
        }
        Vessel(desk,"Small ink pot",new Vector3(.35f,1.24f,.08f),.14f,false);
        var quill=Part(desk,"Writing quill",new Vector3(.42f,1.55f,.1f),new Vector3(.075f,.42f,.025f),linen,PrimitiveType.Sphere);
        quill.localRotation=Quaternion.Euler(0,0,-22);
        for(int i=0;i<2;i++)
        {
            var letter=Part(desk,"Folded family letter",new Vector3(.48f,1.25f+i*.012f,-.22f),new Vector3(.3f,.012f,.2f),paper);
            letter.localRotation=Quaternion.Euler(0,12+i*9,0);
        }
        Part(desk,"Letter wax seal",new Vector3(.48f,1.284f,-.22f),new Vector3(.06f,.009f,.06f),red,PrimitiveType.Cylinder);
    }
    private static void WindowCorner(Transform root)
    {
        var window=Group(root,"Deep window seat",new Vector3(0,0,5.55f));
        var stone=Mat("Window painted limestone");
        Part(window,"Deep stone sill",new Vector3(0,1.93f,-.07f),new Vector3(2.6f,.18f,.66f),stone);
        Part(window,"Green window cushion",new Vector3(.24f,2.065f,-.1f),new Vector3(1.38f,.14f,.47f),green,PrimitiveType.Cube);
        Part(window,"Cushion piping",new Vector3(.24f,2.06f,-.34f),new Vector3(1.38f,.025f,.025f),gold);
        var pot=Group(window,"Window herb pot",new Vector3(-.97f,2.03f,-.05f));
        Vessel(pot,"Earthenware herb pot",Vector3.zero,.26f,false);
        for(int i=0;i<7;i++)
        {
            float a=i*2.39996f;
            var leaf=Part(pot,"Sage leaf",new Vector3(Mathf.Cos(a)*.09f,.3f+(i%3)*.04f,Mathf.Sin(a)*.09f),new Vector3(.09f,.25f,.035f),green,PrimitiveType.Sphere);
            leaf.localRotation=Quaternion.Euler(Mathf.Sin(a)*25,0,Mathf.Cos(a)*35);
        }
        // Side panels leave the arch and the hand-adjustable vista unobstructed.
        foreach(float side in new[]{-1f,1f})
        {
            var curtain=Group(root,"Gathered window curtain",new Vector3(side*1.55f,0,5.43f));
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            const int n=24;
            for(int i=0;i<=n;i++)
            {
                float u=i/(float)n,x=(u-.5f)*.43f,z=Mathf.Cos(u*Mathf.PI*8)*.055f;
                vertices.Add(new Vector3(x,1.97f+.035f*Mathf.Sin(u*Mathf.PI*4),z));
                vertices.Add(new Vector3(x,4.28f,z));uv.Add(new Vector2(u,0));uv.Add(new Vector2(u,1));
                if(i<n){int k=i*2;triangles.AddRange(new[]{k,k+1,k+3,k,k+3,k+2});}
            }
            var mesh=new Mesh{name="Pleated tower window curtain"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Art+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
            curtain.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;
            curtain.gameObject.AddComponent<MeshRenderer>().sharedMaterial=green;
            Part(curtain,"Curtain tieback",new Vector3(0,2.7f,-.07f),new Vector3(.45f,.06f,.07f),gold);
        }
        Part(root,"Window curtain rod",new Vector3(0,4.3f,5.43f),new Vector3(3.65f,.055f,.055f),wood);
        foreach(float x in new[]{-1.84f,1.84f})Part(root,"Curtain rod finial",new Vector3(x,4.3f,5.43f),Vector3.one*.13f,gold,PrimitiveType.Sphere);
    }
    private static void Book(Transform root,Vector3 position,Vector3 size,Material binding,float yaw)
    {
        var book=Group(root,"Bedtime book",position,yaw);
        Part(book,"Bound pages",Vector3.zero,size,paper);
        foreach(float side in new[]{-1f,1f})Part(book,"Book cover",new Vector3(0,side*size.y*.5f,0),new Vector3(size.x+.025f,.016f,size.z+.025f),binding);
    }
    private static void Vessel(Transform root,string name,Vector3 position,float height,bool jug)
    {
        var vessel=Group(root,name,position);
        // An open revolved profile includes the interior rim instead of a solid cylinder cap.
        Vector2[] profile=jug?new[]{new Vector2(0,0),new Vector2(.25f,0),new Vector2(.35f,.3f),new Vector2(.31f,.62f),new Vector2(.16f,.84f),new Vector2(.18f,1),new Vector2(.13f,1),new Vector2(.12f,.82f),new Vector2(.25f,.58f),new Vector2(.27f,.3f),new Vector2(.19f,.1f),new Vector2(0,.1f)}:
            new[]{new Vector2(0,0),new Vector2(.29f,0),new Vector2(.38f,1),new Vector2(.31f,1),new Vector2(.23f,.12f),new Vector2(0,.12f)};
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int n=24;
        for(int j=0;j<profile.Length;j++)for(int i=0;i<=n;i++)
        {float a=i*Mathf.PI*2/n;v.Add(new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x)*height);uv.Add(new Vector2(i/(float)n,profile[j].y));}
        for(int j=0;j<profile.Length-1;j++)for(int i=0;i<n;i++){int k=j*(n+1)+i;t.AddRange(new[]{k,k+n+1,k+1,k+1,k+n+1,k+n+2});}
        var mesh=new Mesh{name=jug?"Bedside jug profile":"Room cup profile"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        // Each size is persisted independently; subsequent runs update the same asset.
        string path=Art+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
        vessel.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;
        vessel.gameObject.AddComponent<MeshRenderer>().sharedMaterial=jug?green:wood;
        if(jug)for(int i=0;i<10;i++)
        {
            float a=(-110+i*24)*Mathf.Deg2Rad;
            Part(vessel,"Jug curved handle",new Vector3((.32f+Mathf.Cos(a)*.22f)*height,(.58f+Mathf.Sin(a)*.29f)*height,0),Vector3.one*height*.13f,gold,PrimitiveType.Sphere);
        }
    }
}
