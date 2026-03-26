using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DonGeonMaster.UI
{
    public class CharacterCustomizer : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject customizerPanel;
        [SerializeField] private RawImage facePreviewImage;

        [Header("GanzSe Prefab")]
        [SerializeField] private GameObject ganzsePrefab;
        [SerializeField] private Material urpMaterial;

        [Header("Type Labels (6)")]
        [SerializeField] private TextMeshProUGUI[] typeLabels;
        [Header("Color Labels (4)")]
        [SerializeField] private TextMeshProUGUI[] colorLabels;

        private static readonly string[] CategoryNames = { "EYES", "HAIR", "FACE HAIR", "EYEBROWS", "NOSE", "EARS" };
        private static readonly int[] MaxTypes = { 5, 5, 5, 5, 5, 2 };
        private static readonly int[] MaxColors = { 5, 5, 5, 5, 0, 0 };

        private int[] currentType = { 1, 1, 1, 1, 1, 1 };
        private int[] currentColor = { 1, 1, 1, 1, 0, 0 };

        private Transform faceRoot;
        private GameObject previewCharacter;
        private Camera previewCamera;
        private Light previewLight;
        private RenderTexture previewRT;

        public bool IsOpen => customizerPanel != null && customizerPanel.activeSelf;

        public void Toggle()
        {
            if (customizerPanel == null) return;
            bool opening = !customizerPanel.activeSelf;
            customizerPanel.SetActive(opening);

            if (opening)
            {
                CreatePreview();
                LoadFromPrefs();
            }
            else
            {
                DestroyPreview();
            }
        }

        public void Close()
        {
            if (customizerPanel != null) customizerPanel.SetActive(false);
            DestroyPreview();
        }

        public void Apply()
        {
            SaveToPrefs();
            Close();
        }

        // ===== Preview System =====
        private void CreatePreview()
        {
            if (ganzsePrefab == null) return;

            // Spawn a copy far off-screen
            Vector3 offscreen = new Vector3(100, 0, 100);
            previewCharacter = Instantiate(ganzsePrefab, offscreen, Quaternion.Euler(0, 180f, 0));
            previewCharacter.name = "CustomizerPreview";

            // Use Animator with Idle animation
            var anim = previewCharacter.GetComponent<Animator>();
            if (anim == null) anim = previewCharacter.AddComponent<Animator>();
            anim.applyRootMotion = false;
            var ctrl = Resources.Load<RuntimeAnimatorController>("AnimPreviewController");
            #if UNITY_EDITOR
            if (ctrl == null)
                ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Project/Art/Animations/AnimPreviewController.controller");
            #endif
            if (ctrl != null)
            {
                anim.runtimeAnimatorController = ctrl;
                anim.Play("Default", 0, 0);
                anim.Update(0);
            }

            // Disable ALL armor parts (show only the face/body, no helmet)
            foreach (Transform t in previewCharacter.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "ARMOR PARTS")
                {
                    // Disable every armor category and all their children
                    for (int c = 0; c < t.childCount; c++)
                    {
                        var cat = t.GetChild(c);
                        for (int j = 0; j < cat.childCount; j++)
                            cat.GetChild(j).gameObject.SetActive(false);
                    }
                    break;
                }
            }

            // Find face root
            foreach (Transform t in previewCharacter.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "FACE DETAILS PARTS" || t.name == "FACE DETAILS")
                { faceRoot = t; break; }
            }

            // Debug: list all face categories and their children
            if (faceRoot != null)
            {
                Debug.Log($"[Customizer] faceRoot found: '{faceRoot.name}' with {faceRoot.childCount} categories");
                foreach (Transform cat in faceRoot)
                {
                    string children = "";
                    for (int c = 0; c < Mathf.Min(cat.childCount, 5); c++)
                        children += cat.GetChild(c).name + ", ";
                    Debug.Log($"  Category '{cat.name}': {cat.childCount} children. First: {children}");
                }
            }
            else
            {
                Debug.LogWarning("[Customizer] faceRoot NOT FOUND! Listing all children:");
                foreach (Transform t in previewCharacter.GetComponentsInChildren<Transform>(true))
                    if (t.parent == previewCharacter.transform)
                        Debug.Log($"  Root child: '{t.name}'");
            }

            // Add lighting for the preview (without light, everything is dark grey!)
            var lightObj = new GameObject("CustomizerLight");
            lightObj.transform.position = offscreen + new Vector3(0.5f, 2.2f, -0.5f);
            lightObj.transform.LookAt(offscreen + new Vector3(0, 1.5f, 0));
            previewLight = lightObj.AddComponent<Light>();
            var light = previewLight;
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.9f); // Warm white
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;

            // Create RenderTexture
            previewRT = new RenderTexture(512, 512, 16);
            previewRT.antiAliasing = 4;

            // Create camera aimed at the face
            var camObj = new GameObject("CustomizerCamera");
            camObj.transform.position = offscreen + new Vector3(0, 1.70f, -0.70f);
            camObj.transform.LookAt(offscreen + new Vector3(0, 1.65f, 0));

            previewCamera = camObj.AddComponent<Camera>();
            previewCamera.targetTexture = previewRT;
            previewCamera.fieldOfView = 40;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.06f, 0.06f, 0.1f, 1f);
            previewCamera.cullingMask = ~0; // Render everything
            previewCamera.nearClipPlane = 0.1f;

            // Display in UI
            if (facePreviewImage != null)
                facePreviewImage.texture = previewRT;
        }

        private void DestroyPreview()
        {
            if (previewCharacter != null) Destroy(previewCharacter);
            if (previewCamera != null) Destroy(previewCamera.gameObject);
            if (previewLight != null) Destroy(previewLight.gameObject);
            if (previewRT != null) { previewRT.Release(); Destroy(previewRT); }
            previewCharacter = null;
            previewCamera = null;
            previewLight = null;
            previewRT = null;
            faceRoot = null;
        }

        // Preview character stays still (face forward, no rotation)

        // ===== Navigation =====
        public void PrevEyeType() { CycleType(0, -1); }
        public void NextEyeType() { CycleType(0, 1); }
        public void PrevHairType() { CycleType(1, -1); }
        public void NextHairType() { CycleType(1, 1); }
        public void PrevBeardType() { CycleType(2, -1); }
        public void NextBeardType() { CycleType(2, 1); }
        public void PrevBrowsType() { CycleType(3, -1); }
        public void NextBrowsType() { CycleType(3, 1); }
        public void PrevNoseType() { CycleType(4, -1); }
        public void NextNoseType() { CycleType(4, 1); }
        public void PrevEarsType() { CycleType(5, -1); }
        public void NextEarsType() { CycleType(5, 1); }
        public void PrevEyeColor() { CycleColor(0, -1); }
        public void NextEyeColor() { CycleColor(0, 1); }
        public void PrevHairColor() { CycleColor(1, -1); }
        public void NextHairColor() { CycleColor(1, 1); }
        public void PrevBeardColor() { CycleColor(2, -1); }
        public void NextBeardColor() { CycleColor(2, 1); }
        public void PrevBrowsColor() { CycleColor(3, -1); }
        public void NextBrowsColor() { CycleColor(3, 1); }

        private void CycleType(int i, int dir)
        {
            currentType[i] = Wrap(currentType[i] + dir, 1, MaxTypes[i]);
            ApplyToCharacter(i);
            UpdateLabels();
        }

        private void CycleColor(int i, int dir)
        {
            if (MaxColors[i] == 0) return;
            currentColor[i] = Wrap(currentColor[i] + dir, 1, MaxColors[i]);
            ApplyToCharacter(i);
            UpdateLabels();
        }

        private int Wrap(int val, int min, int max) =>
            val > max ? min : val < min ? max : val;

        private void ApplyToCharacter(int catIndex)
        {
            if (faceRoot == null)
            {
                Debug.LogWarning("[Customizer] faceRoot is null!");
                return;
            }

            Transform category = null;
            foreach (Transform child in faceRoot)
            {
                if (child.name.ToUpper().Contains(CategoryNames[catIndex]))
                { category = child; break; }
            }
            if (category == null)
            {
                Debug.LogWarning($"[Customizer] Category '{CategoryNames[catIndex]}' not found in faceRoot ({faceRoot.childCount} children)");
                return;
            }

            string prefix = CategoryNames[catIndex] switch
            {
                "EYES" => "Eyes", "HAIR" => "Hair", "FACE HAIR" => "Face Hair",
                "EYEBROWS" => "Eyebrow", "NOSE" => "Nose", "EARS" => "Ears", _ => ""
            };

            string targetName = MaxColors[catIndex] > 0
                ? $"{prefix} Type {currentType[catIndex]} Color {currentColor[catIndex]}"
                : $"{prefix} Type {currentType[catIndex]}";

            // Deactivate all, activate target
            bool found = false;
            for (int j = 0; j < category.childCount; j++)
                category.GetChild(j).gameObject.SetActive(false);
            for (int j = 0; j < category.childCount; j++)
            {
                if (category.GetChild(j).name == targetName)
                {
                    category.GetChild(j).gameObject.SetActive(true);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                string available = "";
                for (int k = 0; k < Mathf.Min(category.childCount, 8); k++)
                    available += $"'{category.GetChild(k).name}', ";
                Debug.LogWarning($"[Customizer] '{targetName}' NOT found in '{category.name}' ({category.childCount} children). Available: {available}");
            }
        }

        private void UpdateLabels()
        {
            if (typeLabels != null)
                for (int i = 0; i < typeLabels.Length && i < 6; i++)
                    if (typeLabels[i]) typeLabels[i].text = $"Type {currentType[i]}";
            if (colorLabels != null)
                for (int i = 0; i < colorLabels.Length && i < 4; i++)
                    if (colorLabels[i]) colorLabels[i].text = $"Couleur {currentColor[i]}";
        }

        private void SaveToPrefs()
        {
            for (int i = 0; i < CategoryNames.Length; i++)
            {
                PlayerPrefs.SetInt($"Face_{CategoryNames[i]}_Type", currentType[i]);
                PlayerPrefs.SetInt($"Face_{CategoryNames[i]}_Color", currentColor[i]);
            }
            PlayerPrefs.Save();
        }

        private void LoadFromPrefs()
        {
            for (int i = 0; i < CategoryNames.Length; i++)
            {
                currentType[i] = PlayerPrefs.GetInt($"Face_{CategoryNames[i]}_Type", 1);
                currentColor[i] = PlayerPrefs.GetInt($"Face_{CategoryNames[i]}_Color", 1);
            }
            UpdateLabels();
            for (int i = 0; i < CategoryNames.Length; i++) ApplyToCharacter(i);
        }

        // Static utility for applying saved customization to any GanzSe character
        public static void ApplyFaceCustomization(GameObject character)
        {
            Transform fRoot = null;
            foreach (Transform t in character.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "FACE DETAILS PARTS" || t.name == "FACE DETAILS")
                { fRoot = t; break; }
            }
            if (fRoot == null) return;

            for (int i = 0; i < CategoryNames.Length; i++)
            {
                int type = PlayerPrefs.GetInt($"Face_{CategoryNames[i]}_Type", 1);
                int color = PlayerPrefs.GetInt($"Face_{CategoryNames[i]}_Color", 1);

                string prefix = CategoryNames[i] switch
                {
                    "EYES" => "Eyes", "HAIR" => "Hair", "FACE HAIR" => "Face Hair",
                    "EYEBROWS" => "Eyebrow", "NOSE" => "Nose", "EARS" => "Ears", _ => ""
                };

                string target = MaxColors[i] > 0
                    ? $"{prefix} Type {type} Color {color}"
                    : $"{prefix} Type {type}";

                foreach (Transform child in fRoot)
                {
                    if (child.name.ToUpper().Contains(CategoryNames[i]))
                    {
                        for (int j = 0; j < child.childCount; j++)
                            child.GetChild(j).gameObject.SetActive(false);
                        for (int j = 0; j < child.childCount; j++)
                        {
                            if (child.GetChild(j).name == target)
                            { child.GetChild(j).gameObject.SetActive(true); break; }
                        }
                        break;
                    }
                }
            }
        }
    }
}
