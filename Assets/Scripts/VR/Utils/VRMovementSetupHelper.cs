using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRMovement.Components;
using StarterAssets;

namespace VRMovement.Utils
{
    /// <summary>
    /// Helper script to automatically setup VR movement components
    /// </summary>
    public class VRMovementSetupHelper : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool findMissingReferences = true;

        [Header("Manual References (Optional)")]
        [SerializeField] private Transform vrOrigin;
        [SerializeField] private Transform playerArmature;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private XRController leftController;
        [SerializeField] private XRController rightController;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupVRMovementSystem();
            }
        }

        [ContextMenu("Setup VR Movement System")]
        public void SetupVRMovementSystem()
        {
            GameObject targetObject = gameObject;

            // Find missing references if enabled
            if (findMissingReferences)
            {
                FindMissingReferences();
            }

            // Add and setup VR Body Tracker
            SetupBodyTracker(targetObject);

            // Add and setup VR Controller Input
            SetupControllerInput(targetObject);

            // Add and setup VR Input Blender
            SetupInputBlender(targetObject);

            // Add and setup VR Position Synchronizer
            SetupPositionSynchronizer(targetObject);

            // Add and setup VR Movement Mode Manager
            SetupModeManager(targetObject);

            // Add main controller
            SetupMainController(targetObject);

            Debug.Log("[VRMovementSetupHelper] VR Movement System setup complete!");
        }

        private void FindMissingReferences()
        {
            // Find VR Origin
            if (vrOrigin == null)
            {
                var xrOrigin = FindObjectOfType<XROrigin>();
                if (xrOrigin != null)
                    vrOrigin = xrOrigin.transform;
            }

            // Find Player Armature
            if (playerArmature == null)
            {
                var thirdPersonController = FindObjectOfType<ThirdPersonController>();
                if (thirdPersonController != null)
                    playerArmature = thirdPersonController.transform;
            }

            // Find Main Camera
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                    mainCamera = FindObjectOfType<Camera>();
            }

            // Find Controllers
            if (leftController == null || rightController == null)
            {
                XRController[] controllers = FindObjectsOfType<XRController>();
                foreach (var controller in controllers)
                {
                    if (controller.controllerNode == UnityEngine.XR.XRNode.LeftHand)
                        leftController = controller;
                    else if (controller.controllerNode == UnityEngine.XR.XRNode.RightHand)
                        rightController = controller;
                }
            }
        }

        private void SetupBodyTracker(GameObject target)
        {
            var bodyTracker = target.GetComponent<VRBodyTracker>();
            if (bodyTracker == null)
            {
                bodyTracker = target.AddComponent<VRBodyTracker>();
            }

            // Setup references
            if (vrOrigin != null)
                bodyTracker.SetVROrigin(vrOrigin);
            if (playerArmature != null)
                bodyTracker.SetPlayerArmature(playerArmature);
        }

        private void SetupControllerInput(GameObject target)
        {
            var controllerInput = target.GetComponent<VRControllerInput>();
            if (controllerInput == null)
            {
                controllerInput = target.AddComponent<VRControllerInput>();
            }

            // Setup references
            if (leftController != null)
                controllerInput.SetLeftController(leftController);
            if (rightController != null)
                controllerInput.SetRightController(rightController);
        }

        private void SetupInputBlender(GameObject target)
        {
            var inputBlender = target.GetComponent<VRInputBlender>();
            if (inputBlender == null)
            {
                inputBlender = target.AddComponent<VRInputBlender>();
            }
        }

        private void SetupPositionSynchronizer(GameObject target)
        {
            var positionSync = target.GetComponent<VRPositionSynchronizer>();
            if (positionSync == null)
            {
                positionSync = target.AddComponent<VRPositionSynchronizer>();
            }

            // Setup references
            if (vrOrigin != null)
                positionSync.SetVROrigin(vrOrigin);
            if (playerArmature != null)
            {
                positionSync.SetPlayerArmature(playerArmature);
                var characterController = playerArmature.GetComponent<CharacterController>();
                if (characterController != null)
                    positionSync.SetCharacterController(characterController);
            }
        }

        private void SetupModeManager(GameObject target)
        {
            var modeManager = target.GetComponent<VRMovementModeManager>();
            if (modeManager == null)
            {
                modeManager = target.AddComponent<VRMovementModeManager>();
            }
        }

        private void SetupMainController(GameObject target)
        {
            var mainController = target.GetComponent<HybridVRMovementControllerRefactored>();
            if (mainController == null)
            {
                mainController = target.AddComponent<HybridVRMovementControllerRefactored>();
            }
        }

        [ContextMenu("Remove VR Movement System")]
        public void RemoveVRMovementSystem()
        {
            var components = new System.Type[]
            {
                typeof(HybridVRMovementControllerRefactored),
                typeof(VRBodyTracker),
                typeof(VRControllerInput),
                typeof(VRInputBlender),
                typeof(VRPositionSynchronizer),
                typeof(VRMovementModeManager)
            };

            foreach (var componentType in components)
            {
                var component = gameObject.GetComponent(componentType);
                if (component != null)
                {
                    DestroyImmediate(component);
                }
            }

            Debug.Log("[VRMovementSetupHelper] VR Movement System removed!");
        }
    }
} 