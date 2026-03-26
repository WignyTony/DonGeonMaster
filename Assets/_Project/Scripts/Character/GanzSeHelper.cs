using UnityEngine;

namespace DonGeonMaster.Character
{
    /// <summary>
    /// Utility methods for GanzSe modular characters.
    /// </summary>
    public static class GanzSeHelper
    {
        /// <summary>
        /// Deactivates ALL armor pieces on a GanzSe character.
        /// The character will show only the base body + face details.
        /// </summary>
        public static void DisableAllArmor(GameObject character)
        {
            foreach (Transform t in character.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "ARMOR PARTS")
                {
                    for (int i = 0; i < t.childCount; i++)
                    {
                        var category = t.GetChild(i);
                        for (int j = 0; j < category.childCount; j++)
                            category.GetChild(j).gameObject.SetActive(false);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Logs the full hierarchy of a GanzSe character for debugging.
        /// Shows all direct children, active state, and renderer counts.
        /// </summary>
        public static void LogCharacterHierarchy(GameObject character)
        {
            Debug.Log($"[GanzSe] Hierarchy of '{character.name}' ({character.transform.childCount} root children):");
            for (int i = 0; i < character.transform.childCount; i++)
            {
                var child = character.transform.GetChild(i);
                var renderers = child.GetComponentsInChildren<Renderer>(true);
                int activeRenderers = 0;
                foreach (var r in renderers)
                    if (r.gameObject.activeInHierarchy) activeRenderers++;
                Debug.Log($"  [{i}] '{child.name}' active={child.gameObject.activeSelf} renderers={activeRenderers}/{renderers.Length} children={child.childCount}");

                // Also log grandchildren for key groups
                for (int j = 0; j < child.childCount; j++)
                {
                    var gc = child.GetChild(j);
                    var gcRenderers = gc.GetComponentsInChildren<Renderer>(true);
                    int gcActive = 0;
                    foreach (var r in gcRenderers)
                        if (r.gameObject.activeInHierarchy) gcActive++;
                    if (gcActive > 0 || gc.childCount > 5)
                        Debug.Log($"    [{j}] '{gc.name}' active={gc.gameObject.activeSelf} renderers={gcActive}/{gcRenderers.Length} children={gc.childCount}");
                }
            }
        }
    }
}
