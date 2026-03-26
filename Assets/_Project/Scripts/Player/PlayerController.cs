using UnityEngine;
using UnityEngine.InputSystem;
using DonGeonMaster.UI;
using DonGeonMaster.Inventory;

namespace DonGeonMaster.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpForce = 7f;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isRunning;
        private bool isGrounded;
        private float attackCooldown;

        public bool IsMoving { get; private set; }
        public bool IsRunning => isRunning;
        public bool IsGrounded => isGrounded;
        public bool IsAttacking { get; private set; }
        public bool IsJumping { get; private set; }
        public Vector3 MoveDirection { get; private set; }

        [Header("UI")]
        [SerializeField] private InventoryUI inventoryUI;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            // Dynamic lookup in case serialized reference is lost after scene reload
            if (inventoryUI == null)
                inventoryUI = FindAnyObjectByType<InventoryUI>();
        }

        private void Update()
        {
            // Toggle inventory
            var kbm = KeyBindingManager.Instance;
            if (kbm != null && kbm.GetKeyDown("Inventory"))
            {
                if (inventoryUI != null)
                    inventoryUI.Toggle();
            }

            // Don't move if inventory is open
            if (inventoryUI != null && inventoryUI.IsOpen) return;

            HandleMovement();
            HandleActions();
        }

        private void HandleMovement()
        {
            if (KeyBindingManager.Instance == null) return;
            var kbm = KeyBindingManager.Instance;

            isGrounded = controller.isGrounded;

            float h = 0f, v = 0f;
            if (kbm.GetKey("MoveForward")) v += 1f;
            if (kbm.GetKey("MoveBack")) v -= 1f;
            if (kbm.GetKey("MoveLeft")) h -= 1f;
            if (kbm.GetKey("MoveRight")) h += 1f;

            isRunning = kbm.GetKey("Run");
            float speed = isRunning ? runSpeed : walkSpeed;

            Vector3 moveDir = new Vector3(h, 0f, v).normalized;
            MoveDirection = moveDir;
            IsMoving = moveDir.magnitude > 0.1f;

            if (IsMoving)
            {
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.LerpAngle(
                    transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

                Vector3 move = moveDir * speed;
                controller.Move(move * Time.deltaTime);
            }

            // Gravity + landing (BEFORE jump so jump can override)
            if (isGrounded)
            {
                if (velocity.y < 0)
                    velocity.y = -2f;
                if (IsJumping)
                    IsJumping = false; // Landed
            }

            // Jump
            if (isGrounded && kbm.GetKeyDown("Jump"))
            {
                velocity.y = jumpForce;
                IsJumping = true;
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleActions()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            IsAttacking = false;
            if (attackCooldown > 0) attackCooldown -= Time.deltaTime;
            if (mouse.leftButton.wasPressedThisFrame && attackCooldown <= 0)
            {
                IsAttacking = true;
                attackCooldown = 0.5f;
            }
        }

        public void TakeHit() { }
        public void Die() { enabled = false; }
    }
}
