using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    [Serializable]
    public class GenerationPreset
    {
        public string presetName;
        public string description;
        public string createdAt;
        public MapGenConfig config;

        public GenerationPreset(string name, MapGenConfig config)
        {
            presetName = name;
            description = "";
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.config = config.Clone();
        }
    }

    public static class PresetManager
    {
        static readonly string PresetFolder = Path.Combine(Application.dataPath, "..", "MapGenPresets");

        public static void SavePreset(GenerationPreset preset)
        {
            Directory.CreateDirectory(PresetFolder);
            string safeName = SanitizeFileName(preset.presetName);
            string path = Path.Combine(PresetFolder, safeName + ".json");
            string json = JsonUtility.ToJson(preset, true);
            File.WriteAllText(path, json);
            Debug.Log($"[PresetManager] Preset sauvegardé: {path}");
        }

        public static GenerationPreset LoadPreset(string presetName)
        {
            string safeName = SanitizeFileName(presetName);
            string path = Path.Combine(PresetFolder, safeName + ".json");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[PresetManager] Preset introuvable: {path}");
                return null;
            }
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GenerationPreset>(json);
        }

        public static List<string> GetAvailablePresets()
        {
            Directory.CreateDirectory(PresetFolder);
            return Directory.GetFiles(PresetFolder, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(n => n)
                .ToList();
        }

        public static void DeletePreset(string presetName)
        {
            string safeName = SanitizeFileName(presetName);
            string path = Path.Combine(PresetFolder, safeName + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[PresetManager] Preset supprimé: {path}");
            }
        }

        public static List<GenerationPreset> GetDefaultPresets()
        {
            return new List<GenerationPreset>
            {
                new("TestRapide", new MapGenConfig
                {
                    mapWidth = 20, mapHeight = 20,
                    minRooms = 3, maxRooms = 5,
                    minRoomSize = 3, maxRoomSize = 5,
                    mode = GenerationMode.StructureSeule
                }) { description = "Petit test rapide structure seule" },

                new("DonjonDense", new MapGenConfig
                {
                    mapWidth = 40, mapHeight = 40,
                    minRooms = 8, maxRooms = 15,
                    minRoomSize = 4, maxRoomSize = 10,
                    vegetationDensity = 0.8f,
                    corridorWidth = 2
                }) { description = "Grand donjon avec beaucoup de salles" },

                new("SalleUnique", new MapGenConfig
                {
                    mapWidth = 15, mapHeight = 15,
                    maxRoomSize = 10,
                    mode = GenerationMode.SalleUnique
                }) { description = "Une seule salle pour tester le contenu" },

                new("StressTest", new MapGenConfig
                {
                    mapWidth = 60, mapHeight = 60,
                    minRooms = 15, maxRooms = 25,
                    minRoomSize = 3, maxRoomSize = 12,
                    vegetationDensity = 0.9f,
                    corridorWidth = 3,
                    forceBossRoom = true,
                    forceSpecialRoom = true
                }) { description = "Map maximale pour stress test" },

                new("SansDecor", new MapGenConfig
                {
                    mapWidth = 30, mapHeight = 30,
                    minRooms = 5, maxRooms = 10,
                    mode = GenerationMode.SansProps
                }) { description = "Structure + gameplay sans décor" },

                new("NavigationOnly", new MapGenConfig
                {
                    mapWidth = 30, mapHeight = 30,
                    minRooms = 6, maxRooms = 10,
                    mode = GenerationMode.StructureSeule,
                    ensureAccessibility = true
                }) { description = "Test de navigation pure" },

                new("Complet", new MapGenConfig
                {
                    mapWidth = 35, mapHeight = 35,
                    minRooms = 6, maxRooms = 12,
                    mode = GenerationMode.Complet,
                    forceBossRoom = true,
                    ensureAccessibility = true
                }) { description = "Génération complète standard" }
            };
        }

        static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        public static string GetPresetFolder() => PresetFolder;
    }
}
