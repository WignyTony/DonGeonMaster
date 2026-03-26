using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DonGeonMaster.Character;
using DonGeonMaster.Equipment;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DonGeonMaster.UI
{
    /// <summary>
    /// Animation preview scene controller.
    /// Spawns a GanzSe character, loads DoubleL animation clips,
    /// and lets the user browse through them with < > buttons.
    /// </summary>
    public class AnimationPreviewController : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private GameObject ganzsePrefab;
        [SerializeField] private Material urpMaterial;
        [SerializeField] private RuntimeAnimatorController animController;

        [Header("Animations")]
        [SerializeField] private AnimationClip[] animationClips;
        [SerializeField] private string[] animationNames;

        [Header("Weapons")]
        [SerializeField] private GameObject[] weaponPrefabs;
        [SerializeField] private string[] weaponNames;
        [SerializeField] private EquipmentData[] weaponEquipmentData;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI animNameLabel;
        [SerializeField] private TextMeshProUGUI weaponNameLabel;

        [Header("Armor UI")]
        [SerializeField] private TextMeshProUGUI slotNameLabel;
        [SerializeField] private TextMeshProUGUI pieceNameLabel;

        [Header("Weapon Position Tool")]
        [SerializeField] private Slider sliderPosX, sliderPosY, sliderPosZ;
        [SerializeField] private Slider sliderRotX, sliderRotY, sliderRotZ;
        [SerializeField] private TextMeshProUGUI valuesLabel;

        private GameObject character;
        private Animator animator;
        private int currentIndex;
        private int currentWeaponIndex = -1; // -1 = no weapon
        private GameObject currentWeaponInstance;
        private Transform handR, handL;

        // Armor browsing — discovered dynamically from prefab hierarchy
        private static readonly Dictionary<string, string> CategoryDisplayNames = new()
        {
            { "HEADS", "Casque" }, { "CHEST ARMOR", "Plastron" }, { "CHESTS", "Plastron" },
            { "LEG ARMOR", "Jambières" }, { "LEGS", "Jambières" },
            { "FEET ARMOR", "Bottes" }, { "FEET", "Bottes" },
            { "BELT ARMOR", "Ceinture" }, { "BELTS", "Ceinture" },
            { "ARM ARMOR", "Brassards" }, { "ARMS", "Brassards" }
        };
        private int currentSlotIndex;
        private int[] currentPieceIndex; // per slot, -1 = none
        private Transform[] armorCategories; // discovered at runtime
        private string[] slotDisplayNamesRuntime; // derived from category names
        private Transform faceDetailsParts; // for helmet toggle

        private void SpawnCharacter()
        {
            if (ganzsePrefab == null) return;

            character = Instantiate(ganzsePrefab, Vector3.zero, Quaternion.Euler(0, 180, 0));
            character.name = "AnimPreviewCharacter";

            // Keep the Animator — need it for animations!
            animator = character.GetComponent<Animator>();
            if (animator == null)
                animator = character.AddComponent<Animator>();

            // Load the AnimatorController we created in ProjectSetup
            var ctrl = animController;
            if (ctrl == null) ctrl = Resources.Load<RuntimeAnimatorController>("AnimPreviewController");
            if (ctrl != null)
            {
                animator.runtimeAnimatorController = ctrl;
                // Force Idle pose immediately (avoid T-pose on first frame)
                animator.Play("Default", 0, 0);
                animator.Update(0);
            }

            // Disable IdlePoseController (Animator will control bones)
            var idlePose = character.GetComponent<IdlePoseController>();
            if (idlePose != null) Destroy(idlePose);
            // Also remove any IdlePoseController that might be added later
            foreach (var ip in character.GetComponents<IdlePoseController>())
                Destroy(ip);

            // Disable all armor for clean view
            GanzSeHelper.DisableAllArmor(character);
            CharacterCustomizer.ApplyFaceCustomization(character);

            // Find hand bones for weapon attachment
            foreach (var t in character.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "hand_r") handR = t;
                else if (t.name == "hand_l") handL = t;
            }

            InitArmorData();
            UpdateWeaponLabel();
        }

        private void Start()
        {
            SpawnCharacter();
            if (animationClips != null && animationClips.Length > 0)
                PlayAnimation(0);
            SetupSliders();
        }

        private void SetupSliders()
        {
            if (sliderPosX != null) sliderPosX.onValueChanged.AddListener(_ => ApplyWeaponTransform());
            if (sliderPosY != null) sliderPosY.onValueChanged.AddListener(_ => ApplyWeaponTransform());
            if (sliderPosZ != null) sliderPosZ.onValueChanged.AddListener(_ => ApplyWeaponTransform());
            if (sliderRotX != null) sliderRotX.onValueChanged.AddListener(_ => ApplyWeaponTransform());
            if (sliderRotY != null) sliderRotY.onValueChanged.AddListener(_ => ApplyWeaponTransform());
            if (sliderRotZ != null) sliderRotZ.onValueChanged.AddListener(_ => ApplyWeaponTransform());
        }

        private void ApplyWeaponTransform()
        {
            if (currentWeaponInstance == null) return;

            float px = sliderPosX != null ? sliderPosX.value : 0;
            float py = sliderPosY != null ? sliderPosY.value : 0;
            float pz = sliderPosZ != null ? sliderPosZ.value : 0;
            float rx = sliderRotX != null ? sliderRotX.value : 0;
            float ry = sliderRotY != null ? sliderRotY.value : 0;
            float rz = sliderRotZ != null ? sliderRotZ.value : 0;

            currentWeaponInstance.transform.localPosition = new Vector3(px, py, pz);
            currentWeaponInstance.transform.localRotation = Quaternion.Euler(rx, ry, rz);

            if (valuesLabel != null)
                valuesLabel.text = $"Pos({px:F3}, {py:F3}, {pz:F3})\nRot({rx:F0}, {ry:F0}, {rz:F0})";
        }

        /// <summary>Called by "Valider" button — saves current slider values to PlayerPrefs for this weapon.</summary>
        public void ValidateWeaponPosition()
        {
            string key = GetWeaponKey();
            if (key == null) return;

            Vector3 savedPos = new Vector3(
                sliderPosX != null ? sliderPosX.value : 0,
                sliderPosY != null ? sliderPosY.value : 0,
                sliderPosZ != null ? sliderPosZ.value : 0);
            Vector3 savedRot = new Vector3(
                sliderRotX != null ? sliderRotX.value : 0,
                sliderRotY != null ? sliderRotY.value : 0,
                sliderRotZ != null ? sliderRotZ.value : 0);

            // Save to PlayerPrefs (for Editor live preview)
            PlayerPrefs.SetFloat($"Wep_{key}_PX", savedPos.x);
            PlayerPrefs.SetFloat($"Wep_{key}_PY", savedPos.y);
            PlayerPrefs.SetFloat($"Wep_{key}_PZ", savedPos.z);
            PlayerPrefs.SetFloat($"Wep_{key}_RX", savedRot.x);
            PlayerPrefs.SetFloat($"Wep_{key}_RY", savedRot.y);
            PlayerPrefs.SetFloat($"Wep_{key}_RZ", savedRot.z);
            PlayerPrefs.Save();

            // Also bake into EquipmentData ScriptableObject assets (persists in builds)
            #if UNITY_EDITOR
            BakeWeaponOffset(key, savedPos, savedRot);
            #endif

            if (valuesLabel != null)
                valuesLabel.text += "\n<color=#4CAF50>Sauvegardé !</color>";
        }

        /// <summary>Returns unique key per weapon (exact name) for PlayerPrefs.</summary>
        private string GetWeaponKey()
        {
            if (currentWeaponIndex < 0 || weaponNames == null || currentWeaponIndex >= weaponNames.Length)
                return null;
            return weaponNames[currentWeaponIndex].Replace(" ", "_");
        }

        private void Update()
        {
            if (character == null) return;

            // Mouse drag to rotate character (right button to avoid conflict with UI)
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                float deltaX = mouse.delta.ReadValue().x;
                character.transform.Rotate(0, -deltaX * 0.5f, 0);
            }
        }

        public void NextAnimation()
        {
            if (animationClips == null || animationClips.Length == 0) return;
            currentIndex = (currentIndex + 1) % animationClips.Length;
            PlayAnimation(currentIndex);
        }

        public void PrevAnimation()
        {
            if (animationClips == null || animationClips.Length == 0) return;
            currentIndex = (currentIndex - 1 + animationClips.Length) % animationClips.Length;
            PlayAnimation(currentIndex);
        }

        private AnimatorOverrideController overrideCtrl;

        private void PlayAnimation(int index)
        {
            if (animator == null || animationClips == null || index >= animationClips.Length) return;
            var clip = animationClips[index];
            if (clip == null) return;

            currentIndex = index;

            // Create override controller once, then just swap clips
            if (overrideCtrl == null && animator.runtimeAnimatorController != null)
                overrideCtrl = new AnimatorOverrideController(animator.runtimeAnimatorController);

            if (overrideCtrl != null)
            {
                var overrides = new System.Collections.Generic.List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideCtrl.GetOverrides(overrides);
                if (overrides.Count > 0)
                {
                    overrides[0] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[0].Key, clip);
                    overrideCtrl.ApplyOverrides(overrides);
                    if (animator.runtimeAnimatorController != overrideCtrl)
                        animator.runtimeAnimatorController = overrideCtrl;
                    animator.Play("Default", 0, 0);
                }
            }

            // Update label
            if (animNameLabel != null)
            {
                string name = (animationNames != null && index < animationNames.Length)
                    ? animationNames[index]
                    : clip.name;
                animNameLabel.text = name;
            }
        }

        // ===== Weapon Navigation =====
        public void NextWeapon()
        {
            if (weaponPrefabs == null || weaponPrefabs.Length == 0) return;
            currentWeaponIndex = (currentWeaponIndex + 1) % weaponPrefabs.Length;
            AttachWeapon(currentWeaponIndex);
        }

        public void PrevWeapon()
        {
            if (weaponPrefabs == null || weaponPrefabs.Length == 0) return;
            currentWeaponIndex--;
            if (currentWeaponIndex < -1) currentWeaponIndex = weaponPrefabs.Length - 1;
            if (currentWeaponIndex == -1)
            {
                // No weapon
                if (currentWeaponInstance != null) Destroy(currentWeaponInstance);
                currentWeaponInstance = null;
                UpdateWeaponLabel();
                return;
            }
            AttachWeapon(currentWeaponIndex);
        }

        private void AttachWeapon(int index)
        {
            if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

            if (index < 0 || index >= weaponPrefabs.Length || weaponPrefabs[index] == null)
            {
                UpdateWeaponLabel();
                return;
            }

            var prefab = weaponPrefabs[index];
            string wName = (weaponNames != null && index < weaponNames.Length) ? weaponNames[index] : prefab.name;
            string upper = wName.ToUpper();

            // Determine hand and transform based on weapon type
            // Offsets from DoubleL reference prefab (same developer ecosystem)
            Transform parent;
            Vector3 pos;
            Quaternion rot;
            Vector3 scale = Vector3.one;

            if (upper.Contains("SHIELD"))
            {
                parent = handL;
                pos = new Vector3(0f, -0.15f, 0f);
                rot = Quaternion.Euler(0f, 90f, 0f);
                scale = new Vector3(0.8f, 0.8f, 0.8f);
            }
            else if (upper.Contains("GREAT SWORD"))
            {
                // Great swords are longer — need more offset to reach handle
                parent = handR;
                pos = new Vector3(0f, -0.45f, 0f);
                rot = Quaternion.Euler(0f, 0f, 0f);
            }
            else if (upper.Contains("HAMMER"))
            {
                parent = handR;
                pos = new Vector3(0f, -0.35f, 0f);
                rot = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                // One-Handed Swords — shorter, less offset needed
                parent = handR;
                pos = new Vector3(0f, -0.30f, 0f);
                rot = Quaternion.Euler(0f, 0f, 0f);
            }

            if (parent == null) parent = character != null ? character.transform : transform;

            currentWeaponInstance = Instantiate(prefab, parent);

            // Load baked position from EquipmentData asset (works in builds)
            if (weaponEquipmentData != null && index < weaponEquipmentData.Length &&
                weaponEquipmentData[index] != null && weaponEquipmentData[index].hasCustomOffset)
            {
                var eq = weaponEquipmentData[index];
                pos = eq.weaponPosOffset;
                rot = Quaternion.Euler(eq.weaponRotOffset);
                scale = eq.weaponScaleOverride;
            }
            else if (sliderPosX != null)
            {
                // No saved data for this weapon — inherit current slider values
                pos = new Vector3(
                    sliderPosX.value,
                    sliderPosY != null ? sliderPosY.value : pos.y,
                    sliderPosZ != null ? sliderPosZ.value : pos.z);
                rot = Quaternion.Euler(
                    sliderRotX != null ? sliderRotX.value : 0,
                    sliderRotY != null ? sliderRotY.value : 0,
                    sliderRotZ != null ? sliderRotZ.value : 0);
            }

            currentWeaponInstance.transform.localPosition = pos;
            currentWeaponInstance.transform.localRotation = rot;
            currentWeaponInstance.transform.localScale = scale;

            // Sync sliders to loaded position
            SyncSlidersToWeapon(pos, rot.eulerAngles);

            UpdateWeaponLabel();
        }

        private void SyncSlidersToWeapon(Vector3 pos, Vector3 rotEuler)
        {
            if (sliderPosX != null) sliderPosX.SetValueWithoutNotify(pos.x);
            if (sliderPosY != null) sliderPosY.SetValueWithoutNotify(pos.y);
            if (sliderPosZ != null) sliderPosZ.SetValueWithoutNotify(pos.z);
            if (sliderRotX != null) sliderRotX.SetValueWithoutNotify(rotEuler.x > 180 ? rotEuler.x - 360 : rotEuler.x);
            if (sliderRotY != null) sliderRotY.SetValueWithoutNotify(rotEuler.y > 180 ? rotEuler.y - 360 : rotEuler.y);
            if (sliderRotZ != null) sliderRotZ.SetValueWithoutNotify(rotEuler.z > 180 ? rotEuler.z - 360 : rotEuler.z);

            if (valuesLabel != null)
                valuesLabel.text = $"Pos({pos.x:F3}, {pos.y:F3}, {pos.z:F3})\nRot({rotEuler.x:F0}, {rotEuler.y:F0}, {rotEuler.z:F0})";
        }

        private void UpdateWeaponLabel()
        {
            if (weaponNameLabel == null) return;
            if (currentWeaponIndex < 0 || weaponPrefabs == null || weaponPrefabs.Length == 0)
            {
                weaponNameLabel.text = "Aucune arme";
                return;
            }
            string name = (weaponNames != null && currentWeaponIndex < weaponNames.Length)
                ? weaponNames[currentWeaponIndex]
                : "Arme " + (currentWeaponIndex + 1);
            weaponNameLabel.text = name;
        }

        public void ReturnToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        // ===== Armor Navigation =====

        private void InitArmorData()
        {
            // Dynamic discovery: find ARMOR PARTS and list ALL its children as categories
            Transform armorParts = null;
            foreach (var t in character.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "ARMOR PARTS") { armorParts = t; break; }
            }

            if (armorParts == null || armorParts.childCount == 0)
            {
                Debug.LogWarning("[AnimPreview] ARMOR PARTS introuvable ou vide !");
                armorCategories = new Transform[0];
                currentPieceIndex = new int[0];
                slotDisplayNamesRuntime = new string[0];
                return;
            }

            int count = armorParts.childCount;
            armorCategories = new Transform[count];
            currentPieceIndex = new int[count];
            slotDisplayNamesRuntime = new string[count];

            for (int i = 0; i < count; i++)
            {
                armorCategories[i] = armorParts.GetChild(i);
                currentPieceIndex[i] = -1;

                string catName = armorCategories[i].name;
                slotDisplayNamesRuntime[i] = CategoryDisplayNames.ContainsKey(catName)
                    ? CategoryDisplayNames[catName]
                    : catName;

                Debug.Log($"[AnimPreview] Slot {i}: \"{catName}\" → {slotDisplayNamesRuntime[i]} ({armorCategories[i].childCount} pièces)");
            }

            // Cache face details for helmet toggle
            foreach (var t in character.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "FACE DETAILS PARTS") { faceDetailsParts = t; break; }
            }

            UpdateSlotLabel();
            UpdatePieceLabel();
        }

        public void NextSlot()
        {
            if (armorCategories == null || armorCategories.Length == 0) return;
            currentSlotIndex = (currentSlotIndex + 1) % armorCategories.Length;
            UpdateSlotLabel();
            UpdatePieceLabel();
        }

        public void PrevSlot()
        {
            if (armorCategories == null || armorCategories.Length == 0) return;
            currentSlotIndex = (currentSlotIndex - 1 + armorCategories.Length) % armorCategories.Length;
            UpdateSlotLabel();
            UpdatePieceLabel();
        }

        public void NextPiece()
        {
            if (armorCategories == null || armorCategories.Length == 0) return;
            var cat = armorCategories[currentSlotIndex];
            if (cat == null || cat.childCount == 0) return;

            int count = cat.childCount;
            currentPieceIndex[currentSlotIndex]++;
            if (currentPieceIndex[currentSlotIndex] >= count)
                currentPieceIndex[currentSlotIndex] = -1;

            ApplyPiece(currentSlotIndex);
        }

        public void PrevPiece()
        {
            if (armorCategories == null || armorCategories.Length == 0) return;
            var cat = armorCategories[currentSlotIndex];
            if (cat == null || cat.childCount == 0) return;

            int count = cat.childCount;
            currentPieceIndex[currentSlotIndex]--;
            if (currentPieceIndex[currentSlotIndex] < -1)
                currentPieceIndex[currentSlotIndex] = count - 1;

            ApplyPiece(currentSlotIndex);
        }

        private void ApplyPiece(int slotIdx)
        {
            var cat = armorCategories[slotIdx];
            if (cat == null) return;

            int pieceIdx = currentPieceIndex[slotIdx];

            // Disable all pieces in this category
            for (int i = 0; i < cat.childCount; i++)
                cat.GetChild(i).gameObject.SetActive(false);

            // Enable selected piece
            if (pieceIdx >= 0 && pieceIdx < cat.childCount)
                cat.GetChild(pieceIdx).gameObject.SetActive(true);

            // Helmet toggle: hide face details when helmet is on
            if (slotIdx == 0 && faceDetailsParts != null)
            {
                bool hasHelmet = pieceIdx >= 0;
                for (int i = 0; i < faceDetailsParts.childCount; i++)
                    faceDetailsParts.GetChild(i).gameObject.SetActive(!hasHelmet);
            }

            UpdatePieceLabel();
        }

        private void UpdateSlotLabel()
        {
            if (slotNameLabel == null) return;
            if (slotDisplayNamesRuntime == null || slotDisplayNamesRuntime.Length == 0)
            {
                slotNameLabel.text = "Aucun slot";
                return;
            }
            slotNameLabel.text = slotDisplayNamesRuntime[currentSlotIndex];
        }

        private void UpdatePieceLabel()
        {
            if (pieceNameLabel == null) return;
            if (armorCategories == null || armorCategories.Length == 0)
            {
                pieceNameLabel.text = "Aucun";
                return;
            }

            var cat = armorCategories[currentSlotIndex];
            int pieceIdx = currentPieceIndex[currentSlotIndex];

            if (cat == null || cat.childCount == 0)
            {
                pieceNameLabel.text = "Aucun";
                return;
            }

            if (pieceIdx < 0)
            {
                pieceNameLabel.text = "Aucun";
                return;
            }

            pieceNameLabel.text = cat.GetChild(pieceIdx).name;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Finds all EquipmentData weapon assets matching this key and bakes the
        /// position/rotation offsets into the ScriptableObject so they persist in builds.
        /// </summary>
        private void BakeWeaponOffset(string key, Vector3 pos, Vector3 rot)
        {
            // Determine scale from weapon type
            string upper = key.ToUpper();
            Vector3 scale = upper.Contains("SHIELD") ? new Vector3(0.8f, 0.8f, 0.8f) : Vector3.one;

            var guids = AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/_Project/Configs/Weapons" });
            foreach (var guid in guids)
            {
                var eq = AssetDatabase.LoadAssetAtPath<EquipmentData>(AssetDatabase.GUIDToAssetPath(guid));
                if (eq == null || eq.meshPrefab == null) continue;

                string eqKey = eq.meshPrefab.name.Replace("FREE ", "").Replace("COLOR ", "C").Replace(" ", "_");
                if (eqKey == key)
                {
                    eq.weaponPosOffset = pos;
                    eq.weaponRotOffset = rot;
                    eq.weaponScaleOverride = scale;
                    eq.hasCustomOffset = true;
                    EditorUtility.SetDirty(eq);
                }
            }
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
