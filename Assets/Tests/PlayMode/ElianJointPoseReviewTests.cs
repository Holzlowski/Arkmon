#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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

public class ElianJointPoseReviewTests
{
    static readonly BindingFlags P=BindingFlags.Public|BindingFlags.Instance;
    static object Field(object o,string n)=>o.GetType().GetField(n,P).GetValue(o);
    static object Property(object o,string n)=>o.GetType().GetProperty(n,P).GetValue(o);
    [UnityTest] public IEnumerator CaptureStaticJointPosesAndCheckConstruction()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/AnimationStudies/ElianJointPoseReview.unity",new LoadSceneParameters(LoadSceneMode.Single));
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ElianJointPoseReview")).First(t=>t!=null);
        var review=UnityEngine.Object.FindFirstObjectByType(type);var camera=(Camera)Field(review,"reviewCamera");
        var figures=((Array)Field(review,"figures")).Cast<Component>().ToArray();var issues=new List<string>();var report=new System.Text.StringBuilder();
        string folder=Environment.GetEnvironmentVariable("ARKMON_JOINT_OUTPUT")??Path.GetFullPath("Library/ElianJointReview");Directory.CreateDirectory(folder);
        void Check(bool condition,string message){if(!condition)issues.Add(message);}
        void Select(int i)=>type.GetMethod("Select").Invoke(review,new object[]{i});
        void Capture(string name,int width,int height)
        {
            foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate();
            var target=new RenderTexture(width,height,24);target.Create();var before=RenderTexture.active;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            float aspect=camera.aspect;
            try
            {
                camera.aspect=width/(float)height;RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            }
            finally {camera.aspect=aspect;RenderTexture.active=before;UnityEngine.Object.Destroy(image);target.Release();UnityEngine.Object.Destroy(target);}
        }
        foreach(bool mirror in new[]{false,true})
        {
            type.GetField("mirrored").SetValue(review,mirror);Select(-1);yield return null;yield return null;Capture(mirror?"00-Unity-Posen-links":"00-Unity-Posen",1920,1080);
            for(int i=0;i<figures.Length;i++)
            {
                Select(i);yield return null;yield return null;
                string label=(i+1).ToString("00")+new[]{"-Unity-Hocke","-Unity-Tragen","-Unity-Anheben"}[i]+(mirror?"-links":"");
                Capture(label,960,1280);var rig=figures[i];var pose=Field(rig,"pose");
                float rightError=(float)Property(rig,"RightReachError"),leftError=(float)Property(rig,"LeftReachError");
                report.AppendLine($"{label}: right grip error {rightError:F5}; left contact error {leftError:F5}");
                float rightWrist=(float)Property(rig,"RightWristAngle"),leftWrist=(float)Property(rig,"LeftWristAngle");
                report.AppendLine($"Wrist deviation right={rightWrist:F2}, left={leftWrist:F2}");
                Check(rightWrist<65&&leftWrist<65,label+" wrist turns backwards");
                Check(rightError<.006f,label+" right arm cannot reach its authored bucket position: "+rightError);
                Check(leftError<.01f,label+" left arm cannot reach its authored palm contact: "+leftError);
                var bucket=(SpriteRenderer)Field(rig,"bucket");Check(bucket.transform.localScale==Vector3.one,label+" bucket resized");
                Check(Vector3.Distance((Vector3)Property(rig,"RightPalm"),bucket.transform.position)<.002f,label+" disconnected handle");
                if((bool)Field(pose,"supportsBucket"))Check(Vector3.Distance((Vector3)Property(rig,"LeftPalm"),(Vector3)Property(rig,"SupportContact"))<.01f,label+" disconnected supporting palm");
                foreach(string name in new[]{"rightLeg","leftLeg","rightArm","leftArm"})
                {
                    var chain=Field(rig,name);var first=(Transform)Field(chain,"first");var joint=(Transform)Field(chain,"middle");var tip=(Transform)Field(chain,"tip");
                    Check(Mathf.Abs(Vector3.Distance(first.position,joint.position)-(float)Property(chain,"FirstLength"))<.0002f,label+" "+name+" upper segment resized");
                    Check(Mathf.Abs(Vector3.Distance(joint.position,tip.position)-(float)Property(chain,"SecondLength"))<.0002f,label+" "+name+" lower segment resized");
                    Check(first.localScale==Vector3.one&&joint.localScale==Vector3.one&&tip.localScale==Vector3.one,label+" "+name+" scaled bone");
                    var skin=(SkinnedMeshRenderer)Field(chain,"surface");Check(skin.bones.Length==3,label+" missing native skin bones");
                    if(name.EndsWith("Leg"))
                    {
                        float flex=180-Vector2.Angle(first.position-joint.position,tip.position-joint.position);
                        report.AppendLine($"{name}: knee flex {flex:F2}");
                        if(i==0)Check(flex>65,label+" must bend actual knee");
                        if(i==1)Check(flex<35,label+" carrying knee still in squat");
                        var baked=new Mesh();skin.BakeMesh(baked);float lowest=baked.vertices.Min(p=>skin.transform.TransformPoint(p).y);UnityEngine.Object.Destroy(baked);
                        report.AppendLine($"{name}: sole y {lowest:F5}");Check(Mathf.Abs(lowest)<.008f,label+" "+name+" visible foot is off ground: "+lowest);
                        Check(skin.sharedMesh.boneWeights.Any(w=>w.boneIndex1==1&&w.weight1>.9f),label+" knee has no art influence");
                        Check(skin.sharedMesh.boneWeights.Any(w=>w.boneIndex2==2&&w.weight2>.9f),label+" foot has no art influence");
                    }
                }
            }
        }
        File.WriteAllText(Path.Combine(folder,"construction-check.txt"),report.ToString());
        Assert.That(issues,Is.Empty,string.Join("\n",issues));LogAssert.NoUnexpectedReceived();
    }
}
#endif