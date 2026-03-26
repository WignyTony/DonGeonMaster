using UnityEngine;
using DonGeonMaster.Inventory;
using DonGeonMaster.Equipment;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DonGeonMaster.Debugging
{
    /// <summary>
    /// Debug: loads all armor/weapon EquipmentData into the inventory at Start for testing.
    /// The armorsToLoad array is auto-populated in the Editor via RefreshReferences().
    /// </summary>
    public class DebugArmorLoader : MonoBehaviour
    {
        [SerializeField] private EquipmentData[] armorsToLoad;

        private void Start()
        {
            StartCoroutine(LoadNextFrame());
        }

        private System.Collections.IEnumerator LoadNextFrame()
        {
            yield return null;

            var inv = PlayerInventory.Instance;
            if (inv == null || armorsToLoad == null) yield break;

            int count = 0;
            foreach (var armor in armorsToLoad)
            {
                if (armor != null && inv.AddItem(armor, 1))
                    count++;
            }

            Debug.Log($"[DebugArmorLoader] Added {count}/{armorsToLoad.Length} armors to inventory.");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Finds all EquipmentData assets in the project and populates the array.
        /// Called automatically after asset regeneration or via context menu.
        /// </summary>
        [ContextMenu("Refresh Equipment References")]
        public void RefreshReferences()
        {
            var guids = AssetDatabase.FindAssets("t:EquipmentData",
                new[] { "Assets/_Project/Configs/Armor", "Assets/_Project/Configs/Weapons" });
            var list = new System.Collections.Generic.List<EquipmentData>();
            foreach (var guid in guids)
            {
                var eq = AssetDatabase.LoadAssetAtPath<EquipmentData>(AssetDatabase.GUIDToAssetPath(guid));
                if (eq != null) list.Add(eq);
            }
            armorsToLoad = list.ToArray();
            EditorUtility.SetDirty(this);
            Debug.Log($"[DebugArmorLoader] Refreshed: {armorsToLoad.Length} equipment references.");
        }
#endif
    }
}
