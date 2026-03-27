using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DonGeonMaster.MapGeneration
{
    public class MapGenerator
    {
        MapGenConfig config;
        System.Random rng;
        MapData map;
        GenerationResult result;

        // Noeud BSP pour la subdivision
        class BSPNode
        {
            public RectInt area;
            public BSPNode left, right;
            public Room room;
            public bool isLeaf => left == null && right == null;
        }

        public (MapData map, GenerationResult result) Generate(MapGenConfig config)
        {
            this.config = config;

            int seed = config.useRandomSeed && !config.lockSeed
                ? UnityEngine.Random.Range(int.MinValue, int.MaxValue)
                : config.seed;
            config.seed = seed;

            rng = new System.Random(seed);
            map = new MapData(config.mapWidth, config.mapHeight);
            result = new GenerationResult
            {
                seed = seed,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var sw = Stopwatch.StartNew();

            try
            {
                Step1_InitGrid();
                Step2_GenerateRooms();
                Step3_ConnectRooms();
                Step4_AssignBiomes();
                Step5_SetSpawnAndExit();
                Step6_FinalizeMap();
            }
            catch (Exception e)
            {
                result.AddPipelineStep($"ERREUR FATALE: {e.Message}");
                result.validationEntries.Add(new ValidationEntry(
                    ValidationSeverity.Erreur, "Pipeline", $"Exception: {e.Message}"));
                result.status = GenerationStatus.Echec;
                Debug.LogError($"[MapGenerator] Erreur fatale: {e}");
            }

            sw.Stop();
            result.generationTimeMs = (float)sw.Elapsed.TotalMilliseconds;
            result.roomCount = map.rooms.Count;
            result.corridorCount = map.corridors.Count;
            result.walkableCellCount = map.CountCells(CellType.Sol) + map.CountCells(CellType.Couloir);
            result.wallCellCount = map.CountCells(CellType.Mur);
            result.waterCellCount = map.CountCells(CellType.Eau);
            result.spawnCell = map.spawnCell;
            result.exitCell = map.exitCell;

            if (map.spawnCell.x >= 0 && map.exitCell.x >= 0)
                result.spawnToExitDistance = Vector2Int.Distance(map.spawnCell, map.exitCell) * config.cellSize;

            result.CountValidation();
            result.BuildSummary();
            return (map, result);
        }

        void Step1_InitGrid()
        {
            result.AddPipelineStep("Initialisation de la grille");
            for (int x = 0; x < config.mapWidth; x++)
                for (int y = 0; y < config.mapHeight; y++)
                    map.cells[x, y].type = CellType.Mur;
            result.AddPipelineStep($"Grille {config.mapWidth}x{config.mapHeight} initialisée (tout en Mur)");
        }

        void Step2_GenerateRooms()
        {
            result.AddPipelineStep("Génération des salles (BSP)");

            if (config.mode == GenerationMode.SalleUnique)
            {
                GenerateSingleRoom();
                return;
            }

            var root = new BSPNode
            {
                area = new RectInt(
                    config.borderMargin,
                    config.borderMargin,
                    config.mapWidth - config.borderMargin * 2,
                    config.mapHeight - config.borderMargin * 2)
            };

            int targetRooms = rng.Next(config.minRooms, config.maxRooms + 1);
            SplitBSP(root, 0, targetRooms);

            var leaves = new List<BSPNode>();
            CollectLeaves(root, leaves);

            int roomId = 0;
            foreach (var leaf in leaves)
            {
                if (roomId >= config.maxRooms) break;
                var room = CreateRoomInPartition(leaf, roomId);
                if (room != null)
                {
                    leaf.room = room;
                    map.rooms.Add(room);
                    CarveRoom(room);
                    roomId++;
                }
            }

            result.AddPipelineStep($"{map.rooms.Count} salles générées sur {targetRooms} ciblées");
            if (map.rooms.Count < config.minRooms)
            {
                result.validationEntries.Add(new ValidationEntry(
                    ValidationSeverity.Warning, "RoomCount",
                    $"Seulement {map.rooms.Count} salles (min: {config.minRooms})"));
            }
        }

        void GenerateSingleRoom()
        {
            int w = Mathf.Min(config.maxRoomSize, config.mapWidth - config.borderMargin * 2);
            int h = Mathf.Min(config.maxRoomSize, config.mapHeight - config.borderMargin * 2);
            int x = (config.mapWidth - w) / 2;
            int y = (config.mapHeight - h) / 2;
            var room = new Room(0, new RectInt(x, y, w, h));
            map.rooms.Add(room);
            CarveRoom(room);
            result.AddPipelineStep("Salle unique générée au centre");
        }

        void SplitBSP(BSPNode node, int depth, int targetRooms)
        {
            int minPartition = config.minRoomSize + 2;
            bool canSplitH = node.area.width >= minPartition * 2;
            bool canSplitV = node.area.height >= minPartition * 2;

            if (!canSplitH && !canSplitV) return;
            if (depth > 10) return;

            bool splitHorizontal;
            if (!canSplitH) splitHorizontal = false;
            else if (!canSplitV) splitHorizontal = true;
            else if (node.area.width > node.area.height * 1.3f) splitHorizontal = true;
            else if (node.area.height > node.area.width * 1.3f) splitHorizontal = false;
            else splitHorizontal = rng.Next(2) == 0;

            if (splitHorizontal)
            {
                int splitMin = node.area.x + minPartition;
                int splitMax = node.area.xMax - minPartition;
                if (splitMin > splitMax) return;
                int splitX = rng.Next(splitMin, splitMax + 1);

                node.left = new BSPNode
                {
                    area = new RectInt(node.area.x, node.area.y, splitX - node.area.x, node.area.height)
                };
                node.right = new BSPNode
                {
                    area = new RectInt(splitX, node.area.y, node.area.xMax - splitX, node.area.height)
                };
            }
            else
            {
                int splitMin = node.area.y + minPartition;
                int splitMax = node.area.yMax - minPartition;
                if (splitMin > splitMax) return;
                int splitY = rng.Next(splitMin, splitMax + 1);

                node.left = new BSPNode
                {
                    area = new RectInt(node.area.x, node.area.y, node.area.width, splitY - node.area.y)
                };
                node.right = new BSPNode
                {
                    area = new RectInt(node.area.x, splitY, node.area.width, node.area.yMax - splitY)
                };
            }

            SplitBSP(node.left, depth + 1, targetRooms);
            SplitBSP(node.right, depth + 1, targetRooms);
        }

        void CollectLeaves(BSPNode node, List<BSPNode> leaves)
        {
            if (node == null) return;
            if (node.isLeaf)
            {
                leaves.Add(node);
                return;
            }
            CollectLeaves(node.left, leaves);
            CollectLeaves(node.right, leaves);
        }

        Room CreateRoomInPartition(BSPNode leaf, int roomId)
        {
            int maxW = Mathf.Min(config.maxRoomSize, leaf.area.width - 2);
            int maxH = Mathf.Min(config.maxRoomSize, leaf.area.height - 2);
            if (maxW < config.minRoomSize || maxH < config.minRoomSize) return null;

            int w = rng.Next(config.minRoomSize, maxW + 1);
            int h = rng.Next(config.minRoomSize, maxH + 1);
            int x = leaf.area.x + rng.Next(1, leaf.area.width - w);
            int y = leaf.area.y + rng.Next(1, leaf.area.height - h);

            return new Room(roomId, new RectInt(x, y, w, h));
        }

        void CarveRoom(Room room)
        {
            for (int x = room.bounds.x; x < room.bounds.xMax; x++)
                for (int y = room.bounds.y; y < room.bounds.yMax; y++)
                {
                    map.SetCellType(x, y, CellType.Sol);
                    map.cells[x, y].roomId = room.id;
                }
        }

        void Step3_ConnectRooms()
        {
            result.AddPipelineStep("Connexion des salles (couloirs)");
            if (map.rooms.Count < 2)
            {
                result.AddPipelineStep("Pas assez de salles pour créer des couloirs");
                return;
            }

            // Connecter chaque salle à la plus proche non encore connectée
            var connected = new HashSet<int> { 0 };
            var unconnected = new HashSet<int>();
            for (int i = 1; i < map.rooms.Count; i++) unconnected.Add(i);

            while (unconnected.Count > 0)
            {
                int bestFrom = -1, bestTo = -1;
                float bestDist = float.MaxValue;

                foreach (int c in connected)
                {
                    foreach (int u in unconnected)
                    {
                        float dist = Vector2Int.Distance(map.rooms[c].center, map.rooms[u].center);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestFrom = c;
                            bestTo = u;
                        }
                    }
                }

                if (bestFrom < 0) break;
                CarveCorridor(map.rooms[bestFrom], map.rooms[bestTo]);
                connected.Add(bestTo);
                unconnected.Remove(bestTo);
            }

            result.AddPipelineStep($"{map.corridors.Count} couloirs créés");
        }

        void CarveCorridor(Room from, Room to)
        {
            var corridor = new Corridor { fromRoomId = from.id, toRoomId = to.id, width = config.corridorWidth };
            var start = from.center;
            var end = to.center;

            bool horizontalFirst = rng.Next(2) == 0;
            var current = start;

            if (horizontalFirst)
            {
                CarveHLine(current.x, end.x, current.y, corridor);
                current = new Vector2Int(end.x, current.y);
                CarveVLine(current.y, end.y, current.x, corridor);
            }
            else
            {
                CarveVLine(current.y, end.y, current.x, corridor);
                current = new Vector2Int(current.x, end.y);
                CarveHLine(current.x, end.x, current.y, corridor);
            }

            from.connectedRoomIds.Add(to.id);
            to.connectedRoomIds.Add(from.id);
            map.corridors.Add(corridor);
        }

        void CarveHLine(int x1, int x2, int y, Corridor corridor)
        {
            int halfW = config.corridorWidth / 2;
            int minX = Mathf.Min(x1, x2);
            int maxX = Mathf.Max(x1, x2);
            for (int x = minX; x <= maxX; x++)
            {
                for (int dy = -halfW; dy <= halfW; dy++)
                {
                    int cy = y + dy;
                    if (map.InBounds(x, cy) && map.cells[x, cy].type == CellType.Mur)
                    {
                        map.SetCellType(x, cy, CellType.Couloir);
                        corridor.cells.Add(new Vector2Int(x, cy));
                    }
                }
            }
        }

        void CarveVLine(int y1, int y2, int x, Corridor corridor)
        {
            int halfW = config.corridorWidth / 2;
            int minY = Mathf.Min(y1, y2);
            int maxY = Mathf.Max(y1, y2);
            for (int y = minY; y <= maxY; y++)
            {
                for (int dx = -halfW; dx <= halfW; dx++)
                {
                    int cx = x + dx;
                    if (map.InBounds(cx, y) && map.cells[cx, y].type == CellType.Mur)
                    {
                        map.SetCellType(cx, y, CellType.Couloir);
                        corridor.cells.Add(new Vector2Int(cx, y));
                    }
                }
            }
        }

        void Step4_AssignBiomes()
        {
            result.AddPipelineStep("Attribution des biomes");

            if (config.useForcedBiome)
            {
                for (int x = 0; x < config.mapWidth; x++)
                    for (int y = 0; y < config.mapHeight; y++)
                        map.cells[x, y].biome = config.forcedBiome;
                result.AddPipelineStep($"Biome forcé: {config.forcedBiome}");
                return;
            }

            float offsetX = rng.Next(0, 10000);
            float offsetY = rng.Next(0, 10000);
            float noiseScale = 0.08f;

            for (int x = 0; x < config.mapWidth; x++)
            {
                for (int y = 0; y < config.mapHeight; y++)
                {
                    float noise = Mathf.PerlinNoise(x * noiseScale + offsetX, y * noiseScale + offsetY);
                    map.cells[x, y].biome = NoiseToBiome(noise);
                }
            }

            // Force le biome cohérent au sein d'une salle
            foreach (var room in map.rooms)
            {
                BiomeType roomBiome = map.cells[room.center.x, room.center.y].biome;
                room.biome = roomBiome;
                for (int x = room.bounds.x; x < room.bounds.xMax; x++)
                    for (int y = room.bounds.y; y < room.bounds.yMax; y++)
                        if (map.InBounds(x, y))
                            map.cells[x, y].biome = roomBiome;
            }

            result.AddPipelineStep("Biomes assignés par bruit de Perlin + cohérence par salle");
        }

        BiomeType NoiseToBiome(float noise)
        {
            if (noise < 0.15f) return BiomeType.Marecage;
            if (noise < 0.35f) return BiomeType.Foret;
            if (noise < 0.50f) return BiomeType.Prairie;
            if (noise < 0.65f) return BiomeType.ForetAutomne;
            if (noise < 0.80f) return BiomeType.Rocailleux;
            if (noise < 0.90f) return BiomeType.Desert;
            return BiomeType.Fantaisie;
        }

        void Step5_SetSpawnAndExit()
        {
            result.AddPipelineStep("Placement du spawn et de la sortie");
            if (map.rooms.Count == 0)
            {
                result.validationEntries.Add(new ValidationEntry(
                    ValidationSeverity.Erreur, "SpawnExit", "Aucune salle pour placer spawn/exit"));
                return;
            }

            // Spawn = première salle
            var spawnRoom = map.rooms[0];
            spawnRoom.isSpawnRoom = true;
            map.spawnCell = spawnRoom.center;
            map.cells[spawnRoom.center.x, spawnRoom.center.y].isSpawnPoint = true;

            if (map.rooms.Count == 1)
            {
                map.exitCell = spawnRoom.center;
                return;
            }

            // Exit = salle la plus éloignée du spawn
            float maxDist = 0;
            Room exitRoom = map.rooms[1];
            for (int i = 1; i < map.rooms.Count; i++)
            {
                float dist = Vector2Int.Distance(spawnRoom.center, map.rooms[i].center);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    exitRoom = map.rooms[i];
                }
            }

            exitRoom.isExitRoom = true;
            map.exitCell = exitRoom.center;
            map.cells[exitRoom.center.x, exitRoom.center.y].isExit = true;

            // Vérifier la distance min spawn-exit
            float actualDist = maxDist * config.cellSize;
            if (actualDist < config.minSpawnToExitDistance)
            {
                result.validationEntries.Add(new ValidationEntry(
                    ValidationSeverity.Warning, "SpawnExitDistance",
                    $"Distance spawn-sortie ({actualDist:F1}) < minimum ({config.minSpawnToExitDistance})"));
            }

            // Boss room = deuxième plus éloignée si demandé
            if (config.forceBossRoom && map.rooms.Count >= 3)
            {
                float secondMax = 0;
                Room bossRoom = null;
                foreach (var room in map.rooms)
                {
                    if (room == spawnRoom || room == exitRoom) continue;
                    float dist = Vector2Int.Distance(spawnRoom.center, room.center);
                    if (dist > secondMax)
                    {
                        secondMax = dist;
                        bossRoom = room;
                    }
                }
                if (bossRoom != null) bossRoom.isBossRoom = true;
            }

            result.AddPipelineStep($"Spawn: ({map.spawnCell.x},{map.spawnCell.y}), " +
                                   $"Sortie: ({map.exitCell.x},{map.exitCell.y}), " +
                                   $"Distance: {actualDist:F1}");
        }

        void Step6_FinalizeMap()
        {
            result.AddPipelineStep("Finalisation de la map");

            // Marquer les cellules du bord comme Vide
            for (int x = 0; x < config.mapWidth; x++)
            {
                for (int m = 0; m < config.borderMargin; m++)
                {
                    map.SetCellType(x, m, CellType.Vide);
                    map.SetCellType(x, config.mapHeight - 1 - m, CellType.Vide);
                }
            }
            for (int y = 0; y < config.mapHeight; y++)
            {
                for (int m = 0; m < config.borderMargin; m++)
                {
                    map.SetCellType(m, y, CellType.Vide);
                    map.SetCellType(config.mapWidth - 1 - m, y, CellType.Vide);
                }
            }

            // Vérifier l'accessibilité
            if (config.ensureAccessibility && map.spawnCell.x >= 0)
            {
                var reachable = map.FloodFillWalkable(map.spawnCell);
                int totalWalkable = map.CountCells(CellType.Sol) + map.CountCells(CellType.Couloir);
                if (reachable.Count < totalWalkable)
                {
                    int unreachable = totalWalkable - reachable.Count;
                    result.validationEntries.Add(new ValidationEntry(
                        ValidationSeverity.Warning, "Accessibilité",
                        $"{unreachable} cellules marchables inaccessibles depuis le spawn"));
                }
            }

            result.AddPipelineStep("Map finalisée");
        }
    }
}
