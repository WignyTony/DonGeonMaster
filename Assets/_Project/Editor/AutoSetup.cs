using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Runs automatically when Unity recompiles scripts.
/// 1. Imports TMP Essential Resources if missing
/// 2. Runs the medieval project setup if not done or outdated
/// </summary>
[InitializeOnLoad]
public static class AutoSetup
{
    // Increment this to force a re-setup after code changes
    private const int SetupVersion = 98;
    private const string VersionKey = "DonGeonMaster_SetupVersion";

    static AutoSetup()
    {
        EditorApplication.delayCall += OnEditorReady;
    }

    private static void OnEditorReady()
    {
        // Don't run during Play mode
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // Step 1: Import TMP Essential Resources if missing
        if (!IsTMPImported())
        {
            Debug.Log("[DonGeonMaster] TMP Essential Resources manquantes, import en cours...");
            ImportTMPResources();
            Debug.Log("[DonGeonMaster] TMP importé. Le setup se lancera après recompilation...");
            return;
        }

        // Step 2: Run medieval setup if not done or outdated
        int currentVersion = EditorPrefs.GetInt(VersionKey, 0);
        if (currentVersion < SetupVersion || !ProjectSetup.IsSetupDone())
        {
            Debug.Log($"[DonGeonMaster] Setup v{SetupVersion} requis (actuel: v{currentVersion}), lancement...");
            ProjectSetup.RunSetup();
            EditorPrefs.SetInt(VersionKey, SetupVersion);

            // Open the MainMenu scene
            var scenePath = "Assets/_Project/Scenes/MainMenu.unity";
            if (File.Exists(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log("[DonGeonMaster] Setup v" + SetupVersion + " terminé ! Scène MainMenu ouverte.");
            }
        }
    }

    private static bool IsTMPImported()
    {
        return Directory.Exists("Assets/TextMesh Pro");
    }

    private static void ImportTMPResources()
    {
        string packageCachePath = "Library/PackageCache";
        if (!Directory.Exists(packageCachePath))
        {
            Debug.LogError("[DonGeonMaster] Package cache not found!");
            return;
        }

        string unityPackagePath = null;
        foreach (var dir in Directory.GetDirectories(packageCachePath, "com.unity.ugui@*"))
        {
            string candidate = Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
            if (File.Exists(candidate))
            {
                unityPackagePath = candidate;
                break;
            }
        }

        if (unityPackagePath == null)
        {
            Debug.LogError("[DonGeonMaster] TMP Essential Resources.unitypackage introuvable !");
            return;
        }

        Debug.Log("[DonGeonMaster] Import depuis : " + unityPackagePath);
        AssetDatabase.ImportPackage(unityPackagePath, false);
    }
}
