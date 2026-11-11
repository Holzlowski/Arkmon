using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameIntroTests
{
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Progress(string name)=>T("DemoOpening").GetProperty(name).GetValue(null);
    static object Intro(string name)=>T("IntroChapter").GetProperty(name).GetValue(null);
    static object Dialogue=>T("DialogueBoxController").GetField("instance").GetValue(null);
    static void Close()=>Dialogue.GetType().GetMethod("CloseDialogue").Invoke(Dialogue,null);
    static void Capture(string name)
    {
        Camera camera=Camera.main;
        if(camera==null)return;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
        var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;float previousAspect=camera.aspect;
        var target=new RenderTexture(1920,1080,24);target.Create();Texture2D picture=null;
        try
        {
            camera.targetTexture=target;camera.aspect=1920f/1080;
            foreach(var canvas in canvases)if(canvas.renderMode==RenderMode.ScreenSpaceOverlay)
            {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.15f;}
            Canvas.ForceUpdateCanvases();
            var requestType=T("UnityEngine.Rendering.Universal.UniversalRenderPipeline+SingleCameraRequest");
            var request=Activator.CreateInstance(requestType);requestType.GetField("destination").SetValue(request,target);
            typeof(UnityEngine.Rendering.RenderPipeline).GetMethods(BindingFlags.Public|BindingFlags.Static)
                .First(m=>m.Name=="SubmitRenderRequest"&&m.IsGenericMethodDefinition).MakeGenericMethod(requestType).Invoke(null,new object[]{camera,request});
            RenderTexture.active=target;picture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            picture.ReadPixels(new Rect(0,0,1920,1080),0,0);picture.Apply();
            string folder=Environment.GetEnvironmentVariable("ARKMON_REVIEW_OUTPUT")??System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","Builds"));
            System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),picture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previousTarget;camera.aspect=previousAspect;RenderTexture.active=previousActive;
            for(int i=0;i<canvases.Length;i++)if(canvases[i]!=null){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
            if(picture!=null)UnityEngine.Object.Destroy(picture);target.Release();UnityEngine.Object.Destroy(target);
        }
    }
    [UnityTest] public IEnumerator FamilyOutfitAllowsExitWithoutAwardingMantleOrVessel()
    {
        T("DemoOpening").GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        yield return SceneManager.LoadSceneAsync("TowerRoom");
        float deadline=Time.realtimeSinceStartup+8;
        while(!(bool)Progress("HasAwakened") && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Progress("HasAwakened"),Is.True);Assert.That(Intro("Active"),Is.True);
        Assert.That(GameObject.Find("Player lamp pickup"),Is.Null);
        Assert.That(Progress("HasLamp"),Is.False);Assert.That(Progress("HasMantle"),Is.False);
        Close();yield return null;
        var beforeRig=GameObject.Find("Elian paper walking rig");
        Assert.That(beforeRig,Is.Not.Null,"Room idle uses the same puppet as walking.");
        var outfitScale=beforeRig.transform.localScale;
        var beforeTorso=beforeRig.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Stable upper body");
        float beforeHeight=beforeTorso.sprite.bounds.size.y;
        Capture("intro-tower-before-outfit");T("DemoOpening").GetMethod("LeaveRoom").Invoke(null,null);yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("TowerRoom"));
        Assert.That((bool)Dialogue.GetType().GetProperty("IsOpen").GetValue(Dialogue),Is.True,"The family outfit is required, not the vessel.");
        Close();var wardrobeObject=GameObject.Find("Wardrobe");var playerObject=GameObject.FindWithTag("Player");
        playerObject.GetComponent<Rigidbody>().position=wardrobeObject.transform.position+new Vector3(0,1,-1.5f);Physics.SyncTransforms();yield return new WaitForFixedUpdate();
        T("DemoOpening").GetMethod("TakeMantle").Invoke(null,null);
        deadline=Time.realtimeSinceStartup+7;
        while((bool)Progress("IsPresenting") && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Intro("HasFamilyOutfit"),Is.True);
        Assert.That(Progress("HasLamp"),Is.False);Assert.That(Progress("HasMantle"),Is.False);Assert.That(Progress("IsEquipped"),Is.False);
        Assert.That(T("PlayerEquipmentAppearance").GetProperty("SpriteName").GetValue(null),Is.EqualTo("Prince-Family"));
        Assert.That(Resources.Load<Sprite>("Prince-Family-Portrait"),Is.Not.Null);
        Close();yield return null;
        var afterRig=GameObject.Find("Elian paper walking rig");
        Assert.That(afterRig,Is.SameAs(beforeRig),"Dressing must retain the existing puppet.");
        Assert.That(Mathf.Abs(afterRig.transform.localScale.x),Is.EqualTo(Mathf.Abs(outfitScale.x)).Within(.001f));
        Assert.That(afterRig.transform.localScale.y,Is.EqualTo(outfitScale.y).Within(.001f));
        Assert.That(beforeTorso.sprite.bounds.size.y,Is.EqualTo(beforeHeight).Within(.001f),"Changing clothes must not resize the upper body.");
        Capture("intro-tower-family-outfit");
        var wardrobe=GameObject.Find("Wardrobe");yield return null;
        Assert.That(wardrobe.GetComponentsInChildren<ParticleSystem>().All(p=>p.particleCount==0),Is.True);
        var camera=Camera.main;var rotation=camera.transform.rotation;var fov=camera.fieldOfView;var position=camera.transform.position;
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        body.position=new Vector3(3,1,-2);Physics.SyncTransforms();Close();yield return new WaitForSeconds(.6f);
        Assert.That(Quaternion.Angle(rotation,camera.transform.rotation),Is.LessThan(.01f));
        Assert.That(camera.fieldOfView,Is.EqualTo(fov));Assert.That(Vector3.Distance(position,camera.transform.position),Is.LessThan(2));
        T("DemoOpening").GetMethod("LeaveRoom").Invoke(null,null);
        deadline=Time.realtimeSinceStartup+8;
        while(SceneManager.GetActiveScene().name!="StartScene" && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("StartScene"));
        yield return new WaitForSeconds(1.5f);Assert.That(Intro("HasFamilyOutfit"),Is.True);
        Close();var john=UnityEngine.Object.FindObjectsByType(T("NPC"),FindObjectsSortMode.None).First(n=>n.GetType().GetField("npcName").GetValue(n).ToString()=="John");
        john.GetType().GetMethod("InteractWithPlayer").Invoke(john,null);yield return null;
        Assert.That(Progress("TrialIntroductionSeen"),Is.False,"John cannot start the old pre-vessel trial.");
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("StartScene"));Close();
        LogAssert.NoUnexpectedReceived();
    }
}
