using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PaperUIPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static object Instance(string name) => T(name).GetProperty("Instance").GetValue(null);

    private IEnumerator Battle()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene");
        for (int i = 0; i < 4; i++) yield return null;
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled = false;
        ((Behaviour)Instance("UnitManager")).enabled = false;
        object deployment = Instance("BattleDeployment");
        IList cells = (IList)Field(deployment, "available");
        Assert.That(Call(deployment, "TryPlace", cells[0]), Is.EqualTo(true));
        float deadline = Time.realtimeSinceStartup + 10;
        while (!(bool)T("BattleDeployment").GetProperty("IsReady").GetValue(deployment)
            && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(T("BattleDeployment").GetProperty("IsReady").GetValue(deployment), Is.EqualTo(true));
        while (!(bool)T("UnitManager").GetProperty("IsBattleRunning").GetValue(Instance("UnitManager"))
            && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(T("UnitManager").GetProperty("IsBattleRunning").GetValue(Instance("UnitManager")), Is.EqualTo(true));
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("PaperUICleanup"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator Commands_SeparateAttacksDisableUnaffordableAndReplaceWithStanding()
    {
        yield return Battle();
        Component unit = (Component)Call(Instance("UnitActionSystem"), "GetSelectedUnit");
        var ui = (Component)UnityEngine.Object.FindFirstObjectByType(T("UnitActionSystemUI"));
        Component[] cards = ui.GetComponentsInChildren(T("ActionButtonUI")).Cast<Component>().ToArray();
        Assert.That(cards.Length, Is.EqualTo(2));
        string[] labels = cards.Select(c => ((TextMeshProUGUI)Field(c, "textMeshProUGUI")).text).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Bewegen", "Fliehen" }, labels);
        Call(ui, "ShowCommandCategory", true);
        yield return null;
        cards = ui.GetComponentsInChildren(T("ActionButtonUI")).Cast<Component>().ToArray();
        labels = cards.Select(c => ((TextMeshProUGUI)Field(c, "textMeshProUGUI")).text).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Krallenhieb", "Flammenschwert" }, labels);
        object selection = Instance("UnitActionSystem");
        Component clawCard = cards.First(c => Field(c, "baseAction") is Component action && action.GetType() == T("ClawAction"));
        clawCard.GetComponent<Button>().onClick.Invoke();
        Assert.That(Call(selection, "GetSelectedAction"), Is.SameAs(unit.GetComponent(T("ClawAction"))));
        Call(selection, "SetBusy");
        yield return null;
        Assert.That(cards.All(c => !c.GetComponent<Button>().interactable), Is.True);
        Assert.That(GameObject.Find("Busy label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Aktion läuft ..."));
        Call(selection, "ClearBusy");
        Component sword = unit.GetComponent(T("FlameSwordAction"));
        Assert.That(Call(unit, "TrySpendActionPointsToTakeAction", sword), Is.EqualTo(true));
        yield return null;
        Component swordCard = cards.First(c => Field(c, "baseAction") == sword);
        Assert.That(swordCard.GetComponent<Button>().interactable, Is.False);
        swordCard.GetComponent<Button>().onClick.Invoke();
        Assert.That(Call(selection, "GetSelectedAction"), Is.SameAs(unit.GetComponent(T("ClawAction"))));
        Call(unit, "SetKnockedDown", true);
        yield return null;
        cards = ui.GetComponentsInChildren(T("ActionButtonUI")).Cast<Component>().ToArray();
        Assert.That(cards.Length, Is.EqualTo(1));
        Assert.That(((TextMeshProUGUI)Field(cards[0], "textMeshProUGUI")).text, Is.EqualTo("Aufstehen"));
        Assert.That(cards[0].GetComponent<Button>().interactable, Is.True);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Commands_PagesAdditionalAttacksAndKeepsBasicActionsSeparate()
    {
        yield return Battle();
        Component unit = (Component)Call(Instance("UnitActionSystem"), "GetSelectedUnit");
        Component ui = (Component)UnityEngine.Object.FindFirstObjectByType(T("UnitActionSystemUI"));
        Array attacks = Array.CreateInstance(T("BaseAction"), 6);
        for (int i = 0; i < 6; i++) attacks.SetValue(unit.gameObject.AddComponent(T("ClawAction")), i);
        unit.GetType().GetField("attacks", Flags).SetValue(unit, attacks);
        Call(ui, "ShowCommandCategory", true);
        yield return null;
        Assert.That(ui.GetComponentsInChildren(T("ActionButtonUI")).Length, Is.EqualTo(4));
        Assert.That(GameObject.Find("Attack page").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("1 / 2"));
        Call(ui, "ChangePage", 1);
        yield return null;
        Assert.That(ui.GetComponentsInChildren(T("ActionButtonUI")).Length, Is.EqualTo(2));
        Assert.That(GameObject.Find("Attack page").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("2 / 2"));
        Assert.That(Call(Instance("UnitActionSystem"), "GetSelectedAction"), Is.SameAs(attacks.GetValue(4)));
        Call(ui, "ShowCommandCategory", false);
        yield return null;
        Assert.That(ui.GetComponentsInChildren(T("ActionButtonUI")).Length, Is.EqualTo(2));
        Assert.That(Call(Instance("UnitActionSystem"), "GetSelectedAction"), Is.SameAs(unit.GetComponent(T("MoveAction"))));
        unit.GetType().GetField("attacks", Flags).SetValue(unit, null);
        Call(ui, "ShowCommandCategory", true);
        yield return null;
        Assert.That(ui.GetComponentsInChildren(T("ActionButtonUI")).Length, Is.EqualTo(0));
        Assert.That(Call(Instance("UnitActionSystem"), "GetSelectedAction"), Is.Null);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator WorldStatus_UpdatesBurnDurationAndKnockdownWithoutChangingOccupancy()
    {
        yield return Battle();
        Component enemy = ((IEnumerable)Call(Instance("UnitManager"), "GetEnemyUnitList")).Cast<Component>().First();
        object cell = Call(enemy, "GetGridPosition");
        Component burning = enemy.GetComponent(T("BurningStatus"));
        Assert.That(Call(burning, "TryIgnite", 1f, 3, 5), Is.EqualTo(true));
        yield return null;
        TextMeshProUGUI label = enemy.GetComponentsInChildren<TextMeshProUGUI>().First(t => t.name == "World status");
        Assert.That(label.text, Is.EqualTo("Brennt · 3 Runden"));
        LogAssert.Expect(LogType.Log, "45");
        Call(burning, "Tick");
        Call(enemy, "SetKnockedDown", true);
        yield return null;
        Assert.That(label.text, Is.EqualTo("Am Boden · Brennt · 2 Runden"));
        Assert.That(Call(enemy, "GetGridPosition"), Is.EqualTo(cell));
        Call(burning, "Extinguish");
        yield return null;
        Assert.That(label.text, Is.EqualTo("Am Boden"));
        LogAssert.NoUnexpectedReceived();
    }

    private static ScriptableObject Tree(int answerCount, string[] lines)
    {
        ScriptableObject tree = ScriptableObject.CreateInstance(T("DialogueTree"));
        object section = Activator.CreateInstance(T("DialogueSection"));
        T("DialogueSection").GetField("dialogue").SetValue(section, lines);
        object branch = Activator.CreateInstance(T("BranchPoint"));
        T("BranchPoint").GetField("question").SetValue(branch, "Bereit für einen Übungskampf?");
        Array answers = Array.CreateInstance(T("Answer"), answerCount);
        for (int i = 0; i < answerCount; i++)
        {
            object answer = Activator.CreateInstance(T("Answer"));
            T("Answer").GetField("answerLabel").SetValue(answer, "Antwort " + (i + 1));
            T("Answer").GetField("nextElement").SetValue(answer, 0); answers.SetValue(answer, i);
        }
        T("BranchPoint").GetField("answers").SetValue(branch, answers);
        T("DialogueSection").GetField("branchPoint").SetValue(section, branch);
        Array sections = Array.CreateInstance(T("DialogueSection"), 1); sections.SetValue(section, 0);
        T("DialogueTree").GetField("sections").SetValue(tree, sections);
        return tree;
    }

    [UnityTest]
    public IEnumerator Dialogue_GrowsAnswerListAndCloseCancelsPendingInputOnce()
    {
        yield return SceneManager.LoadSceneAsync("StartScene"); yield return null; yield return null;
        Component dialog = (Component)T("DialogueBoxController").GetField("instance").GetValue(null);
        ScriptableObject tree = Tree(4, Array.Empty<string>());
        int closed = 0; Action listener = () => closed++;
        T("DialogueBoxController").GetEvent("OnDialogueEnded").AddEventHandler(null, listener);
        try
        {
            Call(dialog, "StartDialogue", tree, 0, "Arkhüter", "BattleScene", null, null);
            yield return null;
            Button[] buttons = (Button[])Field(dialog, "answerObjects");
            Assert.That(buttons.Length, Is.EqualTo(4));
            Assert.That(buttons.All(b => b.gameObject.activeInHierarchy), Is.True);
            Call(dialog, "AnswerQuestion", 8);
            Assert.That(Field(dialog, "answerTriggered"), Is.EqualTo(false));
            Call(dialog, "CloseDialogue"); Call(dialog, "CloseDialogue");
            yield return null;
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(T("DialogueBoxController").GetProperty("IsOpen").GetValue(dialog), Is.EqualTo(false));
            Assert.That(((GameObject)Field(dialog, "answerBox")).activeInHierarchy, Is.False);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StartScene"));
        }
        finally { T("DialogueBoxController").GetEvent("OnDialogueEnded").RemoveEventHandler(null, listener); UnityEngine.Object.Destroy(tree); }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Dialogue_FirstContinueRevealsTextNextContinueShowsChoices()
    {
        yield return SceneManager.LoadSceneAsync("StartScene"); yield return null; yield return null;
        Component dialog = (Component)T("DialogueBoxController").GetField("instance").GetValue(null);
        string line = new string('a', 120);
        ScriptableObject tree = Tree(2, new[] { line });
        Call(dialog, "StartDialogue", tree, 0, "Arkhüter", "BattleScene", null, null);
        Call(dialog, "SkipLine"); yield return null; yield return null;
        Assert.That(((TextMeshProUGUI)Field(dialog, "dialogueText")).text, Is.EqualTo(line));
        Assert.That(((GameObject)Field(dialog, "answerBox")).activeSelf, Is.False);
        Call(dialog, "SkipLine"); yield return null; yield return null;
        Assert.That(((GameObject)Field(dialog, "answerBox")).activeSelf, Is.True);
        Call(dialog, "CloseDialogue"); UnityEngine.Object.Destroy(tree);
        LogAssert.NoUnexpectedReceived();
    }
}
