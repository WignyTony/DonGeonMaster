using UnityEngine;

namespace DonGeonMaster.Effects
{
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        [Header("Intensity")]
        [SerializeField] private float baseIntensity = 1.5f;
        [SerializeField] private float intensityVariation = 0.4f;
        [SerializeField] private float flickerSpeed = 3f;

        [Header("Movement")]
        [SerializeField] private float moveAmount = 0.05f;
        [SerializeField] private float moveSpeed = 2f;

        private Light torchLight;
        private Vector3 startPos;
        private float randomOffset;

        private void Awake()
        {
            torchLight = GetComponent<Light>();
            startPos = transform.localPosition;
            randomOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float time = Time.time + randomOffset;

            // Flicker intensity using multiple Perlin noise octaves
            float noise1 = Mathf.PerlinNoise(time * flickerSpeed, 0f);
            float noise2 = Mathf.PerlinNoise(time * flickerSpeed * 2.3f, 5f) * 0.5f;
            float noise3 = Mathf.PerlinNoise(time * flickerSpeed * 4.7f, 10f) * 0.25f;
            float combined = (noise1 + noise2 + noise3) / 1.75f;

            torchLight.intensity = baseIntensity + (combined - 0.5f) * intensityVariation * 2f;

            // Subtle position movement
            float moveX = (Mathf.PerlinNoise(time * moveSpeed, 20f) - 0.5f) * moveAmount;
            float moveZ = (Mathf.PerlinNoise(time * moveSpeed, 30f) - 0.5f) * moveAmount;
            transform.localPosition = startPos + new Vector3(moveX, 0f, moveZ);
        }
    }
}
