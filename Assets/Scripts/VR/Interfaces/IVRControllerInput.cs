using UnityEngine;

namespace VRMovement.Interfaces
{
    /// <summary>
    /// Interface for VR controller input functionality
    /// </summary>
    public interface IVRControllerInput
    {
        bool IsControllerMoving { get; }
        Vector2 ControllerInput { get; }
        Vector2 GetControllerMovementInput();
        void UpdateControllerInput();
        void SetControllerSettings(float deadzone, float moveScale, bool invertY);
    }
} 