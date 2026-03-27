using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    public class GenerationLogger
    {
        static readonly string LogFolder = Path.Combine(Application.dataPath, "..", "MapGenLogs");

        public static string WriteLog(MapData map, MapGenConfig config, GenerationResult result)
        {
            Directory.CreateDirectory(LogFolder);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string baseName = $"GenerationLog_{timestamp}_seed{result.seed}";

            string txtPath = Path.Combine(LogFolder, baseName + ".txt");
            string jsonPath = Path.Combine(LogFolder, baseName + ".json");

            File.WriteAllText(txtPath, BuildTextLog(map, config, result), Encoding.UTF8);
            File.WriteAllText(jsonPath, BuildJsonLog(map, config, result), Encoding.UTF8);

            Debug.Log($"[GenerationLogger] Logs écrits:\n  {txtPath}\n  {jsonPath}");
            return txtPath;
        }

        public static string WriteBatchReport(GenerationMetrics metrics, MapGenConfig config)
        {
            Directory.CreateDirectory(LogFolder);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string path = Path.Combine(LogFolder, $"BatchReport_{timestamp}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("=".PadRight(60, '='));
            sb.AppendLine("  RAPPORT DE BATCH TEST");
            sb.AppendLine("=".PadRight(60, '='));
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Map: {config.mapWidth}x{config.mapHeight}, Cellule: {config.cellSize}");
            sb.AppendLine($"Salles: {config.minRooms}-{config.maxRooms}, Mode: {config.mode}");
            sb.AppendLine();
            sb.AppendLine(metrics.BuildReport());
            sb.AppendLine();

            if (metrics.failedSeeds.Count > 0)
            {
                sb.AppendLine("--- Seeds en échec ---");
                foreach (var seed in metrics.failedSeeds)
                    sb.AppendLine($"  Seed: {seed}");
            }
            if (metrics.warningSeeds.Count > 0)
            {
                sb.AppendLine("--- Seeds avec warnings ---");
                foreach (var seed in metrics.warningSeeds)
                    sb.AppendLine($"  Seed: {seed}");
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[GenerationLogger] Rapport batch: {path}");
            return path;
        }

        static string BuildTextLog(MapData map, MapGenConfig config, GenerationResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=".PadRight(60, '='));
            sb.AppendLine("  LOG DE GÉNÉRATION PROCÉDURALE");
            sb.AppendLine("=".PadRight(60, '='));
            sb.AppendLine();

            // En-tête
            sb.AppendLine($"Date:       {result.timestamp}");
            sb.AppendLine($"Seed:       {result.seed}");
            sb.AppendLine($"Statut:     {result.status}");
            sb.AppendLine($"Temps:      {result.generationTimeMs:F2} ms");
            sb.AppendLine();

            // Paramètres
            sb.AppendLine("--- PARAMÈTRES ---");
            sb.AppendLine($"Taille map:       {config.mapWidth} x {config.mapHeight}");
            sb.AppendLine($"Taille cellule:   {config.cellSize}");
            sb.AppendLine($"Marge bord:       {config.borderMargin}");
            sb.AppendLine($"Salles:           {config.minRooms}-{config.maxRooms}");
            sb.AppendLine($"Taille salles:    {config.minRoomSize}-{config.maxRoomSize}");
            sb.AppendLine($"Largeur couloirs: {config.corridorWidth}");
            sb.AppendLine($"Densité végét.:   {config.vegetationDensity:P0}");
            sb.AppendLine($"Densité roches:   {config.rockDensity:P0}");
            sb.AppendLine($"Densité décor:    {config.decorDensity:P0}");
            sb.AppendLine($"Mode:             {config.mode}");
            sb.AppendLine($"Layout:           {config.layoutType}");
            sb.AppendLine($"Accessibilité:    {config.ensureAccessibility}");
            sb.AppendLine($"Dist min S→E:     {config.minSpawnToExitDistance}");
            sb.AppendLine($"Boss room:        {config.forceBossRoom}");
            sb.AppendLine($"Salle spéciale:   {config.forceSpecialRoom}");
            sb.AppendLine();

            // Catégories
            sb.AppendLine("--- CATÉGORIES ACTIVÉES ---");
            if (config.enabledCategories.Count == 0)
                sb.AppendLine("  (toutes)");
            else
                foreach (var cat in config.enabledCategories)
                    sb.AppendLine($"  [x] {cat}");
            sb.AppendLine();

            // Résultats
            sb.AppendLine("--- RÉSULTATS ---");
            sb.AppendLine($"Salles générées:        {result.roomCount}");
            sb.AppendLine($"Couloirs:               {result.corridorCount}");
            sb.AppendLine($"Cellules marchables:    {result.walkableCellCount}");
            sb.AppendLine($"Cellules mur:           {result.wallCellCount}");
            sb.AppendLine($"Cellules eau:           {result.waterCellCount}");
            sb.AppendLine($"Objets placés (total):  {result.totalObjectsPlaced}");
            sb.AppendLine($"Spawn:                  ({result.spawnCell.x}, {result.spawnCell.y})");
            sb.AppendLine($"Sortie:                 ({result.exitCell.x}, {result.exitCell.y})");
            sb.AppendLine($"Distance spawn→sortie:  {result.spawnToExitDistance:F1}");
            sb.AppendLine();

            // Objets par catégorie
            if (result.objectsPerCategory.Count > 0)
            {
                sb.AppendLine("--- OBJETS PAR CATÉGORIE ---");
                foreach (var kvp in result.objectsPerCategory)
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                sb.AppendLine();
            }

            // Pipeline
            sb.AppendLine("--- PIPELINE ---");
            foreach (var step in result.pipelineSteps)
                sb.AppendLine($"  {step}");
            sb.AppendLine();

            // Salles
            sb.AppendLine("--- DÉTAIL DES SALLES ---");
            foreach (var room in map.rooms)
            {
                sb.Append($"  Salle {room.id}: {room.bounds.width}x{room.bounds.height} " +
                         $"@({room.bounds.x},{room.bounds.y}) biome={room.biome}");
                if (room.isSpawnRoom) sb.Append(" [SPAWN]");
                if (room.isExitRoom) sb.Append(" [EXIT]");
                if (room.isBossRoom) sb.Append(" [BOSS]");
                if (room.isSpecialRoom) sb.Append(" [SPECIAL]");
                sb.Append($" connexions=[{string.Join(",", room.connectedRoomIds)}]");
                sb.AppendLine();
            }
            sb.AppendLine();

            // Validation
            sb.AppendLine("--- VALIDATION ---");
            sb.AppendLine($"Erreurs: {result.errorCount} | Warnings: {result.warningCount} | Infos: {result.infoCount}");
            foreach (var entry in result.validationEntries)
                sb.AppendLine($"  {entry}");
            sb.AppendLine();

            // Mini-carte ASCII
            sb.AppendLine("--- MINI-CARTE ---");
            sb.AppendLine(BuildAsciiMap(map));

            return sb.ToString();
        }

        static string BuildJsonLog(MapData map, MapGenConfig config, GenerationResult result)
        {
            var data = new JsonLogData
            {
                timestamp = result.timestamp,
                seed = result.seed,
                status = result.status.ToString(),
                generationTimeMs = result.generationTimeMs,
                config = new JsonConfigData
                {
                    mapWidth = config.mapWidth,
                    mapHeight = config.mapHeight,
                    cellSize = config.cellSize,
                    minRooms = config.minRooms,
                    maxRooms = config.maxRooms,
                    mode = config.mode.ToString(),
                    enabledCategories = config.enabledCategories
                },
                results = new JsonResultsData
                {
                    roomCount = result.roomCount,
                    corridorCount = result.corridorCount,
                    totalObjectsPlaced = result.totalObjectsPlaced,
                    walkableCells = result.walkableCellCount,
                    wallCells = result.wallCellCount,
                    spawnCell = $"{result.spawnCell.x},{result.spawnCell.y}",
                    exitCell = $"{result.exitCell.x},{result.exitCell.y}",
                    spawnToExitDistance = result.spawnToExitDistance
                },
                errors = result.errorCount,
                warnings = result.warningCount
            };

            data.validationEntries = new List<string>();
            foreach (var entry in result.validationEntries)
                data.validationEntries.Add(entry.ToString());

            data.pipeline = result.pipelineSteps;

            return JsonUtility.ToJson(data, true);
        }

        static string BuildAsciiMap(MapData map)
        {
            var sb = new StringBuilder();
            for (int y = map.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < map.width; x++)
                {
                    var cell = map.cells[x, y];
                    if (cell.isSpawnPoint) sb.Append('S');
                    else if (cell.isExit) sb.Append('E');
                    else
                    {
                        sb.Append(cell.type switch
                        {
                            CellType.Sol => '.',
                            CellType.Couloir => '+',
                            CellType.Mur => '#',
                            CellType.Eau => '~',
                            _ => ' '
                        });
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string GetLogFolder() => LogFolder;

        public static void OpenLogFolder()
        {
            Directory.CreateDirectory(LogFolder);
            Application.OpenURL("file:///" + LogFolder.Replace("\\", "/"));
        }

        [Serializable]
        class JsonLogData
        {
            public string timestamp;
            public int seed;
            public string status;
            public float generationTimeMs;
            public JsonConfigData config;
            public JsonResultsData results;
            public int errors;
            public int warnings;
            public List<string> validationEntries;
            public List<string> pipeline;
        }

        [Serializable]
        class JsonConfigData
        {
            public int mapWidth, mapHeight;
            public float cellSize;
            public int minRooms, maxRooms;
            public string mode;
            public List<string> enabledCategories;
        }

        [Serializable]
        class JsonResultsData
        {
            public int roomCount, corridorCount, totalObjectsPlaced;
            public int walkableCells, wallCells;
            public string spawnCell, exitCell;
            public float spawnToExitDistance;
        }
    }
}
