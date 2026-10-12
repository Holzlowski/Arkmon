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
        GameObject npc = GameObject.Find("NPC");
        Assert.That(npc, Is.Not.Null);
        Assert.That(npc.GetComponent<Rigidbody>().isKinematic, Is.True);
        Assert.That(npc.GetComponentsInChildren<Collider>().Any(c => c.enabled), Is.True);
        SpriteRenderer shepherd = npc.GetComponentInChildren<SpriteRenderer>();
        Assert.That(shepherd, Is.Not.Null);
        Assert.That(shepherd.enabled, Is.True);
        Assert.That(shepherd.sprite.name, Is.EqualTo("Shepherd-Paper"));
        Assert.That(shepherd.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Unlit"));
        Vector3 npcPosition = npc.transform.position;
        for (int frame = 0; frame < 30; frame++) yield return null;
        Assert.That(npc.transform.position, Is.EqualTo(npcPosition));
        Assert.That(Vector3.Dot(shepherd.transform.up, Camera.main.transform.up), Is.GreaterThan(0.999f));
        // Scene authors can reposition the NPC; validate his renderable presentation
        // without requiring one particular starting-camera composition.
        Assert.That(shepherd.gameObject.activeInHierarchy, Is.True);
        Assert.That(shepherd.bounds.size.x, Is.GreaterThan(0.1f));
        Assert.That(shepherd.bounds.size.y, Is.GreaterThan(0.1f));
        Assert.That(shepherd.sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null);
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
            Type deploymentType = RuntimeType("BattleDeployment");
            object deployment = deploymentType.GetProperty("Instance").GetValue(null);
            Assert.That(deployment, Is.Not.Null);
            Type managerBefore = RuntimeType("UnitManager");
            object managerInstance = managerBefore.GetProperty("Instance").GetValue(null);
            Assert.That(managerBefore.GetProperty("IsBattleRunning").GetValue(managerInstance), Is.False);
            Assert.That(deploymentType.GetProperty("Trainer").GetValue(deployment), Is.Not.Null);
            Transform opponent = (Transform)deploymentType.GetProperty("OpponentTrainer").GetValue(deployment);
            Assert.That(opponent, Is.Not.Null);
            Assert.That(opponent.GetComponentInChildren<SpriteRenderer>().sprite.name, Is.EqualTo("Shepherd-Paper"));
            Assert.That(((ICollection)managerBefore.GetMethod("GetEnemyUnitList").Invoke(managerInstance, null)).Count, Is.EqualTo(0));
            bool sawOpponentSummon = false;
            Type gridType = RuntimeType("LevelGrid");
            object grid = gridType.GetProperty("Instance").GetValue(null);
            int width = (int)gridType.GetMethod("GetWidth").Invoke(grid, null);
            int height = (int)gridType.GetMethod("GetHeight").Invoke(grid, null);
            foreach (int x in new[] { 0, width - 1 })
                foreach (int z in new[] { 0, height - 1 })
                {
                    object corner = Activator.CreateInstance(RuntimeType("GridPosition"), x, z);
                    Vector3 world = (Vector3)gridType.GetMethod("GetWorldPosition").Invoke(grid, new[] { corner });
                    Assert.That(world.x - 1f, Is.GreaterThan(6.52f));
                    Assert.That(world.x + 1f, Is.LessThan(30f));
                    Assert.That(world.z - 1f, Is.GreaterThan(0.39f));
                    Assert.That(world.z + 1f, Is.LessThan(22.56f));
                }
            Transform trainer = (Transform)deploymentType.GetProperty("Trainer").GetValue(deployment);
            Component paperVisual = trainer.GetComponent(RuntimeType("PaperCharacterVisual"));
            Assert.That(paperVisual, Is.Not.Null);
            SpriteRenderer trainerSprite = trainer.GetComponentInChildren<SpriteRenderer>();
            Assert.That(trainerSprite.sprite, Is.Not.Null);
            Assert.That(trainerSprite.sprite.name, Is.EqualTo("Prince-Paper"));
            Vector3 trainerWorld = trainer.position;
            Vector3 firstScale = trainerSprite.transform.localScale;
            float visualMovement = 0f;
            for (int frame = 0; frame < 12; frame++)
            {
                yield return null;
                visualMovement = Mathf.Max(visualMovement, (trainerSprite.transform.localScale - firstScale).sqrMagnitude);
                Assert.That(trainer.position, Is.EqualTo(trainerWorld), "Idle must not move the logical trainer.");
            }
            Assert.That(visualMovement, Is.GreaterThan(0.00000001f));
            object trainerPosition = gridType.GetMethod("GetGridPosition").Invoke(grid, new object[] { trainer.position });
            var pathType = RuntimeType("Pathfinding");
            object pathfinding = pathType.GetProperty("Instance").GetValue(null);
            Assert.That(pathType.GetMethod("IsWalkableGridPosition").Invoke(pathfinding, new[] { trainerPosition }), Is.False);
            object invalidCell = Activator.CreateInstance(RuntimeType("GridPosition"), -1, -1);
            Assert.That(deploymentType.GetMethod("TryPlace").Invoke(deployment, new[] { invalidCell }), Is.EqualTo(false));
            // Markings must actually sit above the rendered paving, not underneath it.
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            float floorTop = renderers.Where(r => r.name == "Arena paving").Max(r => r.bounds.max.y);
            Component[] gridVisuals = UnityEngine.Object.FindObjectsByType(RuntimeType("GridSystemVisualSingle"),
                FindObjectsSortMode.None).Cast<Component>().ToArray();
            Renderer[] visibleCells = gridVisuals.Select(v => v.GetComponentInChildren<Renderer>())
                .Where(r => r.enabled).ToArray();
            Assert.That(visibleCells.Length, Is.GreaterThan(0));
            foreach (Renderer cellRenderer in visibleCells)
                Assert.That(cellRenderer.bounds.min.y, Is.GreaterThan(floorTop + 0.01f));
            var cells = (IList)deploymentType.GetField("available", System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance).GetValue(deployment);
            while (!(bool)deploymentType.GetProperty("IsReady").GetValue(deployment))
            {
                Assert.That(cells.Count, Is.GreaterThan(0));
                object cell = cells[0];
                int countBefore = ((ICollection)managerBefore.GetMethod("GetFriendlyUnitList").Invoke(managerInstance, null)).Count;
                Assert.That(deploymentType.GetMethod("TryPlace").Invoke(deployment, new[] { cell }), Is.EqualTo(true));
                Assert.That(deploymentType.GetMethod("TryPlace").Invoke(deployment, new[] { cell }), Is.EqualTo(false));
                Assert.That(managerBefore.GetProperty("IsBattleRunning").GetValue(managerInstance), Is.False);
                Assert.That(((ICollection)managerBefore.GetMethod("GetFriendlyUnitList").Invoke(managerInstance, null)).Count,
                    Is.EqualTo(countBefore), "The monster must wait for the summon effect to arrive.");
                yield return null;
                GameObject effectObject = GameObject.Find("Monster summon effect");
                Assert.That(effectObject, Is.Not.Null);
                Material material = effectObject.GetComponentInChildren<Renderer>().sharedMaterial;
                Assert.That(material.color.r, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(material.color.g, Is.EqualTo(0.42f).Within(0.0001f));
                Assert.That(material.color.b, Is.EqualTo(0.1f).Within(0.0001f));
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((bool)deploymentType.GetProperty("IsSummoning").GetValue(deployment)
                    && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(managerBefore.GetProperty("IsBattleRunning").GetValue(managerInstance), Is.False);
                    if ((bool)deploymentType.GetProperty("IsOpponentSummoning").GetValue(deployment))
                    {
                        sawOpponentSummon = true;
                        Component cameraControl = (Component)UnityEngine.Object.FindFirstObjectByType(RuntimeType("CameraController"));
                        Assert.That(cameraControl.GetType().GetProperty("IsPresentationLocked").GetValue(cameraControl), Is.EqualTo(true));
                    }
                    yield return null;
                }
                Assert.That(deploymentType.GetProperty("IsSummoning").GetValue(deployment), Is.False);
            }
            Assert.That(sawOpponentSummon, Is.True);
            Component cameraAfter = (Component)UnityEngine.Object.FindFirstObjectByType(RuntimeType("CameraController"));
            Assert.That(cameraAfter.GetType().GetProperty("IsPresentationLocked").GetValue(cameraAfter), Is.EqualTo(false));
            yield return null;
            yield return null;
            Type managerType = RuntimeType("UnitManager");
            object manager = managerType.GetProperty("Instance").GetValue(null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(managerType.GetProperty("IsBattleRunning").GetValue(manager), Is.EqualTo(true));
            var friendlies = (ICollection)managerType.GetMethod("GetFriendlyUnitList").Invoke(manager, null);
            var enemies = (ICollection)managerType.GetMethod("GetEnemyUnitList").Invoke(manager, null);
            Assert.That(friendlies.Count, Is.GreaterThan(0));
            foreach (Component friendly in friendlies)
            {
                Assert.That(friendly.GetComponentInChildren<SpriteRenderer>().sprite.name, Is.EqualTo("Flaca-Sword-Body"));
                Transform paper = friendly.GetComponentInChildren<SpriteRenderer>().transform;
                Assert.That(paper.localScale.y, Is.InRange(0.48f, 0.52f));
                Assert.That(Vector3.Dot(paper.up, Camera.main.transform.up), Is.GreaterThan(0.999f));
            }
            Assert.That(enemies.Count, Is.GreaterThan(0));
            foreach (Component enemy in enemies)
                Assert.That(enemy.GetComponentInChildren<SpriteRenderer>().sprite.name, Is.EqualTo("Distelwolle-Paper"));
            Type selectionType = RuntimeType("UnitActionSystem");
            object selection = selectionType.GetProperty("Instance").GetValue(null);
            Assert.That(selectionType.GetMethod("GetSelectedUnit").Invoke(selection, null), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }
    }

    private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetType(name)).First(type => type != null);

    [UnityTest]
    public IEnumerator PaperPlayer_WalksAndFlipsWithoutTiltingOrMovingLogicalRoot()
    {
        yield return SceneManager.LoadSceneAsync("StartScene", LoadSceneMode.Single);
        yield return null;
        GameObject player = GameObject.FindWithTag("Player");
        Component controller = player.GetComponent(RuntimeType("PlayerController"));
        ((Behaviour)controller).enabled = false;
        Rigidbody body = player.GetComponent<Rigidbody>();
        body.isKinematic = true;
        Component visual = player.GetComponent(RuntimeType("PaperCharacterVisual"));
        Transform pivot = (Transform)visual.GetType().GetField("paperPivot", System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance).GetValue(visual);
        SpriteRenderer sprite = pivot.GetComponent<SpriteRenderer>();
        Vector3 right = Camera.main.transform.right;
        right.y = 0f;
        right.Normalize();
        Vector3 start = player.transform.position;
        float movement = 0f;
        Vector3 initial = pivot.localPosition;
        for (int frame = 0; frame < 15; frame++)
        {
            player.transform.position += right * 0.05f;
            Vector3 expected = player.transform.position;
            yield return null;
            Assert.That(player.transform.position, Is.EqualTo(expected));
            Assert.That(sprite.flipX, Is.False);
            Assert.That(Vector3.Dot(pivot.up, Camera.main.transform.up), Is.GreaterThan(0.999f));
            movement = Mathf.Max(movement, (pivot.localPosition - initial).sqrMagnitude);
        }
        Assert.That(movement, Is.GreaterThan(0.000001f));
        player.transform.position -= right * 0.1f;
        yield return null;
        Assert.That(sprite.flipX, Is.True);
        Assert.That(Vector3.Dot(pivot.up, Camera.main.transform.up), Is.GreaterThan(0.999f));
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        player.transform.position += cameraForward.normalized * 0.1f;
        yield return null;
        Assert.That(sprite.flipX, Is.True, "Vertical movement keeps the last facing.");
        LogAssert.NoUnexpectedReceived();
    }
}
