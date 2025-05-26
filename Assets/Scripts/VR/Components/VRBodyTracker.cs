using UnityEngine;
using VRMovement.Interfaces;

namespace VRMovement.Components
{
    /// <summary>
    /// Handles VR body tracking functionality
    /// </summary>
    public class VRBodyTracker : MonoBehaviour, IVRBodyTracker
    {
        [Header("Body Tracking Settings")]
        [SerializeField] private Transform vrOrigin;
        [SerializeField] private Transform playerArmature;
        [SerializeField] private float bodyTrackingThreshold = 0.1f;
        [SerializeField] private float bodyTrackingWeight = 1.0f;
        [SerializeField] private Vector3 bodyTrackingOffset = Vector3.zero;
        [SerializeField] private float bodyMovementDecayTime = 2.0f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Properties
        public bool IsBodyMoving { get; private set; }
        public Vector3 BodyMovementVelocity { get; private set; }

        // Private variables
        private Vector3 lastOriginPosition;
        private float lastBodyMovementTime;
        private bool isInitialized = false;

        private void Start()
        {
            if (vrOrigin != null)
            {
                lastOriginPosition = vrOrigin.position;
                isInitialized = true;
            }
            else
            {
                Debug.LogWarning("[VRBodyTracker] VR Origin not assigned!");
            }
        }

        public void UpdateBodyTracking()
        {
            if (!isInitialized || vrOrigin == null)
            {
                IsBodyMoving = false;
                return;
            }

            // Calculate body movement
            Vector3 currentOriginPosition = vrOrigin.position;
            Vector3 originMovementDelta = currentOriginPosition - lastOriginPosition;
            Vector3 horizontalMovement = new Vector3(originMovementDelta.x, 0, originMovementDelta.z);

            BodyMovementVelocity = horizontalMovement / Time.deltaTime;
            float movementMagnitude = BodyMovementVelocity.magnitude;

            // Check if body is moving
            if (movementMagnitude > bodyTrackingThreshold)
            {
                IsBodyMoving = true;
                lastBodyMovementTime = Time.time;
            }
            else if (Time.time - lastBodyMovementTime > bodyMovementDecayTime)
            {
                IsBodyMoving = false;
            }

            lastOriginPosition = currentOriginPosition;

            if (showDebugInfo)
            {
                Debug.Log($"[VRBodyTracker] Moving: {IsBodyMoving}, Velocity: {BodyMovementVelocity.magnitude:F2}");
            }
        }

        public Vector2 GetBodyMovementInput()
        {
            if (!IsBodyMoving || playerArmature == null)
                return Vector2.zero;

            Vector3 localBodyMovement = playerArmature.InverseTransformDirection(BodyMovementVelocity);
            Vector2 bodyInput = new Vector2(localBodyMovement.x, localBodyMovement.z) * bodyTrackingWeight;
            
            // Clamp to reasonable values
            return Vector2.ClampMagnitude(bodyInput, 1.0f);
        }

        public void SetBodyTrackingSettings(float threshold, float weight, Vector3 offset)
        {
            bodyTrackingThreshold = threshold;
            bodyTrackingWeight = weight;
            bodyTrackingOffset = offset;
        }

        // Public setters for inspector
        public void SetVROrigin(Transform origin) => vrOrigin = origin;
        public void SetPlayerArmature(Transform armature) => playerArmature = armature;
    }
} 