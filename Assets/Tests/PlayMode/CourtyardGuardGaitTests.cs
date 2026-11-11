using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardGuardGaitTests
{
    [UnityTest] public IEnumerator BothFeetAlternate_ArmsCounterSwing_AndJointsStayConnected()
    {
        yield return (IEnumerator)typeof(CourtyardGuardTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        var root=GameObject.Find("Courtyard Guard");
        var visual=Array.Find(root.GetComponents<MonoBehaviour>(),c=>c.GetType().Name=="CourtyardGuardVisual");
        foreach(var behaviour in root.GetComponents<MonoBehaviour>())if(behaviour.GetType().Name=="CourtyardGuard" || behaviour==visual)behaviour.enabled=false;
        var paper=root.transform.Find("Guard illustrated figure");
        var rig=paper.Find("Guard paper walking rig");rig.gameObject.SetActive(true);paper.GetComponent<SpriteRenderer>().forceRenderingOff=true;
        var near=rig.Find("Guard near leg");var far=rig.Find("Guard far leg");
        var nearArm=rig.Find("Guard near arm");var farArm=rig.Find("Guard far arm");
        Vector3 nearHip=near.localPosition,farHip=far.localPosition;
        Vector3 nearShoulder=nearArm.localPosition,farShoulder=farArm.localPosition;
        var pose=visual.GetType().GetMethod("PresentGait",BindingFlags.NonPublic|BindingFlags.Instance);
        foreach(var part in rig.GetComponentsInChildren<SpriteRenderer>())Assert.That(part.sprite,Is.Not.Null);
        var camera=Camera.main;Vector3 cameraPosition=camera.transform.position;Quaternion cameraAngle=camera.transform.rotation;
        camera.transform.position=root.transform.position+Vector3.up*1.3f-camera.transform.forward*4.5f;
        paper.rotation=camera.transform.rotation;
        for(int i=0;i<2;i++)
        {
            pose.Invoke(visual,new object[]{Mathf.PI*(i==0?.5f:1.5f),1f,false});
            float nearFoot=FootX(near),farFoot=FootX(far);
            Assert.That((nearFoot-farFoot)*(i==0?1:-1),Is.GreaterThan(.5f),"The other foot must lead after half a cycle.");
            float legAngle=Mathf.DeltaAngle(0,near.localEulerAngles.z),armAngle=Mathf.DeltaAngle(0,nearArm.localEulerAngles.z);
            Assert.That(legAngle*armAngle,Is.LessThan(0),"Near arm must oppose near leg.");
            Assert.That(Mathf.DeltaAngle(0,far.localEulerAngles.z),Is.EqualTo(-legAngle).Within(.01f));
            Assert.That(Mathf.DeltaAngle(0,farArm.localEulerAngles.z),Is.EqualTo(-armAngle).Within(.01f));
            typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{i==0?"guard-gait-near-foot-forward":"guard-gait-far-foot-forward"});
        }
        for(int i=0;i<32;i++)
        {
            pose.Invoke(visual,new object[]{i*Mathf.PI/16,1f,false});
            Assert.That(near.localPosition,Is.EqualTo(nearHip));Assert.That(far.localPosition,Is.EqualTo(farHip));
            Assert.That(nearArm.localPosition,Is.EqualTo(nearShoulder));Assert.That(farArm.localPosition,Is.EqualTo(farShoulder));
            float ground=float.PositiveInfinity;
            foreach(var leg in new[]{near,far})foreach(var vertex in leg.GetComponent<SpriteRenderer>().sprite.vertices)
                ground=Mathf.Min(ground,paper.InverseTransformPoint(leg.TransformPoint(vertex)).y);
            Assert.That(ground,Is.EqualTo(.015f).Within(.003f),"One boot must keep ground contact throughout the stride.");
        }
        pose.Invoke(visual,new object[]{Mathf.PI*.5f,1f,false});float facingRight=ProjectedFootX(near,paper)-ProjectedFootX(far,paper);
        pose.Invoke(visual,new object[]{Mathf.PI*.5f,1f,true});float facingLeft=ProjectedFootX(near,paper)-ProjectedFootX(far,paper);
        Assert.That(facingRight*facingLeft,Is.LessThan(0),"Facing left must mirror the entire gait.");
        camera.transform.SetPositionAndRotation(cameraPosition,cameraAngle);
        visual.enabled=true;yield return new WaitForSeconds(.25f);
        Assert.That(rig.gameObject.activeSelf,Is.True,"Talking and resting retain the same puppet proportions.");
        Assert.That(paper.GetComponent<SpriteRenderer>().forceRenderingOff,Is.True);
        LogAssert.NoUnexpectedReceived();
    }
    private static float FootX(Transform leg)
    {
        var bounds=leg.GetComponent<SpriteRenderer>().sprite.bounds;
        return (leg.localRotation*new Vector3(bounds.center.x,bounds.min.y,0)+leg.localPosition).x;
    }
    private static float ProjectedFootX(Transform leg,Transform paper)
    {
        var bounds=leg.GetComponent<SpriteRenderer>().sprite.bounds;
        return paper.InverseTransformPoint(leg.TransformPoint(new Vector3(bounds.center.x,bounds.min.y,0))).x;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        var previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("GuardGaitCleanup"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}