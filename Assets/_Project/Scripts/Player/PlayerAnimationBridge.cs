using UnityEngine;
using DonGeonMaster.Equipment;

namespace DonGeonMaster.Player
{
    /// <summary>
    /// Bridges PlayerController state to Animator parameters each frame.
    /// Chooses attack animation based on equipped weapon handling.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationBridge : MonoBehaviour
    {
        private Animator animator;
        private PlayerController playerController;
        private ModularEquipmentManager equipmentManager;

        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int IsRunning = Animator.StringToHash("IsRunning");
        private static readonly int IsJumping = Animator.StringToHash("IsJumping");
        private static readonly int AttackSlow = Animator.StringToHash("AttackSlow");
        private static readonly int AttackFast = Animator.StringToHash("AttackFast");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            equipmentManager = GetComponent<ModularEquipmentManager>();
        }

        private void Update()
        {
            if (animator == null || playerController == null) return;

            animator.SetBool(IsMoving, playerController.IsMoving);
            animator.SetBool(IsRunning, playerController.IsRunning);
            animator.SetBool(IsJumping, playerController.IsJumping);

            if (playerController.IsAttacking)
            {
                // Slow/Normal/Very slow → Attack 1, Fast/Very fast → Attack 3
                bool useFast = false;
                if (equipmentManager != null)
                {
                    var weapon = equipmentManager.GetEquipped(CharacterStandards.EquipmentSlot.Weapon);
                    if (weapon != null)
                        useFast = weapon.handling == CharacterStandards.Handling.Rapide ||
                                  weapon.handling == CharacterStandards.Handling.TresRapide;
                }
                animator.SetTrigger(useFast ? AttackFast : AttackSlow);
            }
        }
    }
}
