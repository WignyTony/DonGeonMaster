using UnityEngine;
using UnityEditor;

/// <summary>
/// Convertit PandaMat.mat du shader Built-in Standard vers URP/Lit.
/// Menu: DonGeonMaster > Convertir PandaMat en URP
/// </summary>
public class PandaMaterialConverter
{
    static readonly string PandaMatPath =
        "Assets/Pandazole_Ultimate_Pack/Pandazole Nature Environment Pack/Materials/PandaMat.mat";

    [MenuItem("DonGeonMaster/Convertir PandaMat en URP", false, 202)]
    public static void ConvertPandaMat()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(PandaMatPath);
        if (mat == null)
        {
            Debug.LogError($"[PandaMaterialConverter] Material introuvable: {PandaMatPath}");
            return;
        }

        string currentShader = mat.shader.name;
        Debug.Log($"[PandaMaterialConverter] Shader actuel: '{currentShader}'");

        // Deja converti ?
        if (currentShader.Contains("Universal Render Pipeline"))
        {
            Debug.Log("[PandaMaterialConverter] Material deja en URP. Rien a faire.");
            return;
        }

        // Sauvegarder la texture principale avant conversion
        Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
        float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;

        Debug.Log($"[PandaMaterialConverter] Texture principale: {(mainTex != null ? mainTex.name : "AUCUNE")}");
        Debug.Log($"[PandaMaterialConverter] Color: {mainColor} Smoothness: {smoothness} Metallic: {metallic}");

        // Trouver le shader URP/Lit
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[PandaMaterialConverter] Shader 'Universal Render Pipeline/Lit' introuvable. " +
                "Verifier que le package URP est installe.");
            return;
        }

        // Appliquer le shader URP/Lit
        mat.shader = urpLit;

        // Restaurer les proprietes avec les noms URP
        if (mainTex != null)
            mat.SetTexture("_BaseMap", mainTex);
        mat.SetColor("_BaseColor", mainColor);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);

        // Forcer le surface type opaque
        mat.SetFloat("_Surface", 0); // 0 = Opaque
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_ZWrite", 1);
        mat.SetFloat("_SrcBlend", 1);  // One
        mat.SetFloat("_DstBlend", 0);  // Zero
        mat.renderQueue = -1; // Default

        // Activer le keyword pour le workflow metallic
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Debug.Log($"[PandaMaterialConverter] Conversion terminee: '{currentShader}' -> 'Universal Render Pipeline/Lit'");
        Debug.Log($"[PandaMaterialConverter] Texture BaseMap: {(mainTex != null ? mainTex.name : "AUCUNE")}");
        Debug.Log("[PandaMaterialConverter] Les 733 prefabs Pandazole utilisent ce material partage. " +
            "Tous devraient maintenant s'afficher correctement en URP.");
    }

    [MenuItem("DonGeonMaster/Verifier Materials Pipeline", false, 203)]
    public static void VerifyAllMaterials()
    {
        string[] matGuids = AssetDatabase.FindAssets("t:Material",
            new[] { "Assets/Pandazole_Ultimate_Pack", "Assets/_Project" });

        int total = 0, incompatible = 0;

        foreach (var guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            total++;

            string shaderName = mat.shader.name;
            if (shaderName == "Standard" ||
                shaderName == "Hidden/InternalErrorShader" ||
                shaderName.StartsWith("Legacy Shaders/"))
            {
                Debug.LogWarning($"[VerifyMaterials] INCOMPATIBLE: '{path}' -> shader '{shaderName}'");
                incompatible++;
            }
        }

        if (incompatible == 0)
            Debug.Log($"[VerifyMaterials] {total} materials verifies. Tous compatibles URP.");
        else
            Debug.LogWarning($"[VerifyMaterials] {incompatible}/{total} materials incompatibles URP.");
    }
}
