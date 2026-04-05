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

    // ═══ ENUMS ═══

    public enum Family { Building, Prop, Environment, Nature, Marker, Helper }

    public static readonly string[] PhaseNames = {
        "gameplay_markers",
        "ground_foundation",
        "village_perimeter",
        "main_buildings",
        "dungeon_approach",
        "props_dressing",
        "nature_dressing"
    };

    // ═══ STRUCTURES ═══

    public struct CleanupEntry
    {
        public string goName, parentName, prefabSource, note;
        public Vector3 position;
    }

    public struct CreateEntry
    {
        public int index;
        public string phase, zoneId, role, goName, prefabName, prefabAssetPath;
        public string parentName, hierarchyPath, note;
        public Family family;
        public Vector3 position, localPosition, rotation, scale;
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

    // ═══ STATE ═══

    static DateTime startTime;
    static string sessionScene, sessionCatalogInfo;
    static string currentZoneId, currentZoneLabel, currentPhase;
    static int createIndex;

    static List<CleanupEntry> cleanups;
    static List<ZoneData> zones;
    static List<MarkerEntry> markers;
    static List<string> warnings, errors;

    // Counts per family
    static int cntBuilding, cntProp, cntEnvironment, cntNature, cntMarker, cntHelper;

    // Bounds tracking
    static bool hasBounds;
    static Vector3 boundsMin, boundsMax;
    static Dictionary<Family, (Vector3 min, Vector3 max, bool has)> familyBounds;

    // Hierarchy tracking
    static Dictionary<string, int> hierarchyChildCount;

    // ═══ API ═══

    public static void Begin(string scenePath, string catalogInfo)
    {
        startTime = DateTime.Now;
        sessionScene = scenePath;
        sessionCatalogInfo = catalogInfo;
        currentZoneId = currentZoneLabel = currentPhase = null;
        createIndex = 0;
        cleanups = new List<CleanupEntry>();
        zones = new List<ZoneData>();
        markers = new List<MarkerEntry>();
        warnings = new List<string>();
        errors = new List<string>();
        cntBuilding = cntProp = cntEnvironment = cntNature = cntMarker = cntHelper = 0;
        hasBounds = false;
        boundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        boundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        familyBounds = new Dictionary<Family, (Vector3, Vector3, bool)>();
        hierarchyChildCount = new Dictionary<string, int>();
    }

    public static void SetPhase(string phase) => currentPhase = phase;

    public static void BeginZone(string zoneId, string label)
    {
        currentZoneId = zoneId;
        currentZoneLabel = label;
        zones.Add(new ZoneData { zoneId = zoneId, zoneLabel = label });
    }

    public static void LogCleanup(string goName, string parentName, Vector3 position,
        string prefabSource = "", string note = "")
    {
        cleanups.Add(new CleanupEntry {
            goName = goName, parentName = parentName, position = position,
            prefabSource = prefabSource, note = note
        });
    }

    public static void LogCreate(string role, Family family, string goName, string prefabName,
        string prefabAssetPath, Vector3 position, Vector3 localPosition, Vector3 rotation,
        Vector3 scale, string parentName, string hierarchyPath, string note = "")
    {
        createIndex++;
        var entry = new CreateEntry {
            index = createIndex, phase = currentPhase ?? "", zoneId = currentZoneId ?? "",
            role = role, family = family, goName = goName, prefabName = prefabName,
            prefabAssetPath = prefabAssetPath, position = position, localPosition = localPosition,
            rotation = rotation, scale = scale, parentName = parentName,
            hierarchyPath = hierarchyPath, note = note
        };

        if (zones.Count > 0) zones[zones.Count - 1].entries.Add(entry);

        // Family counts
        switch (family) {
            case Family.Building:    cntBuilding++; break;
            case Family.Prop:        cntProp++; break;
            case Family.Environment: cntEnvironment++; break;
            case Family.Nature:      cntNature++; break;
            case Family.Marker:      cntMarker++; break;
            case Family.Helper:      cntHelper++; break;
        }

        // Bounds
        TrackBounds(position);
        TrackFamilyBounds(family, position);

        // Hierarchy
        if (!string.IsNullOrEmpty(parentName)) {
            if (!hierarchyChildCount.ContainsKey(parentName)) hierarchyChildCount[parentName] = 0;
            hierarchyChildCount[parentName]++;
        }
    }

    public static void LogMarker(string name, Vector3 position, Vector3 rotation,
        string visualName = "", bool valid = true)
    {
        markers.Add(new MarkerEntry {
            name = name, position = position, rotation = rotation,
            visualName = visualName, valid = valid
        });
        cntMarker++;
        TrackBounds(position);
        TrackFamilyBounds(Family.Marker, position);
    }

    public static void Warning(string msg) { warnings.Add(msg); Debug.LogWarning($"[HubBuild] {msg}"); }
    public static void Error(string msg)   { errors.Add(msg);   Debug.LogError($"[HubBuild] {msg}"); }

    public static int CountFamily(Family f) {
        switch (f) {
            case Family.Building: return cntBuilding;
            case Family.Prop: return cntProp;
            case Family.Environment: return cntEnvironment;
            case Family.Nature: return cntNature;
            case Family.Marker: return cntMarker;
            case Family.Helper: return cntHelper;
            default: return 0;
        }
    }

    // ═══ BOUNDS HELPERS ═══

    static void TrackBounds(Vector3 p) {
        hasBounds = true;
        boundsMin = Vector3.Min(boundsMin, p);
        boundsMax = Vector3.Max(boundsMax, p);
    }

    static void TrackFamilyBounds(Family f, Vector3 p) {
        if (!familyBounds.ContainsKey(f))
            familyBounds[f] = (p, p, true);
        else {
            var (mn, mx, _) = familyBounds[f];
            familyBounds[f] = (Vector3.Min(mn, p), Vector3.Max(mx, p), true);
        }
    }

    // ═══ END ═══

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

        string txt = BuildTxt(elapsed);
        string json = BuildJson(elapsed);

        File.WriteAllText(txtPath, txt, Encoding.UTF8);
        File.WriteAllText(jsonPath, json, Encoding.UTF8);
        File.Copy(txtPath, latestTxt, true);
        File.Copy(jsonPath, latestJson, true);

        Debug.Log("[HubBuild] ══════ BUILD COMPLETE ══════");
        Debug.Log($"[HubBuild] Created:{createIndex} Deleted:{cleanups.Count} Markers:{markers.Count} Warnings:{warnings.Count} Errors:{errors.Count}");
        Debug.Log($"[HubBuild] Families — Bld:{cntBuilding} Prop:{cntProp} Env:{cntEnvironment} Nat:{cntNature} Marker:{cntMarker} Helper:{cntHelper}");
        if (hasBounds)
            Debug.Log($"[HubBuild] Bounds — X:[{boundsMin.x:F1},{boundsMax.x:F1}] Y:[{boundsMin.y:F1},{boundsMax.y:F1}] Z:[{boundsMin.z:F1},{boundsMax.z:F1}]");
        Debug.Log($"[HubBuild] Time: {elapsed.TotalMilliseconds:F0}ms");
        Debug.Log($"[HubBuild] Logs:\n  {txtPath}\n  {jsonPath}");
    }

    // ═══ TXT ═══

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
                sb.AppendLine($"[DELETE] \"{c.goName}\" parent=\"{c.parentName}\" pos=({V(c.position)})" +
                    (string.IsNullOrEmpty(c.prefabSource) ? "" : $" prefab=\"{c.prefabSource}\"") +
                    (string.IsNullOrEmpty(c.note) ? "" : $" — {c.note}"));
        sb.AppendLine();

        // Zones
        foreach (var zone in zones)
        {
            sb.AppendLine($"--- ZONE: {zone.zoneLabel.ToUpper()} ({zone.zoneId}) ---");
            if (zone.entries.Count == 0) sb.AppendLine("  (empty)");
            foreach (var e in zone.entries)
            {
                sb.Append($"[CREATE #{e.index:D3}] phase={e.phase} family={e.family} role={e.role}");
                sb.Append($" name=\"{e.goName}\" prefab=\"{e.prefabName}\"");
                sb.Append($" pos=({V(e.position)}) local=({V(e.localPosition)})");
                sb.Append($" rot=({V(e.rotation)}) scale=({V(e.scale)})");
                sb.Append($" path=\"{e.hierarchyPath}\"");
                if (!string.IsNullOrEmpty(e.note)) sb.Append($" — {e.note}");
                sb.AppendLine();
            }
            sb.AppendLine();
        }

        // Markers
        sb.AppendLine("--- MARKERS ---");
        foreach (var m in markers)
            sb.AppendLine($"[MARKER] {m.name} pos=({V(m.position)}) rot=({V(m.rotation)}) visual=\"{m.visualName}\" {(m.valid ? "[OK]" : "[INVALID]")}");
        sb.AppendLine();

        // Hierarchy
        sb.AppendLine("--- HIERARCHY ---");
        foreach (var kvp in hierarchyChildCount)
            sb.AppendLine($"  {kvp.Key} ({kvp.Value} children)");
        sb.AppendLine();

        // Bounds
        sb.AppendLine("--- BOUNDS ---");
        if (hasBounds) {
            sb.AppendLine($"Global  X:[{boundsMin.x:F1}, {boundsMax.x:F1}]  Y:[{boundsMin.y:F1}, {boundsMax.y:F1}]  Z:[{boundsMin.z:F1}, {boundsMax.z:F1}]");
            foreach (var kvp in familyBounds) {
                var (mn, mx, _) = kvp.Value;
                sb.AppendLine($"  {kvp.Key,-12} X:[{mn.x:F1}, {mx.x:F1}]  Z:[{mn.z:F1}, {mx.z:F1}]");
            }
        }
        sb.AppendLine();

        // Per-zone summary
        sb.AppendLine("--- PER-ZONE SUMMARY ---");
        foreach (var zone in zones) {
            if (zone.entries.Count == 0) continue;
            int zBld=0,zPrp=0,zEnv=0,zNat=0,zMk=0,zHlp=0;
            foreach (var e in zone.entries) {
                switch(e.family) { case Family.Building:zBld++;break; case Family.Prop:zPrp++;break;
                    case Family.Environment:zEnv++;break; case Family.Nature:zNat++;break;
                    case Family.Marker:zMk++;break; case Family.Helper:zHlp++;break; }
            }
            sb.AppendLine($"  {zone.zoneId,-20} total:{zone.entries.Count} Bld:{zBld} Prp:{zPrp} Env:{zEnv} Nat:{zNat} Mk:{zMk} Hlp:{zHlp}");
        }
        sb.AppendLine();

        // Summary
        sb.AppendLine("--- SUMMARY ---");
        sb.AppendLine($"Created:       {createIndex}");
        sb.AppendLine($"  Building:    {cntBuilding}");
        sb.AppendLine($"  Prop:        {cntProp}");
        sb.AppendLine($"  Environment: {cntEnvironment}");
        sb.AppendLine($"  Nature:      {cntNature}");
        sb.AppendLine($"  Marker:      {cntMarker}");
        sb.AppendLine($"  Helper:      {cntHelper}");
        sb.AppendLine($"Markers:       {markers.Count}");
        sb.AppendLine($"Deleted:       {cleanups.Count}");
        sb.AppendLine($"Warnings:      {warnings.Count}");
        foreach (var w in warnings) sb.AppendLine($"  ! {w}");
        sb.AppendLine($"Errors:        {errors.Count}");
        foreach (var e in errors) sb.AppendLine($"  X {e}");
        sb.AppendLine();
        sb.AppendLine("=== HUB PROTOTYPE BUILD END ===");
        return sb.ToString();
    }

    // ═══ JSON ═══

    static string BuildJson(TimeSpan elapsed)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");

        sb.AppendLine("  \"session\": {");
        sb.AppendLine($"    \"date\": \"{startTime:yyyy-MM-dd HH:mm:ss}\",");
        sb.AppendLine($"    \"scene\": \"{E(sessionScene)}\",");
        sb.AppendLine($"    \"catalog\": \"{E(sessionCatalogInfo)}\",");
        sb.AppendLine($"    \"timeMs\": {elapsed.TotalMilliseconds:F0}");
        sb.AppendLine("  },");

        // Cleanup
        sb.AppendLine("  \"cleanup\": [");
        for (int i = 0; i < cleanups.Count; i++) {
            var c = cleanups[i];
            sb.Append($"    {{\"name\":\"{E(c.goName)}\",\"parent\":\"{E(c.parentName)}\",\"pos\":{JV(c.position)}");
            if (!string.IsNullOrEmpty(c.prefabSource)) sb.Append($",\"prefab\":\"{E(c.prefabSource)}\"");
            if (!string.IsNullOrEmpty(c.note)) sb.Append($",\"note\":\"{E(c.note)}\"");
            sb.Append(i < cleanups.Count - 1 ? "}," : "}"); sb.AppendLine();
        }
        sb.AppendLine("  ],");

        // Zones
        sb.AppendLine("  \"zones\": [");
        for (int z = 0; z < zones.Count; z++) {
            var zone = zones[z];
            sb.AppendLine($"    {{\"zoneId\":\"{E(zone.zoneId)}\",\"label\":\"{E(zone.zoneLabel)}\",\"entries\":[");
            for (int i = 0; i < zone.entries.Count; i++) {
                var e = zone.entries[i];
                sb.Append($"      {{\"index\":{e.index},\"phase\":\"{E(e.phase)}\",\"family\":\"{e.family}\",\"role\":\"{E(e.role)}\",");
                sb.Append($"\"name\":\"{E(e.goName)}\",\"prefab\":\"{E(e.prefabName)}\",\"assetPath\":\"{E(e.prefabAssetPath)}\",");
                sb.Append($"\"pos\":{JV(e.position)},\"localPos\":{JV(e.localPosition)},\"rot\":{JV(e.rotation)},\"scale\":{JV(e.scale)},");
                sb.Append($"\"parent\":\"{E(e.parentName)}\",\"path\":\"{E(e.hierarchyPath)}\"");
                if (!string.IsNullOrEmpty(e.note)) sb.Append($",\"note\":\"{E(e.note)}\"");
                sb.Append(i < zone.entries.Count - 1 ? "}," : "}"); sb.AppendLine();
            }
            sb.Append(z < zones.Count - 1 ? "    ]}," : "    ]}"); sb.AppendLine();
        }
        sb.AppendLine("  ],");

        // Markers
        sb.AppendLine("  \"markers\": [");
        for (int i = 0; i < markers.Count; i++) {
            var m = markers[i];
            sb.Append($"    {{\"name\":\"{E(m.name)}\",\"pos\":{JV(m.position)},\"rot\":{JV(m.rotation)},\"visual\":\"{E(m.visualName)}\",\"valid\":{(m.valid?"true":"false")}}}");
            sb.AppendLine(i < markers.Count - 1 ? "," : "");
        }
        sb.AppendLine("  ],");

        // Hierarchy
        sb.AppendLine("  \"hierarchy\": {");
        int hIdx = 0;
        foreach (var kvp in hierarchyChildCount) {
            hIdx++;
            sb.AppendLine($"    \"{E(kvp.Key)}\": {kvp.Value}{(hIdx < hierarchyChildCount.Count ? "," : "")}");
        }
        sb.AppendLine("  },");

        // Bounds
        sb.AppendLine("  \"bounds\": {");
        if (hasBounds) {
            sb.AppendLine($"    \"global\": {{\"min\":{JV(boundsMin)},\"max\":{JV(boundsMax)}}},");
            sb.AppendLine("    \"perFamily\": {");
            int fIdx = 0;
            foreach (var kvp in familyBounds) {
                fIdx++;
                var (mn, mx, _) = kvp.Value;
                sb.AppendLine($"      \"{kvp.Key}\": {{\"min\":{JV(mn)},\"max\":{JV(mx)}}}{(fIdx < familyBounds.Count ? "," : "")}");
            }
            sb.AppendLine("    }");
        }
        sb.AppendLine("  },");

        // Summary
        sb.AppendLine("  \"summary\": {");
        sb.AppendLine($"    \"totalCreated\": {createIndex},");
        sb.AppendLine($"    \"building\": {cntBuilding},");
        sb.AppendLine($"    \"prop\": {cntProp},");
        sb.AppendLine($"    \"environment\": {cntEnvironment},");
        sb.AppendLine($"    \"nature\": {cntNature},");
        sb.AppendLine($"    \"marker\": {cntMarker},");
        sb.AppendLine($"    \"helper\": {cntHelper},");
        sb.AppendLine($"    \"totalDeleted\": {cleanups.Count}");
        sb.AppendLine("  },");

        sb.AppendLine("  \"warnings\": [");
        for (int i = 0; i < warnings.Count; i++)
            sb.AppendLine($"    \"{E(warnings[i])}\"{(i<warnings.Count-1?",":"")}");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"errors\": [");
        for (int i = 0; i < errors.Count; i++)
            sb.AppendLine($"    \"{E(errors[i])}\"{(i<errors.Count-1?",":"")}");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    static string V(Vector3 v) => $"{v.x:F1},{v.y:F1},{v.z:F1}";
    static string JV(Vector3 v) => $"[{v.x:F2},{v.y:F2},{v.z:F2}]";
    static string E(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
}
