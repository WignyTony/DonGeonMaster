using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DonGeonMaster.Inventory;

namespace DonGeonMaster.UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image rarityBorderImage;
        [SerializeField] private Button button;

        public event Action OnClicked;

        private static readonly Color selectedBorder = new Color(0.3f, 0.8f, 0.9f, 1f);

        private bool isSelected;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => OnClicked?.Invoke());
        }

        public void SetSlot(InventorySlot slot, bool visible)
        {
            gameObject.SetActive(visible);
            if (!visible) return;

            if (slot.IsEmpty)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (quantityText != null) quantityText.text = "";
                if (rarityBorderImage != null) rarityBorderImage.enabled = false;
            }
            else
            {
                if (iconImage != null)
                {
                    iconImage.enabled = slot.item.icon != null;
                    iconImage.sprite = slot.item.icon;
                    iconImage.preserveAspect = true;
                    iconImage.color = Color.white;
                }
                if (quantityText != null)
                    quantityText.text = slot.quantity > 1 ? "x" + slot.quantity : "";

                // Rarity colored border overlay
                if (rarityBorderImage != null)
                {
                    Color rc = slot.item.RarityColor;
                    bool showBorder = slot.item.rarity > ItemRarity.Common;
                    rarityBorderImage.enabled = showBorder;
                    if (showBorder)
                        rarityBorderImage.color = new Color(rc.r, rc.g, rc.b, 0.75f);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (rarityBorderImage != null && selected)
            {
                rarityBorderImage.enabled = true;
                rarityBorderImage.color = selectedBorder;
            }
        }
    }
}
