using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;

public class BattlePresentationTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object I(string name) => T(name).GetProperty("Instance").GetValue(null);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    private static object P(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object F(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);

    private IEnumerator Deploy()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene");
        for (int n = 0; n < 5; n++) yield return null;
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled = false;
        object deployment = I("BattleDeployment");
        IList cells = (IList)F(deployment, "available");
        Assert.That(Call(deployment, "TryPlace", cells[0]), Is.EqualTo(true));
        float deadline = Time.realtimeSinceStartup + 12;
        while (!(bool)P(I("UnitManager"), "IsBattleRunning") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(P(I("UnitManager"), "IsBattleRunning"), Is.EqualTo(true));
        yield return null;
    }

    [UnityTest]
    public IEnumerator Deployment_HidesCombatUIAndReleasesCameraAtArenaOverview()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene");
        for (int n = 0; n < 5; n++) yield return null;
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled = false;
        CanvasGroup commands = GameObject.Find("Paper battle command bar").GetComponent<CanvasGroup>();
        CanvasGroup enemy = GameObject.Find("Opponent monster card").GetComponent<CanvasGroup>();
        Assert.That(commands.alpha, Is.Zero); Assert.That(commands.blocksRaycasts, Is.False);
        Assert.That(enemy.alpha, Is.Zero);
        object deployment = I("BattleDeployment");
        Call(deployment, "TryPlace", ((IList)F(deployment, "available"))[0]);
        Component controls = UnityEngine.Object.FindFirstObjectByType(T("CameraController")) as Component;
        Assert.That(P(controls, "IsPresentationLocked"), Is.EqualTo(true));
        bool opponentSeen = false;
        float deadline = Time.realtimeSinceStartup + 12;
        while (!(bool)P(I("UnitManager"), "IsBattleRunning") && Time.realtimeSinceStartup < deadline)
        { opponentSeen |= (bool)P(deployment, "IsOpponentSummoning"); yield return null; }
        yield return null;
        Assert.That(opponentSeen, Is.True);
        Assert.That(P(controls, "IsPresentationLocked"), Is.EqualTo(false));
        Assert.That(commands.alpha, Is.EqualTo(1)); Assert.That(enemy.alpha, Is.EqualTo(1));
        object grid = I("LevelGrid");
        object center = Activator.CreateInstance(T("GridPosition"), (int)Call(grid, "GetWidth") / 2, (int)Call(grid, "GetHeight") / 2);
        Assert.That(controls.transform.position, Is.EqualTo(Call(grid, "GetWorldPosition", center)));
        foreach (Component unit in (IList)Call(I("UnitManager"), "GetUnitList"))
        {
            Vector3 screen = Camera.main.WorldToViewportPoint(unit.transform.position + Vector3.up * .2f);
            Assert.That(screen.y, Is.GreaterThan(.26f), "Front-row monsters must clear the command bar.");
            Assert.That(screen.z, Is.GreaterThan(0));
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator StatusBadges_ShowBothConditionsCountdownAndExplainStanding()
    {
        yield return Deploy();
        Component enemy = ((IList)Call(I("UnitManager"), "GetEnemyUnitList"))[0] as Component;
        object burning = enemy.GetComponent(T("BurningStatus"));
        Assert.That(Call(burning, "TryIgnite", 1f, 3, 5), Is.EqualTo(true));
        Call(enemy, "SetKnockedDown", true); yield return null; yield return null;
        Transform card = GameObject.Find("Opponent monster card").transform;
        Transform fire = card.Find("Burning badge"), down = card.Find("Knockdown badge");
        Assert.That(fire.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        Assert.That(down.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        Assert.That(fire.GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("3 R."));
        Call(down.GetComponent(T("PaperStatusBadge")), "OnPointerEnter", new PointerEventData(EventSystem.current));
        Assert.That(card.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text.Contains("1 AP")), Is.True);
        int health = (int)P(enemy.GetComponent(T("HealthSystem")), "Health");
        LogAssert.Expect(LogType.Log, (health - 5).ToString()); Call(burning, "Tick");
        yield return null; yield return null;
        Assert.That(fire.GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("2 R."));
        Call(burning, "Extinguish"); Call(enemy, "SetKnockedDown", false);
        yield return null; yield return null;
        Assert.That(fire.GetComponent<CanvasGroup>().alpha, Is.Zero);
        Assert.That(down.GetComponent<CanvasGroup>().alpha, Is.Zero);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator HitFeedback_DisplaysActualDamageWithoutMovingTheLogicalUnit()
    {
        yield return Deploy();
        Component enemy = ((IList)Call(I("UnitManager"), "GetEnemyUnitList"))[0] as Component;
        Vector3 position = enemy.transform.position; Quaternion rotation = enemy.transform.rotation;
        Component health = enemy.GetComponent(T("HealthSystem"));
        LogAssert.Expect(LogType.Log, ((int)P(health, "Health") - 7).ToString());
        Call(health, "Damage", 7, null); yield return null;
        GameObject popup = GameObject.Find("Paper damage number"); Assert.That(popup, Is.Not.Null);
        Assert.That(popup.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("−7"));
        SpriteRenderer body = enemy.GetComponentInChildren<SpriteRenderer>();
        Assert.That(body.color.r, Is.GreaterThan(1));
        Assert.That(enemy.transform.position, Is.EqualTo(position)); Assert.That(enemy.transform.rotation, Is.EqualTo(rotation));
        yield return new WaitForSeconds(.85f);
        Assert.That(popup == null, Is.True); Assert.That(body.color.r, Is.EqualTo(1).Within(.01f));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Result_WaitsForRecallAndConfirmationThenShowsOutcomeDialogueOnce()
    {
        foreach (string result in new[] { "Won", "Lost", "Escaped" })
        {
            yield return SceneManager.LoadSceneAsync("StartScene"); yield return new WaitForSeconds(.8f);
            object loader = T("SceneLoadManager").GetField("instance").GetValue(null);
            Vector3 saved = GameObject.FindWithTag("Player").transform.position;
            Call(loader, "SavePlayerTransform", saved); Call(loader, "SetBattleSpeaker", "John");
            yield return Deploy();
            if (result == "Escaped") Assert.That(Call(I("UnitManager"), "TryEscape"), Is.EqualTo(true));
            else
            {
                string list = result == "Won" ? "GetEnemyUnitList" : "GetFriendlyUnitList";
                Component victim = ((IList)Call(I("UnitManager"), list))[0] as Component;
                Component health = victim.GetComponent(T("HealthSystem"));
                LogAssert.Expect(LogType.Log, "0"); Call(health, "Damage", (int)P(health, "Health"), null);
            }
            float deadline = Time.realtimeSinceStartup + 12;
            while (I("BattleResultPanel") == null && Time.realtimeSinceStartup < deadline) yield return null;
            object panel = I("BattleResultPanel"); Assert.That(panel, Is.Not.Null);
            Assert.That(P(panel, "Result").ToString(), Is.EqualTo(result));
            if (result == "Won")
                Assert.That(((Component)panel).GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text.Contains("Kafla und du")), Is.True);
            Assert.That(P(UnityEngine.Object.FindFirstObjectByType(T("CameraController")), "IsPresentationLocked"), Is.EqualTo(true));
            Assert.That(P(I("BattleDeployment"), "IsRecalling"), Is.EqualTo(false));
            yield return new WaitForSeconds(.2f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BattleScene"));
            Call(panel, "Continue");
            while ((SceneManager.GetActiveScene().name != "StartScene"
                || (bool)T("SceneLoadManager").GetProperty("IsTransitioning").GetValue(null))
                && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            Assert.That(Vector3.Distance(GameObject.FindWithTag("Player").transform.position, saved), Is.LessThan(.03f));
            object dialog = T("DialogueBoxController").GetField("instance").GetValue(null);
            Assert.That(P(dialog, "IsOpen"), Is.EqualTo(true));
            ScriptableObject tree = F(dialog, "temporaryDialogue") as ScriptableObject;
            Array sections = (Array)T("DialogueTree").GetField("sections").GetValue(tree);
            object section = sections.GetValue(0);
            string line = ((string[])section.GetType().GetField("dialogue").GetValue(section))[0];
            Assert.That(line, Does.Contain(result == "Won" ? "Gut gemacht" : result == "Lost" ? "Jeder Kampf" : "Pause"));
            Call(dialog, "CloseDialogue"); yield return null;
            Assert.That(P(dialog, "IsOpen"), Is.EqualTo(false));
            Assert.That(F(loader, "returnResult"), Is.Null);
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("PresentationCleanup" + Guid.NewGuid()));
        yield return SceneManager.UnloadSceneAsync(scene);
    }
}
