using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using StarterAssets;
using System.Collections.Generic;

/// <summary>
/// Converts XR Controller input to Third Person Controller movement
/// </summary>
public class VRInputToThirdPerson : MonoBehaviour
{
    [Header("Controller References")]
    [SerializeField] private XRController leftController;
    [SerializeField] private XRController rightController;
    [SerializeField] private InputHelpers.Button moveButton = InputHelpers.Button.Primary2DAxisClick;
    [SerializeField] private InputHelpers.Button sprintButton = InputHelpers.Button.PrimaryButton;
    [SerializeField] private InputHelpers.Button jumpButton = InputHelpers.Button.SecondaryButton;

    [Header("Movement Settings")]
    [SerializeField] private float deadzone = 0.1f;
    [SerializeField] private float moveScale = 1.0f;
    [SerializeField] private bool invertY = false;

    // Reference to the input component
    private StarterAssetsInputs starterAssetsInputs;

    private void Awake()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        
        // Find controllers if not assigned
        if (leftController == null || rightController == null)
        {
            FindControllers();
        }
    }

    private void FindControllers()
    {
        // Try to find controllers in the scene
        XRController[] controllers = FindObjectsOfType<XRController>();
        
        foreach (XRController controller in controllers)
        {
            if (controller.controllerNode == XRNode.LeftHand)
            {
                leftController = controller;
                Debug.Log("[VRInputToThirdPerson] Found Left Controller");
            }
            else if (controller.controllerNode == XRNode.RightHand)
            {
                rightController = controller;
                Debug.Log("[VRInputToThirdPerson] Found Right Controller");
            }
        }
        
        if (leftController == null)
            Debug.LogWarning("[VRInputToThirdPerson] Left Controller not found! Movement will not work.");
            
        if (rightController == null)
            Debug.LogWarning("[VRInputToThirdPerson] Right Controller not found! Actions will not work.");
    }

    private void Update()
    {
        if (starterAssetsInputs == null)
            return;
            
        // Process movement from left controller
        if (leftController != null)
        {
            ProcessMovement();
        }
        
        // Process actions from right controller
        if (rightController != null)
        {
            ProcessActions();
        }
    }

    private void ProcessMovement()
    {
        // Get primary 2D axis (joystick) value
        if (leftController.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 position))
        {
            // Apply deadzone
            if (position.magnitude < deadzone)
            {
                position = Vector2.zero;
            }
            else
            {
                // Normalize for consistent speed in all directions
                position = position.normalized * ((position.magnitude - deadzone) / (1 - deadzone));
            }
            
            // Apply movement scale
            position *= moveScale;
            
            // Apply Y inversion if needed
            if (invertY)
            {
                position.y = -position.y;
            }
            
            // Set input for ThirdPersonController
            starterAssetsInputs.move = position;
        }
        else
        {
            starterAssetsInputs.move = Vector2.zero;
        }
        
        // Check sprint button
        InputHelpers.IsPressed(leftController.inputDevice, sprintButton, out bool sprintPressed);
        starterAssetsInputs.sprint = sprintPressed;
    }

    private void ProcessActions()
    {
        // Check jump button
        InputHelpers.IsPressed(rightController.inputDevice, jumpButton, out bool jumpPressed);
        starterAssetsInputs.jump = jumpPressed;
    }

    // For enabling VR input and disabling VROriginFollower
    public void EnableVRInput()
    {
        // Disable VROriginFollower if present
        VROriginFollower follower = GetComponent<VROriginFollower>();
        if (follower != null)
        {
            follower.enabled = false;
            Debug.Log("[VRInputToThirdPerson] Disabled VROriginFollower");
        }
        
        // Enable this component
        this.enabled = true;
    }
}