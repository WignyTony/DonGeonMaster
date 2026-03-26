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

## Editor Tools (Assets/_Project/Editor/)

- **`DonGeonMaster > Create Default Armor`** — Deletes and regenerates all armor (108) + weapon assets. Auto-loads thumbnail PNGs from `Thumbnails/Thumb_{assetName}.png` as icons. Also refreshes serialized references in open scenes (DebugArmorLoader, ItemEditorController).
- **`DonGeonMaster > Setup Project`** (ProjectSetup.cs) — Full scene generation (medieval environment, UI, lighting)
- **ProceduralTextures.cs** — Generates stone/floor/wood textures programmatically

## Item Icon Pipeline

1. ScreenManager scene → position item with camera sliders → "Take Screenshot"
2. Saves `Thumb_{itemName}.png` (128x128) to `Assets/_Project/Art/Textures/Thumbnails/`
3. `DefaultArmorConfigs.LoadThumbnailSprite()` loads these PNGs when regenerating equipment assets
4. Falls back to `ProceduralTextures.GenerateSlotIcon()` if no thumbnail exists

## Important Conventions

- **Serialized references break on regeneration:** `DebugArmorLoader.armorsToLoad[]` and `ItemEditorController.items[]` hold direct ScriptableObject refs. Both have `RefreshReferences()` context menu methods. `DefaultArmorConfigs` calls these automatically after regeneration.
- **PlayerPrefs persistence:** Face customization, weapon positions, camera settings, key bindings all use PlayerPrefs (not saved to files).
- **Stats formula:** `ATK = 10 + sum(damage * rarityMul)`, `DEF = 5 + sum(armor * rarityMul)`, `SPD = 20 + sum((attackSpeed-1)*10 + (moveSpeedMod-1)*10)`
- **Combat formula:** `rawDmg = ATK * max(0.5, attackSpeed) * elementMultiplier`, `finalDmg = max(1, rawDmg - DEF*0.5)`, crit = 1.5x
- **Element wheel:** Feu > Glace > Foudre > Poison > Feu, Sacre > Tenebres > Arcane > Sacre (1.5x advantage, 0.75x disadvantage)

## Key File Paths

```
Scripts:        Assets/_Project/Scripts/{namespace}/
Armor assets:   Assets/_Project/Configs/Armor/{Slot}_Armor_Type_{1-6}_Color_{1-3}.asset
Weapon assets:  Assets/_Project/Configs/Weapons/{WEAPON_NAME}_{N}_C{1-3}.asset
Thumbnails:     Assets/_Project/Art/Textures/Thumbnails/Thumb_{assetName}.png
Prefabs:        Assets/_Project/Prefabs/
Input config:   Assets/_Project/InputSystem_Actions.inputactions
URP settings:   Assets/Settings/PC_RPAsset.asset
```
