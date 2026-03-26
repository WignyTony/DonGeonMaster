using UnityEngine;

namespace DonGeonMaster.Inventory
{
    [CreateAssetMenu(fileName = "NewMaterial", menuName = "DonGeonMaster/Material")]
    public class MaterialData : ItemData
    {
        [Header("Material")]
        public string materialType;
        public int craftLevel;

        private void OnEnable()
        {
            category = ItemCategory.Material;
            stackable = true;
            if (maxStack <= 1) maxStack = 999;
        }
    }
}
