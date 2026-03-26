namespace DonGeonMaster.Inventory
{
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;

        public bool IsEmpty => item == null || quantity <= 0;

        public InventorySlot()
        {
            item = null;
            quantity = 0;
        }

        public InventorySlot(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }

        public int AddQuantity(int amount)
        {
            if (item == null) return amount;
            int canAdd = item.maxStack - quantity;
            int toAdd = UnityEngine.Mathf.Min(amount, canAdd);
            quantity += toAdd;
            return amount - toAdd; // Return leftover
        }

        public int RemoveQuantity(int amount)
        {
            int toRemove = UnityEngine.Mathf.Min(amount, quantity);
            quantity -= toRemove;
            if (quantity <= 0) Clear();
            return toRemove;
        }
    }
}
