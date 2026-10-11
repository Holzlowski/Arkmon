using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

// The runtime uses Unity's predefined Assembly-CSharp. Test assemblies cannot
// reference that assembly directly, so resolve its types through reflection.
public class BattleRegressionTests
{
    [Test]
    public void LethalDamage_ReportsDeadBeforeDamageListenersRun()
    {
        Component health = Create("HealthSystem"); Call(health, "Awake");
        bool observedDead = false;
        EventHandler listener = (sender, e) => observedDead = (bool)Property(health, "IsDead");
        health.GetType().GetEvent("OnDamage").AddEventHandler(health, listener);
        Call(health, "Damage", 100, null);
        Assert.That(observedDead, Is.True);
    }

    [Test]
    public void DeadUnit_CannotSpendPointsMoveOrReenterGrid()
    {
        Component attacker = CreateUnit(melee: true);
        Component ram = attacker.GetComponent(RuntimeType("MoveAction"));
        Component health = attacker.GetComponent(RuntimeType("HealthSystem"));
        Call(health, "Damage", 100, null);
        Vector3 position = attacker.transform.position;
        Assert.That(Call(attacker, "TrySpendActionPointsToTakeAction", ram), Is.EqualTo(false));
        Call(attacker, "MoveToWorldPosition", position + Vector3.right * 2f);
        Assert.That(attacker.transform.position, Is.EqualTo(position));
        Assert.That(((Array)Call(attacker, "GetBaseActionArray")).Length, Is.EqualTo(0));
    }

    [Test]
    public void PaperMonster_PrefersActiveAttackTargetOverNearestOpponent()
    {
        Component manager = Create("UnitManager");
        Call(manager, "Awake");
        Component owner = CreateUnit();
        Component near = CreateUnit(enemy: true);
        Component far = CreateUnit(enemy: true);
        near.transform.position = Vector3.right * 2f;
        far.transform.position = Vector3.right * 8f;
        foreach (Component unit in new[] { owner, near, far }) Call(manager, "Unit_OnAnyUnitSpawned", unit, EventArgs.Empty);
        Component shoot = owner.gameObject.AddComponent(RuntimeType("ShootAction"));
        Component visual = owner.gameObject.AddComponent(RuntimeType("PaperCharacterVisual"));
        GameObject pivot = new GameObject("Paper pivot");
        pivot.transform.SetParent(owner.transform);
        pivot.AddComponent<SpriteRenderer>();
        Set(visual, "paperPivot", pivot.transform);
        Call(visual, "OnEnable");
        Assert.That(Call(visual, "GetOpponentDirection"), Is.EqualTo(Vector3.right * 2f));
        Set(shoot, "targetUnit", far);
        Set(shoot, "isActive", true);
        Assert.That(Call(visual, "GetOpponentDirection"), Is.EqualTo(Vector3.right * 8f));
        Set(shoot, "isActive", false);
        Assert.That(Call(visual, "GetOpponentDirection"), Is.EqualTo(Vector3.right * 2f));
    }
    [Test]
    public void SummonColor_UsesMonsterDefinitionWithoutChangingOtherUnits()
    {
        Component first = CreateUnit();
        Component second = CreateUnit();
        Set(first, "summonColor", Color.red);
        Set(second, "summonColor", Color.blue);
        Assert.That(Call(first, "GetSummonColor"), Is.EqualTo(Color.red));
        ScriptableObject definition = ScriptableObject.CreateInstance(RuntimeType("MonsterDefinition"));
        assets.Add(definition);
        Set(definition, "summonColor", Color.green);
        Set(first, "definition", definition);
        Assert.That(Call(first, "GetSummonColor"), Is.EqualTo(Color.green));
        Assert.That(Call(second, "GetSummonColor"), Is.EqualTo(Color.blue));
    }
    [Test]
    public void GridOrigin_OffsetsWorldCoordinatesAndRoundTrips()
    {
        Component grid = Create("LevelGrid");
        Set(grid, "width", 8);
        Set(grid, "height", 8);
        Set(grid, "cellSize", 2f);
        Set(grid, "origin", new Vector3(8f, 0f, 2f));
        Call(grid, "Awake");
        object cell = Activator.CreateInstance(RuntimeType("GridPosition"), 2, 3);
        Vector3 world = (Vector3)Call(grid, "GetWorldPosition", cell);
        Assert.That(world, Is.EqualTo(new Vector3(12f, 0f, 8f)));
        Assert.That(Call(grid, "GetGridPosition", world), Is.EqualTo(cell));
    }
    [Test]
    public void DeploymentZones_RespectSideBoundsAndOpposingTeams()
    {
        ScriptableObject configuration = ScriptableObject.CreateInstance(RuntimeType("BattleConfiguration"));
        assets.Add(configuration);
        Type sideType = RuntimeType("BattleConfiguration").GetNestedType("DeploymentSide");
        Type positionType = RuntimeType("GridPosition");
        Set(configuration, "deploymentDepth", 2);
        Set(configuration, "deploymentRadius", 2);
        foreach (string side in new[] { "Left", "Right", "Bottom", "Top" })
        {
            Set(configuration, "playerSide", Enum.Parse(sideType, side));
            int friendlyCount = 0;
            for (int x = 0; x < 8; x++)
                for (int z = 0; z < 8; z++)
                {
                    object cell = Activator.CreateInstance(positionType, x, z);
                    bool player = (bool)Call(configuration, "Contains", cell, 8, 8, false);
                    bool enemy = (bool)Call(configuration, "Contains", cell, 8, 8, true);
                    Assert.That(player && enemy, Is.False);
                    if (player) friendlyCount++;
                }
            Assert.That(friendlyCount, Is.EqualTo(11));
            object outside = Activator.CreateInstance(positionType, -1, 0);
            Assert.That(Call(configuration, "Contains", outside, 8, 8, false), Is.False);
        }
        Set(configuration, "playerSide", Enum.Parse(sideType, "Left"));
        object boundary = Activator.CreateInstance(positionType, 3, 4);
        object diagonalOutside = Activator.CreateInstance(positionType, 3, 6);
        Assert.That(Call(configuration, "Contains", boundary, 8, 8, false), Is.True);
        Assert.That(Call(configuration, "Contains", diagonalOutside, 8, 8, false), Is.False);
        object trainerCell = Activator.CreateInstance(positionType, 1, 4);
        Assert.That(Call(configuration, "Contains", trainerCell, 8, 8, false), Is.False);
        Set(configuration, "deploymentRadius", 100);
        object enemyCell = Activator.CreateInstance(positionType, 7, 4);
        Assert.That(Call(configuration, "Contains", enemyCell, 8, 8, false), Is.False);
    }
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetType(name)).First(type => type != null);

    private Component Create(string name)
    {
        GameObject gameObject = new GameObject("Test " + name);
        gameObject.SetActive(false); // Invoke lifecycle explicitly without scene Start methods.
        objects.Add(gameObject);
        return gameObject.AddComponent(RuntimeType(name));
    }

    private static object Call(object target, string name, params object[] arguments)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            MethodInfo method = type.GetMethod(name, Fields | BindingFlags.DeclaredOnly);
            if (method != null) return method.Invoke(target, arguments);
        }
        throw new MissingMethodException(target.GetType().Name, name);
    }

    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
    private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static bool AnyActionActive => (bool)RuntimeType("BaseAction").GetProperty("IsAnyActionActive").GetValue(null);

    private Component CreateUnit(bool enemy = false, bool melee = false)
    {
        Component health = Create("HealthSystem");
        Component unit = health.gameObject.AddComponent(RuntimeType("Unit"));
        if (melee)
        {
            health.gameObject.AddComponent(RuntimeType("MoveAction"));
            health.gameObject.AddComponent(RuntimeType("SwordAction"));
        }
        Set(unit, "isEnemy", enemy);
        Call(health, "Awake");
        Call(unit, "Awake");
        foreach (Component action in health.gameObject.GetComponents(RuntimeType("BaseAction"))) Call(action, "Awake");
        return unit;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject gameObject in objects.AsEnumerable().Reverse())
        {
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component == null) continue;
                if (RuntimeType("BaseAction").IsInstanceOfType(component)) Call(component, "OnDisable");
                MethodInfo destroy = component.GetType().GetMethod("OnDestroy", Fields);
                destroy?.Invoke(component, null);
            }
            Object.DestroyImmediate(gameObject);
        }
        objects.Clear();
        foreach (ScriptableObject asset in assets) Object.DestroyImmediate(asset);
        assets.Clear();
        RuntimeType("BaseAction").GetMethod("ResetActiveActions", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    }

    [Test]
    public void LethalDamageWithoutSelection_RaisesDeathOnlyOnce()
    {
        Component health = Create("HealthSystem");
        Call(health, "Awake");
        int deaths = 0;
        EventHandler<Transform> handler = (sender, source) => deaths++;
        health.GetType().GetEvent("OnDead").AddEventHandler(health, handler);
        Call(health, "Damage", 100, null);
        Call(health, "Damage", 100, null);
        Assert.That(deaths, Is.EqualTo(1));
        Assert.That(Property(health, "IsDead"), Is.EqualTo(true));
    }

    [Test]
    public void SelectedUnitDeath_SelectsSurvivorThenClearsSelection()
    {
        Component manager = Create("UnitManager");
        Call(manager, "Awake");
        Component first = CreateUnit();
        Component second = CreateUnit();
        Call(manager, "Unit_OnAnyUnitSpawned", first, EventArgs.Empty);
        Call(manager, "Unit_OnAnyUnitSpawned", second, EventArgs.Empty);
        Component selection = Create("UnitActionSystem");
        Call(selection, "Awake");
        Call(selection, "Start");
        Assert.That(Call(selection, "GetSelectedUnit"), Is.SameAs(first));
        Call(first.GetComponent(RuntimeType("HealthSystem")), "Damage", 100, null);
        Call(manager, "Unit_OnAnyUnitDead", first, EventArgs.Empty);
        Assert.That(Call(selection, "GetSelectedUnit"), Is.SameAs(second));
        Call(second.GetComponent(RuntimeType("HealthSystem")), "Damage", 100, null);
        Call(manager, "Unit_OnAnyUnitDead", second, EventArgs.Empty);
        Assert.That(Call(selection, "GetSelectedUnit"), Is.Null);
        Assert.That(Call(selection, "GetSelectedAction"), Is.Null);
    }

    [Test]
    public void TurnChange_WaitsForActionAndCompletionIsIdempotent()
    {
        Component manager = Create("UnitManager");
        Call(manager, "Awake");
        Component turns = Create("TurnSystem");
        Call(turns, "Awake");
        Call(turns, "NextTurn");
        Assert.That(Call(turns, "GetTurnNumber"), Is.EqualTo(1));
        Type state = RuntimeType("UnitManager").GetNestedType("BattleState");
        Call(manager, "SetState", Enum.Parse(state, "Running"));
        Component unit = CreateUnit(melee: true);
        Component move = unit.GetComponent(RuntimeType("MoveAction"));
        int completions = 0;
        Call(move, "ActionStart", (Action)(() => completions++));
        Call(turns, "NextTurn");
        Assert.That(Call(turns, "GetTurnNumber"), Is.EqualTo(1));
        Call(move, "ActionComplete");
        Call(move, "ActionComplete");
        Assert.That(completions, Is.EqualTo(1));
        Assert.That(AnyActionActive, Is.False);
        Call(turns, "NextTurn");
        Assert.That(Call(turns, "GetTurnNumber"), Is.EqualTo(2));
        Call(move, "ActionStart", (Action)(() => completions++));
        Call(move, "OnDisable");
        Assert.That(AnyActionActive, Is.False);
        Assert.That(completions, Is.EqualTo(2));
    }

    [Test]
    public void RepeatedManagers_RemoveTheirStaticSubscriptions()
    {
        FieldInfo spawned = RuntimeType("Unit").GetField("OnAnyUnitSpawned", BindingFlags.Static | BindingFlags.NonPublic);
        int before = (spawned.GetValue(null) as Delegate)?.GetInvocationList().Length ?? 0;
        for (int i = 0; i < 3; i++)
        {
            Component manager = Create("UnitManager");
            Call(manager, "Awake");
            Assert.That((spawned.GetValue(null) as Delegate)?.GetInvocationList().Length, Is.EqualTo(before + 1));
            Call(manager, "OnDestroy");
            Assert.That((spawned.GetValue(null) as Delegate)?.GetInvocationList().Length ?? 0, Is.EqualTo(before));
        }
    }

    [Test]
    public void BattleResult_HandlesInitializationVictoryDefeatAndFinishedState()
    {
        Component manager = Create("UnitManager");
        Call(manager, "Awake");
        Assert.That(Call(manager, "GetBattleResult"), Is.Null);
        Component friendly = CreateUnit();
        Component enemy = CreateUnit(enemy: true);
        Call(manager, "Unit_OnAnyUnitSpawned", friendly, EventArgs.Empty);
        Call(manager, "Unit_OnAnyUnitSpawned", enemy, EventArgs.Empty);
        Type state = RuntimeType("UnitManager").GetNestedType("BattleState");
        Call(manager, "SetState", Enum.Parse(state, "Running"));
        Assert.That(Call(manager, "GetBattleResult"), Is.Null);
        Call(manager, "Unit_OnAnyUnitDead", enemy, EventArgs.Empty);
        Assert.That(Call(manager, "GetBattleResult").ToString(), Is.EqualTo("Won"));
        Call(manager, "SetState", Enum.Parse(state, "Won"));
        Assert.That(Call(manager, "GetBattleResult"), Is.Null);
        Call(manager, "Unit_OnAnyUnitDead", friendly, EventArgs.Empty);
        Call(manager, "SetState", Enum.Parse(state, "Running"));
        Assert.That(Call(manager, "GetBattleResult").ToString(), Is.EqualTo("Lost"));
    }

    [Test]
    public void MeleeOnlyUnit_CanEvaluateAnApproachingMove()
    {
        Component grid = Create("LevelGrid");
        Set(grid, "width", 8);
        Set(grid, "height", 8);
        Set(grid, "cellSize", 2f);
        Call(grid, "Awake");
        Component manager = Create("UnitManager");
        Call(manager, "Awake");
        Component enemy = CreateUnit(enemy: true, melee: true);
        Component player = CreateUnit();
        Type positionType = RuntimeType("GridPosition");
        object playerPosition = Activator.CreateInstance(positionType, 5, 5);
        Set(player, "gridPosition", playerPosition);
        Call(grid, "AddUnitAtGridPosition", playerPosition, player);
        Call(manager, "Unit_OnAnyUnitSpawned", enemy, EventArgs.Empty);
        Call(manager, "Unit_OnAnyUnitSpawned", player, EventArgs.Empty);
        object candidate = Activator.CreateInstance(positionType, 2, 2);
        object score = Call(enemy.GetComponent(RuntimeType("MoveAction")), "GetEnemyAIAction", candidate);
        Assert.That((int)score.GetType().GetField("actionValue").GetValue(score), Is.GreaterThan(0));
    }

    [Test]
    public void SharedDefinitions_KeepRuntimeHealthSeparateAndProvideActionStats()
    {
        ScriptableObject definition = ScriptableObject.CreateInstance(RuntimeType("MonsterDefinition"));
        ScriptableObject ability = ScriptableObject.CreateInstance(RuntimeType("AbilityDefinition"));
        assets.Add(definition);
        assets.Add(ability);
        Set(definition, "maxHealth", 200);
        Set(definition, "maxActionPoints", 5);
        Set(ability, "actionName", "Sword");
        Set(ability, "range", 2);
        Set(ability, "actionPointCost", 2);
        Array abilities = Array.CreateInstance(RuntimeType("AbilityDefinition"), 1);
        abilities.SetValue(ability, 0);
        Set(definition, "abilities", abilities);
        Component first = CreateUnit(melee: true);
        Component second = CreateUnit();
        foreach (Component unit in new[] { first, second })
        {
            Set(unit, "definition", definition);
            Call(unit, "Awake");
            Call(unit.GetComponent(RuntimeType("HealthSystem")), "Awake");
        }
        Call(first.GetComponent(RuntimeType("HealthSystem")), "Damage", 40, null);
        Assert.That(Call(first, "GetHealthNormalized"), Is.EqualTo(0.8f));
        Assert.That(Call(second, "GetHealthNormalized"), Is.EqualTo(1f));
        Assert.That(Call(first, "GetActionPoints"), Is.EqualTo(5));
        Component sword = first.GetComponent(RuntimeType("SwordAction"));
        Assert.That(Call(sword, "GetActionPointCost"), Is.EqualTo(2));
        Assert.That(Call(sword, "GetMaxSwordDistance"), Is.EqualTo(2));
        Assert.That(definition.GetType().GetField("maxHealth").GetValue(definition), Is.EqualTo(200));
    }
    [Test]
    public void Escape_IsBlockedOutsidePlayerTurnAndDuringActions()
    {
        Component manager = Create("UnitManager");
        Component turns = Create("TurnSystem");
        Call(manager, "Awake");
        Call(turns, "Awake");
        Assert.That(Property(manager, "CanEscape"), Is.False);
        Assert.That(Call(manager, "TryEscape"), Is.False);
        Type state = RuntimeType("UnitManager").GetNestedType("BattleState");
        Call(manager, "SetState", Enum.Parse(state, "Running"));
        Assert.That(Property(manager, "CanEscape"), Is.True);
        Set(turns, "isPlayerTurn", false);
        Assert.That(Call(manager, "TryEscape"), Is.False);
        Set(turns, "isPlayerTurn", true);
        Component action = Create("SpinAction");
        Call(action, "ActionStart", (Action)(() => { }));
        Assert.That(Call(manager, "TryEscape"), Is.False);
        Call(action, "ActionComplete");
        Assert.That(Property(manager, "CanEscape"), Is.True);
        Call(manager, "SetState", Enum.Parse(state, "Escaped"));
        Assert.That(Call(manager, "TryEscape"), Is.False);
    }
}
