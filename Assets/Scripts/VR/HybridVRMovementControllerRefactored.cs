using UnityEngine;
using StarterAssets;
using VRMovement.Interfaces;
using VRMovement.Components;

namespace VRMovement
{
    /// <summary>
    /// Refactored Hybrid VR Movement Controller that uses modular components
    /// Orchestrates body tracking, controller input, and movement blending
    /// </summary>
    public class HybridVRMovementControllerRefactored : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private VRBodyTracker bodyTracker;
        [SerializeField] private VRControllerInput controllerInput;
        [SerializeField] private VRInputBlender inputBlender;
        [SerializeField] private VRPositionSynchronizer positionSync;
        [SerializeField] private VRMovementModeManager modeManager;

        [Header("Character References")]
        [SerializeField] private Transform playerArmature;
        [SerializeField] private StarterAssetsInputs starterAssetsInputs;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Component interfaces for dependency injection
        private IVRBodyTracker bodyTrackerInterface;
        private IVRControllerInput controllerInputInterface;
        private IVRInputBlender inputBlenderInterface;

        private void Awake()
        {
            InitializeComponents();
            SetupInterfaces();
        }

        private void Start()
        {
            SetupEventListeners();
        }

        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (bodyTracker == null)
                bodyTracker = GetComponent<VRBodyTracker>();
            
            if (controllerInput == null)
                controllerInput = GetComponent<VRControllerInput>();
            
            if (inputBlender == null)
                inputBlender = GetComponent<VRInputBlender>();
            
            if (positionSync == null)
                positionSync = GetComponent<VRPositionSynchronizer>();
            
            if (modeManager == null)
                modeManager = GetComponent<VRMovementModeManager>();

            // Find StarterAssetsInputs if not assigned
            if (starterAssetsInputs == null)
            {
                starterAssetsInputs = GetComponent<StarterAssetsInputs>();
                if (starterAssetsInputs == null && playerArmature != null)
                    starterAssetsInputs = playerArmature.GetComponent<StarterAssetsInputs>();
            }

            // Validate required components
            ValidateComponents();
        }

        private void SetupInterfaces()
        {
            bodyTrackerInterface = bodyTracker;
            controllerInputInterface = controllerInput;
            inputBlenderInterface = inputBlender;
        }

        private void SetupEventListeners()
        {
            if (modeManager != null)
            {
                modeManager.OnModeChanged += OnMovementModeChanged;
            }
        }

        private void ValidateComponents()
        {
            if (bodyTracker == null)
                Debug.LogError("[HybridVRMovementControllerRefactored] VRBodyTracker component not found!");
            
            if (controllerInput == null)
                Debug.LogError("[HybridVRMovementControllerRefactored] VRControllerInput component not found!");
            
            if (inputBlender == null)
                Debug.LogError("[HybridVRMovementControllerRefactored] VRInputBlender component not found!");
            
            if (modeManager == null)
                Debug.LogError("[HybridVRMovementControllerRefactored] VRMovementModeManager component not found!");
        }

        private void Update()
        {
            UpdateMovementSystem();
        }

        private void UpdateMovementSystem()
        {
            // Update all input systems
            UpdateInputSystems();

            // Handle mode switching
            HandleModeSwitch();

            // Calculate and apply movement
            CalculateAndApplyMovement();

            // Sync position if enabled
            SyncPosition();

            // Debug information
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }

        private void UpdateInputSystems()
        {
            // Update body tracking
            if (bodyTrackerInterface != null && modeManager.UsesBodyTracking())
            {
                bodyTrackerInterface.UpdateBodyTracking();
            }

            // Update controller input
            if (controllerInputInterface != null && modeManager.UsesControllerInput())
            {
                controllerInputInterface.UpdateControllerInput();
            }
        }

        private void HandleModeSwitch()
        {
            if (modeManager != null && controllerInput != null)
            {
                bool modeSwitchPressed = controllerInput.GetModeSwitchInput();
                modeManager.HandleModeSwitch(modeSwitchPressed);
            }
        }

        private void CalculateAndApplyMovement()
        {
            if (inputBlenderInterface == null || modeManager == null)
                return;

            // Get inputs from each system
            Vector2 bodyInput = bodyTrackerInterface?.GetBodyMovementInput() ?? Vector2.zero;
            Vector2 controllerMovementInput = controllerInputInterface?.GetControllerMovementInput() ?? Vector2.zero;

            // Update blend weights
            bool isBodyMoving = bodyTrackerInterface?.IsBodyMoving ?? false;
            bool isControllerMoving = controllerInputInterface?.IsControllerMoving ?? false;
            float controllerMagnitude = controllerMovementInput.magnitude;

            inputBlenderInterface.UpdateBlendWeights(isBodyMoving, isControllerMoving, controllerMagnitude, modeManager.CurrentMode);

            // Blend inputs
            Vector2 finalMovementInput = inputBlenderInterface.BlendInputs(bodyInput, controllerMovementInput, modeManager.CurrentMode);

            // Apply movement to character
            ApplyMovementToCharacter(finalMovementInput);
        }

        private void ApplyMovementToCharacter(Vector2 movementInput)
        {
            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.move = movementInput;

                // Handle other inputs from controllers
                if (controllerInput != null)
                {
                    starterAssetsInputs.jump = controllerInput.GetJumpInput();
                    starterAssetsInputs.sprint = controllerInput.GetSprintInput();
                }
            }
        }

        private void SyncPosition()
        {
            if (positionSync != null)
            {
                positionSync.SyncPosition();
            }
        }

        private void DisplayDebugInfo()
        {
            if (bodyTrackerInterface == null || controllerInputInterface == null || inputBlenderInterface == null || modeManager == null)
                return;

            string debugText = $"Mode: {modeManager.CurrentMode}\n";
            debugText += $"Body Moving: {bodyTrackerInterface.IsBodyMoving} (Vel: {bodyTrackerInterface.BodyMovementVelocity.magnitude:F2})\n";
            debugText += $"Controller Moving: {controllerInputInterface.IsControllerMoving} (Input: {controllerInputInterface.ControllerInput.magnitude:F2})\n";
            debugText += $"Weights - Body: {inputBlenderInterface.CurrentBodyWeight:F2}, Controller: {inputBlenderInterface.CurrentControllerWeight:F2}";

            Debug.Log($"[HybridVRMovementRefactored] {debugText}");
        }

        // Event handlers
        private void OnMovementModeChanged(MovementMode newMode)
        {
            Debug.Log($"[HybridVRMovementControllerRefactored] Movement mode changed to: {newMode}");
        }

        private void OnDestroy()
        {
            // Cleanup event listeners
            if (modeManager != null)
            {
                modeManager.OnModeChanged -= OnMovementModeChanged;
            }
        }

        // Public API for external control
        public void SetMovementMode(MovementMode mode)
        {
            modeManager?.SetMovementMode(mode);
        }

        public void EnableBodyTracking(bool enable)
        {
            if (bodyTracker != null)
                bodyTracker.enabled = enable;
        }

        public void EnableControllerMovement(bool enable)
        {
            if (controllerInput != null)
                controllerInput.enabled = enable;
        }

        public MovementMode GetCurrentMode()
        {
            return modeManager?.CurrentMode ?? MovementMode.Hybrid;
        }

        public bool IsBodyMoving()
        {
            return bodyTrackerInterface?.IsBodyMoving ?? false;
        }

        public bool IsControllerMoving()
        {
            return controllerInputInterface?.IsControllerMoving ?? false;
        }

        // Component getters for external access
        public VRBodyTracker GetBodyTracker() => bodyTracker;
        public VRControllerInput GetControllerInput() => controllerInput;
        public VRInputBlender GetInputBlender() => inputBlender;
        public VRPositionSynchronizer GetPositionSynchronizer() => positionSync;
        public VRMovementModeManager GetModeManager() => modeManager;
    }
} 