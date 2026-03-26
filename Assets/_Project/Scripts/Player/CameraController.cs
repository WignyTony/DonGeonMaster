using UnityEngine;
using UnityEngine.InputSystem;

namespace DonGeonMaster.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Top-Back View")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -7f);
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private float lookDownAngle = 55f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 3f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 2f;

        private float currentZoom = 1f;

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            transform.rotation = Quaternion.Euler(lookDownAngle, 0f, 0f);
        }

        private void LateUpdate()
        {
            // Re-find player if reference was lost (scene reload)
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
                else return;
            }

            // Zoom via mouse scroll (New Input System)
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y / 120f;
                currentZoom -= scroll * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }

            // Follow position
            Vector3 desiredPos = target.position + offset * currentZoom;
            transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

            // Fixed top-back rotation
            transform.rotation = Quaternion.Euler(lookDownAngle, 0f, 0f);
        }
    }
}
