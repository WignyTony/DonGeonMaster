using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    /// <summary>
    /// Donnees d'une tentative de placement (placee ou rejetee).
    /// </summary>
    [Serializable]
    public struct PlacementAttempt
    {
        // Identite
        public int attemptIndex;
        public string finalStatus; // placed, skip_chance, skip_spacing, skip_spawnZone, skip_prefabNull, skip_oversize
        public string categoryId;
        public string prefabName;
        public string biome;
        public string supportCellType;
        public int cellX, cellY;

        // Transform monde
        public float worldPosX, worldPosY, worldPosZ;
        public float rotEulerX, rotEulerY, rotEulerZ;
        public float scaleX, scaleY, scaleZ;

        // Support / pose
        public float supportCenterX, supportCenterY, supportCenterZ;
        public float yOffsetApplied;
        public float distanceToSpawn, distanceToExit;

        // Bounds
        public float boundsCenterX, boundsCenterY, boundsCenterZ;
        public float boundsSizeX, boundsSizeY, boundsSizeZ;
        public float boundsMinX, boundsMinY, boundsMinZ;
        public float boundsMaxX, boundsMaxY, boundsMaxZ;
        public float maxDimension;
        public int rendererCount;

        // Clamp
        public float initScaleX, initScaleY, initScaleZ;
        public float scaleAfterMultX, scaleAfterMultY, scaleAfterMultZ;
        public float scaleAfterClampX, scaleAfterClampY, scaleAfterClampZ;
        public bool wasBoundsClamped;
        public float clampRatio;
        public float categorySizeCap;

        // Orientation
        public float prefabForwardX, prefabForwardY, prefabForwardZ;
        public float prefabUpX, prefabUpY, prefabUpZ;
        public float firstRendPosX, firstRendPosY, firstRendPosZ;
        public float firstRendRotX, firstRendRotY, firstRendRotZ;
        public bool estimatedTouchesGround;
    }

    /// <summary>
    /// Metadonnees globales de generation + liste de tentatives.
    /// </summary>
    [Serializable]
    public class PlacementDumpGlobal
    {
        public int seed;
        public int mapWidth, mapHeight;
        public float cellSize;
        public float spawnWorldX, spawnWorldZ;
        public float exitWorldX, exitWorldZ;
        public int spawnCellX, spawnCellY;
        public int exitCellX, exitCellY;
        public float vegDensity, rockDensity, decorDensity;
        public int activeCategoryCount;
        public int totalAttempted, totalPlaced;
        public int skipChance, skipSpacing, skipSpawnZone, skipOversize, skipPrefabNull;
        public string placedPerCategory;   // serialise en "cat:n,cat:n"
        public string rejectedPerCategory; // idem
    }

    /// <summary>
    /// Collecte les tentatives de placement et exporte en JSON/CSV/TXT fixes.
    /// Fichiers ecrases a chaque generation.
    /// </summary>
    public static class PlacementDebugDump
    {
        static readonly string DumpFolder = Path.Combine(Application.dataPath, "..", "MapGenLogs");
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static PlacementDumpGlobal global;
        static List<PlacementAttempt> attempts;

        public static void Begin(MapGenConfig config, MapData map, int activeCats)
        {
            attempts = new List<PlacementAttempt>(4096);
            global = new PlacementDumpGlobal
            {
                seed = config.seed,
                mapWidth = config.mapWidth,
                mapHeight = config.mapHeight,
                cellSize = config.cellSize,
                spawnWorldX = map.spawnCell.x * config.cellSize,
                spawnWorldZ = map.spawnCell.y * config.cellSize,
                exitWorldX = map.exitCell.x * config.cellSize,
                exitWorldZ = map.exitCell.y * config.cellSize,
                spawnCellX = map.spawnCell.x, spawnCellY = map.spawnCell.y,
                exitCellX = map.exitCell.x, exitCellY = map.exitCell.y,
                vegDensity = config.vegetationDensity,
                rockDensity = config.rockDensity,
                decorDensity = config.decorDensity,
                activeCategoryCount = activeCats
            };
        }

        public static void Record(PlacementAttempt a)
        {
            attempts?.Add(a);
        }

        public static void Finalize(
            int totalAttempted, int totalPlaced,
            int skipChance, int skipSpacing, int skipSpawnZone, int skipOversize, int skipPrefabNull,
            Dictionary<string, int> placedPerCat, Dictionary<string, int> rejectedPerCat)
        {
            if (global == null) return;
            global.totalAttempted = totalAttempted;
            global.totalPlaced = totalPlaced;
            global.skipChance = skipChance;
            global.skipSpacing = skipSpacing;
            global.skipSpawnZone = skipSpawnZone;
            global.skipOversize = skipOversize;
            global.skipPrefabNull = skipPrefabNull;
            global.placedPerCategory = DictToString(placedPerCat);
            global.rejectedPerCategory = DictToString(rejectedPerCat);
        }

        static string DictToString(Dictionary<string, int> d)
        {
            if (d == null || d.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var kvp in d)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{kvp.Key}:{kvp.Value}");
            }
            return sb.ToString();
        }

        public static void Export()
        {
            if (global == null || attempts == null) return;
            Directory.CreateDirectory(DumpFolder);

            string jsonPath = Path.Combine(DumpFolder, "MapGenDebug_Latest.json");
            string csvPath = Path.Combine(DumpFolder, "MapGenDebug_Latest.csv");
            string txtPath = Path.Combine(DumpFolder, "MapGenDebug_Latest.txt");

            try
            {
                File.WriteAllText(jsonPath, BuildJson(), Encoding.UTF8);
                File.WriteAllText(csvPath, BuildCsv(), Encoding.UTF8);
                File.WriteAllText(txtPath, BuildTxt(), Encoding.UTF8);
                Debug.Log($"[PlacementDebugDump] Exported {attempts.Count} attempts to:\n  {jsonPath}\n  {csvPath}\n  {txtPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlacementDebugDump] Export failed: {e.Message}");
            }
        }

        // ════════════════════════════════════════
        //  JSON
        // ════════════════════════════════════════

        static string BuildJson()
        {
            var sb = new StringBuilder(attempts.Count * 512);
            sb.AppendLine("{");

            // Global
            sb.AppendLine("  \"global\": {");
            sb.AppendLine($"    \"seed\": {global.seed},");
            sb.AppendLine($"    \"mapWidth\": {global.mapWidth}, \"mapHeight\": {global.mapHeight},");
            sb.AppendLine($"    \"cellSize\": {F(global.cellSize)},");
            sb.AppendLine($"    \"spawnWorld\": [{F(global.spawnWorldX)}, {F(global.spawnWorldZ)}],");
            sb.AppendLine($"    \"exitWorld\": [{F(global.exitWorldX)}, {F(global.exitWorldZ)}],");
            sb.AppendLine($"    \"spawnCell\": [{global.spawnCellX}, {global.spawnCellY}],");
            sb.AppendLine($"    \"exitCell\": [{global.exitCellX}, {global.exitCellY}],");
            sb.AppendLine($"    \"densities\": {{ \"veg\": {F(global.vegDensity)}, \"rock\": {F(global.rockDensity)}, \"decor\": {F(global.decorDensity)} }},");
            sb.AppendLine($"    \"activeCategoryCount\": {global.activeCategoryCount},");
            sb.AppendLine($"    \"totalAttempted\": {global.totalAttempted}, \"totalPlaced\": {global.totalPlaced},");
            sb.AppendLine($"    \"skipChance\": {global.skipChance}, \"skipSpacing\": {global.skipSpacing},");
            sb.AppendLine($"    \"skipSpawnZone\": {global.skipSpawnZone}, \"skipOversize\": {global.skipOversize},");
            sb.AppendLine($"    \"skipPrefabNull\": {global.skipPrefabNull},");
            sb.AppendLine($"    \"placedPerCategory\": \"{Esc(global.placedPerCategory)}\",");
            sb.AppendLine($"    \"rejectedPerCategory\": \"{Esc(global.rejectedPerCategory)}\"");
            sb.AppendLine("  },");

            // Attempts
            sb.AppendLine("  \"attempts\": [");
            for (int i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                sb.Append("    {");
                sb.Append($"\"i\":{a.attemptIndex},\"status\":\"{a.finalStatus}\",\"cat\":\"{Esc(a.categoryId)}\",\"prefab\":\"{Esc(a.prefabName)}\",");
                sb.Append($"\"biome\":\"{Esc(a.biome)}\",\"cellType\":\"{Esc(a.supportCellType)}\",\"cx\":{a.cellX},\"cy\":{a.cellY},");
                sb.Append($"\"pos\":[{F(a.worldPosX)},{F(a.worldPosY)},{F(a.worldPosZ)}],");
                sb.Append($"\"rot\":[{F(a.rotEulerX)},{F(a.rotEulerY)},{F(a.rotEulerZ)}],");
                sb.Append($"\"scale\":[{F(a.scaleX)},{F(a.scaleY)},{F(a.scaleZ)}],");
                sb.Append($"\"dSpawn\":{F(a.distanceToSpawn)},\"dExit\":{F(a.distanceToExit)},\"yOff\":{F(a.yOffsetApplied)},");
                sb.Append($"\"bCenter\":[{F(a.boundsCenterX)},{F(a.boundsCenterY)},{F(a.boundsCenterZ)}],");
                sb.Append($"\"bSize\":[{F(a.boundsSizeX)},{F(a.boundsSizeY)},{F(a.boundsSizeZ)}],");
                sb.Append($"\"bMin\":[{F(a.boundsMinX)},{F(a.boundsMinY)},{F(a.boundsMinZ)}],");
                sb.Append($"\"bMax\":[{F(a.boundsMaxX)},{F(a.boundsMaxY)},{F(a.boundsMaxZ)}],");
                sb.Append($"\"maxDim\":{F(a.maxDimension)},\"rendCount\":{a.rendererCount},");
                sb.Append($"\"initScale\":[{F(a.initScaleX)},{F(a.initScaleY)},{F(a.initScaleZ)}],");
                sb.Append($"\"scaleAfterMult\":[{F(a.scaleAfterMultX)},{F(a.scaleAfterMultY)},{F(a.scaleAfterMultZ)}],");
                sb.Append($"\"scaleAfterClamp\":[{F(a.scaleAfterClampX)},{F(a.scaleAfterClampY)},{F(a.scaleAfterClampZ)}],");
                sb.Append($"\"clamped\":{(a.wasBoundsClamped ? "true" : "false")},\"clampRatio\":{F(a.clampRatio)},\"sizeCap\":{F(a.categorySizeCap)},");
                sb.Append($"\"fwd\":[{F(a.prefabForwardX)},{F(a.prefabForwardY)},{F(a.prefabForwardZ)}],");
                sb.Append($"\"up\":[{F(a.prefabUpX)},{F(a.prefabUpY)},{F(a.prefabUpZ)}],");
                sb.Append($"\"rPos\":[{F(a.firstRendPosX)},{F(a.firstRendPosY)},{F(a.firstRendPosZ)}],");
                sb.Append($"\"rRot\":[{F(a.firstRendRotX)},{F(a.firstRendRotY)},{F(a.firstRendRotZ)}],");
                sb.Append($"\"touchGnd\":{(a.estimatedTouchesGround ? "true" : "false")}");
                sb.Append(i < attempts.Count - 1 ? "},\n" : "}\n");
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        // ════════════════════════════════════════
        //  CSV
        // ════════════════════════════════════════

        static readonly string CsvHeader =
            "attemptIndex,finalStatus,categoryId,prefabName,biome,supportCellType,cellX,cellY," +
            "worldPosX,worldPosY,worldPosZ,rotEulerX,rotEulerY,rotEulerZ,scaleX,scaleY,scaleZ," +
            "supportCenterX,supportCenterY,supportCenterZ,yOffsetApplied,distanceToSpawn,distanceToExit," +
            "boundsCenterX,boundsCenterY,boundsCenterZ,boundsSizeX,boundsSizeY,boundsSizeZ," +
            "boundsMinX,boundsMinY,boundsMinZ,boundsMaxX,boundsMaxY,boundsMaxZ,maxDimension,rendererCount," +
            "initScaleX,initScaleY,initScaleZ,scaleAfterMultX,scaleAfterMultY,scaleAfterMultZ," +
            "scaleAfterClampX,scaleAfterClampY,scaleAfterClampZ,wasBoundsClamped,clampRatio,categorySizeCap," +
            "prefabForwardX,prefabForwardY,prefabForwardZ,prefabUpX,prefabUpY,prefabUpZ," +
            "firstRendPosX,firstRendPosY,firstRendPosZ,firstRendRotX,firstRendRotY,firstRendRotZ," +
            "estimatedTouchesGround";

        static string BuildCsv()
        {
            var sb = new StringBuilder(attempts.Count * 300);
            sb.AppendLine(CsvHeader);
            foreach (var a in attempts)
            {
                sb.Append($"{a.attemptIndex},{a.finalStatus},{Esc(a.categoryId)},{Esc(a.prefabName)},{Esc(a.biome)},{Esc(a.supportCellType)},{a.cellX},{a.cellY},");
                sb.Append($"{F(a.worldPosX)},{F(a.worldPosY)},{F(a.worldPosZ)},{F(a.rotEulerX)},{F(a.rotEulerY)},{F(a.rotEulerZ)},{F(a.scaleX)},{F(a.scaleY)},{F(a.scaleZ)},");
                sb.Append($"{F(a.supportCenterX)},{F(a.supportCenterY)},{F(a.supportCenterZ)},{F(a.yOffsetApplied)},{F(a.distanceToSpawn)},{F(a.distanceToExit)},");
                sb.Append($"{F(a.boundsCenterX)},{F(a.boundsCenterY)},{F(a.boundsCenterZ)},{F(a.boundsSizeX)},{F(a.boundsSizeY)},{F(a.boundsSizeZ)},");
                sb.Append($"{F(a.boundsMinX)},{F(a.boundsMinY)},{F(a.boundsMinZ)},{F(a.boundsMaxX)},{F(a.boundsMaxY)},{F(a.boundsMaxZ)},{F(a.maxDimension)},{a.rendererCount},");
                sb.Append($"{F(a.initScaleX)},{F(a.initScaleY)},{F(a.initScaleZ)},{F(a.scaleAfterMultX)},{F(a.scaleAfterMultY)},{F(a.scaleAfterMultZ)},");
                sb.Append($"{F(a.scaleAfterClampX)},{F(a.scaleAfterClampY)},{F(a.scaleAfterClampZ)},{(a.wasBoundsClamped ? 1 : 0)},{F(a.clampRatio)},{F(a.categorySizeCap)},");
                sb.Append($"{F(a.prefabForwardX)},{F(a.prefabForwardY)},{F(a.prefabForwardZ)},{F(a.prefabUpX)},{F(a.prefabUpY)},{F(a.prefabUpZ)},");
                sb.Append($"{F(a.firstRendPosX)},{F(a.firstRendPosY)},{F(a.firstRendPosZ)},{F(a.firstRendRotX)},{F(a.firstRendRotY)},{F(a.firstRendRotZ)},");
                sb.AppendLine($"{(a.estimatedTouchesGround ? 1 : 0)}");
            }
            return sb.ToString();
        }

        // ════════════════════════════════════════
        //  TXT
        // ════════════════════════════════════════

        static string BuildTxt()
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("  PLACEMENT DEBUG DUMP");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"Seed:       {global.seed}");
            sb.AppendLine($"Map:        {global.mapWidth}x{global.mapHeight} cellSize={F(global.cellSize)}");
            sb.AppendLine($"Spawn:      cell({global.spawnCellX},{global.spawnCellY}) world({F(global.spawnWorldX)},{F(global.spawnWorldZ)})");
            sb.AppendLine($"Exit:       cell({global.exitCellX},{global.exitCellY}) world({F(global.exitWorldX)},{F(global.exitWorldZ)})");
            sb.AppendLine($"Densities:  veg={F(global.vegDensity)} rock={F(global.rockDensity)} decor={F(global.decorDensity)}");
            sb.AppendLine($"Categories: {global.activeCategoryCount} active");
            sb.AppendLine();
            sb.AppendLine($"Attempted:  {global.totalAttempted}");
            sb.AppendLine($"Placed:     {global.totalPlaced}");
            sb.AppendLine($"Skip chance:    {global.skipChance}");
            sb.AppendLine($"Skip spacing:   {global.skipSpacing}");
            sb.AppendLine($"Skip spawnZone: {global.skipSpawnZone}");
            sb.AppendLine($"Skip oversize:  {global.skipOversize}");
            sb.AppendLine($"Skip prefabNull:{global.skipPrefabNull}");
            sb.AppendLine();
            sb.AppendLine($"Placed/cat:   {global.placedPerCategory}");
            sb.AppendLine($"Rejected/cat: {global.rejectedPerCategory}");
            sb.AppendLine();

            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("  PLACED ITEMS (sorted by maxDimension desc)");
            sb.AppendLine("-".PadRight(80, '-'));

            var placed = new List<PlacementAttempt>();
            foreach (var a in attempts)
                if (a.finalStatus == "placed") placed.Add(a);
            placed.Sort((a, b) => b.maxDimension.CompareTo(a.maxDimension));

            foreach (var a in placed)
            {
                sb.AppendLine($"  #{a.attemptIndex} {a.categoryId}/{a.prefabName} biome={a.biome} cell=({a.cellX},{a.cellY})");
                sb.AppendLine($"    pos=({F(a.worldPosX)},{F(a.worldPosY)},{F(a.worldPosZ)}) scale=({F(a.scaleX)},{F(a.scaleY)},{F(a.scaleZ)})");
                sb.AppendLine($"    bounds size=({F(a.boundsSizeX)},{F(a.boundsSizeY)},{F(a.boundsSizeZ)}) maxDim={F(a.maxDimension)} rend={a.rendererCount}");
                sb.AppendLine($"    dSpawn={F(a.distanceToSpawn)} dExit={F(a.distanceToExit)} clamped={a.wasBoundsClamped} clampR={F(a.clampRatio)} touchGnd={a.estimatedTouchesGround}");
            }

            sb.AppendLine();
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("  REJECTION SUMMARY");
            sb.AppendLine("-".PadRight(80, '-'));

            var rejByCatAndReason = new Dictionary<string, Dictionary<string, int>>();
            foreach (var a in attempts)
            {
                if (a.finalStatus == "placed") continue;
                string cat = a.categoryId ?? "?";
                if (!rejByCatAndReason.ContainsKey(cat))
                    rejByCatAndReason[cat] = new Dictionary<string, int>();
                var d = rejByCatAndReason[cat];
                if (!d.ContainsKey(a.finalStatus)) d[a.finalStatus] = 0;
                d[a.finalStatus]++;
            }
            foreach (var kvp in rejByCatAndReason)
            {
                sb.Append($"  {kvp.Key}: ");
                foreach (var r in kvp.Value)
                    sb.Append($"{r.Key}={r.Value} ");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static string F(float v) => v.ToString("F2", Inv);
        static string Esc(string s) => s ?? "";
    }
}
