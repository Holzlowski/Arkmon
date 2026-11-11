using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardFifthFeedbackTests
{
    const BindingFlags H=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object S(string t,string p)=>T(t).GetProperty(p).GetValue(null);
    static object Call(object o,string m,params object[] a)=>o.GetType().GetMethod(m).Invoke(o,a);
    static GameObject Player=>GameObject.FindWithTag("Player");
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static Component C(string n,string t)=>GameObject.Find(n).GetComponent(T(t));
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{true});
    static IEnumerator View(string n,Vector3 at,float d=7)=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("CloseUp",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{n,(Vector3?)at,d});
    static void Move(Component c){var p=(Vector3)Call(c,"GetTransform")+Vector3.up;Player.transform.position=p;Player.GetComponent<Rigidbody>().position=p;Physics.SyncTransforms();}
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{n});
    static bool Contains(MeshFilter filter,Vector3 world)
    {
        Vector2 p=filter.transform.InverseTransformPoint(world);var v=filter.sharedMesh.vertices;var t=filter.sharedMesh.triangles;
        float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        for(int i=0;i<t.Length;i+=3)
        {
            Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
            float x=Cross(b-a,p-a),y=Cross(c-b,p-b),z=Cross(a-c,p-c);
            if((x>=-.00001f && y>=-.00001f && z>=-.00001f) || (x<=.00001f && y<=.00001f && z<=.00001f))return true;
        }
        return false;
    }
    static void CheckCarry()
    {
        var root=GameObject.Find("Elian garden paper rig")?.transform;if(root==null)return;
        foreach(string n in new[]{"Near gripping hand","Far gripping hand","Near garden leg","Far garden leg"})
            Assert.That(root.Find(n).localScale,Is.EqualTo(Vector3.one),n+" must keep the size of its painted source.");
        var visual=Player.GetComponent(T("PaperCharacterVisual"));
        foreach(string side in new[]{"Near","Far"})
        {
            var arm=root.Find(side+" gripping hand");var sprite=arm.GetComponent<SpriteRenderer>().sprite;
            Vector2 sourceElbow=(new Vector2(.50f,.38f)*sprite.rect.size-sprite.pivot)/sprite.pixelsPerUnit;
            Vector2 sourceWrist=(new Vector2(.50f,.08f)*sprite.rect.size-sprite.pivot)/sprite.pixelsPerUnit;
            var elbow=root.Find(side+" elbow joint");var wrist=root.Find(side+" wrist joint");
            Assert.That(Vector3.Distance(arm.localPosition,elbow.localPosition),Is.EqualTo(sourceElbow.magnitude).Within(.0002f),"The upper arm must not resize.");
            Assert.That(Vector3.Distance(elbow.localPosition,wrist.localPosition),Is.EqualTo((sourceWrist-sourceElbow).magnitude).Within(.0002f),"The forearm must not resize.");
            var palm=(Vector3)Call(visual,side=="Near"?"get_GardenHandPosition":"get_GardenSupportHandPosition");
            var palmAxis=root.InverseTransformPoint(palm)-wrist.localPosition;var foreAxis=wrist.localPosition-elbow.localPosition;
            Assert.That(Vector2.Angle(palmAxis,foreAxis),Is.LessThan(40.1f),"The hand must not bend backwards at the wrist.");
        }
        Assert.That(root.Find("Near wrist joint").GetComponentInChildren<SpriteRenderer>().sprite.name,Is.EqualTo("Elian-LeftGrip"));
        Assert.That(root.Find("Far wrist joint").GetComponentInChildren<SpriteRenderer>().sprite.name,Is.EqualTo("Elian-RightGrip"));
        var item=root.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(s=>s.name=="Watering can model" || s.name=="Stable water bucket");
        if(item!=null)Assert.That(Vector3.Distance((Vector3)Call(visual,"get_GardenHandPosition"),item.transform.position),Is.LessThan(.005f));
        float assisting=(float)visual.GetType().GetField("gardenSupport",H).GetValue(visual);
        float tilt=(float)visual.GetType().GetField("gardenTilt",H).GetValue(visual);
        if(item!=null && item.name=="Stable water bucket" && assisting>.99f && tilt>10)
            Assert.That(Vector3.Distance((Vector3)Call(visual,"get_GardenSupportHandPosition"),item.transform.TransformPoint(new Vector3(.15f,-.50f,0))),Is.LessThan(.03f),"The second hand stays on the bucket base while lifting and tipping.");
        Assert.That(root.Find("Garden torso").GetComponent<SpriteRenderer>().sprite.name,Does.StartWith("Elian-Walk-"),"Holding a tool keeps the same head and garment as walking.");
    }
    static IEnumerator WaitAction(string prefix,bool pictures=false)
    {
        float end=Time.realtimeSinceStartup+7;int frames=0,shots=0;float next=Time.time+.25f;
        while(((bool)S("CourtyardWaterTask","IsBusy") || (bool)S("CourtyardGarden","IsBusy")) && Time.realtimeSinceStartup<end)
        {
            yield return null;CheckCarry();frames++;
            if(pictures && Time.time>=next && shots<5){yield return View(prefix+"-"+shots,Player.transform.position+Vector3.up*.2f,6);next=Time.time+.55f;shots++;}
        }
        Assert.That(S("CourtyardWaterTask","IsBusy"),Is.False);Assert.That(S("CourtyardGarden","IsBusy"),Is.False);Assert.That(frames,Is.GreaterThan(3));yield return null;
    }
    static Color Pixel(Texture2D texture,Vector2 at)
    {
        var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);var before=RenderTexture.active;
        Graphics.Blit(texture,target);RenderTexture.active=target;
        var sample=new Texture2D(1,1,TextureFormat.RGBA32,false);sample.ReadPixels(new Rect(Mathf.Floor(at.x),Mathf.Floor(at.y),1,1),0,0);sample.Apply();
        Color color=sample.GetPixel(0,0);UnityEngine.Object.Destroy(sample);RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);return color;
    }
    [UnityTest] public IEnumerator PaintedHandleAndFingerContactsAreOpaqueAndArmsDrawInFront()
    {
        yield return Load();var asset=Resources.Load<Sprite>("Garden/Can");var bar=Pixel(asset.texture,asset.rect.position+asset.pivot);
        Assert.That(bar.a,Is.GreaterThan(.90f));Assert.That(bar.r,Is.GreaterThan(bar.b*1.15f),"The tool anchor must sit on the painted brown handle, not an empty point below it.");
        foreach(string side in new[]{"LeftGrip","RightGrip"})
        {
            var hand=Resources.Load<Sprite>("ElianHands/"+side);var skin=Pixel(hand.texture,hand.rect.position+hand.rect.size*new Vector2(.5f,.60f));
            Assert.That(skin.a,Is.GreaterThan(.90f),"The contact belongs inside the drawn fingers.");
        }
        var can=C("Courtyard watering can","CourtyardGarden");((Behaviour)can).enabled=false;
        var model=GameObject.Find("Watering can model").GetComponent<SpriteRenderer>();
        var visual=Player.GetComponent(T("PaperCharacterVisual"));
        Call(visual,"SetGardenPose",model,true,0f,0f,1f,null,null,0f);yield return null;
        var root=GameObject.Find("Elian garden paper rig").transform;var body=root.Find("Garden torso").GetComponentInChildren<MeshRenderer>();
        var upper=root.Find("Near gripping hand").GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Near painted upper arm");
        var fingers=root.Find("Near wrist joint").GetComponentInChildren<MeshRenderer>();
        Assert.That(upper.sortingOrder,Is.GreaterThan(body.sortingOrder));Assert.That(fingers.sortingOrder,Is.GreaterThan(model.sortingOrder));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator StaticGripAndTwoHandPosesUseDistinctRigidHands()
    {
        yield return Load();var can=C("Courtyard watering can","CourtyardGarden");Call(can,"InteractWithPlayer");yield return WaitAction("static-pickup");
        var visual=Player.GetComponent(T("PaperCharacterVisual"));((Behaviour)can).enabled=false;
        var bucket=C("Stable bucket station","CourtyardWaterTask");((Behaviour)bucket).enabled=false;
        var handCan=GameObject.Find("Watering can model").GetComponent<SpriteRenderer>();handCan.enabled=true;
        foreach(int direction in new[]{1,-1})
        {
            Call(visual,"FaceWorldDirection",Camera.main.transform.right*direction);
            Call(visual,"SetGardenPose",handCan,true,0f,0f,1f,null,null,0f);yield return null;CheckCarry();
            yield return View("hands-static-can-"+direction,Player.transform.position+Vector3.up*.2f,5.5f);
        }
        yield return Load();T("CourtyardWaterTask").GetMethod("OfferHelp").Invoke(null,null);
        bucket=C("Stable bucket station","CourtyardWaterTask");Move(bucket);Call(bucket,"InteractWithPlayer");yield return WaitAction("static-bucket-pickup");
        visual=Player.GetComponent(T("PaperCharacterVisual"));((Behaviour)bucket).enabled=false;
        var waterBucket=GameObject.Find("Stable water bucket").GetComponent<SpriteRenderer>();waterBucket.sharedMaterial.SetFloat("_Filled",1);
        Call(visual,"FaceWorldDirection",Camera.main.transform.right);
        foreach(float tilt in new[]{0f,35f,70f})
        {
            Call(visual,"SetGardenPose",waterBucket,true,.65f*(1-tilt/70),tilt,1f,null,null,1f);yield return null;CheckCarry();
            yield return View("hands-static-two-hand-"+tilt,Player.transform.position+Vector3.up*.2f,5.5f);
        }
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Wake_RevealAndFirstStepsKeepTheSameConnectedFigure()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("TowerRoom");float end=Time.realtimeSinceStartup+12;int revealed=0,stepping=0,shots=0;float next=0;
        do
        {
            yield return null;if(Player==null)continue;
            var visual=Player.GetComponent(T("PaperCharacterVisual"));var wake=(float)visual.GetType().GetField("wakeProgress",H).GetValue(visual);
            var hidden=(bool)visual.GetType().GetField("presentationHidden",H).GetValue(visual);
            if(hidden)continue;
            var rig=GameObject.Find("Elian paper walking rig")?.transform;
            Assert.That(rig,Is.Not.Null,"The same puppet must already be present during the reveal, not swapped in for the first step.");
            var original=(SpriteRenderer)visual.GetType().GetField("spriteRenderer",H).GetValue(visual);Assert.That(original.forceRenderingOff,Is.True);
            if(wake>=0)revealed++;else stepping++;
            var body=rig.Find("Stable upper body");var bodyMesh=body.GetComponentInChildren<MeshFilter>();
            foreach(string armName in new[]{"Near arm","Far arm"})
            {
                var arm=rig.Find(armName);Assert.That(arm.localScale,Is.EqualTo(Vector3.one));
                Assert.That(Contains(arm.GetComponentInChildren<MeshFilter>(),arm.position),Is.True,"The shoulder pivot belongs inside its painted sleeve: "+armName);
                Assert.That(Contains(bodyMesh,arm.position),Is.True,"The torso overlaps the moving shoulder: "+armName);
            }
            foreach(string legName in new[]{"Near leg","Far leg"})
            {
                var leg=rig.Find(legName);Assert.That(leg.localScale,Is.EqualTo(Vector3.one));
                Assert.That(leg.GetComponent<SpriteRenderer>().bounds.max.y-body.position.y,Is.GreaterThan(.025f),"The upper leg stays inside the tunic throughout the first step.");
            }
            if(Time.time>=next && shots<7)
            {
                var camera=Camera.main;var position=camera.transform.position;float fov=camera.fieldOfView;
                camera.transform.position=Player.transform.position+Vector3.up*.1f-camera.transform.forward*5.5f;camera.fieldOfView=35;
                Capture("fifth-wake-"+shots);camera.transform.position=position;camera.fieldOfView=fov;
                next=Time.time+.16f;shots++;
            }
        }while(!(bool)S("DemoOpening","HasAwakened") && Time.realtimeSinceStartup<end);
        Assert.That(revealed,Is.GreaterThan(2));Assert.That(stepping,Is.GreaterThan(2));Assert.That(S("DemoOpening","HasAwakened"),Is.True);
        Call(Dialogue,"CloseDialogue");LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Can_FillAndWaterKeepTheOrdinaryFigure()
    {
        yield return Load();var can=C("Courtyard watering can","CourtyardGarden");Call(can,"InteractWithPlayer");yield return WaitAction("fifth-can-pickup",true);
        var visual=Player.GetComponent(T("PaperCharacterVisual"));
        foreach(int side in new[]{-1,1}){Call(visual,"FaceWorldDirection",Camera.main.transform.right*side);yield return null;Assert.That(GameObject.Find("Elian garden paper rig"),Is.Null);yield return View("fifth-can-carry-"+side,Player.transform.position+Vector3.up*.15f,6);}
        var well=C("Courtyard draw well","CourtyardWell");Move(well);Call(well,"InteractWithPlayer");yield return WaitAction("fifth-can-well",true);
        Assert.That(S("CourtyardGarden","WaterCharges"),Is.EqualTo(3));
        var bed=UnityEngine.Object.FindObjectsByType(T("CourtyardGarden"),FindObjectsSortMode.None).Cast<Component>().First(c=>c.name=="Garden watering point");
        Move(bed);Call(bed,"InteractWithPlayer");yield return WaitAction("fifth-can-pour",true);
        Assert.That(S("CourtyardGarden","WaterCharges"),Is.EqualTo(2));LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator ClearingToolPoseCannotDrawAnotherBodyOverTheWalkingPuppet()
    {
        yield return Load();var visual=Player.GetComponent(T("PaperCharacterVisual"));
        var original=(SpriteRenderer)visual.GetType().GetField("spriteRenderer",H).GetValue(visual);
        Assert.That(GameObject.Find("Elian paper walking rig"),Is.Not.Null);
        visual.GetType().GetMethod("SetGardenPose").Invoke(visual,new object[]{null,false,0f,0f,1f,null,null,0f});
        Assert.That(original.forceRenderingOff,Is.True,"A late station update must respect the active puppet.");
        yield return null;Assert.That(original.forceRenderingOff,Is.True);LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Bucket_FillAndTroughKeepFixedArmsAndHandContact()
    {
        yield return Load();T("CourtyardWaterTask").GetMethod("OfferHelp").Invoke(null,null);
        var b=C("Stable bucket station","CourtyardWaterTask");Move(b);Call(b,"InteractWithPlayer");yield return WaitAction("fifth-bucket-pickup",true);
        var well=C("Courtyard draw well","CourtyardWell");Move(well);Call(well,"InteractWithPlayer");yield return WaitAction("fifth-bucket-well",true);
        Assert.That(S("CourtyardWaterTask","BucketFull"),Is.True);
        var trough=C("Horse water delivery","CourtyardWaterTask");Move(trough);Call(trough,"InteractWithPlayer");yield return WaitAction("fifth-bucket-trough",true);
        Assert.That(S("CourtyardWaterTask","Completed"),Is.True);Assert.That(S("CourtyardWaterTask","BucketFull"),Is.False);LogAssert.NoUnexpectedReceived();
    }
}
