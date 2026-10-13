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

The project version is stored as `bundleVersion` in `ProjectSettings/ProjectSettings.asset` (currently `0.8.1`).

1. Install Unity Hub and **Unity 6.3 LTS, editor 6000.3.26f1**, including the build support for your target platform. On this machine the editor is installed at `C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe`; use Locate in Unity Hub if it is not listed automatically.
2. Add this repository as a project in Unity Hub and open it. Allow Unity to import assets and restore the packages listed in `Packages/manifest.json`.
3. Open `Assets/Scenes/TowerRoom.unity` and press Play for the demo opening. `StartScene.unity` starts directly in the courtyard; `BattleScene.unity` starts directly in tactical deployment.
4. To build, use **File > Build Profiles**, select the Windows platform, and choose Build. TowerRoom, StartScene and BattleScene are enabled in that order.

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

Victory and defeat currently return to the previous scene through the same transition; an outcome panel waits for confirmation; there is no penalty, reward or persisted battle result. BattleDeployment provides the readiness signal for the staged team summons; arbitrary mid-battle reinforcements are not implemented. Melee approach scoring uses grid distance and is a simple heuristic, so it can make poor choices around obstacles. Ability assets configure existing actions rather than dynamically granting new ones. Automated PlayMode checks cover repeated scene transitions and action endpoints; subjective presentation and interactive low-frame-rate behavior still require manual checks.

Original Unity migration validation on `6000.3.26f1`: all seven then-existing EditMode tests and both PlayMode tests passed. Asset checks loaded two scenes, 65 prefabs, and 72 materials without missing scripts or invalid shader references. A Windows x64 development build succeeded with zero build errors. Validation ran in batch mode; visual appearance, interactive combat, victory/defeat transitions, and low-frame-rate behavior still need the manual Play Mode checks above.

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

After all friendly monsters are placed, a visible shepherd on the opposite side
summons the enemy monsters sequentially with their own colors. Both trainers' cells
are reserved. The camera pans to the opponent, locks manual camera input for this
presentation, and returns to its previous position. Enemy units do not exist before
this sequence. The manager stays in `Deploying` until both teams are registered
and the camera has returned.
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
The friendly monster uses `Assets/Sprites/Monster/Flaca-SwordRig.png`; the enemy uses `Distelwolle-Paper.png`.
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

The enemy prefab uses **Distelwolle**, a non-starter meadow sheep inspired by the original Schafi: cream wool, lavender face, ram horns, thistles and a small brass bell. Its summon color is muted lilac. It uses the data-driven Rammstoß described below; wool shields remain a future design idea. Art prompts are recorded in `docs/paper-art-generation.json`.

The StartScene NPC uses a generated shepherd paper sprite, the same camera-facing idle animation as the player, and an unlit transparent material. Its body is kinematic with gravity disabled: it is a stationary conversation partner with its existing BoxCollider and dialogue configuration retained. The former mesh presentation is hidden.

Grid markings are spawned with a separate visual height of 0.06 world units. This keeps deployment and action highlights above the arena paving (top at 0.035), while logical grid positions, mouse picking and unit movement stay on the original ground plane. PlayMode regression checks verify visible markings clear the paving and the NPC stays upright and stationary.
The shepherd position can be adjusted in StartScene. Regression checks validate its active sprite, material, upright facing and stationary body without imposing a fixed starting-camera composition.

### Distelwolle: Rammstoß

Distelwolle now has **Move** and **Rammstoß**, replacing its Shoot, Sword, Grenade and other test actions. The cat uses non-elemental Krallenhieb and fire Flammenschwert, described below.

Rammstoß targets an opposing living unit 1–3 grid cells away in the same row or column. Every transit cell must be walkable and unoccupied, and the cell immediately behind the target must be inside the arena, walkable and empty. Diagonal charges and blocked pushes are not allowed. A hit currently deals 30 damage and costs 2 action points. These values live in `Assets/Scriptable Objects/Abilities/Ram.asset`; the Distelwolle definition retains 50 health, 3 action points and its lilac summon color.

The attacker winds up, approaches the target, pushes it one cell away, then remains on its former cell. A killing blow still lets Distelwolle claim the vacated cell. Movement uses MoveTowards and synchronizes grid registration immediately; the target clears its cell before the attacker advances. Disabling the action after impact settles both units on their final cells and releases the busy lock. A target that disappears or becomes invalid before impact cancels the charge and returns the attacker to its start cell. Turns remain blocked while the action runs.

Enemy movement evaluates possible charge lanes and approaches targets when out of range; Rammstoß is prioritized over movement when a valid target is in reach. The impact uses the existing screen shake, and the paper sprite faces its active ram target without tilting. Collision knockdown, stopping before an obstructed opponent, the prone state and a paid **Aufstehen** action are planned later, not part of this version. No elemental damage/resistance system is introduced here.

To try it, enter the NPC battle, place the cat, and end the player turn with a free lane and at most three cells between the monsters. To change balance, edit the Rammstoß and Distelwolle data assets in the Inspector.

### Death handling for paper monsters

Paper monsters do not spawn the legacy skeletal ragdoll. Distelwolle's obsolete ragdoll reference is cleared; the spawner also rejects paper visuals and missing skeleton references, including old scene overrides. Its death subscription is removed on disable.

A lethal hit marks the unit dead and resolves its death event before damage/UI notifications. The unit immediately leaves the grid, loses its action points, disables its actions, colliders, health canvas and unit logic (cancelling movement
and attacks), and is removed from the manager immediately. Its paper visual stays
active: over 0.35 seconds it sags vertically and darkens without tilting. After a
0.65-second defeat hold it shrinks into a pulse in its monster color. The pulse
travels back to its own trainer's vessel, reversing the summon sequence. The root
is hidden on absorption and destroyed after the return flight. This uses the
existing sprite procedurally; dedicated defeated artwork is not yet included.
The End Turn button continuously reflects the turn permission, so it unlocks after a recall even when no further action event fires. Turns, new actions, AI and escape wait for recalls; victory/defeat scene transitions
also wait for every active recall, including simultaneous defeated monsters.
Disabling deployment or unloading the scene cleans up effects and presentation locks. Dead units cannot spend points or update their grid position, and enemy AI skips them. Legacy Animator parameters are only invoked when a live controller is available.

Regression tests kill the actual Distelwolle prefab, inject an obsolete ragdoll reference, interrupt movement and charge with lethal damage, confirm the last opponent still triggers victory, verify upright defeat and both recall colors, and check cleanup and repeated sequential deployment.

Current battle regression validation on Unity `6000.3.26f1`: 39 EditMode and 36 PlayMode tests pass, covering Rammstoß, death interruption, defeat recalls, colored return effects, staged NPC summons, repeated scene entry, fire immunity, deferred spread, obstacle burnout and lethal burn damage. Additional rendered previews were inspected in an isolated project copy.

### Fire, burning and the cat's abilities

Kafla now uses **Krallenhieb** (15 direct non-elemental damage, 1 action point) and
**Flammenschwert** (25 direct fire damage to monsters, 2 action points). Both target
adjacent cells, including diagonals, and show a short paper-style strike effect.
The old Shoot/Sword/Grenade and other test abilities are removed from its prefab.
Movement, Escape and Abwehr are in the basic menu. Glutfunke adds a nearby fire
fan; its cover rules and values are documented under Tactical demo below.
Monster and ability values are in
`Assets/Scriptable Objects/Monsters/Flaca.asset` and the corresponding ability assets.
Kafla's `immuneToBurning` prevents the Burning status; direct fire damage still applies.

`Assets/Scriptable Objects/Battles/Fire Rules.asset` defines the initial balance:
25% monster ignition chance on fire hits, 3 damage ticks of 5 damage, 20% spread
chance to monsters and 50% to vegetation. Flammenschwert damages obstacles directly
and always ignites surviving vegetation. Stones can take direct damage but cannot ignite.
Hitting an already burning target successfully refreshes its duration; damage does
not stack. There is no elemental resistance multiplier or immunity to direct fire
damage in this pass.

A full round completes when the enemy turn returns to the player. BurningSystem
snapshots existing fires, collects four-neighbour exposure (no diagonals), deals
existing fire damage and then attempts new ignitions. Each target gets one spread
roll even with multiple sources. Newly spread fires wait until the next full-round
boundary for damage and propagation. Spread can affect either team and can move
between monsters and vegetation in both directions. Spread does not refresh an
existing fire. A source may spread on its final damage tick before it burns out.
Small upright orange/yellow flames show the status and disappear on expiration or
death. Lethal fire damage uses the same defeat pose and recall as attack damage.

BattleScene contains a small **Burnable arena garden** with trees, bushes and stones.
These are actual single-cell movement obstacles; vegetation has 15 health and
opens its field after burning out. Remaining physical obstacles are checked before
making the cell walkable. Add `BurningObstacle` to a single-cell prop and select
Vegetation or Stone to extend this setup. Outside-arena decorative scenery remains
visual-only; multi-cell obstacles, dousing/extinguish actions and fire persistence
between battles are not implemented. The exploration courtyard and tower room now use the same art direction.

To try it: deploy Flaca, move beside a tree or bush, open ATTACKS and select
FLAMMENSCHWERT. End both sides' turns to see burn damage and possible spread. Change
balance in Fire Rules or the ability assets rather than in action code.

### Paper sword swing

Flaca's `Flaca-SwordRig.png` is a non-destructive generated variant of the original
cat illustration. Unity slices it into body, arm and wooden sword sprites; the
original image remains available. `PaperSwordAnimation` joins the parts below the
paper pivot. The tightly cropped body sits 0.04 world units above the logical root
to keep its feet clear of the arena paving. Only the shoulder and wrist rotate in the camera plane, with the
whole limb assembly mirrored for left/right attacks. Logical position, colliders
and the upright body are unaffected; breathing, walking, defeat and recall still
apply to the complete visual child.

Flammenschwert winds up, swings through contact at 0.42 seconds, then recovers;
the full action lasts 0.85 seconds. The gameplay hit explicitly marks the impact
pose so damage and ignition match the swing. A warm sword tint and short trail
follow the actual blade tip, replacing the earlier projected arc for this rig.
Cancellation or death restores the idle limb pose and clears the trail. Krallenhieb
keeps its existing short strike effect; it does not swing the sword. Generated
art provenance and the full built-in image-generation prompt are recorded in
`docs/paper-art-generation.json`. Tests cover both facing directions, delayed
contact damage, stable logical position/upright body, recovery and death cancellation.

### Paper arena obstacles

BattleScene's six gameplay obstacles now use illustrated tree, thistle bush and
limestone rock sprites in `Assets/Sprites/Environment`. Decorative courtyard
trees use the same tree artwork. `PaperPropVisual` turns only the visual child
toward the main camera; the original root position, obstacle layer and colliders
remain fixed. Native sprite slices remove transparent margins and place the pivot
at the base. Trees are 3.7 units high, bushes and rocks 0.9 units high; courtyard
trees are 5.8 units high. Existing 3D foliage renderers are hidden.

Vegetation still burns and releases its blocked cell when destroyed; stone stays
fireproof. Paving, walls and garden edging retain their spatial geometry. Trees, bushes and rocks are static cutouts with no wind animation or dynamic cast shadows. Full
built-in generation prompts are recorded in `docs/paper-art-generation.json`.


### Meadow landscape and moving sky

BattleScene's arena has warmer paving and stone borders, taller paper trees and
an illustrated crate over the existing crate collider. A 400 by 400 unit decorative
meadow surrounds it: rolling grassland, golden fields, tree groups, thistle
bushes and paper wildflowers. The ground uses a native mesh with vertex colors; its
height stays flat near the arena. None of the surrounding vegetation or terrain
adds gameplay colliders. Distant colors blend into light atmospheric fog.

`BlueSky.shader` draws a blue horizon-to-zenith gradient and a subtle sun. Sixteen
paper clouds move at different gentle speeds via `CloudDrift`, wrap beyond the
arena and face the camera. The sky is visible when the camera framing includes
the horizon; the normal close battle view emphasizes the board. The landscape is
finite decorative scenery, not an explorable level. The exploration courtyard and tower room now use the same art direction.

### Animated paper fire and sword polish

`Assets/Resources/Paper Fire.asset` supplies six fire animation frames, sprite
material, frame rate and the soft sword-trail material. `BurningVisual` uses three
flames for vegetation and two smaller flames for monsters, with different frame
phases and rates. Flames follow the upright paper layer, stay in front of the
artwork and disappear on burnout or death. Cosmetic variation uses a local
`System.Random`, so it does not change ignition/spread random rolls.

Flaca's sword arm now attaches beneath the cloak instead of drawing its shoulder
cap over the chest. A native `Flaca-Forearm.asset` reuses the same texture pixels
for the hand and forearm in front of the body, while the shoulder stays behind it.
The swing has a brief follow-through before its smooth return.
The blade-tip trail uses sampled points along the curved swing, rounded corners,
soft edges and fading transparency, including when a slow frame skips poses.
Contact time, action-point cost, damage, ignition and recovery locks are unchanged.
Tests cover frame animation/cleanup, independent cosmetic randomness, moving
clouds across scene loads and curved trails when rendered frames are skipped.


`MeadowVegetation` fills the surrounding hills with green grass and taller golden
stalks with small grain heads. Broad field regions blend into grassland; the arena
and a margin around its walls stay clear. The wind shader bends the tips in gentle
waves while the bases remain fixed. Decoration adds no gameplay colliders or
burning targets.

The component generates meshes in 40-unit chunks, with denser nearby vegetation
and simpler distant LODs. The meshes are transient rather than large committed
assets, release their CPU copies after upload and are destroyed on disable/scene
unload. The seed and spacing are editable on `Living grass and golden fields`.
These settings and terrain sampling currently match this specific BattleScene.

### Knockdown and standing up

Rammstoß still pushes a living target one cardinal cell and claims its old cell
when the destination is free. An obstacle or the board boundary behind the target
now causes knockdown instead: the target keeps its cell, and Distelwolle stops
on the preceding cell. Another monster behind the target blocks the attack.
A killing blow still lets Distelwolle claim the vacated cell.

Knocked-down monsters keep occupying their field and remain vulnerable to damage
and burning. Their only available action is Aufstehen (1 AP), selected automatically
for the player and available to the AI. Click the monster's own highlighted field
to stand up. The action locks turn changes for 0.35 seconds; interruption leaves
the monster down. The upright paper silhouette flattens and recovers without
rotating the collider. This prototype pose uses deformation rather than new artwork.
Regression tests cover obstacle/border targeting, blocked lanes, lethal hits,
action-point costs, interrupted standing, player selection, upright paper pose
and AI standing before attacks.

### Shared paper interface

BattleScene and StartScene now share the style asset `Assets/Resources/Paper UI.asset`.
It supplies cream paper panels, sage answer/end-turn buttons, selected orange cards,
illustrated action/status icons, portraits and the existing LiberationSans font.
`PaperUIFactory` constructs anchored Unity UI elements; the existing battle and
dialogue controllers bind their views in Start. Legacy scene/prefab children are
hidden during Play Mode rather than rewriting scene geometry or stored dialogues.
The style asset can be edited in the Inspector. `Arkmon > Configure paper UI assets`
recreates the native sprite slices from the two UI atlases and existing character
textures. Full built-in image-generation prompts are in `docs/paper-art-generation.json`.

The command bar separates Aktionen (movement, standing and escape) from Attacken
using paper category tabs, with AP costs, hover explanations and an orange
selected card. Up to four cards are shown per page; additional abilities are
available through previous/next buttons. Switching categories or pages selects
the first visible action. Knockdown returns to Aktionen and disables Attacken. Unaffordable actions, enemy turns and
ongoing actions/recalls disable cards; callbacks recheck availability before acting.
Monster cards show names, current/max HP and graphical AP diamonds. Sprite-less
health fills resize their actual rectangle after damage, both on cards and above
monsters; a PlayMode regression test checks rendered mesh width, not just HP values.
Small world labels and the cards show burning duration and knockdown, updating from runtime
state. An action-in-progress hint and deployment instructions use the same paper
style. The turn tab reports the controlling side and full round number.

Dialogs show an NPC portrait, speaker name, text and vertically arranged choices.
Mouse hover and keyboard selection highlight choices in sage. The existing NPC
name, dialogue content, branching and battle configuration are preserved. E first
reveals a typing line, then advances it; Esc or the close button cancels the
conversation, clears selection and releases player movement. More than three
choices generate extra buttons rather than indexing a fixed three-button array.

Visual review rendered the actual Unity interface at 1600x900, 1280x720 and
1200x900 (4:3). The layouts target desktop landscape screens. The current portrait
set covers Flaca, Distelwolle and the shepherd NPC; additional characters will
need portrait assets. The surrounding world was not redesigned in this UI change.

UI validation: all 39 EditMode and 36 PlayMode project tests passed; the four
UI tests were repeated after the final callback/busy-state changes, and the
Unity render review covered the three landscape resolutions listed above.

Category-tab validation: the five UI tests and selected-monster knockdown/standing
regression passed. A render review confirmed the tabs at 16:9 and 4:3; paging
was checked with six attacks, and an unassigned attack list was checked separately.

Health-bar validation: all six paper UI PlayMode tests passed, including rendered
width after damage for both the HUD card and the world-space bar.

### Castle courtyard, encounters and sound

StartScene has a walled castle courtyard with corner towers, gate and keep
battlements. Paper trees replace the visible legacy round tree meshes; their
existing collision remains. Beyond the walls, a translated copy of the battle
meadow supplies rolling grass/golden fields, flowers, paper trees and drifting
clouds. Both scenes share the meadow sky; the StartScene camera now renders the
skybox. `Arkmon > Configure castle courtyard` rebuilds this scene presentation.
The castle architecture is still a simple geometry prototype, and the environment
is decorative rather than a traversable open world.

Battle trainers use a grid anchor one cell outside opposite edges. Their visible
figures stand 1.4 metres beyond the combined outer bounds of the arena parapets,
so their illustrations clear the decorative stone ring as well as the grid.
The deployment circle keeps its grid anchor, preserving selectable cells despite
the added visual clearance. Trainers no longer consume or block an arena cell.
Movement paths avoid all occupied monster cells, including allies, and cannot
cut diagonally through blocked corners. Movement validates a requested destination
again before starting. Monster footprints currently remain one grid cell;
multi-cell monsters will need footprint-aware occupancy and swept-path checks.

`SceneLoadManager` closes an original, stylized lamp-shaped opening over 0.7 seconds,
loads the next scene behind full black, then opens it over 0.7 seconds. The same
transition is used on return, restoring the saved exploration position. UI input,
exploration movement and deployment clicks are locked during the transition;
duplicate transition requests are ignored. The overlay uses native Unity UI mesh
geometry and is destroyed after the transition.

`SoundManager` plays original sampled courtyard and battle loops: a brighter
adventure theme at 104 BPM in the courtyard, with flute, harp, strings and cello; and a 160 BPM battle arrangement with horns, fast strings, cello, timpani and
orchestral drums. The courtyard has eight bars; battle music has 32 bars, a quieter
flute-led middle and a full reprise.
The destination music starts crossfading when the lamp begins closing; loading
the destination scene keeps the same track rather than restarting it.
One persistent manager owns music/effect sources
and unsubscribes its events on destruction. Music/effect volume is configurable
on the component. Ten cues accompany summon/recall, movement,
standing, claw/fire sword/ram actions and hit events. Flammenschwert uses a separate
air/fire sweep on action start and a layered low impact, roar and crackling
`fire-hit` cue on the actual melee hit, including hits against vegetation.
Colored shrinking spark
particles accompany summon and recall and are cleaned with the existing effect.
Summon and recall use airy, softened/reversed sample textures with blurred glassy
shimmer at lower levels than attack sounds. A separate soft puff marks completed
materialization, without playing during travel or recall. Current runtime music and effects contain no oscillator layers.
Seven action cues now use recorded sword, impact, cloth, grass-footstep, fireball
and crackling layers. Sources and licenses are recorded in
[docs/audio-sources.json](docs/audio-sources.json).

Audio assets are under `Assets/Resources/Audio`. Regenerate them with
`python tools/compose_prototype_audio.py` (requires NumPy), then
`python tools/mix_recorded_effects.py` (requires NumPy, SoundFile and the source
recordings in ignored `Builds/sample-audio`) to restore recorded action cues.
The courtyard now uses an eight-bar loop of the accepted sampled preview.
The 20-second original preview uses GeneralUser GS samples via
`python tools/render_sample_preview.py path/to/GeneralUser-GS.sf2` (NumPy and
tinysoundfont). Output: `Builds/courtyard-sampled-preview.wav`; the courtyard loop is derived from this arrangement. Raw recordings, the SoundFont and local dependencies stay in `Builds`.
Tests cover ally/enemy path occupancy, blocked diagonal
corners, trainers outside the grid, repeated lamp transitions and return position,
one audio manager, and spark color/cleanup. Native Unity render previews are kept
in ignored `Builds`, alongside local test results.

Atmosphere validation on Unity 6000.3.26f1: 42 EditMode tests passed. The combined
38-test PlayMode run passed 37 tests and exposed an audio warning in empty cleanup
scenes; after disabling/stopping audio sources there, all 11 affected scene/UI/flow
tests plus the native render review passed. The final renders cover the player's
courtyard view, the surrounding fields and the lamp-shaped opening. Music PCM
checks confirm unclipped peaks and small loop-boundary discontinuities. No new
player build was produced for this change.

Fantasy-audio/ring refinement: three focused PlayMode tests and a native arena
render passed. Tests check both trainer illustration bounds against the stone
parapets, battle music selection while StartScene is still active during the
closing iris, and `fire-hit` selection after an actual flame-sword vegetation hit.
Both new music loops and the fire impact have PCM peaks at 0.84 without clipping.

### NPC summoning focus and magic refinement

The shepherd mirrors its paper illustration toward the player within three metres
and immediately when spoken to, keeping its logical rotation upright. A small violet crystal charm is drawn into its belt pouch; the battle trainer
summons and recalls monsters through it, with no separate orb geometry. The default introductory battle deploys one Distelwolle. Other battle
configurations can still define larger teams.

Summon/recall sparks use an additive four-point shader and particle sizes independent
of the tiny travelling pulse scale. Larger particles, continuous emission and
start/arrival bursts preserve each monster's summon color. Flammenschwert has a
broader curved trail and a short three-flame paper impact with glowing embers on
actual contact. Damage, range and ignition rules are unchanged.

Latest targeted validation on Unity 6000.3.26f1: all 11 PlayMode tests passed,
covering NPC facing/sphere, one-opponent configuration, transitions, summon sparks,
fire combat and a native visual review. The simultaneous lethal-burn recall test
explicitly spawns a second opponent to retain multi-monster coverage. Native
screenshots in ignored `Builds` show the sphere, sparks and flame sword impact.


Summoning presentation: the current Prince-Paper character temporarily changes to
`Assets/Resources/Player-Summoning.png`, holding his existing round lantern forward.
The effect originates at the raised lantern and the resting sprite is restored on
completion or cancellation. Spark speed varies from 2.8 to 4.2 m/s with a 0.85-second
lifetime, spreading beyond the travelling sphere. Distelwolle summons and recalls
in white. The NPC sprite integrates one violet crystal charm instead of two
runtime sphere objects. Both sprite edits use the built-in image generation tool.

After regenerating prototype audio, restore sampled courtyard music with
`python tools/render_sample_preview.py path/to/GeneralUser-GS.sf2 --install-courtyard`.


Battle audio refinement: `tools/render_battle_audio.py` renders the original
48-second orchestral battle loop and two 0.72-second softer summon/recall cues.
Run `python tools/render_battle_audio.py path/to/GeneralUser-GS.sf2` after the other
audio generators to install these three files. Existing WAV metadata and resource
names are preserved, including battle music starting during the closing transition.
Local previews are in ignored `Builds`. SoundFont and recording provenance is in
`docs/audio-sources.json`; the reference video was not available for listening.
Validation: all three targeted atmosphere/transition PlayMode tests passed after
native Unity import. PCM checks confirm expected duration, stereo channels, no
clipping, matching runtime/preview music and continuous loop endpoints.

### Paper sprite contact shadows

`PaperGroundShadow` adds a soft, warm oval on the ground below paper figures and
props while keeping their unlit illustration colors. Characters and obstacle
visuals attach it automatically, including runtime-spawned trainers and monsters.
Scene paper renderers also receive it; clouds are excluded. The procedural shader
is in Resources so it remains available in player builds. Shadow geometry has no
collider and shares one mesh/material across all instances.

Shadows follow movement and sprite width, disappear with hidden sprites and shrink
with monster recalls. Ground height is sampled after movement, excluding the
figure's own colliders. Grid highlights stay above the ground shadow. This is a
stylized contact ellipse, not a directional silhouette cast by the sun.
Decorative paving bounds lift shadows above the slightly raised visual tiles even
where the physics collider remains on the original ground plane.
Validation on Unity 6000.3.26f1: all five targeted PlayMode tests passed, including
ground-height/self-collider handling, movement, hidden/faded sprites, repeated scene
transitions and a native render review of the courtyard and battle.


Astral summoning audio: run
`python tools/render_astral_summon.py path/to/GeneralUser-GS.sf2` after the other
audio renderers to install the current summon, recall and summon-arrive clips.
This replaces the earlier two-note vibraphone cue. Sources remain recorded cloth,
air and sampled glockenspiel, shaped into a floating texture and airy puff.
`MonsterSummonEffect` plays the arrival cue after materialization completes, just
before the monster is spawned. The local combined preview uses default timing
(0.65-second travel plus 0.3-second arrival) and is stored in ignored `Builds`.
All four targeted atmosphere PlayMode tests pass, including the arrival cue timing
and its absence during recall. PCM checks confirm unclipped, faded stereo outputs.

### Battle presentation flow

Deployment now shows only the placement prompt and highlighted cells. Combat cards,
turn controls and world health/AP bars appear when both teams are registered and
combat is running. CanvasGroups hide presentation without stopping UI controllers.
The placement label stays inside its paper tab at different screen sizes.

`BattlePresentationCamera` temporarily takes over the real camera for the player's
lantern pose, follows the travelling summon pulse, holds the arrival, then shows
the opponent summoning. It returns to the arena centre and releases Cinemachine
and manual controls. Paper scenery blocking the shot is temporarily faded and
restored, including on cancellation. During combat, the same check keeps living
monsters visible behind foreground paper scenery. The wider, higher overview
frames front-row monsters above the command bar. Gameplay colliders and logical positions do
not change. With larger teams, each monster receives its own summon shot.

Monster cards have separate burning and knockdown badges, so both can be visible.
Burning shows remaining rounds; hover, keyboard focus or click explains damage and
spread. Knockdown explains that standing costs 1 AP and does not expire by itself.
`PaperHitFeedback` reads actual health lost to display rising damage numbers and
briefly flash/recoil the illustration; the unit's logical transform remains fixed.
This also covers burn damage. Popups fade and remove themselves after 0.8 seconds.
Damage numbers project the hit position into a screen overlay, keeping their font
size readable independently of camera zoom.

After victory, defeat or escape, `UnitManager` waits for actions and recalls to
finish before opening `BattleResultPanel`. The player chooses **Zurück zum Hof**
(or Enter/Escape); combat no longer returns automatically after one second.
The lamp transition restores the saved exploration position. The original NPC
then gives a short result-specific dialogue once, using the existing dialogue UI
and movement lock. The result is consumed on return; repeated practice battles
remain available and no rewards or persistent monster injuries are introduced.

Presentation validation on Unity 6000.3.26f1: all 46 selected PlayMode tests pass,
covering the new camera/phase UI, simultaneous status badges, damage feedback,
victory/defeat/escape confirmation and one-time return dialogues, alongside fire,
ram, recall, health-bar, shadow and repeated-scene regressions. Native rendered
reviews cover placement, both trainers, travelling effects, overview, conditions,
damage numbers, result panel and return dialogue. The overview test checks that
front-row monsters project above the command bar. Native review outputs stay in
ignored `Builds`; no player build was produced for this change.
All 42 EditMode regressions also pass. The result modal locks camera controls until
confirmation; scene transitions keep camera input locked as well.

## Demo opening: tower room and examination courtyard

Start the demo from `Assets/Scenes/TowerRoom.unity`, now the first enabled build scene.
The opening thought introduces the examination before the Arkanmeisterin's arrival.
Move with WASD and use E near the wardrobe, window or door. The wardrobe equips
his parents' green mantle and changes the player's illustration; the door requires
that preparation before the lamp transition takes him into the courtyard.
`DemoOpening` retains preparation within the current play session, including repeated
practice battles. Progress resets when a new play session begins; no save file is written.
Opening StartScene directly remains supported for quick combat iteration.

The courtyard is 48 metres wide: tower and stable on the left, fountain and garden
in the middle, John by the practice area on the right and an exterior gate at the
far right. The gate currently offers dialogue rather than another playable region.
Castle walls use 3D geometry with a world-space painted limestone shader, while
furniture uses generated paper illustrations. The tower window is a real opening
onto the existing meadow scenery and sky, with moving clouds.

Kafla is the displayed name of the fire cat. Existing Flaca asset filenames and
GUIDs remain to preserve prefab, ability, sprite-rig and UI references.
John's summoning illustration raises the existing violet crystal charm from his
pouch; the effect starts at that charm and his resting sprite is restored afterwards.
Generated assets: Resources/John-Summoning.png, Resources/Prince-Room.png,
Resources/Prince-Mantle.png (copy of the existing dressed sprite), and
Art/SceneStyle/DemoProps.png and DemoStone.png. The source prompts are recorded in
ignored Builds/demo-art-prompts.txt. Images were produced with built-in ImageGen.

`Arkmon/Build demo opening` regenerates the demo scenes from the current courtyard
systems and BattleScene meadow. This editor command replaces courtyard geometry;
commit or back up custom scene changes before rerunning it. The gameplay path is a
prototype: opening uses a camera move and dialogue, not a frame-animated waking pose;
the examination can still be repeated and does not advance a persistent story.
Current committed version remains 0.8.1; next proposed feature version is 0.9.0.

Demo validation: all 47 repository PlayMode tests passed in the broad run. A later
focused run passed all 14 opening/presentation/transition tests, followed by all
3 final opening and rendered-review checks. All 42 EditMode tests pass. No standalone player build was produced.

## Exploration visual polish

TowerRoom and StartScene now use separate morning Volume profiles. Their stone
floors have lower texture contrast than the walls, with a soft painted warm light
pool near the tower window. This is an illustrated lighting treatment, not a
physically simulated beam. The battle Volume and scenery materials remain separate.

The wardrobe is closer to the room's central view, while the arched courtyard door
is mounted on the right wall. Their interaction points follow the furniture.
Exploration uses CinemachineFramingTransposer; the opening camera move now animates
that component's distance and focus, restoring its resting framing after 1.2 seconds.

Generated sprites `DemoDoor.png`, `DemoWorkbench.png` and `DemoFountain.png` replace
the placeholder blocks. The courtyard has distinct garden plots and tinted practice
paving beside John; furniture stays along the rear edge, leaving the east-west path
open. The fountain water is painted into its static illustration, not animated.

`Arkmon/Polish demo rooms` updates only named presentation objects in the two scenes.
`Arkmon/Build demo opening` also applies this polish after rebuilding the scenes.
Terrain materials clip a scene-specific world rectangle so decorative hills cannot
cover the tower room or courtyard; the default battle terrain has no cutout.
Built-in ImageGen source prompts are recorded in ignored Builds/demo-polish-prompts.json.

The tower also has a visible ceiling with warm timber beams. The wardrobe and door
have dedicated reachable interaction points. Seven focused demo,
scene-transition and shadow PlayMode checks passed; an additional rendered review
checks the visible player and fountain presentation. The final room/fountain images
are in ignored Builds/demo-polished-room.png and demo-polished-fountain.png.
No commit, version increment or standalone player build was made for this polish.

## Painted tower-window vista

The courtyard exit is mounted on the tower room's right wall. Its illustration has
fixed wall orientation and its interaction point is inside the room; ordinary
freestanding paper props continue facing the camera.

The tower window now shows `PaintedWindowVista.png`, with transparent
`WindowFieldHills.png` and `WindowClouds.png` layers. All scenery depicts distant
land below the tower; there are no wheat stalks immediately outside the window.
The room's existing procedural landscape is inactive to avoid overlap with the art.
`WindowVistaParallax` follows camera displacement by different small amounts per
layer after Cinemachine updates, and clouds drift slowly. Motion is bounded to
prevent showing panel edges. The real window frame remains stationary and masks
the illustration; this is a painted vista, not a new explorable region.
Built-in ImageGen prompts are in ignored Builds/window-vista-prompt.txt and
Builds/window-parallax-prompts.json. No commits or version increment were made.

## Fantasy battle music preview

tools/render_battle_fantasy_preview.py renders an original 36-bar 6/8 orchestral sketch
with the existing GeneralUser GS soundfont and dependencies from the battle renderer.
Run it with the soundfont path as its first argument. Output is the ignored
Builds/battle-fantasy-preview.wav (about 62 seconds including decay). It progresses
from flute/harp to strings/timpani and a horn/choir climax. It does not replace
runtime audio. This is a listening sketch, not a seamless production loop; sampled
instrument realism is limited by the existing soundfont. Provenance is in
docs/audio-sources.json.

## Courtyard fountain and residence tower

The courtyard fountain uses DemoFountain-Side.png, a transparent watercolor sprite
with shallow basin ellipses matching the exploration camera. The previous fountain
image is retained as source art. Water remains part of the static illustration.
The player's west tower is now an 11-unit octagonal stone body with a 3.6-unit
pointed slate roof, stone cornices and two framed upper windows. Its footprint
remains behind the courtyard path. ConfigureDemoPolish persists generated mesh
assets and rebuilds the named tower idempotently; the painted castle stone shader
is reused. The existing entrance, player arrival and gameplay systems remain in place.
The built-in ImageGen fountain prompt is saved in ignored
Builds/courtyard-tower-fountain-prompt.json.

## Tactical demo and waking sequence

The first TowerRoom visit opens with an occupied-bed paper sprite, a short morning
fade and a camera focus on the bed. The standing player is hidden and input is
locked during this sequence. The occupied bed then returns to the existing empty
bed and the player becomes visible. This is a sprite/fade sequence, not a complete
frame-by-frame getting-out-of-bed animation. Waking runs once per play session.
Taking the mantle plays a short paper-body movement and switches the outfit at
midpoint before the personal dialogue; movement and exit are locked until finished.
An opening victory changes the courtyard objective to awaiting the Arkanmeisterin;
John still offers unlimited training. The Arkanmeisterin arrival is not implemented.

Kafla retains Krallenhieb (15 physical damage, 1 AP) and Flammenschwert (25 fire
damage, 2 AP). Glutfunke hits three of the eight immediately adjacent cells
(18 fire damage, 2 AP, range 1). A cardinal aim selects the three neighbours on
that side; a diagonal aim selects its corner and the two adjoining cardinal
neighbours, making an L shape. Nothing outside that ring is reachable. All three
cells must fit on the board, and an empty selected cell is valid. Ability data
is in Scriptable Objects/Abilities/Ember.asset. Both teams have Abwehr (1 AP):
choose the monster's own marked cell to halve direct damage, rounded up. Abwehr
ends at the next own turn, when knocked down, or when taking another action.
Burning damage ignores Abwehr. Protection does not stack with repeated Abwehr.
The introductory battle still has one monster per side.

CoverSystem uses a physics sight query against the Obstacles layer. Low cover
protects an adjacent target only when it lies between attacker and target; flank
attacks bypass it. For single-target ShootAction, the closest cover absorbs 50% of damage,
rounded up, capped by its remaining HP; multiple obstacles do not stack protection.
Overflow reaches the target in the same hit. Abwehr applies after cover absorption.
Trees are full sight blockers and can themselves be targeted; an ordinary wall
blocks ranged attacks completely. Krallenhieb, Flammenschwert and Rammstoß bypass
cover against monsters. Direct melee attacks can damage obstacle health.

BurningObstacle now supports vegetation, stone and wood, cover health and sight
blocking. Stone is immune to burning but can be shattered by direct damage; wood
and plants can ignite. A fire projectile hitting wood cover always tries to ignite
it; monster ignition remains probabilistic and fire monsters remain burning-immune.
The arena includes two low wood fences where their cells are unoccupied. Existing
stones have 45 HP, trees 60 HP and other vegetation 30 HP; fences have 30 HP.
The original arena crate also has 45 HP and is registered as burnable wood cover.
Damaged obstacle art darkens and develops cracks. World health labels appear
only while hovering/aiming at the obstacle or for 2.5 seconds after damage. On
destruction its colliders are disabled and pathfinding is refreshed immediately.
Ram collision knockdown continues using the existing obstacle rules.

Hovering a valid attack cell shows AP cost, damage, cover damage, ignition chance,
and ram push/knockdown outcome. Preview and ranged impact share CoverSystem rules;
chances are displayed without rolling RNG. Existing ShootAction also uses cover;
Glutfunke bypasses low cover absorption: all occupants, including allies and
obstacles, in the three-cell pattern receive 18 fire damage. Tall blockers shield
individual cells; all sight results are captured before obstacles are destroyed.
Surviving wood and plants ignite reliably; monsters use the existing ignition
chance and immunity. Abwehr still halves direct damage. The preview marks the
whole pattern and warns about allies. Kafla scrapes its sword with an independently
animated claw hand and releases illustrated fiery sparks; the body remains upright.
Arkmon/Configure Glutfunke claw animation imports the body variant and assigns it
to the existing sword rig.
The nearby cardinal/corner patterns, friendly fire, wood ignition, stone immunity,
multiple objects sharing a cell, mirrored claw movement and interrupted actions
are covered by TacticalCombatTests. Current native presentation images are
Builds/presentation-glutfunke-corner.png and Builds/presentation-player-dialogue.png.
The transient Kafla-SparkBody.png was created with built-in ImageGen in edit mode;
the saved prompt is Builds/glutfunke-image-prompt.txt.
The six new tactical integration cases, opening tests and existing fire, ram,
recall and UI checks passed in the final 46-case PlayMode run. Native review images
are in ignored Builds/demo-waking-in-bed.png and demo-tactical-cover-preview.png.
The 35-case BattleRegressionTests EditMode suite also passes on this final runtime.
Three UnityMigrationSceneTests pass with simulated mouse/keyboard devices,
including repeated scene entry; this brings the final PlayMode validation to 49 cases.

Arkmon/Configure tactical demo imports the new sprites, adds Glutfunke to Kafla's
prefab/data and updates named tactical props in BattleScene. Running it again
rebuilds the two named fences without duplicating ability references. New art was
created with built-in ImageGen; exact prompts are in ignored
Builds/tactical-demo-image-prompts.json. These changes are prepared without commits
or a version increment; the next proposed release remains 0.9.0.

## Paper presentation polish

Dialogue portraits follow the actual speaker. The boy uses state-dependent head crops from
Prince-Room (before dressing) and Prince-Mantle (after dressing); John uses his existing portrait. Unknown speakers
show no borrowed portrait. The shared paper UI uses Alegreya Regular from the
official Google Fonts repository (https://github.com/google/fonts/tree/main/ofl/alegreya),
licensed under SIL OFL 1.1, included at Assets/Art/Fonts/OFL.txt. The regular weight
was instantiated from its variable source using FontTools; its SDF atlas includes
Western European characters and the interface punctuation, and is saved statically
so TMP does not erase the prepared characters on editor shutdown.

Arkmon/Configure readable paper presentation sets up the portrait/font, removes
additional camera post-process antialiasing, retains native render scale and 4x
MSAA in High Fidelity, and imports character/monster/UI textures uncompressed
without mipmaps and with a 4096 maximum. Texture import preserves existing sprite
coordinates and pivots. Standalone starts at 1920x1080 with a resizable window;
the Game View still needs an appropriate resolution and display scale. These
settings do not create detail absent from a source illustration.

First arrival in the courtyard is derived from the authored Tower entrance,
1.4 units in front of its face. Battle returns keep the saved exploration position.
The wardrobe is now a 3D cabinet with painted surfaces and hinged doors.
The bed and desk still use illustrations with baked perspective; separating bed
posts, canopy and curtains in depth is a future visual improvement.

No version increment or commits were made; the proposed next feature version
remains 0.9.0.

Presentation validation: all 25 distinct PlayMode cases pass across the focused
combat/opening run and the final portrait/font/UI/rendered review run. The initial
font cache failure was repaired by saving the prepared static atlas. Results are
in Builds/presentation-polish-tests.xml and presentation-polish-final-tests.xml;
the final result for rerun cases is in the latter.

## Tower ceiling and chandelier

The tower room uses painted oak grain on its ceiling beams and a warm lime-plaster
texture on the ceiling. Mesh UVs align wood grain along the beam length; two
crossbeams frame the structure. Assets: Assets/Art/SceneStyle/Tower-Oak-Painted.png
and Tower-Plaster-Painted.png, generated using built-in ImageGen; exact prompts
are recorded in ignored Builds/tower-ceiling-image-prompts.json.

Arkmon/Decorate tower ceiling applies only the named tower room ceiling and builds
a six-candle brass chandelier with curved 3D arms and a linked suspension.
TowerChandelierVisual animates the existing illustrated fire frames at candle size
and gently varies one warm local light. The fixture stays fixed and has no gameplay
colliders. Rerunning configuration replaces its named decoration root and reuses
mesh/material assets. Arkmon/Polish demo rooms reapplies these details as well.

Validation: four DemoOpeningTests and the isolated room capture pass (5/5). Native
1920x1080 screenshots from the initial, bed-side and door-side camera positions
are in Builds/tower-painted-ceiling*.png; results in Builds/tower-ceiling-tests.xml.
These changes are uncommitted; the next proposed version remains 0.9.0.

## Wardrobe, player lamp and safe courtyard exit

Player dialogue portraits follow mantle progress in the demo: Prince-Room before
TakeMantle completes and Prince-Mantle afterwards. The victory panel names Kafla,
and return/gate dialogue also uses Kafla rather than the generic Armon wording.

LampIrisGraphic renders a round lantern body, cap, foot, upper carrying loop and
side handle, following the player's illustrated lamp. Its resource material keeps
PlayerLampIris.shader in builds; instances are disposed with the transition. The
full-open state is transparent and full-closed state is black, verified with native
render captures. Scene timing, input locking and music timing remain unchanged.

The courtyard gate has a solid, Ground-tagged forecourt, roadside rails and tall
invisible side/end boundaries behind the visible wooden barrier. Players can pass
through the arch and stand outside safely; open-world travel is not implemented.
The physics test pushes the actual player across the approach into the boundary
and confirms that its height remains above the ground.

The tower wardrobe has a 3D body, feet, a smooth arched crown, an illustrated
front and two hinged doors. The door texture is mirrored for its second leaf;
side wood grain is aligned vertically. During dressing, the doors open and the
hanging mantle disappears when the outfit changes. PaintedWardrobe.OpenForPreview
only serves visual review; normal gameplay follows DemoOpening progress.
The green garment inside is currently a simple folded mesh.

Arkmon/Refine wardrobe, gate and player lamp applies these scoped changes. The
room-polish command reapplies cabinet and gate geometry too. Wardrobe-Door-Painted.png
was generated with built-in ImageGen; its exact prompt is saved in ignored
Builds/wardrobe-door-image-prompt.txt. Native previews are in
Builds/demo-wardrobe-3d-closed.png, demo-wardrobe-3d-open.png,
demo-room-portrait-no-mantle.png and demo-player-lamp-transition.png.
No commits, push or version increment were made; proposed version remains 0.9.0.

Validation for these refinements: 14 focused PlayMode cases pass, covering outfit
portraits, actual gate physics, outcome text, repeated transitions and opening flow.
The final native visual capture also passes (15 distinct cases total). Results are
in Builds/demo-refinements-tests.xml and demo-refinements-final-review.xml. Closed
mask capture is uniformly black; full-open capture preserves the scene.

Wardrobe interaction: doors open before dressing, hold the hanging mantle in view, then close after the outfit changes. Recorded Kenney CC0 wood/cloth cues accompany the sequence; movement and room exit remain locked until it finishes.
