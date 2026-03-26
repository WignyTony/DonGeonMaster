using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.IO;
using DonGeonMaster.Equipment;

/// <summary>
/// Generates 3D thumbnail renders of ISOLATED armor pieces (not on the body).
/// Uses the Non-Skinned Mesh Parts prefabs from the GanzSe pack.
/// Each piece is instanced alone, camera auto-frames based on bounds, rendered to PNG.
/// </summary>
public static class ArmorThumbnailGenerator
{
    private const string ThumbnailPath = "Assets/_Project/Art/Textures/Thumbnails";
    private const int ThumbSize = 128;

    private static readonly string[] PrefabFolders = new[]
    {
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Non-Skinned Mesh Parts/Armor Parts",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Non-Skinned Mesh Parts/Armor Parts 1.1",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/GREAT SWORDS",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/ONE-HANDED SWORDS",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/SHIELDS",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/HAMMERS",
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/WEAPONS UPDATE 1.1"
    };

    [MenuItem("DonGeonMaster/Generate Armor Thumbnails")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(ThumbnailPath);

        // Setup camera + light
        Vector3 basePos = new Vector3(200, 0, 200);

        var camObj = new GameObject("ThumbCam");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        cam.nearClipPlane = 0.01f;
        cam.fieldOfView = 30;
        camObj.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

        var lightObj = new GameObject("ThumbLight");
        lightObj.transform.rotation = Quaternion.Euler(30, -30, 0);
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.9f);
        light.intensity = 1.5f;
        light.shadows = LightShadows.None;

        var rt = new RenderTexture(ThumbSize, ThumbSize, 16);
        cam.targetTexture = rt;

        // Find all armor prefabs
        var prefabGuids = new System.Collections.Generic.List<string>();
        foreach (var folder in PrefabFolders)
        {
            if (AssetDatabase.IsValidFolder(folder))
                prefabGuids.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { folder }));
        }

        int generated = 0;
        foreach (var guid in prefabGuids)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;
            if (prefab.name.Contains("Arrow") || prefab.name.Contains("Bow")) continue;

            // Derive thumbnail name from prefab name (remove " Part" suffix, clean "FREE "/"COLOR ")
            string pieceName = prefab.name;
            if (pieceName.EndsWith(" Part"))
                pieceName = pieceName.Substring(0, pieceName.Length - 5);

            string safeName = pieceName.Replace(" ", "_");
            string pngPath = $"{ThumbnailPath}/Thumb_{safeName}.png";

            // Skip if already exists
            if (File.Exists(pngPath)) { generated++; continue; }

            // Instantiate the isolated piece
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = basePos;

            // Fix material to URP if needed
            FixMaterial(instance);

            // Calculate bounds to frame the piece
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Object.DestroyImmediate(instance);
                continue;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            // Position camera to frame the object
            Vector3 center = bounds.center;
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float distance = size / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * 1.3f;
            distance = Mathf.Max(distance, 0.3f);

            camObj.transform.position = center + new Vector3(0.3f, 0.2f, -distance);
            camObj.transform.LookAt(center);

            // Render
            cam.Render();

            // Copy to Texture2D
            RenderTexture.active = rt;
            var tex = new Texture2D(ThumbSize, ThumbSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, ThumbSize, ThumbSize), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // Save PNG
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(instance);

            // Import as sprite
            AssetDatabase.ImportAsset(pngPath);
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            generated++;
        }

        // Cleanup
        Object.DestroyImmediate(camObj);
        Object.DestroyImmediate(lightObj);
        rt.Release();
        Object.DestroyImmediate(rt);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Thumbnails] Generated {generated} isolated armor thumbnails.");
    }

    public static void AssignThumbnailsToArmor()
    {
        // Search both Armor and Weapons configs
        var searchFolders = new System.Collections.Generic.List<string>();
        if (AssetDatabase.IsValidFolder("Assets/_Project/Configs/Armor"))
            searchFolders.Add("Assets/_Project/Configs/Armor");
        if (AssetDatabase.IsValidFolder("Assets/_Project/Configs/Weapons"))
            searchFolders.Add("Assets/_Project/Configs/Weapons");

        var guids = AssetDatabase.FindAssets("t:EquipmentData", searchFolders.ToArray());
        int assigned = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var eq = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            if (eq == null) continue;

            // Try armorPartName first, then meshPrefab name for weapons
            string pieceName = null;
            if (!string.IsNullOrEmpty(eq.armorPartName))
                pieceName = eq.armorPartName;
            else if (eq.meshPrefab != null)
            {
                pieceName = eq.meshPrefab.name;
                if (pieceName.EndsWith(" Part"))
                    pieceName = pieceName.Substring(0, pieceName.Length - 5);
            }

            if (string.IsNullOrEmpty(pieceName)) continue;

            string safeName = pieceName.Replace(" ", "_");
            string thumbPath = $"{ThumbnailPath}/Thumb_{safeName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(thumbPath);

            if (sprite != null)
            {
                eq.icon = sprite;
                EditorUtility.SetDirty(eq);
                assigned++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Thumbnails] Assigned {assigned} thumbnails to equipment assets.");
    }

    private static void FixMaterial(GameObject obj)
    {
        // Convert Standard materials to URP on the isolated prefab
        var urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) return;

        foreach (var rend in obj.GetComponentsInChildren<Renderer>(true))
        {
            var mats = rend.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].shader.name == "Standard")
                {
                    // The original Base Palette Material should already be URP
                    // but if not, this ensures the isolated prefab renders correctly
                    var tex = mats[i].GetTexture("_MainTex");
                    mats[i] = new Material(urpShader);
                    if (tex != null) mats[i].SetTexture("_BaseMap", tex);
                    mats[i].SetColor("_BaseColor", Color.white);
                }
            }
            rend.sharedMaterials = mats;
        }
    }
}
