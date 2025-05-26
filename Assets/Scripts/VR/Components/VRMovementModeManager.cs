using UnityEngine;
using VRMovement.Interfaces;

namespace VRMovement.Components
{
    /// <summary>
    /// Manages VR movement modes and mode switching
    /// </summary>
    public class VRMovementModeManager : MonoBehaviour
    {
        [Header("Movement Mode")]
        [SerializeField] private MovementMode currentMode = MovementMode.Hybrid;
        [SerializeField] private bool allowModeSwitch = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Events for mode changes
        public System.Action<MovementMode> OnModeChanged;

        // Properties
        public MovementMode CurrentMode => currentMode;
        public bool AllowModeSwitch => allowModeSwitch;

        private bool lastModeSwitchPressed = false;

        public void HandleModeSwitch(bool modeSwitchPressed)
        {
            if (!allowModeSwitch)
                return;

            // Only trigger on button press (not hold)
            if (modeSwitchPressed && !lastModeSwitchPressed)
            {
                // Cycle through movement modes
                int nextMode = ((int)currentMode + 1) % System.Enum.GetValues(typeof(MovementMode)).Length;
                MovementMode newMode = (MovementMode)nextMode;
                
                SetMovementMode(newMode);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[VRMovementModeManager] Switched to mode: {currentMode}");
                }
            }

            lastModeSwitchPressed = modeSwitchPressed;
        }

        public void SetMovementMode(MovementMode mode)
        {
            if (currentMode != mode)
            {
                MovementMode previousMode = currentMode;
                currentMode = mode;
                
                OnModeChanged?.Invoke(currentMode);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[VRMovementModeManager] Mode changed from {previousMode} to {currentMode}");
                }
            }
        }

        public void SetAllowModeSwitch(bool allow)
        {
            allowModeSwitch = allow;
        }

        // Convenience methods for specific modes
        public bool IsBodyTrackingOnlyMode() => currentMode == MovementMode.BodyTrackingOnly;
        public bool IsControllerOnlyMode() => currentMode == MovementMode.ControllerOnly;
        public bool IsHybridMode() => currentMode == MovementMode.Hybrid;
        public bool IsAdditiveHybridMode() => currentMode == MovementMode.AdditiveHybrid;
        public bool UsesBodyTracking() => currentMode != MovementMode.ControllerOnly;
        public bool UsesControllerInput() => currentMode != MovementMode.BodyTrackingOnly;
    }
} 