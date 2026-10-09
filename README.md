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

The project version is stored as `bundleVersion` in `ProjectSettings/ProjectSettings.asset` (currently `0.1.3`).

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

## Current prototype limitations

Victory and defeat return to the previous scene through the same transition, without rewards, penalties, or persisted results. Initial units must register in Start. Melee approach scoring uses grid distance and can make poor choices around obstacles. Scene transitions and low-frame-rate behavior still require Play Mode validation.
