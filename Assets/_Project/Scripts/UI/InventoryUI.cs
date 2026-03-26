using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DonGeonMaster.Inventory;
using DonGeonMaster.Equipment;

namespace DonGeonMaster.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject inventoryPanel;

        [Header("Left Side — Inventory")]
        [SerializeField] private Transform slotGrid;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private TextMeshProUGUI slotCounterText;

        [Header("Left — Tabs")]
        [SerializeField] private Button[] filterTabs;

        [Header("Left — Item Details")]
        [SerializeField] private GameObject detailsPanel;
        [SerializeField] private TextMeshProUGUI detailName;
        [SerializeField] private TextMeshProUGUI detailDescription;
        [SerializeField] private TextMeshProUGUI detailStats;
        [SerializeField] private Button btnEquip;
        [SerializeField] private Button btnDrop;

        [Header("Right Side — Equipment Slots")]
        [SerializeField] private EquipmentSlotUI[] equipmentSlots;

        [Header("Right — Stats")]
        [SerializeField] private StatsPanel statsPanel;

        private int activeTab; // 0=All, 1=Weapons, 2=Armor, 3=Accessories, 4=Consumables, 5=Materials
        private int selectedSlotIndex = -1;
        private List<InventorySlotUI> slotUIs = new();

        public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        public void Toggle()
        {
            if (inventoryPanel == null) return;
            bool opening = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(opening);
            if (opening)
            {
                activeTab = 0;
                selectedSlotIndex = -1;
                RefreshAll();
            }
        }

        public void Close()
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);
        }

        private void Start()
        {
            // Tab buttons — index maps to filter
            if (filterTabs != null)
            {
                for (int i = 0; i < filterTabs.Length; i++)
                {
                    int idx = i;
                    filterTabs[i].onClick.AddListener(() => SetFilter(idx));
                }
            }

            if (btnEquip != null) btnEquip.onClick.AddListener(OnEquipClicked);
            if (btnDrop != null) btnDrop.onClick.AddListener(OnDropClicked);

            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.OnInventoryChanged += RefreshAll;

            SetupEquipmentSlotCallbacks();

            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (detailsPanel != null) detailsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.OnInventoryChanged -= RefreshAll;
        }

        private void SetFilter(int tabIndex)
        {
            activeTab = tabIndex;
            selectedSlotIndex = -1;
            RefreshInventoryGrid();
            HideDetails();
        }

        private bool PassesFilter(InventorySlot slot)
        {
            if (activeTab == 0) return true;
            if (slot.IsEmpty) return false;

            switch (activeTab)
            {
                case 1: // Armes — weapons + shields
                    return slot.item is EquipmentData w &&
                           (w.slot == CharacterStandards.EquipmentSlot.Weapon ||
                            w.slot == CharacterStandards.EquipmentSlot.Shield);
                case 2: // Armure — all armor pieces (head, chest, legs, feet, belt, arms)
                    return slot.item is EquipmentData a &&
                           a.slot != CharacterStandards.EquipmentSlot.Weapon &&
                           a.slot != CharacterStandards.EquipmentSlot.Shield &&
                           a.slot != CharacterStandards.EquipmentSlot.Back;
                case 3: // Accessoires — back slot (capes, etc.)
                    return slot.item is EquipmentData ac &&
                           ac.slot == CharacterStandards.EquipmentSlot.Back;
                case 4: // Consommables
                    return slot.item.category == ItemCategory.Consumable;
                case 5: // Matériaux
                    return slot.item.category == ItemCategory.Material;
                default:
                    return true;
            }
        }

        public void RefreshAll()
        {
            RefreshInventoryGrid();
            RefreshEquipmentSlots();
            RefreshSlotCounter();
            if (statsPanel != null) statsPanel.RefreshStats();
        }

        private void RefreshInventoryGrid()
        {
            if (PlayerInventory.Instance == null) return;
            var allSlots = PlayerInventory.Instance.GetAllSlots();

            while (slotUIs.Count < allSlots.Count && slotPrefab != null && slotGrid != null)
            {
                var go = Instantiate(slotPrefab, slotGrid);
                var slotUI = go.GetComponent<InventorySlotUI>();
                if (slotUI != null)
                {
                    int index = slotUIs.Count;
                    slotUI.OnClicked += () => SelectSlot(index);
                    slotUIs.Add(slotUI);
                }
            }

            for (int i = 0; i < slotUIs.Count && i < allSlots.Count; i++)
            {
                var slot = allSlots[i];
                bool visible = PassesFilter(slot);
                slotUIs[i].SetSlot(slot, visible);
                slotUIs[i].SetSelected(i == selectedSlotIndex);
            }
        }

        private void RefreshEquipmentSlots()
        {
            if (equipmentSlots == null) return;
            var mem = Object.FindAnyObjectByType<ModularEquipmentManager>();
            if (mem == null) return;
            foreach (var eqSlot in equipmentSlots)
                eqSlot.Refresh(mem.GetEquipped(eqSlot.slot));
        }

        private void RefreshSlotCounter()
        {
            if (slotCounterText != null && PlayerInventory.Instance != null)
            {
                var inv = PlayerInventory.Instance;
                slotCounterText.text = $"{inv.UsedSlots}/{inv.MaxSlots}";
            }
        }

        private void SelectSlot(int index)
        {
            selectedSlotIndex = index;
            RefreshInventoryGrid();

            var slots = PlayerInventory.Instance.GetAllSlots();
            if (index >= 0 && index < slots.Count && !slots[index].IsEmpty)
            {
                ShowDetails(slots[index]);
                // Preview stat changes for equipment
                if (slots[index].item is EquipmentData eq && statsPanel != null)
                    statsPanel.ShowPreview(eq);
            }
            else
            {
                HideDetails();
                if (statsPanel != null) statsPanel.RefreshStats();
            }
        }

        private void OnEquipmentSlotClicked(CharacterStandards.EquipmentSlot slot)
        {
            // Unequip item from slot
            var em = Object.FindAnyObjectByType<ModularEquipmentManager>();
            if (em == null) return;

            var equipped = em.GetEquipped(slot);
            if (equipped != null)
            {
                em.Unequip(slot);
                if (PlayerInventory.Instance != null)
                    PlayerInventory.Instance.AddItem(equipped);
            }
            RefreshAll();
        }

        private void ShowDetails(InventorySlot slot)
        {
            if (detailsPanel == null) return;
            detailsPanel.SetActive(true);

            var item = slot.item;
            if (detailName != null)
            {
                detailName.text = $"{item.itemName} <size=18>({item.rarity})</size>";
                detailName.color = item.RarityColor;
            }
            if (detailDescription != null)
                detailDescription.text = item.description;
            if (detailStats != null)
                detailStats.text = GetStatsText(item);

            if (btnEquip != null)
            {
                var txt = btnEquip.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                    txt.text = item.category == ItemCategory.Equipment ? "Equiper" :
                               item.category == ItemCategory.Consumable ? "Utiliser" : "";
                btnEquip.gameObject.SetActive(item.category != ItemCategory.Material);
            }
        }

        private void HideDetails()
        {
            if (detailsPanel != null) detailsPanel.SetActive(false);
        }

        private void OnEquipClicked()
        {
            if (selectedSlotIndex >= 0 && PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.UseItem(selectedSlotIndex);
                RefreshAll();
                HideDetails();
            }
        }

        private void OnDropClicked()
        {
            if (selectedSlotIndex >= 0 && PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.DropItem(selectedSlotIndex);
                HideDetails();
            }
        }

        /// <summary>
        /// Called by ProjectSetup to wire equipment slot callbacks.
        /// </summary>
        public void SetupEquipmentSlotCallbacks()
        {
            if (equipmentSlots == null) return;
            foreach (var eqSlot in equipmentSlots)
                eqSlot.Setup(eqSlot.slot, OnEquipmentSlotClicked);
        }

        private string GetStatsText(ItemData item)
        {
            if (item is EquipmentData eq)
            {
                string stats = "";
                if (eq.damage > 0) stats += $"ATK +{eq.damage}  ";
                if (eq.armor > 0) stats += $"DEF +{eq.armor}  ";
                if (eq.attackSpeed != 1f) stats += $"SPD x{eq.attackSpeed:F1}  ";
                return stats + $"\nSlot: {eq.slot}  Valeur: {eq.sellValue}";
            }
            if (item is ConsumableData con)
                return $"{con.effect} +{con.value}" + (con.duration > 0 ? $" ({con.duration}s)" : "");
            if (item is MaterialData mat)
                return $"Type: {mat.materialType}  Lvl: {mat.craftLevel}";
            return "";
        }
    }
}
