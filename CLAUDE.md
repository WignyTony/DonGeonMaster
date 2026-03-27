# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Location

The Unity project lives at `C:\TW\jeu\My project\` (Unity Hub name: "My project"). This repo (`C:\TW\jeu\DonGeonMaster\`) contains memory/config; all code is under `My project`.

## Git Repository

- **GitHub:** https://github.com/WignyTony/DonGeonMaster (private)
- **Git root:** `C:\TW\jeu\My project\` (the Unity project folder)
- **Git LFS:** enabled for binary assets (.fbx, .png, .wav, .mp3, .ogg, .ttf, .blend, .jpg, .tga, .psd)
- **CLAUDE.md** is also versioned inside the Unity project repo

## Engine & Stack

- **Unity 6** (6000.4.0f1), URP 17.4.0, New Input System 1.19.0
- **Language:** C# — enums, UI labels, and item names are in **French**
- **Platform:** Windows Standalone (1024x768)

## Architecture

### Namespaces

All code under `DonGeonMaster.*`:
- `Core` — GameManager singleton
- `Equipment` — EquipmentData, CharacterStandards enums, ModularEquipmentManager
- `Inventory` — ItemData hierarchy, PlayerInventory singleton
- `Player` — PlayerController (CharacterController-based), camera, animation bridge
- `Combat` — CombatCalculator (static damage formulas)
- `UI` — InventoryUI, StatsPanel, menus, ScreenManagerController, ItemEditorController
- `Character` — GanzSe modular character utilities
- `Debugging` — DebugArmorLoader
- `Effects` — TorchFlicker, visual effects

### Item System (ScriptableObject-based)

```
ItemData (abstract) → EquipmentData | ConsumableData | MaterialData
```

- Items are `.asset` files in `Assets/_Project/Configs/Armor/` and `Configs/Weapons/`
- Rarity: Common(x1.0) → Uncommon(x1.15) → Rare(x1.3) → Epic(x1.5) → Legendary(x1.75) → Mythic(x2.0)
- `ItemData.RarityMultiplier()` applies rarity scaling to stats at runtime

### Equipment Flow

```
EquipmentData .asset
  → DebugArmorLoader loads into PlayerInventory at Start
  → InventoryUI displays (icon, rarity border color)
  → Player equips → ModularEquipmentManager activates GanzSe child GameObjects (armor) or instantiates meshPrefab (weapons)
  → StatsPanel.RefreshStats() recalculates ATK/DEF/SPD from all equipped items
```

### GanzSe Modular Character

Armor = activating/deactivating child GameObjects under category containers (HEADS, CHEST ARMOR, LEG ARMOR, etc.). Weapons = instantiated prefabs on `hand_r`/`hand_l` bones. Face parts (eyes, hair, beard, etc.) toggled separately, hidden when helmet equipped.

### Key Singletons

- `GameManager` (DontDestroyOnLoad)
- `PlayerInventory` (200 max slots, fires `OnInventoryChanged` event)
- `KeyBindingManager` (rebindable input)

## Scenes

| Scene | Purpose |
|-------|---------|
| MainMenu | Character showcase, face customizer, settings |
| Hub | Gameplay area with player, inventory, equipment |
| ItemEditor | Edit equipment stats via UI (editor-only tool) |
| ScreenManager | Capture 128x128 thumbnail PNGs for item icons |
| AnimationPreview | Test animations with equipment |
| MapGenDebug | Procedural map generation debug/test workshop |

## Editor Tools (Assets/_Project/Editor/)

- **`DonGeonMaster > Create Default Armor`** — Deletes and regenerates all armor (108) + weapon assets. Auto-loads thumbnail PNGs from `Thumbnails/Thumb_{assetName}.png` as icons. Also refreshes serialized references in open scenes (DebugArmorLoader, ItemEditorController).
- **`DonGeonMaster > Setup Project`** (ProjectSetup.cs) — Full scene generation (medieval environment, UI, lighting)
- **`DonGeonMaster > Bake Weapon Positions`** — Transfers weapon offsets from Editor PlayerPrefs into `.asset` ScriptableObjects. **Must run before building** if positions were adjusted in AnimationPreview.
- **`DonGeonMaster > Create Default Armor`** preserves baked weapon offsets across regeneration (savedOffsets dictionary).
- **ProceduralTextures.cs** — Generates stone/floor/wood textures + UI textures (SlotFrame, RarityBorder, QuantityBadge)
- **`DonGeonMaster > Créer Catégories d'Assets MapGen`** — Creates AssetCategory ScriptableObjects from Pandazole pack prefabs + AssetCategoryRegistry
- **`DonGeonMaster > Créer Scène MapGenDebug`** — Creates the MapGenDebug scene with camera, lighting, controller, and all components

## Item Icon Pipeline

1. ScreenManager scene → position item with camera sliders → "Take Screenshot"
2. Saves `Thumb_{itemName}.png` (128x128) to `Assets/_Project/Art/Textures/Thumbnails/`
3. `DefaultArmorConfigs.LoadThumbnailSprite()` loads these PNGs when regenerating equipment assets
4. Falls back to `ProceduralTextures.GenerateSlotIcon()` if no thumbnail exists

## Important Conventions

- **Serialized references break on regeneration:** `DebugArmorLoader.armorsToLoad[]` and `ItemEditorController.items[]` hold direct ScriptableObject refs. Both have `RefreshReferences()` context menu methods. `DefaultArmorConfigs` calls these automatically after regeneration.
- **PlayerPrefs persistence:** Face customization, camera settings, key bindings, equipped items (`Equipped_{Slot}`) use PlayerPrefs.
- **Weapon positions are in ScriptableObjects**, NOT PlayerPrefs. `EquipmentData.weaponPosOffset/weaponRotOffset/weaponScaleOverride` are the source of truth (works in builds). PlayerPrefs are only used for live Editor tweaking in AnimationPreview. `AnimationPreviewController.ValidateWeaponPosition()` bakes to both.
- **AnimatorControllers use serialized fields**, NOT `Resources.Load`. Each script (CharacterShowcase, CharacterCustomizer, AnimationPreviewController) has an `animController` field assigned by ProjectSetup.
- **AutoSetup version** (AutoSetup.cs `SetupVersion`): increment ONLY when scene regeneration is truly needed. Regeneration recreates ALL scenes and re-runs `EnsureArmorExists` which deletes/recreates assets.
- **Stats formula:** `ATK = 10 + sum(damage * rarityMul)`, `DEF = 5 + sum(armor * rarityMul)`, `SPD = 20 + sum((attackSpeed-1)*10 + (moveSpeedMod-1)*10)`
- **Combat formula:** `rawDmg = ATK * max(0.5, attackSpeed) * elementMultiplier`, `finalDmg = max(1, rawDmg - DEF*0.5)`, crit = 1.5x
- **Element wheel:** Feu > Glace > Foudre > Poison > Feu, Sacre > Tenebres > Arcane > Sacre (1.5x advantage, 0.75x disadvantage)

## Git Workflow

- **Push command:** `git -C "C:/TW/jeu/My project" push origin master`
- Si le push échoue avec "Repository not found", c'est un problème d'auth — l'utilisateur doit lancer `! git -C "C:/TW/jeu/My project" push origin master` lui-même pour que le credential manager s'ouvre
- Ne pas commit les dossiers `Build/`, `Library/`, `Temp/`, `Logs/` (déjà dans .gitignore)

## Build Checklist

1. Run `DonGeonMaster > Bake Weapon Positions` if weapon offsets were adjusted
2. Save all scenes (`Ctrl+Shift+S`)
3. Enable **Development Build** in Build Settings for debugging
4. Build And Run

## Equipment Persistence

- Equipped items saved via PlayerPrefs (`Equipped_Head`, `Equipped_Weapon`, etc.) using `itemId`
- `ModularEquipmentManager.SaveEquipment()` auto-saves on every equip/unequip
- `LoadSavedEquipmentDelayed()` restores equipment at frame 2 (after DebugArmorLoader populates inventory)

## Map Generation Debug System (namespace: DonGeonMaster.MapGeneration)

BSP-based procedural outdoor map generator with full debug tooling.

### Setup (2 steps in Unity Editor)
1. `DonGeonMaster > Créer Catégories d'Assets MapGen` — creates 17 AssetCategory SOs from Pandazole pack
2. `DonGeonMaster > Créer Scène MapGenDebug` — creates the debug scene

### Architecture (21 scripts, ~4300 lines)
- **MapGenerator** — BSP room placement + L-shaped corridors + Perlin biome assignment
- **MapGenDebugController** — orchestrates generation, validation, spawning, logging
- **MapGenDebugUI** — runtime-built left panel with collapsible sections (F5-F12 shortcuts)
- **AssetCategory** (ScriptableObject) — flexible category with placement rules (cell types, biomes, density)
- **AssetCategoryRegistry** (ScriptableObject) — holds all categories
- **AssetPlacer** — instantiates prefabs per cell based on category rules
- **GenerationValidator** — 10 post-gen checks (connectivity, bounds, overlaps, path spawn→exit)
- **GenerationLogger** — dual output: human-readable .txt + machine-readable .json in `MapGenLogs/`
- **BatchTestRunner** — stress test X iterations, export report with failed seeds
- **PresetManager** — save/load config presets as JSON in `MapGenPresets/`
- **DebugVisualization** — gizmos for grid, rooms, corridors, spawn/exit, biomes, validation errors

### Pandazole asset transform
All Pandazole assets: scale `(100,100,100)`, rotation X `-90°` (Blender Z-up → Unity Y-up).
TileGround grid: **6-unit spacing**. Configured in `MapGenConfig.assetScale/assetRotation`.

### Log format
`MapGenLogs/GenerationLog_YYYY-MM-DD_HH-mm-ss_seedXXXX.txt` + `.json`

### Keyboard shortcuts (in MapGenDebug scene)
F5=Generate, F6=Regenerate, F7=Clear, F8=Spawn, F9=Screenshot, F10=Camera, F12=ExportLog, Tab=ToggleUI

## Key File Paths

```
Scripts:        Assets/_Project/Scripts/{namespace}/
MapGen scripts: Assets/_Project/Scripts/MapGeneration/
MapGen configs: Assets/_Project/Configs/MapGeneration/
Armor assets:   Assets/_Project/Configs/Armor/{Slot}_Armor_Type_{1-6}_Color_{1-3}.asset
Weapon assets:  Assets/_Project/Configs/Weapons/{WEAPON_NAME}_{N}_C{1-3}.asset
Thumbnails:     Assets/_Project/Art/Textures/Thumbnails/Thumb_{assetName}.png
Prefabs:        Assets/_Project/Prefabs/
Input config:   Assets/_Project/InputSystem_Actions.inputactions
URP settings:   Assets/Settings/PC_RPAsset.asset
```
