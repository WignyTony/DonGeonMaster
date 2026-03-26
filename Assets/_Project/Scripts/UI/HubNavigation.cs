using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonGeonMaster.UI
{
    public class HubNavigation : MonoBehaviour
    {
        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
