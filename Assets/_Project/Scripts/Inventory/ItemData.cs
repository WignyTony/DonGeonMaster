using UnityEngine;

namespace DonGeonMaster.Inventory
{
    public enum ItemCategory { Equipment, Consumable, Material }
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

    public abstract class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Classification")]
        public ItemCategory category;
        public ItemRarity rarity;

        [Header("Stacking")]
        public bool stackable;
        public int maxStack = 1;

        [Header("Value")]
        public int sellValue;

        public Color RarityColor => rarity switch
        {
            ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),
            ItemRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            ItemRarity.Rare => new Color(0.3f, 0.4f, 1.0f),
            ItemRarity.Epic => new Color(0.7f, 0.3f, 0.9f),
            ItemRarity.Legendary => new Color(1.0f, 0.8f, 0.2f),
            ItemRarity.Mythic => new Color(0.8f, 0.1f, 0.15f),
            _ => Color.white
        };

        public static float RarityMultiplier(ItemRarity r) => r switch
        {
            ItemRarity.Common => 1.00f,
            ItemRarity.Uncommon => 1.15f,
            ItemRarity.Rare => 1.30f,
            ItemRarity.Epic => 1.50f,
            ItemRarity.Legendary => 1.75f,
            ItemRarity.Mythic => 2.00f,
            _ => 1f
        };
    }
}
