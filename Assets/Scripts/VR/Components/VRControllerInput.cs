using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using VRMovement.Interfaces;

namespace VRMovement.Components
{
    /// <summary>
    /// Handles VR controller input functionality
    /// </summary>
    public class VRControllerInput : MonoBehaviour, IVRControllerInput
    {
        [Header("Controller References")]
        [SerializeField] private XRController leftController;
        [SerializeField] private XRController rightController;

        [Header("Controller Movement Settings")]
        [SerializeField] private float deadzone = 0.1f;
        [SerializeField] private float moveScale = 1.0f;
        [SerializeField] private bool invertY = false;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Properties
        public bool IsControllerMoving { get; private set; }
        public Vector2 ControllerInput { get; private set; }

        private void Start()
        {
            FindControllers();
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
                        Debug.Log("[VRControllerInput] Found Left Controller");
                    }
                    else if (controller.controllerNode == XRNode.RightHand && rightController == null)
                    {
                        rightController = controller;
                        Debug.Log("[VRControllerInput] Found Right Controller");
                    }
                }
            }
        }

        public void UpdateControllerInput()
        {
            if (leftController == null)
            {
                ControllerInput = Vector2.zero;
                IsControllerMoving = false;
                return;
            }

            // Get controller input
            if (leftController.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rawInput))
            {
                // Apply deadzone
                if (rawInput.magnitude < deadzone)
                {
                    ControllerInput = Vector2.zero;
                }
                else
                {
                    // Normalize for consistent speed
                    ControllerInput = rawInput.normalized * ((rawInput.magnitude - deadzone) / (1 - deadzone));
                    ControllerInput *= moveScale;
                    
                    if (invertY)
                    {
                        ControllerInput = new Vector2(ControllerInput.x, -ControllerInput.y);
                    }
                }

                IsControllerMoving = ControllerInput.magnitude > 0.1f;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[VRControllerInput] Moving: {IsControllerMoving}, Input: {ControllerInput}");
            }
        }

        public Vector2 GetControllerMovementInput()
        {
            return ControllerInput;
        }

        public void SetControllerSettings(float newDeadzone, float newMoveScale, bool newInvertY)
        {
            deadzone = newDeadzone;
            moveScale = newMoveScale;
            invertY = newInvertY;
        }

        // Additional controller button functions
        public bool GetJumpInput()
        {
            if (rightController != null)
            {
                InputHelpers.IsPressed(rightController.inputDevice, InputHelpers.Button.SecondaryButton, out bool jumpPressed);
                return jumpPressed;
            }
            return false;
        }

        public bool GetSprintInput()
        {
            if (leftController != null)
            {
                InputHelpers.IsPressed(leftController.inputDevice, InputHelpers.Button.PrimaryButton, out bool sprintPressed);
                return sprintPressed;
            }
            return false;
        }

        public bool GetModeSwitchInput()
        {
            if (rightController != null)
            {
                InputHelpers.IsPressed(rightController.inputDevice, InputHelpers.Button.MenuButton, out bool modeSwitchPressed);
                return modeSwitchPressed;
            }
            return false;
        }

        // Public setters for inspector
        public void SetLeftController(XRController controller) => leftController = controller;
        public void SetRightController(XRController controller) => rightController = controller;
    }
} 