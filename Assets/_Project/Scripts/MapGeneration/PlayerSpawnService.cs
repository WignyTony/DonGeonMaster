using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    public class PlayerSpawnService : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] GameObject playerPrefab;
        [SerializeField] float spawnHeight = 1.5f;
        [SerializeField] float collisionCheckRadius = 0.5f;
        [SerializeField] int maxFallbackAttempts = 20;

        GameObject currentPlayer;
        MapData currentMap;
        MapGenConfig currentConfig;

        public GameObject CurrentPlayer => currentPlayer;

        public GameObject SpawnPlayer(MapData map, MapGenConfig config)
        {
            currentMap = map;
            currentConfig = config;

            DespawnPlayer();

            Vector3 spawnPos = FindValidSpawnPosition(map, config);
            if (spawnPos == Vector3.negativeInfinity)
            {
                Debug.LogError("[PlayerSpawnService] Impossible de trouver un spawn valide");
                return null;
            }

            if (playerPrefab != null)
            {
                currentPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                currentPlayer = CreateDebugPlayer(spawnPos);
            }

            currentPlayer.name = "DebugPlayer";
            Debug.Log($"[PlayerSpawnService] Joueur spawné à {spawnPos}");
            return currentPlayer;
        }

        public void DespawnPlayer()
        {
            if (currentPlayer != null)
            {
                DestroyImmediate(currentPlayer);
                currentPlayer = null;
            }
        }

        public void RespawnPlayer()
        {
            if (currentMap != null && currentConfig != null)
                SpawnPlayer(currentMap, currentConfig);
        }

        Vector3 FindValidSpawnPosition(MapData map, MapGenConfig config)
        {
            // Essayer le spawn principal
            if (map.spawnCell.x >= 0)
            {
                Vector3 primary = CellToWorld(map.spawnCell, config);
                if (IsPositionValid(primary))
                    return primary;
            }

            // Fallback: chercher dans les salles
            foreach (var room in map.rooms)
            {
                Vector3 roomCenter = CellToWorld(room.center, config);
                if (IsPositionValid(roomCenter))
                    return roomCenter;
            }

            // Fallback: chercher des cellules marchables
            var walkable = map.GetAllWalkableCells();
            for (int i = 0; i < Mathf.Min(maxFallbackAttempts, walkable.Count); i++)
            {
                int idx = Random.Range(0, walkable.Count);
                Vector3 pos = CellToWorld(walkable[idx], config);
                if (IsPositionValid(pos))
                    return pos;
            }

            // Dernier recours: centre de la map
            Vector3 center = new Vector3(
                config.mapWidth * config.cellSize * 0.5f,
                spawnHeight,
                config.mapHeight * config.cellSize * 0.5f);
            return center;
        }

        Vector3 CellToWorld(Vector2Int cell, MapGenConfig config)
        {
            return new Vector3(
                cell.x * config.cellSize,
                spawnHeight,
                cell.y * config.cellSize);
        }

        bool IsPositionValid(Vector3 pos)
        {
            return !Physics.CheckSphere(pos, collisionCheckRadius);
        }

        GameObject CreateDebugPlayer(Vector3 position)
        {
            var player = new GameObject("DebugPlayer");
            player.transform.position = position;

            // Capsule visuelle
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(player.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            var renderer = body.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = new Color(0.2f, 0.8f, 0.2f);

            // CharacterController simple
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = Vector3.zero;

            // Script de mouvement debug
            player.AddComponent<DebugPlayerMovement>();

            // Indicateur de direction (petit cube devant)
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.transform.SetParent(player.transform);
            indicator.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            indicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.3f);
            var indRenderer = indicator.GetComponent<Renderer>();
            indRenderer.material = renderer.material;
            indRenderer.material.color = Color.yellow;

            // Retirer les colliders des primitives (on utilise le CharacterController)
            Destroy(body.GetComponent<Collider>());
            Destroy(indicator.GetComponent<Collider>());

            return player;
        }
    }

    public class DebugPlayerMovement : MonoBehaviour
    {
        public float moveSpeed = 12f;
        public float lookSpeed = 3f;
        float yaw, pitch;
        CharacterController cc;

        void Start()
        {
            cc = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
            }

            if (Cursor.lockState != CursorLockMode.Locked) return;

            yaw += Input.GetAxis("Mouse X") * lookSpeed;
            pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = transform.right * h + transform.forward * v;
            move.y = 0;

            if (Input.GetKey(KeyCode.Space)) move.y = 0.5f;
            if (Input.GetKey(KeyCode.LeftControl)) move.y = -0.5f;

            if (!cc.enabled) return;
            cc.Move(move * (moveSpeed * Time.deltaTime));

            // Gravity
            if (!cc.isGrounded)
                cc.Move(Vector3.down * (9.81f * Time.deltaTime));
        }
    }
}
