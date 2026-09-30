# Grass Render Distance

Tired of grass popping in just a few meters in front of you? This mod lets you choose how far away grass and ground clutter is drawn. Vanilla Valheim stops at 40 m. Small plants and saplings in the grass are extended by the same amount, so nothing pops in closer than the grass.

## Features

- Set the grass draw distance anywhere from 20 to 300 meters (default 80)
- Small plants, saplings and flowers are extended too, so nothing pops in closer than the grass
- Change it in-game with ConfigurationManager (F1); grass regenerates immediately
- Client-side only: no need to install on servers or for other players
- Built for Valheim 1.0

## Installation

**Mod manager (recommended):** install with r2modman or Thunderstore Mod Manager. BepInExPack is installed automatically.

**Manual:** install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/), then put `GrassRenderDistance.dll` in `Valheim\BepInEx\plugins\GrassRenderDistance\`.

## Configuration

Run the game once to generate the config. In r2modman, open **Config editor** and select `mode-smith.valheim.grassrenderdistance.cfg`. For manual installs, the file is in `Valheim\BepInEx\config\`.

| Setting | Default | Description |
|---|---|---|
| `GrassDistance` | 80 | Draw distance in meters (20–300) |
| `ScaleAllClutter` | true | Also extend small plants and saplings by the same amount. Turn off to only change grass |

Edits to the file apply the next time you load a world. With [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) installed, you can change them live in the F1 menu.

**Performance:** the amount of grass grows with the square of the distance, so doubling it means about 4× as much grass. 80–120 m is a good starting point. If performance is tight, turning off `ScaleAllClutter` also helps.

## Recommended

I highly recommend using this alongside **Valheim Performance Optimizations (VPO)**. It eliminated the stutter I was getting and pairs well with a longer grass draw distance. It's optional; this mod works fine without it.

## Compatibility

Tested alongside ValheimPlus and Valheim Performance Optimizations. Doesn't change any game data or saves, so it's safe to add or remove at any time.

## Links

- Source code (MIT): https://github.com/Mode-Smith/GrassRenderDistance
- Nexus Mods: https://www.nexusmods.com/valheim/mods/4125
