using UnityEngine;

namespace DonGeonMaster.Character
{
    /// <summary>
    /// Forces character arms into a natural resting pose.
    /// Uses world-space direction calculation instead of hardcoded Euler angles,
    /// so it works regardless of bone local axis orientation.
    /// </summary>
    public class IdlePoseController : MonoBehaviour
    {
        private Transform shoulderL, shoulderR;
        private Transform upperArmL, upperArmR;
        private Transform forearmL, forearmR;

        // Store the original T-pose rotations
        private Quaternion origShoulderL, origShoulderR;
        private Quaternion origUpperArmL, origUpperArmR;
        private Quaternion origForearmL, origForearmR;

        private bool bonesFound;

        private void Start()
        {
            var allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                string n = t.name.ToLower();
                if (n == "shoulder_l" || n == "shoulder.l") shoulderL = t;
                else if (n == "shoulder_r" || n == "shoulder.r") shoulderR = t;
                else if (n == "upperarm_l" || n == "upperarm.l") upperArmL = t;
                else if (n == "upperarm_r" || n == "upperarm.r") upperArmR = t;
                else if (n == "forearm_l" || n == "lowerarm.l") forearmL = t;
                else if (n == "forearm_r" || n == "lowerarm.r") forearmR = t;
            }

            bonesFound = upperArmL != null || upperArmR != null;

            if (bonesFound)
            {
                // Capture T-pose rotations
                if (shoulderL) origShoulderL = shoulderL.localRotation;
                if (shoulderR) origShoulderR = shoulderR.localRotation;
                if (upperArmL) origUpperArmL = upperArmL.localRotation;
                if (upperArmR) origUpperArmR = upperArmR.localRotation;
                if (forearmL) origForearmL = forearmL.localRotation;
                if (forearmR) origForearmR = forearmR.localRotation;

                Debug.Log($"[IdlePose] Bones found: SL={shoulderL?.name} SR={shoulderR?.name} " +
                          $"UAL={upperArmL?.name} UAR={upperArmR?.name} " +
                          $"FAL={forearmL?.name} FAR={forearmR?.name}");
            }
        }

        private void LateUpdate()
        {
            if (!bonesFound) return;

            // Apply a rotation RELATIVE to the T-pose orientation
            // This works regardless of the bone's local axis setup
            // We rotate arms downward by applying a world-space rotation offset

            // Left side: rotate 70° downward (positive around character's forward axis)
            if (shoulderL)
                shoulderL.localRotation = origShoulderL * Quaternion.AngleAxis(70f, Vector3.forward);
            if (upperArmL)
                upperArmL.localRotation = origUpperArmL * Quaternion.AngleAxis(10f, Vector3.forward);
            if (forearmL)
                forearmL.localRotation = origForearmL * Quaternion.AngleAxis(15f, Vector3.right);

            // Right side: rotate 70° downward (negative around forward for right side)
            if (shoulderR)
                shoulderR.localRotation = origShoulderR * Quaternion.AngleAxis(-70f, Vector3.forward);
            if (upperArmR)
                upperArmR.localRotation = origUpperArmR * Quaternion.AngleAxis(-10f, Vector3.forward);
            if (forearmR)
                forearmR.localRotation = origForearmR * Quaternion.AngleAxis(-15f, Vector3.right);
        }
    }
}
