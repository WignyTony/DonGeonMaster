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
        [SerializeField] private Button button;

        public event Action OnClicked;

        private static readonly Color emptyBg = new Color(0.12f, 0.10f, 0.08f, 0.5f);
        private static readonly Color selectedBorder = new Color(0.3f, 0.8f, 0.9f, 1f); // Cyan accent

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
                if (borderImage != null) borderImage.color = emptyBg;
            }
            else
            {
                if (iconImage != null)
                {
                    iconImage.enabled = slot.item.icon != null;
                    iconImage.sprite = slot.item.icon;
                    iconImage.color = Color.white;
                }
                if (quantityText != null)
                    quantityText.text = slot.quantity > 1 ? "x" + slot.quantity : "";
                // Rarity-colored background (dark tinted version)
                if (borderImage != null && !isSelected)
                {
                    Color rc = slot.item.RarityColor;
                    borderImage.color = new Color(rc.r * 0.2f + 0.08f, rc.g * 0.2f + 0.08f, rc.b * 0.2f + 0.08f, 0.9f);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (borderImage != null)
                borderImage.color = selected ? selectedBorder : emptyBg;
        }
    }
}
