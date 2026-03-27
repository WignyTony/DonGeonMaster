using UnityEngine;
using UnityEngine.InputSystem;

namespace DonGeonMaster.MapGeneration
{
    public class PlayerSpawnService : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] GameObject playerPrefab;
        [SerializeField] GameObject ganzsePrefab;

        [Header("Spawn")]
        [SerializeField] float spawnHeight = 0.1f;
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

            if (playerPrefab != null)
                currentPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            else
                currentPlayer = CreateDebugPlayer(spawnPos);

            currentPlayer.name = "DebugPlayer";
            currentPlayer.tag = "Player";
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
            if (map.spawnCell.x >= 0)
                return CellToWorld(map.spawnCell, config);

            foreach (var room in map.rooms)
                return CellToWorld(room.center, config);

            var walkable = map.GetAllWalkableCells();
            if (walkable.Count > 0)
                return CellToWorld(walkable[Random.Range(0, walkable.Count)], config);

            return new Vector3(
                config.mapWidth * config.cellSize * 0.5f,
                spawnHeight,
                config.mapHeight * config.cellSize * 0.5f);
        }

        Vector3 CellToWorld(Vector2Int cell, MapGenConfig config)
        {
            return new Vector3(cell.x * config.cellSize, spawnHeight, cell.y * config.cellSize);
        }

        /// <summary>
        /// Cree le joueur debug. Priorite : prefab GanzSe (vrai personnage du jeu).
        /// Fallback : capsule primitive si GanzSe absent.
        /// </summary>
        GameObject CreateDebugPlayer(Vector3 position)
        {
            GameObject player;

            if (ganzsePrefab != null)
            {
                // Vrai personnage GanzSe comme dans le Hub
                player = Instantiate(ganzsePrefab, position, Quaternion.identity);
                DonGeonMaster.Character.GanzSeHelper.DisableAllArmor(player);

                // CharacterController identique au vrai jeu
                var cc = player.GetComponent<CharacterController>();
                if (cc == null)
                {
                    cc = player.AddComponent<CharacterController>();
                    cc.height = 1.7f;
                    cc.radius = 0.3f;
                    cc.center = new Vector3(0, 0.85f, 0);
                }
            }
            else
            {
                // Fallback : capsule simple (pas de Shader.Find pour eviter le rose)
                player = new GameObject("DebugPlayer");
                player.transform.position = position;

                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.SetParent(player.transform);
                body.transform.localPosition = Vector3.up;
                body.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
                Destroy(body.GetComponent<Collider>());
                // Pas de material custom : garder le Default-Material gris (jamais rose)

                var cc = player.AddComponent<CharacterController>();
                cc.height = 2f;
                cc.radius = 0.3f;
                cc.center = Vector3.up;
            }

            player.AddComponent<DebugTopDownMovement>();
            return player;
        }
    }

    /// <summary>
    /// Mouvement debug top-down identique au vrai PlayerController :
    /// WASD = deplacement, Shift = courir, rotation smooth, gravite.
    /// </summary>
    public class DebugTopDownMovement : MonoBehaviour
    {
        public float walkSpeed = 3.5f;
        public float runSpeed = 6f;
        public float rotationSpeed = 10f;
        public float gravity = -20f;
        public float jumpForce = 7f;

        CharacterController cc;
        Vector3 velocity;

        void Awake() => cc = GetComponent<CharacterController>();

        void Update()
        {
            if (!enabled || cc == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = 0, v = 0;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v = 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v = -1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h = -1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h = 1;

            Vector3 moveDir = new Vector3(h, 0, v).normalized;
            float speed = kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;

            if (moveDir.magnitude > 0.1f)
            {
                cc.Move(moveDir * (speed * Time.deltaTime));
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.LerpAngle(
                    transform.eulerAngles.y, targetAngle, Time.deltaTime * rotationSpeed);
                transform.rotation = Quaternion.Euler(0, smoothAngle, 0);
            }

            if (cc.isGrounded)
            {
                velocity.y = -1f;
                if (kb.spaceKey.wasPressedThisFrame)
                    velocity.y = jumpForce;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }

            cc.Move(velocity * Time.deltaTime);
        }
    }
}
