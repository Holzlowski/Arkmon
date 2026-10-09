using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class UnityMigrationSceneTests
{
    private Mouse mouse;
    private Keyboard keyboard;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        mouse = InputSystem.AddDevice<Mouse>();
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene cleanup = SceneManager.CreateScene("MigrationTestCleanup");
        SceneManager.SetActiveScene(cleanup);
        yield return SceneManager.UnloadSceneAsync(previous);
        InputSystem.RemoveDevice(mouse);
        InputSystem.RemoveDevice(keyboard);
    }

    [UnityTest]
    public IEnumerator StartScene_LoadsWithoutRuntimeErrors()
    {
        yield return SceneManager.LoadSceneAsync("StartScene", LoadSceneMode.Single);
        yield return null;
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StartScene"));
        Assert.That(Camera.main, Is.Not.Null);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator RepeatedSceneChanges_InitializeBattleAndSelectionEachTime()
    {
        for (int cycle = 0; cycle < 3; cycle++)
        {
            yield return SceneManager.LoadSceneAsync("StartScene", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return null;
            Type managerType = RuntimeType("UnitManager");
            object manager = managerType.GetProperty("Instance").GetValue(null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(managerType.GetProperty("IsBattleRunning").GetValue(manager), Is.EqualTo(true));
            var friendlies = (ICollection)managerType.GetMethod("GetFriendlyUnitList").Invoke(manager, null);
            var enemies = (ICollection)managerType.GetMethod("GetEnemyUnitList").Invoke(manager, null);
            Assert.That(friendlies.Count, Is.GreaterThan(0));
            Assert.That(enemies.Count, Is.GreaterThan(0));
            Type selectionType = RuntimeType("UnitActionSystem");
            object selection = selectionType.GetProperty("Instance").GetValue(null);
            Assert.That(selectionType.GetMethod("GetSelectedUnit").Invoke(selection, null), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }
    }

    private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetType(name)).First(type => type != null);
}
