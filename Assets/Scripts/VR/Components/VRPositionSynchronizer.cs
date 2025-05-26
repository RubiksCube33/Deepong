using UnityEngine;

namespace VRMovement.Components
{
    /// <summary>
    /// Handles position synchronization between VR Origin and player character
    /// </summary>
    public class VRPositionSynchronizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform vrOrigin;
        [SerializeField] private Transform playerArmature;
        [SerializeField] private CharacterController characterController;

        [Header("Position Sync Settings")]
        [SerializeField] private bool followFullPosition = false;
        [SerializeField] private bool applyGravity = true;
        [SerializeField] private float positionSyncSpeed = 10f;
        [SerializeField] private Vector3 bodyTrackingOffset = Vector3.zero;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private StarterAssets.ThirdPersonController thirdPersonController;

        private void Start()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (characterController == null)
                characterController = playerArmature?.GetComponent<CharacterController>();

            if (thirdPersonController == null)
                thirdPersonController = playerArmature?.GetComponent<StarterAssets.ThirdPersonController>();
        }

        public void SyncPosition()
        {
            if (vrOrigin == null || playerArmature == null)
                return;

            Vector3 targetPosition = vrOrigin.position + bodyTrackingOffset;

            if (!followFullPosition)
            {
                targetPosition.y = playerArmature.position.y;
            }

            // Handle CharacterController position sync
            if (characterController != null && characterController.enabled)
            {
                // Disable temporarily for teleport
                characterController.enabled = false;
                playerArmature.position = Vector3.Lerp(playerArmature.position, targetPosition, Time.deltaTime * positionSyncSpeed);
                characterController.enabled = true;

                // Apply gravity if needed
                if (applyGravity && thirdPersonController != null)
                {
                    Vector3 verticalVelocity = new Vector3(0, thirdPersonController.Gravity * Time.deltaTime, 0);
                    characterController.Move(verticalVelocity * Time.deltaTime);
                }
            }
            else
            {
                playerArmature.position = Vector3.Lerp(playerArmature.position, targetPosition, Time.deltaTime * positionSyncSpeed);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[VRPositionSync] Target: {targetPosition}, Current: {playerArmature.position}");
            }
        }

        // Public setters for inspector and runtime configuration
        public void SetVROrigin(Transform origin) => vrOrigin = origin;
        public void SetPlayerArmature(Transform armature) => playerArmature = armature;
        public void SetCharacterController(CharacterController controller) => characterController = controller;
        public void SetFollowFullPosition(bool follow) => followFullPosition = follow;
        public void SetApplyGravity(bool gravity) => applyGravity = gravity;
        public void SetPositionSyncSpeed(float speed) => positionSyncSpeed = speed;
        public void SetBodyTrackingOffset(Vector3 offset) => bodyTrackingOffset = offset;
    }
} 