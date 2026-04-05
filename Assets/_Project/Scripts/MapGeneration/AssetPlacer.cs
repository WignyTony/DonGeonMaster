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

        public bool skipStructuralCategories;

        /// <summary>Info de rendu sol par cellule (x,y) → CellSupportInfo. Alimente par le renderer avant PlaceAssets.</summary>
        public Dictionary<(int x, int y), CellSupportInfo> cellRenderLookup;

        Dictionary<string, List<Vector3>> placedPerCategory = new();

        // Compteurs
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
        int attemptIndex;

        const float SpawnExclusionRadius = 12f;

        struct BigInfo { public string prefab; public string cat; public Vector3 bSize; public Vector3 pos; public float maxDim; }
        List<BigInfo> biggestPlaced = new();

        void Inc(Dictionary<string, int> d, string k) { if (!d.ContainsKey(k)) d[k] = 0; d[k]++; }

        void ResetCounters()
        {
            cntPlaced.Clear(); cntSkipChance.Clear(); cntSkipSpacing.Clear();
            cntSkipPrefabNull.Clear(); cntSkipSpawnZone.Clear(); cntSkipOversize.Clear();
            placedPerCategory.Clear();
            totalAttempted = 0; totalPlaced = 0; totalSkipChance = 0;
            totalSkipSpacing = 0; totalSkipPrefabNull = 0;
            totalSkipSpawnZone = 0; totalSkipOversize = 0;
            hasAnyPlaced = false; attemptIndex = 0;
            posMin = new Vector3(float.MaxValue, 0, float.MaxValue);
            posMax = new Vector3(float.MinValue, 0, float.MinValue);
            biggestPlaced.Clear();
        }

        void TrackPos(Vector3 p)
        {
            if (!hasAnyPlaced) { posMin = p; posMax = p; hasAnyPlaced = true; }
            else { if (p.x < posMin.x) posMin.x = p.x; if (p.z < posMin.z) posMin.z = p.z; if (p.x > posMax.x) posMax.x = p.x; if (p.z > posMax.z) posMax.z = p.z; }
        }

        public void Initialize(Transform root, MapGenConfig cfg, int seed, GenerationResult res)
        {
            mapRoot = root; config = cfg; rng = new System.Random(seed); result = res;
            ResetCounters();
        }

        public int PlaceAssets(MapData map, AssetCategoryRegistry registry)
        {
            if (registry == null) { result.AddPipelineStep("Pas de registre"); Debug.LogWarning("[AssetPlacer] registry null"); return 0; }

            result.AddPipelineStep("Debut placement");
            bool skipD = config.mode == GenerationMode.StructureSeule || config.mode == GenerationMode.StructureEtGameplay;
            bool skipG = config.mode == GenerationMode.StructureSeule || config.mode == GenerationMode.StructureEtDecor;
            var cats = registry.GetEnabledCategories(config.enabledCategories);

            Debug.Log($"[AssetPlacer] Debut | cats:{cats.Count} veg={config.vegetationDensity:F2} rock={config.rockDensity:F2} decor={config.decorDensity:F2} skipStruct={skipStructuralCategories}");

            Vector3 spawnW = CellToWorld(map.spawnCell.x, map.spawnCell.y);
            Vector3 exitW = CellToWorld(map.exitCell.x, map.exitCell.y);
            float exclSq = SpawnExclusionRadius * SpawnExclusionRadius;

            // Demarrer le dump
            PlacementDebugDump.Begin(config, map, cats.Count);

            HashSet<string> shaderWarned = new();

            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var cell = map.cells[x, y];
                    if (cell.type == CellType.Vide) continue;

                    foreach (var cat in cats)
                    {
                        if (skipD && cat.isDecoration && !cat.isStructural) continue;
                        if (skipG && cat.isGameplay && !cat.isStructural) continue;
                        if (config.mode == GenerationMode.SansProps && cat.isDecoration && !cat.isStructural) continue;
                        if (skipStructuralCategories && cat.isStructural) continue;
                        if (!cat.IsAllowedOnCell(cell.type, cell.biome)) continue;

                        float dm = GetDensityMultiplier(cat, cell);
                        float chance = cat.placementChance * dm;

                        for (int i = 0; i < cat.maxPerCell; i++)
                        {
                            totalAttempted++;
                            attemptIndex++;

                            // Base attempt record
                            // Support visuel sol
                            string supMode = "", supType = "", supObj = "";
                            if (cellRenderLookup != null && cellRenderLookup.TryGetValue((x, y), out var cri))
                            {
                                supMode = cri.renderMode;
                                supType = cri.materialName;
                                supObj = cri.objectName;
                            }

                            var rec = new PlacementAttempt
                            {
                                attemptIndex = attemptIndex,
                                categoryId = cat.categoryId,
                                biome = cell.biome.ToString(),
                                supportCellType = cell.type.ToString(),
                                surfaceShape = cell.surfaceShape.ToString(),
                                cellFloorHeight = cell.floorHeight,
                                cellX = x, cellY = y,
                                supportCenterX = x * config.cellSize,
                                supportCenterY = 0,
                                supportCenterZ = y * config.cellSize,
                                initScaleX = config.assetScale.x,
                                initScaleY = config.assetScale.y,
                                initScaleZ = config.assetScale.z,
                                categorySizeCap = cat.EffectiveMaxBoundsSize,
                                supportRenderMode = supMode,
                                supportVisualType = supType,
                                supportObjectName = supObj
                            };

                            // Chance check
                            if ((float)rng.NextDouble() > chance)
                            {
                                totalSkipChance++; Inc(cntSkipChance, cat.categoryId);
                                rec.finalStatus = "skip_chance"; rec.prefabName = "";
                                PlacementDebugDump.Record(rec);
                                continue;
                            }

                            var prefab = cat.GetRandomPrefab(rng);
                            if (prefab == null)
                            {
                                totalSkipPrefabNull++; Inc(cntSkipPrefabNull, cat.categoryId);
                                rec.finalStatus = "skip_prefabNull"; rec.prefabName = "";
                                PlacementDebugDump.Record(rec);
                                continue;
                            }
                            rec.prefabName = prefab.name;

                            // Shader warn (once)
                            if (!shaderWarned.Contains(cat.categoryId))
                            {
                                var pr = prefab.GetComponentInChildren<Renderer>();
                                if (pr != null && pr.sharedMaterial != null)
                                {
                                    string sn = pr.sharedMaterial.shader.name;
                                    if (sn == "Standard" || sn == "Hidden/InternalErrorShader" || sn.StartsWith("Legacy Shaders/"))
                                        Debug.LogWarning($"[AssetPlacer] Shader URP incompatible: '{sn}' on '{prefab.name}' ({cat.categoryId})");
                                }
                                shaderWarned.Add(cat.categoryId);
                            }

                            Vector3 wp = CellToWorld(x, y, cell.floorHeight) + GetRandomOffset(cat);
                            rec.worldPosX = wp.x; rec.worldPosY = wp.y; rec.worldPosZ = wp.z;

                            float dS = Mathf.Sqrt((wp.x - spawnW.x) * (wp.x - spawnW.x) + (wp.z - spawnW.z) * (wp.z - spawnW.z));
                            float dE = Mathf.Sqrt((wp.x - exitW.x) * (wp.x - exitW.x) + (wp.z - exitW.z) * (wp.z - exitW.z));
                            rec.distanceToSpawn = dS; rec.distanceToExit = dE;

                            // Spawn zone
                            if (dS * dS < exclSq * 1.0001f || dE * dE < exclSq * 1.0001f)
                            {
                                totalSkipSpawnZone++; Inc(cntSkipSpawnZone, cat.categoryId);
                                rec.finalStatus = "skip_spawnZone";
                                PlacementDebugDump.Record(rec);
                                continue;
                            }

                            // Spacing
                            if (cat.minSpacing > 0 && IsTooClose(cat.categoryId, wp, cat.minSpacing))
                            {
                                totalSkipSpacing++; Inc(cntSkipSpacing, cat.categoryId);
                                rec.finalStatus = "skip_spacing";
                                PlacementDebugDump.Record(rec);
                                continue;
                            }

                            // === INSTANTIATE ===
                            // Rotation de base : structurel (tiles) utilise assetRotation (-90 X),
                            // props utilisent identity (debout) + variation Y aleatoire
                            Quaternion baseRot = cat.isStructural
                                ? Quaternion.Euler(config.assetRotation)
                                : Quaternion.identity;

                            var go = Object.Instantiate(prefab, wp, baseRot, mapRoot);

                            Vector3 baseScale = config.assetScale * cat.scaleMultiplier;
                            rec.scaleAfterMultX = baseScale.x; rec.scaleAfterMultY = baseScale.y; rec.scaleAfterMultZ = baseScale.z;

                            if (cat.allowRotationVariation)
                            {
                                float yr = (float)rng.NextDouble() * 360f;
                                go.transform.Rotate(Vector3.up, yr, Space.World);
                            }

                            float sv = Mathf.Lerp(cat.minScaleVariation, cat.maxScaleVariation, (float)rng.NextDouble());
                            go.transform.localScale = baseScale * sv;

                            if (cat.yOffset != 0)
                            {
                                go.transform.position += Vector3.up * cat.yOffset;
                                rec.yOffsetApplied = cat.yOffset;
                            }

                            // Bounds
                            var renderers = go.GetComponentsInChildren<Renderer>();
                            rec.rendererCount = renderers.Length;
                            Bounds cb = new Bounds(go.transform.position, Vector3.zero);
                            foreach (var r in renderers) cb.Encapsulate(r.bounds);
                            float maxDim = Mathf.Max(cb.size.x, cb.size.y, cb.size.z);

                            // Clamp
                            float cap = cat.EffectiveMaxBoundsSize;
                            bool clamped = false;
                            float clampRatio = 1f;
                            if (maxDim > cap && maxDim > 0.01f)
                            {
                                clampRatio = cap / maxDim;
                                go.transform.localScale *= clampRatio;
                                clamped = true;

                                cb = new Bounds(go.transform.position, Vector3.zero);
                                foreach (var r in renderers) cb.Encapsulate(r.bounds);
                                maxDim = Mathf.Max(cb.size.x, cb.size.y, cb.size.z);

                                if (maxDim > cap * 2f)
                                {
                                    Object.Destroy(go);
                                    totalSkipOversize++; Inc(cntSkipOversize, cat.categoryId);
                                    rec.finalStatus = "skip_oversize";
                                    rec.wasBoundsClamped = true; rec.clampRatio = clampRatio;
                                    PlacementDebugDump.Record(rec);
                                    continue;
                                }
                            }

                            // Fill record with final data
                            rec.finalStatus = "placed";
                            var fp = go.transform.position;
                            var fe = go.transform.rotation.eulerAngles;
                            var fs = go.transform.localScale;
                            rec.worldPosX = fp.x; rec.worldPosY = fp.y; rec.worldPosZ = fp.z;
                            rec.rotEulerX = fe.x; rec.rotEulerY = fe.y; rec.rotEulerZ = fe.z;
                            rec.scaleX = fs.x; rec.scaleY = fs.y; rec.scaleZ = fs.z;
                            rec.scaleAfterClampX = fs.x; rec.scaleAfterClampY = fs.y; rec.scaleAfterClampZ = fs.z;
                            rec.wasBoundsClamped = clamped; rec.clampRatio = clampRatio;
                            rec.boundsCenterX = cb.center.x; rec.boundsCenterY = cb.center.y; rec.boundsCenterZ = cb.center.z;
                            rec.boundsSizeX = cb.size.x; rec.boundsSizeY = cb.size.y; rec.boundsSizeZ = cb.size.z;
                            rec.boundsMinX = cb.min.x; rec.boundsMinY = cb.min.y; rec.boundsMinZ = cb.min.z;
                            rec.boundsMaxX = cb.max.x; rec.boundsMaxY = cb.max.y; rec.boundsMaxZ = cb.max.z;
                            rec.maxDimension = maxDim;

                            // Orientation
                            rec.prefabForwardX = go.transform.forward.x;
                            rec.prefabForwardY = go.transform.forward.y;
                            rec.prefabForwardZ = go.transform.forward.z;
                            rec.prefabUpX = go.transform.up.x;
                            rec.prefabUpY = go.transform.up.y;
                            rec.prefabUpZ = go.transform.up.z;
                            if (renderers.Length > 0)
                            {
                                var fr = renderers[0];
                                rec.firstRendPosX = fr.transform.position.x;
                                rec.firstRendPosY = fr.transform.position.y;
                                rec.firstRendPosZ = fr.transform.position.z;
                                var re = fr.transform.rotation.eulerAngles;
                                rec.firstRendRotX = re.x; rec.firstRendRotY = re.y; rec.firstRendRotZ = re.z;
                            }
                            rec.estimatedTouchesGround = cb.min.y <= 0.5f;

                            PlacementDebugDump.Record(rec);

                            go.name = $"{cat.categoryId}_{x}_{y}_{i}";
                            cell.placedObjects.Add(go);
                            cell.placedAssetCategories.Add(cat.categoryId);

                            if (!placedPerCategory.ContainsKey(cat.categoryId))
                                placedPerCategory[cat.categoryId] = new List<Vector3>();
                            placedPerCategory[cat.categoryId].Add(wp);

                            Inc(cntPlaced, cat.categoryId);
                            totalPlaced++;
                            TrackPos(wp);

                            biggestPlaced.Add(new BigInfo { prefab = prefab.name, cat = cat.categoryId, bSize = cb.size, pos = wp, maxDim = maxDim });
                        }
                    }
                }
            }

            // Finalize dump
            var allRej = new Dictionary<string, int>();
            foreach (var k in cntSkipChance) { if (!allRej.ContainsKey(k.Key)) allRej[k.Key] = 0; allRej[k.Key] += k.Value; }
            foreach (var k in cntSkipSpacing) { if (!allRej.ContainsKey(k.Key)) allRej[k.Key] = 0; allRej[k.Key] += k.Value; }
            foreach (var k in cntSkipSpawnZone) { if (!allRej.ContainsKey(k.Key)) allRej[k.Key] = 0; allRej[k.Key] += k.Value; }
            foreach (var k in cntSkipOversize) { if (!allRej.ContainsKey(k.Key)) allRej[k.Key] = 0; allRej[k.Key] += k.Value; }
            foreach (var k in cntSkipPrefabNull) { if (!allRej.ContainsKey(k.Key)) allRej[k.Key] = 0; allRej[k.Key] += k.Value; }

            PlacementDebugDump.Finalize(
                totalAttempted, totalPlaced,
                totalSkipChance, totalSkipSpacing, totalSkipSpawnZone, totalSkipOversize, totalSkipPrefabNull,
                cntPlaced, allRej);

            // Synthese console
            result.objectsPerCategory = new Dictionary<string, int>(cntPlaced);
            result.totalObjectsPlaced = totalPlaced;
            LogSynthesis(cats.Count);

            result.AddPipelineStep($"Placement: {totalPlaced}/{totalAttempted} (chance:{totalSkipChance} spacing:{totalSkipSpacing} spawn:{totalSkipSpawnZone} over:{totalSkipOversize})");
            foreach (var kvp in cntPlaced) result.AddPipelineStep($"  {kvp.Key}: {kvp.Value}");

            return totalPlaced;
        }

        void LogSynthesis(int ec)
        {
            Debug.Log("[AssetPlacer] ══════ SYNTHESE ══════");
            Debug.Log($"[AssetPlacer] Attempted:{totalAttempted} Placed:{totalPlaced}");
            Debug.Log($"[AssetPlacer] Skip — chance:{totalSkipChance} spacing:{totalSkipSpacing} spawn:{totalSkipSpawnZone} over:{totalSkipOversize} null:{totalSkipPrefabNull}");
            if (hasAnyPlaced) Debug.Log($"[AssetPlacer] Pos — min:({posMin.x:F0},{posMin.z:F0}) max:({posMax.x:F0},{posMax.z:F0})");

            var ids = new HashSet<string>();
            foreach (var k in cntPlaced.Keys) ids.Add(k);
            foreach (var k in cntSkipChance.Keys) ids.Add(k);
            foreach (var k in cntSkipOversize.Keys) ids.Add(k);
            foreach (var id in ids)
            {
                int p = cntPlaced.ContainsKey(id) ? cntPlaced[id] : 0;
                int sc = cntSkipChance.ContainsKey(id) ? cntSkipChance[id] : 0;
                int ss = cntSkipSpacing.ContainsKey(id) ? cntSkipSpacing[id] : 0;
                int sz = cntSkipSpawnZone.ContainsKey(id) ? cntSkipSpawnZone[id] : 0;
                int ov = cntSkipOversize.ContainsKey(id) ? cntSkipOversize[id] : 0;
                Debug.Log($"[AssetPlacer]   {id}: {p} placed | ch:{sc} sp:{ss} sz:{sz} over:{ov}");
            }

            if (totalPlaced == 0)
            {
                Debug.LogWarning("[AssetPlacer] *** ZERO PLACES ***");
                if (totalAttempted == 0) Debug.LogWarning("[AssetPlacer] Aucune tentative");
                else if (totalSkipChance == totalAttempted) Debug.LogWarning("[AssetPlacer] 100% refuse par chance");
            }

            if (biggestPlaced.Count > 0)
            {
                Debug.Log("[AssetPlacer] ── TOP 10 ──");
                foreach (var p in biggestPlaced.OrderByDescending(b => b.maxDim).Take(10))
                    Debug.Log($"[AssetPlacer]   {p.prefab} ({p.cat}) maxDim={p.maxDim:F1} bounds=({p.bSize.x:F1},{p.bSize.y:F1},{p.bSize.z:F1})");
            }
            Debug.Log("[AssetPlacer] ════════════════════");
        }

        bool IsTooClose(string id, Vector3 c, float d)
        {
            if (!placedPerCategory.ContainsKey(id)) return false;
            var ps = placedPerCategory[id]; float dSq = d * d;
            for (int i = ps.Count - 1; i >= 0; i--) { float dx = c.x - ps[i].x, dz = c.z - ps[i].z; if (dx * dx + dz * dz < dSq) return true; }
            return false;
        }

        float GetDensityMultiplier(AssetCategory cat, MapCell cell)
        {
            float bd;
            switch (cat.densityType)
            {
                case DensityType.Structural: bd = 1f; break;
                case DensityType.Vegetation: bd = config.vegetationDensity; break;
                case DensityType.Rock:       bd = config.rockDensity; break;
                default:                     bd = config.decorDensity; break;
            }
            return bd * (cell.type == CellType.Couloir ? 0.3f : 1f);
        }

        Vector3 CellToWorld(int x, int y, float floorHeight = 0f) => new Vector3(x * config.cellSize, floorHeight, y * config.cellSize);

        Vector3 GetRandomOffset(AssetCategory cat)
        {
            if (cat.minSpacing <= 0) return Vector3.zero;
            float h = config.cellSize * 0.25f;
            return new Vector3((float)(rng.NextDouble() * 2 - 1) * h, 0, (float)(rng.NextDouble() * 2 - 1) * h);
        }

        public Vector3 GetWorldPosition(Vector2Int c) => new Vector3(c.x * config.cellSize, 0f, c.y * config.cellSize);
    }
}
