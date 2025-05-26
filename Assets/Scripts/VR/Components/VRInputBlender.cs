using UnityEngine;
using VRMovement.Interfaces;

namespace VRMovement.Components
{
    /// <summary>
    /// Handles blending between body tracking and controller input
    /// </summary>
    public class VRInputBlender : MonoBehaviour, IVRInputBlender
    {
        [Header("Hybrid Settings")]
        [SerializeField] private float blendSpeed = 5.0f;
        [SerializeField] private float controllerOverrideThreshold = 0.3f;
        [SerializeField] private float bodyMovementDecayTime = 2.0f;
        [SerializeField] private float controllerMovementWeight = 1.0f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Properties
        public float CurrentBodyWeight { get; private set; } = 1.0f;
        public float CurrentControllerWeight { get; private set; } = 0.0f;

        // Private variables
        private float lastBodyMovementTime;

        public Vector2 BlendInputs(Vector2 bodyInput, Vector2 controllerInput, MovementMode mode)
        {
            Vector2 finalInput = Vector2.zero;

            switch (mode)
            {
                case MovementMode.BodyTrackingOnly:
                    finalInput = bodyInput;
                    break;

                case MovementMode.ControllerOnly:
                    finalInput = controllerInput;
                    break;

                case MovementMode.Hybrid:
                    // Blend between body and controller input
                    finalInput = bodyInput * CurrentBodyWeight + controllerInput * CurrentControllerWeight;
                    break;

                case MovementMode.AdditiveHybrid:
                    // Add controller input on top of body tracking
                    finalInput = bodyInput + (controllerInput * controllerMovementWeight);
                    finalInput = Vector2.ClampMagnitude(finalInput, 1.0f);
                    break;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[VRInputBlender] Mode: {mode}, Body: {bodyInput}, Controller: {controllerInput}, Final: {finalInput}");
                Debug.Log($"[VRInputBlender] Weights - Body: {CurrentBodyWeight:F2}, Controller: {CurrentControllerWeight:F2}");
            }

            return finalInput;
        }

        public void UpdateBlendWeights(bool isBodyMoving, bool isControllerMoving, float controllerMagnitude, MovementMode mode)
        {
            if (isBodyMoving)
            {
                lastBodyMovementTime = Time.time;
            }

            float targetBodyWeight = 1.0f;
            float targetControllerWeight = 0.0f;

            switch (mode)
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
                    if (isControllerMoving && controllerMagnitude > controllerOverrideThreshold)
                    {
                        targetBodyWeight = 0.0f;
                        targetControllerWeight = 1.0f;
                    }
                    else if (isBodyMoving || (Time.time - lastBodyMovementTime) < bodyMovementDecayTime)
                    {
                        targetBodyWeight = 1.0f;
                        targetControllerWeight = 0.0f;
                    }
                    else
                    {
                        // No input - maintain current weights but decay to neutral
                        targetBodyWeight = Mathf.Lerp(CurrentBodyWeight, 0.5f, Time.deltaTime);
                        targetControllerWeight = Mathf.Lerp(CurrentControllerWeight, 0.5f, Time.deltaTime);
                    }
                    break;

                case MovementMode.AdditiveHybrid:
                    // Both systems active
                    targetBodyWeight = 1.0f;
                    targetControllerWeight = 1.0f;
                    break;
            }

            // Smooth blend weights
            CurrentBodyWeight = Mathf.Lerp(CurrentBodyWeight, targetBodyWeight, Time.deltaTime * blendSpeed);
            CurrentControllerWeight = Mathf.Lerp(CurrentControllerWeight, targetControllerWeight, Time.deltaTime * blendSpeed);
        }

        public void SetBlendSettings(float newBlendSpeed, float newControllerOverrideThreshold, float newBodyMovementDecayTime)
        {
            blendSpeed = newBlendSpeed;
            controllerOverrideThreshold = newControllerOverrideThreshold;
            bodyMovementDecayTime = newBodyMovementDecayTime;
        }

        // Public setters for inspector
        public void SetControllerMovementWeight(float weight) => controllerMovementWeight = weight;
    }
} 