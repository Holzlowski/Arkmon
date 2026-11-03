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
    [UnityTest] public IEnumerator EastGate_ConnectsCourtyardToFarBank()
    {
        yield return SceneManager.LoadSceneAsync("StartScene");yield return null;yield return null;
        GameObject player=GameObject.FindWithTag("Player");((Behaviour)player.GetComponent(T("PlayerController"))).enabled=false;
        Rigidbody body=player.GetComponent<Rigidbody>();body.position=new Vector3(40,1,-1);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        Assert.That(Physics.Raycast(new Vector3(53,2,-1),Vector3.down,out RaycastHit hit,4),Is.True);
        Assert.That(hit.collider.CompareTag("Ground"),Is.True);
        for(int i=0;i<160;i++)
        {body.linearVelocity=new Vector3(5,body.linearVelocity.y,0);yield return new WaitForFixedUpdate();}
        Assert.That(body.position.x,Is.GreaterThan(51), "Elian reaches the far bank.");
        Assert.That(body.position.y,Is.GreaterThan(.4f));
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Scene previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("GateCleanup"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}
