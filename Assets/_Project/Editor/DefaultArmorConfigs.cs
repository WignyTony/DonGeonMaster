using UnityEngine;
using UnityEditor;
using DonGeonMaster.Equipment;
using DonGeonMaster.Inventory;

public static class DefaultArmorConfigs
{
    private const string ArmorPath = "Assets/_Project/Configs/Armor";

    struct ArmorDef
    {
        public string categoryPrefix; // "Head Armor", "Chest Armor", etc.
        public CharacterStandards.EquipmentSlot slot;
        public string slotLabel; // French display name
        public int baseArmor;   // Base armor for Type 1
    }

    private static readonly ArmorDef[] ArmorCategories = new[]
    {
        new ArmorDef { categoryPrefix = "Head Armor",  slot = CharacterStandards.EquipmentSlot.Head,  slotLabel = "Casque",    baseArmor = 5 },
        new ArmorDef { categoryPrefix = "Chest Armor", slot = CharacterStandards.EquipmentSlot.Chest, slotLabel = "Plastron",  baseArmor = 10 },
        new ArmorDef { categoryPrefix = "Legs Armor",  slot = CharacterStandards.EquipmentSlot.Legs,  slotLabel = "Jambieres", baseArmor = 7 },
        new ArmorDef { categoryPrefix = "Feet Armor",  slot = CharacterStandards.EquipmentSlot.Feet,  slotLabel = "Bottes",    baseArmor = 4 },
        new ArmorDef { categoryPrefix = "Belt Armor",  slot = CharacterStandards.EquipmentSlot.Belt,  slotLabel = "Ceinture",  baseArmor = 3 },
        new ArmorDef { categoryPrefix = "Arm Armor",   slot = CharacterStandards.EquipmentSlot.Arms,  slotLabel = "Brassards", baseArmor = 6 },
    };

    // Color names for display
    private static readonly string[] ColorNames = { "Sombre", "Royal", "Ancien" };

    // Rarity per color variant
    private static readonly ItemRarity[] ColorRarities =
    {
        ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic
    };

    public static void CreateAll()
    {
        System.IO.Directory.CreateDirectory(ArmorPath);
        int created = 0;

        foreach (var cat in ArmorCategories)
        {
            for (int type = 1; type <= 6; type++)
            {
                for (int color = 1; color <= 3; color++)
                {
                    string partName = $"{cat.categoryPrefix} Type {type} Color {color}";
                    string assetName = partName.Replace(" ", "_");
                    string path = $"{ArmorPath}/{assetName}.asset";

                    if (AssetDatabase.LoadAssetAtPath<EquipmentData>(path) != null) continue;

                    var eq = ScriptableObject.CreateInstance<EquipmentData>();
                    eq.itemId = $"armor_{assetName.ToLower()}";
                    eq.itemName = $"{cat.slotLabel} T{type} {ColorNames[color - 1]}";
                    eq.description = $"{cat.slotLabel} de type {type}, variante {ColorNames[color - 1]}.\nArmure solide forgee dans les profondeurs.";
                    eq.category = ItemCategory.Equipment;
                    eq.rarity = ColorRarities[color - 1];
                    eq.stackable = false;
                    eq.maxStack = 1;
                    eq.slot = cat.slot;
                    eq.armorPartName = partName;
                    // Weight/Material based on COLOR: Sombre=Léger(Cuir), Royal=Moyen(Os), Ancien=TrèsLourd(Plaques)
                    eq.weight = color == 1 ? CharacterStandards.EquipmentWeight.Leger
                              : color == 2 ? CharacterStandards.EquipmentWeight.Moyen
                              : CharacterStandards.EquipmentWeight.TresLourd;
                    float weightFactor = ((int)eq.weight + 1) / 5f;
                    eq.armor = Mathf.Max(1, Mathf.RoundToInt(cat.baseArmor * weightFactor));
                    eq.damage = 0;
                    eq.attackSpeed = eq.weight == CharacterStandards.EquipmentWeight.Leger ? 1.0f
                                   : eq.weight == CharacterStandards.EquipmentWeight.Moyen ? 0.95f
                                   : 0.85f;
                    eq.moveSpeedModifier = 1f;
                    eq.sellValue = 0;
                    eq.armorMaterial = color == 1 ? CharacterStandards.ArmorMaterial.Cuir
                              : color == 2 ? CharacterStandards.ArmorMaterial.Os
                              : CharacterStandards.ArmorMaterial.Plaques;
                    eq.handling = color == 1 ? CharacterStandards.Handling.Rapide
                              : color == 2 ? CharacterStandards.Handling.Normal
                              : CharacterStandards.Handling.TresLent;

                    // Use existing thumbnail screenshot if available, otherwise procedural icon
                    eq.icon = LoadThumbnailSprite(assetName) ?? ProceduralTextures.GenerateSlotIcon(SlotIconName(cat.slot));

                    AssetDatabase.CreateAsset(eq, path);
                    created++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DonGeonMaster] Created {created} armor assets in {ArmorPath}");
    }

    private static string SlotIconName(CharacterStandards.EquipmentSlot slot) => slot switch
    {
        CharacterStandards.EquipmentSlot.Head => "Head",
        CharacterStandards.EquipmentSlot.Chest => "Chest",
        CharacterStandards.EquipmentSlot.Legs => "Legs",
        CharacterStandards.EquipmentSlot.Feet => "Feet",
        CharacterStandards.EquipmentSlot.Belt => "Amulet",
        CharacterStandards.EquipmentSlot.Arms => "Shield",
        _ => "Chest"
    };

    [MenuItem("DonGeonMaster/Create Default Armor")]
    public static void EnsureArmorExists()
    {
        // Delete existing armor assets to force recreation with new color-based weights
        if (AssetDatabase.IsValidFolder(ArmorPath))
        {
            var oldGuids = AssetDatabase.FindAssets("t:EquipmentData", new[] { ArmorPath });
            foreach (var guid in oldGuids)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }
        CreateAll();
        EnsureWeaponsExist();
        RefreshAllSceneReferences();
    }

    /// <summary>
    /// After regeneration, refresh all DebugArmorLoader and ItemEditorController
    /// references in open scenes so they point to the new assets.
    /// </summary>
    private static void RefreshAllSceneReferences()
    {
        // Refresh DebugArmorLoaders
        var loaders = Object.FindObjectsByType<DonGeonMaster.Debugging.DebugArmorLoader>(FindObjectsSortMode.None);
        foreach (var loader in loaders)
        {
            loader.RefreshReferences();
            EditorUtility.SetDirty(loader);
        }

        // Refresh ItemEditorControllers
        var editors = Object.FindObjectsByType<DonGeonMaster.UI.ItemEditorController>(FindObjectsSortMode.None);
        foreach (var editor in editors)
        {
            editor.RefreshReferences();
            EditorUtility.SetDirty(editor);
        }

        // Save scene changes
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[DonGeonMaster] Refreshed all scene references to equipment assets.");
    }

    private const string WeaponPath = "Assets/_Project/Configs/Weapons";

    private static readonly string[] WeaponFolders = {
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/GREAT SWORDS",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/ONE-HANDED SWORDS",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/SHIELDS",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/HAMMERS",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/WEAPONS UPDATE 1.1"
    };

    private static void ForceDefaults()
    {
        // Rarity and element are now intentional per-item values — no longer force-reset
    }

    public static void EnsureWeaponsExist()
    {
        // Delete existing weapon assets to force recreation with new stats
        if (AssetDatabase.IsValidFolder(WeaponPath))
        {
            var oldGuids = AssetDatabase.FindAssets("t:EquipmentData", new[] { WeaponPath });
            foreach (var guid in oldGuids)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }
        System.IO.Directory.CreateDirectory(WeaponPath);
        int created = 0;

        foreach (var folder in WeaponFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.name.Contains("Arrow") || prefab.name.Contains("Bow")) continue;

                string cleanName = prefab.name.Replace("FREE ", "").Replace("COLOR ", "C");
                string assetName = cleanName.Replace(" ", "_");
                string assetPath = $"{WeaponPath}/{assetName}.asset";

                if (AssetDatabase.LoadAssetAtPath<EquipmentData>(assetPath) != null) continue;

                string upper = prefab.name.ToUpper();
                bool isShield = upper.Contains("SHIELD");
                var slot = isShield ? CharacterStandards.EquipmentSlot.Shield : CharacterStandards.EquipmentSlot.Weapon;

                // Determine stats from weapon type
                int damage = 5;
                float atkSpeed = 1f;
                string label;
                CharacterStandards.WeaponType wType;
                CharacterStandards.EquipmentWeight wWeight;
                CharacterStandards.Handling wHandling;
                ItemRarity wRarity;
                if (upper.Contains("GREAT SWORD")) { damage = 3; atkSpeed = 0.8f; label = "Grande Épée"; wType = CharacterStandards.WeaponType.GrandeEpee; wWeight = CharacterStandards.EquipmentWeight.Moyen; wHandling = CharacterStandards.Handling.Normal; wRarity = ItemRarity.Uncommon; }
                else if (upper.Contains("HAMMER")) { damage = 4; atkSpeed = 0.7f; label = "Masse"; wType = CharacterStandards.WeaponType.Masse; wWeight = CharacterStandards.EquipmentWeight.Lourd; wHandling = CharacterStandards.Handling.Lent; wRarity = ItemRarity.Rare; }
                else if (isShield) { damage = 0; atkSpeed = 1.0f; label = "Bouclier"; wType = CharacterStandards.WeaponType.Bouclier; wWeight = CharacterStandards.EquipmentWeight.Lourd; wHandling = CharacterStandards.Handling.Lent; wRarity = ItemRarity.Uncommon; }
                else { damage = 2; atkSpeed = 1.2f; label = "Épée"; wType = CharacterStandards.WeaponType.Epee; wWeight = CharacterStandards.EquipmentWeight.Leger; wHandling = CharacterStandards.Handling.Rapide; wRarity = ItemRarity.Common; }

                var eq = ScriptableObject.CreateInstance<EquipmentData>();
                eq.itemId = $"weapon_{assetName.ToLower()}";
                eq.itemName = cleanName;
                eq.description = $"{label} forgé(e) dans les profondeurs.";
                eq.category = ItemCategory.Equipment;
                eq.rarity = wRarity;
                eq.stackable = false;
                eq.maxStack = 1;
                eq.slot = slot;
                eq.armorPartName = ""; // Weapons are prefabs, not modular parts
                eq.meshPrefab = prefab;
                eq.armor = isShield ? 3 : 0;
                eq.damage = damage;
                eq.attackSpeed = atkSpeed;
                eq.moveSpeedModifier = 1f;
                eq.sellValue = 0;
                eq.weaponType = wType;
                eq.weight = wWeight;
                eq.handling = wHandling;
                eq.icon = LoadThumbnailSprite(assetName) ?? ProceduralTextures.GenerateSlotIcon(isShield ? "Shield" : "Weapon");

                AssetDatabase.CreateAsset(eq, assetPath);
                created++;
            }
        }

        if (created > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DonGeonMaster] Created {created} weapon assets in {WeaponPath}");
        }
    }

    private const string ThumbnailPath = "Assets/_Project/Art/Textures/Thumbnails";

    /// <summary>
    /// Loads an existing thumbnail screenshot as a Sprite.
    /// Matches by asset name: "Chest_Armor_Type_1_Color_1" → "Thumb_Chest_Armor_Type_1_Color_1.png"
    /// </summary>
    private static Sprite LoadThumbnailSprite(string assetName)
    {
        string pngPath = $"{ThumbnailPath}/Thumb_{assetName}.png";

        // Ensure the texture is imported as Sprite
        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer == null) return null; // PNG does not exist

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }
}
