using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PaperEffectsTests
{
    private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).First(t => t != null);
    [UnitySetUp] public IEnumerator LoadBattle()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene");
        for (int i = 0; i < 4; i++) yield return null;
    }
    [UnityTearDown] public IEnumerator UnloadBattle()
    {
        Scene battle = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("PaperEffectsCleanup"));
        yield return SceneManager.UnloadSceneAsync(battle);
    }

    [UnityTest] public IEnumerator BurningSpritesAnimateWithoutChangingBattleRandomnessAndDisappearOnBurnout()
    {
        Component tree = UnityEngine.Object.FindObjectsByType(RuntimeType("BurningObstacle"), FindObjectsSortMode.None)
            .Cast<Component>().First(o => o.name.Contains("tree"));
        Component status = tree.GetComponent(RuntimeType("BurningStatus"));
        string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
        Assert.That(status.GetType().GetMethod("TryIgnite").Invoke(status, new object[] { 1f, 3, 5 }), Is.EqualTo(true));
        Assert.That(JsonUtility.ToJson(UnityEngine.Random.state), Is.EqualTo(randomBefore));
        GameObject visual = GameObject.Find("Burning flames");
        var renderers = visual.GetComponentsInChildren<SpriteRenderer>();
        Assert.That(renderers.Length, Is.GreaterThan(1));
        Sprite[] initial = renderers.Select(r => r.sprite).ToArray();
        yield return new WaitForSeconds(.15f);
        Assert.That(renderers.Where((r, i) => r.sprite != initial[i]).Any(), Is.True);
        foreach (var renderer in renderers)
            Assert.That(Vector3.Dot(renderer.transform.up, Camera.main.transform.up), Is.GreaterThan(.999f));
        for (int i = 0; i < 3; i++) status.GetType().GetMethod("Tick").Invoke(status, null);
        yield return null;
        Assert.That(visual == null, Is.True);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator MeadowCloudsMoveAndTheSkySurvivesRepeatedSceneLoads()
    {
        for (int cycle = 0; cycle < 2; cycle++)
        {
            Assert.That(RenderSettings.skybox, Is.Not.Null);
            Assert.That(RenderSettings.skybox.shader.name, Is.EqualTo("Arkmon/Blue Sky"));
            Component cloud = (Component)UnityEngine.Object.FindFirstObjectByType(RuntimeType("CloudDrift"));
            Vector3 before = cloud.transform.position;
            yield return new WaitForSeconds(.15f);
            Assert.That(cloud.transform.position.x, Is.GreaterThan(before.x));
            Assert.That(Vector3.Dot(cloud.transform.up, Camera.main.transform.up), Is.GreaterThan(.999f));
            Assert.That(GameObject.Find("Idyllic meadow").GetComponentsInChildren<Collider>().Length, Is.EqualTo(0));
            if (cycle == 0)
            {
                yield return SceneManager.LoadSceneAsync("StartScene");
                yield return SceneManager.LoadSceneAsync("BattleScene");
                for (int i = 0; i < 4; i++) yield return null;
            }
        }
        LogAssert.NoUnexpectedReceived();
    }
}
