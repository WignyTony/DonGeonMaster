using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    public class AssetPlacer
    {
        Transform mapRoot;
        MapGenConfig config;
        System.Random rng;
        GenerationResult result;

        /// <summary>
        /// Si true, les categories structurelles (Sols, Eau) sont ignorees.
        /// Active par le mode debug pour ne pas recouvrir le blockout colore.
        /// </summary>
        public bool skipStructuralCategories;

        // Positions placees PAR CATEGORIE pour enforcement de minSpacing
        Dictionary<string, List<Vector3>> placedPerCategory = new();

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

        // === COMPTEURS ===
        Dictionary<string, int> cntPlaced = new();
        Dictionary<string, int> cntSkipChance = new();
        Dictionary<string, int> cntSkipSpacing = new();
        Dictionary<string, int> cntSkipPrefabNull = new();
        Dictionary<string, int> cntSkipSpawnZone = new();
        Dictionary<string, int> cntSkipOversize = new();

        int totalAttempted, totalPlaced, totalSkipChance, totalSkipSpacing;
        int totalSkipPrefabNull, totalSkipSpawnZone, totalSkipOversize;

        Vector3 posMin, posMax;
        bool hasAnyPlaced;

        // Gabarits max en unites monde par famille (largeur ou hauteur max)
        // cellSize=6, wallHeight=3 → les props doivent rester sous ces reperes
        const float MaxBoundsTree = 4.5f;
        const float MaxBoundsBush = 3.0f;
        const float MaxBoundsRock = 3.0f;
        const float MaxBoundsSmall = 2.0f;

        // Rayon no-spawn autour du spawn et de l'exit (en unites monde)
        const float SpawnExclusionRadius = 12f;

        // Tracker les plus gros objets places pour le log final
        struct PlacedInfo
        {
            public string prefabName;
            public string categoryId;
            public Vector3 boundsSize;
            public Vector3 position;
            public float maxDim;
        }
        List<PlacedInfo> biggestPlaced = new();

        void Inc(Dictionary<string, int> dict, string key)
        {
            if (!dict.ContainsKey(key)) dict[key] = 0;
            dict[key]++;
        }

        void ResetCounters()
        {
            cntPlaced.Clear(); cntSkipChance.Clear(); cntSkipSpacing.Clear();
            cntSkipPrefabNull.Clear(); cntSkipSpawnZone.Clear(); cntSkipOversize.Clear();
            placedPerCategory.Clear();
            totalAttempted = 0; totalPlaced = 0; totalSkipChance = 0;
            totalSkipSpacing = 0; totalSkipPrefabNull = 0;
            totalSkipSpawnZone = 0; totalSkipOversize = 0;
            hasAnyPlaced = false;
            posMin = new Vector3(float.MaxValue, 0, float.MaxValue);
            posMax = new Vector3(float.MinValue, 0, float.MinValue);
            biggestPlaced.Clear();
        }

        void TrackPosition(Vector3 pos)
        {
            if (!hasAnyPlaced) { posMin = pos; posMax = pos; hasAnyPlaced = true; }
            else
            {
                if (pos.x < posMin.x) posMin.x = pos.x;
                if (pos.z < posMin.z) posMin.z = pos.z;
                if (pos.x > posMax.x) posMax.x = pos.x;
                if (pos.z > posMax.z) posMax.z = pos.z;
            }
        }

        float GetMaxBoundsForCategory(string categoryId)
        {
            if (categoryId == "Arbres" || categoryId == "Cactus") return MaxBoundsTree;
            if (categoryId == "Buissons" || categoryId == "Feuillage" || categoryId == "Epines") return MaxBoundsBush;
            if (categoryId == "RochesDures" || categoryId == "RochesTendres" || categoryId == "Troncs") return MaxBoundsRock;
            return MaxBoundsSmall;
        }

        public void Initialize(Transform root, MapGenConfig config, int seed, GenerationResult result)
        {
            this.mapRoot = root;
            this.config = config;
            this.rng = new System.Random(seed);
            this.result = result;
            ResetCounters();
        }

        public int PlaceAssets(MapData map, AssetCategoryRegistry registry)
        {
            if (registry == null)
            {
                result.AddPipelineStep("Pas de registre d'assets - placement ignore");
                Debug.LogWarning("[AssetPlacer] registry == null, placement annule");
                return 0;
            }

            result.AddPipelineStep("Debut du placement des assets");

            bool skipDecor = config.mode == GenerationMode.StructureSeule ||
                             config.mode == GenerationMode.StructureEtGameplay;
            bool skipGameplay = config.mode == GenerationMode.StructureSeule ||
                                config.mode == GenerationMode.StructureEtDecor;

            var enabledCategories = registry.GetEnabledCategories(config.enabledCategories);

            Debug.Log($"[AssetPlacer] Debut placement | categories actives: {enabledCategories.Count} | " +
                $"densites: veg={config.vegetationDensity:F2} rock={config.rockDensity:F2} decor={config.decorDensity:F2} | " +
                $"skipStructural={skipStructuralCategories} mode={config.mode}");

            // Positions monde du spawn et de l'exit pour la zone d'exclusion
            Vector3 spawnWorld = CellToWorld(map.spawnCell.x, map.spawnCell.y);
            Vector3 exitWorld = CellToWorld(map.exitCell.x, map.exitCell.y);
            float exclRadSq = SpawnExclusionRadius * SpawnExclusionRadius;

            Debug.Log($"[AssetPlacer] Spawn exclusion: radius={SpawnExclusionRadius} " +
                $"spawn=({spawnWorld.x:F0},{spawnWorld.z:F0}) exit=({exitWorld.x:F0},{exitWorld.z:F0})");

            HashSet<string> shaderWarned = new();

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
                        if (skipStructuralCategories && category.isStructural) continue;
                        if (!category.IsAllowedOnCell(cell.type, cell.biome)) continue;

                        float densityMul = GetDensityMultiplier(category, cell);
                        float chance = category.placementChance * densityMul;

                        for (int i = 0; i < category.maxPerCell; i++)
                        {
                            totalAttempted++;

                            if ((float)rng.NextDouble() > chance)
                            {
                                totalSkipChance++;
                                Inc(cntSkipChance, category.categoryId);
                                continue;
                            }

                            var prefab = category.GetRandomPrefab(rng);
                            if (prefab == null)
                            {
                                totalSkipPrefabNull++;
                                Inc(cntSkipPrefabNull, category.categoryId);
                                continue;
                            }

                            // Shader check (une fois par categorie)
                            if (!shaderWarned.Contains(category.categoryId))
                            {
                                var rend = prefab.GetComponentInChildren<Renderer>();
                                if (rend != null && rend.sharedMaterial != null)
                                {
                                    string sn = rend.sharedMaterial.shader.name;
                                    if (sn == "Standard" || sn == "Hidden/InternalErrorShader" || sn.StartsWith("Legacy Shaders/"))
                                        Debug.LogWarning($"[AssetPlacer] Shader incompatible URP: '{sn}' sur '{prefab.name}' (categorie '{category.categoryId}')");
                                }
                                shaderWarned.Add(category.categoryId);
                            }

                            Vector3 worldPos = CellToWorld(x, y);
                            worldPos += GetRandomOffset(category);

                            // Zone d'exclusion autour du spawn et de l'exit
                            float dxS = worldPos.x - spawnWorld.x, dzS = worldPos.z - spawnWorld.z;
                            float dxE = worldPos.x - exitWorld.x, dzE = worldPos.z - exitWorld.z;
                            if (dxS * dxS + dzS * dzS < exclRadSq || dxE * dxE + dzE * dzE < exclRadSq)
                            {
                                totalSkipSpawnZone++;
                                Inc(cntSkipSpawnZone, category.categoryId);
                                continue;
                            }

                            // minSpacing PAR CATEGORIE
                            if (category.minSpacing > 0 && IsTooCloseInCategory(category.categoryId, worldPos, category.minSpacing))
                            {
                                totalSkipSpacing++;
                                Inc(cntSkipSpacing, category.categoryId);
                                continue;
                            }

                            var go = Object.Instantiate(prefab, worldPos,
                                Quaternion.Euler(config.assetRotation), mapRoot);

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

                            // Mesurer les bounds reelles apres instanciation + scale
                            var allRenderers = go.GetComponentsInChildren<Renderer>();
                            Bounds combinedBounds = new Bounds(go.transform.position, Vector3.zero);
                            foreach (var r in allRenderers)
                                combinedBounds.Encapsulate(r.bounds);
                            Vector3 bSize = combinedBounds.size;
                            float maxDim = Mathf.Max(bSize.x, bSize.y, bSize.z);

                            // Gabarit max : rescaler si depasse
                            float maxAllowed = GetMaxBoundsForCategory(category.categoryId);
                            if (maxDim > maxAllowed && maxDim > 0.01f)
                            {
                                float shrink = maxAllowed / maxDim;
                                go.transform.localScale *= shrink;

                                // Remesurer
                                combinedBounds = new Bounds(go.transform.position, Vector3.zero);
                                foreach (var r in allRenderers)
                                    combinedBounds.Encapsulate(r.bounds);
                                bSize = combinedBounds.size;
                                float newMax = Mathf.Max(bSize.x, bSize.y, bSize.z);

                                // Si meme apres shrink c'est encore > 2x le gabarit, skip
                                if (newMax > maxAllowed * 2f)
                                {
                                    Object.Destroy(go);
                                    totalSkipOversize++;
                                    Inc(cntSkipOversize, category.categoryId);
                                    continue;
                                }
                            }

                            go.name = $"{category.categoryId}_{x}_{y}_{i}";
                            cell.placedObjects.Add(go);
                            cell.placedAssetCategories.Add(category.categoryId);

                            if (!placedPerCategory.ContainsKey(category.categoryId))
                                placedPerCategory[category.categoryId] = new List<Vector3>();
                            placedPerCategory[category.categoryId].Add(worldPos);

                            Inc(cntPlaced, category.categoryId);
                            totalPlaced++;
                            TrackPosition(worldPos);

                            // Tracker pour le log des plus gros
                            biggestPlaced.Add(new PlacedInfo
                            {
                                prefabName = prefab.name,
                                categoryId = category.categoryId,
                                boundsSize = bSize,
                                position = worldPos,
                                maxDim = Mathf.Max(bSize.x, bSize.y, bSize.z)
                            });
                        }
                    }
                }
            }

            // === SYNTHESE ===
            result.objectsPerCategory = new Dictionary<string, int>(cntPlaced);
            result.totalObjectsPlaced = totalPlaced;

            LogSynthesis(enabledCategories.Count);

            result.AddPipelineStep($"Placement termine: {totalPlaced}/{totalAttempted} " +
                $"(chance:{totalSkipChance} spacing:{totalSkipSpacing} spawn:{totalSkipSpawnZone} oversize:{totalSkipOversize})");
            foreach (var kvp in cntPlaced)
                result.AddPipelineStep($"  {kvp.Key}: {kvp.Value}");

            return totalPlaced;
        }

        void LogSynthesis(int enabledCount)
        {
            Debug.Log("[AssetPlacer] ══════ SYNTHESE PLACEMENT ══════");
            Debug.Log($"[AssetPlacer] Attempted: {totalAttempted} | Placed: {totalPlaced}");
            Debug.Log($"[AssetPlacer] Skipped — chance:{totalSkipChance} spacing:{totalSkipSpacing} " +
                $"spawnZone:{totalSkipSpawnZone} oversize:{totalSkipOversize} prefabNull:{totalSkipPrefabNull}");

            if (hasAnyPlaced)
                Debug.Log($"[AssetPlacer] Positions — min:({posMin.x:F0},{posMin.z:F0}) max:({posMax.x:F0},{posMax.z:F0})");

            // Par categorie
            HashSet<string> allIds = new();
            foreach (var k in cntPlaced.Keys) allIds.Add(k);
            foreach (var k in cntSkipChance.Keys) allIds.Add(k);
            foreach (var k in cntSkipSpacing.Keys) allIds.Add(k);
            foreach (var k in cntSkipSpawnZone.Keys) allIds.Add(k);
            foreach (var k in cntSkipOversize.Keys) allIds.Add(k);

            foreach (var id in allIds)
            {
                int p = cntPlaced.ContainsKey(id) ? cntPlaced[id] : 0;
                int sc = cntSkipChance.ContainsKey(id) ? cntSkipChance[id] : 0;
                int ss = cntSkipSpacing.ContainsKey(id) ? cntSkipSpacing[id] : 0;
                int sz = cntSkipSpawnZone.ContainsKey(id) ? cntSkipSpawnZone[id] : 0;
                int ov = cntSkipOversize.ContainsKey(id) ? cntSkipOversize[id] : 0;
                Debug.Log($"[AssetPlacer]   {id}: {p} places | chance:{sc} spacing:{ss} spawnZone:{sz} oversize:{ov}");
            }

            // Detection zero-placement
            if (totalPlaced == 0)
            {
                Debug.LogWarning("[AssetPlacer] *** ZERO OBJETS PLACES ***");
                Debug.LogWarning($"[AssetPlacer] Categories actives: {enabledCount} | Attempted: {totalAttempted}");
                if (totalAttempted == 0)
                    Debug.LogWarning("[AssetPlacer] Aucune tentative — verifier biomes/cellTypes/mode");
                else if (totalSkipChance == totalAttempted)
                    Debug.LogWarning("[AssetPlacer] 100% refuse par chance — densites trop basses");
            }

            // Top 10 plus gros prefabs places
            if (biggestPlaced.Count > 0)
            {
                Debug.Log("[AssetPlacer] ── TOP 10 PLUS GROS PREFABS ──");
                var top = biggestPlaced.OrderByDescending(p => p.maxDim).Take(10);
                foreach (var p in top)
                {
                    Debug.Log($"[AssetPlacer]   {p.prefabName} ({p.categoryId}) " +
                        $"bounds=({p.boundsSize.x:F1},{p.boundsSize.y:F1},{p.boundsSize.z:F1}) " +
                        $"maxDim={p.maxDim:F1} pos=({p.position.x:F0},{p.position.z:F0})");
                }
            }

            Debug.Log("[AssetPlacer] ════════════════════════════════");
        }

        bool IsTooCloseInCategory(string categoryId, Vector3 candidate, float minDist)
        {
            if (!placedPerCategory.ContainsKey(categoryId))
                return false;

            var positions = placedPerCategory[categoryId];
            float minDistSq = minDist * minDist;

            for (int i = positions.Count - 1; i >= 0; i--)
            {
                float dx = candidate.x - positions[i].x;
                float dz = candidate.z - positions[i].z;
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
            if (category.isStructural) return 1f;
            string id = category.categoryId;
            if (VegetationIds.Contains(id)) return config.vegetationDensity;
            if (RockIds.Contains(id)) return config.rockDensity;
            return config.decorDensity;
        }

        Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(x * config.cellSize, 0f, y * config.cellSize);
        }

        Vector3 GetRandomOffset(AssetCategory category)
        {
            if (category.minSpacing <= 0) return Vector3.zero;
            float halfCell = config.cellSize * 0.25f;
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
