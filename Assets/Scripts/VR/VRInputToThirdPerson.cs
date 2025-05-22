using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using StarterAssets;

/// <summary>
/// Connects VR controller input to the ThirdPersonController
/// </summary>
public class VRInputToThirdPerson : MonoBehaviour
{
    [Header("Controller References")]
    [SerializeField] private GameObject leftHandController;
    [SerializeField] private GameObject rightHandController;
    
    [Header("Movement Settings")]
    [SerializeField] private float deadZone = 0.1f;
    [SerializeField] private bool useLeftHandForMovement = true;
    [SerializeField] private bool useRightHandForRotation = true;
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private bool snapTurn = true;
    [SerializeField] private float snapTurnAmount = 45f;
    [SerializeField] private float snapTurnDelay = 0.5f;
    
    // References to the starter assets components
    private StarterAssetsInputs starterAssetsInputs;
    private ThirdPersonController thirdPersonController;
    
    // Variables for snap turning
    private float snapTurnTimer = 0f;
    private bool canSnapTurn = true;
    
    // Input device references
    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;
    
    // Cache for XR nodes
    private List<InputDevice> leftHandDevices = new List<InputDevice>();
    private List<InputDevice> rightHandDevices = new List<InputDevice>();
    
    // Debug flag
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    // Force controller movement without PlayerInput
    [Header("Force Direct Control")]
    [SerializeField] private bool forceDirectControl = true;
    
    // Alternative input method
    private bool usingAlternativeInput = false;
    
    private void Awake()
    {
        // Get references to the input and controller components
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        
        if (starterAssetsInputs == null)
        {
            Debug.LogError("[VRInputToThirdPerson] No StarterAssetsInputs found on this object!");
        }
        
        if (thirdPersonController == null)
        {
            Debug.LogError("[VRInputToThirdPerson] No ThirdPersonController found on this object!");
        }
    }
    
    private void Start()
    {
        // Auto-find controllers if not assigned
        if (leftHandController == null || rightHandController == null)
        {
            FindControllers();
        }
        
        // Disable any PlayerInput component to avoid conflicts
        UnityEngine.InputSystem.PlayerInput playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            if (debugMode) Debug.Log("[VRInputToThirdPerson] Disabling PlayerInput component to avoid conflicts");
            playerInput.enabled = false;
        }
        
        // Get the input devices
        StartCoroutine(InitializeInputDevices());
        
        if (debugMode) Debug.Log("[VRInputToThirdPerson] Initialized on " + gameObject.name);
        
        // Make sure CharacterController is enabled
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
            Debug.Log("[VRInputToThirdPerson] Enabled CharacterController component");
        }
        
        // Register for device connected/disconnected events
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
    }
    
    private void OnDestroy()
    {
        // Unregister events
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }
    
    private void OnDeviceConnected(InputDevice device)
    {
        if (debugMode) Debug.Log($"[VRInputToThirdPerson] Device connected: {device.name}, characteristics: {device.characteristics}");
        RefreshDevices();
    }
    
    private void OnDeviceDisconnected(InputDevice device)
    {
        if (debugMode) Debug.Log($"[VRInputToThirdPerson] Device disconnected: {device.name}");
        RefreshDevices();
    }
    
    private void RefreshDevices()
    {
        // Clear and refresh the device lists
        leftHandDevices.Clear();
        rightHandDevices.Clear();
        
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftHandDevices);
        if (leftHandDevices.Count > 0)
        {
            leftHandDevice = leftHandDevices[0];
            if (debugMode) Debug.Log($"[VRInputToThirdPerson] Refreshed left hand device: {leftHandDevice.name}");
        }
        
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);
        if (rightHandDevices.Count > 0)
        {
            rightHandDevice = rightHandDevices[0];
            if (debugMode) Debug.Log($"[VRInputToThirdPerson] Refreshed right hand device: {rightHandDevice.name}");
        }
    }
    
    private IEnumerator InitializeInputDevices()
    {
        // Wait for XR subsystem to initialize
        yield return new WaitForSeconds(1.0f);
        
        bool devicesFound = false;
        int attempts = 0;
        
        // First, try to get all available devices and log them
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        Debug.Log($"[VRInputToThirdPerson] Found {allDevices.Count} input devices:");
        foreach (var device in allDevices)
        {
            Debug.Log($"[VRInputToThirdPerson] Device: {device.name}, Characteristics: {device.characteristics}, IsValid: {device.isValid}");
        }
        
        while (!devicesFound && attempts < 10)
        {
            attempts++;
            if (debugMode) Debug.Log("[VRInputToThirdPerson] Attempting to find input devices, attempt " + attempts);
            
            // Try the more specific controller characteristics first
            leftHandDevices.Clear();
            rightHandDevices.Clear();
            
            // Try with HeldInHand characteristic
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, 
                leftHandDevices);
                
            if (leftHandDevices.Count == 0)
            {
                // Fall back to just Left Controller
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, 
                    leftHandDevices);
            }
            
            if (leftHandDevices.Count > 0)
            {
                leftHandDevice = leftHandDevices[0];
                Debug.Log($"[VRInputToThirdPerson] Found left hand device: {leftHandDevice.name}, valid: {leftHandDevice.isValid}");
            }
            
            // Try with HeldInHand characteristic
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, 
                rightHandDevices);
                
            if (rightHandDevices.Count == 0)
            {
                // Fall back to just Right Controller
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, 
                    rightHandDevices);
            }
            
            if (rightHandDevices.Count > 0)
            {
                rightHandDevice = rightHandDevices[0];
                Debug.Log($"[VRInputToThirdPerson] Found right hand device: {rightHandDevice.name}, valid: {rightHandDevice.isValid}");
            }
            
            // If we have at least one valid device, proceed (we'll fall back to alternative input if needed)
            devicesFound = (leftHandDevices.Count > 0 || rightHandDevices.Count > 0);
            
            if (!devicesFound)
            {
                if (debugMode) Debug.Log("[VRInputToThirdPerson] Devices not found, waiting and trying again...");
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        if (!devicesFound)
        {
            Debug.LogWarning("[VRInputToThirdPerson] Could not find valid input devices after multiple attempts. Using alternative input method.");
            usingAlternativeInput = true;
        }
    }
    
    private void FindControllers()
    {
        // Find controllers in Camera Offset
        Transform cameraOffset = GameObject.FindObjectOfType<Camera>()?.transform.parent;
        
        if (cameraOffset != null)
        {
            if (debugMode) Debug.Log($"[VRInputToThirdPerson] Found Camera Offset: {cameraOffset.name} with {cameraOffset.childCount} children");
            
            foreach (Transform child in cameraOffset)
            {
                if (debugMode) Debug.Log($"[VRInputToThirdPerson] Checking child: {child.name}");
                
                if (child.name.Contains("Left Controller"))
                {
                    leftHandController = child.gameObject;
                    Debug.Log("[VRInputToThirdPerson] Found Left Controller in the scene hierarchy: " + child.name);
                    
                    // Check for any tracked pose drivers or action-based controllers
                    var poseDriver = child.GetComponent<TrackedPoseDriver>();
                    if (poseDriver != null)
                    {
                        if (debugMode) Debug.Log("[VRInputToThirdPerson] Left controller has TrackedPoseDriver");
                    }
                }
                else if (child.name.Contains("Right Controller"))
                {
                    rightHandController = child.gameObject;
                    Debug.Log("[VRInputToThirdPerson] Found Right Controller in the scene hierarchy: " + child.name);
                    
                    // Check for any tracked pose drivers or action-based controllers
                    var poseDriver = child.GetComponent<TrackedPoseDriver>();
                    if (poseDriver != null)
                    {
                        if (debugMode) Debug.Log("[VRInputToThirdPerson] Right controller has TrackedPoseDriver");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[VRInputToThirdPerson] Could not find Camera Offset parent");
        }
        
        if (leftHandController == null)
        {
            Debug.LogWarning("[VRInputToThirdPerson] Left hand controller GameObject not found. VR movement may not work correctly.");
        }
        
        if (rightHandController == null)
        {
            Debug.LogWarning("[VRInputToThirdPerson] Right hand controller GameObject not found. VR rotation may not work correctly.");
        }
    }
    
    private void Update()
    {
        if (starterAssetsInputs == null)
        {
            return;
        }
        
        // Check if we need to refresh the devices
        if (Time.frameCount % 300 == 0)
        {
            RefreshDevices();
        }
        
        // Get controller input and apply to movement
        UpdateControllerInput();
    }
    
    private void UpdateControllerInput()
    {
        bool leftHandValid = leftHandDevice.isValid;
        bool rightHandValid = rightHandDevice.isValid;
        
        if (debugMode && Time.frameCount % 100 == 0)
        {
            Debug.Log($"[VRInputToThirdPerson] Devices valid - Left: {leftHandValid}, Right: {rightHandValid}");
        }
        
        // If devices aren't valid, try alternative input or return
        if (!leftHandValid && !rightHandValid)
        {
            if (usingAlternativeInput)
            {
                UpdateAlternativeInput();
            }
            return;
        }
        
        // Apply movement from the appropriate controller
        InputDevice movementDevice = useLeftHandForMovement ? leftHandDevice : rightHandDevice;
        bool movementDeviceValid = useLeftHandForMovement ? leftHandValid : rightHandValid;
        
        if (movementDeviceValid)
        {
            // Get the 2D axis values from the joystick
            if (movementDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 movementValue))
            {
                // Apply deadzone
                if (movementValue.magnitude < deadZone)
                {
                    movementValue = Vector2.zero;
                }
                
                // Set the input values for the StarterAssetsInputs
                starterAssetsInputs.move = movementValue;
                
                if (debugMode && movementValue.magnitude > 0)
                {
                    Debug.Log($"[VRInputToThirdPerson] Movement value: {movementValue}");
                }
                
                // Force direct movement if needed
                if (forceDirectControl && thirdPersonController != null && movementValue.magnitude > 0)
                {
                    ApplyDirectMovement(movementValue);
                }
            }
            
            // Check for sprint button
            if (movementDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool sprintPressed))
            {
                starterAssetsInputs.sprint = sprintPressed;
                
                if (debugMode && sprintPressed)
                {
                    Debug.Log("[VRInputToThirdPerson] Sprint pressed");
                }
            }
            
            // Check for jump button
            if (movementDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool jumpPressed))
            {
                starterAssetsInputs.jump = jumpPressed;
                
                if (debugMode && jumpPressed)
                {
                    Debug.Log("[VRInputToThirdPerson] Jump pressed");
                }
                
                // Force direct jump if needed
                if (forceDirectControl && thirdPersonController != null && jumpPressed && thirdPersonController.Grounded)
                {
                    ApplyDirectJump();
                }
            }
        }
        
        // Apply rotation from the appropriate controller
        InputDevice rotationDevice = useRightHandForRotation ? rightHandDevice : leftHandDevice;
        bool rotationDeviceValid = useRightHandForRotation ? rightHandValid : leftHandValid;
        
        if (rotationDeviceValid && useRightHandForRotation)
        {
            // Get the 2D axis values from the joystick
            if (rotationDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rotationValue))
            {
                ApplyRotation(rotationValue.x);
            }
        }
    }
    
    private void UpdateAlternativeInput()
    {
        // This is a fallback method that uses the controller GameObject positions/movements
        // if XR Input System isn't working correctly
        if (leftHandController != null && useLeftHandForMovement)
        {
            // Get any potential TrackedPoseDriver or similar component
            var poseDriver = leftHandController.GetComponent<TrackedPoseDriver>();
            if (poseDriver != null && poseDriver.enabled)
            {
                // Try to simulate joystick input from controller movement
                Vector2 simulatedMovement = Vector2.zero;
                
                // Get the forward direction of the controller
                Vector3 controllerForward = leftHandController.transform.forward;
                controllerForward.y = 0; // Project onto horizontal plane
                
                if (controllerForward.magnitude > deadZone)
                {
                    controllerForward.Normalize();
                    simulatedMovement.y = 0.5f; // Forward movement
                }
                
                starterAssetsInputs.move = simulatedMovement;
                
                if (forceDirectControl && thirdPersonController != null && simulatedMovement.magnitude > 0)
                {
                    ApplyDirectMovement(simulatedMovement);
                }
            }
        }
        
        if (rightHandController != null && useRightHandForRotation)
        {
            // Try to simulate rotation from controller rotation
            Vector3 controllerRight = rightHandController.transform.right;
            float xRotation = Vector3.Dot(controllerRight, Camera.main.transform.right);
            
            if (Mathf.Abs(xRotation) > 0.7f)
            {
                ApplyRotation(xRotation);
            }
        }
    }
    
    private void ApplyDirectMovement(Vector2 movementValue)
    {
        // This is a fallback method in case normal input system doesn't work
        Vector3 inputDirection = new Vector3(movementValue.x, 0.0f, movementValue.y).normalized;
        float targetSpeed = starterAssetsInputs.sprint ? thirdPersonController.SprintSpeed : thirdPersonController.MoveSpeed;
        
        // Rotate to camera direction
        Transform cameraTransform = Camera.main.transform;
        Vector3 cameraForward = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, thirdPersonController.RotationSmoothTime);
        
        // Move in input direction relative to camera
        Vector3 moveDir = targetRotation * inputDirection;
        moveDir *= targetSpeed * Time.deltaTime;
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.Move(moveDir);
            if (debugMode && Time.frameCount % 20 == 0)
            {
                Debug.Log($"[VRInputToThirdPerson] Direct move: {moveDir}, speed: {targetSpeed}");
            }
        }
    }
    
    private void ApplyDirectJump()
    {
        // Apply direct jump force
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            float jumpForce = Mathf.Sqrt(thirdPersonController.JumpHeight * -2f * thirdPersonController.Gravity);
            controller.Move(new Vector3(0, jumpForce * Time.deltaTime, 0));
            if (debugMode)
            {
                Debug.Log($"[VRInputToThirdPerson] Direct jump force: {jumpForce}");
            }
        }
    }
    
    private void ApplyRotation(float xValue)
    {
        // Apply deadzone
        if (Mathf.Abs(xValue) < deadZone)
        {
            xValue = 0f;
        }
        
        if (snapTurn)
        {
            // Handle snap turning
            if (canSnapTurn && Mathf.Abs(xValue) > 0.7f)
            {
                float rotationAmount = snapTurnAmount * Mathf.Sign(xValue);
                transform.Rotate(0, rotationAmount, 0);
                
                if (debugMode)
                {
                    Debug.Log($"[VRInputToThirdPerson] Snap turn: {rotationAmount} degrees");
                }
                
                // Start the cooldown
                canSnapTurn = false;
                snapTurnTimer = snapTurnDelay;
            }
            
            // Handle the cooldown timer
            if (!canSnapTurn)
            {
                snapTurnTimer -= Time.deltaTime;
                if (snapTurnTimer <= 0f)
                {
                    canSnapTurn = true;
                }
            }
        }
        else
        {
            // Smooth turning
            float rotationAmount = xValue * rotationSpeed * Time.deltaTime * 100f;
            transform.Rotate(0, rotationAmount, 0);
            
            if (debugMode && Mathf.Abs(rotationAmount) > 0.1f)
            {
                Debug.Log($"[VRInputToThirdPerson] Smooth rotation: {rotationAmount}");
            }
        }
    }
    
    // For debugging
    void OnGUI()
    {
        if (debugMode)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            GUILayout.Label("VR Input Status:");
            
            // Check if device lists are populated
            GUILayout.Label($"Left devices found: {leftHandDevices.Count}");
            GUILayout.Label($"Right devices found: {rightHandDevices.Count}");
            
            // Show device validity
            GUILayout.Label($"Left controller valid: {(leftHandDevice.isValid ? "Yes" : "No")}");
            GUILayout.Label($"Right controller valid: {(rightHandDevice.isValid ? "Yes" : "No")}");
            
            // Show alternative input state
            GUILayout.Label($"Using alternative input: {usingAlternativeInput}");
            GUILayout.Label($"Force direct control: {forceDirectControl}");
            
            // Show controller GameObjects
            GUILayout.Label($"Left controller GameObject: {(leftHandController != null ? leftHandController.name : "Not found")}");
            GUILayout.Label($"Right controller GameObject: {(rightHandController != null ? rightHandController.name : "Not found")}");
            
            if (starterAssetsInputs != null)
            {
                GUILayout.Label($"Move: {starterAssetsInputs.move}");
                GUILayout.Label($"Jump: {starterAssetsInputs.jump}");
                GUILayout.Label($"Sprint: {starterAssetsInputs.sprint}");
            }
            
            // Show additional info about the character controller
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                GUILayout.Label($"CharacterController enabled: {controller.enabled}");
                GUILayout.Label($"Velocity: {controller.velocity}");
                GUILayout.Label($"Is grounded: {controller.isGrounded}");
            }
            
            // Show ThirdPersonController info
            if (thirdPersonController != null)
            {
                GUILayout.Label($"Grounded state: {thirdPersonController.Grounded}");
            }
            
            GUILayout.EndArea();
        }
    }
} 