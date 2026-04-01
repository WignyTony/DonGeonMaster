using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    /// <summary>Type de surface pour la cellule.</summary>
    public enum SurfaceShape
    {
        Flat,
        RampNorth,
        RampSouth,
        RampEast,
        RampWest,
        Stairs
    }

    [System.Serializable]
    public class MapCell
    {
        public int x;
        public int y;
        public CellType type = CellType.Vide;
        public BiomeType biome = BiomeType.Foret;
        public int roomId = -1;
        public bool isPath;
        public bool isSpawnPoint;
        public bool isExit;
        public bool isOccupied;
        public List<string> placedAssetCategories = new();

        /// <summary>Hauteur du sol en unites monde. Utilise par le renderer et le collision ground.</summary>
        public float floorHeight;

        /// <summary>Forme de la surface (plat, rampe, escalier).</summary>
        public SurfaceShape surfaceShape = SurfaceShape.Flat;

        [System.NonSerialized]
        public List<GameObject> placedObjects = new();

        public MapCell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool IsWalkable => type == CellType.Sol || type == CellType.Couloir;
        public bool IsInRoom => roomId >= 0;

        /// <summary>Position monde du dessus du sol pour cette cellule.</summary>
        public float WorldFloorY => floorHeight;
    }

    [System.Serializable]
    public class Room
    {
        public int id;
        public RectInt bounds;
        public BiomeType biome;
        public bool isSpawnRoom;
        public bool isExitRoom;
        public bool isBossRoom;
        public bool isSpecialRoom;
        public List<int> connectedRoomIds = new();
        public Vector2Int center;

        public Room(int id, RectInt bounds)
        {
            this.id = id;
            this.bounds = bounds;
            center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
        }

        public int Area => bounds.width * bounds.height;
    }

    [System.Serializable]
    public class Corridor
    {
        public int fromRoomId;
        public int toRoomId;
        public List<Vector2Int> cells = new();
        public int width = 1;
    }
}
