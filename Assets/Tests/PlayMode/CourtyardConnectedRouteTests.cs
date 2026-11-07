using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardConnectedRouteTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static IEnumerator Load()=>(IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
    static void Capture(string n)=>typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{n});
    [UnityTest]public IEnumerator TowerWorkshopStableGardenCastleGate_StayConnected()
    {
        yield return Load();
        float originalSpeed=Time.timeScale;Time.timeScale=3;
        try
        {
        Assert.That(UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.name=="Path edge grass" || r.name=="Path edge pebble").All(r=>!r.enabled),Is.True);
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        ((Behaviour)player.GetComponent(T("PlayerInteractionFocus"))).enabled=false;
        body.isKinematic=false;body.position=new Vector3(-34,2.4f,8);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        Vector2[] route={
            new Vector2(-34,4.2f),new Vector2(-30,1.3f),new Vector2(-26,-1.7f),new Vector2(-25,-3.2f),new Vector2(-31,-7.5f),new Vector2(-33,-10.8f),new Vector2(-35.9f,-10.2f),new Vector2(-35.9f,-8.8f),
            new Vector2(-35.9f,-10.2f),new Vector2(-33,-10.8f),new Vector2(-31,-7.5f),new Vector2(-25,-3.2f),new Vector2(-18,-3),new Vector2(-15.6f,-1.1f),new Vector2(-10.8f,-.55f),
            new Vector2(-7,-1.15f),new Vector2(-3,-3.5f),new Vector2(7,-1.8f),new Vector2(12,-2),new Vector2(21,-2.9f),new Vector2(28,-2.5f),new Vector2(35,-2),
            new Vector2(35.4f,-8),new Vector2(35.4f,-12.05f),new Vector2(27.3f,-12.05f),new Vector2(27.3f,-15.85f),
            new Vector2(27.3f,-12.05f),new Vector2(35.4f,-12.05f),new Vector2(35.4f,-8),new Vector2(35,-2),new Vector2(28,-2.5f),new Vector2(21,-2.9f),new Vector2(12,-2),new Vector2(12,4),new Vector2(12,8),new Vector2(12,11.3f),
            new Vector2(12,4),new Vector2(13,-1.8f),new Vector2(21,-2.9f),new Vector2(32,-4.1f),new Vector2(40.8f,-1)};
        Vector3? passingPoint=null, afterPassingPoint=null;
        var guard=GameObject.Find("Courtyard Guard");
        for(int step=0;step<route.Length;step++)
        {
            float distance=100;
            for(int frame=0;frame<600;frame++)
            {
                var d=new Vector3(route[step].x,body.position.y,route[step].y)-body.position;
                distance=d.magnitude;if(distance<.18f)break;
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
                var v=d.normalized*4;body.linearVelocity=new Vector3(v.x,body.linearVelocity.y,v.z);
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocity=Vector3.zero;
            Assert.That(distance,Is.LessThan(.2f),"Blocked courtyard segment "+step+" at "+body.position+" guard "+guard?.transform.position+" detour "+passingPoint+" obstacles "+string.Join(",",Physics.OverlapSphere(body.position,.9f).Where(c=>c.attachedRigidbody!=body && !c.isTrigger && !c.CompareTag("Ground")).Select(c=>c.name)));
            Assert.That(body.position.y,Is.GreaterThan(.4f));
            if(new[]{0,4,8,12,15,18,21,23}.Contains(step))
            {yield return new WaitForSeconds(.7f);Capture("connected-courtyard-"+step);}
        }
        body.isKinematic=true;
        yield return Load();
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardVergePolish"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType(T("CourtyardGardenPlace"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
        }
        finally{Time.timeScale=originalSpeed;}
    }
}
