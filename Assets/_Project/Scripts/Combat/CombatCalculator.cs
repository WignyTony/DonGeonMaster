using UnityEngine;
using DonGeonMaster.Equipment;

namespace DonGeonMaster.Combat
{
    public static class CombatCalculator
    {
        /// <summary>
        /// Calculate raw damage before defense.
        /// </summary>
        public static int CalculateRawDamage(int attackerAtk, float attackSpeed,
            CharacterStandards.ElementType atkElement = CharacterStandards.ElementType.Aucun,
            CharacterStandards.ElementType defElement = CharacterStandards.ElementType.Aucun)
        {
            float baseDmg = attackerAtk * Mathf.Max(0.5f, attackSpeed);
            float elementMul = GetElementMultiplier(atkElement, defElement);
            return Mathf.Max(1, Mathf.RoundToInt(baseDmg * elementMul));
        }

        /// <summary>
        /// Calculate final damage after defense reduction.
        /// </summary>
        public static int CalculateFinalDamage(int rawDamage, int defenderDef)
        {
            float reduction = defenderDef * 0.5f;
            return Mathf.Max(1, Mathf.RoundToInt(rawDamage - reduction));
        }

        /// <summary>
        /// Full damage calculation: ATK vs DEF with element and speed.
        /// </summary>
        public static int CalculateDamage(int attackerAtk, float attackSpeed, int defenderDef,
            CharacterStandards.ElementType atkElement = CharacterStandards.ElementType.Aucun,
            CharacterStandards.ElementType defElement = CharacterStandards.ElementType.Aucun)
        {
            int raw = CalculateRawDamage(attackerAtk, attackSpeed, atkElement, defElement);
            return CalculateFinalDamage(raw, defenderDef);
        }

        /// <summary>
        /// Check if attack is critical based on crit chance (0-100).
        /// </summary>
        public static bool IsCritical(int critChance)
        {
            return Random.Range(0, 100) < critChance;
        }

        /// <summary>
        /// Apply critical multiplier (x1.5).
        /// </summary>
        public static int ApplyCritical(int damage, bool isCrit)
        {
            return isCrit ? Mathf.RoundToInt(damage * 1.5f) : damage;
        }

        /// <summary>
        /// Element advantage system.
        /// Feu > Glace > Foudre > Poison > Feu
        /// Sacre > Tenebres > Arcane > Sacre
        /// Returns 1.5 advantage, 0.75 disadvantage, 1.0 neutral.
        /// </summary>
        public static float GetElementMultiplier(CharacterStandards.ElementType attacker, CharacterStandards.ElementType defender)
        {
            if (attacker == CharacterStandards.ElementType.Aucun || defender == CharacterStandards.ElementType.Aucun)
                return 1f;

            bool advantage = (attacker, defender) switch
            {
                (CharacterStandards.ElementType.Feu, CharacterStandards.ElementType.Glace) => true,
                (CharacterStandards.ElementType.Glace, CharacterStandards.ElementType.Foudre) => true,
                (CharacterStandards.ElementType.Foudre, CharacterStandards.ElementType.Poison) => true,
                (CharacterStandards.ElementType.Poison, CharacterStandards.ElementType.Feu) => true,
                (CharacterStandards.ElementType.Sacre, CharacterStandards.ElementType.Tenebres) => true,
                (CharacterStandards.ElementType.Tenebres, CharacterStandards.ElementType.Arcane) => true,
                (CharacterStandards.ElementType.Arcane, CharacterStandards.ElementType.Sacre) => true,
                _ => false
            };

            bool disadvantage = (defender, attacker) switch
            {
                (CharacterStandards.ElementType.Feu, CharacterStandards.ElementType.Glace) => true,
                (CharacterStandards.ElementType.Glace, CharacterStandards.ElementType.Foudre) => true,
                (CharacterStandards.ElementType.Foudre, CharacterStandards.ElementType.Poison) => true,
                (CharacterStandards.ElementType.Poison, CharacterStandards.ElementType.Feu) => true,
                (CharacterStandards.ElementType.Sacre, CharacterStandards.ElementType.Tenebres) => true,
                (CharacterStandards.ElementType.Tenebres, CharacterStandards.ElementType.Arcane) => true,
                (CharacterStandards.ElementType.Arcane, CharacterStandards.ElementType.Sacre) => true,
                _ => false
            };

            if (advantage) return 1.5f;
            if (disadvantage) return 0.75f;
            return 1f;
        }
    }
}
