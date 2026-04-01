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

        /// <summary>
        /// Si true, les categories structurelles (Sols, Eau) sont ignorees.
        /// Active par le mode debug pour ne pas recouvrir le blockout colore.
        /// </summary>
        public bool skipStructuralCategories;

        // Limite de logs detailles par categorie pour eviter le spam
        const int MaxDetailedLogsPerCategory = 3;

        // Positions deja placees pour enforcement de minSpacing
        List<Vector3> placedPositions = new();

        // IDs de categories pour le mapping de densite
        static readonly HashSet<string> VegetationIds = new()
        {
            "Arbres", "Buissons", "Herbe", "Fleurs",
            "Cactus", "Feuillage", "Epines"
        };

        static readonly HashSet<string> RockIds = new()
        {
            "RochesDures", "RochesTendres", "Minerais", "Gemmes"
        };

        public void Initialize(Transform root, MapGenConfig config, int seed, GenerationResult result)
        {
            this.mapRoot = root;
            this.config = config;
            this.rng = new System.Random(seed);
            this.result = result;
            categoryCount.Clear();
            placedPositions.Clear();
        }

        public int PlaceAssets(MapData map, AssetCategoryRegistry registry)
        {
            if (registry == null)
            {
                result.AddPipelineStep("Pas de registre d'assets - placement ignore");
                return 0;
            }

            result.AddPipelineStep("Debut du placement des assets");

            // Log des densites configurees
            Debug.Log($"[AssetPlacer] Densites: veg={config.vegetationDensity:F2} " +
                $"rock={config.rockDensity:F2} decor={config.decorDensity:F2} | " +
                $"skipStructural={skipStructuralCategories}");

            int totalPlaced = 0;
            int skippedSpacing = 0;

            bool skipDecor = config.mode == GenerationMode.StructureSeule ||
                             config.mode == GenerationMode.StructureEtGameplay;
            bool skipGameplay = config.mode == GenerationMode.StructureSeule ||
                                config.mode == GenerationMode.StructureEtDecor;

            var enabledCategories = registry.GetEnabledCategories(config.enabledCategories);

            // Compteur de logs detailles par categorie
            Dictionary<string, int> detailedLogCount = new();

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

                        // Skip des categories structurelles en mode debug
                        if (skipStructuralCategories && category.isStructural) continue;

                        if (!category.IsAllowedOnCell(cell.type, cell.biome)) continue;

                        float densityMul = GetDensityMultiplier(category, cell);
                        float chance = category.placementChance * densityMul;

                        for (int i = 0; i < category.maxPerCell; i++)
                        {
                            if ((float)rng.NextDouble() > chance) continue;

                            var prefab = category.GetRandomPrefab(rng);

                            // Garde-fou: prefab null
                            if (prefab == null)
                            {
                                Debug.LogWarning($"[AssetPlacer] Prefab null dans categorie '{category.categoryId}' " +
                                    $"({category.prefabs.Count} prefabs declares)");
                                continue;
                            }

                            // Garde-fou: verifier renderer/material/shader
                            var prefabRenderer = prefab.GetComponentInChildren<Renderer>();
                            if (prefabRenderer == null)
                            {
                                Debug.LogWarning($"[AssetPlacer] Pas de Renderer sur prefab '{prefab.name}' " +
                                    $"(categorie '{category.categoryId}')");
                            }
                            else if (prefabRenderer.sharedMaterial == null)
                            {
                                Debug.LogWarning($"[AssetPlacer] Material null sur prefab '{prefab.name}' " +
                                    $"(categorie '{category.categoryId}')");
                            }
                            else
                            {
                                string shaderName = prefabRenderer.sharedMaterial.shader.name;
                                if (shaderName == "Standard" ||
                                    shaderName == "Hidden/InternalErrorShader" ||
                                    shaderName.StartsWith("Legacy Shaders/"))
                                {
                                    Debug.LogWarning($"[AssetPlacer] Shader incompatible URP: '{shaderName}' " +
                                        $"sur prefab '{prefab.name}' material '{prefabRenderer.sharedMaterial.name}' " +
                                        $"(categorie '{category.categoryId}'). Convertir en URP/Lit.");
                                }
                            }

                            Vector3 worldPos = CellToWorld(x, y);
                            worldPos += GetRandomOffset(category);

                            // Enforcement de minSpacing : skip si trop proche d'un objet existant
                            if (category.minSpacing > 0 && IsTooClose(worldPos, category.minSpacing))
                            {
                                skippedSpacing++;
                                continue;
                            }

                            var go = Object.Instantiate(prefab, worldPos,
                                Quaternion.Euler(config.assetRotation), mapRoot);

                            // Scale finale = assetScale * scaleMultiplier * variation
                            Vector3 baseScale = config.assetScale * category.scaleMultiplier;
                            if (category.allowRotationVariation)
                            {
                                float yRot = (float)rng.NextDouble() * 360f;
                                go.transform.Rotate(Vector3.up, yRot, Space.World);
                            }

                            float scaleVar = Mathf.Lerp(category.minScaleVariation,
                                category.maxScaleVariation, (float)rng.NextDouble());
                            go.transform.localScale = baseScale * scaleVar;

                            if (category.yOffset != 0)
                                go.transform.position += Vector3.up * category.yOffset;

                            go.name = $"{category.categoryId}_{x}_{y}_{i}";
                            cell.placedObjects.Add(go);
                            cell.placedAssetCategories.Add(category.categoryId);
                            placedPositions.Add(worldPos);

                            if (!categoryCount.ContainsKey(category.categoryId))
                                categoryCount[category.categoryId] = 0;
                            categoryCount[category.categoryId]++;
                            totalPlaced++;

                            // Log detaille pour les premiers placements de chaque categorie
                            if (!detailedLogCount.ContainsKey(category.categoryId))
                                detailedLogCount[category.categoryId] = 0;
                            if (detailedLogCount[category.categoryId] < MaxDetailedLogsPerCategory)
                            {
                                string shaderInfo = "N/A";
                                var rend = go.GetComponentInChildren<Renderer>();
                                if (rend != null && rend.sharedMaterial != null)
                                    shaderInfo = rend.sharedMaterial.shader.name;

                                Debug.Log($"[AssetPlacer] PLACE '{go.name}' | " +
                                    $"cell=({x},{y}) type={cell.type} biome={cell.biome} | " +
                                    $"densMul={densityMul:F2} chance={chance:F2} | " +
                                    $"prefab='{prefab.name}' shader='{shaderInfo}' | " +
                                    $"pos={go.transform.position} rot={go.transform.rotation.eulerAngles} " +
                                    $"scale={go.transform.localScale}");
                                detailedLogCount[category.categoryId]++;
                            }
                        }
                    }
                }
            }

            result.objectsPerCategory = new Dictionary<string, int>(categoryCount);
            result.totalObjectsPlaced = totalPlaced;
            result.AddPipelineStep($"Placement termine: {totalPlaced} objets ({skippedSpacing} ignores par minSpacing)");

            // Log resume par categorie
            foreach (var kvp in categoryCount)
            {
                result.AddPipelineStep($"  {kvp.Key}: {kvp.Value}");
                Debug.Log($"[AssetPlacer] Resume: {kvp.Key} = {kvp.Value} objets places");
            }
            if (skippedSpacing > 0)
                Debug.Log($"[AssetPlacer] {skippedSpacing} placements ignores (minSpacing)");

            return totalPlaced;
        }

        /// <summary>
        /// Verifie si la position candidate est trop proche d'un objet deja place.
        /// Utilise la distance horizontale (XZ) uniquement.
        /// </summary>
        bool IsTooClose(Vector3 candidate, float minDist)
        {
            float minDistSq = minDist * minDist;
            for (int i = placedPositions.Count - 1; i >= 0; i--)
            {
                float dx = candidate.x - placedPositions[i].x;
                float dz = candidate.z - placedPositions[i].z;
                if (dx * dx + dz * dz < minDistSq)
                    return true;
            }
            return false;
        }

        float GetDensityMultiplier(AssetCategory category, MapCell cell)
        {
            float baseDensity = GetCategoryDensity(category);
            float cellFactor = cell.type == CellType.Couloir ? 0.3f : 1f;
            return baseDensity * cellFactor;
        }

        float GetCategoryDensity(AssetCategory category)
        {
            // Structural → toujours 1.0
            if (category.isStructural)
                return 1f;

            string id = category.categoryId;

            // Vegetation → vegetationDensity
            if (VegetationIds.Contains(id))
                return config.vegetationDensity;

            // Rochers / minerais / gemmes → rockDensity
            if (RockIds.Contains(id))
                return config.rockDensity;

            // Decor par defaut → decorDensity
            return config.decorDensity;
        }

        Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(x * config.cellSize, 0f, y * config.cellSize);
        }

        Vector3 GetRandomOffset(AssetCategory category)
        {
            if (category.minSpacing <= 0) return Vector3.zero;
            // Offset reduit : 20% du cellSize pour garder les objets centres dans leur cellule
            float halfCell = config.cellSize * 0.2f;
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
