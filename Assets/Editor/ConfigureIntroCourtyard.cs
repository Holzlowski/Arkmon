using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureIntroCourtyard
{
    const string Art="Assets/Art/Intro/";
    static Material stone,oak,roof,grass,path,soil;
    [MenuItem("Arkmon/Build intro courtyard layout")]
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        var root=new GameObject("Intro courtyard layout pending").transform;
        string[] keep={"Player residence tower","Tower entrance","Stable","Workbench","Wheelbarrow","Bench seat","Fountain basin","Outside gate"};
        var retained=new Dictionary<string,Transform>();foreach(string name in keep){var obj=GameObject.Find(name);if(obj!=null){obj.transform.SetParent(root,true);retained[name]=obj.transform;}}
        var old=GameObject.Find("Intro courtyard layout");if(old!=null)Object.DestroyImmediate(old);
        var legacy=GameObject.Find("Demo castle courtyard");
        foreach(Transform child in legacy.transform.Cast<Transform>().ToArray())
            if(child.GetComponentInChildren<PlayerController>()==null && child.GetComponentInChildren<NPC>()==null)Object.DestroyImmediate(child.gameObject);
        root.name="Intro courtyard layout";
        var opening=legacy.GetComponent<DemoOpening>();if(opening.GetComponent<CourtyardCamera>()==null)opening.gameObject.AddComponent<CourtyardCamera>();
        var cameraSettings=new SerializedObject(opening.GetComponent<CourtyardCamera>());
        cameraSettings.FindProperty("distance").floatValue=30;
        cameraSettings.FindProperty("pitch").floatValue=25;
        cameraSettings.FindProperty("fieldOfView").floatValue=48;
        cameraSettings.FindProperty("focusHeight").floatValue=3.2f;
        cameraSettings.FindProperty("horizontalBounds").vector2Value=new Vector2(-30,52);
        cameraSettings.ApplyModifiedPropertiesWithoutUndo();
        var textureImporter=AssetImporter.GetAtPath(Art+"Aurenfeld illustrated limestone.png") as TextureImporter;
        if(textureImporter!=null)
        {
            textureImporter.textureType=TextureImporterType.Default;textureImporter.wrapMode=TextureWrapMode.Repeat;
            textureImporter.maxTextureSize=2048;textureImporter.anisoLevel=4;textureImporter.SaveAndReimport();
        }
        stone=Clone("Courtyard stone architecture","Assets/Art/SceneStyle/Courtyard painted limestone.mat");
        stone.shader=Shader.Find("Arkmon/Tower Painted Light");
        stone.SetFloat("_DetailStrength",1);
        stone.SetFloat("_Illustration",0);
        stone.SetFloat("_TextureScale",.26f);
        stone.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Aurenfeld illustrated limestone.png"));
        oak=Clone("Courtyard oak architecture","Assets/Art/SceneStyle/Courtyard painted oak.mat");
        roof=Clone("Courtyard roof architecture","Assets/Art/SceneStyle/Courtyard painted roof.mat");
        grass=Mat("Courtyard grass floor",new Color(.40f,.54f,.22f));
        grass.shader=Shader.Find("Arkmon/Tower Painted Light");
        grass.SetTexture("_BaseMap",BuildEarthTexture());
        grass.SetFloat("_TextureScale",.65f);
        grass.SetFloat("_DetailStrength",.18f);
        path=Mat("Courtyard warm earth",new Color(.54f,.39f,.24f));
        path.SetTexture("_BaseMap",BuildEarthTexture());
        soil=Mat("Courtyard garden soil",new Color(.25f,.15f,.07f));
        Box(root,"Courtyard walkable ground",new Vector3(0,-.15f,1),new Vector3(88,.3f,40),grass,true).gameObject.tag="Ground";
        BuildPath(root, new Vector2(-34,5.35f), new Vector2(-34,3.8f), new Vector2(-30,1.3f), new Vector2(-18,-5.8f),
            new Vector2(-5,-6.2f), new Vector2(7,-1.8f), new Vector2(21,-2.9f),
            new Vector2(32,-4.1f), new Vector2(43,-1));
        BuildPath(root, new Vector2(-16,-6), new Vector2(-16,0), new Vector2(-12,1.3f));
        BuildPath(root, new Vector2(11,-1), new Vector2(13,3), new Vector2(12,8));
        Box(root,"Meadow beyond the west breach",new Vector3(-74,-.24f,21),new Vector3(65,.38f,105),grass);
        Walls(root);
        BuildMeadowRelief(root);
        Move(retained,"Player residence tower",new Vector3(-34,1.4f,13),1.3f);
        Move(retained,"Tower entrance",new Vector3(-34,1.4f,9.4f),1.1f);
        CourtyardStable.Build(retained["Stable"],stone);
        UnifyTowerMaterials(retained["Player residence tower"]);
        Move(retained,"Workbench",new Vector3(-23,0,7.3f),1);
        Move(retained,"Wheelbarrow",new Vector3(22.2f,0,7.9f),1);
        Move(retained,"Bench seat",new Vector3(-1.6f,0,3.4f),1);
        Move(retained,"Fountain basin",new Vector3(-2.8f,0,5.7f),1.15f);
        if(retained.TryGetValue("Outside gate",out var oldGate))Object.DestroyImmediate(oldGate.gameObject);
        CourtyardWestArchitecture.Build(root,retained["Player residence tower"],stone,oak,GroundHeight);
        CourtyardWorkshop.Build(root,retained["Workbench"],stone);
        var workshop=CourtyardWorkshop.Position;
        BuildPath(root,new Vector2(-25,-3.2f),new Vector2(workshop.x+1.5f,workshop.z-6),new Vector2(workshop.x+1.6f,workshop.z-4.2f));
        int detailStart=root.childCount;
        CourtyardDetails(root);
        ShiftGardenDetails(root,detailStart);
        DressBreach(root);
        CourtyardGardenCottage.Build(root,stone);
        House(root,"East cottage",new Vector3(35,0,13),5.5f,3.8f,5,roofPitch:42,roofFinish:SlateRoof());
        House(root,"Castle upper keep",new Vector3(12,0,20.5f),18,14,5,true);
        House(root,"Castle keep west tower",new Vector3(2.4f,0,19.8f),4,16.5f,4,true);
        House(root,"Castle keep east tower",new Vector3(21.6f,0,19.8f),4,16.5f,4,true);
        House(root,"Castle hall",new Vector3(12,0,16),13,9,5.5f,true);
        House(root,"Cellar storehouse",new Vector3(27.3f,0,15.5f),5,4.2f,4.5f);
        Entrance(root,retained["Tower entrance"].position+new Vector3(0,0,-.45f),CourtyardEntrance.Destination.Tower);
        Door(root,new Vector3(12,0,12.95f),3.5f,4.5f);Entrance(root,new Vector3(12,0,12.2f),CourtyardEntrance.Destination.ThroneHall);
        Door(root,new Vector3(27.3f,0,13),2.3f,2.9f);Entrance(root,new Vector3(27.3f,0,12.2f),CourtyardEntrance.Destination.Cellar);
        EastCrossing(root);Entrance(root,new Vector3(41,0,-1),CourtyardEntrance.Destination.OuterGate);
        Garden(root);GrassTufts(root);Horizon();
        int finishStart=root.childCount;
        FinishCourtyard(root);
        ShiftGardenDetails(root,finishStart);
        ValidateBuildingClearances(root);
        AddCourtyardAmbience(root);
        DressGardenVerge(root);
        foreach(var npc in Object.FindObjectsByType<NPC>(FindObjectsSortMode.None))if(npc.npcName=="John")npc.transform.position=new Vector3(10.3f,GroundHeight(10.3f,10.4f)+.005f,10.4f);
        var player=GameObject.FindWithTag("Player");player.transform.position=new Vector3(-34,2.4f,8);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static Material Clone(string name,string source){string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(source));AssetDatabase.CreateAsset(mat,path);}EditorUtility.SetDirty(mat);return mat;}
    static Material Mat(string name,Color tint){string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",tint);mat.SetFloat("_Smoothness",.02f);EditorUtility.SetDirty(mat);return mat;}
    static Transform Box(Transform root,string name,Vector3 position,Vector3 size,Material material,bool collision=false)
    {var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;}
    static void Move(Dictionary<string,Transform> objects,string name,Vector3 position,float scale){if(objects.TryGetValue(name,out var obj)){obj.position=position;obj.localScale=Vector3.one*scale;}}
    static void UnifyTowerMaterials(Transform tower)
    {
        var slate=SlateRoof();
        slate.shader=Shader.Find("Arkmon/Tower Painted Light");
        slate.SetFloat("_TextureScale",.55f);
        slate.SetFloat("_DetailStrength",1);
        EditorUtility.SetDirty(slate);
        foreach(var renderer in tower.GetComponentsInChildren<MeshRenderer>())
        {
            string part=renderer.name.ToLowerInvariant();
            if(part.Contains("roof"))renderer.sharedMaterial=slate;
            else if(!part.Contains("window") || part.Contains("sill") || part.Contains("lintel") || part.Contains("jamb"))
                renderer.sharedMaterial=stone;
        }
    }

    static void ShiftGardenDetails(Transform root,int start)
    {
        // Retained eastern cottage details still use their original authored coordinates.
        for(int i=start;i<root.childCount;i++)
        {
            var part=root.GetChild(i);
            if(part.name.StartsWith("East cottage") || (part.name.StartsWith("Cottage") && part.position.x>30))
                part.position+=Vector3.right*2;
        }
    }

    static void ValidateBuildingClearances(Transform root)
    {
        string[] homes={"Stable","West cottage","Garden cottage","East cottage","Cellar storehouse","Ida workshop"};
        string[] castles={"Castle west wing","Aurenfeld upper residence","Tower connecting wing","Castle hall","Castle upper keep","Castle keep west tower","Castle keep east tower"};
        Bounds BoundsOf(string name)
        {
            var renderers=root.Find(name).GetComponentsInChildren<MeshRenderer>();
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        for(int i=0;i<homes.Length;i++)
        foreach(string other in homes.Skip(i+1).Concat(castles))
            if(BoundsOf(homes[i]).Intersects(BoundsOf(other)))
                throw new System.InvalidOperationException("Building overlap: "+homes[i]+" "+BoundsOf(homes[i])+" / "+other+" "+BoundsOf(other));
    }

    // Compact hills leave flat building pads and accessible entrance landings.
    static float GroundHeight(float x,float z)
    {
        float Hill(float cx,float cz,float rx,float rz,float height)
        {
            float r=Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((z-cz)/rz,2);
            return height*Mathf.Pow(Mathf.Max(0,1-r),2);
        }
        float height=Hill(-39,10,5.8f,8,1.35f)+Hill(-28,4,5.5f,5,1.15f)
            +Hill(-18,3,7,3.5f,1.1f)
            +Hill(-20,-11,10,5,1.2f)+Hill(0,-11,8,5,1.55f)
            +Hill(26,-10,10,6,1.25f)+Hill(37,5,4,4,.75f);
        // A level working pad with a soft edge into the existing sloped meadow.
        var workshop=CourtyardWorkshop.Position;
        float edge=Mathf.Max(Mathf.Abs(x-workshop.x)-4.3f,Mathf.Abs(z-(workshop.z-1))-4.3f);
        float stableEdge=Mathf.Max(Mathf.Abs(x-CourtyardStable.Position.x)-4.5f,Mathf.Abs(z-(CourtyardStable.Position.z-1))-3.8f);
        return height*Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/1.5f))*Mathf.SmoothStep(0,1,Mathf.Clamp01(stableEdge/1.2f));
    }

    static void BuildMeadowRelief(Transform root)
    {
        const int columns=172, rows=68;
        var vertices=new List<Vector3>();
        var triangles=new List<int>();
        for(int row=0;row<=rows;row++)
        for(int column=0;column<=columns;column++)
        {
            float u=column/(float)columns, v=row/(float)rows;
            float x=Mathf.Lerp(-43,43,u), z=Mathf.Lerp(-17,17,v);
            vertices.Add(new Vector3(x,GroundHeight(x,z)+.005f,z));
            if(row==0 || column==0)continue;
            int index=vertices.Count-1;
            int lower=index-columns-1;
            triangles.AddRange(new[]{lower-1,index-1,lower,lower,index-1,index});
        }
        const string asset=Art+"Courtyard meadow relief.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,asset);}
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var meadow=new GameObject("Rolling courtyard meadow",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
        meadow.transform.SetParent(root,false);
        meadow.tag="Ground";
        meadow.GetComponent<MeshFilter>().sharedMesh=mesh;
        meadow.GetComponent<MeshRenderer>().sharedMaterial=grass;
        meadow.GetComponent<MeshCollider>().sharedMesh=mesh;
    }

    static void Walls(Transform root)
    {
        Box(root,"North castle wall",new Vector3(0,4,19),new Vector3(86,8,1.2f),stone,true);
        Box(root,"West wall near",new Vector3(-43,4,-7),new Vector3(1.2f,8,21),stone,true);
        Box(root,"West wall far",new Vector3(-43,4,15),new Vector3(1.2f,8,9),stone,true);
        Box(root,"Broken west wall base",new Vector3(-43,.45f,7),new Vector3(1.5f,.9f,7),stone,true);
        for(int i=0;i<8;i++)
        {
            var rubble=Box(root,"Fallen limestone",new Vector3(-42+i*.37f,.22f,5+Mathf.Sin(i*2)*1.5f),
                new Vector3(.7f,.44f,.55f),stone);
            rubble.localRotation=Quaternion.Euler(0,i*37,0);
        }
        var eastCutaway=Box(root,"East wall south of gate",new Vector3(43,.55f,-10.5f),new Vector3(1.2f,1.1f,14),stone);
        var eastBlocker=eastCutaway.gameObject.AddComponent<BoxCollider>();
        eastBlocker.center=new Vector3(0,3,0);eastBlocker.size=new Vector3(1,7,1);
        Box(root,"East wall north of gate",new Vector3(43,4,10.5f),new Vector3(1.2f,8,18),stone,true);
        Box(root,"East gate lintel",new Vector3(43,6.5f,-1),new Vector3(2,3,5),stone,true);
        // The near wall is visually cut down, but its full-height collision encloses the yard.
        var front=Box(root,"Cutaway near castle wall",new Vector3(0,.55f,-17),new Vector3(86,1.1f,1.1f),stone);
        var blocker=front.gameObject.AddComponent<BoxCollider>();blocker.center=new Vector3(0,3,0);blocker.size=new Vector3(1,7,1);
        for(int i=0;i<29;i++)Box(root,"North battlement",new Vector3(-42+i*3,8.65f,19),new Vector3(1.3f,1.3f,1.3f),stone);
        foreach(float x in new[]{-43,43})for(int i=x>0?6:0;i<12;i++)Box(root,"Side battlement",new Vector3(x,8.65f,-15+i*3),new Vector3(1.3f,1.3f,1.3f),stone);
    }
    static void CourtyardDetails(Transform root)
    {
        // The low connecting wing leaves the tower entrance and terrace clear.
        House(root,"West cottage",new Vector3(-3.8f,0,15.7f),4.3f,2.7f,4.1f,roofPitch:16);
        BuildPath(root,new Vector2(4,-2),new Vector2(2.4f,.7f),new Vector2(2,6.8f));
        Box(root,"Garden bench seat",new Vector3(4.9f,.55f,3.4f),new Vector3(2.1f,.15f,.48f),oak,true);
        foreach(float x in new[]{4.15f,5.65f})
            Box(root,"Garden bench stone foot",new Vector3(x,.23f,3.4f),new Vector3(.24f,.46f,.35f),stone,true);
        var crest=AssetDatabase.LoadAssetAtPath<Material>(Art+"Aurenfeld painted crest.mat");
        if(crest!=null)
        {
            foreach(float x in new[]{9.5f,14.5f})
            {
                Box(root,"Aurenfeld green banner",new Vector3(x,5.8f,13.05f),new Vector3(1.25f,2.5f,.08f),
                    Mat("Courtyard banner green",new Color(.075f,.19f,.10f)));
                var shield=GameObject.CreatePrimitive(PrimitiveType.Quad);
                shield.name="Aurenfeld banner crest";
                shield.transform.SetParent(root,false);
                shield.transform.localPosition=new Vector3(x,6.05f,12.99f);
                shield.transform.localScale=new Vector3(.9f,.98f,1);
                shield.GetComponent<Renderer>().sharedMaterial=crest;
                Object.DestroyImmediate(shield.GetComponent<Collider>());
            }
        }
    }

    static Texture2D BuildEarthTexture()
    {
        const string asset=Art+"Courtyard earth grain.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
        if(texture!=null)return texture;

        const int size=128;
        texture=new Texture2D(size,size,TextureFormat.RGBA32,true);
        texture.name="Courtyard earth grain";
        texture.wrapMode=TextureWrapMode.Repeat;
        texture.filterMode=FilterMode.Trilinear;
        var pixels=new Color32[size*size];
        var random=new System.Random(44);
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            float u=x*Mathf.PI*2/size, v=y*Mathf.PI*2/size;
            float wash=.85f+.05f*Mathf.Sin(u*2+Mathf.Sin(v*3))+.035f*Mathf.Cos(v*4-u*3);
            float grain=((float)random.NextDouble()-.5f)*.12f;
            pixels[y*size+x]=new Color(wash+grain,wash+grain*.8f,wash+grain*.65f);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture,asset);
        return texture;
    }

    static void DressGardenVerge(Transform root)
    {
        var clover = Mat("Courtyard verge clover", new Color(.29f,.43f,.13f));
        var dryStone = Mat("Courtyard path pebbles", new Color(.52f,.45f,.33f));
        var rng = new System.Random(214);
        var meadowShade = Mat("Courtyard meadow shade", new Color(.28f,.39f,.145f));
        for(int i=0;i<38;i++)
        {
            float x=-26+(float)rng.NextDouble()*45;
            float z=i%2==0?-8-(float)rng.NextDouble()*6:6+(float)rng.NextDouble()*4;
            Leaf(root,"Meadow color patch",new Vector3(x,.002f,z),
                new Vector3(.65f+(float)rng.NextDouble()*1.2f,.009f,.5f+(float)rng.NextDouble()*.7f),meadowShade);
        }
        for (int i = 0; i < 85; i++)
        {
            float x = -30 + (float)rng.NextDouble() * 51;
            float z = 15.5f + (float)rng.NextDouble() * 2.6f;
            Leaf(root,"Wall foot groundcover",new Vector3(x,.13f,z),new Vector3(.42f,.29f,.34f),clover);
        }
        for (int i = 0; i < 32; i++)
        {
            float x = -15 + i * .94f + ((float)rng.NextDouble()-.5f)*1.1f;
            float z = -5.9f + Mathf.SmoothStep(0,3.35f,i/31f);
            float jitter = ((float)rng.NextDouble()-.5f)*.55f;
            Leaf(root,"Path edge grass",new Vector3(x,.11f,z+jitter-.25f),new Vector3(.18f+(float)rng.NextDouble()*.22f,.16f+(float)rng.NextDouble()*.2f,.21f),clover);
            if(i%3==0)
                Leaf(root,"Path edge pebble",new Vector3(x,.027f,z+jitter+.36f),new Vector3(.17f,.065f,.12f),dryStone);
        }
    }

    static void AddCourtyardAmbience(Transform root)
    {
        AssetDatabase.ImportAsset("Assets/Resources/Audio/IntroFoley/courtyard-stream.wav");
        var stream = new GameObject("Spatial stream ambience", typeof(AudioSource));
        stream.transform.SetParent(root, false);
        stream.transform.localPosition = new Vector3(47, -.45f, -1);
        var audio = stream.GetComponent<AudioSource>();
        audio.clip = Resources.Load<AudioClip>("Audio/IntroFoley/courtyard-stream");
        audio.loop = true;
        audio.volume = .24f;
        audio.spatialBlend = 1;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.minDistance = 4;
        audio.maxDistance = 19;
        audio.dopplerLevel = 0;

        var pollen = Mat("Courtyard pollen gold", new Color(1f, .79f, .42f));
        pollen.EnableKeyword("_EMISSION");
        pollen.SetColor("_EmissionColor", new Color(.9f, .56f, .18f));
        for (int i = 0; i < 24; i++)
        {
            var position = new Vector3(-42 + i % 8 * .61f, .45f + i % 7 * .29f, 4 + i / 8 * 1.24f);
            Leaf(root, "Sunlit pollen", position, Vector3.one * (.025f + i % 3 * .008f), pollen);
            root.GetChild(root.childCount - 1).gameObject.AddComponent<CourtyardSceneryMotion>();
        }
        for (int i = 0; i < 24; i++)
        {
            float x = -41 + i % 12 * .58f;
            float z = i < 12 ? 3.6f : 10.4f;
            var blade = Box(root, "Windblown grass", new Vector3(x,.24f,z), new Vector3(.055f,.48f,.08f), grass);
            blade.gameObject.AddComponent<CourtyardSceneryMotion>();
        }
    }

    static void FinishCourtyard(Transform root)
    {
        var moss=Mat("Courtyard stone patina",new Color(.27f,.32f,.16f));
        for(int i=0;i<16;i++)
        {
            float z=3.6f+i*.4f;
            Box(root,"Broken masonry edge",new Vector3(-43,.9f+Mathf.Sin(i*1.6f)*.36f,z),
                new Vector3(1.25f,.42f,.42f),stone,true);
            Leaf(root,"Moss on fallen stone",new Vector3(-42.35f,.38f,z),new Vector3(.26f,.16f,.36f),moss);
        }
        var windowShade=Mat("Courtyard window shadow",new Color(.105f,.15f,.155f));
        var shutters=Mat("Courtyard painted shutters",new Color(.24f,.36f,.28f));
        foreach(var facade in new[]{new Vector3(-3.8f,0,13.6f),new Vector3(33,0,10.43f)})
        {
            Box(root,"Cottage recessed door",facade+Vector3.up*.95f,new Vector3(.8f,1.9f,.13f),oak);
            foreach(float side in new[]{-.52f,.52f})
                Box(root,"Cottage window shutter",facade+new Vector3(1.35f+side,1.65f,0),
                    new Vector3(.25f,.67f,.12f),shutters);
            var window=facade+new Vector3(1.35f,1.65f,-.065f);
            Box(root,"Cottage window shadow",window,new Vector3(.74f,.65f,.11f),windowShade);
            foreach(float side in new[]{-.37f,.37f})
                Box(root,"Cottage window jamb",window+new Vector3(side,0,-.04f),new Vector3(.085f,.72f,.17f),oak);
            Box(root,"Cottage window head",window+new Vector3(0,.34f,-.04f),new Vector3(.82f,.09f,.17f),oak);
            Box(root,"Cottage window sill",window+new Vector3(0,-.34f,-.085f),new Vector3(.91f,.12f,.26f),stone);
            Box(root,"Cottage window mullion",window+new Vector3(0,0,-.075f),new Vector3(.065f,.63f,.08f),oak);
        }
        Box(root,"East cottage chimney",new Vector3(34.4f,4.4f,13.5f),new Vector3(.63f,1.7f,.67f),stone);
        foreach(float x in new[]{32.52f,33.48f})
            Box(root,"East cottage stone door jamb",new Vector3(x,1.05f,10.34f),new Vector3(.17f,2.1f,.29f),stone);
        Box(root,"East cottage stone lintel",new Vector3(33,2.09f,10.34f),new Vector3(1.15f,.22f,.29f),stone);
        Box(root,"East cottage stone threshold",new Vector3(33,.07f,10.18f),new Vector3(1.22f,.14f,.51f),stone);

        var metal=Mat("Courtyard forged iron",new Color(.19f,.15f,.11f));
        foreach(float x in new[]{43.45f,50.48f})
            Box(root,"Drawbridge iron strap",new Vector3(x,.047f,-1),new Vector3(.18f,.035f,4.15f),metal);
        foreach(float z in new[]{-3.1f,1.1f})
            Box(root,"Gate chain anchor",new Vector3(44.6f,3.9f,z),new Vector3(.21f,.48f,.38f),metal);
        foreach(float x in new[]{44.2f,49.7f})
        for(int i=0;i<12;i++)
        {
            float z=-15+i*2.9f;
            Leaf(root,"Stream bank rushes",new Vector3(x,-.29f,z),new Vector3(.24f,.76f,.28f),grass);
            Leaf(root,"Rounded stream stone",new Vector3(x,-.73f,z+.7f),new Vector3(.54f,.33f,.64f),stone);
        }
        foreach(Transform child in root)
        {
            if(child.name=="Stream surface glint" || child.name=="Aurenfeld green banner" || child.name=="Aurenfeld banner crest")
                child.gameObject.AddComponent<CourtyardSceneryMotion>();
        }
    }

    static void DressBreach(Transform root)
    {
        var foliage=Mat("Courtyard ivy",new Color(.22f,.34f,.10f));
        var flowers=Mat("Courtyard golden flowers",new Color(.94f,.63f,.18f));
        var rng=new System.Random(721);
        for(int i=0;i<48;i++)
        {
            float z=3.8f+(float)rng.NextDouble()*6.4f;
            float x=-42.2f+(float)rng.NextDouble()*4;
            float y=i<24?.28f:.65f+(float)rng.NextDouble()*.65f;
            Leaf(root,"Breach foliage",new Vector3(i<24?x:-42.32f,y,z),new Vector3(.32f,.2f,.39f),foliage);
            if(i%4==0)Leaf(root,"Breach wildflower",new Vector3(x,.43f,z),Vector3.one*.15f,flowers);
        }
        for(int cluster=0;cluster<14;cluster++)
        {
            float x=-42.3f+(float)rng.NextDouble()*5.5f;
            float z=3.6f+(float)rng.NextDouble()*7f;
            for(int blade=0;blade<5;blade++)
            {
                float height=.18f+(float)rng.NextDouble()*.36f;
                var stalk=Box(root,"Breach grass blade",new Vector3(x+blade*.065f,height*.46f,z),
                    new Vector3(.046f,height,.065f),blade%2==0?grass:foliage);
                stalk.localRotation=Quaternion.Euler(blade*7,cluster*23,blade*8-16);
            }
        }
        for(int i=0;i<27;i++)
        {
            float z=4.25f+i*.18f;
            float y=.78f+Mathf.Sin(i*.52f)*.22f;
            Leaf(root,"Ivy along broken masonry",new Vector3(-42.25f,y,z),
                new Vector3(.18f,.15f,.24f),foliage);
        }
        var sun=new GameObject("Warm light through the breach",typeof(Light));
        sun.transform.SetParent(root,false);
        sun.transform.localPosition=new Vector3(-47,5.5f,7);
        sun.transform.rotation=Quaternion.LookRotation(new Vector3(13,-5,-1.1f));
        var light=sun.GetComponent<Light>();
        light.type=LightType.Spot;
        light.color=new Color(1f,.79f,.48f);
        light.intensity=11f;
        light.range=22;
        light.spotAngle=47;
        light.shadows=LightShadows.Soft;
    }

    static void Leaf(Transform root,string name,Vector3 position,Vector3 scale,Material material)
    {
        var leaf=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaf.name=name;
        leaf.transform.SetParent(root,false);
        leaf.transform.localPosition=position+Vector3.up*GroundHeight(position.x,position.z);
        leaf.transform.localScale=scale;
        leaf.GetComponent<Renderer>().sharedMaterial=material;
        Object.DestroyImmediate(leaf.GetComponent<Collider>());
    }

    static void BuildPath(Transform root,params Vector2[] points)
    {
        var vertices=new List<Vector3>();
        var uvs=new List<Vector2>();
        var triangles=new List<int>();
        float distance=0;
        Vector2 previous=points[0];
        const int acrossSegments=16;
        for(int i=0;i<points.Length-1;i++)
        {
        int samples=Mathf.Max(8,Mathf.CeilToInt(Vector2.Distance(points[i],points[i+1])/.2f));
        for(int sample=0;sample<=samples;sample++)
        {
            if(i>0 && sample==0)continue;
            float t=sample/(float)samples;
            Vector2 a=points[Mathf.Max(0,i-1)], b=points[i], c=points[i+1], d=points[Mathf.Min(points.Length-1,i+2)];
            Vector2 q=2*a-5*b+4*c-d, r=-a+3*b-3*c+d;
            Vector2 center=.5f*(2*b+(c-a)*t+q*t*t+r*t*t*t);
            Vector2 tangent=(c-a)+2*q*t+3*r*t*t;
            Vector2 across=new Vector2(-tangent.y,tangent.x).normalized;
            distance+=Vector2.Distance(previous,center);
            previous=center;
            float halfWidth=(points.Length>3?2.05f:1.12f)+.24f*Mathf.Sin(distance*.34f)+.12f*Mathf.Sin(distance*1.13f);
            int index=vertices.Count;
            for(int side=0;side<=acrossSegments;side++)
            {
                Vector2 edge=center+across*(halfWidth*Mathf.Lerp(-1,1,side/(float)acrossSegments));
                vertices.Add(new Vector3(edge.x,GroundHeight(edge.x,edge.y)+.03f,edge.y));
                uvs.Add(edge*.25f);
            }
            if(index>0)
            for(int side=0;side<acrossSegments;side++)
            {
                int a0=index-acrossSegments-1+side,b0=index+side;
                triangles.AddRange(new[]{a0,a0+1,b0,b0,a0+1,b0+1});
            }
        }
        }
        string asset=Art+"Courtyard curved path "+points[0].x+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,asset);}
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var road=new GameObject("Curved courtyard path",typeof(MeshFilter),typeof(MeshRenderer));
        road.transform.SetParent(root,false);
        road.GetComponent<MeshFilter>().sharedMesh=mesh;
        road.GetComponent<MeshRenderer>().sharedMaterial=path;
    }

    static void EastCrossing(Transform root)
    {
        var iron=Mat("Courtyard forged iron",new Color(.19f,.15f,.11f));
        foreach(float z in new[]{-3.2f,1.2f})
        {
            Box(root,"Gate stone pier",new Vector3(43.4f,2.3f,z),new Vector3(2.3f,4.6f,.7f),stone,true);
            for(int i=0;i<13;i++)
            {
                float t=i/12f;
                var link=Box(root,"Drawbridge chain link",Vector3.Lerp(new Vector3(44,3.9f,z),new Vector3(50.45f,.25f,z),t),
                    new Vector3(.6f,.095f,.095f),iron);
                link.localRotation=Quaternion.Euler(i%2*35f,0,-29f);
            }
        }
        for(int i=0;i<=12;i++)
        {
            float angle=i*Mathf.PI/12f;
            var block=Box(root,"Gate arch voussoir",new Vector3(43.45f,3.3f+Mathf.Sin(angle)*2.25f,-1+Mathf.Cos(angle)*2.25f),
                new Vector3(2.35f,.49f,.63f),stone);
            block.localRotation=Quaternion.Euler(-i*15f,0,0);
        }
        var water=Mat("Courtyard stream",new Color(.08f,.37f,.49f));
        water.SetFloat("_Smoothness",.72f);
        var ripple=Mat("Courtyard stream foam",new Color(.43f,.72f,.72f));
        for(int i=0;i<22;i++)
        {
            float x=45.2f+Mathf.Sin(i*2.4f)*1.8f;
            Box(root,"Stream surface glint",new Vector3(x,-.753f,-17+i*1.65f),
                new Vector3(.12f,.012f,.45f+(i%4)*.2f),ripple);
        }
        Box(root,"Exterior stream",new Vector3(47,-.8f,23),new Vector3(6.4f,.08f,100),water);
        Box(root,"Far stream bank",new Vector3(75,-.15f,23),new Vector3(50,.3f,100),grass,true).gameObject.tag="Ground";
        Box(root,"Meadow behind the east wall",new Vector3(26,-.18f,46),new Vector3(36,.26f,54),grass);
        Box(root,"Stream bank below the grass",new Vector3(50.15f,-.56f,23),new Vector3(.4f,1f,100),soil);
        var deck=Box(root,"East drawbridge deck",new Vector3(47,-.14f,-1),new Vector3(8,.28f,4.2f),oak,true);
        deck.gameObject.tag="Ground";
        for(int plank=0;plank<16;plank++)
            Box(root,"Drawbridge plank",new Vector3(43.25f+plank*.5f,.017f,-1),new Vector3(.48f,.035f,4.2f),oak);
        foreach(float z in new[]{-3.1f,1.1f})
            Box(root,"Drawbridge edge beam",new Vector3(47,.17f,z),new Vector3(8,.34f,.18f),oak,true);
        BuildPath(root,new Vector2(50.8f,-1),new Vector2(54,-1),new Vector2(58,-1),new Vector2(62,-1));
    }

    static Material SlateRoof()
    {
        const string asset=Art+"Courtyard slate courses.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
        if(texture==null)
        {
            const int size=128;
            texture=new Texture2D(size,size,TextureFormat.RGBA32,true);
            texture.wrapMode=TextureWrapMode.Repeat;
            texture.filterMode=FilterMode.Trilinear;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                int row=y/16, shifted=(x+row%2*8)%size;
                float shade=.82f+.055f*Mathf.Sin(shifted/16*17+row*13);
                if(y%16<2 || shifted%16==0)shade*=.66f;
                else if(y%16==2)shade*=1.12f;
                pixels[y*size+x]=new Color(shade,shade,shade);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture,asset);
        }
        var material=Mat("Courtyard slate roof",new Color(.36f,.45f,.52f));
        material.SetTexture("_BaseMap",texture);
        return material;
    }

    static Material TintedRoof(string name,Color tint)
    {
        var material=Clone(name,"Assets/Art/SceneStyle/Courtyard painted roof.mat");
        material.SetColor("_BaseColor",tint);
        return material;
    }

    static void House(Transform root,string name,Vector3 at,float width,float height,float depth,bool castle=false,float roofPitch=26f,Material roofFinish=null)
    {
        var house=new GameObject(name).transform;house.SetParent(root,false);house.localPosition=at;
        Box(house,name+" structure",new Vector3(0,height/2,0),new Vector3(width,height,depth),stone,true);
        if(castle)
        {
            CastleFacade(house,width,height,depth);
            int count=Mathf.Max(2,Mathf.FloorToInt(width/1.65f));
            for(int i=0;i<count;i++)Box(house,"Hall battlement",new Vector3(-width/2+(i+.5f)*width/count,height+.6f,-depth/2),new Vector3(.9f,1.2f,.9f),stone);
        }
        else
        {
            float run=depth*.5f+.3f;
            float radians=roofPitch*Mathf.Deg2Rad;
            float rise=run*Mathf.Tan(radians);
            foreach(float side in new[]{-1f,1f})
            {
                var slope=Box(house,"Painted roof slope",new Vector3(0,height+rise*.5f,side*run*.5f),
                    new Vector3(width+.6f,.18f,run/Mathf.Cos(radians)),roofFinish!=null?roofFinish:roof);
                slope.localRotation=Quaternion.Euler(side*roofPitch,0,0);
            }
            Box(house,"Roof ridge beam",new Vector3(0,height+rise,0),new Vector3(width+.64f,.18f,.19f),oak);
            BuildGable(house,width,depth,height,rise);
        }
        if(castle)return;
        foreach(float x in new[]{-width/2+.15f,width/2-.15f})Box(house,"Facade oak beam",new Vector3(x,height/2,-depth/2-.08f),new Vector3(.2f,height,.2f),oak);
        Box(house,"Facade cross beam",new Vector3(0,height*.68f,-depth/2-.08f),new Vector3(width,.2f,.2f),oak);
    }
    static void BuildGable(Transform house,float width,float depth,float height,float rise)
    {
        string asset=Art+house.name+" gable.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,asset);}
        mesh.Clear();
        mesh.vertices=new[]{new Vector3(-width/2,height,-depth/2),new Vector3(-width/2,height,depth/2),new Vector3(-width/2,height+rise,0),
            new Vector3(width/2,height,-depth/2),new Vector3(width/2,height,depth/2),new Vector3(width/2,height+rise,0)};
        mesh.triangles=new[]{0,1,2,3,5,4,0,2,5,0,5,3,1,4,5,1,5,2};
        mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var gable=new GameObject("Painted solid gable",typeof(MeshFilter),typeof(MeshRenderer));
        gable.transform.SetParent(house,false);
        gable.GetComponent<MeshFilter>().sharedMesh=mesh;
        gable.GetComponent<MeshRenderer>().sharedMaterial=stone;
    }

    static void CastleFacade(Transform house,float width,float height,float depth)
    {
        float front=-depth/2;
        var shadow=Mat("Courtyard window shadow",new Color(.105f,.15f,.155f));
        Box(house,"Stone foundation course",new Vector3(0,.25f,0),new Vector3(width+.35f,.5f,depth+.35f),stone);
        foreach(float y in new[]{height*.47f,height-.2f})
            Box(house,"Projecting stone cornice",new Vector3(0,y,0),new Vector3(width+.45f,.24f,depth+.45f),stone);
        foreach(float x in new[]{-width/2+.18f,width/2-.18f})
        {
            Box(house,"Castle corner buttress",new Vector3(x,height*.45f,front-.24f),new Vector3(.62f,height*.9f,.66f),stone);
            Box(house,"Buttress foot",new Vector3(x,.42f,front-.4f),new Vector3(.94f,.84f,.92f),stone);
        }
        int bays=Mathf.Max(1,Mathf.FloorToInt(width/3));
        foreach(float y in new[]{height*.63f,height*.84f})
        for(int i=0;i<bays;i++)
        {
            float x=-width/2+(i+.5f)*width/bays;
            Box(house,"Deep castle window",new Vector3(x,y,front-.035f),new Vector3(.82f,1.38f,.075f),shadow);
            foreach(float side in new[]{-.5f,.5f})
                Box(house,"Window stone reveal",new Vector3(x+side,y,front-.16f),new Vector3(.18f,1.65f,.36f),stone);
            foreach(float side in new[]{-.79f,.79f})
                Box(house,"Window stone sill",new Vector3(x,y+side,front-.19f),new Vector3(1.15f,.18f,.42f),stone);
            Box(house,"Slender stone mullion",new Vector3(x,y,front-.12f),new Vector3(.09f,1.4f,.16f),stone);
        }
    }

    static void Door(Transform root,Vector3 at,float width,float height)
    {
        Box(root,"Closed oak entrance",at+Vector3.up*(height/2),new Vector3(width,height,.2f),oak,true);
        foreach(float x in new[]{-width/2,width/2})Box(root,"Doorway jamb",at+new Vector3(x,height/2,-.05f),new Vector3(.3f,height+.3f,.35f),stone);
        Box(root,"Doorway lintel",at+new Vector3(0,height+.1f,0),new Vector3(width+.5f,.35f,.35f),stone);
    }
    static void Entrance(Transform root,Vector3 position,CourtyardEntrance.Destination destination)
    {
        var obj=new GameObject(destination+" courtyard entrance",typeof(SphereCollider),typeof(CourtyardEntrance));obj.transform.SetParent(root,false);obj.transform.position=position;
        var trigger=obj.GetComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.5f;obj.GetComponent<CourtyardEntrance>().destination=destination;
    }
    static void Garden(Transform root)
    {
        for(int i=0;i<3;i++)
        {
            Vector3 at=new Vector3(9+i*4.8f,.1f,4.5f);Box(root,"Garden soil bed",at,new Vector3(4.3f,.15f,3.3f),soil);
            foreach(float x in new[]{-2.2f,2.2f})Box(root,"Garden border",at+new Vector3(x,.1f,0),new Vector3(.13f,.3f,3.5f),oak,true);
            foreach(float z in new[]{-1.7f,1.7f})Box(root,"Garden border",at+new Vector3(0,.1f,z),new Vector3(4.5f,.3f,.13f),oak,true);
            var crop=Mat("Courtyard crop "+i,i==0?new Color(.43f,.62f,.19f):new Color(.23f,.42f,.12f));
            var carrot=Mat("Courtyard carrot orange",new Color(.86f,.31f,.08f));
            for(int j=0;j<12;j++)
            {
                var spot=at+new Vector3(-1.55f+j%4*1.02f,.18f,-1+j/4);
                Leaf(root,i==0?"Lettuce":i==1?"Carrot greens":"Bean leaves",spot+Vector3.up*(i==2?.55f:.13f),
                    i==0?new Vector3(.58f,.35f,.55f):new Vector3(.25f,.35f,.24f),crop);
                if(i==1)Leaf(root,"Carrot shoulders",spot,new Vector3(.17f,.19f,.17f),carrot);
            }
            if(i==2)
            {
                foreach(float x in new[]{-1.8f,1.8f})
                    Box(root,"Bean trellis post",at+new Vector3(x,.64f,0),new Vector3(.08f,1.25f,.08f),oak);
                Box(root,"Bean trellis crossbar",at+Vector3.up*1.2f,new Vector3(3.75f,.09f,.09f),oak);
            }
        }
    }
    static void GrassTufts(Transform root)
    {
        var v=new List<Vector3>();var t=new List<int>();var rng=new System.Random(913);
        for(int i=0;i<850;i++)
        {
            float x=(float)rng.NextDouble()*82-41,z=(float)rng.NextDouble()*32-15;
            if(z>-6f&&z<2.5f || z>4.5f || new[]{-34f,-21,12,25,37}.Any(p=>Mathf.Abs(x-p)<1.7f))continue;
            float h=.18f+(float)rng.NextDouble()*.28f;int n=v.Count;v.AddRange(new[]{new Vector3(x-.1f,.015f,z),new Vector3(x,h,z+.06f),new Vector3(x+.1f,.015f,z),new Vector3(x,.015f,z-.1f),new Vector3(x+.06f,h,z),new Vector3(x,.015f,z+.1f)});t.AddRange(new[]{n,n+1,n+2,n+3,n+4,n+5});
        }
        for(int i=0;i<v.Count;i++){var point=v[i];point.y+=GroundHeight(point.x,point.z);v[i]=point;}
        var mesh=new Mesh{name="Courtyard grass tufts"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string file=Art+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(file);if(old==null){AssetDatabase.CreateAsset(mesh,file);old=mesh;}else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
        var obj=new GameObject("Courtyard decorative grass",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);obj.GetComponent<MeshFilter>().sharedMesh=old;obj.GetComponent<Renderer>().sharedMaterial=grass;
    }
    static void Horizon()
    {
        var root=GameObject.Find("Castle horizon and golden fields");if(root==null)return;
        var material=Clone("Courtyard outer meadow cutout","Assets/Art/SceneStyle/Courtyard meadow cutout.mat");material.SetVector("_ClearArea",new Vector4(-45,-19,62,22));material.SetFloat("_SunResponse",1);
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())if(renderer.sharedMaterial!=null && renderer.sharedMaterial.shader.name=="Arkmon/Meadow")renderer.sharedMaterial=material;
        var morningWind=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/MeadowVegetation/Courtyard morning wind.mat");
        foreach(var vegetation in root.GetComponentsInChildren<MeadowVegetation>())
        {
            vegetation.SetClearArea(new Rect(-45,-19,107,41));
            if(morningWind!=null)
            {
                var settings=new SerializedObject(vegetation);settings.FindProperty("material").objectReferenceValue=morningWind;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        foreach(var item in root.GetComponentsInChildren<Transform>().ToArray())if(item!=null && (item.name=="Meadow tree"||item.name=="Meadow wildflowers"||item.name=="Meadow thistles") && new Rect(-45,-19,107,41).Contains(new Vector2(item.position.x,item.position.z)))Object.DestroyImmediate(item.gameObject);
    }
}
