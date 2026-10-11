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

The project version is stored as `bundleVersion` in `ProjectSettings/ProjectSettings.asset` (currently `0.4.0-alpha.2`).

1. Install Unity Hub and **Unity 6.3 LTS, editor 6000.3.26f1**, including the build support for your target platform. On this machine the editor is installed at `C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe`; use Locate in Unity Hub if it is not listed automatically.
2. Add this repository as a project in Unity Hub and open it. Allow Unity to import assets and restore the packages listed in `Packages/manifest.json`.
3. Open `Assets/Scenes/StartScene.unity` and press Play. Open `Assets/Scenes/BattleScene.unity` to inspect the tactical combat setup directly.
4. To build, use **File > Build Profiles**, select the Windows platform, and choose Build. Both StartScene and BattleScene are already enabled in the scene list.

## Unity 6.3 migration

The project targets Unity `6000.3.26f1` with URP `17.3.0`, Unity UI `2.0.0` (including TextMeshPro), Input System `1.20.0`, and Cinemachine `2.10.7`. Cinemachine remains on the compatible 2.x API. Package versions and their resolved dependencies are pinned in `Packages/manifest.json` and `Packages/packages-lock.json`.

Unity's asset upgrades updated the scenes, prefabs, materials, texture import settings, and URP configuration. Render Graph is enabled; the obsolete compatibility-mode marker is cleared. TextMeshPro essential resources are updated while retaining the existing settings and default font assignment. These serialization changes account for the broad asset diff.

The input asset generates its wrapper at `Assets/Scripts/Player/PlayerInputActions.cs`, avoiding a duplicate class in the Assets root. InputManager enables and disables its actions with the component lifecycle and disposes the generated asset when destroyed. PlayerController uses Unity 6's `Rigidbody.linearVelocity` API.

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

Select PlayMode and run `Arkmon.PlayMode.Tests` to verify the StartScene startup and three repeated transitions between StartScene and BattleScene. Each battle must initialize both teams and a valid friendly selection. The tests provide virtual mouse and keyboard devices and clean up their scene and devices afterwards.

Also verify the following in Play Mode:

1. Enter and leave a battle at least three times; check the Console for callbacks to destroyed objects and confirm exactly one scene transition per battle.
2. Kill the selected friendly unit, then the final friendly unit. Check replacement selection, empty action UI, and return to the previous scene after defeat.
3. Eliminate the final enemy and check that the final attack completes before returning. Test a grenade eliminating both teams; defeat should take precedence.
4. During movement and attacks, repeatedly click End Turn. The turn must remain unchanged until completion. Disable or destroy an acting unit and verify that the turn lock is released.
5. Test movement and grenade arrival at low frame rates, and an enemy with MoveAction and SwordAction but no ShootAction.

## Current prototype limitations

Victory and defeat currently return to the previous scene through the same transition; there is no dedicated defeat screen, penalty, reward, or persisted battle result. Initialization assumes the scene's initial units register in Start; asynchronous spawning would need an explicit readiness signal. Melee approach scoring uses grid distance and is a simple heuristic, so it can make poor choices around obstacles. Ability assets configure existing actions rather than dynamically granting new ones. Play Mode scene transitions and low-frame-rate behavior still require engine validation.

Validation on Unity `6000.3.26f1`: all seven EditMode tests and both PlayMode tests passed. Asset checks loaded two scenes, 65 prefabs, and 72 materials without missing scripts or invalid shader references. A Windows x64 development build succeeded with zero build errors. Validation ran in batch mode; visual appearance, interactive combat, victory/defeat transitions, and low-frame-rate behavior still need the manual Play Mode checks above.

The local game design PDF in docs and its generation files in tmp/gdd are excluded from Git.

### Battle escape and return

Victory, defeat, and escape use the same return flow: wait for actions to finish,
then load the saved world scene and restore the player's saved position.
Escape is accepted only during the player turn while the battle is running and
no action is active. Repeated clicks cannot start additional return transitions.
NPC conversations and battles remain repeatable for prototype testing.

### NPC teams, deployment, and monster summoning

`BattleConfiguration` assets define the player and enemy unit prefabs, the player
deployment side (Left, Right, Bottom, or Top), a player deployment radius in grid
cells, and the enemy deployment depth in columns or rows. Create one via
**Assets > Create > Arkmon > Battle Configuration** and
assign it to an NPC's `Battle Configuration` field. Leaving that field empty uses
the BattleScene default: `Assets/Scriptable Objects/Battles/Default NPC Battle.asset`.
The example contains one friendly monster and two opponents. Team prefabs must
have the matching `Unit.isEnemy` setting. This is a prototype roster per encounter;
an inventory or persistent player team is not implemented yet.

BattleScene's `Battle Deployment` object handles setup. It replaces the existing
scene units with the configured teams; these scene units remain as a fallback if
configuration is invalid. The player clicks free, walkable blue cells within a
radius of three grid cells around the trainer on the left by default. The trainer
stands inside the arena, one cell inward from the grid edge. Its cell is reserved
and blocked for monster movement. The circle is clipped to the playable area.
Only free, walkable cells outside the enemy zone are selectable.
Other sides use the corresponding edge; enemy
units are distributed along the opposite edge. Multiple friendly monsters are
placed sequentially, with occupied cells excluded.

A stationary `Battle Trainer` prefab uses the young prince paper sprite.
A monster-colored pulse with a trail travels in an arc from the trainer's vessel to the chosen
cell, then expands into an arrival flash. The monster is instantiated after the
effect completes. `MonsterSummonEffect` exposes color, material, travel duration,
and arrival duration in the Inspector. The monster's `MonsterDefinition.summonColor`
controls the pulse; prefabs without a definition use `Unit`'s `Summon Color` field.
A private material copy prevents recoloring the shared effect material.
Camera controls remain available during
deployment, and the initial camera position focuses on the trainer's side.

The manager stays in `Deploying` until every friendly monster is registered.
Attacks, movement, turn changes, AI, and escape are blocked during deployment.
Repeated placement clicks during summoning are ignored. NPC battles remain
repeatable. Automated tests cover all four deployment sides and repeated
scene/deployment/summon cycles; effect appearance and camera framing require a
manual check in the Editor.

BattleScene uses an 11 by 10 grid with cell size 2 and world origin `(8, 0, 2)`.
This keeps the complete cell footprints inside the existing arena perimeter colliders.
`LevelGrid.Origin` is shared by grid visuals, mouse-to-cell conversion, spawning,
and pathfinding. When changing an arena, adjust its grid origin and dimensions
to the interior of the walls; the walls are not moved automatically.

### Paper characters and idle animation

The player and the battle trainer now use `Assets/Sprites/Characters/Prince-Paper.png`.
The friendly monster uses `Assets/Sprites/Monster/Flaca-Paper.png`; the enemy uses `Distelwolle-Paper.png`.
These are generated transparent illustrations based on the locally kept GDD's
paperlook direction: strong contours, simple color areas, an orange fire-cat with
cape and wooden sword, and a young prince in a green cloak carrying a vessel.
The earlier sprite files remain available. Generation prompts are recorded in
`docs/paper-art-generation.json`; the local GDD remains excluded from Git.

`PaperCharacterVisual` animates a separate `Paper Visual` child with gentle
breathing and bobbing. Figures stay upright in the camera's image plane and face
left or right only by flipping the sprite. The player faces its screen-horizontal
movement; purely vertical movement retains its previous facing. Monsters face
their active attack target, otherwise the nearest living opponent.
Idle motion eases out during movement or actions. Movement uses a rhythmic
step bounce and subtle squash, without rotating the figure. Both example monsters
use `Visual Scale = 0.5`, half their previous width and height.
Only the visual child changes; colliders, world position and grid occupancy do not.
Cycle duration, visual scale, and motion amounts are adjustable in the Inspector. These are
procedural paper animations, not skeletal rigs or frame-by-frame sprite sheets.
The existing unit Animator and attack callbacks are retained, with the old
body and equipment renderers hidden. Dedicated paper attack and walk poses are
still future work. The exploration player's C key still toggles the paper visual
and the diagnostic capsule.

### Courtyard and arena presentation

StartScene uses a warm limestone courtyard, terracotta keep, wooden benches and simple garden trees. BattleScene uses stone paving and low visual parapets; the original perimeter colliders and grid origin/dimensions are retained. Old decorative tree visuals are disabled. The generated scenery uses native Unity meshes and materials in `Assets/Art/SceneStyle`.

Each scene has its own Volume profile with ACES tonemapping, restrained bloom, desaturation and vignette. Directional lighting is warm with soft shadows; old baked lighting and environment reflections are cleared. New decoration has no colliders and does not introduce new gameplay obstacles.

The enemy prefab uses **Distelwolle**, a non-starter meadow sheep inspired by the original Schafi: cream wool, lavender face, ram horns, thistles and a small brass bell. Its summon color is muted lilac. It uses data-driven Rammstoß; wool shields remain future work. Art prompts are recorded in `docs/paper-art-generation.json`.

The StartScene NPC uses a generated shepherd paper sprite, the same camera-facing idle animation as the player, and an unlit transparent material. Its body is kinematic with gravity disabled: it is a stationary conversation partner with its existing BoxCollider and dialogue configuration retained. The former mesh presentation is hidden.

Grid markings are spawned with a separate visual height of 0.06 world units. This keeps deployment and action highlights above the arena paving (top at 0.035), while logical grid positions, mouse picking and unit movement stay on the original ground plane. PlayMode regression checks verify visible markings clear the paving and the NPC stays upright and stationary.
The shepherd position can be adjusted in StartScene. Regression checks validate its active sprite, material, upright facing and stationary body without imposing a fixed starting-camera composition.

### Distelwolle: Rammstoß

Distelwolle now has **Move** and **Rammstoß**, replacing its Shoot, Sword, Grenade and other test actions. The cat retains its test kit for the next ability pass (planned: non-elemental Krallenhieb and stronger fire Flammenschwert).

Rammstoß targets an opposing living unit 1–3 grid cells away in the same row or column. Every transit cell must be walkable and unoccupied, and the cell immediately behind the target must be inside the arena, walkable and empty. Diagonal charges and blocked pushes are not allowed. A hit currently deals 30 damage and costs 2 action points. These values live in `Assets/Scriptable Objects/Abilities/Ram.asset`; the Distelwolle definition retains 50 health, 3 action points and its lilac summon color.

The attacker winds up, approaches the target, pushes it one cell away, then remains on its former cell. A killing blow still lets Distelwolle claim the vacated cell. Movement uses MoveTowards and synchronizes grid registration immediately; the target clears its cell before the attacker advances. Disabling the action after impact settles both units on their final cells and releases the busy lock. A target that disappears or becomes invalid before impact cancels the charge and returns the attacker to its start cell. Turns remain blocked while the action runs.

Enemy movement evaluates possible charge lanes and approaches targets when out of range; Rammstoß is prioritized over movement when a valid target is in reach. The impact uses the existing screen shake, and the paper sprite faces its active ram target without tilting. Collision knockdown, stopping before an obstructed opponent, the prone state and a paid **Aufstehen** action are planned later, not part of this version. No elemental damage/resistance system is introduced here.

To try it, enter the NPC battle, place the cat, and end the player turn with a free lane and at most three cells between the monsters. To change balance, edit the Rammstoß and Distelwolle data assets in the Inspector.


### Death handling for paper monsters

Paper monsters never spawn the legacy skeletal ragdoll, including old scene overrides with missing skeleton references. Lethal damage resolves death before cosmetic/UI listeners. Dead units immediately leave the grid and manager, lose action points and deactivate, cancelling their actions. Dead units cannot move, spend points or reenter the grid; AI skips them. Animator parameters require a live controller.
