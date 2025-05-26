using UnityEngine;

namespace VRMovement.Interfaces
{
    /// <summary>
    /// Interface for VR input blending functionality
    /// </summary>
    public interface IVRInputBlender
    {
        float CurrentBodyWeight { get; }
        float CurrentControllerWeight { get; }
        Vector2 BlendInputs(Vector2 bodyInput, Vector2 controllerInput, MovementMode mode);
        void UpdateBlendWeights(bool isBodyMoving, bool isControllerMoving, float controllerMagnitude, MovementMode mode);
        void SetBlendSettings(float blendSpeed, float controllerOverrideThreshold, float bodyMovementDecayTime);
    }

    public enum MovementMode
    {
        BodyTrackingOnly,    // Only use body tracking for movement
        ControllerOnly,      // Only use controller input for movement
        Hybrid,              // Intelligently blend both
        AdditiveHybrid       // Add controller movement on top of body tracking
    }
} 