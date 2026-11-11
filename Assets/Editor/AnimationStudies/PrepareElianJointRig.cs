using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class PrepareElianJointRig
{
    const string Folder="Assets/Animations/ElianJointRig";
    public const string ScenePath="Assets/Scenes/AnimationStudies/ElianJointPoseReview.unity";
    static readonly Dictionary<string,bool> readable=new Dictionary<string,bool>();
    [Serializable] sealed class Outline {public Vector2[] vertices;public int[] triangles;}
    [MenuItem("Arkmon/Animationsprobe/Gelenkfigur öffnen")]
    public static void Open()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(System.IO.File.Exists(ScenePath))EditorSceneManager.OpenScene(ScenePath);else Run();
    }
    public static void Run()
    {
        System.IO.Directory.CreateDirectory(Folder+"/Meshes");System.IO.Directory.CreateDirectory(Folder+"/Materials");System.IO.Directory.CreateDirectory(Folder+"/Poses");
        AssetDatabase.Refresh();
        GameObject actor=null;
        try
        {
            actor=new GameObject("Elian joint figure",typeof(ElianJointRig),typeof(SortingGroup),typeof(Animator));
            var rig=actor.GetComponent<ElianJointRig>();
            rig.pelvis=Bone("Pelvis",actor.transform,new Vector2(0,.62f));
            rig.torso=Bone("Chest",rig.pelvis,new Vector2(0,-.06f));
            var body=Resources.Load<Sprite>("Elian-Walk-Family");
            var torsoSource=PaperPlayerWalkRig.CreatePaintedMesh(body,"Body");
            var bodyMaterial=MaterialFor("Body",body.texture,Color.white);
            Mesh torsoMesh=Clip(torsoSource,.54f*body.rect.height/body.pixelsPerUnit,true,Vector3.zero);
            rig.head=Bone("Neck and head",rig.torso,new Vector2(0,.54f*body.rect.height/body.pixelsPerUnit));
            Mesh headMesh=Clip(torsoSource,.50f*body.rect.height/body.pixelsPerUnit,false,rig.head.localPosition);
            rig.garment=Garment(rig.torso,torsoMesh,bodyMaterial);
            StaticArt("Head artwork",rig.head,SaveMesh(headMesh,"Head"),bodyMaterial,10);
            UnityEngine.Object.DestroyImmediate(torsoSource);
            rig.rightLeg=Leg("Right",rig.pelvis,new Vector2(-.11f,0),Resources.Load<Sprite>("Elian-Walk-NearLeg"),2,Color.white);
            rig.leftLeg=Leg("Left",rig.pelvis,new Vector2(.11f,0),Resources.Load<Sprite>("Elian-Walk-FarLeg"),1,new Color(.90f,.90f,.90f,1));
            PelvisCover(rig.pelvis,Resources.Load<Sprite>("Elian-Walk-NearLeg"));
            Vector2 size=body.rect.size/body.pixelsPerUnit;
            var arm=Resources.Load<Sprite>("Garden/UpperArm");
            var rect=arm.rect;float height=Mathf.Round(rect.height/.88f);rect.y-=height-rect.height;rect.height=height;
            rig.rightArm=Chain("Right arm",rig.torso,new Vector2(-.22f*size.x,.44f*size.y),arm.texture,rect,arm.pixelsPerUnit*.65f,
                new Vector2(.52f,.90f),new Vector2(.50f,.50f),new Vector2(.50f,.08f),4,Color.white,false);
            rig.leftArm=Chain("Left arm",rig.torso,new Vector2(.22f*size.x,.44f*size.y),arm.texture,rect,arm.pixelsPerUnit*.65f,
                new Vector2(.52f,.90f),new Vector2(.50f,.50f),new Vector2(.50f,.08f),5,Color.white,false);
            rig.rightHand=Bone("Right hand",rig.rightArm.tip,Vector2.zero);rig.leftHand=Bone("Left hand",rig.leftArm.tip,Vector2.zero);
            var right=StaticArt("Right hand artwork",rig.rightHand,null,null,8);rig.rightHandArt=right.GetComponent<MeshFilter>();rig.rightHandSurface=right.GetComponent<MeshRenderer>();
            var left=StaticArt("Left hand artwork",rig.leftHand,null,null,8);rig.leftHandArt=left.GetComponent<MeshFilter>();rig.leftHandSurface=left.GetComponent<MeshRenderer>();
            rig.rightGrip=Hand("RightGrip",new Vector2(.5f,.60f));
            rig.leftRelaxed=Hand("LeftSupport",new Vector2(.48f,.43f));
            // Left palm cups upward in front of its forearm, with the thumb opposite the right grip.
            rig.leftSupport=Hand("LeftSupport",new Vector2(.48f,.43f));
            var source=Resources.Load<Sprite>("StableCare/Bucket");
            var bucketSprite=CourtyardCharacterArt.Sprite(source.texture,source.rect,new Vector2(.5f,.94f),source.pixelsPerUnit,Folder+"/Bucket.asset","Joint review bucket");
            var item=new GameObject("Rigid bucket",typeof(SpriteRenderer));item.transform.SetParent(actor.transform,false);
            rig.bucket=item.GetComponent<SpriteRenderer>();rig.bucket.sprite=bucketSprite;rig.bucket.sortingOrder=7;
            var contents=new Material(Resources.Load<Shader>("CourtyardBucketContents"));contents.SetFloat("_Filled",1);
            rig.bucket.sharedMaterial=Save(contents,Folder+"/Materials/Bucket.mat");
            rig.bucketBase=new Vector2(.08f,bucketSprite.bounds.min.y+.012f);
            var squat=Pose("01-Hocke","Hocke",new Vector2(-.28f,.425f),-28,-28,new Vector2(.16f,-bucketSprite.bounds.min.y+.008f),220,false,new Vector2(-.12f,.49f),180);
            var carry=Pose("02-Tragen","Tragen",new Vector2(0,.68f),0,0,new Vector2(-.39f,.73f),180,false,new Vector2(.28f,.72f),-120);
            var lift=Pose("03-Anheben","Beidhändig anheben",new Vector2(0,.67f),-25,-25,new Vector2(.36f,1.20f),300,true,Vector2.zero,-65);
            rig.ApplyPose(carry);
            PrefabUtility.SaveAsPrefabAsset(actor,Folder+"/ElianJointFigure.prefab");
            UnityEngine.Object.DestroyImmediate(actor);actor=null;
            CreateScene(new[]{squat,carry,lift});
            AssetDatabase.SaveAssets();
            Debug.Log("ELIAN_JOINT_RIG_READY native skinned meshes; fixed chains; three static poses; no transitions/build");
        }
        finally
        {
            if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);
            foreach(var entry in readable){var importer=(TextureImporter)AssetImporter.GetAtPath(entry.Key);importer.isReadable=entry.Value;importer.SaveAndReimport();}
            readable.Clear();
        }
    }
    static Transform Bone(string name,Transform parent,Vector2 position)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;
    }
    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old==null){AssetDatabase.CreateAsset(value,path);return value;}
        EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(old);return old;
    }
    static Mesh SaveMesh(Mesh mesh,string name){mesh.name=name;return Save(mesh,Folder+"/Meshes/"+name+".asset");}
    static Material MaterialFor(string name,Texture texture,Color tint)
    {
        var mat=new Material(Resources.Load<Shader>("PaintedArm"));mat.name=name;mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",tint);
        return Save(mat,Folder+"/Materials/"+name+".mat");
    }
    static GameObject StaticArt(string name,Transform parent,Mesh mesh,Material mat,int order)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;
        renderer.sortingOrder=order;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go;
    }
    static void PelvisCover(Transform parent,Sprite source)
    {
        // Rounded, textured pelvis coverage behind the tunic and thigh seams.
        const int count=48;var vertices=new Vector3[count+1];var uv=new Vector2[count+1];var indices=new int[count*3];
        Vector2 UV(float x,float y)=>new Vector2((source.rect.x+x*source.rect.width)/source.texture.width,(source.rect.y+y*source.rect.height)/source.texture.height);
        vertices[0]=new Vector3(0,.07f,0);uv[0]=UV(.365f,.87f);
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count;vertices[i+1]=new Vector3(Mathf.Cos(a)*.20f,.07f+Mathf.Sin(a)*.13f,0);
            uv[i+1]=UV(.365f+Mathf.Cos(a)*.12f,.87f+Mathf.Sin(a)*.08f);
            indices[i*3]=0;indices[i*3+1]=i+1;indices[i*3+2]=(i+1)%count+1;
        }
        var mesh=new Mesh{name="Pelvis coverage"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.colors=Enumerable.Repeat(Color.white,vertices.Length).ToArray();mesh.RecalculateBounds();
        StaticArt("Pelvis clothing overlap",parent,SaveMesh(mesh,"Pelvis-cover"),MaterialFor("Pelvis",source.texture,Color.white),2);
    }
    static SkinnedMeshRenderer Garment(Transform parent,Mesh mesh,Material material)
    {
        var vertices=mesh.vertices;var delta=new Vector3[vertices.Length];
        for(int i=0;i<vertices.Length;i++)delta[i]=Vector3.up*(.10f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.32f,vertices[i].y))));
        mesh.AddBlendShapeFrame("Squat tunic fold",100,delta,null,null);
        mesh.bindposes=new[]{Matrix4x4.identity};mesh.boneWeights=Enumerable.Repeat(new BoneWeight{boneIndex0=0,weight0=1},vertices.Length).ToArray();
        var go=new GameObject("Torso and tunic fold",typeof(SkinnedMeshRenderer));go.transform.SetParent(parent,false);
        var renderer=go.GetComponent<SkinnedMeshRenderer>();renderer.sharedMesh=SaveMesh(mesh,"Torso");renderer.sharedMaterial=material;
        renderer.bones=new[]{parent};renderer.rootBone=parent;renderer.updateWhenOffscreen=true;renderer.localBounds=new Bounds(Vector3.zero,Vector3.one*5);
        renderer.sortingOrder=3;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
    }
    static ElianJointRig.HandDrawing Hand(string name,Vector2 contact)
    {
        var sprite=Resources.Load<Sprite>("ElianHands/"+name);
        var data=JsonUtility.FromJson<Outline>(Resources.Load<TextAsset>("ElianHands/"+name+"-outline").text);
        var mesh=new Mesh{name=name};mesh.vertices=data.vertices.Select(p=>(Vector3)p).ToArray();mesh.triangles=data.triangles;
        mesh.uv=data.vertices.Select(p=>new Vector2((p.x*sprite.pixelsPerUnit+sprite.pivot.x+sprite.rect.x)/sprite.texture.width,
            (p.y*sprite.pixelsPerUnit+sprite.pivot.y+sprite.rect.y)/sprite.texture.height)).ToArray();
        mesh.colors=Enumerable.Repeat(Color.white,data.vertices.Length).ToArray();mesh.RecalculateBounds();
        return new ElianJointRig.HandDrawing{mesh=SaveMesh(mesh,name),material=MaterialFor(name,sprite.texture,Color.white),palm=(contact*sprite.rect.size-sprite.pivot)/sprite.pixelsPerUnit};
    }
    static ElianJointRig.Chain Leg(string name,Transform parent,Vector2 at,Sprite source,int order,Color color)
        =>Chain(name+" leg",parent,at,source.texture,source.rect,source.pixelsPerUnit,
            new Vector2(.27f,.96f),new Vector2(.31f,.61f),new Vector2(.37f,.22f),order,color,true);
    static ElianJointRig.Chain Chain(string name,Transform parent,Vector2 at,Texture2D texture,Rect rect,float ppu,
        Vector2 rootPoint,Vector2 jointPoint,Vector2 tipPoint,int order,Color color,bool foot)
    {
        var chain=new ElianJointRig.Chain();chain.first=Bone(name+" / "+(foot?"Hip":"Shoulder"),parent,at);
        Vector2 Unit(Vector2 n)=>Vector2.Scale(Vector2.Scale(n-rootPoint,rect.size),new Vector2(foot?1:.68f,1))/ppu;
        chain.bindFirst=Unit(jointPoint);chain.bindSecond=Unit(tipPoint)-Unit(jointPoint);
        chain.middle=Bone(foot?"Knee":"Elbow",chain.first,chain.bindFirst);chain.tip=Bone(foot?"Ankle and foot":"Wrist",chain.middle,chain.bindSecond);
        bool[] mask=MainShape(texture,rect);int width=(int)rect.width,height=(int)rect.height;
        int bottom=height-1,top=0;for(int i=0;i<mask.Length;i++)if(mask[i]){bottom=Math.Min(bottom,i/width);top=Math.Max(top,i/width);}
        chain.soleLift=(tipPoint.y*height-bottom+.5f)/ppu;
        const int rows=64,cols=16;var vertices=new Vector3[(rows+1)*(cols+1)];var uv=new Vector2[vertices.Length];
        var weights=new BoneWeight[vertices.Length];var colors=new Color[vertices.Length];var indices=new List<int>();
        for(int y=0;y<=rows;y++)
        {
            float py=Mathf.Lerp(bottom,top,y/(float)rows);int iy=Mathf.Clamp(Mathf.RoundToInt(py),0,height-1),left=width-1,right=0;
            for(int x=0;x<width;x++)if(mask[iy*width+x]){left=Math.Min(left,x);right=Math.Max(right,x);}
            for(int x=0;x<=cols;x++)
            {
                int k=y*(cols+1)+x;float px=Mathf.Lerp(left-.5f,right+.5f,x/(float)cols),n=py/height;
                vertices[k]=Vector2.Scale(new Vector2(px,py)-rootPoint*rect.size,new Vector2(foot?1:.68f,1))/ppu;
                uv[k]=new Vector2((rect.x+px)/texture.width,(rect.y+py)/texture.height);
                float w0=Mathf.SmoothStep(0,1,Mathf.InverseLerp(jointPoint.y-.06f,jointPoint.y+.06f,n));
                float w2=foot?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.50f,n)):0;
                weights[k]=new BoneWeight{boneIndex0=0,weight0=w0,boneIndex1=1,weight1=(1-w0)*(1-w2),boneIndex2=2,weight2=(1-w0)*w2};
                colors[k]=new Color(1,1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(foot?.88f:.86f,.98f,n)));
                if(y==rows||x==cols)continue;
                indices.AddRange(new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2});
            }
        }
        var go=new GameObject(name+" native skin",typeof(SkinnedMeshRenderer));go.transform.SetParent(chain.first,false);
        var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=indices.ToArray();mesh.boneWeights=weights;
        var bones=new[]{chain.first,chain.middle,chain.tip};mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*go.transform.localToWorldMatrix).ToArray();mesh.RecalculateBounds();
        chain.surface=go.GetComponent<SkinnedMeshRenderer>();chain.surface.sharedMesh=SaveMesh(mesh,name.Replace(" ","-"));chain.surface.bones=bones;chain.surface.rootBone=chain.first;
        chain.surface.sharedMaterial=MaterialFor(name.Replace(" ","-"),texture,color);chain.surface.sortingOrder=order;
        chain.surface.updateWhenOffscreen=true;chain.surface.localBounds=new Bounds(Vector3.zero,Vector3.one*5);chain.surface.quality=SkinQuality.Bone4;
        chain.surface.shadowCastingMode=ShadowCastingMode.Off;chain.surface.receiveShadows=false;
        Debug.Log($"JOINT_CHAIN {name} lengths={chain.FirstLength:F4}/{chain.SecondLength:F4} sole={chain.soleLift:F4}");
        return chain;
    }
    static bool[] MainShape(Texture2D texture,Rect rect)
    {
        string path=AssetDatabase.GetAssetPath(texture);
        if(!readable.ContainsKey(path)){var importer=(TextureImporter)AssetImporter.GetAtPath(path);readable.Add(path,importer.isReadable);importer.isReadable=true;importer.SaveAndReimport();}
        var pixels=AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetPixels32();int w=(int)rect.width,h=(int)rect.height,tw=texture.width;
        bool Opaque(int n)=>pixels[((int)rect.y+n/w)*tw+(int)rect.x+n%w].a>220;
        var seen=new bool[w*h];var largest=new List<int>();
        for(int n=0;n<seen.Length;n++)
        {
            if(seen[n]||!Opaque(n))continue;var part=new List<int>{n};seen[n]=true;
            for(int k=0;k<part.Count;k++)
            {
                int p=part[k],x=p%w,y=p/w;
                foreach(var d in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                {
                    int nx=x+d.x,ny=y+d.y,next=ny*w+nx;
                    if(nx<0||nx>=w||ny<0||ny>=h||seen[next]||!Opaque(next))continue;
                    seen[next]=true;part.Add(next);
                }
            }
            if(part.Count>largest.Count)largest=part;
        }
        if(largest.Count<100)throw new InvalidOperationException("Missing artwork "+texture.name);
        var mask=new bool[w*h];foreach(int n in largest)mask[n]=true;return mask;
    }
    static Mesh Clip(Mesh source,float cut,bool below,Vector3 origin)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();var v=source.vertices;var t=source.triangles;var tex=source.uv;
        for(int i=0;i<t.Length;i+=3)
        {
            var polygon=new List<(Vector3 p,Vector2 uv)>();for(int k=0;k<3;k++)polygon.Add((v[t[i+k]],tex[t[i+k]]));
            var output=new List<(Vector3 p,Vector2 uv)>();
            for(int k=0;k<polygon.Count;k++)
            {
                var a=polygon[k];var b=polygon[(k+1)%polygon.Count];bool ain=below?a.p.y<=cut:a.p.y>=cut,bin=below?b.p.y<=cut:b.p.y>=cut;
                if(ain)output.Add(a);
                if(ain!=bin){float f=(cut-a.p.y)/(b.p.y-a.p.y);output.Add((Vector3.Lerp(a.p,b.p,f),Vector2.Lerp(a.uv,b.uv,f)));}
            }
            int start=vertices.Count;foreach(var point in output){vertices.Add(point.p-origin);uv.Add(point.uv);}
            for(int k=1;k<output.Count-1;k++)indices.AddRange(new[]{start,start+k,start+k+1});
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.colors=Enumerable.Repeat(Color.white,vertices.Count).ToArray();mesh.RecalculateBounds();return mesh;
    }
    static ElianJointPose Pose(string file,string label,Vector2 pelvis,float torso,float head,Vector2 handle,float rightAngle,bool support,Vector2 leftPalm,float leftAngle)
    {
        string path=Folder+"/Poses/"+file+".asset";var value=AssetDatabase.LoadAssetAtPath<ElianJointPose>(path);
        if(value==null){value=ScriptableObject.CreateInstance<ElianJointPose>();AssetDatabase.CreateAsset(value,path);}
        value.label=label;value.tunicFold=file.StartsWith("01")?100:0;value.pelvis=pelvis;value.torsoDegrees=torso;value.headDegrees=head;value.bucketHandle=handle;value.rightHandDegrees=rightAngle;
        value.supportsBucket=support;value.leftPalm=leftPalm;value.leftHandDegrees=leftAngle;value.rightSole=new Vector2(-.23f,0);value.leftSole=new Vector2(.10f,0);EditorUtility.SetDirty(value);return value;
    }
    static void CreateScene(ElianJointPose[] poses)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Elian joint pose review",typeof(ElianJointPoseReview));var review=root.GetComponent<ElianJointPoseReview>();
        var camera=new GameObject("Review camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";
        camera.orthographic=true;camera.orthographicSize=2.05f;camera.transform.position=new Vector3(0,1.1f,-10);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.87f,.85f,.80f);review.reviewCamera=camera;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Quad);ground.name="Quiet floor";UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
        ground.transform.position=new Vector3(0,-.5f,1);ground.transform.localScale=new Vector3(20,1,1);ground.GetComponent<MeshRenderer>().sharedMaterial=MaterialFor("Review-floor",Texture2D.whiteTexture,new Color(.74f,.73f,.67f));ground.GetComponent<MeshRenderer>().sortingOrder=-10;
        review.figures=new ElianJointRig[3];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/ElianJointFigure.prefab");
        for(int i=0;i<3;i++)
        {
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);actor.name="Elian / "+poses[i].label;actor.transform.position=new Vector3((i-1)*2.5f,0,0);
            var rig=actor.GetComponent<ElianJointRig>();rig.ApplyPose(poses[i]);review.figures[i]=rig;
            var text=new GameObject("Pose label "+i,typeof(TextMeshPro));text.transform.position=new Vector3((i-1)*2.5f-.8f,2.4f,-1);
            var label=text.GetComponent<TextMeshPro>();label.font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");label.fontSize=1.6f;label.color=new Color(.22f,.25f,.23f);
            label.text=(i+1)+" · "+(i==2?"Anheben":poses[i].label);label.rectTransform.sizeDelta=new Vector2(2.5f,.3f);label.rectTransform.pivot=new Vector2(0,1);label.GetComponent<MeshRenderer>().sortingOrder=50;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rig);
        }
        review.Select(-1);EditorSceneManager.SaveScene(scene,ScenePath);
    }
}