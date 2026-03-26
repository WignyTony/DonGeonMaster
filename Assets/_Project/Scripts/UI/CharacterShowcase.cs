using UnityEngine;
using DonGeonMaster.Character;

namespace DonGeonMaster.UI
{
    public class CharacterShowcase : MonoBehaviour
    {
        [Header("GanzSe Prefab")]
        [SerializeField] private GameObject ganzsePrefab;
        [SerializeField] private RuntimeAnimatorController animController;

        [Header("Display")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float rotationSpeed = 30f;

        private GameObject currentCharacter;

        public GameObject CurrentCharacter => currentCharacter;
        public string CurrentName => "Hero";

        private void Start()
        {
            if (ganzsePrefab != null)
                ShowGanzSeCharacter();
        }

        private void Update()
        {
            if (currentCharacter != null)
                currentCharacter.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }

        public void Next() { }
        public void Previous() { }

        private void ShowGanzSeCharacter()
        {
            if (currentCharacter != null)
                Destroy(currentCharacter);

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            currentCharacter = Instantiate(ganzsePrefab, pos, Quaternion.identity);
            currentCharacter.name = "ShowcaseHero";

            GanzSeHelper.DisableAllArmor(currentCharacter);
            CharacterCustomizer.ApplyFaceCustomization(currentCharacter);

            // Use Animator with Idle animation
            var animator = currentCharacter.GetComponent<Animator>();
            if (animator == null) animator = currentCharacter.AddComponent<Animator>();
            animator.applyRootMotion = false;
            var ctrl = animController;
            if (ctrl == null) ctrl = Resources.Load<RuntimeAnimatorController>("AnimPreviewController");
            if (ctrl != null)
            {
                animator.runtimeAnimatorController = ctrl;
                animator.Play("Default", 0, 0);
                animator.Update(0);
            }
        }

        public void RefreshGanzSe()
        {
            if (ganzsePrefab != null)
                ShowGanzSeCharacter();
        }
    }
}
