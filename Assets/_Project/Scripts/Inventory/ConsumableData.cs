using UnityEngine;

namespace DonGeonMaster.Inventory
{
    public enum ConsumableEffect { Heal, Mana, Buff, Cure }

    [CreateAssetMenu(fileName = "NewConsumable", menuName = "DonGeonMaster/Consumable")]
    public class ConsumableData : ItemData
    {
        [Header("Effect")]
        public ConsumableEffect effect;
        public float value;
        public float duration;
        public string buffName;

        private void OnEnable()
        {
            category = ItemCategory.Consumable;
            stackable = true;
            if (maxStack <= 1) maxStack = 99;
        }
    }
}
