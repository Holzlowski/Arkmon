**Arkmon** is a game prototype that blends classic monster-taming mechanics (inspired by *Pokémon*) with **grid-based tactical combat** and a rich, arcane world. You take on the role of an **Ark Hunter**, capturing and commanding magical creatures born from raw arcane energy.

## 🌌 World of Arkmon

In a realm shaped by ancient magical forces, mystical creatures known as **Arkmon** have emerged. Using enchanted vessels, skilled Ark Hunters can capture these beings and use their powers in battle, exploration, and discovery.

 ## 🕹️ Core Gameplay Features (Prototype)

- 🧪 **Experimental Grid-Based Combat**  
  Movement, attack ranges, and positioning happen on a tactical grid. Current prototype supports basic tactical mechanics and monster interactions.

- 🐉 **Monster Taming System**  
  Capture and train magical creatures called Arkmon, each with unique abilities and types.

- 🧠 **Branching Dialogue System**  
  Talk to NPCs using a dialogue system with choice-driven paths that affect the direction and outcome of conversations.

- 🌀 **Arcane-Themed Fantasy Setting**  
  A mystical world filled with magical creatures and ancient powers.

---

## 🌟 Vision: Dynamic Environments & Tactical Depth

The long-term goal of **Arkmon** is to make each battle feel unique by integrating **terrain, weather, and monster abilities** into the core combat mechanics:
One Example: 
- Ice-type Arkmon will be able to **freeze rivers or lakes** to create temporary ice bridges.
- Only certain monster types (e.g., Ice-types) will **move across them safely**.
- Other types might **slip** or face **movement penalties**, making battlefield control and environmental strategy crucial.
- The environment should become an **active element in combat**, not just a backdrop.

These mechanics are currently in **concept and design** and represent the **core vision** of what Arkmon aims to become.
 

## 🛠️ Built With

- Unity Engine
- C#
- Custom Combat Logic & Environment Interactions
- Dialogue System with branching paths

## Development setup

The project version is stored as `bundleVersion` in `ProjectSettings/ProjectSettings.asset` (currently `0.2.2`).

1. Install Unity Hub and **Unity 2022.3.17f1**, including the build support for your target platform.
2. Add this repository as a project in Unity Hub and open it. Allow Unity to import assets and restore the packages listed in `Packages/manifest.json`.
3. Open `Assets/Scenes/StartScene.unity` and press Play. Open `Assets/Scenes/BattleScene.unity` to inspect the tactical combat setup directly.
4. To build, use **File > Build Settings**, select the target platform, and choose Build. Both StartScene and BattleScene are already enabled in the build settings.

## Combat architecture

- `LevelGrid` tracks unit occupancy; `Pathfinding` calculates routes. Movement validation performs one route search per candidate tile.
- `Unit` owns its current action points and action components; `HealthSystem` owns current health and emits death once.
- `BaseAction` tracks active player and enemy actions. Completion is idempotent; disabling an active action releases its completion callback and turn lock.
- `UnitManager` registers units in Awake and evaluates battle results after initialization, at the end of a frame. A result locks the battle immediately; the scene transition waits for active actions to finish and then delays one second. If both teams are eliminated, defeat takes precedence.
- `UnitActionSystem` selects a surviving friendly unit when the current selection dies. With no survivors, selection and action are cleared. UI and grid visuals support that state.
- `TurnSystem` rejects turn changes during active actions and outside a running battle. `TurnSystemUI` disables the End Turn button accordingly.
- `EnemyAI` evaluates existing Shoot and Sword capabilities when scoring movement; melee-only units can score movement toward an opponent without requiring ShootAction.

## Monster and ability data

Create assets using **Assets > Create > Arkmon > Monster Definition** and **Ability Definition**. Assign a Monster Definition to the Unit component on a monster prefab. Set maximum health, maximum action points, and its ability definitions.

Ability `actionName` must match the corresponding action name exactly: `Move`, `Shoot`, `Sword`, or `Grenade`. Set range and action-point cost; attack definitions also provide damage. For example, a Sword definition can use range 1, cost 1, and damage 100. Movement uses range and cost, and ignores damage.

Definitions supply configuration for action components already attached to the prefab. They do not create components or replace the existing Basic Actions / Attacks UI arrays. Missing definitions retain existing prototype defaults. Current health and action points stay on each unit instance; shared assets are not mutated during combat.

## Verification

Open **Window > General > Test Runner**, select EditMode, and run `Arkmon.EditMode.Tests`. The regression tests cover death without a selection, duplicate lethal damage, replacement and empty selections, turn locking, repeated completion and cancellation, repeated manager subscriptions, battle-result decisions, melee-only movement evaluation, and separate runtime health with shared definitions. The tests use reflection because the existing runtime lives in Unity's predefined Assembly-CSharp assembly.

Also verify the following in Play Mode:

1. Enter and leave a battle at least three times; check the Console for callbacks to destroyed objects and confirm exactly one scene transition per battle.
2. Kill the selected friendly unit, then the final friendly unit. Check replacement selection, empty action UI, and return to the previous scene after defeat.
3. Eliminate the final enemy and check that the final attack completes before returning. Test a grenade eliminating both teams; defeat should take precedence.
4. During movement and attacks, repeatedly click End Turn. The turn must remain unchanged until completion. Disable or destroy an acting unit and verify that the turn lock is released.
5. Test movement and grenade arrival at low frame rates, and an enemy with MoveAction and SwordAction but no ShootAction.

## Current prototype limitations

Victory and defeat currently return to the previous scene through the same transition; there is no dedicated defeat screen, penalty, reward, or persisted battle result. Initialization assumes the scene's initial units register in Start; asynchronous spawning would need an explicit readiness signal. Melee approach scoring uses grid distance and is a simple heuristic, so it can make poor choices around obstacles. Ability assets configure existing actions rather than dynamically granting new ones. Play Mode scene transitions and low-frame-rate behavior still require engine validation.

Validation status: the combat sources and regression-test source passed a separate C# semantic compilation using installed Unity libraries and stand-ins for external package types. This does not verify a complete Unity build or test execution. The target Unity 2022.3.17f1 is unavailable on the current machine; an isolated attempt with another installed editor stopped at license validation before tests ran.

The local game design PDF in docs and its generation files in tmp/gdd are excluded from Git.
