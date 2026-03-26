using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace DonGeonMaster.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Character Showcase")]
        [SerializeField] private CharacterShowcase showcase;
        [SerializeField] private TextMeshProUGUI characterNameLabel;
        [SerializeField] private GameObject characterNav;

        [Header("Customizer")]
        [SerializeField] private CharacterCustomizer customizer;

        public void OnPlayClicked()
        {
            SceneManager.LoadScene("Hub");
        }

        public void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                bool opening = !settingsPanel.activeSelf;
                settingsPanel.SetActive(opening);
                if (characterNav != null) characterNav.SetActive(!opening);
            }
        }

        public void OnCustomizeClicked()
        {
            if (customizer != null)
            {
                customizer.Toggle();

                // If customizer just closed, refresh the showcase with new face settings
                if (!customizer.IsOpen && showcase != null)
                    showcase.RefreshGanzSe();
            }
        }

        public void OnAnimPreviewClicked()
        {
            SceneManager.LoadScene("AnimationPreview");
        }

        public void OnScreenManagerClicked()
        {
            SceneManager.LoadScene("ScreenManager");
        }

        public void OnItemEditorClicked()
        {
            SceneManager.LoadScene("ItemEditor");
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnNextCharacter()
        {
            if (showcase != null) { showcase.Next(); UpdateCharacterLabel(); }
        }

        public void OnPrevCharacter()
        {
            if (showcase != null) { showcase.Previous(); UpdateCharacterLabel(); }
        }

        private void Start() { UpdateCharacterLabel(); }

        private void UpdateCharacterLabel()
        {
            if (characterNameLabel != null && showcase != null)
                characterNameLabel.text = showcase.CurrentName;
        }
    }
}
