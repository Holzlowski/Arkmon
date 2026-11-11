#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ElianBucketMotionStudyTests
{
    [UnityTest] public IEnumerator ReviewBothDirectionsAndExportMotion()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/AnimationStudies/ElianBucketMotionStudy.unity",new LoadSceneParameters(LoadSceneMode.Single));
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ElianBucketMotionStudy")).First(t=>t!=null);
        var study=UnityEngine.Object.FindFirstObjectByType(type);
        type.GetField("autoPlay").SetValue(study,false);
        Action<float> pose=t=>type.GetMethod("Evaluate").Invoke(study,new object[]{t});
        Vector3 Value(string n)=>(Vector3)type.GetProperty(n).GetValue(study);
        var camera=(Camera)type.GetProperty("StudyCamera").GetValue(study);
        string folder=Environment.GetEnvironmentVariable("ARKMON_STUDY_OUTPUT")??Path.GetFullPath("Library/ElianBucketStudy");
        bool full=Environment.GetEnvironmentVariable("ARKMON_STUDY_FULL")=="1";
        Directory.CreateDirectory(folder);
        var target=new RenderTexture(1280,720,24);target.Create();var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            foreach(bool mirror in new[]{false,true})
            {
                type.GetField("mirrored").SetValue(study,mirror);
                string direction=mirror?"links":"rechts";string frames=Path.Combine(folder,direction);Directory.CreateDirectory(frames);
                pose(2.2f-1f/240);var before=GameObject.Find("Study bucket").transform.position;
                pose(2.2f);var after=GameObject.Find("Study bucket").transform.position;
                Assert.That(Vector3.Distance(before,after),Is.LessThan(.015f),"The bucket must not jump to the hand on pickup.");
                int count=full?337:15;
                Vector3 lastHandle=Vector3.zero;float lastTime=-1;
                for(int i=0;i<count;i++)
                {
                    float time=full?i/24f:i;
                    pose(time);
                    bool attached=(bool)type.GetProperty("IsAttached").GetValue(study);
                    if(attached)Assert.That(Vector3.Distance(Value("HandPosition"),Value("HandlePosition")),Is.LessThan(.002f));
                    if((float)type.GetProperty("SupportWeight").GetValue(study)>.99f)
                        Assert.That(Vector3.Distance(Value("SupportHandPosition"),Value("SupportContactPosition")),Is.LessThan(.03f),"The supporting hand must reach the actual bucket base throughout the lift and pour.");
                    if(full && lastTime>=2.2f && time>=2.2f)
                        Assert.That(Vector3.Distance(lastHandle,Value("HandlePosition")),Is.LessThan(.10f),"A consecutive frame must not teleport the vessel.");
                    lastHandle=Value("HandlePosition");lastTime=time;
                    foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate();
                    camera.aspect=1280f/720;
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                    File.WriteAllBytes(Path.Combine(frames,$"{i:0000}.png"),texture.EncodeToPNG());
                    if(i%12==0)yield return null;
                }
                // Scrubbing backwards must reproduce the same bucket pose, independent of previous animation state.
                pose(9.5f);var expected=Value("HandlePosition");pose(0);pose(9.5f);
                Assert.That(Vector3.Distance(expected,Value("HandlePosition")),Is.LessThan(.001f));
            }
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            RenderTexture.active=previous;UnityEngine.Object.Destroy(texture);target.Release();UnityEngine.Object.Destroy(target);
        }
    }
}
#endif
