using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    [Serializable]
    public class MapGenConfig
    {
        [Header("Taille de la map")]
        public int mapWidth = 30;
        public int mapHeight = 30;
        public float cellSize = 6f;
        public int borderMargin = 2;

        [Header("Salles")]
        public int minRooms = 5;
        public int maxRooms = 12;
        public int minRoomSize = 3;
        public int maxRoomSize = 8;

        [Header("Couloirs")]
        public int corridorWidth = 2;
        public int maxCorridors = 20;

        [Header("Densité")]
        [Range(0f, 1f)] public float vegetationDensity = 0.6f;
        [Range(0f, 1f)] public float rockDensity = 0.2f;
        [Range(0f, 1f)] public float decorDensity = 0.3f;
        public float minDistanceBetweenPOI = 10f;

        [Header("Seed")]
        public int seed;
        public bool useRandomSeed = true;
        public bool lockSeed;

        [Header("Mode de génération")]
        public GenerationMode mode = GenerationMode.Complet;
        public LayoutType layoutType = LayoutType.BSP;
        public BiomeType forcedBiome = BiomeType.Foret;
        public bool useForcedBiome;

        [Header("Contraintes")]
        public bool ensureAccessibility = true;
        public bool validateAfterGeneration = true;
        public float minSpawnToExitDistance = 15f;
        public bool forceBossRoom;
        public bool forceSpecialRoom;
        public bool forceStartRoom = true;
        public bool forceExitRoom = true;

        [Header("Catégories activées")]
        public List<string> enabledCategories = new();

        [Header("Asset Transform (Pandazole)")]
        public Vector3 assetScale = new(100f, 100f, 100f);
        public Vector3 assetRotation = new(-90f, 0f, 0f);

        public MapGenConfig Clone()
        {
            var json = JsonUtility.ToJson(this);
            return JsonUtility.FromJson<MapGenConfig>(json);
        }
    }
}
