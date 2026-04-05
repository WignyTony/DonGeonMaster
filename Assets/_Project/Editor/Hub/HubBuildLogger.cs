using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Logging detaille pour la generation du HubPrototype.
/// Independant de MapGeneration. Ecrit TXT + JSON + Latest dans HubLogs/.
/// Usage : Begin() → BeginZone/LogCreate/LogMarker/Warning → End()
/// </summary>
public static class HubBuildLogger
{
    static readonly string LogFolder = Path.Combine(Application.dataPath, "..", "HubLogs");

    // Session state
    static DateTime startTime;
    static string sessionScene;
    static string sessionCatalogInfo;
    static string currentZoneId;
    static string currentZoneLabel;
    static int createIndex;

    // Data
    static List<CleanupEntry> cleanups;
    static List<ZoneData> zones;
    static List<MarkerEntry> markers;
    static List<string> warnings;
    static List<string> errors;

    // Counts
    static int totalPrefabs;
    static int totalProps;
    static int totalBuildings;
    static int totalEnvironment;
    static int totalNature;

    // ═══ STRUCTURES ═══

    public struct CleanupEntry
    {
        public string goName, parentName, prefabSource, note;
        public Vector3 position;
    }

    public struct CreateEntry
    {
        public int index;
        public string zoneId, role, goName, prefabName, prefabAssetPath, parentName, note;
        public Vector3 position, rotation, scale;
    }

    public struct MarkerEntry
    {
        public string name, visualName;
        public Vector3 position, rotation;
        public bool valid;
    }

    public class ZoneData
    {
        public string zoneId, zoneLabel;
        public List<CreateEntry> entries = new();
    }

    // ═══ API ═══

    public static void Begin(string scenePath, string catalogInfo)
    {
        startTime = DateTime.Now;
        sessionScene = scenePath;
        sessionCatalogInfo = catalogInfo;
        currentZoneId = null;
        currentZoneLabel = null;
        createIndex = 0;
        cleanups = new List<CleanupEntry>();
        zones = new List<ZoneData>();
        markers = new List<MarkerEntry>();
        warnings = new List<string>();
        errors = new List<string>();
        totalPrefabs = 0; totalProps = 0; totalBuildings = 0;
        totalEnvironment = 0; totalNature = 0;
    }

    public static void LogCleanup(string goName, string parentName, Vector3 position,
        string prefabSource = "", string note = "")
    {
        cleanups.Add(new CleanupEntry
        {
            goName = goName, parentName = parentName, position = position,
            prefabSource = prefabSource, note = note
        });
    }

    public static void BeginZone(string zoneId, string label)
    {
        currentZoneId = zoneId;
        currentZoneLabel = label;
        zones.Add(new ZoneData { zoneId = zoneId, zoneLabel = label });
    }

    public static void LogCreate(string role, string goName, string prefabName,
        string prefabAssetPath, Vector3 position, Vector3 rotation, Vector3 scale,
        string parentName, string note = "")
    {
        createIndex++;
        var entry = new CreateEntry
        {
            index = createIndex, zoneId = currentZoneId, role = role,
            goName = goName, prefabName = prefabName, prefabAssetPath = prefabAssetPath,
            position = position, rotation = rotation, scale = scale,
            parentName = parentName, note = note
        };

        if (zones.Count > 0)
            zones[zones.Count - 1].entries.Add(entry);

        totalPrefabs++;
        if (role.StartsWith("building")) totalBuildings++;
        else if (role.StartsWith("prop") || role.StartsWith("plaza_prop")) totalProps++;
        else if (role.Contains("road") || role.Contains("fence") || role.Contains("ground") ||
                 role.Contains("dungeon") || role.Contains("mud") || role.Contains("sand") ||
                 role.Contains("cobble") || role.Contains("corner")) totalEnvironment++;
        else if (role.Contains("tree") || role.Contains("bush") || role.Contains("nature")) totalNature++;
    }

    public static void LogMarker(string name, Vector3 position, Vector3 rotation,
        string visualName = "", bool valid = true)
    {
        markers.Add(new MarkerEntry
        {
            name = name, position = position, rotation = rotation,
            visualName = visualName, valid = valid
        });
    }

    public static void Warning(string msg)
    {
        warnings.Add(msg);
        Debug.LogWarning($"[HubBuild] {msg}");
    }

    public static void Error(string msg)
    {
        errors.Add(msg);
        Debug.LogError($"[HubBuild] {msg}");
    }

    // ═══ END — WRITE FILES ═══

    public static void End()
    {
        var elapsed = DateTime.Now - startTime;
        Directory.CreateDirectory(LogFolder);

        string ts = startTime.ToString("yyyy-MM-dd_HH-mm-ss");
        string baseName = $"HubBuild_{ts}";

        string txtPath = Path.Combine(LogFolder, baseName + ".txt");
        string jsonPath = Path.Combine(LogFolder, baseName + ".json");
        string latestTxt = Path.Combine(LogFolder, "HubBuild_Latest.txt");
        string latestJson = Path.Combine(LogFolder, "HubBuild_Latest.json");

        string txtContent = BuildTxt(elapsed);
        string jsonContent = BuildJson(elapsed);

        File.WriteAllText(txtPath, txtContent, Encoding.UTF8);
        File.WriteAllText(jsonPath, jsonContent, Encoding.UTF8);
        File.Copy(txtPath, latestTxt, true);
        File.Copy(jsonPath, latestJson, true);

        int totalCreated = createIndex;
        int totalDeleted = cleanups.Count;

        Debug.Log($"[HubBuild] ══════ BUILD COMPLETE ══════");
        Debug.Log($"[HubBuild] Created:{totalCreated} Deleted:{totalDeleted} Markers:{markers.Count} Warnings:{warnings.Count} Errors:{errors.Count}");
        Debug.Log($"[HubBuild] Prefabs:{totalPrefabs} (Bld:{totalBuildings} Prop:{totalProps} Env:{totalEnvironment} Nat:{totalNature})");
        Debug.Log($"[HubBuild] Time: {elapsed.TotalMilliseconds:F0}ms");
        Debug.Log($"[HubBuild] Logs:\n  {txtPath}\n  {jsonPath}");
    }

    // ═══ TXT BUILDER ═══

    static string BuildTxt(TimeSpan elapsed)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== HUB PROTOTYPE BUILD START ===");
        sb.AppendLine($"Date:      {startTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Scene:     {sessionScene}");
        sb.AppendLine($"Catalog:   {sessionCatalogInfo}");
        sb.AppendLine($"Time:      {elapsed.TotalMilliseconds:F0}ms");
        sb.AppendLine();

        // Cleanup
        sb.AppendLine("--- CLEANUP ---");
        if (cleanups.Count == 0)
            sb.AppendLine("[INFO] Scene created from scratch (NewScene EmptyScene)");
        else
            foreach (var c in cleanups)
                sb.AppendLine($"[DELETE] \"{c.goName}\" parent=\"{c.parentName}\" pos=({c.position.x:F1},{c.position.y:F1},{c.position.z:F1})" +
                    (string.IsNullOrEmpty(c.prefabSource) ? "" : $" prefab=\"{c.prefabSource}\"") +
                    (string.IsNullOrEmpty(c.note) ? "" : $" — {c.note}"));
        sb.AppendLine();

        // Zones
        foreach (var zone in zones)
        {
            sb.AppendLine($"--- ZONE: {zone.zoneLabel.ToUpper()} ({zone.zoneId}) ---");
            if (zone.entries.Count == 0)
                sb.AppendLine("  (empty)");
            foreach (var e in zone.entries)
            {
                sb.Append($"[CREATE #{e.index:D3}] role={e.role}");
                sb.Append($" name=\"{e.goName}\"");
                sb.Append($" prefab=\"{e.prefabName}\"");
                sb.Append($" pos=({e.position.x:F1},{e.position.y:F1},{e.position.z:F1})");
                sb.Append($" rot=({e.rotation.x:F0},{e.rotation.y:F0},{e.rotation.z:F0})");
                sb.Append($" scale=({e.scale.x:F2},{e.scale.y:F2},{e.scale.z:F2})");
                sb.Append($" parent=\"{e.parentName}\"");
                if (!string.IsNullOrEmpty(e.note)) sb.Append($" — {e.note}");
                sb.AppendLine();
            }
            sb.AppendLine();
        }

        // Markers
        sb.AppendLine("--- MARKERS ---");
        foreach (var m in markers)
        {
            sb.Append($"[MARKER] {m.name}");
            sb.Append($" pos=({m.position.x:F1},{m.position.y:F1},{m.position.z:F1})");
            sb.Append($" rot=({m.rotation.x:F0},{m.rotation.y:F0},{m.rotation.z:F0})");
            if (!string.IsNullOrEmpty(m.visualName)) sb.Append($" visual=\"{m.visualName}\"");
            sb.Append(m.valid ? " [OK]" : " [INVALID]");
            sb.AppendLine();
        }
        sb.AppendLine();

        // Summary
        sb.AppendLine("--- SUMMARY ---");
        sb.AppendLine($"Created:      {createIndex}");
        sb.AppendLine($"  Prefabs:    {totalPrefabs}");
        sb.AppendLine($"  Buildings:  {totalBuildings}");
        sb.AppendLine($"  Props:      {totalProps}");
        sb.AppendLine($"  Environm:   {totalEnvironment}");
        sb.AppendLine($"  Nature:     {totalNature}");
        sb.AppendLine($"Markers:      {markers.Count}");
        sb.AppendLine($"Deleted:      {cleanups.Count}");
        sb.AppendLine($"Warnings:     {warnings.Count}");
        foreach (var w in warnings) sb.AppendLine($"  ⚠ {w}");
        sb.AppendLine($"Errors:       {errors.Count}");
        foreach (var e in errors) sb.AppendLine($"  ✖ {e}");
        sb.AppendLine();
        sb.AppendLine("=== HUB PROTOTYPE BUILD END ===");

        return sb.ToString();
    }

    // ═══ JSON BUILDER ═══

    static string BuildJson(TimeSpan elapsed)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");

        // Session
        sb.AppendLine("  \"session\": {");
        sb.AppendLine($"    \"date\": \"{startTime:yyyy-MM-dd HH:mm:ss}\",");
        sb.AppendLine($"    \"scene\": \"{Esc(sessionScene)}\",");
        sb.AppendLine($"    \"catalog\": \"{Esc(sessionCatalogInfo)}\",");
        sb.AppendLine($"    \"timeMs\": {elapsed.TotalMilliseconds:F0}");
        sb.AppendLine("  },");

        // Cleanup
        sb.AppendLine("  \"cleanup\": [");
        for (int i = 0; i < cleanups.Count; i++)
        {
            var c = cleanups[i];
            sb.Append($"    {{\"name\":\"{Esc(c.goName)}\",\"parent\":\"{Esc(c.parentName)}\",");
            sb.Append($"\"pos\":[{c.position.x:F2},{c.position.y:F2},{c.position.z:F2}]");
            if (!string.IsNullOrEmpty(c.prefabSource)) sb.Append($",\"prefab\":\"{Esc(c.prefabSource)}\"");
            if (!string.IsNullOrEmpty(c.note)) sb.Append($",\"note\":\"{Esc(c.note)}\"");
            sb.Append("}");
            if (i < cleanups.Count - 1) sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("  ],");

        // Zones
        sb.AppendLine("  \"zones\": [");
        for (int z = 0; z < zones.Count; z++)
        {
            var zone = zones[z];
            sb.AppendLine($"    {{\"zoneId\":\"{Esc(zone.zoneId)}\",\"label\":\"{Esc(zone.zoneLabel)}\",\"entries\":[");
            for (int i = 0; i < zone.entries.Count; i++)
            {
                var e = zone.entries[i];
                sb.Append($"      {{\"index\":{e.index},\"role\":\"{Esc(e.role)}\",\"name\":\"{Esc(e.goName)}\",");
                sb.Append($"\"prefab\":\"{Esc(e.prefabName)}\",\"assetPath\":\"{Esc(e.prefabAssetPath)}\",");
                sb.Append($"\"pos\":[{e.position.x:F2},{e.position.y:F2},{e.position.z:F2}],");
                sb.Append($"\"rot\":[{e.rotation.x:F1},{e.rotation.y:F1},{e.rotation.z:F1}],");
                sb.Append($"\"scale\":[{e.scale.x:F2},{e.scale.y:F2},{e.scale.z:F2}],");
                sb.Append($"\"parent\":\"{Esc(e.parentName)}\"");
                if (!string.IsNullOrEmpty(e.note)) sb.Append($",\"note\":\"{Esc(e.note)}\"");
                sb.Append("}");
                if (i < zone.entries.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append("    ]}");
            if (z < zones.Count - 1) sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("  ],");

        // Markers
        sb.AppendLine("  \"markers\": [");
        for (int i = 0; i < markers.Count; i++)
        {
            var m = markers[i];
            sb.Append($"    {{\"name\":\"{Esc(m.name)}\",");
            sb.Append($"\"pos\":[{m.position.x:F2},{m.position.y:F2},{m.position.z:F2}],");
            sb.Append($"\"rot\":[{m.rotation.x:F1},{m.rotation.y:F1},{m.rotation.z:F1}],");
            sb.Append($"\"visual\":\"{Esc(m.visualName)}\",\"valid\":{(m.valid ? "true" : "false")}}}");
            if (i < markers.Count - 1) sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("  ],");

        // Summary
        sb.AppendLine("  \"summary\": {");
        sb.AppendLine($"    \"totalCreated\": {createIndex},");
        sb.AppendLine($"    \"totalPrefabs\": {totalPrefabs},");
        sb.AppendLine($"    \"totalBuildings\": {totalBuildings},");
        sb.AppendLine($"    \"totalProps\": {totalProps},");
        sb.AppendLine($"    \"totalEnvironment\": {totalEnvironment},");
        sb.AppendLine($"    \"totalNature\": {totalNature},");
        sb.AppendLine($"    \"totalMarkers\": {markers.Count},");
        sb.AppendLine($"    \"totalDeleted\": {cleanups.Count}");
        sb.AppendLine("  },");

        // Warnings/Errors
        sb.AppendLine("  \"warnings\": [");
        for (int i = 0; i < warnings.Count; i++)
        {
            sb.Append($"    \"{Esc(warnings[i])}\"");
            if (i < warnings.Count - 1) sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("  ],");
        sb.AppendLine("  \"errors\": [");
        for (int i = 0; i < errors.Count; i++)
        {
            sb.Append($"    \"{Esc(errors[i])}\"");
            if (i < errors.Count - 1) sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("  ]");

        sb.AppendLine("}");
        return sb.ToString();
    }

    static string Esc(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
}
