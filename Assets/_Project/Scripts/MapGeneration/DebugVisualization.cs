using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    public class DebugVisualization : MonoBehaviour
    {
        [Header("Affichage")]
        [SerializeField] bool showGrid = true;
        [SerializeField] bool showRooms = true;
        [SerializeField] bool showCorridors = true;
        [SerializeField] bool showSpawnExit = true;
        [SerializeField] bool showConnections = true;
        [SerializeField] bool showCellTypes = true;
        [SerializeField] bool showBiomes;
        [SerializeField] bool showValidationErrors = true;

        MapData map;
        MapGenConfig config;
        GenerationResult result;
        bool hasData;

        static readonly Color RoomColor = new(0.2f, 0.7f, 0.3f, 0.15f);
        static readonly Color CorridorColor = new(0.3f, 0.5f, 0.8f, 0.15f);
        static readonly Color WallColor = new(0.5f, 0.3f, 0.2f, 0.08f);
        static readonly Color WaterColor = new(0.2f, 0.4f, 0.9f, 0.2f);
        static readonly Color SpawnColor = new(0f, 1f, 0f, 0.8f);
        static readonly Color ExitColor = new(1f, 0.2f, 0.2f, 0.8f);
        static readonly Color ConnectionColor = new(1f, 1f, 0f, 0.5f);
        static readonly Color ErrorColor = new(1f, 0f, 0f, 0.8f);
        static readonly Color WarningColor = new(1f, 0.8f, 0f, 0.6f);
        static readonly Color GridColor = new(0.3f, 0.3f, 0.3f, 0.2f);

        static readonly Dictionary<BiomeType, Color> BiomeColors = new()
        {
            { BiomeType.Foret, new Color(0.1f, 0.5f, 0.15f, 0.2f) },
            { BiomeType.ForetAutomne, new Color(0.7f, 0.4f, 0.1f, 0.2f) },
            { BiomeType.ForetHiver, new Color(0.7f, 0.8f, 0.9f, 0.2f) },
            { BiomeType.Prairie, new Color(0.4f, 0.7f, 0.2f, 0.2f) },
            { BiomeType.Desert, new Color(0.8f, 0.7f, 0.3f, 0.2f) },
            { BiomeType.Marecage, new Color(0.3f, 0.4f, 0.2f, 0.2f) },
            { BiomeType.Rocailleux, new Color(0.5f, 0.5f, 0.5f, 0.2f) },
            { BiomeType.Fantaisie, new Color(0.6f, 0.2f, 0.7f, 0.2f) }
        };

        public void SetData(MapData map, MapGenConfig config, GenerationResult result)
        {
            this.map = map;
            this.config = config;
            this.result = result;
            hasData = true;
        }

        public void ClearData()
        {
            map = null;
            config = null;
            result = null;
            hasData = false;
        }

        void OnDrawGizmos()
        {
            if (!hasData || map == null || config == null) return;
            float cs = config.cellSize;

            // Grille
            if (showGrid)
            {
                Gizmos.color = GridColor;
                for (int x = 0; x <= map.width; x++)
                {
                    Gizmos.DrawLine(
                        new Vector3(x * cs, 0.1f, 0),
                        new Vector3(x * cs, 0.1f, map.height * cs));
                }
                for (int y = 0; y <= map.height; y++)
                {
                    Gizmos.DrawLine(
                        new Vector3(0, 0.1f, y * cs),
                        new Vector3(map.width * cs, 0.1f, y * cs));
                }

                // Bornes de la map
                Gizmos.color = Color.white;
                var bounds = new Vector3(map.width * cs, 1, map.height * cs);
                Gizmos.DrawWireCube(bounds * 0.5f, bounds);
            }

            // Cellules par type
            if (showCellTypes && !showBiomes)
            {
                for (int x = 0; x < map.width; x++)
                {
                    for (int y = 0; y < map.height; y++)
                    {
                        var cell = map.cells[x, y];
                        Color color = cell.type switch
                        {
                            CellType.Sol => RoomColor,
                            CellType.Couloir => CorridorColor,
                            CellType.Mur => WallColor,
                            CellType.Eau => WaterColor,
                            _ => Color.clear
                        };
                        if (color.a < 0.01f) continue;
                        Gizmos.color = color;
                        Vector3 pos = new Vector3((x + 0.5f) * cs, 0.05f, (y + 0.5f) * cs);
                        Gizmos.DrawCube(pos, new Vector3(cs * 0.95f, 0.1f, cs * 0.95f));
                    }
                }
            }

            // Biomes
            if (showBiomes)
            {
                for (int x = 0; x < map.width; x++)
                {
                    for (int y = 0; y < map.height; y++)
                    {
                        var cell = map.cells[x, y];
                        if (cell.type == CellType.Vide) continue;
                        if (BiomeColors.TryGetValue(cell.biome, out Color bColor))
                        {
                            Gizmos.color = bColor;
                            Vector3 pos = new Vector3((x + 0.5f) * cs, 0.05f, (y + 0.5f) * cs);
                            Gizmos.DrawCube(pos, new Vector3(cs * 0.95f, 0.1f, cs * 0.95f));
                        }
                    }
                }
            }

            // Contour des salles
            if (showRooms)
            {
                foreach (var room in map.rooms)
                {
                    Gizmos.color = Color.green;
                    Vector3 rCenter = new Vector3(
                        (room.bounds.x + room.bounds.width * 0.5f) * cs,
                        0.2f,
                        (room.bounds.y + room.bounds.height * 0.5f) * cs);
                    Vector3 rSize = new Vector3(room.bounds.width * cs, 0.4f, room.bounds.height * cs);
                    Gizmos.DrawWireCube(rCenter, rSize);

                    // Numéro de la salle
#if UNITY_EDITOR
                    string label = $"R{room.id}";
                    if (room.isSpawnRoom) label += " [S]";
                    if (room.isExitRoom) label += " [E]";
                    if (room.isBossRoom) label += " [B]";
                    UnityEditor.Handles.Label(rCenter + Vector3.up * 2, label,
                        new GUIStyle { normal = { textColor = Color.white }, fontSize = 14, fontStyle = FontStyle.Bold });
#endif
                }
            }

            // Connexions entre salles
            if (showConnections)
            {
                Gizmos.color = ConnectionColor;
                foreach (var corridor in map.corridors)
                {
                    var fromRoom = map.GetRoom(corridor.fromRoomId);
                    var toRoom = map.GetRoom(corridor.toRoomId);
                    if (fromRoom == null || toRoom == null) continue;

                    Vector3 from = new Vector3((fromRoom.center.x + 0.5f) * cs, 1f, (fromRoom.center.y + 0.5f) * cs);
                    Vector3 to = new Vector3((toRoom.center.x + 0.5f) * cs, 1f, (toRoom.center.y + 0.5f) * cs);
                    Gizmos.DrawLine(from, to);
                }
            }

            // Spawn & Exit
            if (showSpawnExit)
            {
                if (map.spawnCell.x >= 0)
                {
                    Gizmos.color = SpawnColor;
                    Vector3 spawnPos = new Vector3((map.spawnCell.x + 0.5f) * cs, 1f, (map.spawnCell.y + 0.5f) * cs);
                    Gizmos.DrawSphere(spawnPos, cs * 0.4f);
                    Gizmos.DrawWireSphere(spawnPos, cs * 0.6f);
                }
                if (map.exitCell.x >= 0)
                {
                    Gizmos.color = ExitColor;
                    Vector3 exitPos = new Vector3((map.exitCell.x + 0.5f) * cs, 1f, (map.exitCell.y + 0.5f) * cs);
                    Gizmos.DrawCube(exitPos, new Vector3(cs * 0.6f, cs * 0.6f, cs * 0.6f));
                    Gizmos.DrawWireCube(exitPos, new Vector3(cs * 0.8f, cs * 0.8f, cs * 0.8f));
                }
            }

            // Erreurs de validation
            if (showValidationErrors && result != null)
            {
                foreach (var entry in result.validationEntries)
                {
                    if (!entry.cell.HasValue) continue;
                    var cellPos = entry.cell.Value;
                    Vector3 pos = new Vector3((cellPos.x + 0.5f) * cs, 2f, (cellPos.y + 0.5f) * cs);

                    if (entry.severity == ValidationSeverity.Erreur)
                    {
                        Gizmos.color = ErrorColor;
                        Gizmos.DrawWireSphere(pos, cs * 0.5f);
                    }
                    else if (entry.severity == ValidationSeverity.Warning)
                    {
                        Gizmos.color = WarningColor;
                        Gizmos.DrawWireSphere(pos, cs * 0.35f);
                    }
                }
            }
        }
    }
}
