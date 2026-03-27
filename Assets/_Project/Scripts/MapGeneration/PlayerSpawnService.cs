using UnityEngine;
using UnityEngine.InputSystem;

namespace DonGeonMaster.MapGeneration
{
    public class PlayerSpawnService : MonoBehaviour
    {
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
            {
                Vector3 primary = CellToWorld(map.spawnCell, config);
                if (!Physics.CheckSphere(primary, collisionCheckRadius))
                    return primary;
            }

            foreach (var room in map.rooms)
            {
                Vector3 pos = CellToWorld(room.center, config);
                if (!Physics.CheckSphere(pos, collisionCheckRadius))
                    return pos;
            }

            var walkable = map.GetAllWalkableCells();
            for (int i = 0; i < Mathf.Min(maxFallbackAttempts, walkable.Count); i++)
            {
                Vector3 pos = CellToWorld(walkable[Random.Range(0, walkable.Count)], config);
                if (!Physics.CheckSphere(pos, collisionCheckRadius))
                    return pos;
            }

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
        /// Cree un joueur debug qui reproduit le gameplay top-down du vrai PlayerController :
        /// WASD pour bouger, Shift pour courir, rotation smooth vers la direction, gravite.
        /// Pas de mouse look FPS — coherent avec le jeu.
        /// </summary>
        GameObject CreateDebugPlayer(Vector3 position)
        {
            var player = new GameObject("DebugPlayer");
            player.transform.position = position;

            // Visuel : capsule verte
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(player.transform);
            body.transform.localPosition = Vector3.up; // centrer la capsule
            body.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            Destroy(body.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.8f, 0.3f);
            body.GetComponent<Renderer>().material = mat;

            // Indicateur de direction (cone devant)
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.transform.SetParent(player.transform);
            indicator.transform.localPosition = new Vector3(0, 1f, 0.6f);
            indicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.3f);
            Destroy(indicator.GetComponent<Collider>());
            indicator.GetComponent<Renderer>().material = mat;
            indicator.GetComponent<Renderer>().material.color = Color.yellow;

            // CharacterController
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.3f;
            cc.center = Vector3.up;

            // Script de mouvement top-down (comme le vrai jeu)
            player.AddComponent<DebugTopDownMovement>();

            return player;
        }
    }

    /// <summary>
    /// Mouvement debug top-down identique au vrai PlayerController :
    /// WASD = deplacement, Shift = courir, rotation smooth vers la direction.
    /// Utilise InputSystem directement (pas KeyBindingManager pour eviter les dependances).
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

        void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (!enabled || cc == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // Mouvement WASD (axes monde, pas relatif a la camera)
            float h = 0, v = 0;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v = 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v = -1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h = -1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h = 1;

            Vector3 moveDir = new Vector3(h, 0, v).normalized;
            bool running = kb.leftShiftKey.isPressed;
            float speed = running ? runSpeed : walkSpeed;

            // Deplacement horizontal
            if (moveDir.magnitude > 0.1f)
            {
                cc.Move(moveDir * (speed * Time.deltaTime));

                // Rotation smooth vers la direction de mouvement
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.LerpAngle(
                    transform.eulerAngles.y, targetAngle, Time.deltaTime * rotationSpeed);
                transform.rotation = Quaternion.Euler(0, smoothAngle, 0);
            }

            // Saut
            if (cc.isGrounded)
            {
                velocity.y = -1f; // petite force vers le bas pour rester ground
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
