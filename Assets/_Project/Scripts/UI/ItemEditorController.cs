using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DonGeonMaster.Equipment;
using DonGeonMaster.Inventory;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DonGeonMaster.UI
{
    /// <summary>
    /// Item Editor scene controller (Editor-only).
    /// Browse equipment, configure types via dropdowns, stats via number inputs.
    /// Shows 3D preview. Saves on "Valider" button only.
    /// </summary>
    public class ItemEditorController : MonoBehaviour
    {
        [Header("Items")]
        [SerializeField] private EquipmentData[] items;
        [SerializeField] private GameObject[] itemPrefabs;

        [Header("3D Preview")]
        [SerializeField] private Camera renderCamera;
        [SerializeField] private Light sceneLight;
        [SerializeField] private RawImage previewImage;

        [Header("UI - Info")]
        [SerializeField] private TextMeshProUGUI itemNameLabel;
        [SerializeField] private TextMeshProUGUI slotLabel;

        [Header("UI - Dropdowns")]
        [SerializeField] private TMP_Dropdown dropRarity;
        [SerializeField] private TMP_Dropdown dropWeight;
        [SerializeField] private TMP_Dropdown dropWeaponType;
        [SerializeField] private TMP_Dropdown dropArmorMaterial;
        [SerializeField] private TMP_Dropdown dropHandling;
        [SerializeField] private TMP_Dropdown dropElement;

        [Header("UI - Dropdown Rows (to show/hide)")]
        [SerializeField] private GameObject rowWeaponType;
        [SerializeField] private GameObject rowArmorMaterial;

        [Header("UI - Stat Inputs")]
        [SerializeField] private TMP_InputField inputArmor;
        [SerializeField] private TMP_InputField inputDamage;

        [Header("UI - Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackLabel;

        private int currentIndex;
        private GameObject currentPreviewInstance;

        // Filtered dropdown mappings — stores the real enum values for the current filtered list
        private int[] currentMatEnumValues;
        private int[] currentHandlingEnumValues;
        private RenderTexture previewRT;
        private Vector3 itemCenter;

        private static readonly string[] RarityNames = { "Commun", "Peu commun", "Rare", "Épique", "Légendaire", "Mythique" };
        private static readonly string[] WeightNames = { "Très léger", "Léger", "Moyen", "Lourd", "Très lourd" };
        private static readonly string[] WeaponTypeNames = { "Épée", "Grande épée", "Dague", "Marteau", "Masse", "Hache", "Grande hache", "Bouclier", "Bâton", "Lance" };
        private static readonly string[] ArmorMatNames = { "Tissu", "Cuir", "Mailles", "Plaques", "Écailles", "Os", "Mystique" };
        private static readonly string[] HandlingNames = { "Très rapide", "Rapide", "Normal", "Lent", "Très lent" };
        private static readonly string[] ElementNames = { "Aucun", "Feu", "Glace", "Foudre", "Poison", "Sacré", "Ténèbres", "Arcane" };

        private void Start()
        {
            previewRT = new RenderTexture(256, 256, 16);
            if (renderCamera != null)
                renderCamera.targetTexture = previewRT;
            if (previewImage != null)
                previewImage.texture = previewRT;

            PopulateDropdown(dropRarity, RarityNames);
            PopulateDropdown(dropWeight, WeightNames);
            PopulateDropdown(dropWeaponType, WeaponTypeNames);
            PopulateDropdown(dropArmorMaterial, ArmorMatNames);
            PopulateDropdown(dropHandling, HandlingNames);
            PopulateDropdown(dropElement, ElementNames);

            // Auto-sync material + handling when weight changes
            if (dropWeight != null) dropWeight.onValueChanged.AddListener(OnWeightChanged);

            if (items != null && items.Length > 0)
                ShowItem(0);
        }

        private void OnDestroy()
        {
            if (previewRT != null) { previewRT.Release(); Destroy(previewRT); }
            if (currentPreviewInstance != null) Destroy(currentPreviewInstance);
        }

        private void PopulateDropdown(TMP_Dropdown drop, string[] options)
        {
            if (drop == null) return;
            drop.ClearOptions();
            drop.AddOptions(new System.Collections.Generic.List<string>(options));
        }

        private void Update()
        {
            if (currentPreviewInstance == null) return;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                float deltaX = mouse.delta.ReadValue().x;
                currentPreviewInstance.transform.Rotate(0, -deltaX * 0.5f, 0);
            }
        }

        public void NextItem()
        {
            if (items == null || items.Length == 0) return;
            currentIndex = (currentIndex + 1) % items.Length;
            ShowItem(currentIndex);
        }

        public void PrevItem()
        {
            if (items == null || items.Length == 0) return;
            currentIndex = (currentIndex - 1 + items.Length) % items.Length;
            ShowItem(currentIndex);
        }

        private void ShowItem(int index)
        {
            currentIndex = index;
            var item = items[index];
            if (item == null) return;

            // Info
            if (itemNameLabel != null)
                itemNameLabel.text = $"{item.itemName}\n<size=14>({index + 1}/{items.Length})</size>";
            if (slotLabel != null)
                slotLabel.text = $"Slot: {item.slot}";

            // Show/hide context-dependent rows
            bool isWeapon = item.slot == CharacterStandards.EquipmentSlot.Weapon ||
                            item.slot == CharacterStandards.EquipmentSlot.Shield;
            if (rowWeaponType != null) rowWeaponType.SetActive(isWeapon);
            if (rowArmorMaterial != null) rowArmorMaterial.SetActive(!isWeapon);

            // Always load item's saved values
            if (dropRarity != null) dropRarity.SetValueWithoutNotify((int)item.rarity);
            if (dropWeight != null) dropWeight.SetValueWithoutNotify((int)item.weight);
            if (dropWeaponType != null) dropWeaponType.SetValueWithoutNotify((int)item.weaponType);
            if (dropElement != null) dropElement.SetValueWithoutNotify((int)item.element);
            if (inputArmor != null) inputArmor.text = item.armor.ToString();
            if (inputDamage != null) inputDamage.text = item.damage.ToString();

            // Refresh filtered dropdowns based on item's weight
            OnWeightChanged((int)item.weight);

            // 3D Preview
            SpawnPreview(item);

            if (feedbackLabel != null) feedbackLabel.text = "";
        }

        private void SpawnPreview(EquipmentData item)
        {
            if (currentPreviewInstance != null) Destroy(currentPreviewInstance);

            // Get prefab from the parallel array (matched by ProjectSetup)
            GameObject prefab = (itemPrefabs != null && currentIndex < itemPrefabs.Length)
                ? itemPrefabs[currentIndex] : null;

            // Fallback to meshPrefab
            if (prefab == null) prefab = item.meshPrefab;
            if (prefab == null) return;

            currentPreviewInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            currentPreviewInstance.name = "EditorPreview";

            // Fix materials
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null)
            {
                foreach (var rend in currentPreviewInstance.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = rend.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] != null && mats[i].shader.name == "Standard")
                        {
                            var tex = mats[i].GetTexture("_MainTex");
                            mats[i] = new Material(urpShader);
                            if (tex != null) mats[i].SetTexture("_BaseMap", tex);
                            mats[i].SetColor("_BaseColor", Color.white);
                        }
                    }
                    rend.sharedMaterials = mats;
                }
            }

            // Auto-frame
            var renderers = currentPreviewInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                itemCenter = bounds.center;

                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float dist = size / (2f * Mathf.Tan(30f * 0.5f * Mathf.Deg2Rad)) * 1.3f;
                dist = Mathf.Max(dist, 0.3f);

                if (renderCamera != null)
                {
                    renderCamera.transform.position = itemCenter + new Vector3(0.3f, 0.2f, -dist);
                    renderCamera.transform.LookAt(itemCenter);
                }
                if (sceneLight != null)
                    sceneLight.transform.rotation = renderCamera != null ? renderCamera.transform.rotation : Quaternion.Euler(30, -30, 0);
            }
        }

        /// <summary>Repopulate material + handling dropdowns based on weight selection.</summary>
        private void OnWeightChanged(int weightIndex)
        {
            // Weight → allowed ArmorMaterial enum values
            var matOptions = weightIndex switch
            {
                0 => new[] { (0, "Tissu") },                        // TresLeger
                1 => new[] { (1, "Cuir") },                          // Leger
                2 => new[] { (5, "Os"), (6, "Mystique") },           // Moyen
                3 => new[] { (2, "Mailles"), (4, "Écailles") },      // Lourd
                4 => new[] { (3, "Plaques") },                        // TresLourd
                _ => new[] { (2, "Mailles") }
            };

            // Weight → allowed Handling enum values
            var handlingOptions = weightIndex switch
            {
                0 => new[] { (0, "Très rapide") },  // TresLeger
                1 => new[] { (1, "Rapide") },        // Leger
                2 => new[] { (2, "Normal") },         // Moyen
                3 => new[] { (3, "Lent") },           // Lourd
                4 => new[] { (4, "Très lent") },      // TresLourd
                _ => new[] { (2, "Normal") }
            };

            // Repopulate material dropdown
            if (dropArmorMaterial != null)
            {
                dropArmorMaterial.ClearOptions();
                var names = new System.Collections.Generic.List<string>();
                currentMatEnumValues = new int[matOptions.Length];
                for (int i = 0; i < matOptions.Length; i++)
                {
                    currentMatEnumValues[i] = matOptions[i].Item1;
                    names.Add(matOptions[i].Item2);
                }
                dropArmorMaterial.AddOptions(names);
                dropArmorMaterial.SetValueWithoutNotify(0);
            }

            // Repopulate handling dropdown
            if (dropHandling != null)
            {
                dropHandling.ClearOptions();
                var names = new System.Collections.Generic.List<string>();
                currentHandlingEnumValues = new int[handlingOptions.Length];
                for (int i = 0; i < handlingOptions.Length; i++)
                {
                    currentHandlingEnumValues[i] = handlingOptions[i].Item1;
                    names.Add(handlingOptions[i].Item2);
                }
                dropHandling.AddOptions(names);
                dropHandling.SetValueWithoutNotify(0);
            }

            // Auto-fill armor/damage based on weight and item type
            int baseValue = weightIndex + 1; // TresLeger=1, Leger=2, Moyen=3, Lourd=4, TresLourd=5
            var item = CurrentItem();
            bool isWeapon = item != null &&
                (item.slot == CharacterStandards.EquipmentSlot.Weapon ||
                 item.slot == CharacterStandards.EquipmentSlot.Shield);

            if (isWeapon && item.slot == CharacterStandards.EquipmentSlot.Shield)
            {
                // Shield: armor based on weight, no damage
                if (inputArmor != null) inputArmor.text = baseValue.ToString();
                if (inputDamage != null) inputDamage.text = "0";
            }
            else if (isWeapon)
            {
                // Weapon: damage based on weight, no armor
                if (inputDamage != null) inputDamage.text = baseValue.ToString();
                if (inputArmor != null) inputArmor.text = "0";
            }
            else
            {
                // Armor: armor based on weight, no damage
                if (inputArmor != null) inputArmor.text = baseValue.ToString();
                if (inputDamage != null) inputDamage.text = "0";
            }
        }

        /// <summary>Called by Valider button — saves all current values to the asset.</summary>
        public void ValidateItem()
        {
            var item = CurrentItem();
            if (item == null) return;

            // Read dropdowns (rarity, weight, weaponType, element use direct enum index)
            if (dropRarity != null) item.rarity = (ItemRarity)dropRarity.value;
            if (dropWeight != null) item.weight = (CharacterStandards.EquipmentWeight)dropWeight.value;
            if (dropWeaponType != null) item.weaponType = (CharacterStandards.WeaponType)dropWeaponType.value;
            if (dropElement != null) item.element = (CharacterStandards.ElementType)dropElement.value;

            // Material + Handling use filtered lists → map dropdown index to real enum value
            if (dropArmorMaterial != null && currentMatEnumValues != null && dropArmorMaterial.value < currentMatEnumValues.Length)
                item.armorMaterial = (CharacterStandards.ArmorMaterial)currentMatEnumValues[dropArmorMaterial.value];
            if (dropHandling != null && currentHandlingEnumValues != null && dropHandling.value < currentHandlingEnumValues.Length)
                item.handling = (CharacterStandards.Handling)currentHandlingEnumValues[dropHandling.value];

            // Read stat inputs
            if (inputArmor != null && int.TryParse(inputArmor.text, out int arm)) item.armor = arm;
            if (inputDamage != null && int.TryParse(inputDamage.text, out int dmg)) item.damage = dmg;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(item);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(item);
#endif
            if (feedbackLabel != null)
                feedbackLabel.text = "<color=#4CAF50>Sauvegardé !</color>";
        }

        private EquipmentData CurrentItem()
        {
            if (items == null || currentIndex >= items.Length) return null;
            return items[currentIndex];
        }

        public void ReturnToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

#if UNITY_EDITOR
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
            items = list.ToArray();
            EditorUtility.SetDirty(this);
            Debug.Log($"[ItemEditorController] Refreshed: {items.Length} equipment references.");
        }
#endif
    }
}
