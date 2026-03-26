using UnityEngine;
using DonGeonMaster.UI;

namespace DonGeonMaster.Character
{
    /// <summary>
    /// Applies saved face customization from PlayerPrefs at runtime Start.
    /// Also disables all armor (show only equipped pieces).
    /// Attach to any GanzSe character that needs runtime face sync.
    /// </summary>
    public class ApplyFaceOnStart : MonoBehaviour
    {
        private void Start()
        {
            GanzSeHelper.DisableAllArmor(gameObject);
            CharacterCustomizer.ApplyFaceCustomization(gameObject);
        }
    }
}
