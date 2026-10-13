using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class AtmosphereFlowTests
{
    private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    private static bool Transitioning => (bool)T("SceneLoadManager").GetProperty("IsTransitioning").GetValue(null);
    [UnityTest]
    public IEnumerator AstralPuff_PlaysAfterMaterializationButNotDuringTravelOrRecall()
    {
        yield return SceneManager.LoadSceneAsync("StartScene"); yield return null;
        object audio = T("SoundManager").GetField("instance").GetValue(null);
        var root = new GameObject("Summon timing test");
        var effect = (MonoBehaviour)root.AddComponent(T("MonsterSummonEffect"));
        Coroutine summoning = effect.StartCoroutine((IEnumerator)Call(effect, "Play",
            Vector3.zero, Vector3.right, (Color?)Color.white));
        Assert.That(T("SoundManager").GetProperty("LastEffectName").GetValue(audio), Is.EqualTo("summon"));
        yield return new WaitForSeconds(.3f);
        Assert.That(T("SoundManager").GetProperty("LastEffectName").GetValue(audio), Is.EqualTo("summon"));
        yield return summoning;
        Assert.That(T("SoundManager").GetProperty("LastEffectName").GetValue(audio), Is.EqualTo("summon-arrive"));
        bool absorbed = false;
        yield return effect.StartCoroutine((IEnumerator)Call(effect, "PlayRecall",
            Vector3.right, Vector3.zero, Color.white, (Action<float>)(_ => { }), (Action)(() => absorbed = true)));
        Assert.That(absorbed, Is.True);
        Assert.That(T("SoundManager").GetProperty("LastEffectName").GetValue(audio), Is.EqualTo("recall"));
        UnityEngine.Object.Destroy(root);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator LampTransition_RepeatedBattleReturnRestoresPlayerAndSingleAudioManager()
    {
        yield return SceneManager.LoadSceneAsync("StartScene"); yield return new WaitForSeconds(.7f);
        object manager = T("SceneLoadManager").GetField("instance").GetValue(null);
        Vector3 position = GameObject.FindWithTag("Player").transform.position;
        for (int cycle = 0; cycle < 2; cycle++)
        {
            Call(manager, "SavePlayerTransform", position);
            Call(manager, "LoadBattleScene", "BattleScene");
            Assert.That(Transitioning, Is.True);
            object soundtrack = T("SoundManager").GetField("instance").GetValue(null);
            Assert.That(T("SoundManager").GetProperty("CurrentMusicName").GetValue(soundtrack), Is.EqualTo("battle"));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StartScene"), "Battle music starts before loading, during the closing iris.");
            float deadline = Time.realtimeSinceStartup + 15;
            while (Transitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Transitioning, Is.False); Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BattleScene"));
            yield return null;
            Component deployment = (Component)UnityEngine.Object.FindFirstObjectByType(T("BattleDeployment"));
            object grid = T("LevelGrid").GetProperty("Instance").GetValue(null);
            foreach (string property in new[] { "Trainer", "OpponentTrainer" })
            {
                Transform trainer = (Transform)T("BattleDeployment").GetProperty(property).GetValue(deployment);
                Assert.That(Call(grid, "IsValidGridPosition", Call(grid, "GetGridPosition", trainer.position)), Is.EqualTo(false));
                SpriteRenderer figure = trainer.GetComponentInChildren<SpriteRenderer>();
                foreach (Renderer stone in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (stone.name.EndsWith("parapet", StringComparison.OrdinalIgnoreCase))
                        Assert.That(stone.bounds.Intersects(figure.bounds), Is.False, "Trainer illustration must clear the stone ring.");
            }
            Call(manager, "LoadSceneBeforeBattle");
            while (Transitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Transitioning, Is.False); Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StartScene"));
            Assert.That(GameObject.FindWithTag("Player").transform.position, Is.EqualTo(position));
            Assert.That(UnityEngine.Object.FindObjectsByType(T("SoundManager"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Component audio = (Component)T("SoundManager").GetField("instance").GetValue(null);
            Assert.That(audio.GetComponents<AudioSource>().Any(s => s.clip != null && s.clip.name == "courtyard" && s.loop), Is.True);
            yield return null; // Destroy releases the transition overlay at the end of the frame.
            Assert.That(GameObject.Find("Lamp scene transition"), Is.Null);
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator SummonSparks_UseMonsterColorAndCleanUpOnCancel()
    {
        yield return SceneManager.LoadSceneAsync("StartScene"); yield return null;
        var root = new GameObject("Spark test");
        var effect = (MonoBehaviour)root.AddComponent(T("MonsterSummonEffect"));
        Color color = new Color(.8f, .3f, .15f);
        effect.StartCoroutine((IEnumerator)Call(effect, "Play", Vector3.zero, Vector3.right * 3, (Color?)color));
        yield return null;
        ParticleSystem sparks = root.GetComponentInChildren<ParticleSystem>();
        Assert.That(sparks, Is.Not.Null); Assert.That(sparks.main.startColor.color, Is.EqualTo(color));
        Assert.That(sparks.main.scalingMode, Is.EqualTo(ParticleSystemScalingMode.Shape));
        Assert.That(sparks.main.startSize.constantMin, Is.GreaterThanOrEqualTo(.12f));
        Assert.That(sparks.main.startSpeed.constantMin, Is.GreaterThanOrEqualTo(2.8f));
        Assert.That(sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name, Is.EqualTo("Arkmon/Magic Sparks"));
        Call(effect, "CancelAll"); effect.StopAllCoroutines(); yield return null;
        Assert.That(root.GetComponentsInChildren<ParticleSystem>().Length, Is.EqualTo(0));
        UnityEngine.Object.Destroy(root);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest]
    public IEnumerator NPC_FacesPlayerKeepsRootUprightAndUsesIntegratedSummoningCharm()
    {
        yield return SceneManager.LoadSceneAsync("StartScene"); yield return null; yield return null;
        Component npc = (Component)UnityEngine.Object.FindFirstObjectByType(T("NPC"));
        GameObject player = GameObject.FindWithTag("Player");
        ((Behaviour)player.GetComponent(T("PlayerController"))).enabled = false;
        player.GetComponent<Rigidbody>().isKinematic = true;
        Quaternion rotation = npc.transform.rotation;
        SpriteRenderer body = npc.GetComponentInChildren<SpriteRenderer>();
        object dialog = T("DialogueBoxController").GetField("instance").GetValue(null);
        foreach (int side in new[] { -1, 1 })
        {
            player.transform.position = npc.transform.position + Camera.main.transform.right * side;
            Call(npc, "InteractWithPlayer"); yield return null;
            Assert.That(body.flipX, Is.EqualTo(side == -1));
            Assert.That(npc.transform.rotation, Is.EqualTo(rotation));
            Assert.That(npc.GetComponent(T("NPCSummoningFocus")), Is.Not.Null);
            Assert.That(GameObject.Find("NPC magical sphere"), Is.Null);
            Assert.That(GameObject.Find("Summoning orb core"), Is.Null);
            Call(dialog, "CloseDialogue"); yield return null;
        }
        yield return SceneManager.LoadSceneAsync("BattleScene"); yield return null; yield return null;
        ScriptableObject configuration = Resources.FindObjectsOfTypeAll(T("BattleConfiguration")).Cast<ScriptableObject>()
            .First(c => c.name == "Default NPC Battle");
        Assert.That(((Array)T("BattleConfiguration").GetField("enemyTeam").GetValue(configuration)).Length, Is.EqualTo(1));
        Component deployment = (Component)UnityEngine.Object.FindFirstObjectByType(T("BattleDeployment"));
        Transform trainer = (Transform)T("BattleDeployment").GetProperty("Trainer").GetValue(deployment);
        Component visual = trainer.GetComponent(T("PaperCharacterVisual"));
        SpriteRenderer trainerBody = trainer.GetComponentsInChildren<SpriteRenderer>().First(r => r.sprite != null && r.sprite.name.Contains("Prince"));
        Sprite resting = trainerBody.sprite;
        Call(visual, "SetSummoningPose", true);
        Assert.That(trainerBody.sprite, Is.EqualTo(Resources.Load<Sprite>("Player-Summoning")));
        Assert.That(trainerBody.sprite, Is.Not.EqualTo(resting));
        Call(visual, "SetSummoningPose", false);
        Assert.That(trainerBody.sprite, Is.EqualTo(resting));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("AtmosphereCleanup" + Guid.NewGuid()));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}
