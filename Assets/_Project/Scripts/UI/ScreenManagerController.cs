using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DonGeonMaster.UI
{
    /// <summary>
    /// Screenshot tool for equipment items.
    /// Spawns each armor piece in 3D, lets user adjust camera with sliders,
    /// and takes a screenshot to use as inventory icon.
    /// </summary>
    public class ScreenManagerController : MonoBehaviour
    {
        [Header("Items")]
        [SerializeField] private GameObject[] itemPrefabs;
        [SerializeField] private string[] itemNames;

        [Header("Camera")]
        [SerializeField] private Camera renderCamera;
        [SerializeField] private Light sceneLight;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI itemLabel;
        [SerializeField] private TextMeshProUGUI valuesLabel;
        [SerializeField] private Slider sliderDist, sliderRotY, sliderHeight;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private RawImage thumbPreview;

        private int currentIndex;
        private GameObject currentInstance;
        private RenderTexture previewRT;
        private Vector3 itemCenter;
        private Texture2D loadedThumb; // currently loaded thumbnail texture

        private const int ThumbSize = 128;
        private const string ThumbnailPath = "Assets/_Project/Art/Textures/Thumbnails";

        private void Start()
        {
            previewRT = new RenderTexture(256, 256, 16);
            if (renderCamera != null)
                renderCamera.targetTexture = previewRT;
            if (previewImage != null)
                previewImage.texture = previewRT;

            if (sliderDist != null) sliderDist.onValueChanged.AddListener(_ => UpdateCamera());
            if (sliderRotY != null) sliderRotY.onValueChanged.AddListener(_ => UpdateCamera());
            if (sliderHeight != null) sliderHeight.onValueChanged.AddListener(_ => UpdateCamera());

            if (itemPrefabs != null && itemPrefabs.Length > 0)
                ShowItem(0);
        }

        private void OnDestroy()
        {
            if (previewRT != null)
            {
                previewRT.Release();
                Destroy(previewRT);
            }
        }

        public void NextItem()
        {
            if (itemPrefabs == null || itemPrefabs.Length == 0) return;
            currentIndex = (currentIndex + 1) % itemPrefabs.Length;
            ShowItem(currentIndex);
        }

        public void PrevItem()
        {
            if (itemPrefabs == null || itemPrefabs.Length == 0) return;
            currentIndex = (currentIndex - 1 + itemPrefabs.Length) % itemPrefabs.Length;
            ShowItem(currentIndex);
        }

        private void ShowItem(int index)
        {
            if (currentInstance != null) Destroy(currentInstance);

            currentIndex = index;
            var prefab = itemPrefabs[index];
            if (prefab == null) return;

            currentInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            currentInstance.name = "PreviewItem";

            // Fix materials to URP if needed
            FixMaterials(currentInstance);

            // Calculate bounds for auto-framing
            var renderers = currentInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);
                itemCenter = bounds.center;

                // Auto-set distance based on object size
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float dist = size / (2f * Mathf.Tan(30f * 0.5f * Mathf.Deg2Rad)) * 1.3f;
                dist = Mathf.Max(dist, 0.3f);

                // Load saved camera settings, or keep current slider values (inherited from previous item)
                string key = GetItemKey();
                if (key != null && PlayerPrefs.HasKey($"Scr_{key}_D"))
                {
                    if (sliderDist != null) sliderDist.SetValueWithoutNotify(PlayerPrefs.GetFloat($"Scr_{key}_D", dist));
                    if (sliderRotY != null) sliderRotY.SetValueWithoutNotify(PlayerPrefs.GetFloat($"Scr_{key}_R", 30f));
                    if (sliderHeight != null) sliderHeight.SetValueWithoutNotify(PlayerPrefs.GetFloat($"Scr_{key}_H", 0.2f));
                }
                // else: keep current slider values — inherited from previous item
            }

            UpdateCamera();
            UpdateLabel();
            LoadExistingThumbnail();
        }

        private void LoadExistingThumbnail()
        {
            // Clean up previous texture
            if (loadedThumb != null) { Destroy(loadedThumb); loadedThumb = null; }

            if (thumbPreview == null) return;

            string key = GetItemKey();
            if (key == null) { thumbPreview.texture = null; return; }

            string pngPath = $"{Application.dataPath}/_Project/Art/Textures/Thumbnails/Thumb_{key}.png";
            if (File.Exists(pngPath))
            {
                byte[] bytes = File.ReadAllBytes(pngPath);
                loadedThumb = new Texture2D(2, 2);
                loadedThumb.LoadImage(bytes);
                thumbPreview.texture = loadedThumb;
                thumbPreview.color = Color.white;
            }
            else
            {
                thumbPreview.texture = null;
                thumbPreview.color = new Color(0.15f, 0.13f, 0.10f, 0.5f);
            }
        }

        private void UpdateCamera()
        {
            if (renderCamera == null || currentInstance == null) return;

            float dist = sliderDist != null ? sliderDist.value : 1f;
            float rotY = sliderRotY != null ? sliderRotY.value : 30f;
            float height = sliderHeight != null ? sliderHeight.value : 0.2f;

            float rad = rotY * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad) * dist, height, -Mathf.Cos(rad) * dist);
            renderCamera.transform.position = itemCenter + offset;
            renderCamera.transform.LookAt(itemCenter);

            // Light always faces the item from camera direction (no dark side)
            if (sceneLight != null)
                sceneLight.transform.rotation = renderCamera.transform.rotation;

            if (valuesLabel != null)
                valuesLabel.text = $"Dist: {dist:F2}  Rot: {rotY:F0}°  H: {height:F2}";
        }

        private void UpdateLabel()
        {
            if (itemLabel == null) return;
            string name = (itemNames != null && currentIndex < itemNames.Length)
                ? itemNames[currentIndex]
                : "Item " + (currentIndex + 1);
            itemLabel.text = $"{name}\n<size=16>({currentIndex + 1}/{itemPrefabs.Length})</size>";
        }

        private string GetItemKey()
        {
            if (itemNames == null || currentIndex >= itemNames.Length) return null;
            return itemNames[currentIndex].Replace(" ", "_");
        }

        public void TakeScreenshot()
        {
            if (renderCamera == null || currentInstance == null) return;

            // Save camera settings
            string key = GetItemKey();
            if (key != null)
            {
                PlayerPrefs.SetFloat($"Scr_{key}_D", sliderDist != null ? sliderDist.value : 1f);
                PlayerPrefs.SetFloat($"Scr_{key}_R", sliderRotY != null ? sliderRotY.value : 30f);
                PlayerPrefs.SetFloat($"Scr_{key}_H", sliderHeight != null ? sliderHeight.value : 0.2f);
                PlayerPrefs.Save();
            }

            // Render to high-quality texture
            var thumbRT = new RenderTexture(ThumbSize, ThumbSize, 16);
            renderCamera.targetTexture = thumbRT;
            renderCamera.Render();

            RenderTexture.active = thumbRT;
            var tex = new Texture2D(ThumbSize, ThumbSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, ThumbSize, ThumbSize), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // Restore preview RT
            renderCamera.targetTexture = previewRT;

            // Save PNG
            string safeName = key ?? "unknown";
            string dirPath = Application.dataPath + "/_Project/Art/Textures/Thumbnails";
            Directory.CreateDirectory(dirPath);
            string pngPath = $"{dirPath}/Thumb_{safeName}.png";
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Destroy(tex);

            thumbRT.Release();
            Destroy(thumbRT);

            if (valuesLabel != null)
                valuesLabel.text += $"\n<color=#4CAF50>Sauvé: Thumb_{safeName}.png</color>";

            Debug.Log($"[Screenshot] Saved: {pngPath}");

            // Refresh the thumbnail preview
            LoadExistingThumbnail();
        }

        private void FixMaterials(GameObject obj)
        {
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) return;

            foreach (var rend in obj.GetComponentsInChildren<Renderer>(true))
            {
                var mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].shader.name == "Standard")
                    {
                        var mainTex = mats[i].GetTexture("_MainTex");
                        mats[i] = new Material(urpShader);
                        if (mainTex != null) mats[i].SetTexture("_BaseMap", mainTex);
                        mats[i].SetColor("_BaseColor", Color.white);
                    }
                }
                rend.sharedMaterials = mats;
            }
        }

        public void ReturnToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
