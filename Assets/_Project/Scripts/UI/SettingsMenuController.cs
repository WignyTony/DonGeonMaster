using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DonGeonMaster.UI
{
    public class SettingsMenuController : MonoBehaviour
    {
        [Header("Binding UI (set dynamically)")]
        [SerializeField] private Button[] bindButtons;
        [SerializeField] private TextMeshProUGUI[] bindLabels;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        private void OnEnable()
        {
            RefreshAllLabels();

            if (bindButtons == null) return;

            for (int i = 0; i < bindButtons.Length && i < KeyBindingManager.Actions.Length; i++)
            {
                int index = i; // Capture for closure
                string action = KeyBindingManager.Actions[index];
                bindButtons[i].onClick.AddListener(() => StartRebind(action, bindLabels[index]));
            }
        }

        private void OnDisable()
        {
            if (bindButtons == null) return;
            foreach (var btn in bindButtons)
            {
                if (btn != null) btn.onClick.RemoveAllListeners();
            }
        }

        private void StartRebind(string action, TextMeshProUGUI label)
        {
            if (KeyBindingManager.Instance == null || KeyBindingManager.Instance.IsRebinding) return;

            label.text = "...";
            SetAllButtonsInteractable(false);

            KeyBindingManager.Instance.StartRebind(action, (newKey) =>
            {
                label.text = newKey.ToString();
                SetAllButtonsInteractable(true);
            });
        }

        private void RefreshAllLabels()
        {
            if (KeyBindingManager.Instance == null || bindLabels == null) return;

            for (int i = 0; i < bindLabels.Length && i < KeyBindingManager.Actions.Length; i++)
            {
                string action = KeyBindingManager.Actions[i];
                bindLabels[i].text = KeyBindingManager.Instance.GetBindingDisplayName(action);
            }
        }

        private void SetAllButtonsInteractable(bool interactable)
        {
            if (bindButtons == null) return;
            foreach (var btn in bindButtons)
            {
                if (btn != null) btn.interactable = interactable;
            }
        }

        public void OnFullscreenToggled(bool isFullscreen)
        {
            if (isFullscreen)
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
            else
            {
                // Windowed at full screen resolution
                int w = Display.main.systemWidth;
                int h = Display.main.systemHeight;
                Screen.SetResolution(w, h, false);
            }
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Start()
        {
            bool fs = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            if (fs)
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            else
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, false);
        }

        public void OnResetClicked()
        {
            KeyBindingManager.Instance.ResetToDefaults();
            RefreshAllLabels();
        }

        public void OnBackClicked()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }
    }
}
