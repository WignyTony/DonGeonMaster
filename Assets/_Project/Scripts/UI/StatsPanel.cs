using UnityEngine;
using TMPro;
using DonGeonMaster.Equipment;
using DonGeonMaster.Inventory;

namespace DonGeonMaster.UI
{
    public class StatsPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI magText;
        [SerializeField] private TextMeshProUGUI spdText;
        [SerializeField] private TextMeshProUGUI crtText;
        [SerializeField] private TextMeshProUGUI vitText;

        // Base stats (can be set from player stats system later)
        private int baseAtk = 10, baseDef = 5, baseMag = 3;
        private int baseSpd = 20, baseCrt = 5, baseVit = 100;

        public void RefreshStats()
        {
            var em = Object.FindAnyObjectByType<ModularEquipmentManager>();
            int bonusAtk = 0, bonusDef = 0;
            float bonusSpd = 0;

            if (em != null)
            {
                foreach (CharacterStandards.EquipmentSlot slot in System.Enum.GetValues(typeof(CharacterStandards.EquipmentSlot)))
                {
                    var eq = em.GetEquipped(slot);
                    if (eq != null)
                    {
                        float rm = ItemData.RarityMultiplier(eq.rarity);
                        bonusAtk += Mathf.RoundToInt(eq.damage * rm);
                        bonusDef += Mathf.RoundToInt(eq.armor * rm);
                        bonusSpd += (eq.attackSpeed - 1f) * 10f;
                        bonusSpd += (eq.moveSpeedModifier - 1f) * 10f;
                    }
                }
            }

            SetStat(atkText, "ATK", baseAtk + bonusAtk);
            SetStat(defText, "DEF", baseDef + bonusDef);
            SetStat(magText, "MAG", baseMag);
            SetStat(spdText, "SPD", baseSpd + (int)bonusSpd);
            SetStat(crtText, "CRT", baseCrt);
            SetStat(vitText, "VIT", baseVit);
        }

        public void ShowPreview(EquipmentData item)
        {
            if (item == null) { RefreshStats(); return; }

            var em = Object.FindAnyObjectByType<ModularEquipmentManager>();

            // Calculate current totals from ALL equipped slots (same as RefreshStats)
            int totalAtk = baseAtk, totalDef = baseDef;
            float totalSpd = baseSpd;

            if (em != null)
            {
                foreach (CharacterStandards.EquipmentSlot slot in System.Enum.GetValues(typeof(CharacterStandards.EquipmentSlot)))
                {
                    var eq = em.GetEquipped(slot);
                    if (eq != null)
                    {
                        float rm = ItemData.RarityMultiplier(eq.rarity);
                        totalAtk += Mathf.RoundToInt(eq.damage * rm);
                        totalDef += Mathf.RoundToInt(eq.armor * rm);
                        totalSpd += (eq.attackSpeed - 1f) * 10f + (eq.moveSpeedModifier - 1f) * 10f;
                    }
                }
            }

            // Calculate the diff: new item vs currently equipped in that slot
            var current = em != null ? em.GetEquipped(item.slot) : null;
            float rmNew = ItemData.RarityMultiplier(item.rarity);
            float rmCur = current != null ? ItemData.RarityMultiplier(current.rarity) : 1f;

            int diffAtk = Mathf.RoundToInt(item.damage * rmNew) - (current != null ? Mathf.RoundToInt(current.damage * rmCur) : 0);
            int diffDef = Mathf.RoundToInt(item.armor * rmNew) - (current != null ? Mathf.RoundToInt(current.armor * rmCur) : 0);
            int diffSpd = (int)(((item.attackSpeed - 1f) * 10f + (item.moveSpeedModifier - 1f) * 10f) -
                           (current != null ? (current.attackSpeed - 1f) * 10f + (current.moveSpeedModifier - 1f) * 10f : 0));

            SetStatPreview(atkText, "ATK", totalAtk, diffAtk);
            SetStatPreview(defText, "DEF", totalDef, diffDef);
            SetStatPreview(spdText, "SPD", (int)totalSpd, diffSpd);
        }

        private void SetStat(TextMeshProUGUI text, string label, int value)
        {
            if (text != null)
                text.text = $"{label}: {value}";
        }

        private void SetStatPreview(TextMeshProUGUI text, string label, int current, int diff)
        {
            if (text == null) return;
            if (diff > 0)
                text.text = $"{label}: {current} <color=#44CC44>+{diff}</color>";
            else if (diff < 0)
                text.text = $"{label}: {current} <color=#CC4444>{diff}</color>";
            else
                text.text = $"{label}: {current}";
        }
    }
}
