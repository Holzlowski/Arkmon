using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CourtyardNearViewTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest] public IEnumerator CloseRoute_KeepsElianInFrameAndRecordsGroundContacts()
    {
        yield return (IEnumerator)typeof(CourtyardGardenTaskTests).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});
        var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody>();
        var notes=new List<string>();
        var points=new[]{new Vector3(-34,0,8),new Vector3(-34,0,4.2f),new Vector3(-30,0,1.3f),new Vector3(-35.9f,0,-10.2f),new Vector3(-18,0,-3),new Vector3(-10.8f,0,-.55f),new Vector3(12,0,8),new Vector3(12,0,11.3f),new Vector3(27.3f,0,-15.85f),new Vector3(35,0,-2),new Vector3(40.8f,0,-1)};
        var names=new[]{"tower-door","tower-stairs","tower-base","workshop","stable","central-path","castle-stairs","castle-door","garden-front","east-crossing","gate"};
        for(int i=0;i<points.Length;i++)
        {
            var at=points[i];
            var floor=Physics.RaycastAll(at+Vector3.up*25,Vector3.down,50,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.CompareTag("Ground")).OrderByDescending(h=>h.point.y).First();
            at.y=floor.point.y+1;body.position=player.transform.position=at;Physics.SyncTransforms();yield return new WaitForSeconds(1.5f);
            var view=Camera.main;var uv=view.WorldToViewportPoint(at);
            Assert.That(uv.z,Is.GreaterThan(0));Assert.That(uv.x,Is.InRange(.08f,.92f),names[i]);Assert.That(uv.y,Is.InRange(.10f,.90f),names[i]);
            var blockers=Physics.RaycastAll(view.transform.position,at-view.transform.position,Vector3.Distance(at,view.transform.position)-.6f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.attachedRigidbody!=body && !h.collider.CompareTag("Ground"));
            notes.Add(names[i]+" floor="+floor.point.y+" blockers="+string.Join(",",blockers.Select(h=>h.collider.name)));
            var focus=player.GetComponent(T("PlayerInteractionFocus"));
            var canvas=(Canvas)focus.GetType().GetField("canvas",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(focus);
            if(canvas.enabled)
            {
                var panel=(RectTransform)focus.GetType().GetField("panel",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(focus);
                Camera uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                var bottom=RectTransformUtility.WorldToScreenPoint(uiCamera,panel.TransformPoint(new Vector3(0,panel.rect.yMin,0)));
                foreach(var sprite in player.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled && !r.forceRenderingOff && r.sprite!=null))
                {
                    var head=view.WorldToScreenPoint(sprite.bounds.center+Vector3.up*sprite.bounds.extents.y);
                    if(Mathf.Abs(bottom.x-head.x)<panel.rect.width*canvas.scaleFactor*.5f)
                        Assert.That(bottom.y,Is.GreaterThan(head.y+4),"Interaction hint covers Elian at "+names[i]);
                }
            }
            if(names[i]=="garden-front")
            {
                var wall=GameObject.Find("Cutaway near castle wall");var renderer=wall.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                Assert.That(block.GetVector("_ViewCutaway").w,Is.GreaterThan(1));
                Assert.That(wall.GetComponent<Collider>().enabled,Is.True);
                var effect=(Behaviour)wall.GetComponent(T("CourtyardNearView"));effect.enabled=false;renderer.GetPropertyBlock(block);
                Assert.That(block.GetVector("_ViewCutaway").w,Is.Zero);effect.enabled=true;yield return new WaitForSeconds(.6f);
            }
            typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"near-review-"+names[i]});
        }
        var fitted=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh!=null && f.sharedMesh.name.EndsWith("terrain fitted")).ToArray();
        Assert.That(fitted.Length,Is.GreaterThanOrEqualTo(7));
        foreach(var filter in fitted)
            foreach(var vertex in filter.sharedMesh.vertices)
            {
                var world=filter.transform.TransformPoint(vertex);
                var ground=Physics.RaycastAll(new Vector3(world.x,20,world.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.CompareTag("Ground")).Max(h=>h.point.y);
                Assert.That(world.y-ground,Is.InRange(-.026f,.086f),"Unsupported approach stone: "+filter.name);
            }
        File.WriteAllLines("Library/courtyard-near-view.txt",notes);
        LogAssert.NoUnexpectedReceived();
    }
}
