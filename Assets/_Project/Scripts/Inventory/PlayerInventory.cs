using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DonGeonMaster.Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private int maxSlots = 200;

        private List<InventorySlot> slots = new();

        public event Action OnInventoryChanged;

        public int MaxSlots => maxSlots;
        public int UsedSlots => slots.Count(s => !s.IsEmpty);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize empty slots
            for (int i = 0; i < maxSlots; i++)
                slots.Add(new InventorySlot());
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            int remaining = quantity;

            // First: try to stack with existing slots
            if (item.stackable)
            {
                foreach (var slot in slots)
                {
                    if (!slot.IsEmpty && slot.item == item && slot.quantity < item.maxStack)
                    {
                        remaining = slot.AddQuantity(remaining);
                        if (remaining <= 0) break;
                    }
                }
            }

            // Then: fill empty slots
            while (remaining > 0)
            {
                var emptySlot = slots.FirstOrDefault(s => s.IsEmpty);
                if (emptySlot == null)
                {
                    Debug.LogWarning("[Inventory] Full! Cannot add " + item.itemName);
                    OnInventoryChanged?.Invoke();
                    return false;
                }

                int toAdd = Mathf.Min(remaining, item.maxStack);
                emptySlot.item = item;
                emptySlot.quantity = toAdd;
                remaining -= toAdd;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            int remaining = quantity;

            // Remove from slots (last to first to preserve order)
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (!slots[i].IsEmpty && slots[i].item == item)
                {
                    remaining -= slots[i].RemoveQuantity(remaining);
                    if (remaining <= 0) break;
                }
            }

            OnInventoryChanged?.Invoke();
            return remaining <= 0;
        }

        public bool UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            var slot = slots[slotIndex];
            if (slot.IsEmpty) return false;

            var item = slot.item;

            switch (item)
            {
                case ConsumableData consumable:
                    ApplyConsumable(consumable);
                    slot.RemoveQuantity(1);
                    OnInventoryChanged?.Invoke();
                    return true;

                case Equipment.EquipmentData equipment:
                    var mem = Equipment.ModularEquipmentManager.FindAnyObjectByType<Equipment.ModularEquipmentManager>();
                    if (mem != null)
                    {
                        var currentlyEquipped = mem.GetEquipped(equipment.slot);
                        if (currentlyEquipped != null)
                            AddItem(currentlyEquipped);
                        mem.Equip(equipment);
                        slot.RemoveQuantity(1);
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                    return false;

                default:
                    Debug.Log("[Inventory] Cannot use " + item.itemName);
                    return false;
            }
        }

        public void DropItem(int slotIndex, int quantity = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            var slot = slots[slotIndex];
            if (slot.IsEmpty) return;

            slot.RemoveQuantity(quantity);
            OnInventoryChanged?.Invoke();
        }

        public List<InventorySlot> GetAllSlots() => slots;

        public List<InventorySlot> GetSlotsByCategory(ItemCategory category)
        {
            return slots.Where(s => !s.IsEmpty && s.item.category == category).ToList();
        }

        public bool HasItem(ItemData item, int quantity = 1)
        {
            int total = slots.Where(s => !s.IsEmpty && s.item == item).Sum(s => s.quantity);
            return total >= quantity;
        }

        public int GetItemCount(ItemData item)
        {
            return slots.Where(s => !s.IsEmpty && s.item == item).Sum(s => s.quantity);
        }

        private void ApplyConsumable(ConsumableData consumable)
        {
            // TODO: Apply actual effects when health/mana systems exist
            Debug.Log($"[Inventory] Used {consumable.itemName}: {consumable.effect} +{consumable.value}");
        }
    }
}
