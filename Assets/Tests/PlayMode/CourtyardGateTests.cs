using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CourtyardGateTests
{
    private static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest] public IEnumerator GateForecourt_HasSolidGroundAndStopsPlayerBeforeOuterEdge()
    {
        yield return SceneManager.LoadSceneAsync("StartScene");yield return null;yield return null;
        GameObject player=GameObject.FindWithTag("Player");((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;
        Rigidbody body=player.GetComponent<Rigidbody>();body.position=new Vector3(26,2,.75f);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        Assert.That(Physics.Raycast(new Vector3(27,2,.75f),Vector3.down,out RaycastHit hit,4),Is.True);
        Assert.That(hit.collider.CompareTag("Ground"),Is.True);
        for(int i=0;i<100;i++)
        {body.linearVelocity=new Vector3(5,body.linearVelocity.y,0);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.x,Is.GreaterThan(28));Assert.That(body.position.x,Is.LessThan(30));
        Assert.That(body.position.y,Is.GreaterThan(.4f));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Scene previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("GateCleanup"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}
