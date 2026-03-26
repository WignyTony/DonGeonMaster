using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DonGeonMaster.Inventory;

namespace DonGeonMaster.Equipment
{
    /// <summary>
    /// Equipment manager for GanzSe modular characters.
    /// Equips armor by activating/deactivating child GameObjects in category containers.
    /// </summary>
    public class ModularEquipmentManager : MonoBehaviour
    {
        // Category container transforms (found at Start)
        private Dictionary<CharacterStandards.EquipmentSlot, Transform> categories = new();
        private Dictionary<CharacterStandards.EquipmentSlot, EquipmentData> equippedItems = new();
        private Dictionary<CharacterStandards.EquipmentSlot, GameObject> weaponInstances = new();
        private Transform facePartsRoot;
        private Transform handR, handL;

        // Mapping from EquipmentSlot to GanzSe category name
        private static readonly Dictionary<CharacterStandards.EquipmentSlot, string[]> SlotToCategoryNames = new()
        {
            { CharacterStandards.EquipmentSlot.Head, new[] { "HEADS" } },
            { CharacterStandards.EquipmentSlot.Chest, new[] { "CHEST ARMOR", "CHESTS" } },
            { CharacterStandards.EquipmentSlot.Legs, new[] { "LEG ARMOR", "LEGS" } },
            { CharacterStandards.EquipmentSlot.Feet, new[] { "FEET ARMOR", "FEET" } },
            { CharacterStandards.EquipmentSlot.Belt, new[] { "BELT ARMOR", "BELTS" } },
            { CharacterStandards.EquipmentSlot.Arms, new[] { "ARM ARMOR", "ARMS" } },
        };

        private void Start()
        {
            CacheCategories();
            StartCoroutine(LoadSavedEquipmentDelayed());
        }

        private void CacheCategories()
        {
            // Find ARMOR PARTS root
            Transform armorRoot = null;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "ARMOR PARTS") { armorRoot = child; break; }
            }

            if (armorRoot == null)
            {
                Debug.LogWarning("[ModularEquipment] ARMOR PARTS not found in character hierarchy.");
                return;
            }

            // Find each category container
            foreach (var kvp in SlotToCategoryNames)
            {
                foreach (string catName in kvp.Value)
                {
                    foreach (Transform child in armorRoot.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == catName && child.parent == armorRoot)
                        {
                            categories[kvp.Key] = child;
                            break;
                        }
                    }
                    if (categories.ContainsKey(kvp.Key)) break;
                }
            }

            // Find face parts root
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "FACE DETAILS PARTS" || child.name == "FACE DETAILS")
                {
                    facePartsRoot = child;
                    break;
                }
            }

            // Find hand bones for weapon attachment
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "hand_r") handR = t;
                else if (t.name == "hand_l") handL = t;
            }

            Debug.Log($"[ModularEquipment] Found {categories.Count} armor categories, hands: R={handR != null} L={handL != null}");
        }

        public bool Equip(EquipmentData equipment)
        {
            if (equipment == null) return false;

            // Weapon/Shield with meshPrefab → instantiate on hand
            if (equipment.meshPrefab != null &&
                (equipment.slot == CharacterStandards.EquipmentSlot.Weapon ||
                 equipment.slot == CharacterStandards.EquipmentSlot.Shield))
            {
                return EquipWeapon(equipment);
            }

            // Modular armor → activate child in category
            if (string.IsNullOrEmpty(equipment.armorPartName)) return false;
            if (!categories.TryGetValue(equipment.slot, out var category)) return false;

            // Deactivate all children in this category
            for (int i = 0; i < category.childCount; i++)
                category.GetChild(i).gameObject.SetActive(false);

            // Activate the matching child
            bool found = false;
            for (int i = 0; i < category.childCount; i++)
            {
                var child = category.GetChild(i);
                if (child.name == equipment.armorPartName)
                {
                    child.gameObject.SetActive(true);
                    found = true;
                    break;
                }
            }

            if (found)
            {
                equippedItems[equipment.slot] = equipment;
                SaveEquipment();

                // Head armor: hide face details
                if (equipment.slot == CharacterStandards.EquipmentSlot.Head && facePartsRoot != null)
                    facePartsRoot.gameObject.SetActive(false);
            }

            return found;
        }

        private bool EquipWeapon(EquipmentData equipment)
        {
            // Destroy previous weapon in this slot
            if (weaponInstances.TryGetValue(equipment.slot, out var old) && old != null)
                Destroy(old);

            bool isShield = equipment.slot == CharacterStandards.EquipmentSlot.Shield;
            Transform parent = isShield ? handL : handR;
            if (parent == null) parent = transform;

            var instance = Instantiate(equipment.meshPrefab, parent);

            Vector3 pos;
            Quaternion rot;
            Vector3 scale;

            if (equipment.hasCustomOffset)
            {
                // Use offsets baked into the ScriptableObject (works in builds)
                pos = equipment.weaponPosOffset;
                rot = Quaternion.Euler(equipment.weaponRotOffset);
                scale = equipment.weaponScaleOverride;
            }
            else
            {
                // Fallback: type-based defaults
                string upper = equipment.meshPrefab.name.ToUpper();
                rot = Quaternion.identity;
                scale = Vector3.one;

                if (upper.Contains("SHIELD"))
                {
                    pos = new Vector3(0f, -0.15f, 0f);
                    rot = Quaternion.Euler(0f, 90f, 0f);
                    scale = new Vector3(0.8f, 0.8f, 0.8f);
                }
                else if (upper.Contains("GREAT SWORD"))
                    pos = new Vector3(0f, -0.45f, 0f);
                else if (upper.Contains("HAMMER"))
                    pos = new Vector3(0f, -0.35f, 0f);
                else
                    pos = new Vector3(0f, -0.30f, 0f);
            }

            instance.transform.localPosition = pos;
            instance.transform.localRotation = rot;
            instance.transform.localScale = scale;

            Debug.Log($"[EquipWeapon] {equipment.itemName} | hasCustom={equipment.hasCustomOffset} " +
                      $"| pos={pos} rot={equipment.weaponRotOffset} scale={scale} " +
                      $"| parent={parent.name} handR={handR?.name} handL={handL?.name} " +
                      $"| animator={GetComponent<Animator>()?.runtimeAnimatorController?.name}");

            weaponInstances[equipment.slot] = instance;
            equippedItems[equipment.slot] = equipment;
            SaveEquipment();
            return true;
        }

        public void Unequip(CharacterStandards.EquipmentSlot slot)
        {
            // Destroy weapon instance if any
            if (weaponInstances.TryGetValue(slot, out var weaponObj) && weaponObj != null)
            {
                Destroy(weaponObj);
                weaponInstances.Remove(slot);
            }

            // Deactivate modular armor if category exists
            if (categories.TryGetValue(slot, out var category))
            {
                for (int i = 0; i < category.childCount; i++)
                    category.GetChild(i).gameObject.SetActive(false);
            }

            equippedItems.Remove(slot);
            SaveEquipment();

            // Head: show face details again
            if (slot == CharacterStandards.EquipmentSlot.Head && facePartsRoot != null)
                facePartsRoot.gameObject.SetActive(true);
        }

        public EquipmentData GetEquipped(CharacterStandards.EquipmentSlot slot)
        {
            return equippedItems.TryGetValue(slot, out var data) ? data : null;
        }

        public void UnequipAll()
        {
            foreach (var slot in new List<CharacterStandards.EquipmentSlot>(equippedItems.Keys))
                Unequip(slot);
        }

        // =================================================================
        // PERSISTENCE — save/load equipped items via PlayerPrefs
        // =================================================================

        private const string PrefsPrefix = "Equipped_";

        private void SaveEquipment()
        {
            foreach (CharacterStandards.EquipmentSlot slot in System.Enum.GetValues(typeof(CharacterStandards.EquipmentSlot)))
            {
                string key = PrefsPrefix + slot;
                if (equippedItems.TryGetValue(slot, out var eq) && eq != null)
                    PlayerPrefs.SetString(key, eq.itemId);
                else
                    PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Waits for inventory to be populated (DebugArmorLoader runs after 1 frame),
        /// then re-equips saved items via UseItem which handles removal + events.
        /// </summary>
        private IEnumerator LoadSavedEquipmentDelayed()
        {
            yield return null; // frame 1: DebugArmorLoader starts coroutine
            yield return null; // frame 2: DebugArmorLoader has added items

            var inv = PlayerInventory.Instance;
            if (inv == null) yield break;

            foreach (CharacterStandards.EquipmentSlot slot in System.Enum.GetValues(typeof(CharacterStandards.EquipmentSlot)))
            {
                string savedId = PlayerPrefs.GetString(PrefsPrefix + slot, "");
                if (string.IsNullOrEmpty(savedId)) continue;

                var allSlots = inv.GetAllSlots();
                for (int i = 0; i < allSlots.Count; i++)
                {
                    if (!allSlots[i].IsEmpty && allSlots[i].item is EquipmentData eq && eq.itemId == savedId)
                    {
                        inv.UseItem(i); // handles equip + inventory removal + events
                        break;
                    }
                }
            }
        }
    }
}
