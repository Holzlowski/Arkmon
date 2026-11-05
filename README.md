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

The project version is stored as `bundleVersion` in `ProjectSettings/ProjectSettings.asset` (currently `0.26.6`).

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
procedural paper animations for monsters and idle motion. The exploration player
uses eight painted walking frames per outfit, advanced by actual distance travelled.
The cycle spans 4.8 world units and avoids an additional procedural walking
squash/bounce while these frames are playing, reducing competing movement rhythms.
Raw directional input and immediate horizontal velocity changes remove input smoothing
and release drift; diagonal input is capped to the configured movement speed.
The existing unit Animator and attack callbacks are retained, with the old
body and equipment renderers hidden. Dedicated monster walk poses remain future
work. The exploration player's C key still toggles the paper visual
and the diagnostic capsule.

### Courtyard and arena presentation

StartScene uses a warm limestone courtyard, terracotta keep, wooden benches and simple garden trees. BattleScene uses stone paving and low visual parapets; the original perimeter colliders and grid origin/dimensions are retained. Old decorative tree visuals are disabled. The generated scenery uses native Unity meshes and materials in `Assets/Art/SceneStyle`.

The courtyard stable, wheelbarrow, workbench, fountain, bench, crates, raised herb
bed edging and tower doorway use painted 3D geometry, matching the wardrobe and
bed approach. Their depth follows the camera perspective and their colliders
keep the player outside solid furniture. Flowers and characters retain paper sprites.
Rebuild these props with `Arkmon/Build painted courtyard props`; import player
walking sheets with `Arkmon/Import player walk cycles`. Fountain water is currently
static, and the stable is decorative rather than an accessible interior.

The first courtyard arrival briefly reveals John before returning camera control.
After the first successful trial and John's return dialogue, a dismissible demo
completion panel points toward the Arkanmeisterin; repeat practice remains available.
Battle commands explain why they are disabled and show a target-selection prompt
when no attack preview or detailed explanation is already displayed.
Available command cards clear the legacy Button renderer tint when adopting the
paper layout; disabled cards use the availability-driven CanvasGroup instead.
The guard shield is native maskable UI geometry with its own CanvasRenderer.
The two command areas stay in a left sidebar. Battle starts at area selection;
opening Actions or Attacks does not select a command automatically. The heading
tracks area, command and target selection. Cancel (or Escape) clears the selected
targetable command; Back from the command list returns to area selection.
Category controls are unavailable while an action runs or the enemy takes its turn.

The trial arena keeps two open approaches around central stone cover, with a
burnable bush and fences offering destructible routes. `Arkmon/Configure John
trial arena` reapplies the scoped obstacle arrangement. During John's first trial,
optional advice reacts once to moving behind cover, an ignited obstacle and the
player monster being knocked down. These messages queue until an action finishes,
do not pause combat, and can be dismissed. After the first victory, John's return
dialogue and the objective point toward waiting for the Arkanmeisterin; further
practice is available. Her arrival remains outside the current demo.

In the tower opening, the player starts without mantle or lamp. The golden lamp
is collected from the writing desk and the green mantle from the wardrobe, in
either order. Both use quest shimmer until collected; room exit requires both.
The objective and item dialogue name the remaining equipment. Separate session
flags survive scene changes and reset on a new play session. World sprites,
eight-frame walking cycles and upper-body dialogue portraits cover all four
equipment combinations through `PlayerEquipmentAppearance`. Opening StartScene
or BattleScene directly retains the fully equipped prototype appearance.
Use `Arkmon/Configure preparation equipment` to import the extra variants,
refresh equipment portraits and place the table lamp without rebuilding furniture.

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

The first TowerRoom visit keeps the same 3D bed visible throughout sleep, sitting
and standing. A separate paper boy lies on the mattress beneath the raised mesh
cover; the standing player is hidden and input locked. The camera leans down for
a closer mattress view, then restores its exploration angle. Short fades join the
lying, seated and standing poses; this is not a skeletal getting-out-of-bed animation.
Waking runs once per play session.
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

Wake-up presentation: the 3D bed stays visible while the lying boy changes to a seated pose; the boy straightens beside the bed over 0.9 seconds while the camera eases back to exploration. A dedicated seated sprite now bridges the sleeping illustration and standing pose; climbing is still suggested by the short fade.

The tower canopy bed now uses separate 3D oak frame/posts, linen mattress and pillows, folded green cover, canopy and gathered corner curtains. The opening keeps this geometry throughout sleep, sitting and standing; the player starts at the bedside marker. Configure via Arkmon > Build painted canopy bed. Dedicated painted velvet and linen textures replace the plaster placeholder, and the pillows use rounded geometry.

Tower window: a 2.7-unit opening with a rounded stone arch replaces the wide rectangular opening. The wall masks oversized background, field and cloud layers; stronger depth-dependent camera follow, eased movement and independent cloud drift improve parallax. Configure via Arkmon > Build small arched tower window. The landscape remains layered illustration planes rather than explorable terrain.


### Tower opening details

The opening flows from sleep to a dedicated seated pose, then a short rise into the standing character. Camera and input are restored on completion or interruption. Wardrobe dressing frames the open doors before the outfit changes and returns to exploration afterwards. A short courtyard thought points to John on the first arrival only; his repeatable practice battles remain available.

The desk and chair use separate 3D parts with painted oak surfaces, drawers, books, a letter, ink pot, quill and animated candle. The room adds a warm window light, a subtle transparent shaft and at most 180 drifting dust particles. Quiet recorded courtyard ambience fades down for dialogue and scene transitions; sources and the reproducible mix are recorded in `docs/audio-sources.json` and `tools/render_tower_ambience.py`.

`PlayerInteractionFocus` shares collider and distance rules between the visible [E] prompt and actual interaction. A small gold ground halo marks the closest reachable object; prompts hide during dialogue, dressing and transitions. `Arkmon > Finish tower opening details` rebuilds the furniture and atmosphere without altering the combat arena.

Prototype limitations: sitting and standing are separate paper poses joined by a short fade, not a skeletal animation; props use simplified geometry, and the distant courtyard sound bed is a sparse recording mix rather than a dynamic ambient simulation.


### Circular tower room and visible morning light

The tower bedroom has a circular floor/ceiling and curved stone shell (radius 7.82, center Z -1.7). Front walls are a visual cutaway with solid collision bounds. The arched window and right-wall doorway remain open in the shell; the window has invisible fall protection and the door has a stone recess. Furniture fits the curved wall, and eight radial oak beams support the ceiling. Rebuild with `Arkmon > Build round tower room`.

Tower stone now uses a room-specific triplanar painted shader that receives real main/additional light and shadow maps. Tower-only material copies preserve courtyard and arena appearances. A warm spotlight shines from outside the arched window through slender wooden mullions; cooler ambient fill preserves readable shadows. Matte wood/cloth and slightly glossier brass separate material responses. Reapply with `Arkmon > Refine tower lighting` after rebuilding room geometry.

`Prince-Sleeping.png` is the isolated boy, not a bed illustration. The sprite lies on the mattress; the actual mesh blanket covers his legs and the bed frame never switches to a sprite. Sleep camera framing is closer and steeper to show the lying pose. The character retains the game's paper style even while the furniture is 3D.


### Layered vista, dense morning and first John trial

The arched tower window now uses separate painted sky, mountain, farmland and low hill images plus independently drifting clouds. WindowVistaParallax projects the camera position relative to the fixed aperture, with a stable reference even during the wake-up camera sequence. Oversized layers and bounded offsets keep edges behind the wall. The view remains distant: no wheat stands immediately outside the tower. Configure with Arkmon > Finish tower opening details.

The morning uses one restrained stylized shaft, soft golden dust motes drifting slowly inside a narrow volume aligned with the window beam (prewarmed, at most 180 particles), softer window shadows and small flickering candle accents. Extra haze sheets and the additive golden floor patch are disabled, preserving painted surface detail. Three soft contact-shadow decals anchor the bed, wardrobe and writing desk without changing collisions. Bloom is restrained and the color filter is neutral. These shafts/contact decals are artistic approximations, not physically simulated volumetric fog or screen-space ambient occlusion. High Fidelity supports actual additional-light shadows; lower quality presets may omit the spotlight shadows while retaining lighting and contact shading.

John offers a short first-trial conversation with a prepare-later choice. Acceptance activates optional, dismissible battle advice for summoning, movement, attack targeting and ending the turn. Advice reacts to actions without requiring a fixed solution. Cancelling the introduction keeps it available; later practice battles use the existing dialogue without repeated advice. Progress is session-only and resets for a new play session. First victory points toward the awaited Arkanmeisterin; her arrival is not yet implemented.

AttackPreview uses the same impact/cover/push rules as gameplay. Gold grid outlines show hits, red warns of allies, grey shows blocked fan cells, green marks damaged cover, violet shows pushed targets and a smaller blue outline shows the rammer's final position. Killing blows omit a push/knockdown that will not happen. Text explains AP, direct damage, cover absorption and burn chance/damage/duration. Marker renderers are pooled and hide during actions, opponent turns, transitions and UI hover.

These features form the 0.11.0 demo milestone.

Validation: nine distinct focused PlayMode cases pass across `Builds/first-trial-tests.xml` and `Builds/first-trial-final-tests.xml`, including actual John dialogue acceptance through the scene transition, cancellation/repeated practice, fan and ram preview plans, real Ember damage, door/parallax behavior and native render captures. Native screenshots are `Builds/tower-dense-morning.png`, `Builds/tower-layered-vista-left.png`, `Builds/tower-layered-vista-right.png`, `Builds/john-first-trial-dialogue.png` and `Builds/battle-tactical-attack-preview.png`.


Tower collision and burning-hover refinement: the mountain layer is raised by 0.9 world units and the low-hill layer by 1.25, placing both within the arched view. Exploration movement now sets Rigidbody horizontal velocity while preserving vertical gravity, with continuous dynamic collision detection; it no longer translates the Transform through contacts. The tower doorway's stone backing retains its collider: leave through [E], rather than walking through the scene boundary.

Obstacle inspection labels show current LP and existing burn damage/remaining rounds. When a valid attack preview is displayed, competing world labels hide. The attack panel is explicitly labelled as the next attack, uses readable obstacle names and shows LP before/after the hit. Already burning targets show refresh behavior instead of being described as newly ignited, and the repetitive Glutfunke color legend is removed.

Six distinct focused checks pass after the hover-input fix: normal-controller movement against four tower boundaries, prior cutaway physics, actual door interaction/parallax, burning-fence wording, actual Ember damage and the native vista render. Results are in `Builds/tower-hover-tests.xml` and the successful targeted rerun `Builds/tower-hover-final-tests.xml`. No version increment, commit or push was made.


Command navigation: both Actions and Attacks have a Back button that closes command cards and clears targeting without spending AP. Category buttons reopen either list, including the previously selected category. Commands rebind to the selected summoned unit when deployment changes into Running. Guard uses a native paper shield graphic instead of movement footsteps. The focused first-turn/navigation integration test confirms all three basic buttons are interactable, both back/reopen paths work, targeting clears and the shield exists (`Builds/command-navigation-tests.xml`, 1 passed). Included in the 0.11.0 demo milestone.


Quest hints: the wardrobe uses subdued emissive contours with softly twinkling particles along each illustrated door; the door surfaces remain unchanged. Other mesh items, such as the collectible lamp, retain the surface shimmer. SetObjectiveActive controls the task cue; SetInteractionTarget supports a separate interaction component. Brightness follows PlayerInteractionFocus.Current, so dialogue, transitions and dressing return the cue to its distant intensity. PaintedWardrobe binds the closest wardrobe interaction point and disables the cue as soon as HasMantle is set. Particles follow the moving door faces, and runtime particle objects, overlay meshes and materials are cleaned up. The resource shader is included in builds. The focused PlayMode test passes for distant, reachable, dialogue-blocked and completed states; native previews are Builds/quest-wardrobe-dim.png, quest-wardrobe-bright.png and quest-wardrobe-complete.png. Included in the 0.11.0 demo milestone.

Wardrobe quest contours now use a subdued continuous line with 72 softly twinkling, drifting motes per door. The particles follow the hinges, brighten with interaction focus and clear immediately when the mantle is taken.

Painted room surfaces retain their source resolution and mipmaps, with trilinear filtering, 16x anisotropy and a restrained -0.25 mip bias. Windows imports explicitly use uncompressed RGBA32 for these seven surface textures. Render scale was already 1.0; this is a modest texture clarity adjustment, not an upscale or invented image detail. Reapply via Arkmon > Refine painted texture clarity after regenerating texture imports.

## Fixed demo milestone

Demo **0.11.0** is preserved by Git tag `demo-v0.11.0` before the next design iteration. See [demo playtest and rebuild instructions](docs/demo-playtest.md). The Windows demo ZIP is a separate local artifact under `Builds`; the tag preserves all source and assets.

## New game intro (work in progress)

On `feature/game-intro`, the tower opening introduces Elian of House Aurenfeld. The wardrobe supplies a brown-and-cream family outfit; it does not award the green summoner mantle. There is no collectible vessel in the bedroom. The family outfit alone unlocks the courtyard door, survives scene changes and updates world/walk/dialogue visuals. John offers a greeting rather than the old pre-vessel trial. The Arkanmeisterin arrival, vessel/mantle awards, starter encounters and cellar story are not yet implemented; the gate currently offers a waiting-for-arrival line.

`IntroChapter` marks the new exploration scene profile; the archived demo remains available at tag `demo-v0.11.0`. `TowerRoomCamera` keeps a 25-degree pitch, zero yaw, 40-degree field of view and 12-unit distance, with small bounded tracking. The cutaway hides ceiling meshes without changing wall collisions. `ConfigureGameIntro.Run` imports family sprites and creates the Aurenfeld wall crest. This is the first functional room pass; furniture/art polish towards the concept images remains in progress.

Validation: `GameIntroTests` exercises waking, blocked early exit, dressing, separate outfit/mantle/vessel state, fixed camera angle/zoom, courtyard arrival and John greeting. Earlier demo-flow/equipment tests document the archived demo and need adaptation for the new chapter before they can be used as its acceptance suite. Current metadata remains 0.11.0; proposed next feature version is 0.12.0.

PlayMode tests mute output through the test-only `SilentPlayModeTests` assembly fixture and restore the previous listener volume afterwards. Audio sources still run normally, preserving scene and music timing. Automated Unity test launches additionally use `-noaudio` to suppress output from process startup through shutdown. Normal Play Mode and game builds retain audio.

### Intro visual refinement

`PolishIntroRoom.Run` reapplies the intro profile, then sets a fixed 28-degree pitch, -7-degree yaw, 43-degree field of view and 12.4-unit distance with bounded tracking. It repositions the chandelier away from the wardrobe, enlarges the rug, adds turned bed posts, crown moldings, a scalloped valance, bedside chest collision and dressed window arch stones. Painted oak and limestone materials are scoped to the room; morning light and neutral grading retain surface details. The Aurenfeld wall emblem uses a shield-shaped mesh rather than a rectangular parchment card. Elian uses the silver eight-point star directly on the family tunic in idle/walk/portrait art.

`ConfigureKaflaRefinement.Run` binds a slimmer, amber-eyed paper kitten based on the starter reference on page 7 of the local design PDF. New body, arm, forearm, wooden sword, portrait and alternate Glutfunke body share the existing 1.67-unit body height and attack timings. The PDF remains excluded from Git. These are refinements of the playable prototype, not a claim of matching the concept illustrations exactly. Focused intro and Kafla tests cover dressing/courtyard state, camera stability, sword swing, scrape body restoration and defeated pose; native captures are in Builds.

Kafla's sword rig uses a sprite-specific shoulder anchor inside the cape/torso silhouette. Glutfunke keeps this joint fixed and changes the arm rotation instead of translating the shoulder away from the body. The refinement test checks the anchor and unchanged joint position during scraping.

The refined shoulder anchor overlaps the torso beneath the cape rather than merely touching its outer paper edge. The native idle capture was checked for a continuous arm/cape silhouette after the initial offset proved insufficient.

Kafla uses a separately illustrated gripping paw and forearm. The short shoulder cap remains behind the cape; the forearm draws in front, and the paw is parented to the sword wrist so its handle opening follows blade rotation. Glutfunke retains its separate claw sprite. The focused test verifies hand parenting and forearm draw order as well as sword/scrape/defeat restoration.


### Playable intro courtyard layout

`StartScene` now contains an 88-by-40-unit courtyard floor, high enclosing castle walls and a clear east-west crossing path. The left side houses the residence tower and workshop; the centre holds the stable, fountain, bench and John; the right side contains three vegetable beds, a wheelbarrow, cellar entrance and outer gate. Buildings use coarse painted geometry for a first playable spatial pass. Decorative grass does not block movement. The near wall is cut down visually while retaining full-height collision.

`CourtyardCamera` keeps a fixed 32-degree pitch and 45-degree field of view, smoothly following within explicit horizontal/depth bounds. `CourtyardEntrance` provides four labelled interaction points. The tower door returns to `TowerRoom`, where Elian appears beside the room door with his existing outfit and awakening state. Throne hall, cellar and outer gate currently offer short story dialogue; their interiors and Arkanmeisterin arrival are not implemented yet. Play from `TowerRoom`, collect the family outfit, leave and explore the courtyard with the usual movement and interaction controls.

The reproducible editor entry point is `Arkmon > Build intro courtyard layout` (`ConfigureIntroCourtyard.Run`). Focused PlayMode checks cover the unobstructed crossing route, courtyard size, four entrances, fixed camera rotation/zoom, outer-wall collision, tower return and the existing dressing/arrival flow. Automated runs remain silent. This change is uncommitted; proposed next feature version remains 0.12.0.

Validation: both focused cases passed in `Builds/intro-courtyard-tests.xml`; after correcting the visible tower-door placement and adding the outer-gate face, the courtyard integration case passed again in `Builds/intro-courtyard-final-tests.xml`. Native captures show west, centre and east views under `Builds/intro-courtyard-*.png`.


### Room seams, heraldry and continuous walking

`RefineIntroRoomDetails.Run` refines the existing intro room without rebuilding the courtyard. Window light is reduced from 38 to 31 and room grading exposure from +0.18 to -0.05. The illustrated courtyard door is tinted down. A curved stone backing closes the former door opening, window seam stones bridge the flat arch and round wall, and the floor extends into the wall thickness. The cutaway remains open for the camera. The family shield is reused on the bed footboard and room door. A newly illustrated rug bears only the silver eight-point star.

The rug is now a lit, alpha-tested floor mesh at y=0.012 rather than a transparent SpriteRenderer competing with the character's sorting. Contact shadows include its surface. `PaperPlayerWalkRig` uses two independently animated paper legs, half a cycle apart, plus a stable upper body for both the cream room outfit and brown family outfit. Step phase follows distance over a 2.4-unit stride, stops on release/walls and ignores teleports. A foot-plane constraint keeps rotated boots above the floor; facing remains left/right. Rigidbody interpolation smooths the rendered position between physics ticks. Original idle, dialogue, dressing and summoning poses remain in use; older outfits retain their sprite-sheet fallback. This is a continuous cut-paper gait, not a full hand-drawn animation set.

`ImportElianWalkRig.Run` imports four alpha-bounded rig parts with hip pivots from `Elian-Walk-Rig.png`. New raster assets were produced with the built-in image generator; exact prompts are recorded in `docs/paper-art-generation.json`. No commits or version bumps have been made; the next proposed feature version remains 0.12.0.

Validation: the intro and courtyard flow cases passed in `Builds/intro-room-details-final-tests.xml`. The room/walk case passed after the final boot-clearance and rig-size correction in `Builds/intro-room-walk-final-tests.xml`, checking opposite leg phases, sixteen foot-clearance samples, room/family outfit switching and mirrored facing. Native room and door captures are `Builds/intro-room-heraldry.png` and `Builds/intro-door-heraldry.png`. Tests ran with audio disabled.

Interaction markers: wardrobe and tower-room door halos now use prop-relative presentation positions, separate from their reachable interaction points. The wardrobe marker sits 0.28 units in front of the cabinet collider; the door marker sits 0.35 units inward from its wall-mounted art. Both use a smaller 0.35-unit radius. Interaction range and prompt selection remain unchanged. Suggested commit: `fix(ui): anchor interaction halos beside room props`; proposed next feature version remains 0.12.0.

Tower door/window follow-up: `FixTowerDoorAndWindow.Run` replaces the door sprite display with an alpha-tested URP Lit mesh using the same artwork and UVs, so it receives the room lighting. The source sprite stays disabled as reproducible import data. An approach collider in the door plane stops the full paper silhouette before the illustration. Window mullions now share the arch plane at z=5.91; the vertical member overlaps sill and arch, and the horizontal member reaches both jambs. `RefineIntroRoomDetails.Run` also reapplies this fix. Suggested commit: `fix(room): align door collision and attach window mullions`. Next proposed version remains 0.12.0; no commit or push has been made.

The door uses a restrained texture-matched emission floor to retain painted details in deep shadow while still receiving scene light. Door collision validation pushes the actual Rigidbody into the door for 70 physics ticks and checks the whole rendered silhouette against the door plane. Window checks cover sill/arch overlap and matching frame depth.

Validation: the existing intro flow passed in `Builds/tower-door-window-tests.xml`; the corrected complete silhouette/frame test passed in `Builds/tower-door-ambient-tests.xml`. Native renders confirm the final readable door brightness and connected window cross. All Unity checks ran with `-noaudio`.


### Complete tower opening and first-town vista

`FinishTowerIntro.Run` assembles the current playable room pass without changing the courtyard layout. The courtyard door is now real geometry: eight arched green oak planks on a hinge, separate bronze straps/rivets/handle, an Aurenfeld shield, stone threshold and a recessed stone arch whose back follows the round tower wall. Curved masonry closes the former rectangular opening outside the arch. The old sprite remains inactive as source/reference data. `BuildPaintedTowerDoor` rebuilds the model; `FixTowerDoorAndWindow` preserves it instead of recreating the old flat surface.

The fixed exploration camera uses 28-degree pitch, -7-degree yaw, 45-degree field of view and 13.8-unit distance, focused at (0.25, 1, 1.2), with small bounded tracking. Upper masonry closes the backdrop above the cutaway walls. Foreground/side cutaway collision keeps the walkable area within the composition. Wardrobe focus is a small smoothed offset; it never changes angle or zoom and eases back without reversing the offset.

Elian wakes beside the actual bed, rises and takes a short paper-animated step away from its edge. The bed stays 3D in sleeping, seated and standing states. Waking temporarily suspends collisions/gravity to prevent the bed from pushing the hidden player away; normal physics is restored afterwards or on cancellation. Family-outfit preparation retains the opening wardrobe, removable hanging outfit, star clothing/portrait and disappearing quest cue. Its closing dialogue explicitly directs Elian towards the courtyard.

The room door sequence positions Elian beside the hinge swing, opens the wooden leaf, then walks him through during a short black fade. Before the vessel is awarded, tower/courtyard transitions use black fades rather than a lamp-shaped iris. Battle transitions retain their existing iris. On arrival, Elian starts at the tower entrance and walks 1.15 units out onto its path before the first greeting. Returning to the room places him beside the new door without replaying waking or losing his outfit.

`ExplorationPresentationMotion` owns short scripted steps, synchronizes Transform/Rigidbody positions and restores gravity, collisions, interpolation and movement state. `TowerRoomDoor` exposes the hinge and authored approach/passage positions. Regular input remains blocked during these sequences.

The window uses the countryside framing from before the town pass: field layer at (0, 3.4, 6.9), foreground hills at (0, 4.65, 6.581), and the original SceneStyle materials. The extra town overlay is absent. The user will adjust these scene layers to reveal the small painted village; no runtime town-framing override is added.

Native intro captures now use URP `SubmitRenderRequest`, not the legacy direct camera render, which produced misleading visual artifacts in the automated captures. Output defaults to the tested project's Builds directory; set `ARKMON_REVIEW_OUTPUT` to choose a separate review folder. All capture targets/canvas modes are restored after rendering.

Suggested commits: `feat(room): build painted arched tower door`, `feat(intro): polish waking and courtyard arrival`, and `feat(world): reveal first town in tower vista`. These are uncommitted changes; the next proposed feature version remains 0.12.0.

Acceptance validation: all five focused PlayMode cases pass in `Builds/tower-intro-acceptance-tests.xml`, covering wake/bedside physics, outfit and quest-cue completion, fixed camera/frustum limits with real boundary movement, full paper silhouette against the 3D door, preserved room/courtyard state, animated door opening, black transition and completed entrance step. Captures include `Builds/tower-intro-finished-morning.png`, `tower-intro-3d-door.png` and `tower-intro-courtyard-arrival.png`. All runs used `-noaudio`; normal gameplay audio is unchanged.


Original window vista correction: the existing painted village is visible by shifting the field layer and lowering the nearest hills slightly. Explicit queue offsets keep fields in front of mountains and foreground hills in front of fields. The focused tower opening test passed in `Builds/original-vista-tests.xml`; the native capture confirms the village is visible. Suggested commit: `fix(visuals): reveal town in original tower vista`. Next proposed version remains 0.12.0; changes are not committed or pushed.


### Personal tower-room furnishing

`FurnishTowerRoom.Run` rebuilds the named `Elian personal room details` group using the existing painted oak, linen, velvet, parchment and brass materials. A small round bedside table holds an open water jug, cup and bedtime book; slippers and a folded fringed throw complete the bed corner. The study adds a wall shelf with bound books and a framed pressed flower, an open handwritten book, ink pot, quill and sealed family letters. A deeper stone window sill has a green cushion with piping, an herb pot and pleated side curtains. Props are real lit geometry; vessels have open rims and curtains use a persisted pleated mesh. The table has a solid collider; small surface decorations do not introduce movement obstacles. The waking side of the bed, wardrobe approach, central rug and door route remain clear.

This furnishing pass also restores the original vista via `FinishTowerIntro.RestoreOriginalVista`. The earlier town-framing experiment described above is superseded: no extra town image or forced field/hill render queue is used in the current scene. Existing parallax remains available for manual layer alignment in the editor. No new art generation, gameplay interaction or quest requirement is introduced by these props.

Suggested coherent commits: `fix(visuals): restore original tower window composition` (vista editor setup and scene materials/positions), then `feat(room): furnish Elian's bed study and window corners` (furnishing editor, generated meshes and scene details). Changes remain uncommitted; the next proposed feature version remains 0.12.0. Validation: silent focused intro flow and native room capture in `Builds/furnished-tower-tests.xml` and `Builds/tower-intro-finished-morning.png`.


### Covered sleeping pose and warmer room lighting

`PolishTowerWakeAndLight.Run` updates the current tower scene without rebuilding furniture or changing the vista. Sleeping uses a sprite sub-rectangle of the existing head/collar artwork and a separate lit, gently domed quilt over the covered body. The flat daytime cover/border is hidden only during sleeping; pillows lift slightly, and the ordinary cover returns for sitting and exploration. No new character bitmap is generated. `PaintedCanopyBed` positions the seated pose along the free right side (local 1.42, 0.18, 0.3), away from the front post, and keeps the subsequent player rise and short step close to that side. Bed geometry remains 3D throughout.

The tower uses a warmer trilight ambient palette, a softer golden window spotlight, stronger local chandelier/desk candle pools, and a modest animated bedside candle using the existing paper fire frames. Exposure is slightly reduced and contrast softened. The existing window layers and manually editable parallax composition are unchanged. The extended focused intro check captures covered sleeping and sitting as well as validating the complete waking, outfit and courtyard transition (`Builds/cozy-wake-tests.xml`). All checks use `-noaudio`.

Suggested commits: `fix(intro): tuck sleeping Elian beneath quilt and align bedside poses` (bed controller, sleeping head sprite, quilt mesh, scene pose references and focused checks) and `feat(visuals): warm tower room candle lighting` (lighting editor setup, bedside candle, scene light settings and volume profile). No commit or push has been made; next proposed feature version remains 0.12.0.


### Paper silhouette clearance and walking arms

The tower-room player overrides the prefab's thin box with a 1.30 × 1.93 × 1.25 collision volume offset (-0.06, 0, 0.40). The camera-facing illustration leans back in world space: its head extends behind its feet, beyond the old 0.31-unit-deep collider. The new depth encloses that visible silhouette while preserving the original floor-contact height. The adjustment is scoped to the tower scene; it does not enlarge the battle units or alter the grid. `FixTowerSilhouetteCollision.Run` reapplies the authored collider settings. The latest manual room changes are preserved; only the four size/centre prefab overrides were merged into the scene.

`PaperPlayerWalkRig` renders the existing room/family upper-body artwork as complementary body and arm surfaces with explicit UVs. Shoulder pivots give the arms a restrained nine-degree swing opposite the corresponding leg; the shared distance-based phase, mirrored left/right facing, stable head and torso, and existing idle/dressing/summoning poses remain. No bitmap replacement or new image generation is needed. Six small outfit/part meshes are cached per rig and destroyed with its shared material on disposal. Lower-body sprites retain their existing presentation.

`PlayerSilhouetteTests` physically pushes the player into the rotated wardrobe and curved wall, checks visible sprite vertices against the furniture plane/tower radius, and checks opposite arm angles, half-cycle reversal and mirrored presentation for both outfits. `TowerChapterFinishTests` verifies waking, outfit selection and the door/courtyard transition. Its quilt check respects the optional quilt reference in the currently authored scene. Tests run silently with `-noaudio`; results are in `Builds/silhouette-tests.xml`.

Suggested commits: `fix(room): fit collision to camera-facing player silhouette` and `feat(animation): add shoulder-driven walking arm swing`. No commit or push has been made. The next proposed feature version remains 0.12.0.

The tower-room camera uses an 11-degree pitch with no sideways yaw. Elian's paper gait keeps the forward swing at 27 degrees and extends the rear swing to 40 degrees. The waking seat moves toward the mattress, and the bed shares a rounded rectangular linen cushion mesh between its two pillows.

The tower camera uses high-quality SMAA at native render scale. Oak, linen and velvet use stronger anisotropic texture filtering for oblique surfaces. The room balances soft window light with warmer chandelier, desk and bedside candle pools.

The tower lighting uses a dimmer ambient fill and lower exposure, with stronger window shadows and soft shadows from the desk candle.

The tower room uses closer framing with a slightly wider field of view to keep the doorway visible. Aurenfeld furniture details add oak and brass molding, wardrobe hardware and a velvet chair back. The chandelier chain continues above the visible cutaway.

Version 0.12.0 adds the playable Elian tower and courtyard introduction. The wardrobe keeps its interaction prompt and quest highlight without a floor ring.

Intro foley uses Kenney [RPG Audio](https://kenney.nl/assets/rpg-audio) (cloth2) and [Impact Sounds](https://kenney.nl/assets/impact-sounds) (three concrete, carpet, grass and wood variants), both CC0. The bed cue starts just before Elian sits; footsteps follow distance travelled while grounded and distinguish the rug, paving and courtyard grass.

Tower foley: the wake cloth cue uses full effects gain, and the wardrobe creaks when the doors start opening and thuds when they reach the closed position. The chandelier emits a quiet spatial crackle loop generated by tools/render_chandelier_audio.py. Elian's rear leg swing reaches 52 degrees while the forward swing remains at 27.

Inspecting the tower window eases the camera into a framed vista while the dialogue is open, then restores the normal exploration view.

Open `Assets/Scenes/TowerRoomPaper.unity` and press Play to compare the painted 3D furniture. This separate editor preview keeps the original meshes, collisions and animations and uses scene-specific matte wood, linen and velvet materials. The build entry scene remains TowerRoom.

The painted furniture comparison extends the wardrobe's dark outlines and broad color shapes to the room's wooden furnishings, with flatter fabric tones on the original 3D surfaces.

For a neutral comparison build, use **Arkmon → Build Windows intro comparison**. The standalone test package starts with Intro A and Intro B, which share the same playable flow and differ in room styling. Restart the application to try the other version; the regular build continues to start directly in the original tower room. The selected variant also persists when returning to the tower from the courtyard.

The courtyard garden is optional: pick up the watering can near the first bed and water the three plots. Wet soil darkens, and progress persists during the current play session. The gate remains available without the garden task.

The courtyard has signposts for the garden, stable, workshop and gate. Marta reacts to the optional watering task, Ida and Hannes provide short local encounters, and the wheelbarrow is parked by the garden. Residents use temporary silhouettes. The watering cue is generated by tools/render_garden_audio.py.

Rebuild the saved courtyard with **Arkmon → Build intro courtyard layout**. Tower, castle wings and walls share world-projected, shadow-receiving limestone, so stone size is independent of building dimensions. Castle facades have projecting cornices, buttresses and framed windows. Standalone buildings are checked for intersecting renderer bounds, including roof overhangs, before the scene is saved. The joined castle sections deliberately share their structural junctions.

The courtyard meadow has a walkable mesh with smooth slopes beside the tower and across the lawns. Paths sample the same height surface along and across their width; grass, residents and signposts follow the ground. Building entrances and vegetable plots retain level ground. `CourtyardPolishTests` checks building separation, shared stone materials and the terrain collider; `IntroCourtyardTests` and `CourtyardGateTests` cover the route, garden, tower return and bridge. The buildings and resident silhouettes remain prototype art.

### Western residence and tower approach

`CourtyardWestArchitecture`, invoked by **Arkmon → Build intro courtyard layout**, builds the west residence as a lower connecting wing and a taller building behind it. Blue slate roofs, dormers, arched windows, dressed stone courses and Aurenfeld banners link it to Elian's tower. A broad stair connects the raised stone terrace to the courtyard, with stone balustrades and ivy along its edges. The camera keeps a constant angle and field of view, with a configurable focus height to include more of the buildings. Warm directional sunlight and cooler ambient shadows apply to the courtyard only.

The shared world-projected masonry uses `Assets/Art/Intro/Aurenfeld illustrated limestone.png`. It was generated with the built-in image tool; the rest of this pass uses existing art and authored geometry. Meshes and materials are persisted alongside the texture. The separate village houses and NPC silhouettes still need their own art pass. Courtyard PlayMode checks include walking up and down the tower steps, building clearance, the garden, bridge and return to the tower. Run with `-disableAudio` for silent validation; captures are written to `Builds/intro-courtyard-*.png`.

The decorative stair treads use a continuous invisible collision wedge, because the Rigidbody walker otherwise catches on vertical risers. All five focused courtyard tests passed after this correction (`Library/courtyard-west-tests.xml`). The saved before image is `Builds/west-courtyard-before.png`; the current west view is `Builds/intro-courtyard-west.png`. These local captures use different camera framing and are review artifacts, not packaged game assets.

<details>
<summary>Limestone texture generation prompt</summary>

Use case: stylized-concept. Asset type: seamless square albedo texture for 3D castle masonry in a hand-painted illustrated storybook RPG with paper characters. Create only a flat orthographic swatch filling the whole square with about 8 horizontal courses of staggered large limestone blocks, each course about 4 or 5 blocks. Warm muted oatmeal and cream stone, occasional desaturated ochre and cool grey blocks, dark brown-grey thin irregular mortar lines, restrained hand-inked outlines, broad gouache pigment patches, lightly chipped rounded corners, a few tiny moss traces. Clear simple shapes at game-camera distance, no photorealism, no photographic noise, no 3D render. Completely even diffuse lighting, no cast shadows, no directional illumination, no vignette, no perspective, no border, no text or objects. Edges must tile seamlessly on all four sides, matching rows horizontally. Final opaque square texture, 1024x1024 or larger.

</details>

### Marta's garden cottage

The garden cottage is built by `CourtyardGardenCottage` as one local hierarchy: cream limewashed walls, painted timber framing, a front-facing gable, individually shaped terracotta roof tiles, sage shutters, a bracketed porch, a low stone plinth, shallow approach stones, herb pots and a potting table. The generator no longer relocates its separate details using coordinate ranges. `Marta work spot` belongs to the house and is the spawn anchor used by `CourtyardLife`; moving the house moves the intended work station with it.

Marta uses the transparent `Assets/Resources/Marta-Gardener.png` sprite, showing a gardener with a harvest basket. `CourtyardGardenerVisual` adds restrained idle breathing, a contact shadow and horizontal facing toward nearby Elian. Her interaction anchor remains fixed. Existing garden dialogue and watering progression are preserved. The doorway is scenery; this pass does not add an enterable cottage interior. Ida is covered by the workshop pass below; Hannes is covered by the stable pass below.

The focused courtyard checks cover building separation, Marta's anchor, left/right facing, dialogue and clear door approach, as well as the existing garden, tower stairs and bridge. Silent runs use `-disableAudio`; the new gameplay capture is `Builds/intro-courtyard-marta.png`.

<details>
<summary>Marta sprite generation prompt (built-in image tool)</summary>

Create a single full-body 2D game sprite on a transparent background: Marta, a kindly middle-aged gardener living in a cozy fantasy castle courtyard. Hand-drawn storybook paper-cut RPG style, dark expressive ink outlines, narrow warm cream cut-paper edge, softly painted gouache shading, clean readable silhouette, charming slightly chibi proportions about 3.5 heads tall. Warm smiling face, auburn hair with grey streaks tied into a loose braid, soft straw sunhat with a sage ribbon, cream rolled-sleeve blouse, muted sage green skirt and bodice, faded ochre work apron with a little gardening pocket, practical brown boots. Standing naturally in three-quarter front view facing slightly to the viewer's right, both feet visible and flat on the same baseline. Holds a shallow wicker harvest basket of carrots and a small cabbage at waist height with both hands, elbows naturally attached. Large friendly eyes, welcoming expression. Restrained earthy green, ochre, brown and cream palette matching a hand-painted medieval fantasy world. Only ONE character, centered, generous transparent margin, entire hat and boots included, no scenery, no text, no ground, no cast shadow, no extra poses, no photorealism, no 3D rendering.

</details>

Validation for this pass: the five existing courtyard cases pass in Library/marta-courtyard-tests.xml; the new Marta case passes in Library/marta-focused-tests.xml after isolating its facing-only teleports from Rigidbody interpolation. The physical stairs and clearance checks remain unchanged. Builds/intro-courtyard-marta-detail.png uses a temporary closer inspection camera; Builds/intro-courtyard-marta.png uses the normal gameplay camera. No gameplay camera settings were changed by the cottage pass.

### Per-line dialogue emotions

`DialogueSection.presentations` optionally supplies one `DialogueLinePresentation` per text line: an empty speaker uses the conversation's speaker, emotion defaults to Neutral, and an optional portrait overrides the normal lookup. Missing entries preserve existing dialogue trees. Branch questions have their own optional presentation. Battle ownership continues to use the initiating speaker even when an individual line is spoken by Elian.

Reusable `DialoguePortraitSet` assets under `Resources/DialoguePortraits` map a speaker to neutral, friendly, surprised and worried portraits. A missing expression falls back to that speaker's neutral portrait; legacy player/John portraits and explicit portrait overrides still work. Unknown speakers show no portrait instead of inheriting the previous speaker's face. Closing, replacing or ending a conversation clears the active presentation. `DialoguePortraitMotion` gives each line a short, restrained emphasis within the existing parchment frame.

`MartaConversation` provides the greeting, Elian's response, garden request and a separate post-watering thank-you. `CourtyardGardenerVisual` listens to line presentation and garden completion, faces Elian and plays small upright nod/bounce motions without moving the work spot. The world sprite itself is unchanged; facial expressions currently appear in the dialogue portraits. It unsubscribes from all events when disabled. Other NPCs can adopt the same portrait-set and line-presentation data; their expression artwork is not included in this pass.

`ConfigureMartaEmotions.Run` imports the expression sheet and creates four Unity sprite rectangles plus the Marta portrait set, without editing the bitmap. The generated sheet is `Assets/Art/Intro/Marta dialogue expressions.png`; it was created with the built-in image tool using the existing Marta sprite as its reference.

Validation: all three focused PlayMode tests in `Library/marta-emotions-tests.xml` pass. They cover all four expressions, Elian/Marta speaker changes, natural completion, cancellation, scene reload subscriptions, one-time garden completion, thank-you dialogue, legacy branches, missing portraits and explicit overrides. Runs use `-disableAudio`. Actual UI captures are in `Builds/marta-dialogue-*.png`.

<details>
<summary>Marta expression-sheet generation prompt</summary>

Using the attached Marta gardener sprite as the exact character and art-style reference, create a transparent 2 by 2 portrait expression sheet for a fantasy RPG dialogue UI. Keep exactly the SAME face, age, auburn braid, straw sunhat, sage ribbon, cream blouse and green bodice in all four portraits. Each portrait is a head-and-shoulders bust with the full hat visible, at identical scale and position inside its own quadrant. Precisely four equally sized square cells in a 2x2 grid, no drawn borders, no text labels, no extra figures. Leave generous transparent gutters and transparent margins so no portrait touches another quadrant. Top LEFT: neutral attentive, relaxed closed mouth, calm eyebrows. Top RIGHT: warmly happy and grateful, clear broad smile, cheerful softened eyes. Bottom LEFT: pleasantly surprised, raised eyebrows, open eyes and small rounded open mouth. Bottom RIGHT: gently worried about dry plants, inner eyebrows raised, slightly downturned closed mouth, compassionate expression, not angry or crying. Expressions should be distinct even at small UI size. Same three-quarter front angle facing slightly viewer-right throughout. Illustrated ink-and-gouache paper-cut aesthetic and narrow warm cream cutout contour, matching the reference. Actual transparent background, no backdrop, no drop shadows, no glow, no gradients surrounding the portraits. Square canvas.

</details>


### Ida's workshop and conversations

Ida has a dedicated workshop beside the west terrace, within the widened space between the tower and the residence. `CourtyardWorkshop.Build` creates a single grouped half-timbered building with a plaster facade, brown shingled roof, sheltered retained workbench, tool rail, timber rack, offcut bin and shallow entrance stones. A short curved path connects the door to the courtyard route. The meadow is smoothly flattened only around this station. Ida's spawn anchor is a child of the workshop, so she stays assigned to her workplace.

`CourtyardCarpenterVisual` uses three illustrated hand-plane working poses and a separate idle pose, with restrained breathing. Starting a conversation with Ida switches to the idle pose and turns her horizontally toward Elian. Her work stays paused during Elian's replies and resumes after normal completion or cancellation. Dialogue event subscriptions are removed on disable. This is a small frame-based ambient animation, not a full skeletal animation.

`IdaConversation` adds a six-line exchange about repairing Marta's garden tool, using neutral, friendly, surprised and worried portraits. Facial expressions appear in the dialogue UI; the world figure keeps its work/idle face. The dialogue does not add a new quest. The workshop door remains scenery and has no enterable interior yet. Hannes is covered by the stable pass below.

`ConfigureIdaWorkshop.Run` imports the two transparent sheets and creates native Unity Sprite assets under `Resources/Ida` and `Resources/DialoguePortraits`. Rectangles and shared foot pivots are set in sprite metadata; the generated bitmap pixels are unchanged.

Validation uses silent Unity PlayMode runs (`-disableAudio`). The eight courtyard cases cover building separation, free approaches, existing stairs/garden/bridge behavior, Ida's changing work poses, all dialogue expressions, work interruption/resumption and subscriptions after scene reload. Results: `Library/ida-courtyard-tests.xml` (8/8 passed). After the final bench/sign placement refinement, the two affected workshop cases passed again in `Library/ida-workshop-tests.xml`. `Builds/intro-courtyard-ida.png` uses the gameplay camera; `Builds/intro-courtyard-ida-detail.png` is a closer inspection view. `Builds/ida-dialogue-worried.png` shows the actual dialogue UI.

Generated artwork (built-in image tool):
- `Assets/Art/Intro/Ida carpenter poses.png`, original output `exec-ccb98de0-1186-42f7-bbe0-accf1ee35b77.png`.
- `Assets/Art/Intro/Ida dialogue expressions.png`, original output `exec-af4bf23c-d637-42a6-bf16-c3ce96ccef56.png`, with the pose sheet as character reference.

<details>
<summary>Ida working-pose generation prompt</summary>

Create a production 2x2 sprite sheet for ONE original female carpenter NPC named Ida, in the hand-drawn ink and gouache paper-cut style of a cozy medieval fantasy RPG. Square transparent canvas, four equal square quadrants, no labels, borders, scenery, floor or shadows. In EVERY quadrant show the SAME complete full-body character at identical size and aligned boots, plenty of transparent margin. Ida is a capable friendly woman about 35, warm medium brown skin, dark brown curly hair in a high bun tied with an ochre scarf, expressive brown eyes, burgundy rolled-sleeve shirt, tan leather work apron, dark olive cropped trousers, solid brown boots; small brass pencil tucked behind ear. Charming proportions about 3.5 heads high, bold dark outlines, narrow cream paper edge, softly painted flat color. Three-quarter front view facing viewer-right. Top LEFT: relaxed talking pose, arms resting naturally down, gentle closed-mouth smile. Top RIGHT: working pose A, both hands on a small wooden hand plane at waist height, arms extended slightly forward to viewer-right. Bottom LEFT: working pose B, same plane held at waist height but elbows pulled back closer to torso, slight focused expression. Bottom RIGHT: working pose C, hands and plane halfway between A and B. The three working poses must differ ONLY in forearm positions and the small plane, keeping head, torso, legs and boots aligned exactly; no leaning or body rotation. The hand plane's sole stays horizontal; a separate 3D workbench will be placed below it in the game, so do NOT draw a workbench or plank. Anatomically connected arms and hands, consistent clothing and face across all four cells. Earthy warm colors, absolutely transparent background without glow or decorative noise.

</details>

<details>
<summary>Ida dialogue-portrait generation prompt</summary>

Using the attached Ida sprite sheet strictly as the CHARACTER REFERENCE (not as a layout to preserve), create a 2x2 dialogue PORTRAIT expression sheet, on transparent square canvas. Four equal square cells, each with one head-and-shoulders bust at identical scale. Preserve the same brown-skinned female carpenter, dark curly bun, ochre hair scarf, brass pencil behind ear, burgundy shirt, tan leather apron, face and hand-drawn ink/gouache paper-cut style. Do not draw tools or hands. Top LEFT: neutral attentive expression with closed mouth. Top RIGHT: openly friendly happy smile and warm eyes. Bottom LEFT: surprised with raised eyebrows, wide eyes and a small rounded open mouth. Bottom RIGHT: thoughtfully worried, eyebrows tilted inward/up, gently downturned mouth, not angry. Keep the SAME face and viewing angle, clothing and hair across all four. Complete hair bun and scarf in every cell. Narrow cream cut-paper edge. Transparent margins at least 8% around every bust, no crossing the cell boundaries, no captions, no frames, no background, no glow, no shadow, no surrounding flecks. Strong facial readability at small size.

</details>

### Workshop placement refinement

The workshop group moved from (-23, 0, 4.3) to (-27, 0, -8.3): four units west and 12.6 units toward the foreground. Its work spot, bench and door approach move together. The wayfinding sign now follows a workshop child anchor instead of an independent world coordinate. A curved side path connects the tower route around the west side of the workshop to its forecourt, keeping the main crossing open. The level ground patch follows the shared workshop position; the previous patch beside the castle returns to the meadow relief.

The existing courtyard checks and screenshots are reused for this layout change; workshop checks resolve their positions relative to the building. Validation: 8/8 courtyard PlayMode cases passed (Library/ida-relocation-tests.xml), and the west gameplay and workshop views were visually reviewed. The gameplay camera itself is unchanged.

### Expanded west courtyard and Hannes' stable

The temporary southwest workshop placement has been superseded. The west residence and upper residence now sit ten units farther east, while the tower keeps its original position, staircase and return point. A lower, longer rear connecting wing links the buildings. The terrace extension and facade ivy follow the new architecture. Ida is back in the west terrace area at (-25, 0, 4.3), with a short entrance branch from the main path. Her former foreground site returns to meadow.

`CourtyardStable.Build` replaces the retained Stable's old geometry with grouped oak posts, pale plaster, a slate gable roof, feed-store doors, an open-looking gated bay, bound hay, a hollow water trough and a tack rail. The stable sits at (-11.5, 0, 5), clear of the residence behind it. The forecourt and work pad are level; their surroundings retain meadow relief. The stall remains scenery behind a collision boundary; this pass does not add horses or an accessible stable interior.

`Hannes work spot` and `Stable sign spot` are child anchors. `CourtyardLife` resolves them when spawning Hannes and his sign. `CourtyardStablekeeperVisual` uses three illustrated saddle-brushing frames plus a relaxed conversation pose (see the saddle refinement below). The activity pauses throughout his conversation, including Elian's lines, turns toward Elian and resumes on completion or cancellation. Event subscriptions are removed on disable.

`HannesConversation` provides a warm six-line conversation about caring for the horses and Elian's nerves before the examination. Four portraits (neutral, friendly, surprised, worried) use the existing per-line dialogue system. Expressions are shown in the UI; the world figure changes between working and relaxed poses. There is no new quest or change to examination progression.

`ConfigureHannesStable.Run` imports the two transparent sheets and native Unity sprite assets under Resources/Hannes and Resources/DialoguePortraits, then rebuilds the courtyard. Artwork was generated with the built-in image tool and copied into the project without altering the bitmap:
- `Assets/Art/Intro/Hannes stablekeeper poses.png` (source exec-563359dd-7757-46d3-910a-8b4d061f2f9f.png).
- `Assets/Art/Intro/Hannes dialogue expressions.png` (source exec-74355af3-5725-4110-acfe-dac1bf9fedf0.png; pose sheet used as the character reference).

Silent validation reuses the courtyard regression checks and adds `CourtyardStableTests` for Hannes' anchor, unobstructed approach, moving work frames, all expressions, Elian's reply, cancellation, resumption and scene-reload subscriptions. All ten courtyard cases pass in Library/hannes-courtyard-tests.xml, including the final low-gallery window adjustment. The west view, stable close-up and dialogue portrait were visually inspected. Gameplay captures: Builds/intro-courtyard-west.png and Builds/intro-courtyard-hannes.png; the closer inspection camera produces Builds/intro-courtyard-hannes-detail.png. Builds/hannes-dialogue-worried.png shows the actual dialogue UI.

<details>
<summary>Hannes working-pose generation prompt</summary>

Production sprite sheet for a hand-drawn cozy medieval fantasy paper RPG. Transparent square canvas, exact 2x2 equal cells, no labels or grid. SAME character in all four: Hannes, kindly sturdy middle-aged male stablekeeper, warm light tan skin, wavy chestnut hair, short rounded beard and moustache, expressive brown eyes, soft moss-green cap, cream rolled-sleeve shirt, muted blue waistcoat, brown leather belt with small pouch, dark ochre trousers, brown work boots. Charming 3.5 heads tall, strong dark ink contours, narrow cream cut-paper edge, hand-painted gouache shading, earthy colors. Entire figure head to boots in every cell, generous transparent margins, identical scale and aligned boots/head. Three-quarter front facing viewer-right. Top LEFT relaxed friendly conversation pose, hands naturally at sides, no tools. Top RIGHT working pose: both hands hold a small horse grooming brush and a short brown leather strap at waist level, right hand brushing along the strap toward viewer-right. Bottom LEFT same working pose with brush hand pulled back. Bottom RIGHT brush midway. Only hands and forearms change, torso/head/feet stay aligned. Anatomically connected arms. No horses, scenery, tables, floor, cast shadow, captions, additional objects or glow. All cell content inside 8 percent margins. Actual alpha transparency.

</details>

<details>
<summary>Hannes portrait generation prompt</summary>

Use attached Hannes sprite sheet as the exact CHARACTER and ink/gouache paper-cut STYLE reference. Create a transparent square 2x2 DIALOGUE PORTRAIT sheet, four equal cells with one head-and-shoulders bust in each. Same friendly middle-aged stablekeeper, chestnut wavy hair and short beard/moustache, moss green soft cap, cream shirt, muted blue waistcoat, warm tan skin and brown eyes. Same angle and identity in all cells. No hands, brushes, straps or tools. Top LEFT neutral attentive closed-mouth expression. Top RIGHT friendly warm smile. Bottom LEFT surprised with raised eyebrows and slightly open mouth. Bottom RIGHT gently concerned, inward raised eyebrows and slightly downturned mouth, not angry. Full cap inside each cell with generous transparent margins. Narrow cream paper outline, dark expressive ink, softly painted earthy colors. No text, borders, backdrops or shadows. Actual transparent background.

</details>

### Hannes: readable saddle care

Hannes now brushes a separate saddle on a splayed wooden stand beside him. The saddle has a curved seat, raised pommel/cantle, hanging leather flaps with edge trim, and metal stirrups. His new work poses extend the brush sideways over the seat; the ambiguous hand-held strap is gone. The greeting now explicitly mentions cleaning the saddle. Conversation pause/resume and the existing four expression portraits are preserved.

The replacement transparent sheet is `Assets/Art/Intro/Hannes saddle cleaning poses.png`, generated with the built-in image tool using the previous pose sheet as character reference (source output `exec-808713f9-810a-44b3-a79f-7eda068beb04.png`). The original sheet is retained as source history. `ConfigureHannesStable` updates existing native sprite assets in place when their source texture changes, preserving their GUIDs. All poses share the same crop, foot pivot and world scale. The saddle meshes are authored in `CourtyardStable`, not baked into the character image.

Both focused CourtyardStableTests pass in Library/hannes-saddle-tests.xml (silent run). All three work-frame captures were visually checked for brush/seat contact. The three work-frame views are Builds/hannes-saddle-WorkForward.png, Builds/hannes-saddle-WorkMiddle.png and Builds/hannes-saddle-WorkBack.png. The animation remains a small frame-based idle activity.

<details>
<summary>Saddle-brushing pose sheet prompt (built-in image tool)</summary>

Edit the attached Hannes sprite sheet into a NEW 2x2 animation sheet. Preserve this exact kindly bearded stablekeeper identity, moss cap, blue waistcoat, cream rolled-sleeve shirt, ochre trousers, boots, ink/gouache paper cut style and transparent background. Critical change: REMOVE the separate leather strap/loop entirely. Keep his worn belt intact, but his brush must NEVER touch or overlap his belt or torso. In the three working poses he reaches SIDEWAYS to viewer RIGHT to brush a separate saddle that will be rendered as a 3D object in the game. Do NOT draw the saddle, stand, horse, floor or scenery. Layout: four equal cells on square transparent canvas, same scale and feet baseline. Entire figure in each cell with 5 percent transparent margins. Body and boot center at x=40 percent of each cell leaving open space on the right for the extended arms. Top LEFT: relaxed conversation pose, arms at sides, no brush. Top RIGHT: working pose A, torso upright, BOTH arms reaching to viewer right, elbow connected naturally to shoulder, brush held with bristles DOWN at x=80 percent and y=50 percent of cell. Brush entirely to RIGHT of the torso, clear transparent gap under forearms, belt fully visible and unobstructed. Free hand gently resting near brush at x=70 percent, as if steadying the unseen saddle. Bottom LEFT: same body/head/feet, brush moves back horizontally to x=70 percent at exactly same height, still entirely outside torso silhouette. Bottom RIGHT: brush at x=75 percent same height. Keep shoulders, torso, hat and boots EXACTLY stationary across working frames; only forearms and hands move. Thick dark outlines, cream paper edge, earthy painted color. No long leather straps, loops, harness, reins, captions, panels or shadows. Genuine transparent alpha.

</details>

### Shared NPC art style (Elian / John reference)

The approved reference is `Assets/Art/Intro/CharacterStyle/Approved style reference.png`. Elian and John remain the visual baseline. Hannes, Ida and Marta now use bold dark brown outlines, large simple eyes, compact silhouettes and clean angular shading instead of textured, detailed painting. Their established clothes, tools and roles remain recognizable. John/Hannes being brothers is only a possible background idea; this pass adds no family lore or dialogue about it.

Active generated source sheets:
- `Assets/Art/Intro/CharacterStyle/Hannes poses.png` — relaxed pose and three sideways saddle-brushing poses.
- `Assets/Art/Intro/CharacterStyle/Hannes expressions.png` — neutral, friendly, surprised, worried.
- `Assets/Art/Intro/CharacterStyle/Ida poses.png` — relaxed pose and three hand-plane poses.
- `Assets/Art/Intro/CharacterStyle/Ida expressions.png` — the same four emotions.
- `Assets/Art/Intro/CharacterStyle/Marta gardener.png` — gardener with basket, hat and silver hair streak.
- `Assets/Art/Intro/CharacterStyle/Marta expressions.png` — the same four emotions.

All six assets were created with the built-in image tool using the approved lineup and each resident's previous design as references. Earlier source images remain available for comparison. No bitmap processing was used for slicing or recoloring.

`CourtyardCharacterArt` centralizes texture import, sprite rectangle creation and portrait-set updates. Existing sprite GUIDs are preserved with serialized replacement, including when slicing or pivots change. `ConfigureCourtyardCharacterStyle.Run` imports all three residents and rebuilds the scene once. Individual Hannes/Ida menu actions remain available; Marta now resolves `Resources/Marta/Gardener` with the existing sprite name `Marta-Gardener`. Portrait names and expression lookup remain unchanged.

The saddle stand is scaled to the new brush height; the workbench is lowered and its planing board extends under the full hand-plane stroke. These adjustments keep the tools visibly on their work surfaces. Sprite names are restored after initial asset creation as well as updates.

Existing targeted PlayMode tests cover work pause/resume, speaker/expression changes, cancellation, subscriptions after reload, garden completion and free approaches. Verified on 2026-11-04: 10/10 targeted tests passed in Library/courtyard-character-style-tests.xml; after the final planing-board alignment, 2/2 workshop tests passed again in Library/character-style-bench-tests.xml. All runs used -disableAudio. The work-frame captures are Builds/hannes-saddle-Work*.png and Builds/ida-planing-Work*.png; dialogue captures are Builds/hannes-dialogue-*.png, Builds/ida-dialogue-*.png and Builds/marta-dialogue-*.png. These are actual Unity renders; the detail views use a closer inspection camera.

<details>
<summary>Hannes poses — final generation prompt</summary>

Production transparent 2x2 sprite sheet of ONLY Hannes, the RIGHTMOST adult character in reference 1. Reference 1 is the approved character and style: rounded moss flat cap, chestnut beard grouped into simple shapes, large expressive oval brown eyes, cream rolled sleeves, blue waistcoat with gold buttons, ochre trousers, belt pouch, chunky boots, compact broad adult build. Preserve this EXACT approved Hannes design, not John. Bold clean dark brown outlines, narrow cream paper border, flat rich color with two angular shade tones, NO grain, hatching, realistic folds or painterly texture. Reference 2 is ONLY a pose/position guide; replace its old realistic face/body rendering with approved style.
Square canvas, 4 equal cells, transparent background without glow or shadows. Full body in each cell, same head/torso/boot position and scale. Body/feet center x=40% of each cell, boot soles y=98%, head top y=3%. Top LEFT relaxed talking pose with arms down, no tools. Top RIGHT working pose A: arms extended sideways to viewer RIGHT, brush bristles pointing DOWN at x=80%, y=55% of cell. Bottom LEFT working pose B brush at x=73%, same y=55%; bottom RIGHT midpoint brush x=76%, same height. Free hand extended near the brush as if steadying a separate unseen saddle. Brush must remain ENTIRELY to the right of torso and must NOT touch belt. No leather strap or reins. The saddle is a separate 3D object so DO NOT DRAW SADDLE OR STAND. Work poses differ ONLY in forearms/hands/brush, same upright torso and planted legs. No text, gridlines or scenery. All cells cleanly separated with transparent margins.

</details>

<details>
<summary>Hannes portraits — final generation prompt</summary>

Create a transparent 2x2 dialogue portrait sheet of the EXACT Hannes from the attached new sprite sheet. Only head and shoulders, no hands, brush or props. Preserve moss flat cap, chunky chestnut hair, short grouped beard, large simple brown cartoon eyes, cream collar and blue waistcoat. Match the same bold clean dark brown outlines, angular flat two-tone shading and narrow cream paper edge. No painterly grain, fine hair texture, hatching or realistic rendering. Four identical-scale busts centered in four equal square cells with generous transparent margins, complete cap visible. Top LEFT neutral attentive closed-mouth expression. Top RIGHT warmly friendly smiling. Bottom LEFT surprised, raised brows and small open mouth. Bottom RIGHT concerned, raised inner brows and gently downturned mouth, not angry. Same viewing angle and identity throughout. Square transparent canvas, no labels, panels, background, shadow or glow.

</details>

<details>
<summary>Ida poses — final generation prompt</summary>

Create a transparent 2x2 full-body sprite sheet of female carpenter Ida. Reference 1 defines the REQUIRED shared style of Elian, John and Hannes: bold smooth dark brown outlines, narrow cream cut-paper edge, large simple expressive oval eyes, compact stylized adult body, clean flat colors and angular two-tone shadows, NO grain, hatching, realistic anatomy or fine fabric/hair detail. Reference 2 gives ONLY Ida's identity, clothing and pose layout; replace its realistic illustrative rendering entirely. Preserve warm medium-brown skin, dark curly hair in a high bun tied with ochre scarf, brass pencil behind ear, burgundy rolled-sleeve shirt, tan work apron, dark olive trousers, brown boots. A capable friendly adult woman, distinct face, rounded simple hands, around 3.3 heads tall. Same design all cells.
Square alpha canvas, four equal cells, full figure within each cell with transparent margins, boots y=98%, top of bun y=3%, body center x=50%. Top LEFT idle conversational pose, arms down naturally, no tool. Top RIGHT both hands grasp small wooden hand plane at waist height pointing viewer RIGHT, arms pushed forward. Bottom LEFT same pose, elbows pulled back. Bottom RIGHT mid-stroke. Plane sole stays horizontal at y=55% of each cell, plane extends to RIGHT of apron, no floating arms. Head, torso, boots and stance MUST stay identical across work frames, only forearms/hands/tool move. The workbench exists as a separate 3D object so do not draw bench, plank, floor, shadows or surroundings. No labels or borders. Strongly match the graphic cartoon style of reference 1, not the painterly reference 2. Genuine transparent background.

</details>

<details>
<summary>Ida portraits — final generation prompt</summary>

Create a transparent 2x2 DIALOGUE PORTRAIT sheet of the EXACT illustrated Ida in the reference. Four head-and-shoulder busts at identical scale and angle, no hands or tools. Preserve warm medium brown skin, dark curly high bun, ochre tied scarf, pencil behind ear, burgundy shirt and tan apron straps. Use the exact same bold dark brown contour, narrow cream paper border, big simple oval eyes and clean angular two-tone color shading. NO grain, realistic facial detail, fine hair strands or hatching. Top LEFT neutral attentive closed mouth. Top RIGHT friendly happy smile. Bottom LEFT surprised with raised brows, wide eyes and small rounded open mouth. Bottom RIGHT worried with inner brows raised and gently downturned mouth, not angry. Same face across all cells. Entire bun/scarf inside each cell, equal generous transparent margins. Square canvas, no captions, frame, background or shadows. Actual alpha transparency.

</details>

<details>
<summary>Marta world sprite — final generation prompt</summary>

Redesign ONLY Marta, the female gardener in reference 2, into the exact approved cartoon paper RPG art style of reference 1. Reference 1 is the STYLE standard only: bold dark brown clean outlines, narrow cream paper edge, big simple oval brown eyes, grouped hair shapes, chunky hands/boots, compact stylized adult proportions, flat clean earthy color with angular two-tone shading. NO painterly grain, fabric weave, fine hair strands, hatching or realistic anatomy. Preserve Marta's identity from reference 2: kindly middle-aged woman, auburn braid with a clear silver streak, broad straw sunhat with sage ribbon bow, cream rolled sleeves, sage bodice and long skirt, ochre gardening apron with small trowel pocket, brown boots, holding a shallow basket with a cabbage and carrots. Simplify basket weave to a few bold strokes, simplify clothing to few large folds. Friendly adult face, slightly rounded build, about 3.3 heads tall, not a child, calm smile. Three-quarter front facing viewer-right, full figure including complete hat and boots, feet aligned flat. One standalone character centered with generous transparent margins, tall portrait canvas, no other characters, labels, environment, ground, shadow or surrounding glow. Actual alpha transparency. Strongly match Elian and John from reference 1; do NOT retain the detailed storybook rendering of reference 2.

</details>

<details>
<summary>Marta portraits — final generation prompt</summary>

Create a transparent square 2x2 dialogue portrait sheet of EXACTLY Marta from the attached new cartoon sprite. Four identical-scale head-and-shoulders busts with complete broad straw hat and sage ribbon, auburn braid with silver streak, cream blouse and sage bodice. No basket, hands, tools or vegetables. Match thick clean dark brown contours, narrow cream cut-paper border, large simple oval brown eyes and clean angular two-tone shading. No realistic detail, grain, hatching or fabric texture. Top LEFT neutral attentive, relaxed closed mouth. Top RIGHT warmly friendly happy smile. Bottom LEFT surprised, raised brows and small open rounded mouth. Bottom RIGHT gently worried, raised inner brows and downturned mouth, not angry. Preserve same middle-aged face, hat, hairstyle and three-quarter angle in all cells. Transparent margins around each full hat so nothing crosses cell boundaries. No labels, grid, backgrounds, shadows or glow. Actual alpha transparency.

</details>

### Planned courtyard work before the Arkanmeisterin arrival

Planning note (2026-11-04); this is a backlog, not a statement of implemented behavior or authorization to implement every proposal.

Agreed next sequence to retain:
1. Refine Hannes' brushing and Ida's planing: consistent tool contact, pauses, small head movements and restrained work sounds.
2. Refine conversation transitions: stop work, face Elian, use line-appropriate expressions and resume work after dialogue.
3. Personalize the stable, workshop and garden with a few readable occupation-specific props while preserving clear approaches.
4. Complete Marta's small watering activity: pick up the watering can, water vegetables, show feedback and acknowledge completion in dialogue.

Additional proposed pass before staging the Arkanmeisterin:
- Review the courtyard from the gameplay camera: tower/arrival on the left, castle silhouette, resident stations and gate on the right remain readable.
- Unify tower/castle masonry scale, painted materials, roofs and architectural details; check overlap and unwanted occlusion.
- Shape gentle terrain transitions, worn path edges, grass, soil and restrained vegetation without compromising movement.
- Establish warm sunlight, cool sheltered areas, contact shadows and restrained atmospheric effects that preserve character readability.
- Check camera framing, character visibility, collision, stairs, narrow approaches and spawn/return positions.
- Make interaction prompts consistent; hide distracting markers and avoid repeated tutorial text.
- Add localized environmental sound and balance it against dialogue, footsteps and music; keep automated tests silent.
- Give optional resident conversations a clear purpose and warm relationship to Elian, without prematurely revealing the arrival lore.
- Make the gate, external stream and drawbridge visually coherent; reserve an unobstructed staging area for the later arrival.
- Review the complete tower-to-courtyard route, optional activities, dialogue cancellation and scene reloads in a focused playable test.

The arrival sequence, starter introductions/selection, new castle interiors and cellar gate story follow this courtyard pass. See TODO.md for the current agreed direction, open design decisions and completion criteria. This roadmap records future work; the implemented project version is listed in Development setup.

### Active courtyard checklist

The maintained checklist for courtyard polish and the surrounding landscape is [TODO.md](TODO.md). It expands the planning note above and is the current place to track open tasks and reviewed completion.

### Hannes' horses and local stable sounds

The stable now has two open bays with low gates: a feeding chestnut by a small hay rack and an alert dun horse with a dark mane. The front roof overhang is reduced for visibility. The existing solid stable boundary and free approach to Hannes remain; the horses are ambient residents, not rideable or independently roaming animals.

`ConfigureStableHorses.Run` imports the two source images in `Assets/Art/Intro/StableHorses`, creates native sprite assets in `Assets/Resources/StableHorses`, configures mono audio imports and updates only the stable in StartScene. The full courtyard builder also includes the horses on later rebuilds. Generation used the built-in image tool; exact prompts and saved paths are in `docs/paper-art-generation.json`.

`CourtyardHorse` animates a subdivided illustration mesh for continuous breathing, small neck nods, ear twitches and occasional tail movement. Hoof-height vertices and world anchors stay fixed. The two horses have different timing; runtime meshes and materials are released on destruction.

`StableLocalFoley` attenuates sounds using the player's distance (fully faded at 14 units), supplies subtle stereo placement, ducks during conversations and mutes during scene transitions. Batch-mode tests never play these sources. The short natural snort is DenisChardonnet's CC0 recording from BigSoundBank; gentle brushing and straw rustling reuse Kenney CC0 cloth foley. Sources are documented in `docs/audio-sources.json`. No music was replaced.

Hannes uses an eased three-pose brushing rhythm with a longer rest, timed quiet brush cues, immediate silence when speaking and a short settling pause before resuming. This remains a three-pose sprite animation, not a full skeletal arm rig.

Focused validation: six stable/courtyard PlayMode tests passed in `Library/stable-horses-tests.xml`, including fixed hoof vertices, distinct work poses, conversation cancellation/reload, free approaches and player-distance audio attenuation. Unity captures: `Builds/stable-horses-detail.png` and `Builds/intro-courtyard-hannes.png`. The detail capture uses a closer inspection camera. After switching to the focused stable-only scene update, the three stable tests passed again in Library/stable-horses-final-tests.xml. Final in-game sound balance and visual preference remain for user review.

### Ida's planing rhythm and recorded workshop foley

`CourtyardCarpenterVisual` now starts with the tool drawn back, makes two deliberate cuts (0.62 seconds forward, 0.78 seconds returning plus a short settle), then rests before repeating. The resident stays fixed at her work spot. This remains an eased sequence of three illustrations, not continuous skeletal animation.

Two short mono excerpts of DeqstersLab's CC0 hand-plane recording alternate on forward cuts. `StableLocalFoley` provides the same player-distance attenuation and silent batch mode used by the stable. Cues stop on return, when Ida starts speaking or when the visual is disabled. Work remains paused through Elian's replies and resumes after a 0.45-second settling delay; other residents' dialogue endings do not reset her cycle. Source and processing details are in `docs/audio-sources.json`; `tools/render_workshop_audio.py` reproduces the clips from the public HQ preview using numpy and soundfile.

Validation: all three `CourtyardWorkshopTests` passed in `Library/ida-planing-polish-tests.xml` (silent PlayMode run). Checks cover work poses and fixed placement, unobstructed access, dialogue emotions and scene-reload subscriptions, mono clip availability, local audibility, interruption, settling delay and re-enabling without duplicate audio components. Updated inspection capture: `Builds/intro-courtyard-ida-detail.png`. Subjective motion and sound balance remain for in-game review. Released with the Ida polish in version 0.21.1.

### Patrolling Aurenfeld guard

The approved moustached guard is installed by `IntroChapter` in StartScene, alongside the existing courtyard residents. `CourtyardGuard` uses a kinematic capsule and follows the authored route in `Assets/Resources/Guard/Patrol.asset` from the castle forecourt, around the vegetable garden and wheelbarrow, to the right-hand gate and back. Stops include a four-second watch at the castle and five seconds beside the gate. The points, waits, facing directions and walking speed are editable on that asset; importing art again preserves route edits.

The guard waits for Elian and solid obstacles, follows the Ground-tagged terrain, and refuses missing ground or abrupt height changes. All dialogue and scene/opening presentations pause the patrol. Speaking to the guard turns him toward Elian and presents his own friendly/neutral portraits, with Elian's portrait on the player's replies. Closing or cancelling dialogue gives a short settling delay before the route resumes. This is a single fixed patrol, not general crowd navigation: an obstacle left on the route makes him wait rather than find another path.

`CourtyardGuardVisual` uses six separate illustrated paper parts: torso, two legs, two arms and sheathed sword. Both legs share a distance-driven phase offset by half a cycle; the arms swing oppositely. Fixed hip and shoulder pivots keep joints connected. The entire puppet follows the lowest boot-outline point for ground contact, and motion settles over a short blend before returning to the idle sprite. Facing changes only between left and right. The earlier eight-frame sheet repeated the leading leg and is retained as unused source art only. A shared paper contact shadow anchors the figure. The images retain the approved green tabard, light armour and Aurenfeld star; additional surprised/worried portraits are available for later conversations. The current conversation does not trigger the Arkanmeisterin's arrival or advance the story.

Use **Arkmon > Import courtyard guard art and patrol** (`ConfigureCourtyardGuard.Run`) to reproduce sprite slicing and portrait assets. The importer aligns feet from source alpha bounds and creates default route data only when absent; it does not rebuild or save the courtyard scene. Source images are `Assets/Art/Intro/CharacterStyle/Guard concept.png`, `Guard idle.png`, `Guard puppet.png` and `Guard expressions.png`. Exact generation and transparency-refinement prompts are recorded in `docs/paper-art-generation.json`; the built-in image tool was used. No Python image editing was used.

The guard feature is included in version `0.22.0`, following the separate Ida polish in `0.21.1`.

Guard validation: all three `CourtyardGuardTests` passed in `Library/courtyard-guard-tests.xml`, including a complete out-and-back patrol with obstacle-overlap checks and watch pauses, player/prop waiting, dialogue speaker/portrait changes, cancellation/resume and scene-reload event cleanup. The full-route test runs at 4x game time; all tests use `-disableAudio`. Unity captures are `Builds/intro-courtyard-guard-patrol.png` and `Builds/intro-courtyard-guard-dialogue.png`. Earlier route attempts correctly stopped at the garden border/wheelbarrow; the final route goes around both. Walking style and character scale remain subject to player review.

Guard gait correction: replaced the repeated-leading-leg frame sequence with `Guard puppet.png` and six native sprite parts. The importer fits convex alpha outlines to the two legs; the continuous gait uses those outlines for boot contact without changing source pixels. A focused pose test verifies leading-foot reversal after half a cycle, opposite arm swing, stable hip/shoulder positions, left-facing mirroring and ground contact throughout 32 sampled phases. All four guard tests passed in `Library/guard-gait-final-tests.xml` (40.6 seconds of test execution, silent). Close-up Unity captures: `Builds/guard-gait-near-foot-forward.png` and `Builds/guard-gait-far-foot-forward.png`. This correction is included in the guard feature in version 0.22.0.

### Marta's watering task (0.23.0)

The optional garden task now keeps the watering can visible beside Elian after pickup. At each of the three vegetable beds, [E] starts a 1.9-second pour with animated droplets, the existing water sound and gradually darker soil. Exploration pauses during the pour; [Esc] cancels it. A bed counts only after the pour finishes. Cancellation or scene unloading removes the transient effect and restores an unfinished bed's dry appearance.

`CourtyardGarden` owns the session progress, carried prop, pouring lifecycle and objective text. `DemoOpening` displays that objective and includes watering in the existing presentation lock. `MartaConversation` offers progress-aware lines, thanks Elian after all three beds are watered and reminds him to return the can. Her thanks is recorded when its friendly line actually appears, so closing the preceding surprise line does not silently finish that step. Completed beds, the carried can and acknowledgement survive courtyard reloads within the current play session; a new session resets them. This is not disk-based saving.

The can can also be put down before the task is complete and picked up again. Inspecting a watered bed never repeats the completion celebration. The watering can is now a drawn, transparent 2D sprite. `PaperPlayerGardenRig` combines new family/room torsos and articulated cream-sleeved arms with the existing legs. Two-segment arms follow the handle and support point; the near hand stays in front of the handle while the can tilts. Carrying retains alternating walking legs and left/right facing. Picking up and putting down each include a .55-second bend and .55-second rise. The inventory changes only after the animation finishes; cancelling restores the previous carried/placed state.

Before reaching, Elian takes a short authored step to the can or bed. The approach is rejected if the actual body sweep or destination overlaps an obstacle, and cancellation restores gravity, collisions, interpolation and movement state. All garden animations share the presentation lock and support [Esc]. The illustration is `Assets/Art/Intro/CharacterStyle/Elian garden puppet.png`, generated with the built-in image tool; the complete prompt and references are recorded in `docs/paper-art-generation.json`. Reimport via **Arkmon > Import Elian garden animation art** (`ImportElianGardenRig.Run` in batch mode). Source pixels/alpha remain untouched; native sprite metadata defines the six parts. Scale, joint appearance and audio balance remain subject to player review.

Validation: six focused PlayMode tests passed in `Library/garden-animation-final-tests.xml` (60.8 seconds of test execution, with audio disabled). They cover unfinished/cancelled pours, effect cleanup, scene reload during watering, session reset, all three beds, one-time celebration, interrupted thanks, can return, both facing directions, hand/handle alignment, cancelled pickup/placement/approach, blocked approaches, physics restoration and the courtyard route. Unity close-ups: `Builds/garden-pick-up-right.png`, `Builds/garden-pick-up-left.png`, `Builds/garden-carry-closeup.png`, `Builds/garden-put-down-closeup.png` and `Builds/garden-pour-closeup.png`. The close-up test temporarily changes the camera for review only; gameplay framing is unchanged.

The garden task and illustrated player animation are included in version `0.23.0`.

Garden arm correction: the generated forearm cells also contained upper-arm/shoulder artwork. `ImportElianGardenRig` now slices only the lower 65% of those cells at the original pixel density. The reach pose brings the can closer while standing and bends farther when picking it up; the upper sleeve overlaps the elbow cut instead of exposing another shoulder shape. Its open cuff is trimmed from the lower 12% so no second sleeve opening remains at the joint. Source image pixels remain unchanged.

John grounding correction (0.23.1): his `StartScene` prefab override placed the foot-pivoted NPC at Y=1. It now uses the courtyard surface at Y=0.005. `ConfigureIntroCourtyard` uses `GroundHeight + 0.005` so regeneration preserves this placement. No global NPC prefab or battle scene is changed. Two silent targeted tests passed in `Library/garden-arms-final-tests.xml` (14.0 seconds): garden handle alignment/cancellation in both directions and John's foot contour over idle frames, dialogue and scene reload. Visual review: `Builds/courtyard-john-grounded.png` and the updated garden close-ups.


### Courtyard refinement releases

- **0.23.2** — Add source illustrations, reproducible sprite importers and shared courtyard materials/shaders. Record generation provenance and preserve existing character artwork.
- **0.24.0** — Dress the grooming area, align Hannes to the horse, and add reachable individual horse greetings. Pause grooming during dialogue and add a documented recorded hoof cue. Existing silent stable-care tests cover interaction, reload and clearance.
- **0.25.0** — Dress Ida's bench and house, add workpiece observation dialogue, and pause work and audio immediately when conversation starts. Focused workshop tests cover access, cancellation and unchanged quest progress.
- **0.25.1** — Replace placeholder vegetables, add spout-following streams and persistent wetness feedback, and clear the garden frontage. Preserve Marta's source aspect ratio and add a brief completed-bed reaction. Include targeted watering and dialogue regressions.
- **0.25.2** — Add collider-free illustrated ground cover and soft path edges between work areas. Bring the courtyard camera from 30 to 25 and use linear distance fog. Include shader, interaction and reload checks.
- **0.25.3** — Apply a common painted masonry palette to the castle, residence and outer walls. Add recessed windows and an arched throne-hall portal while preserving entrance inspection and collision.
- **0.25.4** — Replace the surface cellar door with a sealed underground approach, walkable ramp, stairs and retaining masonry. Place prompts at the actual lower landing and cover descent, return and reload in PlayMode tests.
- **0.25.5** — Bring the eastern cottage into the shared illustrated palette, add facade details and a grounded approach. Preserve access beside the cellar and verify decoration does not block exploration.
- **0.25.6** — Connect garden, buried cellar and eastern cottage with fading earth paths and low planted meadow relief. Update the terrain collider and move the guard route clear of the cellar retaining wall. Final silent landscape, cottage and complete patrol checks passed (3/3).

- **0.25.7** — clear the castle approach and refine watering. Move the compact kitchen beds beside Marta's cottage and keep the castle/patrol corridor clear. Align watering with the resized beds, lower the garden sign, and group low meadow planting beside paths. Include approach, watering and reload regressions.
- **0.25.8** — establish golden morning light and gentle haze. Align the saved scene and authoring tools around warm low-angle sunlight, a golden horizon and readable ambient fill. Preserve mountain colors with a gradual 180-1600 m fog range and opt-in scene sunlight on meadow materials.
- **0.25.9** — connect terrain and cultivate the golden fields. Blend the western breach and eastern bank into the meadow, dress the gate/bridge/stream, and protect the return crossing. Extend the winding road over cultivated distant hills. Add clustered ripe grain, shared wind/shadow deformation, grounded root contacts and distance-based LOD crossfades. Preserve scene-specific behavior and include terrain, crossing and meadow regressions.
- **0.26.0** — add a reversible flight over Aurenfeld. Offer an optional lookout interaction at the western breach with a ten-second scenic flight, illustrated mountains/clouds and return controls. Block exploration input during presentation and restore camera, HUD and controls on return, cancellation or scene teardown.
- **0.26.1** — add painted tree varieties and grove shadows. Introduce illustrated oak, birch, apple and willow variants with reproducible sprite import settings and generation provenance. Add alpha-clipped crown shadows and terrain-following tree contacts. Cover grove lifecycle, lookout cancellation/reload and ten camera-flight checkpoints.
- **0.26.2** — connect the painted farm to the field road. Add a distant illustrated farm with chimney-anchored rising smoke, a curved access lane, a trampled forecourt, an apple tree and low fence planting. Share lane/forecourt masks with grain generation so the entrance stays clear. Include supported-material, road-visibility and reload checks; the farm remains scenic and non-colliding.
- **0.26.3** — soften wall foundations and animate undergrowth. Ground enclosing walls with bounded moss, earth transitions and low planting while preserving the gate and patrol access. Animate illustrated plants with restrained rooted wind, retaining their colors and mirroring. Include material restoration, movement and reload checks.
- **0.26.4** — ground props and refine local morning accents. Make figure shadows follow supporting slopes and hide them when no ground is found. Add soft contacts beneath workshop/garden furniture and reversible local sunlight accents at the tower, breach, gate and garden. Include rendering/lifecycle regressions and reconcile the README and remaining courtyard tasks.

### Implementation and validation notes

The following development notes describe the courtyard work included through 0.26.4. Earlier proposed-commit/version references in historical sections are superseded by the release list above.

### Illustrated details at Marta's garden

`CourtyardGardenDetails` adds a seed organizer with gloves and twine to the existing garden bench, a seedling tray on the potting table, a wall-mounted hand-tool rack and cabbage/carrot/sprout markers inside the three vegetable beds. Two old table pots are hidden and the cloth is moved aside to make space. The details are installed only in StartScene, stay attached to the garden cottage hierarchy and add no colliders or interaction targets. Camera-facing props align after the courtyard camera updates; the wall rack remains fixed to the house.

Source art: `Assets/Art/Intro/CharacterStyle/Marta garden details.png`. **Arkmon > Import Marta garden details** creates six native Sprite assets in `Assets/Resources/GardenDetails` from transparent bounds, without changing source pixels. Generation prompt and reference are recorded in `docs/paper-art-generation.json`.

The existing complete garden-task check covers watering all three beds, Marta's acknowledgement and returning the can. Silent results: `Library/marta-details-tests.xml`. Inspection captures: `Builds/marta-garden-workspace.png` and `Builds/marta-garden-beds.png`; these use temporary review framing and do not change the gameplay camera. This pass decorates Marta's workstation. The house remains prototype scenery; illustrated vegetables are described below.

### Painted vegetables and watering polish

`CourtyardVegetableBed` replaces the old crop renderers inside the three beds with illustrated cabbage, carrot greens and young bean plants. Two cabbage/carrot variants, deterministic small size/position differences, occasional grass and low earth clods vary the rows. Borders, trellis and watering points remain in place. An unscaled art hierarchy avoids shearing under the soil mesh; decorations have no colliders.

**Arkmon > Import illustrated garden vegetables** (`ImportGardenVegetables.Run`) slices `Assets/Art/Intro/CharacterStyle/Garden vegetables.png` into six native sprites in `Assets/Resources/GardenPlants`. Source pixels are unchanged. The built-in image tool, exact prompt and style reference are documented in `docs/paper-art-generation.json`.

`GardenWaterEffect` adds five fine curved streams, falling drops and small landing splashes. It follows the can spout and gently sweeps the front plants. This is decorative animation, not fluid simulation or leaf collision detection. Soil, plants and earth clods share progressive wetness; completed wetness is restored directly from session progress after reload. Cancellation resets incomplete wetness and removes the effect, audio and temporary material.

`CourtyardGardenerVisual` briefly looks at the completed beds with the existing happy nod before turning back to Elian. Dialogue interrupts the glance. Her personal thanks remains in the conversation, and the glance does not mark it as read.

Validation: six checks passed in `Library/garden-polish-tests.xml`. The reload check exposed floating-point error when reconstructing wetness from soil color; initialization now reads task progress directly. The targeted rerun passed in `Library/garden-polish-reload-tests.xml`. Together these cover blocked approaches, mirrored grip, cancellation, scene reload, completion/thanks/returning the can, garden-to-player facing and dialogue/event behavior. All runs are silent. Unity captures: `Builds/marta-garden-beds.png`, `Builds/garden-water-polished.png` and `Builds/garden-task-complete.png`.

### Stable care and horse greetings

The existing chestnut and dun illustrations remain the horses' visual baseline. In StartScene, `CourtyardStableCare` places the dun on a small grooming spot outside the second bay and moves Hannes beside it. The saddle stand is parked at the left side of the stable. A hanging bridle, grooming caddy, spare hay and a water bucket use new paper artwork. The dun has a solid body collider; greeting anchors remain outside the stall boundaries. This is a runtime arrangement applied before residents spawn, not a rebuild of the courtyard scene.

Hannes uses `Hannes horse grooming.png` through four new native sprites. `ImportStableCare.Run` aligns each pose by opaque boot bounds and a common body height; image pixels are unchanged. His established three-pose stroke/rest rhythm now brushes the horse. Any open dialogue pauses grooming immediately, including Elian's first horse-greeting line; work resumes after the existing short settling delay. The ordinary Hannes greeting now refers to grooming the horse rather than cleaning the saddle.

`CourtyardHorseGreeting` supplies separate reachable prompts for the two horses. Elian greets the animal, it responds with a small head/ear reaction and quiet snort, and Hannes describes the feeding chestnut's calm nature or the dun's cautious curiosity. Speaker-specific portraits follow the dialogue lines. Existing conversations, transitions and remote calls cannot start a second greeting. Closing the conversation restores exploration and grooming. Repeat greetings use a shorter first line during the current scene visit; they are not saved to disk. This pass adds greeting, not a petting animation, riding or free horse movement.

`CourtyardHorse` also makes an occasional tiny hoof lift, followed by a quiet impact on settling. The cue is a 0.245-second excerpt of Joseph SARDIN's CC0 recorded hoof foley, with its source, edit recipe and license in `docs/audio-sources.json`. `tools/render_stable_hoof.py` reproduces the clip. Snorting, brushing and straw rustling retain player-distance attenuation, dialogue ducking and silent batch-mode behavior.

Use **Arkmon > Import stable care artwork** to reproduce native sprite slicing. Source art: `Assets/Art/Intro/CharacterStyle/Hannes horse grooming.png` and `Stable care details.png`; prompts and references from the built-in image tool are in `docs/paper-art-generation.json`. Earlier source art and native sprites are retained.

Validation: all four focused PlayMode cases passed in `Library/stable-care-final-tests.xml` (25.8 seconds on the first complete pass). Coverage includes fixed work anchors, approach clearance, both reachable horse prompts, speaker/portrait changes, work interruption/resumption, remote/overlapping greeting rejection, reload cleanup, horse motion, hoof lift/settling and audio attenuation. Final art alignment is checked separately in `Library/stable-care-art-tests.xml`. Review captures: `Builds/stable-care-overview.png`, `Builds/stable-care-GroomForward.png`, `Builds/stable-care-GroomMiddle.png`, `Builds/stable-care-GroomBack.png` and both `Builds/stable-*-greeting.png`. The overview is a temporary inspection camera; gameplay framing is unchanged. Movement feel and sound balance remain for user review.


### Ida workshop details and workpiece observation

`CourtyardWorkshopDetails` dresses the existing workshop at runtime: an illustrated tool rail, carpenter caddy, offcut basket, spare timber and visible curled shavings replace simple placeholder props. The planing blank is shortened to match Ida's existing three work poses. The repaired garden-trowel head, cracked grip and replacement grip make her dialogue about Marta's tool visible on the bench. The main door and front interaction approach stay clear; the old timber collider is reduced to the new stack's footprint.

At the right side of the bench, **[E] Werkstück ansehen** opens a six-line conversation between Elian and Ida with their appropriate portraits/emotions. `CourtyardWorkshopWorkpiece` checks distance and rejects interaction during another conversation, a transition or the opening. Ida pauses immediately even when Elian speaks first, stops the plane sound and waits briefly before resuming after completion or cancellation. This observation changes no quest, inventory or garden progress.

Use **Arkmon > Import workshop details artwork** to reproduce the six native sprite slices. The unchanged generated source is `Assets/Art/Intro/CharacterStyle/Ida workshop details.png`; the exact prompt and Ida style reference are recorded in `docs/paper-art-generation.json`.

Validation: five focused PlayMode cases passed in `Library/workshop-details-final-tests.xml` (32.9 seconds of test execution). They cover work poses/anchors, door access, dialogue moods and cleanup after reload, audio interruption/resumption, reachable workpiece selection, cancellation, repeated/remote interaction rejection and unchanged garden progress. `Builds/workshop-details-overview.png` and `workshop-details-Work*.png` use a temporary close inspection camera. `Builds/courtyard-review-ida-workshop.png`, `courtyard-review-stable.png` and `courtyard-review-garden-cottage.png` use the unchanged, serialized gameplay camera; they must not be confused with the detail view.

A final focused clearance rerun after resizing the timber collider also passed (`Library/workshop-clearance-tests.xml`, 7.1 seconds).

Visual review: the three work areas have distinct activities and consistent illustrated props. Broader environment polish remains necessary, especially the angular earth-path edges and large smooth grass areas between them. Small tools are hard to read from the distant gameplay camera. These are follow-up items in `TODO.md`, not claims that the whole courtyard is finished. Ida's existing three-frame animation still needs user review in motion.


### Illustrated workshop and first courtyard verge section

Ida's existing 3D workshop now uses a generated four-surface atlas for oak, warm plaster, terracotta shingles and sandstone. `CourtyardWorkshopStyle` assigns these materials only to the workshop (including the workbench and entrance stones). `Courtyard Illustrated Surface` keeps grain aligned to beam direction, scales texture detail in metres, adds restrained edge ink and follows the scene's directional light, shadows and fog. The original source atlas is preserved. Use **Arkmon > Import courtyard illustration** to reproduce the materials and six native verge sprite slices.

`CourtyardVergePolish`, installed by `IntroChapter` after the residents, adds a bounded first pass from Ida's workshop to Hannes' stable. It clones the affected path meshes, assigns a softly irregular alpha edge inside that section and retains the original material on triangles outside it. A depth offset avoids fighting with intersecting paths. Deterministic groups of drawn grass, clover, ferns, small flowers and mossy stones decorate verges and building corners. Decorations have no colliders and avoid solid furniture, interaction points and path centers. Runtime mesh/material copies are restored and destroyed on teardown; the authored scene geometry and navigation remain intact.

The StartScene camera distance is reduced from 30 to 25, preserving its 25-degree pitch, 48-degree field of view, movement limits and damping. This is a closer framing trial for user review. In the editor, **restart StartScene in Play Mode** to see the runtime material and verge dressing. No whole-courtyard rebuild is needed.

Validation: both focused PlayMode cases in `CourtyardIllustrationTests` passed after refinement (`Library/courtyard-illustration-final-tests.xml`, 12.8 seconds of test execution, audio disabled). Checks cover supported rendering shaders, non-blocking decoration, workshop/stable approach clearance, workpiece focus selection and single installation after reload. Real Unity captures: `Builds/workshop-illustration-detail.png` uses a temporary inspection camera; `Builds/courtyard-polish-gameplay-ida-workshop.png` and `Builds/courtyard-polish-gameplay-stable.png` use the new gameplay framing. The initial orange palette was toned down, scene fog restored and vegetation inside solid furniture rejected after inspecting the first captures.

The illustrated surface style now covers Ida's house and Hannes' stable (see below); the garden cottage is extended below; castle materials remain their existing versions. This is a reviewable first section, not a completed art pass on the entire courtyard. Remaining broad grass areas, the far-right path section and further style propagation are tracked in `TODO.md` for feedback-driven follow-up.


### Illustrated stable, horse care ground and connecting planting

`CourtyardStableStyle`, installed by `CourtyardStableCare`, extends the workshop's painted architectural style to the stable with its own darker oak and muted green shingles. It reuses the warm plaster and sandstone materials for continuity. Material replacement is restricted to the original stable building/wood materials and explicitly excludes the deforming horse meshes, leather, iron, water and hay. All building colliders, horse anchors, grooming poses and greeting points remain intact.

Three collider-free ground meshes add a worn earth patch at the care spot and restrained straw around the feeding area/inside the first bay. They follow the existing ground with feathered irregular edges. `Courtyard Painted Ground Patch` renders before the softened paths and paper contact shadows, retaining visible grounding beneath the figures. The old small block-shaped forecourt straw pieces are hidden. Runtime meshes are destroyed on teardown.

`CourtyardVergePolish.ConnectWorkplaces` adds three low, overlapping clover/grass islands between the workshop and stable. Placement uses the same checks against solid objects, path centers and interaction points as the existing verge pass. Stable corner planting is moved slightly outward so it sits outside the foundation collision.

Use **Arkmon > Import illustrated stable surfaces** to recreate the four materials from the original `Assets/Art/Intro/CharacterStyle/Hannes illustrated surfaces.png` atlas. Its complete built-in image-tool prompt and architectural reference are in `docs/paper-art-generation.json`. Restart **StartScene in Play Mode** to see the dressing; camera framing remains the previously approved trial distance of 25.

Validation: all three focused `CourtyardIllustrationTests` passed in `Library/stable-illustration-final-tests.xml` (16.4 seconds of test execution, silent batch mode). Coverage includes workshop/stable approach clearance, workpiece focus, both horse greeting prompts and opening dialogue, preserved transparent horse artwork, supported materials, shadow/ground rendering order, collision-free decoration and single installation after scene reload. `Builds/stable-illustration-detail.png` is a temporary inspection-camera view; `Builds/courtyard-polish-gameplay-stable.png` shows both buildings from the normal gameplay camera. Colors, planting density and visual balance still await user feedback; the garden cottage and castle are outside this pass.


### Illustrated Marta cottage and garden verges

`CourtyardGardenStyle`, installed after the vegetable beds, reuses the approved courtyard atlas on Marta's existing 3D cottage: painted oak framing, cream plaster, terracotta shingles, sandstone and distinct sage-green doors/shutters. `ImportGardenIllustration.Run` recreates the two garden accent materials (`Garden sage`, `Garden clay`) without changing existing material GUIDs. No additional generated source artwork is required.

The cottage's placeholder herb leaves are hidden and replaced with small illustrated fern sprites in the pots, window boxes and ground clusters. Garden bed borders, bean supports and the garden work bench use the same painted wood. Soil materials/property blocks remain owned by the watering system, preserving dry/wet feedback. Existing geometry, building collisions, character poses and interaction anchors are unchanged.

The soft path and non-blocking verge planting now extend to the last vegetable bed. Additional low growth sits behind and outside the beds. Placement excludes the crop rectangles with a half-metre margin and retains existing checks against interactions, solid objects and path centers. No new decoration colliders are added.

Validation: all five selected PlayMode tests passed in `Library/garden-illustration-tests.xml` (40.7 seconds test execution, audio disabled). They cover all four watering task cases plus material support, clear approaches and interaction focus at the can and beds, unchanged soil ownership, and a single garden-style installation after reload. Real captures: `Builds/garden-illustration-gameplay.png` uses the normal gameplay camera; `Builds/garden-illustration-detail.png` and `Builds/garden-illustration-beds.png` use temporary inspection cameras. The fountain and front bench were subsequently rearranged as described below. Castle materials remain unchanged; the entire courtyard is not yet visually complete.

Restart StartScene in Play Mode to see the runtime dressing. Suggested coherent commits: `feat(art): illustrate Marta cottage and garden furnishings` (importer, accent materials, local styling and integration) and `feat(courtyard): extend soft verges around vegetable beds` (bounded path/planting extension and access checks). These changes remain uncommitted; metadata remains `0.23.1`, with `0.23.2` proposed for the first pending commit only.


### Marta garden place: open frontage and side seating

`CourtyardGardenPlace.Install`, after the garden material pass and before verge planting, disables the authored fountain (including its collider) and large front bench in Play Mode. The original scene objects remain available for a later courtyard square. A copy of the bench becomes a smaller painted side seat facing toward the vegetable garden. Its existing solid collider scales with the furniture; no new sitting interaction is introduced.

The cottage approach stones are narrower and slightly staggered. `CourtyardVergePolish` identifies only the cottage spur by its authored starting center (4,-2), clones its mesh and reduces its width to 60 percent with a slight lateral variation, following the ground. The main route and other spurs keep their geometry. The normal soft-edge pass still applies. A feathered, collider-free earth patch marks Marta's potting workspace; low clover, grass and a few stones link the cleared frontage toward the stable. Existing interaction, crop and obstacle exclusion checks protect the usable space.

Validation: both targeted PlayMode tests passed in `Library/garden-place-tests.xml` (18.7 seconds execution, audio disabled). Checks cover the removed active fountain/front bench, side-seat overlap against solid objects, clear cottage and watering approaches, focus selection, single installation after reload, and the complete three-bed watering/thanks/can-return sequence. Updated screenshots `Builds/garden-illustration-gameplay.png` (normal game camera) and `Builds/garden-illustration-detail.png` (inspection close-up) were visually reviewed. Restart StartScene in Play Mode to see the arrangement; seating placement and density remain open to player feedback.


### Marta silhouette and illustrated throne-hall portal

Marta's world sprite now preserves the source drawing's 1024 x 1536 aspect ratio. Its previous power-of-two import stretched it to 1024 x 2048, making the character look too narrow. `ConfigureMartaEmotions.RepairWorldSprite` disables that rescaling for this texture and rebuilds the native sprite while preserving its GUID, 2.26-unit world height and boot pivot. The mistaken extra 10 percent runtime height adjustment has been removed; the baseline is (1, 1, 1). Breathing remains subtle; expression squash is reduced from 1.4/2.5 percent to 0.3/0.6 percent. Original image pixels and dialogue portrait assets are unchanged. The normal Marta configuration command also retains the corrected import setting.

`CourtyardCastleEntranceStyle`, installed with the other courtyard dressing, locates the existing ThroneHall inspection anchor and replaces only the nearby closed entrance's visible door/trim. It builds twelve extruded oak planks under a round arch, deep limestone jambs and segmented arch stones, a keystone, threshold, two sets of iron straps, brass studs and door pulls. The frame uses a muted painted plaster atlas region for carved limestone; the door uses the stable oak atlas with a longer vertical grain. Generated meshes and local materials are released on scene teardown. No new image asset is generated.

The original Aurenfeld crest image/material is reused on a shield-shaped mesh fixed above the door handles. Its geometry does not copy the courtyard banner's movement component. Solid jambs and a front-aligned closed-door collider keep the illustration physically grounded, while the existing **Burgeingang ansehen** interaction and Elian's throne-hall line remain unchanged. This pass adds no opening animation, interior transition or new quest; the neighboring castle walls remain for a later art pass.

Validation: two final focused PlayMode cases passed in `Library/castle-entrance-final-tests.xml` (13.5 seconds execution, audio disabled). They cover approach clearance, a solid closed door, interaction focus and dialogue, fixed crest placement, supported materials, reload uniqueness, Marta's then-current baseline and all four existing dialogue emotions/speaker changes. Real screenshots: `Builds/castle-entrance-gameplay.png` uses the normal game camera; `Builds/castle-entrance-detail.png` and `Builds/marta-proportions-detail.png` are inspection views. The first image review led to less repetitive door grain and removal of inherited banner motion.


Marta aspect-ratio follow-up: the original width correction is verified in `Library/marta-aspect-tests.xml` (2/2 focused PlayMode cases passed, 13.2 seconds, audio disabled). The checks assert the imported 1024 x 1536 texture, original 2:3 sprite aspect, unchanged 2.26-unit height, dialogue reactions and reload behavior. `Builds/marta-proportions-detail.png` has been recaptured and visually reviewed with the corrected proportions. Use **Arkmon > Repair Marta sprite proportions** to reproduce the native sprite without rebuilding dialogue portraits.


### Illustrated central castle facade

`CourtyardCastleFacadeStyle` extends the entrance palette to **Castle hall**, **Castle upper keep** and its two flanking towers. The existing stone atlas supplies outlined masonry with metre-based texture sizing; the plaster atlas supplies muted dressed-limestone trim matching the portal. Existing structure meshes, dimensions, solid colliders and navigation remain intact. The outer walls and western residence are extended in the shared-palette pass below; the cellar storehouse retains its previous finish.

The original rectangular window drawings/reveals are hidden on these four buildings. New dark round-arched panes, projecting stone jambs, segmented arches, layered sills and thin glazing bars give the openings more depth. Battlements receive shallow capstones. Geometry is combined into three material batches per building, rather than separate renderers for every arch stone; these decorative batches have no colliders. Two existing banner crests reuse their artwork on shield-shaped meshes to remove the rectangular backing, retaining their original banner movement. No raster image is regenerated. Runtime meshes and local material instances are released on scene teardown.

Validation: the focused entrance/facade PlayMode case passed in `Library/castle-facade-tests.xml` (7.5 seconds execution, audio disabled). It verifies preserved building colliders, supported facade/window materials, collider-free decoration, unchanged cottage material, portal approach and collision, the inspection dialogue, Marta's corrected proportions, and one facade installation/four window-surround batches after reload. Real gameplay and detail images were visually reviewed at `Builds/castle-entrance-gameplay.png` and `Builds/castle-entrance-detail.png`. Restart StartScene in Play Mode to see the changes; facade palette and window proportions still await player feedback.


### Courtyard architectural color and contrast

The illustrated architecture shader no longer uses a fixed 0.78 saturation factor and large constant shadow fill. It exposes paint saturation (0.98), local contrast (1.05 around a 0.18 linear-light pivot), ambient fill (0.43), sunlight strength (0.50) and retained sunlight in shadow (0.28). These defaults retain more of the original painted colors and separate sunlit surfaces from shade without increasing scene exposure. They apply to the courtyard materials sharing `Courtyard Illustrated Surface`, including the central castle, illustrated houses and their wooden furnishings; character sprite shaders and UI are unchanged.

StartScene fog is now linear from 30 to 190 world units instead of exponential-squared density 0.01. This reduces the pale veil over courtyard buildings while retaining a distant atmospheric fade. Camera angle, position settings, directional-light settings and post-processing volumes are unchanged. The viewing distance previously contributed to the washed-out look through fog; perspective itself was not the main cause.

Validation: both selected existing PlayMode cases passed in `Library/courtyard-color-final-tests.xml` (13.1 seconds, audio disabled). Castle access/reload/material checks and workshop/stable game/detail captures were reused, with no new color-only tests. Castle, cottage, workshop and stable captures were visually inspected; an initially stronger orange treatment was reduced before the final pass. The castle's previous matching captures are retained as `Builds/castle-entrance-gameplay-before-contrast.png` and `Builds/castle-entrance-detail-before-contrast.png`; current captures keep their normal names. Color balance remains subject to in-game user feedback.


### Shared castle, residence and enclosing-wall palette

`CourtyardCastleFacadeStyle` now extends the central castle's illustrated stone and dressed-limestone trim to the player tower, its entrance, west wing, upper residence, connecting wing and raised approach. Explicitly named enclosing walls, battlements, broken-wall stones and eastern gate masonry share that palette. Replacement is restricted to the original architectural materials and named roots; blue slate roofs, wood, iron, ivy and other furnishings retain their own finishes. Residence crests also reuse the shield-shaped mesh. No new source artwork is required.

The shared masonry enables the shader's optional face-aligned projection for vertical faces. This keeps the apparent stone width consistent on oblique octagonal tower facets and straight walls, with object scale still supplied per renderer. Cube trim retains restrained edge ink; custom meshes do not receive inappropriate cube-edge outlines. Geometry, collisions, camera framing and interaction anchors are unchanged.

Validation: both targeted PlayMode cases passed in `Library/castle-palette-tests.xml` (16.7 seconds execution, audio disabled). They cover shared supported materials, tower texture scale/projection, preserved boundary colliders, tower interaction focus, scene reload and actual stair ascent/descent. Five captures use the unchanged gameplay camera: `Builds/castle-palette-tower.png`, `castle-palette-west-residence.png`, `castle-palette-castle-connection.png`, `castle-palette-central-castle.png` and `castle-palette-east-wall.png`. Tower, west residence, castle connection and east-wall views were visually reviewed. Restart StartScene in Play Mode to see the runtime styling. The cellar storehouse and eastern cottage still need their own pass; this does not complete the entire courtyard.


### Exterior cellar access trial

`CourtyardCellarEntranceStyle`, installed after the castle facade, replaces the visible cellar storehouse with a low stone service entrance beside the castle. It reuses the courtyard stone/plaster/oak artwork: a segmented round arch and barrel vault, a short masonry connection to the castle foundation, a heavy strapped oak door with the family seal, an amber lantern, retaining walls, four descending treads, a lower landing and a small worn-earth/flagstone approach with ferns. No new image assets are generated. The nearby eastern cottage remains outside this pass.

The entrance descends 0.75 world units. Only runtime mesh copies of the intersecting courtyard ground/meadow/path are clipped to the rectangular stairwell; the surrounding floor and authored scene assets remain intact. A sealed convex ramp with buried ends and low friction carries the player over the visible steps without catching on the landing or upper lip. The old storehouse collision is disabled and replaced with matching body, vault, side-wall and closed-door collision. Local meshes, materials and the physics material are released on scene teardown.

The existing cellar inspection anchor moves to the lower landing, retaining Elian's inspection dialogue. The door remains closed: this adds neither a cellar interior nor the sealed mystical story gate. The exterior service entrance is a visual trial for user feedback; the story gate is still intended for the interior below the castle. Restart StartScene in Play Mode to see the runtime layout.

Validation: both focused PlayMode cases passed in `Library/cellar-entrance-final-tests.xml` (17.8 seconds execution, audio disabled). The cellar test walks down and back up with the real player body, checks inspection focus/dialogue, closed-door blocking, excavated floor height, preserved neighboring ground, correct vault collision and reload uniqueness. The existing tower stair ascent/descent case also passes. `Builds/cellar-entrance-gameplay.png` uses the normal game camera; `Builds/cellar-entrance-detail.png` is a temporary inspection view. Both were visually reviewed. Initial tests exposed stair lip collisions; overlapping ramp ends resolve them. Palette, scale and the new exterior location remain subject to user review.


### Buried cellar entrance at the castle foundation

The exterior-access trial above has been revised: the doorway now sits entirely below courtyard level at the eastern castle foundation, 1.5 units west and 3.9 units behind its first trial position. The landing is 3.95 units below the courtyard, reached by eighteen treads in an approximately 8.6-unit stair run. Full-depth retaining walls enclose the excavated ground; only low coping remains above ground. The portal/vault are sunk by 3.2 units and the former tall foundation link is replaced with a low connection beside the castle. The door remains closed with the existing inspection dialogue; the actual cellar rooms and story gate are still future work.

The buried geometry reuses the existing ground clipping and smooth ramp, with submerged endpoints and matching landing/side collision. The lantern moves to the inside face so the retaining wall does not hide its glow. Lower ambient material fill helps distinguish the stairwell from the courtyard. Only the cellar interaction marker uses its landing height; its label is raised to leave Elian's head visible. Other interaction marker heights and the gameplay camera are unchanged.

Validation: the focused PlayMode regression passed in `Library/cellar-underground-final-tests.xml` (20.2 seconds, audio disabled). It verifies the entire door below ground, landing depth, physical descent and return, closed-door collision, inspection dialogue, marker height and reload. `Builds/cellar-underground-gameplay.png` and `Builds/cellar-underground-bottom-gameplay.png` use the unchanged gameplay camera; `Builds/cellar-underground-detail.png` uses a temporary inspection camera. All three were visually reviewed. Suggested coherent commit: `feat(courtyard): sink cellar access below courtyard level` — deeper foundation entrance, longer walkable stair and excavation, interior lantern, corrected interaction marker and regression coverage. No commit/push or version increment; metadata remains `0.23.1`, with `0.23.2` proposed for the first pending commit.


### Illustrated eastern cottage and forecourt

`CourtyardEastCottageStyle`, installed after the cellar entrance in StartScene, gives the existing eastern cottage warm painted plaster, outlined oak beams, muted green shingles and sage shutters from the existing courtyard atlases. Its structure, location and original building solid are preserved. The old small facade details are hidden locally; three framed windows add dark panes, projecting jambs/sills, glazing bars and shutter braces. The planked closed door has iron fittings, a carved surround, a small supported canopy and a matching closed-door collider. The chimney is raised above the steep roof and capped. No new house interior, interaction or resident is introduced.

A feathered worn-earth patch, seven staggered flagstones and low fern/clover/grass groups connect the frontage to the courtyard. Ground height is sampled from the existing walkable surfaces; flagstones also follow the local slope. Decorative plants and stones have no colliders. The main eastward route and the passage beside the cellar remain clear. Scene-owned materials are reused; the two local glass/iron materials and ground mesh are released on teardown. No additional generated image assets are required.

Validation: the cottage clearance/reload test and the existing physical cellar stair test both passed in `Library/east-cottage-final-tests.xml` (28.4 seconds execution, audio disabled). Checks cover shader support, retained building solid, closed-door collision, free entrance/east/cellar approaches, terrain-following flagstones and single installation after reload. `Builds/east-cottage-gameplay.png` uses the unchanged gameplay camera; `Builds/east-cottage-detail.png` uses a temporary inspection view. Colors and proportions still await player feedback. Restart StartScene in Play Mode to load this runtime dressing.

### Buried arch closure from eastern views

Reviewing the cottage exposed a sky-colored gap above the neighboring cellar arch from oblique viewpoints. A shallow solid stone cap closes the ground cutout above the portal, and decorative masonry wedges fill between the curved arch and that level cap. Stair, door and inspection positions are unchanged. The cellar movement regression above includes the cap; the final visual closure was recaptured and reviewed with the cottage view test (`Library/east-cottage-view-tests.xml`, passed, 8.6 seconds, audio disabled). Suggested separate commit: `fix(courtyard): close gaps above buried cellar arch`. This remains part of the pending work, with no separate version increment prepared.


### Eastern courtyard ground, connecting paths and meadow groups

`CourtyardEastGroundStyle.PrepareTerrain` runs before courtyard furnishings sample their ground. It clones the authored meadow mesh and adds three bounded, shallow rises (maximum 0.26 units) in the eastern free spaces. Existing paths, the patrol corridor, cottage foundation and cellar opening keep their reference heights. The mesh collider follows the altered surface. Ground dressing is installed after the existing verge pass: three curved footpath connections join the garden edge, cellar approach and eastern house to the main route, with soft sides and fading endpoints. `CourtyardVergePolish` now extends the existing softened main-path treatment to x=43 at the gate.

Six low meadow groups mix drawn grass, clover, sparse flowers and subtle ground-color patches. Narrow earth/planting strips tie the cottage, cellar retaining walls and eastern boundary to the terrain. Positions follow the walkable surface. Plants exclude crops, entrances, the cellar shaft, path centers, solid props and the patrol corridor. Decorative meshes and sprites have no colliders. Existing artwork/materials are reused; runtime mesh/material instances are released on teardown. Camera and lighting settings remain unchanged. This is an eastern-section pass; wider courtyard landscaping and lighting remain future work.

Validation: the final three focused cases passed in `Library/east-landscape-final-tests.xml` (37.2 seconds execution, audio disabled): landscape placement/reload and actual player traversal over the new relief, eastern cottage/route clearance and a complete guard patrol to the gate and back. The existing cellar descent/return/inspection case passed in the preceding `Library/east-landscape-tests.xml` run. That earlier run exposed two issues: a flat-height clearance assertion intersected the intended ground rise (now paired with a physical walk test), and the old guard route approached the relocated cellar wall too closely (fixed below). Four real gameplay-camera captures were generated as `Builds/east-landscape-garden-link.png`, `east-landscape-overview.png`, `east-landscape-house.png` and `east-landscape-gate.png`; overview, house and gate were visually reviewed. Player feedback on planting density and path balance remains pending. Restart StartScene in Play Mode to see the new landscape.

### Guard clearance beside the sunken cellar

The second patrol point moves from (23.8,11.1) to (23.35,11.1); the third moves from (24.8,4.5) to (23.35,5). This keeps the guard in the available gap between the wheelbarrow and cellar retaining wall until it has passed the staircase, before heading toward the gate. Both the native `Guard/Patrol` resource and the editor's new-route defaults use these coordinates. The eight-stop route, pace, pauses, dialogue and obstacle response are retained. The complete patrol regression above passes in both travel directions.

### Aurenfeld golden daylight (local preview)

Art direction: Aurenfeld's outdoor light should feel warmly golden, comfortable and welcoming, with fresh meadow greens, readable warm shadows and a blue sky as a complementary color. The previous cooler, darker trial was rejected in favor of this regional identity. Use this direction for later outdoor areas; interior lighting remains separately authored.

StartScene retains the 42-degree sun elevation and -38-degree yaw, with honey-gold light (1, .88, .65), intensity 1.65 and shadow strength .88. Higher ambient sky/equator/ground colors lift shaded objects. A pale golden distance fog begins at 40 metres and ends at 200. The illustrated courtyard shader combines cream sky fill with warm earth bounce: ambient fill .52, directional contribution .64 and retained direct light in shadow .15. Directional shading and the reduced shadow bias still give beams, roof overhangs and facade projections depth.

The courtyard-only volume adds .25 stops exposure, modest saturation (+6), contrast (+2), restrained bloom (.14) and less edge darkening (.025 vignette). The grass base is brighter green (.40, .54, .22); ConfigureIntroCourtyard preserves this on regeneration. No camera, collision or gameplay changes are part of this color pass. The relocated vegetable garden remains intact.

Validation: three existing palette, work-area capture and garden access/reload checks passed silently in `Library/courtyard-golden-light-tests.xml`. Native Unity gameplay captures: `Builds/garden-illustration-gameplay.png`, `Builds/castle-palette-central-castle.png` and `Builds/courtyard-polish-gameplay-ida-workshop.png`. The corresponding `-before-golden-light.png` files preserve the previous images. The earlier release checks and garden-task results remain documented below.

This revised lighting pass remains uncommitted. Suggested commit: `style(courtyard): establish warm golden Aurenfeld daylight` (scene lighting, courtyard shader/volume, grass material and its generator). Current version is `0.25.6`; the next prepared commit would use `0.25.7`.

### Kitchen garden beside Marta's cottage (local correction)

The three vegetable beds no longer span the approach to the castle. `CourtyardGardenPlace.PrepareBeds` groups the original soil, wooden borders, crops and bean supports into three compact beds east of Marta's cottage, before terrain, interactions or decorative planting sample their positions. Their footprints are 72% of the original size and their order is preserved, so crop identity and saved watering bits remain consistent. A scene-local root prevents double installation. The seed bench and garden sign are moved beside the new layout; Marta retains her clear work spot at the potting table.

`CourtyardGarden` derives can/watering anchors from the relocated soil and targets the resized front plant row. `CourtyardVegetableBed` scales planting positions, plants and clods to the actual footprint. Restart StartScene in Play Mode to see this runtime dressing. No scene regeneration is needed, and the preceding local lighting changes remain intact.

Validation: the existing garden illustration/access check and four garden-task checks passed in `Library/garden-relocation-tests.xml`. Its new broad corridor sweep initially touched John at the edge; after narrowing that check to the actual central passage between John and the guard, the targeted physical walk/reload check passed in `Library/garden-route-final-tests.xml` (11.8 seconds). All six distinct checks now pass across those runs, with audio disabled. They cover walking from the main path to the castle, all three watering actions, cancellation/reload, thanks and can return. Real gameplay views: `Builds/garden-relocated-gameplay.png` and `Builds/garden-relocated-castle-approach.png`.

Suggested separate commit: `fix(garden): clear the castle approach with compact side beds`. This correction is not committed or pushed. Current version remains `0.25.6`; the next prepared commit would use `0.25.7`.


### Grouped courtyard meadow islands (local preview)

`CourtyardVergePolish` now adds eight bounded planting islands beside the western approach, between workplaces and along the south side of the main path. Existing drawn grass, clover and occasional small flower sprites form low groups over irregular, softly faded ground-color patches. The islands follow the existing terrain and add no colliders. Warm golden lighting and the side-mounted kitchen garden are preserved; open lawn remains between groups.

All verge planting now excludes the central castle approach and a 1.1-metre corridor along the guard route, in addition to existing path, solid-object, interaction and vegetable-bed exclusions. Patch colors fade where those exclusions intersect an island. Runtime meshes/materials use the existing teardown ownership; no new textures or external assets were added.

Validation: six existing illustration, castle-palette, garden-access and actual castle-walk/reload checks passed in `Library/courtyard-meadow-islands-tests.xml` (47.8 seconds). After moving foreground islands farther into the normal gameplay view, three targeted capture/garden/walk checks passed in `Library/courtyard-meadow-final-tests.xml`. Both runs were silent. Review `Builds/courtyard-polish-gameplay-ida-workshop.png`, `Builds/courtyard-polish-gameplay-stable.png` and `Builds/garden-illustration-gameplay.png`; corresponding `-before-meadow-islands.png` files retain the earlier views. This is a bounded vegetation pass, not a completed whole-courtyard landscape or a frame-rate benchmark.

Suggested commit: `style(courtyard): group meadow planting around clear walking routes`. This pass remains local with the preceding light and garden changes. Current version stays `0.25.6`; the next prepared commit would use `0.25.7`.


### Western breach and near-field transition (local preview)

`CourtyardBreachStyle` runs after the shared castle facade palette. It replaces the old primitive rubble and foliage with uneven masonry courses, two small rubble piles and drawn low planting. Unsupported battlements above the opening are retired. Three soft earth patches lead to a clear viewing place; the low wall remains a visible collision boundary. Existing golden lighting is preserved.

The outside meadow previously began with an abrupt raised cutout edge, exposing sky beneath it in close views. A bounded western blend lowers the nearby landscape and gradually returns to the existing fields. `MeadowVegetation` applies the same height adjustment to courtyard wheat; nearby tree and flower anchors follow it. Terrain meshes are cloned at runtime and restored on teardown. Battle-scene vegetation and authored scene assets are unchanged by this adjustment.

Validation: the focused Play Mode test passed in `Library/courtyard-breach-landscape-tests.xml` (13.1 seconds, audio disabled). It checks the near-field mesh height, walks the actual player body into the viewing place, pushes against the low wall, and reloads the scene. The initial approach test found an obstructing rubble stone; the piles now sit closer to the wall. `Builds/west-breach-tower-approach.png` and `Builds/west-breach-gameplay.png` use the normal gameplay camera; `Builds/west-breach-detail.png` is a temporary inspection view. These captures were visually reviewed. No full-game regression or performance benchmark was run. The subsequent optional lookout and camera flight are documented below; landscape narration remains future work.

Restart StartScene in Play Mode to see the runtime dressing. Suggested commit: `style(courtyard): shape the western breach and meadow transition`. Changes remain local and uncommitted. Version remains `0.25.6`; the next proposed commit version is `0.25.7`.


### Optional western-field lookout (local playable preview)

Restart StartScene, walk to the low broken wall left of Elian's tower, and press E at "Über die Felder schauen". `CourtyardLookout` moves the camera toward the opening and then over the fields over ten seconds. The final panorama stays open until E, Escape or the on-screen return button is used. Returning follows the same route backwards (at most 3.5 seconds). The player stays in the courtyard; no quest progress is changed. The objective and nearby interaction prompts are hidden during the view. The small landscape caption is provisional descriptive text, not the planned outside narrator's lore.

`CourtyardCamera` owns an explicit scenic pose override, keeping Cinemachine and the normal follow camera from competing. `DemoOpening.IsPresenting` includes the lookout so movement and other interactions stay locked until the return finishes. Early cancellation, component disabling and scene unload restore the camera override and objective, remove the view UI and release that lock. Interaction discovery uses the existing range/collider rules; the lookout marker follows the raised courtyard ground.

`CourtyardLookoutLandscape` adds distant western mountains and gently drifting clouds using the existing tower-window paintings. Two dedicated resource materials reference those textures without duplicating or editing them. The scenery has no colliders; its owned quad mesh is disposed on unload. This is a bounded western view, not a fully landscaped exterior region. The subsequent western field-edge and tree-group pass is documented below; the wider world remains future visual work.

Validation: `CourtyardLookoutTests.Lookout_CancelsRepeatsAndRestoresCameraInputAndHud` passed in `Library/courtyard-lookout-landscape-tests.xml` (23.7 seconds execution, audio disabled). It exercises actual interaction selection and range, movement suppression, duplicate activation prevention, early return, the complete flight and UI-button callback, repetition, component disabling and scene reload. It also verifies scenery materials, absence of scenery colliders and cloud movement. Real captures reviewed: `Builds/lookout-interaction.png`, `lookout-masonry-frame.png`, `lookout-fields-flight.png`, `lookout-fields-panorama.png`. Physical keyboard input and an exported build were not separately tested; no full-suite or performance run was needed for this bounded change.

Suggested commit: `feat(courtyard): add a reversible western-field lookout` — optional scenic flight, shared control lock, return UI and painted horizon with lifecycle regression coverage. Changes are local, not committed or pushed. Version remains `0.25.6`; the next proposed commit version is `0.25.7`.


### Western field margins and groves (local visual polish)

`CourtyardFieldMargins` adds two irregular green seams between the western crops, a narrow winding earth path, and four groups of three illustrated trees at different distances. Small fern/mixed-leaf groups and occasional flowers follow the seams with intentional gaps. The existing tower-window mountains remain visible through the middle. Existing scattered tree/flower props are retired only within this western dressing area; their active state is restored on teardown. Existing sprites and materials are reused, with no new texture files.

`MeadowVegetation` samples the same curves in StartScene Play Mode: wheat gives way to short green grass near the margins, and vegetation is omitted from the narrow centre of the path. The effect fades at the western range limits. Ribbons, trees and plants follow the exterior surface height; they introduce no colliders and do not move the player, camera, courtyard buildings or garden. Runtime ribbons/materials are released on teardown. The mountain/cloud renderers now sort behind the nearer transparent trees; visual review caught and corrected a mountain layer drawing over tree crowns.

Validation: the focused lookout lifecycle test passed in `Library/courtyard-field-margins-final-tests.xml` (23.9 seconds execution, audio disabled). Added checks cover twelve trees outside the wall, upward-facing path geometry, absence of decorative colliders, correct horizon/tree order and a single landscape installation after reload. The existing full flight, cancellation and control-restoration checks still pass. Final captures reviewed: `Builds/lookout-fields-panorama.png`, `lookout-masonry-frame.png`, `lookout-interaction.png`; `-before-field-margins.png` captures preserve the previous view. This is a bounded scenery pass; the wider landscape and a performance/build assessment remain open. Restart StartScene in Play Mode to see it.

Suggested separate commit: `style(courtyard): shape western field margins and tree groves` — shared crop-edge curves, winding field path, grouped planting and corrected background sorting. This pass remains local alongside the earlier changes. Version remains `0.25.6`; the next proposed commit version is `0.25.7`.


### Eastern gate, drawbridge and stream detail pass (local preview)

`CourtyardGateStyle` dresses the existing crossing on the right-hand wall. A segmented curved limestone arch and fitted masonry replace the old box-shaped arch outline. Bridge decking uses the existing illustrated oak with lengthwise grain and a larger texture scale; straps gain small rivets, and two meshes of alternating oval links replace the disconnected chain rods. Gate piers gain carved feet and capitals. Existing gate interaction and arrival space remain unchanged; the Arkanmeisterin is not triggered by this work.

The stream remains wholly outside the wall, below the bridge. A gently varying water ribbon, shallow green banks and sparse drawn plants replace the flat surface/glint boxes and primitive bank foliage. `Resources/CourtyardStream.shader` provides restrained moving highlights and lighter shallows, respecting the existing light, shadow and distance fog. The authored deck, far-bank ground and gate solids are retained. Decorative geometry adds no colliders; owned meshes and materials are released on scene teardown. The existing stream audio is unchanged.

The initial physical test found that the box-shaped player caught the old five-millimetre meadow edge while returning from the bridge. `CourtyardBridgeThreshold` adds a shallow, visible bevel over that seam, with both ends below the neighboring surfaces and a matching Ground mesh collider. It avoids changing general movement or adding a teleport. This is a separate movement fix from the visual dressing.

Validation: `CourtyardCrossingStyleTests.Crossing_RemainsWalkableBothWays_WithExteriorWaterAndClearApproach` passed in `Library/courtyard-crossing-verified-tests.xml` (15.6 seconds execution, audio disabled). It walks the real player body across and back, checks the far bank, verifies decorative collision isolation, water bounds and visible arch-face winding, then reloads the scene. `Builds/east-crossing-gameplay.png` uses the normal gameplay camera; `Builds/east-crossing-detail.png` is a temporary oblique inspection view. Both views were reviewed during this pass. No full-suite or exported-build test was run.

The following eastern-field pass addresses the sky-colored gap and abruptly ending path identified in the oblique inspection view. The wider gate surroundings still need an overall visual acceptance pass. Restart StartScene in Play Mode to load these changes.

Suggested commits: `style(courtyard): refine the eastern gate bridge and stream` (gate/bridge dressing and water shader) and `fix(courtyard): bevel the return seam at the drawbridge` (physical threshold and traversal regression). Changes are local, not committed or pushed. Current version remains `0.25.6`; the next actual commit would propose `0.25.7`, with later version increments decided per commit.


### Eastern bank and field approach (local preview)

`CourtyardEastFieldStyle` lowers the near eastern meadow to meet the existing far bank, then blends back to the original rolling landscape beyond it. The existing exterior mesh owner in `CourtyardBreachStyle` applies this bounded profile together with the western profile, so there is still a single mesh clone/restore lifecycle. Trees and generated grass follow the same height adjustment. The western profile and battle landscapes are unchanged.

The rectangular exterior path is replaced with a narrowing, gently curving earth ribbon, a soft grass fringe and sparse illustrated plants. The shared path curve also clears crop blades from the trail. Transparent earth is explicitly drawn after its wider grass fringe. Old path renderers are restored and owned geometry/materials released when the dressing is destroyed. No new texture assets are needed.

This is scenery dressing, not a new playable region: the existing solid bank and camera limits are retained. The distant path beyond the authored bank is a visual continuation, not a completed exploration route. Restart StartScene in Play Mode to load the changes.

Suggested commit: `style(courtyard): connect the eastern bank to the golden fields` — shared terrain/vegetation height blending, winding exterior path, planted verges and extended crossing regression. Changes remain local and uncommitted; version stays `0.25.6`, with `0.25.7` proposed for the next actual commit.

Validation: the extended crossing test passed in `Library/courtyard-east-fields-final-tests.xml` (22.2 seconds execution, audio disabled), physically walking the player to x=70 and back through the gate, checking upward path faces/height continuity, bank support and installation after reload. Western breach and lookout tests passed in `Library/courtyard-east-fields-tests.xml` (14.0/22.5 seconds). The initial crossing run caught an incorrect path height on the authored bank; the correction uses that bank's actual flat surface. Visual review also caught the grass ribbon covering the earth ribbon and led to explicit draw order. Final gameplay and oblique captures (`Builds/east-crossing-gameplay.png`, `Builds/east-crossing-detail.png`) were reviewed. No full suite or exported build was run.


### Courtyard wall feet (local visual polish)

`CourtyardWallFootStyle` connects the north/east enclosing walls to the lawn with separate low fern, clover and mixed-leaf groups and soft earth patches. Ground samples follow the existing collision surface. The placement excludes interaction approaches, the gate corridor, solid furniture/buildings and the guard route. The decoration adds no colliders and reuses the existing illustrated sprites/materials.

The illustrated surface shader now supports optional per-renderer `_FootMoss` parameters: low, irregular green staining retains the original drawn masonry. Only the three selected enclosing walls opt in; the shared stone material and other buildings keep their existing appearance. Property blocks preserve texture scale and other renderer settings. Teardown restores the previous moss parameters and releases generated meshes/materials. `IntroChapter` installs the pass after the existing landscaping.

Validation: both targeted PlayMode tests passed with audio disabled in `Library/courtyard-wall-foot-tests.xml` (34.2 seconds total execution). Castle palette coverage checks local versus shared moss state, absence of decorative colliders, clear gate planting, preserved texture scale after teardown and single installation after reload. The crossing regression physically walks to the far bank and back. Gameplay and oblique captures were visually reviewed; `Builds/courtyard-wall-foot-detail.png` records this pass. No exported-build or full-suite test was run.

Restart StartScene in Play Mode to see the additions. This is a bounded wall-foot pass; climbing ivy, wider environmental composition and visual acceptance in motion remain open. Suggested commit: `style(courtyard): soften enclosing wall foundations` — opt-in moss shading, low planting, ground transitions and lifecycle checks. Nothing committed or pushed. Version remains `0.25.6`; next proposed commit version is `0.25.7`.


### Gentle wind in illustrated courtyard plants (local preview)

`CourtyardPlantWind` runs once after the StartScene dressing has been installed. It selects only the existing Grass, Clover, Mixed, Fern and Flowers resources. Characters, vegetables, trees, standalone pebbles and the existing field-wind system are excluded. Each selected source sprite gets one shared runtime copy with a 5-by-7 vertex grid and one shared material; imported assets are not modified. `OverrideGeometry` receives sprite-rectangle pixel coordinates, as specified in the [Unity API documentation](https://docs.unity.cn/6000.0/Documentation/ScriptReference/Sprite.OverrideGeometry.html).

`Resources/CourtyardPlantWind.shader` bends upper vertices with a height-squared weight, keeping the planted edge stationary. Gentle waves and slower gusts vary by world position, while maximum motion is limited by both plant height and type. Existing billboarding, sprite colors, transparency and horizontal flipping remain intact. Bounds expand to include the possible sideways movement. Wind runs on the GPU; no per-plant Update scripts or CPU mesh deformation are added. Disabling the scene-owned component restores original sprite/material bindings, re-enabling reapplies them, and teardown releases runtime resources. Draw-call/performance profiling in an exported build remains open.

Validation: `CourtyardPlantWindTests` passed with audio disabled in `Library/courtyard-plant-wind-geometry-tests.xml` (11.2 seconds execution). The test renders an isolated tinted/flipped fern, compares the zero-wind image with the original sprite shader, verifies actual image movement above stable roots, checks resource sharing and untouched source geometry, excludes player/vegetables, then tests disabling, enabling and scene reload. Earlier failed iterations exposed the wrong coordinate space for sprite geometry; the final test includes the correction. Sampled render frames and the gameplay capture were visually inspected. `Builds/courtyard-plant-wind-preview.gif` contains 24 actual test-render frames; `Builds/courtyard-plant-wind-gameplay.png` shows the normal camera. No full-suite or exported-build check was run.

Restart StartScene in Play Mode to see the movement. Suggested commit: `feat(courtyard): add rooted wind motion to illustrated plants` — shared subdivided sprites, restrained GPU wind, lifecycle restoration and render-based verification. Changes remain local; no commit or push. Version stays `0.25.6`; the next proposed commit version is `0.25.7`.


### Clear mountains and golden morning light (feedback correction)

The western mountain painting was being strongly blended towards the pale global fog color, hiding its blue shaded faces. `CourtyardPaintedHorizon.shader` now renders the existing mountain/cloud paintings with controlled tint/contrast and only a small height-dependent haze near the mountain base. It deliberately avoids the global distance fog: the painted image already includes atmospheric perspective. Source images, sizes, positions and layer sorting are unchanged.

StartScene now uses a dedicated morning sky and field-wind material. The sun is lower (22 degrees elevation), with its sky glow aligned to the directional light; warmer light, amber horizon and warm cloud highlights distinguish it from the previous midday appearance. The upper sky remains blue. Global fog is less pale; the later distance-fog refinement extends its linear range to 180–1600 metres. Existing ambient fill keeps the courtyard readable. Meadow and crop shaders have an opt-in scene-sun response, enabled only on courtyard field materials; other scenes retain their former shader defaults and shared sky/vegetation materials. Ground normals respond to the actual sun, and crop tips receive more warmth than their bases. No full-screen yellow filter was added.

`ConfigureIntroCourtyard.Horizon` retains the field material selection on regeneration, and the west architecture lighting setup uses the same morning sun/sky/fog values as the saved scene. These generators were updated and compiled, not executed to rebuild the user's layout.

Validation: the existing lookout lifecycle and castle palette tests passed silently in `Library/courtyard-morning-view-tests.xml` (2/2, 35.8 seconds). After the final sky color adjustment, the lookout test passed again in `Library/courtyard-morning-final-tests.xml` (24.0 seconds). The panorama, return view and central courtyard capture were visually reviewed. `Builds/lookout-golden-morning.png` records the final view; `Builds/lookout-before-morning-correction.png` preserves the earlier local panorama. No exported-build or full-suite verification.

Suggested commit: `fix(courtyard): restore mountain contrast and golden morning light` — painting-specific horizon shading, courtyard-only morning sky/field materials and aligned scene/generator lighting. Changes remain local, uncommitted and unpushed; version stays `0.25.6`, next proposed commit `0.25.7`.


### Western grove shadows (local visual polish)

`CourtyardTreeShadows` adds an alpha-cutout, shadow-only mesh for each of the 12 western field trees and a soft contact patch at its roots. The upright shadow cards face the fixed morning sun instead of following the viewing camera. Contact patches sample the actual meadow triangles so they follow the sloping terrain. The courtyard meadow and wind shaders receive main-light shadows when their existing `_SunResponse` setting is enabled. Other scenes retain their default appearance.

The component installs after the StartScene landscaping, adds no colliders, follows tree visibility and releases its generated materials/meshes on teardown. Geometry is prepared for the fixed scene lighting and tree positions; moving trees or a future day/night cycle would need an update path. Real shadows depend on the selected URP quality profile and shadow distance.

Validation: the focused lookout PlayMode test passed silently in `Library/courtyard-tree-shadow-render-final-tests.xml` (23.9 seconds). It checks terrain alignment, alpha cutout casters, hiding/restoring trees and scene reload. Two captures within the same frame verify that the casters actually darken more than 1,000 image pixels, without wind/cloud movement contaminating the comparison. `Builds/lookout-tree-shadows.png` is the reviewed panorama; `Builds/lookout-tree-shadows-disabled.png` records the comparison. No full-suite or exported-build check was run.

Suggested commit: `style(courtyard): ground western groves with morning shadows` — scene-owned tree casters, sloping root contacts, meadow shadow reception and render/lifecycle coverage. This pass covers the western groves only; the wider character/furniture/building shadow task remains open. Restart StartScene in Play Mode to see it. Nothing committed or pushed; version remains `0.25.6`, next proposed commit version `0.25.7`.


### Courtyard furniture contact and paper-shadow grounding

`CourtyardFurnitureShadows` adds soft ground contact beneath Ida's workbench, Marta's potting table and the two garden benches. Existing directional sun shadows remain in use. Footprints follow each furniture object's horizontal axes and sample walkable ground; they add no colliders. The four stationary patches share one material, follow source visibility, and release their meshes/material on scene teardown. Moving furniture would require rebuilding the footprints. Buildings and portrait/character colors are not changed by this pass.

`PaperGroundShadow` now starts its floor ray near the owner's feet instead of above its head, preventing overhead tables/roofs from becoming the shadow surface. Contacts align with gentle ground slopes and disappear when no supporting surface is found. Invisible paving is excluded. The existing movement-based sampling cache and shared rendering resources are retained; moving platforms are not covered by this change.

Validation: `PaperShadowTests` and `CourtyardFurnitureShadowTests` passed silently in `Library/courtyard-furniture-shadow-final-tests.xml` (2/2, 7.6 seconds execution). Coverage includes an overhead obstacle, sloped/missing floor, sprite visibility/opacity, four furniture contacts, disable/enable, scene reload and a same-frame rendered on/off comparison. The close bench view and normal gameplay capture were visually reviewed (`Builds/courtyard-furniture-contacts.png`, `Builds/courtyard-contacts-gameplay.png`). No full-suite or Windows-build check was run.

Suggested small commits: `fix(rendering): keep paper shadows on supporting ground` (PaperGroundShadow and its regression test); `style(courtyard): add soft contact beneath work furniture` (new furniture component, IntroChapter hook and focused scene test), with relevant documentation hunks in each. Nothing committed or pushed. Version remains `0.25.6`; the next actual commit would propose `0.25.7`.


### Local morning highlights at courtyard landmarks

`CourtyardMorningAccents` emphasizes existing sunlight near the tower entrance, western wall breach and eastern gate. The illustrated surface shader adds a smoothly bounded, opt-in direct-sun contribution using the same sun color, normals and shadow attenuation as its ordinary lighting. Occluded surfaces receive no added highlight. This is local art direction, not extra light sources, volumetric beams or a global exposure change. Character sprites and the central castle are not opted in.

The scene component writes only `_MorningAccent` and `_MorningAccentStrength` to selected renderer property blocks. Shared materials remain untouched; texture scales, moss and other properties are preserved when enabling, disabling or destroying the component. Installation runs after the existing StartScene dressing. It creates no colliders, meshes, extra lights or material copies, and performs no per-frame work. The three fixed zones are tailored to the current layout; moving the authored architecture requires adjusting/rebuilding the zones.

Validation: `CourtyardMorningAccentTests` passed silently in `Library/courtyard-morning-accent-final-tests.xml` (8.7 seconds). Each landmark has an independent same-frame on/off image comparison, disabling only its own zone. Coverage also checks shared materials, the unaffected central facade, property restoration and a single installation after scene reload. The three rendered previews were visually reviewed: `Builds/courtyard-morning-accent-tower.png`, `Builds/courtyard-morning-accent-breach.png`, `Builds/courtyard-morning-accent-gate.png`. No full-suite or exported-build test was run.

Suggested commit: `style(courtyard): emphasize morning light at key landmarks` — local surface-shader highlights, reversible scene setup and isolated render/lifecycle checks. Restart StartScene in Play Mode to see the change. Nothing committed or pushed; version remains `0.25.6`, next proposed actual commit `0.25.7`.


### Garden morning-light accent

The existing `CourtyardMorningAccents` setup now includes a restrained fourth zone centered on Marta's potting table (8 m radius, strength 0.30). It selects the potting table, kitchen-garden wooden frames/trellises, benches and approach flagstones. Crop sprites retain their existing colors; soil and earth clods are explicitly excluded, including if their material gains accent support later. No additional shader, light, geometry or collider was required.

Validation: the extended landmark/garden rendering test and the watering completion/cancellation/reload test passed silently in `Library/courtyard-garden-morning-tests.xml` (2/2, 24.8 seconds execution). The isolated garden on/off comparison confirms visible changes, and assertions preserve wet soil and crop colors when toggling accents. The normal gameplay, closer garden and wet-bed captures were visually reviewed (`Builds/courtyard-garden-morning-gameplay.png`, `Builds/courtyard-morning-accent-garden.png`, `Builds/courtyard-garden-morning-wet.png`). Full-suite/build verification and joint visual acceptance remain open. The review also exposed a pre-existing sightline issue where the garden sign covered Elian; the subsequent low-marker correction below addresses it.

Suggested commit: `style(courtyard): warm Marta's garden work area` — extend the local sunlight profile while preserving watering feedback, with render and gameplay validation. Changes remain local; nothing committed or pushed. Version stays `0.25.6`; next proposed actual commit `0.25.7`.


### Low garden wayfinding marker

The garden sign is now placed beside the first bed using that bed's bounds, with a 0.75 m post instead of 1.5 m. Its board dimensions and text size are retained. `CourtyardLife.Sign` accepts an optional post height; the other three wayfinding signs keep their previous heights. No extra colliders, interactions or update scripts were added.

The existing garden illustration/approach/reload test passed silently in `Library/courtyard-garden-sign-tests.xml` (11.8 seconds). Captures at the formerly obscured approach and beside/behind the marker were visually checked: Elian's head stays clear, while ordinary lower-body overlap can still occur when standing directly behind scenery. The garden and cottage approaches and interaction targets remain covered by the existing test. Preview: `Builds/garden-sign-approach.png`, `Builds/garden-sign-side.png`, `Builds/garden-sign-behind.png`. No full-suite or exported-build check was run.

Suggested commit: `fix(courtyard): lower garden sign to keep Elian visible` — anchor the low marker beside the first bed and record three gameplay-camera views in the existing garden check. Changes remain local; no commit or push. Version remains `0.25.6`; next proposed actual commit `0.25.7`.


### Aurenfeld tree varieties

`CourtyardTreeStyle` replaces the active courtyard/meadow tree illustrations in StartScene with four newly generated, hand-drawn silhouettes: spreading oak and slender birch along the fields, a small apple tree beside the garden cottage, and a willow on the far stream bank north of the drawbridge. Western grove spacing increases from 3.2 m to 6.2 m. Existing field tree heights are retained; all variants use uniform scaling, not stretched crowns. Original demo/battle assets remain untouched.

Standalone transparent PNGs live in `Assets/Resources/CourtyardStyle/Trees`. `CourtyardTreeImporter` imports them as single sprites with a low root pivot, uncompressed color, bilinear filtering and coverage-preserving mipmaps. The built-in image generator and exact prompts are recorded in `Generation.json` beside the assets. The runtime replacement runs before western shadow construction, so the new crowns also supply their shadow silhouettes. Added trees are scenery, without new gameplay collisions or destructibility. Removing the style restores original renderers and materials; scene reload builds it once.

Suggested commit: `feat(courtyard): introduce illustrated Aurenfeld tree varieties` — four species, habitat placement, matching western shadows and targeted rendering/reload checks. No commit or push; project version remains `0.25.6`, next proposed actual commit `0.25.7`.

Validation: the tree resource/placement/reload check and existing western lookout/shadow rendering check passed silently (2/2, 31.4 seconds execution; `Library/courtyard-trees-tests.xml`). After moving the apple tree from behind the castle facade to the visible cottage side, the affected tree check passed again (1/1, 7.3 seconds; `Library/courtyard-tree-placement-tests.xml`). Reviewed in-game captures: `Builds/aurenfeld-trees-garden.png`, `Builds/aurenfeld-trees-stream.png`, and `Builds/lookout-fields-panorama.png`. No full-suite run or exported player build was made for this art change.


### Painted farm and road to the horizon

`CourtyardDistantFarm` now presents a fixed, upright scenic illustration instead of the initial block-built farmhouse/barn. The new silhouette includes a steep tiled farmhouse roof, a large barn with braced doors and hayloft, hay wagon, haystacks, climbing greenery and a fenced yard. It sits on the sampled meadow at (-144, 0, 14), with a 20 m illustrated width. This is explicitly distant scenery, not a walkable new location. The artwork is `Assets/Resources/CourtyardStyle/Farm/AurenfeldFarm.png`; `Generation.json` records the built-in image generator prompt and normalized chimney-mouth anchor. `CourtyardTreeImporter` also configures this standalone sprite, preserving transparency, mipmaps and a ground-level pivot.

The existing chimney smoke remains a separate world-space particle system: 1.8 puffs/second, 8-11 second lifetime, maximum 32 particles, gentle drift and fading. Its emitter is anchored to the illustrated chimney mouth instead of the former 3D chimney. The card does not rotate with the camera during the lookout flight. Owned materials are released on unload.

`CourtyardFieldMargins` extends the winding road from x=-48 to x=-372, narrowing its half-width from 1.3 m to 0.85 m and fading out beyond the final rise. Its distant bends lead to the right of the farm so the barn does not hide the continuation. Path and vegetation-clearance masks share the same centerline; the earth ribbon draws explicitly after its grassy verge. `CourtyardDistantFields` adds one lightweight, non-colliding rolling-ground mesh behind the original meadow (22,713 vertices after the parcel refinement, no extra grass simulation), and both road and hills sample the same surface function. The mountain/cloud cards move farther back with proportional scaling about the final lookout camera, preserving their apparent composition while leaving room for the distant ground. Roads, grassy margins and distant fields now share the same restrained global fog response; StartScene uses a gradual 180-1600 m linear fade to retain warm greens/golds and a readable distant road.

Validation: the expanded farm/road PlayMode check passed silently (1/1, 9.2 seconds; `Library/courtyard-farm-horizon-tests.xml`). It checks the illustration, chimney smoke, supported materials, upward-facing ground/path geometry, road extent, visible road differences in the horizon band, no added colliders and a single farm after reload. The existing lookout return/input/shadow check also passed during this change before the final local color/road-route adjustments. Reviewed final actual-camera views: `Builds/aurenfeld-farm-panorama.png`, `Builds/aurenfeld-farm-detail.png`, and the diagnostic `Builds/aurenfeld-far-road-detail.png`. No standalone build or full suite was run.

Suggested coherent commits: `style(courtyard): replace distant farm with illustrated homestead` (art, import, scenic rendering and chimney anchor), then `feat(courtyard): extend field road to the horizon` (distant ground, road continuation, local haze and rendering checks). Nothing committed or pushed. Version stays `0.25.6`; next proposed actual commit `0.25.7`.


### Aurenfeld meadow clusters, wind and foreground shadows

The StartScene meadow now uses darker green/brown roots, a mid-stem color transition and warmer yellow-green/gold tips. Two deterministic world-space noise scales vary density, height and tint, producing irregular grass islands and small sparse areas rather than uniform scatter. Near tufts contain 3-5 blades; distant tufts retain three. Existing courtyard exclusions and western/eastern bare paths are preserved. Other scenes retain their previous palette and distribution.

`MeadowWind.shader` adds opt-in broad travelling wind waves with small tuft-level variation. Roots remain fixed; both forward rendering and the new shadow-caster pass call the same deformation function. Only `Courtyard morning wind.mat` enables the new field profile. Expanded mesh bounds account for the stronger bending.

`MeadowVegetation.NearShadowsEnabled` can disable the new shadows for comparison. At most four nearby LOD0 chunks can cast; LOD1 never casts. Individual tufts progressively stop casting between 26 and 34 metres from the viewing camera. The explicit camera position is passed per renderer, including during shadow rendering. Selection is updated after camera movement or a toggle, not repeatedly for a stationary view. Far vegetation keeps its inexpensive vertex-color shading. No additional lights, colliders or scene-wide shadow-resolution changes are introduced.

Validation: the targeted silent PlayMode check passed (1/1, 9.7 seconds; `Library/courtyard-meadow-tests.xml`). It checks supported shaders, the near-only four-chunk budget, disabling shadows, visible rendered shadow differences, retirement after moving the camera to the east, and clean scene reload. Same-frame 1080p captures showed 186,419 darker pixels with grass shadows. Reviewed `Builds/aurenfeld-meadow-panorama.png`, `Builds/aurenfeld-meadow-shadows.png`, and `Builds/aurenfeld-meadow-no-shadows.png`.

An uncapped Editor A/B/B/A comparison used a 1920x1080 camera target, 20 warmup frames per block and 160 samples per state on an NVIDIA GeForce RTX 3070 Ti. Shadows ON: median 3.82 ms, p95 8.16 ms; OFF: median 3.76 ms, p95 4.08 ms. These are editor frame intervals including CPU/GPU/test overhead, not isolated GPU costs or a standalone-build FPS guarantee. Typical times were close, but the enabled sample had larger spikes. Recheck representative gameplay in a player build before selecting final quality defaults. Raw report: `Builds/aurenfeld-meadow-timing.txt`.

Suggested coherent commits: `style(meadow): add warm clustered Aurenfeld vegetation` (distribution, heights and palette); then `feat(meadow): add wind-matched foreground shadows` (shared wind deformation, bounded shadow selection and comparison test). Changes remain uncommitted and unpushed. Version stays `0.25.6`; the next actual commit would propose `0.25.7`, incremented only when committing.


### Grass root contact shading

`MeadowRootContacts` adds a small, soft multiplicative contact patch per near grass/getreide tuft, batched into one mesh per occupied LOD0 chunk. Its five sampled vertices follow the actual rendered meadow triangles. The same ground sampler seats each blade root slightly into that surface rather than trusting only the continuous terrain height formula. Sampling occurs during mesh construction; no per-blade GameObjects, colliders, raycasts or frame updates are added. Sampling caches are cleared after construction, and owned meshes/materials are released during rebuild/unload.

`MeadowRootContact.shader` darkens the existing ground color with a soft radial falloff. Contacts remain fixed under the roots, independently of wind and directional-light shadow bias. They fade between 32 and 48 m and share the grass's near LOD, so there are no distant orphan contact patches. This is a stylized local occlusion approximation, not screen-space ambient occlusion. Other scenes keep their existing setup.

Validation: both targeted meadow tests passed silently (2/2, 21.3 seconds; `Library/courtyard-root-contact-tests.xml`). The new check disables long sun shadows and verifies a visible on/off contact difference, supported materials, shared LOD membership, no colliders and consistent recreation after scene reload. The existing sun-shadow budget/render test also passed. Reviewed `Builds/aurenfeld-grass-contact-on.png`, `Builds/aurenfeld-grass-contact-off.png`, `Builds/aurenfeld-grass-contact-combined.png`; panorama is `Builds/aurenfeld-grass-contact-panorama.png`. No standalone build was run; the old shadow timing comparison does not isolate the added contact cost.

Suggested commit: `fix(meadow): ground grass tufts with soft contact shadows` — sample rendered terrain for roots and add batched near-LOD contact shading with an independent render comparison. Uncommitted and unpushed. Version remains `0.25.6`; next proposed actual commit `0.25.7`.


### Restrained Aurenfeld distance fog

StartScene linear fog now starts at 180 m and reaches full coverage at 1600 m, replacing the much stronger 65-340 m range. The west architecture authoring setup uses the same values so regeneration preserves the correction. Distant-field and road/verge material fog strengths are unified at 1, avoiding mismatched haze between adjacent surfaces. The mountain/cloud paintings retain their existing artistic atmospheric perspective. Lighting and the warm sky palette are unchanged by this correction.

Validation: the focused silent PlayMode farm/landscape check passed (1/1, 10.7 seconds; `Library/courtyard-distance-fog-tests.xml`), including supported materials, distant road visibility and scene reload. Reviewed `Builds/aurenfeld-clear-distance.png` and a diagnostic capture with the old global fog range, `Builds/aurenfeld-old-distance-fog.png`. The diagnostic retains the new unified material strengths and is not an exact reproduction of the entire previous setup. No full suite or standalone build was run for this adjustment.

Suggested commit: `fix(courtyard): soften and unify distance fog` — preserve landscape colors with a gradual distant fade and consistent ground/path fog response. Changes remain uncommitted; version stays `0.25.6`, next proposed actual commit `0.25.7`.


### Golden western fields

The Aurenfeld western panorama now uses ripe grain throughout instead of alternating green grass and wheat. `CourtyardFieldMargins.AdjustGold` smoothly transitions from the original vegetation at x=-46 to fully ripe grain at x=-54, leaving the courtyard/eastern landscape unchanged. Existing density islands, wind, root contacts, shadow limits and bare-road exclusions remain in use. The original ground is recolored on its existing runtime mesh copy; `GoldenGround` supplies the same varied ochre/straw palette for the distant hills. Road verges now resemble golden stubble. Trees and small broadleaf plants retain their colors. Original shared ground meshes are restored on teardown.

Validation: focused silent PlayMode landscape check passed (1/1, 11.2 seconds; `Library/courtyard-golden-fields-tests.xml`), including distant road visibility, supported materials and scene reload. Visually reviewed the actual Unity capture `Builds/aurenfeld-golden-fields.png`. Grain heads add geometry where short grass previously grew; density and the near-shadow budget were not increased, but this run is not a performance benchmark.

Suggested commit: `style(courtyard): turn western meadows into golden grain fields` — unify vegetation, ground and distant hills around a ripe golden harvest palette. No commit or push; version remains `0.25.6`, next proposed actual commit `0.25.7`.


### Cultivated distant landscape

`CourtyardDistantFields` now divides the remote hills into broad, gently bent crop parcels with alternating straw/ochre shades and narrow green meadow boundaries. The pattern fades in behind the near grain fields to avoid a hard seam. Ground resolution is 113 by 201 vertices (22,713 total), sufficient to resolve the soft boundaries without per-field objects or distant grass simulation. Three small clusters reuse the illustrated oak/birch and undergrowth sprites; their bases follow the terrain, a clearance excludes the winding road, and all decoration remains collider-free. Owned meshes and materials are released on teardown.

Validation: the focused silent landscape PlayMode check passed (1/1, 11.7 seconds; `Library/courtyard-field-parcels-tests.xml`), covering distant road visibility and scene reload. Actual Unity views with identical camera settings: `Builds/aurenfeld-parcels-before.png` and `Builds/aurenfeld-parcels-after.png`. The field-edge mask was refined after inspecting the first render. These captures validate the final outlook; the subsequent farm-access and camera-flight pass is documented below.

Suggested commit: `style(courtyard): structure distant hills into cultivated fields` — add varied golden parcels, green boundaries and sparse illustrated tree/hedge groups. Uncommitted and unpushed; version remains `0.25.6`, next proposed actual commit `0.25.7`.


### Farm access and lookout flight polish

`CourtyardFarmApproach` adds a curved, ground-following lane from the western main path to the illustrated farm gate, a soft-edged trampled forecourt, an apple tree beside the farmhouse and six low fence plants. The same sampled centreline and forecourt ellipse exclude grain during `MeadowVegetation` mesh generation, keeping the access clear. The fixed farm illustration and chimney anchor are preserved. New meshes/materials are owned by the approach and released on unload; decoration adds no colliders or gameplay interactions.

StartScene meadow LOD groups now use distance-based dither crossfading (22% of the near LOD range, 18% of the far range). The wind shader's forward/shadow passes and root-contact shader consume the same LOD fade, avoiding abrupt chunk/detail switches and lingering contacts. Other scenes retain their previous LOD mode. Geometry counts and the four-chunk sun-shadow budget remain unchanged by this refinement; the brief overlap renders both LODs, so standalone-build performance still needs measurement.

Validation: four targeted PlayMode tests passed silently in 59.1 seconds (`Library/courtyard-farm-flight-tests.xml`): farm/road/reload, lookout cancellation/repeat/return/input restoration, sun-shadow budget/render comparison, and root contacts. The earlier intermediate capture attempt used an unsupported batchmode end-of-frame yield; the capture loop now advances with normal frame yields and the final run passes. Reviewed all ten actual Unity frames `Builds/lookout-flight-01.png` through `lookout-flight-10.png`, plus `Builds/aurenfeld-farm-detail.png`. No open terrain edge was visible in these sampled frames. This is a sampled visual review plus automated traversal, not a continuous-video or standalone-build benchmark. No new build was produced.

Suggested coherent commits (still uncommitted/unpushed):
- `style(courtyard): connect the distant farm to the field road` — approach mesh, matching grain exclusion, forecourt, orchard tree, fence plants and focused validation.
- `fix(meadow): crossfade vegetation during the lookout flight` — LOD group settings, wind/contact shader fades and ten-stage camera review.

Version remains `0.25.6`; next proposed actual commit is `0.25.7`, incremented only when committing.


Pre-push validation: 12 additional targeted silent PlayMode checks passed in 149.8 seconds (`Library/courtyard-prepush-tests.xml`), covering garden access/watering, crossing, castle palette, plant wind, furniture contacts, local morning accents and figure shadows. Together with the four farm/flight/meadow checks, 16 targeted tests passed for this state. No full-suite or standalone-build claim is implied.


Release reconciliation: the eight courtyard commits above include all previously local work through `0.26.4`. Historical uncommitted/proposed-version statements in the development notes are superseded by this release list. Targeted pre-push checks are recorded below.

## Scenic courtyard releases

- **0.26.5** — `style(landscape): refine golden fields and distant hills`. Group ripe grain with varied heights and short stubble. Replace green parcel separators with golden tones, soften distant hill shapes and blend a dense short grain transition into a filtered painted surface. Keep road and scenery on the same sampled terrain. Validation: targeted landscape captures and silent PlayMode scenery/reload checks passed.
- **0.26.6** — `style(landscape): refine field margins and grove placement`. Add opt-in cart ruts and varied road width. Reduce scattered bushes to low roadside herbs, group the existing trees and apply a scoped olive/gold palette. Fade the secondary margin before the distant hills so it no longer reads as a second road. Validation: final panorama and existing tree-shadow/lookout checks passed.
