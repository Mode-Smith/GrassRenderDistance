# Grass Render Distance

A small BepInEx mod for Valheim (1.0) that lets you set how far away grass and other ground clutter is drawn. Vanilla draws grass out to 40 m. Small plants and saplings in the grass are extended by the same amount, so nothing pops in closer than the grass.

## Installation

**Download:** [Thunderstore](https://thunderstore.io/c/valheim/p/Mode_Smith/GrassRenderDistance/) · [Nexus Mods](https://www.nexusmods.com/valheim/mods/4125)

**Mod manager (r2modman / Thunderstore Mod Manager):** install from Thunderstore; BepInExPack is installed automatically.

**Manual:**

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Download the mod from Nexus Mods and extract it into your Valheim folder, so the DLL ends up at
   `Valheim\BepInEx\plugins\GrassRenderDistance\GrassRenderDistance.dll`.
3. Start the game once to generate the config file.

This is a client-side mod. It doesn't need to be installed on servers or by other players.

## Configuration

Edit `Valheim\BepInEx\config\mode-smith.valheim.grassrenderdistance.cfg`:

| Setting | Default | Range | Description |
|---|---|---|---|
| `GrassDistance` | 80 | 20–300 | Grass draw distance in meters |
| `ScaleAllClutter` | true | true/false | Also extend the fade-out distance of small plants, saplings and flowers by the same factor, so they don't pop in closer than the grass. Turn off to only change grass. |

Changes made in the file take effect the next time you load a world. With [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) installed, you can change the value in-game (F1) and it applies immediately.

Higher distances cost more performance: the amount of grass grows with the square of the distance, so doubling it means roughly 4× as much grass. 80–120 m is a good starting point. If performance is tight, turning off `ScaleAllClutter` also helps.

## How it works

A Harmony postfix on `ClutterSystem.Awake` sets `ClutterSystem.m_distance` to the configured value and calls `ClutterSystem.ClearAll()`, which discards existing grass patches so they regenerate at the new distance.

Each clutter type also has its own fade distance, which the game only ever lowers to match `m_distance`, never raises. With `ScaleAllClutter` on, the mod scales those per-type distances by `GrassDistance / 40` on the clutter prefabs: `InstanceRenderer.m_lodMinDistance`/`m_lodMaxDistance` for instanced clutter, and `LODGroup` transition heights for clutter spawned as objects. Vanilla values are recorded first, so changing the setting repeatedly always scales from vanilla.

## Building

Requires the .NET SDK and a Valheim install with BepInEx.

```
dotnet build -c Release
```

The build copies the DLL into `BepInEx\plugins\GrassRenderDistance\`. If Valheim isn't in the default Steam location, pass your path:

```
dotnet build -c Release -p:ValheimDir="D:\SteamLibrary\steamapps\common\Valheim"
```

## License

[MIT](LICENSE)
