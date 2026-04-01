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
        public string surfaceShape;
        public int cellX, cellY;
        public float cellFloorHeight;

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

        // Support visuel du sol
        public string supportRenderMode;   // "blockout" ou "realGround"
        public string supportVisualType;   // "RealFloor_Stone", "RealCorridor_Wood", "DebugFloor", etc.
        public string supportObjectName;   // "Floor_12_8" etc.
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
        public string placedPerCategory;
        public string rejectedPerCategory;

        // Rendu sol
        public bool realGroundEnabled;
        public int realGroundFloorCells;
        public int realGroundCorridorCells;
        public int blockoutCells;

        // Collision ground
        public int collisionCellsTotal;
        public int collisionCellsFloor;
        public int collisionCellsCorridor;
        public float collisionGroundY;
        public float collisionGroundThickness;

        // Hero locomotion
        public float heroStepOffset;
        public float heroSlopeLimit;
        public float heroHeight;
        public float heroRadius;
        public float heroSkinWidth;
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
        static List<DebugTools.MapStructureDebugRenderer.CellRenderInfo> tileInfos;
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

        public static void SetGroundRenderInfo(bool realGroundEnabled, int realFloor, int realCorridor, int blockout,
            List<DebugTools.MapStructureDebugRenderer.CellRenderInfo> tiles = null)
        {
            if (global == null) return;
            global.realGroundEnabled = realGroundEnabled;
            global.realGroundFloorCells = realFloor;
            global.realGroundCorridorCells = realCorridor;
            global.blockoutCells = blockout;
            tileInfos = tiles;
        }

        public static void SetHeroLocomotion(float stepOffset, float slopeLimit, float height, float radius, float skinWidth)
        {
            if (global == null) return;
            global.heroStepOffset = stepOffset;
            global.heroSlopeLimit = slopeLimit;
            global.heroHeight = height;
            global.heroRadius = radius;
            global.heroSkinWidth = skinWidth;
        }

        public static void SetCollisionInfo(int total, int floor, int corridor, float groundY, float thickness)
        {
            if (global == null) return;
            global.collisionCellsTotal = total;
            global.collisionCellsFloor = floor;
            global.collisionCellsCorridor = corridor;
            global.collisionGroundY = groundY;
            global.collisionGroundThickness = thickness;
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
            sb.AppendLine($"    \"rejectedPerCategory\": \"{Esc(global.rejectedPerCategory)}\",");
            sb.AppendLine($"    \"realGroundEnabled\": {(global.realGroundEnabled ? "true" : "false")},");
            sb.AppendLine($"    \"realGroundFloorCells\": {global.realGroundFloorCells},");
            sb.AppendLine($"    \"realGroundCorridorCells\": {global.realGroundCorridorCells},");
            sb.AppendLine($"    \"blockoutCells\": {global.blockoutCells},");
            sb.AppendLine($"    \"collisionCellsTotal\": {global.collisionCellsTotal},");
            sb.AppendLine($"    \"collisionCellsFloor\": {global.collisionCellsFloor},");
            sb.AppendLine($"    \"collisionCellsCorridor\": {global.collisionCellsCorridor},");
            sb.AppendLine($"    \"collisionGroundY\": {F(global.collisionGroundY)},");
            sb.AppendLine($"    \"collisionGroundThickness\": {F(global.collisionGroundThickness)},");
            sb.AppendLine($"    \"heroStepOffset\": {F(global.heroStepOffset)},");
            sb.AppendLine($"    \"heroSlopeLimit\": {F(global.heroSlopeLimit)},");
            sb.AppendLine($"    \"heroHeight\": {F(global.heroHeight)},");
            sb.AppendLine($"    \"heroRadius\": {F(global.heroRadius)},");
            sb.AppendLine($"    \"heroSkinWidth\": {F(global.heroSkinWidth)}");
            sb.AppendLine("  },");

            // Attempts
            sb.AppendLine("  \"attempts\": [");
            for (int i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                sb.Append("    {");
                sb.Append($"\"i\":{a.attemptIndex},\"status\":\"{a.finalStatus}\",\"cat\":\"{Esc(a.categoryId)}\",\"prefab\":\"{Esc(a.prefabName)}\",");
                sb.Append($"\"biome\":\"{Esc(a.biome)}\",\"cellType\":\"{Esc(a.supportCellType)}\",\"surface\":\"{Esc(a.surfaceShape)}\",\"cx\":{a.cellX},\"cy\":{a.cellY},\"floorH\":{F(a.cellFloorHeight)},");
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
                sb.Append($"\"touchGnd\":{(a.estimatedTouchesGround ? "true" : "false")},");
                sb.Append($"\"supMode\":\"{Esc(a.supportRenderMode)}\",\"supType\":\"{Esc(a.supportVisualType)}\",\"supObj\":\"{Esc(a.supportObjectName)}\"");
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
            "attemptIndex,finalStatus,categoryId,prefabName,biome,supportCellType,surfaceShape,cellX,cellY,cellFloorHeight," +
            "worldPosX,worldPosY,worldPosZ,rotEulerX,rotEulerY,rotEulerZ,scaleX,scaleY,scaleZ," +
            "supportCenterX,supportCenterY,supportCenterZ,yOffsetApplied,distanceToSpawn,distanceToExit," +
            "boundsCenterX,boundsCenterY,boundsCenterZ,boundsSizeX,boundsSizeY,boundsSizeZ," +
            "boundsMinX,boundsMinY,boundsMinZ,boundsMaxX,boundsMaxY,boundsMaxZ,maxDimension,rendererCount," +
            "initScaleX,initScaleY,initScaleZ,scaleAfterMultX,scaleAfterMultY,scaleAfterMultZ," +
            "scaleAfterClampX,scaleAfterClampY,scaleAfterClampZ,wasBoundsClamped,clampRatio,categorySizeCap," +
            "prefabForwardX,prefabForwardY,prefabForwardZ,prefabUpX,prefabUpY,prefabUpZ," +
            "firstRendPosX,firstRendPosY,firstRendPosZ,firstRendRotX,firstRendRotY,firstRendRotZ," +
            "estimatedTouchesGround,supportRenderMode,supportVisualType,supportObjectName";

        static string BuildCsv()
        {
            var sb = new StringBuilder(attempts.Count * 300);
            sb.AppendLine(CsvHeader);
            foreach (var a in attempts)
            {
                sb.Append($"{a.attemptIndex},{a.finalStatus},{Esc(a.categoryId)},{Esc(a.prefabName)},{Esc(a.biome)},{Esc(a.supportCellType)},{Esc(a.surfaceShape)},{a.cellX},{a.cellY},{F(a.cellFloorHeight)},");
                sb.Append($"{F(a.worldPosX)},{F(a.worldPosY)},{F(a.worldPosZ)},{F(a.rotEulerX)},{F(a.rotEulerY)},{F(a.rotEulerZ)},{F(a.scaleX)},{F(a.scaleY)},{F(a.scaleZ)},");
                sb.Append($"{F(a.supportCenterX)},{F(a.supportCenterY)},{F(a.supportCenterZ)},{F(a.yOffsetApplied)},{F(a.distanceToSpawn)},{F(a.distanceToExit)},");
                sb.Append($"{F(a.boundsCenterX)},{F(a.boundsCenterY)},{F(a.boundsCenterZ)},{F(a.boundsSizeX)},{F(a.boundsSizeY)},{F(a.boundsSizeZ)},");
                sb.Append($"{F(a.boundsMinX)},{F(a.boundsMinY)},{F(a.boundsMinZ)},{F(a.boundsMaxX)},{F(a.boundsMaxY)},{F(a.boundsMaxZ)},{F(a.maxDimension)},{a.rendererCount},");
                sb.Append($"{F(a.initScaleX)},{F(a.initScaleY)},{F(a.initScaleZ)},{F(a.scaleAfterMultX)},{F(a.scaleAfterMultY)},{F(a.scaleAfterMultZ)},");
                sb.Append($"{F(a.scaleAfterClampX)},{F(a.scaleAfterClampY)},{F(a.scaleAfterClampZ)},{(a.wasBoundsClamped ? 1 : 0)},{F(a.clampRatio)},{F(a.categorySizeCap)},");
                sb.Append($"{F(a.prefabForwardX)},{F(a.prefabForwardY)},{F(a.prefabForwardZ)},{F(a.prefabUpX)},{F(a.prefabUpY)},{F(a.prefabUpZ)},");
                sb.Append($"{F(a.firstRendPosX)},{F(a.firstRendPosY)},{F(a.firstRendPosZ)},{F(a.firstRendRotX)},{F(a.firstRendRotY)},{F(a.firstRendRotZ)},");
                sb.AppendLine($"{(a.estimatedTouchesGround ? 1 : 0)},{Esc(a.supportRenderMode)},{Esc(a.supportVisualType)},{Esc(a.supportObjectName)}");
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
            sb.AppendLine($"Ground render: {(global.realGroundEnabled ? "SOLS REELS" : "BLOCKOUT")}");
            sb.AppendLine($"  Real floor cells:    {global.realGroundFloorCells}");
            sb.AppendLine($"  Real corridor cells: {global.realGroundCorridorCells}");
            sb.AppendLine($"  Blockout cells:      {global.blockoutCells}");
            sb.AppendLine();
            sb.AppendLine($"Collision ground:");
            sb.AppendLine($"  Total cells:     {global.collisionCellsTotal}");
            sb.AppendLine($"  Sol cells:       {global.collisionCellsFloor}");
            sb.AppendLine($"  Couloir cells:   {global.collisionCellsCorridor}");
            sb.AppendLine($"  Ground Y:        {F(global.collisionGroundY)}");
            sb.AppendLine($"  Thickness:       {F(global.collisionGroundThickness)}");
            sb.AppendLine();
            sb.AppendLine($"Hero locomotion:");
            sb.AppendLine($"  stepOffset:  {F(global.heroStepOffset)}");
            sb.AppendLine($"  slopeLimit:  {F(global.heroSlopeLimit)}");
            sb.AppendLine($"  height:      {F(global.heroHeight)}");
            sb.AppendLine($"  radius:      {F(global.heroRadius)}");
            sb.AppendLine($"  skinWidth:   {F(global.heroSkinWidth)}");
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

            // ── TILE DEBUG ──
            if (tileInfos != null && tileInfos.Count > 0)
            {
                var realTiles = new List<DebugTools.MapStructureDebugRenderer.CellRenderInfo>();
                foreach (var t in tileInfos)
                    if (t.renderMode == "realGround_prefab") realTiles.Add(t);

                if (realTiles.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("-".PadRight(80, '-'));
                    sb.AppendLine("  TILE DE SOL — DIAGNOSTIC DETAILLE");
                    sb.AppendLine("-".PadRight(80, '-'));
                    sb.AppendLine($"  Total tiles reelles: {realTiles.Count}");

                    int strips = 0, covers = 0;
                    var prefabFreq = new Dictionary<string, int>();
                    var meshFreq = new Dictionary<string, int>();

                    foreach (var t in realTiles)
                    {
                        if (t.looksLikeStrip) strips++;
                        if (t.coversCellProperly) covers++;
                        string pn = t.prefabName ?? "?";
                        if (!prefabFreq.ContainsKey(pn)) prefabFreq[pn] = 0; prefabFreq[pn]++;
                        string mn = t.meshName ?? "?";
                        if (!meshFreq.ContainsKey(mn)) meshFreq[mn] = 0; meshFreq[mn]++;
                    }

                    sb.AppendLine($"  Strips (ratio>2): {strips}/{realTiles.Count}");
                    sb.AppendLine($"  Couvrent la cellule (>90%): {covers}/{realTiles.Count}");
                    sb.AppendLine();

                    // Prefab frequency
                    sb.AppendLine("  Prefabs utilises:");
                    foreach (var kvp in prefabFreq) sb.AppendLine($"    {kvp.Key}: {kvp.Value}");
                    sb.AppendLine();

                    // Mesh frequency
                    sb.AppendLine("  Meshes utilises:");
                    foreach (var kvp in meshFreq) sb.AppendLine($"    {kvp.Key}: {kvp.Value}");
                    sb.AppendLine();

                    // Top 20 plus suspectes (par ratio aspect)
                    realTiles.Sort((a, b) => b.aspectRatio.CompareTo(a.aspectRatio));
                    // Frequence par axe up
                    var upFreq = new Dictionary<string, int>();
                    foreach (var t in realTiles)
                    {
                        string ax = t.chosenUpAxis ?? "?";
                        if (!upFreq.ContainsKey(ax)) upFreq[ax] = 0; upFreq[ax]++;
                    }
                    sb.AppendLine("  Axe up detecte:");
                    foreach (var kvp in upFreq) sb.AppendLine($"    {kvp.Key}: {kvp.Value}");
                    sb.AppendLine();

                    sb.AppendLine("  Top 20 tiles aspect ratio le plus extreme:");
                    int count = 0;
                    foreach (var t in realTiles)
                    {
                        if (count >= 20) break;
                        sb.AppendLine($"    ({t.x},{t.y}) {t.prefabName} ratio={F(t.aspectRatio)} upAxis={t.chosenUpAxis} rot={t.appliedRotation} " +
                            $"rawBounds=({F(t.rawBoundsSize.x)},{F(t.rawBoundsSize.y)},{F(t.rawBoundsSize.z)}) " +
                            $"sf=({F(t.scaleFactorX)},{F(t.scaleFactorY)},{F(t.scaleFactorZ)}) " +
                            $"final=({F(t.finalBoundsSize.x)},{F(t.finalBoundsSize.y)},{F(t.finalBoundsSize.z)}) " +
                            $"strip={t.looksLikeStrip} covers={t.coversCellProperly}");
                        count++;
                    }
                    sb.AppendLine();

                    // Top 20 par plus petit footprintD
                    realTiles.Sort((a, b) => a.footprintD.CompareTo(b.footprintD));
                    sb.AppendLine("  Top 20 tiles plus petit footprintDepth:");
                    count = 0;
                    foreach (var t in realTiles)
                    {
                        if (count >= 20) break;
                        sb.AppendLine($"    ({t.x},{t.y}) {t.prefabName} fpW={F(t.footprintW)} fpD={F(t.footprintD)} fpH={F(t.footprintH)} covers={t.coversCellProperly}");
                        count++;
                    }
                }
            }

            return sb.ToString();
        }

        static string F(float v) => v.ToString("F2", Inv);
        static string Esc(string s) => s ?? "";
    }
}
