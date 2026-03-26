using UnityEngine;
using DonGeonMaster.Inventory;

namespace DonGeonMaster.Equipment
{
    [CreateAssetMenu(fileName = "NewEquipment", menuName = "DonGeonMaster/Equipment Data")]
    public class EquipmentData : ItemData
    {
        [Header("Slot")]
        public CharacterStandards.EquipmentSlot slot;

        [Header("Classification")]
        public CharacterStandards.EquipmentWeight weight;
        public CharacterStandards.WeaponType weaponType;
        public CharacterStandards.ArmorMaterial armorMaterial;
        public CharacterStandards.Handling handling;
        public CharacterStandards.ElementType element;

        [Header("GanzSe Modular")]
        [Tooltip("Name of the child GameObject in the GanzSe character to activate")]
        public string armorPartName;

        [Header("Visuals (legacy)")]
        public GameObject meshPrefab;

        [Header("Weapon Offset (set by AnimationPreview > Valider)")]
        public Vector3 weaponPosOffset;
        public Vector3 weaponRotOffset;
        public Vector3 weaponScaleOverride = Vector3.one;
        public bool hasCustomOffset;

        [Header("Stats")]
        public int armor;
        public int damage;
        public float attackSpeed = 1f;
        public float moveSpeedModifier = 1f;

        private void OnEnable()
        {
            category = ItemCategory.Equipment;
            stackable = false;
            maxStack = 1;
        }
    }
}
