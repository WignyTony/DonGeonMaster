using UnityEngine;
using DonGeonMaster.Player;
using DonGeonMaster.UI;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Phase 3 : branche le vrai heros du projet sur la structure 3D debug.
    /// Aucun placeholder, aucune capsule, aucun faux controleur.
    /// Reutilise strictement PlayerController + CameraController + KeyBindingManager.
    ///
    /// Usage :
    ///   heroDebugBridge.ganzsePrefab doit etre assigne dans l'Inspector
    ///   (ou par un script Editor au setup de la scene).
    /// </summary>
    public class HeroDebugBridge : MonoBehaviour
    {
        [Header("Prefab du vrai heros (GanzSe)")]
        [SerializeField] GameObject ganzsePrefab;

        GameObject heroInstance;
        PlayerController playerController;
        CameraController cameraController;
        Camera heroCamera;
        bool isActive;

        public bool IsActive => isActive;
        public GameObject HeroInstance => heroInstance;

        /// <summary>
        /// Active le heros : instancie si necessaire, place au spawn, active controles + camera.
        /// topDownCamera est la camera debug a desactiver pendant le mode heros.
        /// </summary>
        public void Activate(MapData map, MapGenConfig config, Camera topDownCamera)
        {
            if (map == null || config == null)
            {
                UnityEngine.Debug.LogError("[HeroDebugBridge] MapData ou config null");
                return;
            }

            // S'assurer que KeyBindingManager existe (requis par PlayerController)
            EnsureKeyBindingManager();

            // Instancier le heros si pas encore fait
            if (heroInstance == null)
            {
                if (!SpawnHero(map, config))
                    return;
            }
            else
            {
                // Repositionner sur le spawn de la nouvelle map
                heroInstance.transform.position = GetSpawnPosition(map, config);
            }

            // Activer le heros
            heroInstance.SetActive(true);
            if (playerController != null) playerController.enabled = true;

            // Activer la camera heros, desactiver la camera top-down
            if (heroCamera != null) heroCamera.enabled = true;
            if (cameraController != null) cameraController.enabled = true;
            if (topDownCamera != null) topDownCamera.enabled = false;

            isActive = true;
            Cursor.lockState = CursorLockMode.None;
            UnityEngine.Debug.Log("[HeroDebugBridge] Heros active");
        }

        /// <summary>
        /// Desactive le heros : desactive controles + camera, reactive la camera top-down.
        /// </summary>
        public void Deactivate(Camera topDownCamera)
        {
            if (playerController != null) playerController.enabled = false;
            if (cameraController != null) cameraController.enabled = false;
            if (heroCamera != null) heroCamera.enabled = false;
            if (heroInstance != null) heroInstance.SetActive(false);

            if (topDownCamera != null) topDownCamera.enabled = true;

            isActive = false;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// Detruit completement le heros (pour regeneration).
        /// </summary>
        public void DestroyHero()
        {
            if (heroCamera != null) DestroyImmediate(heroCamera.gameObject);
            if (heroInstance != null) DestroyImmediate(heroInstance);
            heroInstance = null;
            playerController = null;
            cameraController = null;
            heroCamera = null;
            isActive = false;
        }

        // ════════════════════════════════════════════
        //  SPAWN
        // ════════════════════════════════════════════

        bool SpawnHero(MapData map, MapGenConfig config)
        {
            if (ganzsePrefab == null)
            {
                UnityEngine.Debug.LogError("[HeroDebugBridge] ganzsePrefab non assigne ! " +
                    "Assigne le prefab GanzSe dans l'Inspector du HeroDebugBridge.");
                return false;
            }

            Vector3 spawnPos = GetSpawnPosition(map, config);

            // Instancier le vrai prefab GanzSe
            heroInstance = Instantiate(ganzsePrefab, spawnPos, Quaternion.identity);
            heroInstance.name = "DebugHero";
            heroInstance.tag = "Player";

            // Desactiver les armures par defaut (comme le fait ProjectSetup)
            DonGeonMaster.Character.GanzSeHelper.DisableAllArmor(heroInstance);

            // CharacterController (comme dans le vrai Hub)
            var cc = heroInstance.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = heroInstance.AddComponent<CharacterController>();
                cc.height = 1.7f;
                cc.radius = 0.3f;
                cc.center = new Vector3(0, 0.85f, 0);
            }

            // Le VRAI PlayerController du projet
            playerController = heroInstance.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = heroInstance.AddComponent<PlayerController>();

            // Creer la camera heros avec le VRAI CameraController
            var camGO = new GameObject("HeroCamera");
            camGO.tag = "MainCamera";
            heroCamera = camGO.AddComponent<Camera>();
            heroCamera.clearFlags = CameraClearFlags.SolidColor;
            heroCamera.backgroundColor = new Color(0.06f, 0.06f, 0.10f);
            heroCamera.nearClipPlane = 0.1f;
            heroCamera.farClipPlane = 500f;
            heroCamera.fieldOfView = 60;
            heroCamera.enabled = false; // desactive par defaut, active par Activate()

            cameraController = camGO.AddComponent<CameraController>();
            // CameraController.target est [SerializeField] private — il auto-find par tag "Player"
            // On force via reflection pour etre certain
            var targetField = typeof(CameraController).GetField("target",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetField != null)
                targetField.SetValue(cameraController, heroInstance.transform);
            cameraController.enabled = false; // desactive par defaut

            UnityEngine.Debug.Log($"[HeroDebugBridge] Heros instancie a {spawnPos}");
            return true;
        }

        Vector3 GetSpawnPosition(MapData map, MapGenConfig config)
        {
            Vector2Int spawnCell = map.spawnCell;
            if (spawnCell.x < 0 && map.rooms.Count > 0)
                spawnCell = map.rooms[0].center;

            float cx = spawnCell.x >= 0 ? spawnCell.x * config.cellSize : map.width * config.cellSize * 0.5f;
            float cz = spawnCell.y >= 0 ? spawnCell.y * config.cellSize : map.height * config.cellSize * 0.5f;

            // Hauteur de cellule comme estimation initiale
            var cell = spawnCell.x >= 0 ? map.GetCell(spawnCell.x, spawnCell.y) : null;
            float cellH = cell != null ? cell.floorHeight : 0f;

            // Raycast vers le bas depuis au-dessus pour trouver la vraie surface
            Vector3 rayOrigin = new Vector3(cx, cellH + 50f, cz);
            string method = "fallback";
            float resolvedY = cellH + 0.15f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f))
            {
                resolvedY = hit.point.y + 0.1f;
                method = "raycast";
                UnityEngine.Debug.Log($"[HeroDebugBridge] Spawn raycast hit '{hit.collider.gameObject.name}' " +
                    $"at Y={hit.point.y:F2} → spawnY={resolvedY:F2}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[HeroDebugBridge] Spawn raycast miss — " +
                    $"fallback cellH={cellH:F2} → spawnY={resolvedY:F2}");
            }

            UnityEngine.Debug.Log($"[HeroDebugBridge] Spawn cell=({spawnCell.x},{spawnCell.y}) " +
                $"method={method} resolvedY={resolvedY:F2} cellFloorH={cellH:F2}");

            return new Vector3(cx, resolvedY, cz);
        }

        void EnsureKeyBindingManager()
        {
            if (KeyBindingManager.Instance != null) return;
            var kbmGO = new GameObject("KeyBindingManager");
            kbmGO.AddComponent<KeyBindingManager>();
            UnityEngine.Debug.Log("[HeroDebugBridge] KeyBindingManager cree");
        }
    }
}
