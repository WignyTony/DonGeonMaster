using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using DonGeonMaster.MapGeneration;

public class DefaultAssetCategorySetup
{
    static readonly string PandazolePath =
        "Assets/Pandazole_Ultimate_Pack/Pandazole Nature Environment Pack/Prefabs";
    static readonly string OutputPath = "Assets/_Project/Configs/MapGeneration";

    [MenuItem("DonGeonMaster/Créer Catégories d'Assets MapGen", false, 201)]
    public static void CreateDefaultCategories()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath,
            "_Project/Configs/MapGeneration"));
        AssetDatabase.Refresh();

        var categories = new List<AssetCategory>();

        // === SOLS ===
        categories.Add(CreateCategory("Sols", "Sols", "Tiles de sol (TileGround)",
            new Color(0.5f, 0.4f, 0.3f), "TileGround",
            new List<CellType> { CellType.Sol, CellType.Couloir, CellType.Mur },
            new List<BiomeType>(),
            placementChance: 1f, maxPerCell: 1, isStructural: true, isDecoration: false,
            allowRotation: false, minScale: 1f, maxScale: 1f, minSpacing: 0,
            densityType: DensityType.Structural, scaleMultiplier: 1f));

        // === EAU ===
        categories.Add(CreateCategory("Eau", "Eau", "Tiles d'eau (TileWater)",
            new Color(0.2f, 0.5f, 0.9f), "TileWater",
            new List<CellType> { CellType.Eau },
            new List<BiomeType>(),
            placementChance: 1f, maxPerCell: 1, isStructural: true, isDecoration: false,
            allowRotation: false, minScale: 1f, maxScale: 1f, minSpacing: 0,
            densityType: DensityType.Structural, scaleMultiplier: 1f));

        // === ARBRES ===
        categories.Add(CreateCategory("Arbres", "Arbres", "Arbres (Spring/Fall/Winter)",
            new Color(0.2f, 0.6f, 0.2f), "Tree",
            new List<CellType> { CellType.Mur },
            new List<BiomeType> { BiomeType.Foret, BiomeType.ForetAutomne, BiomeType.ForetHiver, BiomeType.Prairie },
            placementChance: 0.35f, maxPerCell: 1, minSpacing: 4f,
            densityType: DensityType.Vegetation, scaleMultiplier: 0.4f));

        // === BUISSONS ===
        categories.Add(CreateCategory("Buissons", "Buissons", "Buissons bas",
            new Color(0.3f, 0.5f, 0.2f), "Bush",
            new List<CellType> { CellType.Mur, CellType.Sol },
            new List<BiomeType> { BiomeType.Foret, BiomeType.Prairie, BiomeType.ForetAutomne },
            placementChance: 0.20f, maxPerCell: 1, minSpacing: 2f,
            densityType: DensityType.Vegetation, scaleMultiplier: 0.3f));

        // === HERBE ===
        categories.Add(CreateCategory("Herbe", "Herbe", "Touffes d'herbe",
            new Color(0.4f, 0.7f, 0.3f), "Grass",
            new List<CellType> { CellType.Sol, CellType.Couloir },
            new List<BiomeType> { BiomeType.Foret, BiomeType.Prairie, BiomeType.ForetAutomne },
            placementChance: 0.15f, maxPerCell: 1, minSpacing: 1f,
            densityType: DensityType.Vegetation, scaleMultiplier: 0.2f));

        // === FLEURS ===
        categories.Add(CreateCategory("Fleurs", "Fleurs", "Fleurs décoratives",
            new Color(0.9f, 0.4f, 0.6f), "Flower",
            new List<CellType> { CellType.Sol },
            new List<BiomeType> { BiomeType.Prairie, BiomeType.Foret, BiomeType.Fantaisie },
            placementChance: 0.12f, maxPerCell: 1, minSpacing: 1f,
            densityType: DensityType.Vegetation, scaleMultiplier: 0.2f));

        // === ROCHES DURES ===
        categories.Add(CreateCategory("RochesDures", "Roches dures", "Gros blocs rocheux",
            new Color(0.5f, 0.5f, 0.5f), "HardRock",
            new List<CellType> { CellType.Mur, CellType.Sol },
            new List<BiomeType> { BiomeType.Rocailleux, BiomeType.Desert, BiomeType.ForetHiver },
            placementChance: 0.20f, maxPerCell: 1, minSpacing: 3f,
            densityType: DensityType.Rock, scaleMultiplier: 0.35f));

        // === ROCHES TENDRES ===
        categories.Add(CreateCategory("RochesTendres", "Roches tendres", "Petites pierres et galets",
            new Color(0.6f, 0.55f, 0.5f), "SoftRock",
            new List<CellType> { CellType.Sol, CellType.Couloir, CellType.Mur },
            new List<BiomeType>(),
            placementChance: 0.12f, maxPerCell: 1, minSpacing: 1.5f,
            densityType: DensityType.Rock, scaleMultiplier: 0.25f));

        // === CACTUS ===
        categories.Add(CreateCategory("Cactus", "Cactus", "Végétation désertique",
            new Color(0.4f, 0.6f, 0.2f), "Cactus",
            new List<CellType> { CellType.Mur, CellType.Sol },
            new List<BiomeType> { BiomeType.Desert },
            placementChance: 0.25f, maxPerCell: 1, minSpacing: 3f,
            densityType: DensityType.Vegetation, scaleMultiplier: 0.35f));

        // === CHAMPIGNONS ===
        categories.Add(CreateCategory("Champignons", "Champignons", "Champignons variés",
            new Color(0.7f, 0.3f, 0.3f), "Mashroom",
            new List<CellType> { CellType.Sol, CellType.Mur },
            new List<BiomeType> { BiomeType.Foret, BiomeType.Marecage, BiomeType.Fantaisie },
            placementChance: 0.08f, maxPerCell: 1, minSpacing: 2f,
            densityType: DensityType.Decor, scaleMultiplier: 0.2f));

        // === MINERAIS ===
        categories.Add(CreateCategory("Minerais", "Minerais", "Nodes minéraux et gemmes",
            new Color(0.3f, 0.7f, 0.9f), "MineralNode",
            new List<CellType> { CellType.Sol, CellType.Mur },
            new List<BiomeType> { BiomeType.Rocailleux, BiomeType.Fantaisie },
            placementChance: 0.08f, maxPerCell: 1, minSpacing: 5f,
            isGameplay: true, densityType: DensityType.Rock, scaleMultiplier: 0.3f));

        // === GEMMES ===
        categories.Add(CreateCategory("Gemmes", "Gemmes", "Cristaux et pierres précieuses",
            new Color(0.8f, 0.3f, 0.9f), "Jem",
            new List<CellType> { CellType.Sol },
            new List<BiomeType> { BiomeType.Fantaisie, BiomeType.Rocailleux },
            placementChance: 0.04f, maxPerCell: 1, minSpacing: 7f,
            isGameplay: true, densityType: DensityType.Rock, scaleMultiplier: 0.2f));

        // === TRONCS ===
        categories.Add(CreateCategory("Troncs", "Troncs d'arbres", "Troncs coupés et souches",
            new Color(0.5f, 0.35f, 0.2f), "TreeTrunk",
            new List<CellType> { CellType.Sol, CellType.Mur },
            new List<BiomeType> { BiomeType.Foret, BiomeType.ForetAutomne, BiomeType.ForetHiver },
            placementChance: 0.10f, maxPerCell: 1, minSpacing: 3f,
            densityType: DensityType.Decor, scaleMultiplier: 0.3f));

        // === ÉPINES ===
        categories.Add(CreateCategory("Epines", "Épines", "Buissons épineux",
            new Color(0.4f, 0.3f, 0.2f), "Thorns",
            new List<CellType> { CellType.Mur },
            new List<BiomeType> { BiomeType.Desert, BiomeType.Marecage },
            placementChance: 0.15f, maxPerCell: 1, minSpacing: 2f,
            densityType: DensityType.Vegetation, scaleMultiplier: 0.25f));

        // === FEUILLAGE ===
        categories.Add(CreateCategory("Feuillage", "Feuillage", "Masses de feuilles",
            new Color(0.3f, 0.55f, 0.2f), "Foliage",
            new List<CellType> { CellType.Mur, CellType.Sol },
            new List<BiomeType> { BiomeType.Foret, BiomeType.Marecage },
            placementChance: 0.12f, maxPerCell: 1, minSpacing: 2f,
            densityType: DensityType.Vegetation, scaleMultiplier: 0.25f));

        // === DÉBRIS ===
        var debrisPrefabs = new List<string> { "Bones", "Skull", "Log", "Stick", "Leave" };
        categories.Add(CreateCategoryMultiPrefix("Debris", "Débris", "Os, crânes, bûches, bâtons",
            new Color(0.6f, 0.5f, 0.4f), debrisPrefabs,
            new List<CellType> { CellType.Sol, CellType.Couloir },
            new List<BiomeType>(),
            placementChance: 0.05f, maxPerCell: 1, minSpacing: 4f,
            densityType: DensityType.Decor, scaleMultiplier: 0.2f));

        // === CORAUX ===
        categories.Add(CreateCategory("Coraux", "Coraux", "Formations coralliennes",
            new Color(0.9f, 0.4f, 0.5f), "Coral",
            new List<CellType> { CellType.Eau },
            new List<BiomeType> { BiomeType.Marecage },
            placementChance: 0.15f, maxPerCell: 1, minSpacing: 2f,
            densityType: DensityType.Decor, scaleMultiplier: 0.3f));

        // === Créer le Registry ===
        var registry = ScriptableObject.CreateInstance<AssetCategoryRegistry>();
        registry.categories = categories;
        AssetDatabase.CreateAsset(registry, $"{OutputPath}/AssetCategoryRegistry.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DefaultAssetCategorySetup] {categories.Count} catégories créées dans {OutputPath}/");
        Debug.Log("[DefaultAssetCategorySetup] Registry: AssetCategoryRegistry.asset");

        // Vérifier le nombre de prefabs chargés
        int totalPrefabs = 0;
        foreach (var cat in categories)
            totalPrefabs += cat.prefabs.Count;
        Debug.Log($"[DefaultAssetCategorySetup] Total prefabs référencés: {totalPrefabs}");
    }

    static AssetCategory CreateCategory(string id, string displayName, string description,
        Color debugColor, string prefabPrefix,
        List<CellType> allowedCells, List<BiomeType> allowedBiomes,
        float placementChance = 0.5f, int maxPerCell = 1, float minSpacing = 1f,
        bool isStructural = false, bool isDecoration = true, bool isGameplay = false,
        bool allowRotation = true, float minScale = 0.9f, float maxScale = 1.1f,
        DensityType densityType = DensityType.Decor, float scaleMultiplier = 1f)
    {
        var cat = ScriptableObject.CreateInstance<AssetCategory>();
        cat.categoryId = id;
        cat.displayName = displayName;
        cat.description = description;
        cat.debugColor = debugColor;
        cat.allowedCellTypes = allowedCells;
        cat.allowedBiomes = allowedBiomes;
        cat.placementChance = placementChance;
        cat.maxPerCell = maxPerCell;
        cat.minSpacing = minSpacing;
        cat.isStructural = isStructural;
        cat.isDecoration = isDecoration;
        cat.isGameplay = isGameplay;
        cat.allowRotationVariation = allowRotation;
        cat.minScaleVariation = minScale;
        cat.maxScaleVariation = maxScale;
        cat.densityType = densityType;
        cat.scaleMultiplier = scaleMultiplier;

        // Charger les prefabs
        cat.prefabs = LoadPrefabsByPrefix(prefabPrefix);

        string assetPath = $"{OutputPath}/{id}.asset";
        AssetDatabase.CreateAsset(cat, assetPath);
        return cat;
    }

    static AssetCategory CreateCategoryMultiPrefix(string id, string displayName, string description,
        Color debugColor, List<string> prefabPrefixes,
        List<CellType> allowedCells, List<BiomeType> allowedBiomes,
        float placementChance = 0.5f, int maxPerCell = 1, float minSpacing = 1f,
        bool isStructural = false, bool isDecoration = true, bool isGameplay = false,
        DensityType densityType = DensityType.Decor, float scaleMultiplier = 1f)
    {
        var cat = ScriptableObject.CreateInstance<AssetCategory>();
        cat.categoryId = id;
        cat.displayName = displayName;
        cat.description = description;
        cat.debugColor = debugColor;
        cat.allowedCellTypes = allowedCells;
        cat.allowedBiomes = allowedBiomes;
        cat.placementChance = placementChance;
        cat.maxPerCell = maxPerCell;
        cat.minSpacing = minSpacing;
        cat.isStructural = isStructural;
        cat.isDecoration = isDecoration;
        cat.isGameplay = isGameplay;
        cat.densityType = densityType;
        cat.scaleMultiplier = scaleMultiplier;

        cat.prefabs = new List<GameObject>();
        foreach (var prefix in prefabPrefixes)
        {
            cat.prefabs.AddRange(LoadPrefabsByPrefix(prefix));
        }

        string assetPath = $"{OutputPath}/{id}.asset";
        AssetDatabase.CreateAsset(cat, assetPath);
        return cat;
    }

    static List<GameObject> LoadPrefabsByPrefix(string prefix)
    {
        var prefabs = new List<GameObject>();

        // Chercher dans le dossier Pandazole
        var guids = AssetDatabase.FindAssets($"t:Prefab {prefix}", new[] { PandazolePath });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            // Vérifier que le nom commence bien par le préfixe (éviter les faux positifs)
            if (!fileName.StartsWith(prefix + "_") && fileName != prefix) continue;
            // Exclure le Demo prefab
            if (fileName == "Demo") continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        if (prefabs.Count == 0)
        {
            Debug.LogWarning($"[DefaultAssetCategorySetup] Aucun prefab trouvé pour le préfixe '{prefix}' " +
                $"dans {PandazolePath}");
        }
        else
        {
            Debug.Log($"[DefaultAssetCategorySetup] {prefix}: {prefabs.Count} prefabs chargés");
        }

        return prefabs;
    }
}
