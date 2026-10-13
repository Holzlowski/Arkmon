using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TacticalTrialTests
{
    const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static object I(string name) => T(name).GetProperty("Instance").GetValue(null);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    static object Cell(int x, int z) => Activator.CreateInstance(T("GridPosition"), x, z);
    static Vector3 World(int x, int z) => (Vector3)Call(I("LevelGrid"), "GetWorldPosition", Cell(x, z));
    [UnityTest] public IEnumerator ArenaHasOpenRoutesAndGuideRespondsOnceToActualEvents()
    {
        yield return new CommandNavigationTests().FirstTurn_CommandsAreAvailableAndBackCancelsTargetingInBothCategories();
        var obstacles = UnityEngine.Object.FindObjectsByType(T("BurningObstacle"), FindObjectsSortMode.None).Cast<Component>().ToArray();
        var occupied = obstacles.Select(o => Call(o, "GetGridPosition").ToString()).ToArray();
        Assert.That(occupied.Distinct().Count(), Is.EqualTo(occupied.Length), "Obstacles must occupy distinct cells.");
        Assert.That(obstacles.Any(o => Call(o, "GetGridPosition").Equals(Cell(4, 5))), Is.True, "Left stone cover.");
        Assert.That(obstacles.Any(o => Call(o, "GetGridPosition").Equals(Cell(4, 4))), Is.True, "Burnable bush beside centre cover.");
        object[] pathArgs = { Cell(2, 4), Cell(8, 4), 0 };
        var path = (IList)I("Pathfinding").GetType().GetMethod("FindPath").Invoke(I("Pathfinding"), pathArgs);
        Assert.That(path, Is.Not.Null); Assert.That(path.Count, Is.GreaterThan(0), "An open route around centre obstacles must exist.");
        foreach (int row in new[] { 2, 8 })
        {
            object[] flankArgs = { Cell(2, row), Cell(8, row), 0 };
            Assert.That(I("Pathfinding").GetType().GetMethod("FindPath").Invoke(I("Pathfinding"), flankArgs), Is.Not.Null, "Open flank at row " + row);
        }
        var guide = new GameObject("Trial reactions test").AddComponent(T("FirstTrialGuide")); yield return null;
        var selected = (Component)Call(I("UnitActionSystem"), "GetSelectedUnit");
        var enemy = ((IEnumerable)Call(I("UnitManager"), "GetEnemyUnitList")).Cast<Component>().First();
        Call(enemy, "MoveToWorldPosition", World(8, 5)); Call(selected, "MoveToWorldPosition", World(3, 4)); Physics.SyncTransforms();
        var move = selected.GetComponent(T("MoveAction")); bool done = false;
        Assert.That(Call(move, "IsValidActionGridPosition", Cell(3, 5)), Is.True);
        Call(move, "TakeAction", Cell(3, 5), (Action)(() => done = true));
        float deadline = Time.realtimeSinceStartup + 4; while (!done && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(done, Is.True); yield return null;
        Assert.That(guide.GetType().GetProperty("CurrentHint").GetValue(guide), Does.Contain("Deckung"));
        var observationUntil = guide.GetType().GetField("observationUntil", F);
        var burnable = obstacles.First(o => Call(o, "GetGridPosition").Equals(Cell(4, 4))).GetComponent(T("BurningStatus"));
        Call(burnable, "TryIgnite", 1f, 3, 5); Call(burnable, "TryIgnite", 1f, 3, 5);
        observationUntil.SetValue(guide, 0f); yield return null;
        Assert.That(guide.GetType().GetProperty("CurrentHint").GetValue(guide), Does.Contain("Holz und Pflanzen"));
        Call(selected, "SetKnockedDown", true); observationUntil.SetValue(guide, 0f); yield return null;
        Assert.That(guide.GetType().GetProperty("CurrentHint").GetValue(guide), Does.Contain("Aufstehen"));
        var ui = (Component)UnityEngine.Object.FindFirstObjectByType(T("UnitActionSystemUI"));
        Assert.That(Call(I("UnitActionSystem"), "GetSelectedAction"), Is.Null, "Knockdown must not silently select a command.");
        Call(ui, "ShowCommandCategory", false); yield return null;
        var cards = (Transform)ui.GetType().GetField("actionButtonContainerTransform", F).GetValue(ui);
        Assert.That(cards.GetComponentsInChildren<UnityEngine.UI.Button>().Length, Is.EqualTo(1));
        Call(burnable, "Extinguish"); Call(burnable, "TryIgnite", 1f, 3, 5);
        Assert.That(((ICollection)guide.GetType().GetField("observations", F).GetValue(guide)).Count, Is.EqualTo(0), "Each tactical explanation appears once.");
        UnityEngine.Object.Destroy(guide.gameObject); yield return null; Call(selected, "SetKnockedDown", false);
        LogAssert.NoUnexpectedReceived();
    }
}
