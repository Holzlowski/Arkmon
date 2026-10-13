# Arkmon Demo 0.11.0

This demo is a fixed milestone before the next design iteration. Git tag:
`demo-v0.11.0`. It begins in the tower bedroom, continues through collecting the
mantle and lamp in either order, and leads to John's trial in the courtyard.
After winning, return to John to finish the demo; practice battles remain available.

## Windows playtest

Extract the complete ZIP and start `Arkmon.exe`. Keep `Arkmon_Data`,
`MonoBleedingEdge`, `UnityPlayer.dll` and the other bundled runtime files together.
Controls: WASD to move, Space to jump, E to interact, mouse to select battle
commands/targets, Escape to cancel targeting or return from a command menu.
Use Alt+F4 to exit.

Feedback should include the scene, steps to reproduce, a screenshot when helpful,
and the log from `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Arkmon/Player.log`.
Try both equipment orders, a first victory and a repeated battle. Check movement,
attack previews, cover, burning, knockdown/standing up and action completion.

## Rebuild

Use Unity 6000.3.26f1 with Windows Build Support. Build Settings include
TowerRoom, StartScene and BattleScene, in that order. For a batch build:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Unity.exe' `
  -batchmode -quit -projectPath '<project-folder>' `
  -executeMethod CreateDemoBuild.Run `
  -arkmonBuildPath '<output-folder>/Arkmon.exe' -logFile '<build-log>'
```

The build helper defaults to `Builds/Arkmon-Demo-<version>/Arkmon.exe`.
Builds are local artifacts excluded from Git. The Git tag preserves source/assets;
the separate ZIP preserves the playable Windows demo. Runtime build metadata
records the exact source commit and version.

## Prototype limits

This is a short single-player prototype with one trial opponent and repeatable
practice, not a full adventure. Progress is session-based. Furniture and terrain
use painted approximations; source image resolution limits close-up sharpness.
Lighting/shadows depend on the graphics quality preset. Music and visual polish
remain open to revision in the next version.
