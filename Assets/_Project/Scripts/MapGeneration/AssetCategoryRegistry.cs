using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    [CreateAssetMenu(fileName = "AssetCategoryRegistry", menuName = "DonGeonMaster/Map Generation/Category Registry")]
    public class AssetCategoryRegistry : ScriptableObject
    {
        public List<AssetCategory> categories = new();

        public AssetCategory GetCategory(string categoryId)
        {
            return categories.FirstOrDefault(c => c != null && c.categoryId == categoryId);
        }

        public List<AssetCategory> GetEnabledCategories(List<string> enabledIds)
        {
            if (enabledIds == null || enabledIds.Count == 0)
                return new List<AssetCategory>(categories.Where(c => c != null));
            return categories.Where(c => c != null && enabledIds.Contains(c.categoryId)).ToList();
        }

        public List<AssetCategory> GetCategoriesForCell(CellType cellType, BiomeType biome, List<string> enabledIds)
        {
            return GetEnabledCategories(enabledIds)
                .Where(c => c.IsAllowedOnCell(cellType, biome))
                .ToList();
        }

        public List<string> AllCategoryIds => categories
            .Where(c => c != null)
            .Select(c => c.categoryId)
            .ToList();

        public List<string> AllDisplayNames => categories
            .Where(c => c != null)
            .Select(c => c.displayName)
            .ToList();
    }
}
