using UnityEngine;

namespace VRMovement.Interfaces
{
    /// <summary>
    /// Interface for VR body tracking functionality
    /// </summary>
    public interface IVRBodyTracker
    {
        bool IsBodyMoving { get; }
        Vector3 BodyMovementVelocity { get; }
        Vector2 GetBodyMovementInput();
        void UpdateBodyTracking();
        void SetBodyTrackingSettings(float threshold, float weight, Vector3 offset);
    }
} 