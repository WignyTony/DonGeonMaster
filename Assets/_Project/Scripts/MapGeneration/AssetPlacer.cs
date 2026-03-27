using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    public class AssetPlacer
    {
        Transform mapRoot;
        MapGenConfig config;
        System.Random rng;
        GenerationResult result;
        Dictionary<string, int> categoryCount = new();

        public void Initialize(Transform root, MapGenConfig config, int seed, GenerationResult result)
        {
            this.mapRoot = root;
            this.config = config;
            this.rng = new System.Random(seed);
            this.result = result;
            categoryCount.Clear();
        }

        public int PlaceAssets(MapData map, AssetCategoryRegistry registry)
        {
            if (registry == null)
            {
                result.AddPipelineStep("Pas de registre d'assets - placement ignoré");
                return 0;
            }

            result.AddPipelineStep("Début du placement des assets");
            int totalPlaced = 0;

            bool skipDecor = config.mode == GenerationMode.StructureSeule ||
                             config.mode == GenerationMode.StructureEtGameplay;
            bool skipGameplay = config.mode == GenerationMode.StructureSeule ||
                                config.mode == GenerationMode.StructureEtDecor;

            var enabledCategories = registry.GetEnabledCategories(config.enabledCategories);

            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var cell = map.cells[x, y];
                    if (cell.type == CellType.Vide) continue;

                    foreach (var category in enabledCategories)
                    {
                        if (skipDecor && category.isDecoration && !category.isStructural) continue;
                        if (skipGameplay && category.isGameplay && !category.isStructural) continue;
                        if (config.mode == GenerationMode.SansProps && category.isDecoration && !category.isStructural) continue;

                        if (!category.IsAllowedOnCell(cell.type, cell.biome)) continue;

                        float densityMul = GetDensityMultiplier(category, cell);
                        float chance = category.placementChance * densityMul;

                        for (int i = 0; i < category.maxPerCell; i++)
                        {
                            if ((float)rng.NextDouble() > chance) continue;

                            var prefab = category.GetRandomPrefab(rng);
                            if (prefab == null) continue;

                            Vector3 worldPos = CellToWorld(x, y);
                            worldPos += GetRandomOffset(category);

                            var go = Object.Instantiate(prefab, worldPos,
                                Quaternion.Euler(config.assetRotation), mapRoot);

                            Vector3 scale = config.assetScale;
                            if (category.allowRotationVariation)
                            {
                                float yRot = (float)rng.NextDouble() * 360f;
                                go.transform.Rotate(Vector3.up, yRot, Space.World);
                            }

                            float scaleVar = Mathf.Lerp(category.minScaleVariation,
                                category.maxScaleVariation, (float)rng.NextDouble());
                            go.transform.localScale = scale * scaleVar;

                            if (category.yOffset != 0)
                                go.transform.position += Vector3.up * category.yOffset;

                            go.name = $"{category.categoryId}_{x}_{y}_{i}";
                            cell.placedObjects.Add(go);
                            cell.placedAssetCategories.Add(category.categoryId);

                            if (!categoryCount.ContainsKey(category.categoryId))
                                categoryCount[category.categoryId] = 0;
                            categoryCount[category.categoryId]++;
                            totalPlaced++;
                        }
                    }
                }
            }

            result.objectsPerCategory = new Dictionary<string, int>(categoryCount);
            result.totalObjectsPlaced = totalPlaced;
            result.AddPipelineStep($"Placement terminé: {totalPlaced} objets");

            foreach (var kvp in categoryCount)
                result.AddPipelineStep($"  {kvp.Key}: {kvp.Value}");

            return totalPlaced;
        }

        float GetDensityMultiplier(AssetCategory category, MapCell cell)
        {
            if (cell.type == CellType.Mur)
                return config.vegetationDensity;
            if (cell.type == CellType.Sol)
                return config.decorDensity;
            if (cell.type == CellType.Couloir)
                return config.decorDensity * 0.3f;
            return 1f;
        }

        Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(x * config.cellSize, 0f, y * config.cellSize);
        }

        Vector3 GetRandomOffset(AssetCategory category)
        {
            if (category.minSpacing <= 0) return Vector3.zero;
            float halfCell = config.cellSize * 0.4f;
            float ox = (float)(rng.NextDouble() * 2 - 1) * halfCell;
            float oz = (float)(rng.NextDouble() * 2 - 1) * halfCell;
            return new Vector3(ox, 0, oz);
        }

        public Vector3 GetWorldPosition(Vector2Int cell)
        {
            return new Vector3(cell.x * config.cellSize, 0f, cell.y * config.cellSize);
        }
    }
}
