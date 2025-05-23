using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using StarterAssets;
using System.Collections.Generic;

/// <summary>
/// Hybrid VR Movement Controller that combines body tracking and controller input
/// Intelligently switches between or blends body tracking and controller-based movement
/// </summary>
public class HybridVRMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform vrOrigin;
    [SerializeField] private Transform playerArmature;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private XRController leftController;
    [SerializeField] private XRController rightController;

    [Header("Movement Mode")]
    [SerializeField] private MovementMode currentMode = MovementMode.Hybrid;
    [SerializeField] private bool allowModeSwitch = true;
    [SerializeField] private InputHelpers.Button modeSwitchButton = InputHelpers.Button.MenuButton;

    [Header("Body Tracking Settings")]
    [SerializeField] private bool enableBodyTracking = true;
    [SerializeField] private float bodyTrackingThreshold = 0.1f; // Minimum movement to consider as body tracking
    [SerializeField] private float bodyTrackingWeight = 1.0f;
    [SerializeField] private Vector3 bodyTrackingOffset = Vector3.zero;

    [Header("Controller Movement Settings")]
    [SerializeField] private bool enableControllerMovement = true;
    [SerializeField] private float controllerMovementWeight = 1.0f;
    [SerializeField] private float deadzone = 0.1f;
    [SerializeField] private float moveScale = 1.0f;
    [SerializeField] private bool invertY = false;

    [Header("Hybrid Settings")]
    [SerializeField] private float blendSpeed = 5.0f;
    [SerializeField] private float controllerOverrideThreshold = 0.3f; // Joystick input above this overrides body tracking
    [SerializeField] private float bodyMovementDecayTime = 2.0f; // Time to wait before body movement stops affecting blend

    [Header("Position Sync")]
    [SerializeField] private bool followFullPosition = false;
    [SerializeField] private bool applyGravity = true;
    [SerializeField] private float positionSyncSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    public enum MovementMode
    {
        BodyTrackingOnly,    // Only use body tracking for movement
        ControllerOnly,      // Only use controller input for movement
        Hybrid,              // Intelligently blend both
        AdditiveHybrid       // Add controller movement on top of body tracking
    }

    // Private variables
    private StarterAssetsInputs starterAssetsInputs;
    private ThirdPersonController thirdPersonController;
    private CharacterController characterController;
    private Animator animator;

    // Body tracking data
    private Vector3 lastOriginPosition;
    private Vector3 bodyMovementVelocity;
    private float lastBodyMovementTime;
    private bool isBodyMoving;

    // Controller input data
    private Vector2 controllerInput;
    private bool isControllerMoving;

    // Blending data
    private float currentBodyWeight = 1.0f;
    private float currentControllerWeight = 0.0f;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeComponents();
        FindControllers();
    }

    private void Start()
    {
        if (vrOrigin != null)
        {
            lastOriginPosition = vrOrigin.position;
            isInitialized = true;
        }
    }

    private void InitializeComponents()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        if (starterAssetsInputs == null)
            starterAssetsInputs = playerArmature?.GetComponent<StarterAssetsInputs>();
            
        if (thirdPersonController == null)
            thirdPersonController = playerArmature?.GetComponent<ThirdPersonController>();
            
        if (characterController == null)
            characterController = playerArmature?.GetComponent<CharacterController>();
            
        if (animator == null)
            animator = playerArmature?.GetComponent<Animator>();
    }

    private void FindControllers()
    {
        if (leftController == null || rightController == null)
        {
            XRController[] controllers = FindObjectsOfType<XRController>();
            
            foreach (XRController controller in controllers)
            {
                if (controller.controllerNode == XRNode.LeftHand && leftController == null)
                {
                    leftController = controller;
                    Debug.Log("[HybridVRMovement] Found Left Controller");
                }
                else if (controller.controllerNode == XRNode.RightHand && rightController == null)
                {
                    rightController = controller;
                    Debug.Log("[HybridVRMovement] Found Right Controller");
                }
            }
        }
    }

    private void Update()
    {
        if (!isInitialized || vrOrigin == null || playerArmature == null)
            return;

        // Handle mode switching
        if (allowModeSwitch)
        {
            HandleModeSwitch();
        }

        // Update movement data
        UpdateBodyTracking();
        UpdateControllerInput();

        // Calculate movement based on current mode
        Vector2 finalMovementInput = CalculateMovementInput();

        // Apply movement to character
        ApplyMovement(finalMovementInput);

        // Sync position if needed
        SyncPosition();

        // Debug information
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    private void HandleModeSwitch()
    {
        bool modeSwitchPressed = false;
        
        if (rightController != null)
        {
            InputHelpers.IsPressed(rightController.inputDevice, modeSwitchButton, out modeSwitchPressed);
        }

        if (modeSwitchPressed)
        {
            // Cycle through movement modes
            int nextMode = ((int)currentMode + 1) % System.Enum.GetValues(typeof(MovementMode)).Length;
            currentMode = (MovementMode)nextMode;
            
            Debug.Log($"[HybridVRMovement] Switched to mode: {currentMode}");
        }
    }

    private void UpdateBodyTracking()
    {
        if (!enableBodyTracking || vrOrigin == null)
        {
            isBodyMoving = false;
            return;
        }

        // Calculate body movement
        Vector3 currentOriginPosition = vrOrigin.position;
        Vector3 originMovementDelta = currentOriginPosition - lastOriginPosition;
        Vector3 horizontalMovement = new Vector3(originMovementDelta.x, 0, originMovementDelta.z);

        bodyMovementVelocity = horizontalMovement / Time.deltaTime;
        float movementMagnitude = bodyMovementVelocity.magnitude;

        // Check if body is moving
        if (movementMagnitude > bodyTrackingThreshold)
        {
            isBodyMoving = true;
            lastBodyMovementTime = Time.time;
        }
        else if (Time.time - lastBodyMovementTime > bodyMovementDecayTime)
        {
            isBodyMoving = false;
        }

        lastOriginPosition = currentOriginPosition;
    }

    private void UpdateControllerInput()
    {
        if (!enableControllerMovement || leftController == null)
        {
            controllerInput = Vector2.zero;
            isControllerMoving = false;
            return;
        }

        // Get controller input
        if (leftController.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rawInput))
        {
            // Apply deadzone
            if (rawInput.magnitude < deadzone)
            {
                controllerInput = Vector2.zero;
            }
            else
            {
                // Normalize for consistent speed
                controllerInput = rawInput.normalized * ((rawInput.magnitude - deadzone) / (1 - deadzone));
                controllerInput *= moveScale;
                
                if (invertY)
                {
                    controllerInput.y = -controllerInput.y;
                }
            }

            isControllerMoving = controllerInput.magnitude > 0.1f;
        }
    }

    private Vector2 CalculateMovementInput()
    {
        Vector2 bodyInput = Vector2.zero;
        Vector2 controllerMovementInput = controllerInput;

        // Convert body movement to input
        if (isBodyMoving && enableBodyTracking)
        {
            Vector3 localBodyMovement = playerArmature.InverseTransformDirection(bodyMovementVelocity);
            bodyInput = new Vector2(localBodyMovement.x, localBodyMovement.z) * bodyTrackingWeight;
            
            // Clamp to reasonable values
            bodyInput = Vector2.ClampMagnitude(bodyInput, 1.0f);
        }

        // Calculate blend weights based on mode
        CalculateBlendWeights();

        Vector2 finalInput = Vector2.zero;

        switch (currentMode)
        {
            case MovementMode.BodyTrackingOnly:
                finalInput = bodyInput;
                break;

            case MovementMode.ControllerOnly:
                finalInput = controllerMovementInput;
                break;

            case MovementMode.Hybrid:
                // Blend between body and controller input
                finalInput = bodyInput * currentBodyWeight + controllerMovementInput * currentControllerWeight;
                break;

            case MovementMode.AdditiveHybrid:
                // Add controller input on top of body tracking
                finalInput = bodyInput + (controllerMovementInput * controllerMovementWeight);
                finalInput = Vector2.ClampMagnitude(finalInput, 1.0f);
                break;
        }

        return finalInput;
    }

    private void CalculateBlendWeights()
    {
        float targetBodyWeight = 1.0f;
        float targetControllerWeight = 0.0f;

        switch (currentMode)
        {
            case MovementMode.BodyTrackingOnly:
                targetBodyWeight = 1.0f;
                targetControllerWeight = 0.0f;
                break;

            case MovementMode.ControllerOnly:
                targetBodyWeight = 0.0f;
                targetControllerWeight = 1.0f;
                break;

            case MovementMode.Hybrid:
                // Controller input overrides body tracking when above threshold
                if (isControllerMoving && controllerInput.magnitude > controllerOverrideThreshold)
                {
                    targetBodyWeight = 0.0f;
                    targetControllerWeight = 1.0f;
                }
                else if (isBodyMoving)
                {
                    targetBodyWeight = 1.0f;
                    targetControllerWeight = 0.0f;
                }
                else
                {
                    // No input - maintain current weights but decay to neutral
                    targetBodyWeight = Mathf.Lerp(currentBodyWeight, 0.5f, Time.deltaTime);
                    targetControllerWeight = Mathf.Lerp(currentControllerWeight, 0.5f, Time.deltaTime);
                }
                break;

            case MovementMode.AdditiveHybrid:
                // Both systems active
                targetBodyWeight = 1.0f;
                targetControllerWeight = 1.0f;
                break;
        }

        // Smooth blend weights
        currentBodyWeight = Mathf.Lerp(currentBodyWeight, targetBodyWeight, Time.deltaTime * blendSpeed);
        currentControllerWeight = Mathf.Lerp(currentControllerWeight, targetControllerWeight, Time.deltaTime * blendSpeed);
    }

    private void ApplyMovement(Vector2 movementInput)
    {
        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.move = movementInput;

            // Handle other inputs from controllers
            if (rightController != null)
            {
                InputHelpers.IsPressed(rightController.inputDevice, InputHelpers.Button.SecondaryButton, out bool jumpPressed);
                starterAssetsInputs.jump = jumpPressed;
            }

            if (leftController != null)
            {
                InputHelpers.IsPressed(leftController.inputDevice, InputHelpers.Button.PrimaryButton, out bool sprintPressed);
                starterAssetsInputs.sprint = sprintPressed;
            }
        }
    }

    private void SyncPosition()
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
    }

    private void DrawDebugInfo()
    {
        string debugText = $"Mode: {currentMode}\n";
        debugText += $"Body Moving: {isBodyMoving} (Vel: {bodyMovementVelocity.magnitude:F2})\n";
        debugText += $"Controller Moving: {isControllerMoving} (Input: {controllerInput.magnitude:F2})\n";
        debugText += $"Weights - Body: {currentBodyWeight:F2}, Controller: {currentControllerWeight:F2}";

        Debug.Log($"[HybridVRMovement] {debugText}");
    }

    // Public methods for external control
    public void SetMovementMode(MovementMode mode)
    {
        currentMode = mode;
    }

    public void EnableBodyTracking(bool enable)
    {
        enableBodyTracking = enable;
    }

    public void EnableControllerMovement(bool enable)
    {
        enableControllerMovement = enable;
    }

    public MovementMode GetCurrentMode()
    {
        return currentMode;
    }

    public bool IsBodyMoving()
    {
        return isBodyMoving;
    }

    public bool IsControllerMoving()
    {
        return isControllerMoving;
    }
} 