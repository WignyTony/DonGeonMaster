using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    /// <summary>
    /// Determine quel slider de densite controle cette categorie.
    /// </summary>
    public enum DensityType
    {
        Vegetation,
        Rock,
        Decor,
        Structural
    }

    [CreateAssetMenu(fileName = "NewAssetCategory", menuName = "DonGeonMaster/Map Generation/Asset Category")]
    public class AssetCategory : ScriptableObject
    {
        [Header("Identité")]
        public string categoryId;
        public string displayName;
        [TextArea(1, 3)]
        public string description;
        public Color debugColor = Color.white;

        [Header("Prefabs")]
        public List<GameObject> prefabs = new();

        [Header("Règles de placement")]
        public List<CellType> allowedCellTypes = new() { CellType.Sol };
        public List<BiomeType> allowedBiomes = new();
        [Range(0f, 1f)] public float placementChance = 0.5f;
        public int maxPerCell = 1;
        public float minSpacing = 1f;
        public bool allowRotationVariation = true;
        [Range(0.8f, 1.2f)] public float minScaleVariation = 0.9f;
        [Range(0.8f, 1.2f)] public float maxScaleVariation = 1.1f;
        public float yOffset;

        [Header("Catégorisation")]
        public bool isStructural;
        public bool isDecoration = true;
        public bool isGameplay;
        public bool isRequired;

        [Header("Densité")]
        [Tooltip("Determine quel slider de densite (vegetation/rock/decor) controle cette categorie")]
        public DensityType densityType = DensityType.Decor;

        public bool IsAllowedOnCell(CellType cellType, BiomeType biome)
        {
            if (!allowedCellTypes.Contains(cellType)) return false;
            if (allowedBiomes.Count > 0 && !allowedBiomes.Contains(biome)) return false;
            return true;
        }

        public GameObject GetRandomPrefab(System.Random rng)
        {
            if (prefabs == null || prefabs.Count == 0) return null;
            int cleanCount = 0;
            foreach (var p in prefabs)
                if (p != null) cleanCount++;
            if (cleanCount == 0) return null;

            GameObject result = null;
            while (result == null)
            {
                result = prefabs[rng.Next(prefabs.Count)];
            }
            return result;
        }
    }
}
