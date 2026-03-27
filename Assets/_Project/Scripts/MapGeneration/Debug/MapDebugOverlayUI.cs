using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Phase 4 : overlay discret en haut a droite.
    /// Visible uniquement en mode TopDown. Invisible en Config et Hero.
    /// </summary>
    public class MapDebugOverlayUI : MonoBehaviour
    {
        GameObject panel;
        TextMeshProUGUI statText;

        public void Build()
        {
            // Canvas (propre, pas partage avec la sidebar)
            var canvasGO = new GameObject("OverlayCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            // Pas de GraphicRaycaster : l'overlay ne bloque aucun clic

            // Petit bandeau haut-droite
            panel = new GameObject("OverlayPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1); // coin haut-droite
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(420, 80);
            panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.10f, 0.75f);
            panel.GetComponent<Image>().raycastTarget = false;

            // Texte (enfant)
            var txtGO = new GameObject("Stats");
            txtGO.transform.SetParent(panel.transform, false);
            var trt = txtGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12, 6);
            trt.offsetMax = new Vector2(-12, -6);
            statText = txtGO.AddComponent<TextMeshProUGUI>();
            statText.text = "";
            statText.fontSize = 12;
            statText.color = new Color(0.8f, 0.8f, 0.85f);
            statText.alignment = TextAlignmentOptions.TopLeft;
            statText.raycastTarget = false;

            panel.SetActive(false);
        }

        public void UpdateStats(GenerationResult r)
        {
            if (r == null || statText == null) return;

            string statusColor = r.status switch
            {
                GenerationStatus.Succes => "#66DD77",
                GenerationStatus.SuccesAvecWarnings => "#DDCC44",
                _ => "#DD4444"
            };

            statText.text =
                $"<color={statusColor}>{r.status}</color>  |  " +
                $"Seed: {r.seed}  |  " +
                $"Temps: {r.generationTimeMs:F1}ms\n" +
                $"Salles: {r.roomCount}  |  " +
                $"Couloirs: {r.corridorCount}  |  " +
                $"Cellules: {r.walkableCellCount + r.wallCellCount}  |  " +
                $"<color=#DD4444>E:{r.errorCount}</color>  " +
                $"<color=#DDCC44>W:{r.warningCount}</color>";
        }

        public void Show() { if (panel != null) panel.SetActive(true); }
        public void Hide() { if (panel != null) panel.SetActive(false); }
    }
}
