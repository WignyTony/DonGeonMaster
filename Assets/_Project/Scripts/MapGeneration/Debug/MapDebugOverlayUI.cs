using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Overlay discret haut-droite. Visible en TopDown uniquement.
    /// Ajoute validation runtime : si le renderer n'a rien construit, affiche RENDER ECHEC.
    /// </summary>
    public class MapDebugOverlayUI : MonoBehaviour
    {
        GameObject panel;
        TextMeshProUGUI statText;

        public void Build()
        {
            var canvasGO = new GameObject("OverlayCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            panel = new GameObject("OverlayPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(480, 90);
            panel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.80f);
            panel.GetComponent<Image>().raycastTarget = false;

            var txtGO = new GameObject("Stats");
            txtGO.transform.SetParent(panel.transform, false);
            var trt = txtGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 4);
            trt.offsetMax = new Vector2(-10, -4);
            statText = txtGO.AddComponent<TextMeshProUGUI>();
            statText.text = "";
            statText.fontSize = 11;
            statText.color = new Color(0.8f, 0.8f, 0.85f);
            statText.alignment = TextAlignmentOptions.TopLeft;
            statText.raycastTarget = false;
            statText.richText = true;

            panel.SetActive(false);
        }

        /// <summary>
        /// Met a jour l'overlay avec les resultats de generation + validation runtime.
        /// </summary>
        public void UpdateStats(GenerationResult r, MapStructureDebugRenderer renderer)
        {
            if (statText == null) return;
            if (r == null) { statText.text = "Aucune generation"; return; }

            // Validation runtime : le renderer a-t-il reellement construit quelque chose ?
            bool renderOK = renderer != null && renderer.HasRendered;
            bool spawnOK = renderer != null && renderer.HasSpawnMarker;
            bool exitOK = renderer != null && renderer.HasExitMarker;

            string renderStatus;
            if (!renderOK)
                renderStatus = "<color=#DD4444>RENDER ECHEC</color>";
            else if (!spawnOK || !exitOK)
                renderStatus = "<color=#DDCC44>RENDER PARTIEL</color>";
            else
                renderStatus = "<color=#66DD77>RENDER OK</color>";

            string logicStatus = r.status switch
            {
                GenerationStatus.Succes => "<color=#66DD77>LOGIQUE OK</color>",
                GenerationStatus.SuccesAvecWarnings => "<color=#DDCC44>LOGIQUE WARN</color>",
                _ => "<color=#DD4444>LOGIQUE ECHEC</color>"
            };

            statText.text =
                $"{logicStatus}  |  {renderStatus}\n" +
                $"Seed: <color=#AACCFF>{r.seed}</color>  |  " +
                $"Temps: {r.generationTimeMs:F1}ms  |  " +
                $"Salles: {r.roomCount}  |  Couloirs: {r.corridorCount}\n" +
                $"Sol: {(renderer != null ? renderer.RenderedFloorCount : 0)}  |  " +
                $"Murs: {(renderer != null ? renderer.RenderedWallCount : 0)}  |  " +
                $"Spawn: {(spawnOK ? "OK" : "NON")}  |  Exit: {(exitOK ? "OK" : "NON")}  |  " +
                $"<color=#DD4444>E:{r.errorCount}</color>  " +
                $"<color=#DDCC44>W:{r.warningCount}</color>  " +
                $"<color=#88AACC>[F5=Regen Tab=Config]</color>";
        }

        public void Show() { if (panel != null) panel.SetActive(true); }
        public void Hide() { if (panel != null) panel.SetActive(false); }
    }
}
