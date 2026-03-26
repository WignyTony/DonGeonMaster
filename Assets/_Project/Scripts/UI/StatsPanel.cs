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
            var current = em != null ? em.GetEquipped(item.slot) : null;

            float rmNew = ItemData.RarityMultiplier(item.rarity);
            float rmCur = current != null ? ItemData.RarityMultiplier(current.rarity) : 1f;

            int curAtk = current != null ? Mathf.RoundToInt(current.damage * rmCur) : 0;
            int curDef = current != null ? Mathf.RoundToInt(current.armor * rmCur) : 0;
            float curSpd = current != null ? (current.attackSpeed - 1f) * 10f + (current.moveSpeedModifier - 1f) * 10f : 0;

            int newAtk = Mathf.RoundToInt(item.damage * rmNew);
            int newDef = Mathf.RoundToInt(item.armor * rmNew);
            float newSpd = (item.attackSpeed - 1f) * 10f + (item.moveSpeedModifier - 1f) * 10f;

            int diffAtk = newAtk - curAtk;
            int diffDef = newDef - curDef;
            int diffSpd = (int)(newSpd - curSpd);

            SetStatPreview(atkText, "ATK", baseAtk + curAtk, diffAtk);
            SetStatPreview(defText, "DEF", baseDef + curDef, diffDef);
            SetStatPreview(spdText, "SPD", baseSpd + (int)curSpd, diffSpd);
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
