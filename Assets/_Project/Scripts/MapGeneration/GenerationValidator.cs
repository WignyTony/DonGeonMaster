using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    public class GenerationValidator
    {
        public List<ValidationEntry> Validate(MapData map, MapGenConfig config, GenerationResult result)
        {
            var entries = new List<ValidationEntry>();

            ValidateSpawn(map, config, entries);
            ValidateConnectivity(map, entries);
            ValidateRoomSizes(map, config, entries);
            ValidateBounds(map, config, entries);
            ValidateIsolatedRooms(map, entries);
            ValidateEmptyRooms(map, entries);
            ValidateCategoryExclusions(map, config, entries);
            ValidateSpawnToExitPath(map, config, entries);
            ValidateOverlaps(map, entries);
            ValidateMandatoryRooms(map, config, entries);

            result.validationEntries.AddRange(entries);
            result.CountValidation();

            string statusStr = result.status switch
            {
                GenerationStatus.Succes => "SUCCES",
                GenerationStatus.SuccesAvecWarnings => "SUCCES (warnings)",
                _ => "ECHEC"
            };
            result.AddPipelineStep($"Validation terminée: {statusStr} " +
                                   $"({result.errorCount}E / {result.warningCount}W / {result.infoCount}I)");
            return entries;
        }

        void ValidateSpawn(MapData map, MapGenConfig config, List<ValidationEntry> entries)
        {
            if (map.spawnCell.x < 0 || map.spawnCell.y < 0)
            {
                entries.Add(new ValidationEntry(ValidationSeverity.Erreur,
                    "SpawnValide", "Aucun point de spawn défini"));
                return;
            }

            var cell = map.GetCell(map.spawnCell);
            if (cell == null || !cell.IsWalkable)
            {
                entries.Add(new ValidationEntry(ValidationSeverity.Erreur,
                    "SpawnValide", "Le spawn est sur une cellule non marchable", map.spawnCell));
            }
            else
            {
                entries.Add(new ValidationEntry(ValidationSeverity.Info,
                    "SpawnValide", $"Spawn valide à ({map.spawnCell.x},{map.spawnCell.y})", map.spawnCell));
            }
        }

        void ValidateConnectivity(MapData map, List<ValidationEntry> entries)
        {
            if (map.spawnCell.x < 0) return;
            var reachable = map.FloodFillWalkable(map.spawnCell);
            var allWalkable = map.GetAllWalkableCells();

            if (reachable.Count == allWalkable.Count)
            {
                entries.Add(new ValidationEntry(ValidationSeverity.Info,
                    "Connectivité", $"Map entièrement connectée ({reachable.Count} cellules)"));
            }
            else
            {
                int isolated = allWalkable.Count - reachable.Count;
                entries.Add(new ValidationEntry(ValidationSeverity.Warning,
                    "Connectivité", $"{isolated}/{allWalkable.Count} cellules marchables inaccessibles"));

                // Identifier les zones isolées
                var unvisited = new HashSet<Vector2Int>(allWalkable);
                unvisited.ExceptWith(reachable);
                int zoneCount = 0;
                while (unvisited.Count > 0)
                {
                    var start = default(Vector2Int);
                    foreach (var v in unvisited) { start = v; break; }
                    var zone = map.FloodFillWalkable(start);
                    if (zone.Count == 0)
                    {
                        unvisited.Remove(start);
                        continue;
                    }
                    unvisited.ExceptWith(zone);
                    zoneCount++;
                    entries.Add(new ValidationEntry(ValidationSeverity.Warning,
                        "ZoneIsolée", $"Zone isolée #{zoneCount}: {zone.Count} cellules", start));
                }
            }
        }

        void ValidateRoomSizes(MapData map, MapGenConfig config, List<ValidationEntry> entries)
        {
            foreach (var room in map.rooms)
            {
                if (room.bounds.width < config.minRoomSize || room.bounds.height < config.minRoomSize)
                {
                    entries.Add(new ValidationEntry(ValidationSeverity.Warning,
                        "TailleSalle", $"Salle {room.id} trop petite: {room.bounds.width}x{room.bounds.height}",
                        room.center));
                }
            }
        }

        void ValidateBounds(MapData map, MapGenConfig config, List<ValidationEntry> entries)
        {
            foreach (var room in map.rooms)
            {
                if (room.bounds.x < config.borderMargin || room.bounds.y < config.borderMargin ||
                    room.bounds.xMax > config.mapWidth - config.borderMargin ||
                    room.bounds.yMax > config.mapHeight - config.borderMargin)
                {
                    entries.Add(new ValidationEntry(ValidationSeverity.Warning,
                        "HorsLimites", $"Salle {room.id} hors marge de sécurité", room.center));
                }
            }
        }

        void ValidateIsolatedRooms(MapData map, List<ValidationEntry> entries)
        {
            foreach (var room in map.rooms)
            {
                if (room.connectedRoomIds.Count == 0 && map.rooms.Count > 1)
                {
                    entries.Add(new ValidationEntry(ValidationSeverity.Warning,
                        "SalleIsolée", $"Salle {room.id} n'a aucune connexion", room.center));
                }
            }
        }

        void ValidateEmptyRooms(MapData map, List<ValidationEntry> entries)
        {
            foreach (var room in map.rooms)
            {
                bool hasContent = false;
                for (int x = room.bounds.x; x < room.bounds.xMax && !hasContent; x++)
                    for (int y = room.bounds.y; y < room.bounds.yMax && !hasContent; y++)
                        if (map.InBounds(x, y) && map.cells[x, y].placedObjects.Count > 0)
                            hasContent = true;

                if (!hasContent)
                {
                    entries.Add(new ValidationEntry(ValidationSeverity.Info,
                        "SalleVide", $"Salle {room.id} ne contient aucun objet placé", room.center));
                }
            }
        }

        void ValidateCategoryExclusions(MapData map, MapGenConfig config, List<ValidationEntry> entries)
        {
            // Vérifier que les catégories désactivées n'ont pas d'objets placés
            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    foreach (var cat in map.cells[x, y].placedAssetCategories)
                    {
                        if (config.enabledCategories.Count > 0 && !config.enabledCategories.Contains(cat))
                        {
                            entries.Add(new ValidationEntry(ValidationSeverity.Erreur,
                                "CatégorieExclue",
                                $"Objet de catégorie désactivée '{cat}' trouvé",
                                new Vector2Int(x, y)));
                        }
                    }
                }
            }
        }

        void ValidateSpawnToExitPath(MapData map, MapGenConfig config, List<ValidationEntry> entries)
        {
            if (map.spawnCell.x < 0 || map.exitCell.x < 0) return;

            var path = map.FindPath(map.spawnCell, map.exitCell);
            if (path == null)
            {
                entries.Add(new ValidationEntry(ValidationSeverity.Erreur,
                    "CheminSpawnExit", "Aucun chemin entre spawn et sortie"));
            }
            else
            {
                float pathLength = path.Count * config.cellSize;
                entries.Add(new ValidationEntry(ValidationSeverity.Info,
                    "CheminSpawnExit",
                    $"Chemin trouvé: {path.Count} cellules ({pathLength:F0} unités)"));

                if (pathLength < config.minSpawnToExitDistance)
                {
                    entries.Add(new ValidationEntry(ValidationSeverity.Warning,
                        "DistanceMinSpawnExit",
                        $"Chemin trop court ({pathLength:F0} < {config.minSpawnToExitDistance})"));
                }
            }
        }

        void ValidateOverlaps(MapData map, List<ValidationEntry> entries)
        {
            // Vérifier les salles qui se chevauchent
            for (int i = 0; i < map.rooms.Count; i++)
            {
                for (int j = i + 1; j < map.rooms.Count; j++)
                {
                    var a = map.rooms[i].bounds;
                    var b = map.rooms[j].bounds;
                    if (a.Overlaps(b))
                    {
                        entries.Add(new ValidationEntry(ValidationSeverity.Warning,
                            "Chevauchement",
                            $"Salles {i} et {j} se chevauchent"));
                    }
                }
            }
        }

        void ValidateMandatoryRooms(MapData map, MapGenConfig config, List<ValidationEntry> entries)
        {
            if (config.forceStartRoom && !map.rooms.Exists(r => r.isSpawnRoom))
            {
                entries.Add(new ValidationEntry(ValidationSeverity.Erreur,
                    "SalleObligatoire", "Pas de salle de spawn marquée"));
            }
            if (config.forceExitRoom && !map.rooms.Exists(r => r.isExitRoom))
            {
                entries.Add(new ValidationEntry(ValidationSeverity.Erreur,
                    "SalleObligatoire", "Pas de salle de sortie marquée"));
            }
            if (config.forceBossRoom && !map.rooms.Exists(r => r.isBossRoom))
            {
                entries.Add(new ValidationEntry(ValidationSeverity.Warning,
                    "SalleObligatoire", "Pas de salle de boss (pas assez de salles ?)"));
            }
        }
    }
}
