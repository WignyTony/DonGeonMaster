using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DonGeonMaster.Equipment;
using DonGeonMaster.Inventory;

namespace DonGeonMaster.UI
{
    public class EquipmentSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image glowImage;
        [SerializeField] private Image placeholderIcon;
        [SerializeField] private TextMeshProUGUI slotLabel;
        [SerializeField] private Button button;

        public CharacterStandards.EquipmentSlot slot;

        private EquipmentData equippedItem;
        private System.Action<CharacterStandards.EquipmentSlot> onClicked;

        private static readonly Color emptyColor = new Color(0.12f, 0.10f, 0.08f, 0.6f);
        private static readonly Color borderColor = new Color(0.25f, 0.22f, 0.18f, 0.8f);

        public void Setup(CharacterStandards.EquipmentSlot slotType, System.Action<CharacterStandards.EquipmentSlot> callback)
        {
            slot = slotType;
            onClicked = callback;
            if (slotLabel != null)
                slotLabel.text = SlotDisplayName(slotType);
            if (button != null)
                button.onClick.AddListener(() => onClicked?.Invoke(slot));
            Refresh(null);
        }

        public void Refresh(EquipmentData item)
        {
            equippedItem = item;

            if (item == null)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (placeholderIcon != null) placeholderIcon.enabled = true;
                if (borderImage != null) borderImage.color = emptyColor;
                if (glowImage != null) glowImage.enabled = false;
            }
            else
            {
                if (placeholderIcon != null) placeholderIcon.enabled = false;
                if (iconImage != null)
                {
                    iconImage.enabled = item.icon != null;
                    iconImage.sprite = item.icon;
                }
                if (borderImage != null)
                    borderImage.color = item.RarityColor * 0.7f + Color.white * 0.3f;
                if (glowImage != null)
                {
                    bool hasGlow = item.rarity >= ItemRarity.Rare;
                    glowImage.enabled = hasGlow;
                    if (hasGlow)
                        glowImage.color = new Color(item.RarityColor.r, item.RarityColor.g, item.RarityColor.b, 0.3f);
                }
            }
        }

        private static string SlotDisplayName(CharacterStandards.EquipmentSlot s) => s switch
        {
            CharacterStandards.EquipmentSlot.Head => "Tete",
            CharacterStandards.EquipmentSlot.Chest => "Torse",
            CharacterStandards.EquipmentSlot.Legs => "Jambes",
            CharacterStandards.EquipmentSlot.Feet => "Bottes",
            CharacterStandards.EquipmentSlot.Weapon => "Arme",
            CharacterStandards.EquipmentSlot.Shield => "Bouclier",
            CharacterStandards.EquipmentSlot.Back => "Dos",
            CharacterStandards.EquipmentSlot.Belt => "Ceinture",
            CharacterStandards.EquipmentSlot.Arms => "Brassards",
            _ => s.ToString()
        };
    }
}
