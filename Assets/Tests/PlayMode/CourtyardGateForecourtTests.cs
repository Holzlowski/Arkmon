using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardGateForecourtTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest] public IEnumerator Forecourt_CanBeCrossedAndReturnedFrom_WithAnOpenConversationSpace()
    {
        yield return Load();
        var root=GameObject.Find("Eastern gate forecourt");Assert.That(root,Is.Not.Null);
        Assert.That(root.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
        var overlap=Physics.OverlapBox(new Vector3(57.3f,1,1.15f),new Vector3(2.1f,.7f,1.4f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
        Assert.That(overlap.Where(c=>!c.CompareTag("Ground")),Is.Empty,"The central meeting space must remain free of props.");
        var ground=GameObject.Find("Irregular gate forecourt earth").GetComponent<MeshFilter>();
        foreach(var v in ground.sharedMesh.vertices.Where((v,i)=>i%51==0))
        {
            var at=ground.transform.TransformPoint(v);
            float surface=Physics.RaycastAll(at+Vector3.up*10,Vector3.down,20,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider.CompareTag("Ground")).Max(h=>h.point.y);
            Assert.That(at.y-surface,Is.InRange(.025f,.045f));
        }
        // Meadow meshes are uploaded without CPU copies; vegetation clearance is reviewed in the route captures.
        var body=GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        ((Behaviour)body.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        body.isKinematic=false;body.position=new Vector3(51.8f,1.1f,-1);Physics.SyncTransforms();
        Vector2[] route={new Vector2(57.3f,.8f),new Vector2(58.2f,3),new Vector2(63,-.8f),new Vector2(51.8f,-1),new Vector2(40,-1)};
        for(int step=0;step<route.Length;step++)
        {
            float distance=100;
            for(int frame=0;frame<260;frame++)
            {
                var d=new Vector3(route[step].x,body.position.y,route[step].y)-body.position;distance=d.magnitude;
                if(distance<.18f)break;var speed=d.normalized*4;
                body.linearVelocity=new Vector3(speed.x,body.linearVelocity.y,speed.z);yield return new WaitForFixedUpdate();
            }
            body.linearVelocity=Vector3.zero;Assert.That(distance,Is.LessThan(.2f),"Blocked forecourt route at "+body.position);
            Assert.That(body.position.y,Is.GreaterThan(.45f));
            yield return new WaitForSeconds(.8f);Capture("gate-forecourt-"+step);
        }
        body.isKinematic=true;
        var previous=root;yield return Load();Assert.That(previous==null,Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardGateForecourt"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
