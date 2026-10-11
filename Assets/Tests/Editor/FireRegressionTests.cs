using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class FireRegressionTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
    private Component system;
    private ScriptableObject rules;
    private UnityEngine.Random.State randomState;
    private static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).First(t => t != null);
    private static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, Flags).Invoke(o, a);
    private static object P(object o, string n) => o.GetType().GetProperty(n).GetValue(o);
    private static void Set(object o, string n, object v) => o.GetType().GetField(n, Flags).SetValue(o, v);
    private GameObject Create(string name)
    { var go = new GameObject(name); go.SetActive(false); objects.Add(go); return go; }
    private Component Unit(int x, int z, bool immune = false, bool enemy = false)
    {
        var go = Create("Fire test unit"); var health = go.AddComponent(T("HealthSystem")); var unit = go.AddComponent(T("Unit"));
        var definition = ScriptableObject.CreateInstance(T("MonsterDefinition")); assets.Add(definition);
        Set(definition, "immuneToBurning", immune); Set(unit, "definition", definition); Set(unit, "isEnemy", enemy);
        Call(health, "Awake"); Call(unit, "Awake");
        Set(unit, "gridPosition", Activator.CreateInstance(T("GridPosition"), x, z));
        go.SetActive(true); var status = go.GetComponent(T("BurningStatus")); Call(status, "OnEnable"); return status;
    }
    private Component Plant(int x, int z, bool stone = false)
    {
        var go = Create("Fire test obstacle"); var obstacle = go.AddComponent(T("BurningObstacle"));
        Set(obstacle, "maxHealth", 100); Set(obstacle, "materialKind", Enum.ToObject(T("BurningObstacle").GetNestedType("MaterialKind"), stone ? 1 : 0));
        Set(obstacle, "position", Activator.CreateInstance(T("GridPosition"), x, z)); Call(obstacle, "Awake");
        go.SetActive(true); var status = go.GetComponent(T("BurningStatus")); Call(status, "OnEnable"); return status;
    }
    private void Ignite(Component status) => Assert.That(Call(status, "TryIgnite", 1f, 3, 5), Is.EqualTo(true));
    private void Round() => Call(system, "ResolveRound");
    private float Health(Component status) => (float)Call(status.GetComponent(T("Unit")), "GetHealthNormalized");
    [SetUp] public void SetUp()
    {
        randomState = UnityEngine.Random.state;
        system = Create("Fire test system").AddComponent(T("BurningSystem")); Call(system, "Awake");
        rules = ScriptableObject.CreateInstance(T("FireRules")); assets.Add(rules);
        Set(rules, "monsterSpreadChance", 1f); Set(rules, "vegetationSpreadChance", 1f); Set(system, "rules", rules);
    }
    [TearDown] public void TearDown()
    {
        foreach (GameObject go in objects)
        {
            var status = go.GetComponent(T("BurningStatus")); if (status != null) Call(status, "OnDisable");
            UnityEngine.Object.DestroyImmediate(go);
        }
        objects.Clear(); foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset); assets.Clear();
        UnityEngine.Random.state = randomState;
    }
    [Test] public void Fire_NewNeighboursWaitOneRoundForDamageAndPropagation()
    {
        var a = Unit(1, 1); var b = Unit(2, 1, enemy: true); var c = Unit(3, 1);
        Ignite(a); Round();
        Assert.That(Health(a), Is.EqualTo(.95f).Within(.001f)); Assert.That(Health(b), Is.EqualTo(1f));
        Assert.That(P(b, "RemainingRounds"), Is.EqualTo(3)); Assert.That(P(c, "IsBurning"), Is.EqualTo(false));
        Round(); Assert.That(Health(b), Is.EqualTo(.95f).Within(.001f)); Assert.That(P(c, "IsBurning"), Is.EqualTo(true));
        Assert.That(Health(c), Is.EqualTo(1f));
    }
    [Test] public void Fire_OnlyCardinalNeighboursIgnite()
    {
        var a = Plant(2, 2); var diagonal = Unit(3, 3); var cardinal = Plant(2, 3);
        Ignite(a); Round(); Assert.That(P(diagonal, "IsBurning"), Is.EqualTo(false)); Assert.That(P(cardinal, "IsBurning"), Is.EqualTo(true));
    }
    [Test] public void Fire_MonstersCanIgniteVegetationAndOpposingOrFriendlyMonsters()
    {
        var a = Unit(2, 2); var friend = Unit(1, 2); var enemy = Unit(3, 2, enemy: true); var plant = Plant(2, 3);
        Ignite(a); Round(); foreach (var s in new[] { friend, enemy, plant }) Assert.That(P(s, "IsBurning"), Is.EqualTo(true));
    }
    [Test] public void Fire_ImmunityRejectsIgnitionButAllowsDirectDamage()
    {
        var cat = Unit(1, 1, immune: true); Assert.That(Call(cat, "TryIgnite", 1f, 3, 5), Is.EqualTo(false));
        Call(cat.GetComponent(T("Unit")), "Damage", 25, null); Assert.That(Health(cat), Is.EqualTo(.75f).Within(.001f));
        var rock = Plant(2, 2, true); Assert.That(Call(rock, "TryIgnite", 1f, 3, 5), Is.EqualTo(false));
        Call(rock.GetComponent(T("BurningObstacle")), "Damage", 100); Assert.That(P(rock.GetComponent(T("BurningObstacle")), "Health"), Is.EqualTo(100));
    }
    [Test] public void Fire_ExpiresAfterExactlyThreeTicksAndRefreshDoesNotStackDamage()
    {
        var a = Unit(1, 1); Ignite(a); Round(); Ignite(a);
        Assert.That(P(a, "RemainingRounds"), Is.EqualTo(3)); Assert.That(P(a, "DamagePerRound"), Is.EqualTo(5));
        Round(); Round(); Round(); Round(); Assert.That(Health(a), Is.EqualTo(.8f).Within(.001f)); Assert.That(P(a, "IsBurning"), Is.EqualTo(false));
    }
    [Test] public void Fire_MultipleSourcesRollOnlyOnceForTheSameTarget()
    {
        var a = Unit(1, 2); var b = Unit(3, 2); var target = Unit(2, 2); Ignite(a); Ignite(b);
        Set(rules, "monsterSpreadChance", .2f);
        UnityEngine.Random.InitState(123); float roll = UnityEngine.Random.value; float expectedNext = UnityEngine.Random.value;
        UnityEngine.Random.InitState(123); Round();
        Assert.That(UnityEngine.Random.value, Is.EqualTo(expectedNext)); Assert.That(P(target, "IsBurning"), Is.EqualTo(roll < .2f));
    }
    [Test] public void Fire_ZeroChanceNeverIgnitesOrRefreshes()
    {
        var a = Unit(1, 1); Assert.That(Call(a, "TryIgnite", 0f, 3, 5), Is.EqualTo(false));
        Ignite(a); Round(); Assert.That(Call(a, "TryIgnite", 0f, 3, 5), Is.EqualTo(false)); Assert.That(P(a, "RemainingRounds"), Is.EqualTo(2));
    }
}
