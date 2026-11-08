#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Profiling;

// Explicit command-line diagnostic, excluded from non-development players.
[DefaultExecutionOrder(11000)]
public sealed class CourtyardBuildReview : MonoBehaviour
{
    [Serializable] sealed class Sample {public string station;public float fps,p95Milliseconds;public int frames;public double drawCalls,setPassCalls,triangles,mainThreadMilliseconds,renderThreadMilliseconds;}
    [Serializable] sealed class Report {public bool passed;public string graphics,version;public int width,height;public bool developmentBuild;public bool offscreenRendering;public Sample[] samples;public string[] errors;}
    readonly List<Sample> samples=new List<Sample>();readonly List<string> errors=new List<string>();string output;Rigidbody body;RenderTexture target;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Begin()
    {
        if(!Environment.GetCommandLineArgs().Contains("-arkmonCourtyardReview"))return;
        Application.runInBackground=true;
        var go=new GameObject("Courtyard build review");DontDestroyOnLoad(go);go.AddComponent<CourtyardBuildReview>();
    }
    void OnEnable()=>Application.logMessageReceived+=Log;
    void OnDisable()=>Application.logMessageReceived-=Log;
    void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);}
    IEnumerator Start()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-reviewOutput");
        output=i>=0 && i+1<args.Length?Path.GetFullPath(args[i+1]):Path.Combine(Application.persistentDataPath,"CourtyardReview");Directory.CreateDirectory(output);
        AudioListener.volume=0;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
        yield return null;IntroChapter.EquipFamilyOutfit();yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(2);
        target=new RenderTexture(1600,900,24);target.Create();PrepareCanvases();
        DialogueBoxController.instance?.CloseDialogue();var player=GameObject.FindWithTag("Player");
        if(player==null){errors.Add("Missing player");Finish();yield break;}
        player.GetComponent<PlayerController>().enabled=false;player.GetComponent<PlayerInteractionFocus>().enabled=false;
        body=player.GetComponent<Rigidbody>();body.isKinematic=false;body.position=new Vector3(-34,2.4f,8);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        if(args.Contains("-reviewProfile"))
        {
            body.isKinematic=true;body.interpolation=RigidbodyInterpolation.None;
            yield return PlacePlayer(new Vector3(-10.8f,1,-.55f));
            yield return new WaitForSeconds(2);
            var renderers=FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if(args.Contains("-reviewNoInstancing"))
                foreach(var material in renderers.Select(r=>r.sharedMaterial).Where(m=>m!=null && m.shader.name=="Arkmon/Courtyard Illustrated Surface").Distinct())material.enableInstancing=false;
            File.WriteAllLines(Path.Combine(output,"renderers.txt"),renderers.Where(r=>r.enabled && r.sharedMaterial!=null)
                .GroupBy(r=>r.sharedMaterial.shader.name).OrderByDescending(g=>g.Count())
                .Select(g=>g.Key+" count="+g.Count()));
            yield return Measure("profile-baseline");Capture("profile-baseline");
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float shadowDistance=pipeline.shadowDistance;pipeline.shadowDistance=0;
            yield return new WaitForSeconds(1);yield return Measure("profile-no-shadows");pipeline.shadowDistance=shadowDistance;
            foreach(string category in new[]{"PlantWind","IllustratedSurface","MeadowWind"})
            {
                var hidden=renderers.Where(r=>r.enabled && r.sharedMaterial!=null && r.sharedMaterial.shader.name.Replace(" ", "").Contains(category)).ToArray();
                foreach(var r in hidden)r.enabled=false;
                yield return new WaitForSeconds(1);yield return Measure("profile-without-"+category);
                foreach(var r in hidden)r.enabled=true;
            }
            yield return new WaitForSeconds(1);yield return Measure("profile-baseline-repeat");Finish();yield break;
        }
        Vector2[] route={new Vector2(-34,4.2f),new Vector2(-30,1.3f),new Vector2(-26,-1.7f),new Vector2(-25,-3.2f),new Vector2(-31,-7.5f),new Vector2(-33,-10.8f),new Vector2(-35.9f,-10.2f),new Vector2(-35.9f,-8.8f),
            new Vector2(-35.9f,-10.2f),new Vector2(-33,-10.8f),new Vector2(-31,-7.5f),new Vector2(-25,-3.2f),new Vector2(-18,-3),new Vector2(-15.6f,-1.1f),new Vector2(-10.8f,-.55f),new Vector2(-7,-1.15f),new Vector2(-3,-3.5f),new Vector2(7,-1.8f),new Vector2(12,-2),new Vector2(21,-2.9f),new Vector2(28,-2.5f),new Vector2(35,-2),
            new Vector2(35.4f,-8),new Vector2(35.4f,-12.05f),new Vector2(27.3f,-12.05f),new Vector2(27.3f,-15.85f),
            new Vector2(27.3f,-12.05f),new Vector2(35.4f,-12.05f),new Vector2(35.4f,-8),new Vector2(35,-2),new Vector2(28,-2.5f),new Vector2(21,-2.9f),new Vector2(12,-2),new Vector2(12,4),new Vector2(12,8),new Vector2(12,11.3f),
            new Vector2(12,4),new Vector2(13,-1.8f),new Vector2(21,-2.9f),new Vector2(32,-4.1f),new Vector2(40.8f,-1),new Vector2(46,-1),new Vector2(57.3f,.8f),new Vector2(63,-.8f),new Vector2(40,-1)};
        var guard=FindFirstObjectByType<CourtyardGuard>();
        Vector3? passingPoint=null, afterPassingPoint=null;
        for(int step=0;step<route.Length;step++)
        {
            float end=Time.realtimeSinceStartup+12,distance=100;
            while(Time.realtimeSinceStartup<end)
            {
                var d=new Vector3(route[step].x,body.position.y,route[step].y)-body.position;distance=d.magnitude;if(distance<.18f)break;
                // Finish a chosen sidestep before targeting the route again. Recomputing it
                // every frame can oscillate while a courteous guard waits for the player.
                if(passingPoint.HasValue)
                {
                    var at=passingPoint.Value;at.y=body.position.y;passingPoint=at;
                    if(Vector3.Distance(body.position,at)<.25f){passingPoint=afterPassingPoint;afterPassingPoint=null;}
                }
                if(!passingPoint.HasValue && guard!=null)
                {
                    var offset=guard.transform.position-body.position;offset.y=0;
                    var heading=d.normalized;float ahead=Vector3.Dot(offset,heading);
                    if(offset.sqrMagnitude<6.25f && ahead>0 && ahead<distance+.5f && (offset-heading*ahead).magnitude<1.1f)
                    {
                        var side=new Vector3(heading.z,0,-heading.x);if(side.z<0)side=-side;
                        // Step sideways first; a diagonal straight to the far point still clips the guard.
                        passingPoint=body.position+side*1.8f;
                        afterPassingPoint=new Vector3(guard.transform.position.x,body.position.y,guard.transform.position.z)+heading*2.0f+side*1.8f;
                    }
                }
                if(passingPoint.HasValue)d=passingPoint.Value-body.position;
                var v=d.normalized*4;body.linearVelocity=new Vector3(v.x,body.linearVelocity.y,v.z);yield return new WaitForFixedUpdate();
            }
            body.linearVelocity=Vector3.zero;
            if(distance>.2f || body.position.y<.4f){errors.Add("Blocked route "+step+" at "+body.position+" near "+string.Join(",",Physics.OverlapSphere(body.position,.9f).Where(c=>c.attachedRigidbody!=body && !c.CompareTag("Ground") && !c.isTrigger).Select(c=>c.name)));Finish();yield break;}
            if(new[]{0,4,8,12,18,23,24,25}.Contains(step))
            {
                yield return new WaitForSeconds(.8f);yield return Measure("station-"+step);
                Capture("station-"+step);yield return null;yield return null;
            }
        }
        body.isKinematic=true;body.interpolation=RigidbodyInterpolation.None;
        var station=GameObject.Find("Courtyard watering can").GetComponent<CourtyardGarden>();
        yield return PlacePlayer(station.transform.position+Vector3.up);station.InteractWithPlayer();yield return WaitGarden();DialogueBoxController.instance?.CloseDialogue();
        if(!CourtyardGarden.HasCan)errors.Add("Can pickup failed");
        var bed=FindObjectsByType<CourtyardGarden>(FindObjectsSortMode.None).First(c=>c.name=="Garden watering point");
        yield return PlacePlayer(bed.transform.position+Vector3.up);bed.InteractWithPlayer();yield return WaitGarden();DialogueBoxController.instance?.CloseDialogue();
        if(CourtyardGarden.WateredCount!=1)errors.Add("Pour did not complete");
        yield return PlacePlayer(station.transform.position+Vector3.up);station.InteractWithPlayer();yield return WaitGarden();DialogueBoxController.instance?.CloseDialogue();
        if(CourtyardGarden.HasCan)errors.Add("Can return failed before leaving garden");
        var lookout=FindFirstObjectByType<CourtyardLookout>();yield return PlacePlayer(new Vector3(-40.9f,1,7));
        lookout.InteractWithPlayer();yield return new WaitForSeconds(10.3f);if(!CourtyardLookout.IsPresenting)errors.Add("Lookout did not open");
        yield return Measure("western-lookout");Capture("western-lookout");yield return null;
        lookout.ReturnToCourtyard();yield return new WaitForSeconds(3.8f);if(CourtyardLookout.IsPresenting)errors.Add("Lookout did not release camera");
        yield return SceneManager.LoadSceneAsync("StartScene");yield return new WaitForSeconds(1);
        if(FindObjectsByType<CourtyardAmbientDetails>(FindObjectsSortMode.None).Length!=1 || FindObjectsByType<CourtyardResidenceGround>(FindObjectsSortMode.None).Length!=1)errors.Add("Duplicate scene details");
        Finish();
    }
    IEnumerator PlacePlayer(Vector3 position)
    {
        body.transform.position=position;body.position=position;Physics.SyncTransforms();
        yield return new WaitForSeconds(1);
        DialogueBoxController.instance?.CloseDialogue();
    }
    IEnumerator WaitGarden(){float end=Time.realtimeSinceStartup+5;while(CourtyardGarden.IsBusy && Time.realtimeSinceStartup<end)yield return null;if(CourtyardGarden.IsBusy)errors.Add("Garden action timeout");}
    IEnumerator Measure(string name)
    {
        using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
        using var passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count");
        using var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
        using var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread");
        using var render=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread");
        double drawSum=0,passSum=0,triSum=0,mainSum=0,renderSum=0;
        PrepareCanvases();var frames=new List<float>();float end=Time.realtimeSinceStartup+3;yield return null;
        while(Time.realtimeSinceStartup<end){frames.Add(Time.unscaledDeltaTime);drawSum+=draws.Valid?draws.LastValue:0;passSum+=passes.Valid?passes.LastValue:0;triSum+=triangles.Valid?triangles.LastValue:0;mainSum+=main.Valid?main.LastValue:0;renderSum+=render.Valid?render.LastValue:0;yield return null;}
        if(frames.Count==0){errors.Add("No measured frames");yield break;}
        frames.Sort();var sample=new Sample{station=name,frames=frames.Count,fps=1/frames.Average(),p95Milliseconds=1000*frames[Mathf.Min(frames.Count-1,Mathf.FloorToInt(frames.Count*.95f))]};sample.drawCalls=drawSum/frames.Count;sample.setPassCalls=passSum/frames.Count;sample.triangles=triSum/frames.Count;sample.mainThreadMilliseconds=mainSum/frames.Count/1e6;sample.renderThreadMilliseconds=renderSum/frames.Count/1e6;samples.Add(sample);
        Debug.Log("COURTYARD_REVIEW "+name+" fps="+sample.fps);
    }
    void PrepareCanvases()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if(canvas.renderMode==RenderMode.ScreenSpaceOverlay)
            {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.15f;}
        Canvas.ForceUpdateCanvases();
    }
    void LateUpdate(){if(target!=null && SceneManager.GetActiveScene().name=="StartScene")RenderFrame();}
    void RenderFrame()
    {
        var camera=Camera.main;if(camera==null || target==null)return;
        var previous=camera.targetTexture;float aspect=camera.aspect;
        try
        {
            camera.targetTexture=target;camera.aspect=1600f/900;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
        }
        finally{camera.targetTexture=previous;camera.aspect=aspect;}
    }
    void Capture(string name)
    {
        PrepareCanvases();RenderFrame();var previous=RenderTexture.active;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());}
        finally{RenderTexture.active=previous;Destroy(image);}
    }
    void OnDestroy(){if(target!=null){target.Release();Destroy(target);}}
    void Finish()
    {
        var report=new Report{passed=errors.Count==0,graphics=SystemInfo.graphicsDeviceName,version=Application.version,width=1600,height=900,developmentBuild=Debug.isDebugBuild,offscreenRendering=true,samples=samples.ToArray(),errors=errors.ToArray()};
        File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(report,true));Debug.Log("COURTYARD_BUILD_REVIEW "+(report.passed?"PASSED":"FAILED"));Application.Quit(report.passed?0:1);
    }
}
#endif
