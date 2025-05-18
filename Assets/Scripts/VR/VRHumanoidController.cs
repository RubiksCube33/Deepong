using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Controls a humanoid model in VR by mapping XR controller positions to the model's limbs
/// </summary>
public class VRHumanoidController : MonoBehaviour
{
    [Header("XR References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform leftHandController;
    [SerializeField] private Transform rightHandController;
    [SerializeField] private Transform headset;

    [Header("Humanoid References")]
    [SerializeField] private Transform humanoidRoot;
    [SerializeField] private Transform humanoidHead;
    [SerializeField] private Transform humanoidLeftHand;
    [SerializeField] private Transform humanoidRightHand;
    
    [Header("Offset Settings")]
    [SerializeField] private Vector3 rootPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 headPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 leftHandPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 rightHandPositionOffset = Vector3.zero;
    
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rootRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 headRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 leftHandRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 rightHandRotationOffset = Vector3.zero;
    
    [Header("Scaling")]
    [SerializeField] private float modelScale = 1.0f;
    
    [Header("IK Settings (Optional)")]
    [SerializeField] private bool useIK = true;
    [SerializeField, Range(0f, 1f)] private float ikWeight = 1.0f;

    private Animator humanoidAnimator;
    private Vector3 initialRootPosition;
    private Quaternion initialRootRotation;

    // Public properties for easier access
    public Transform XROrigin { get => xrOrigin; set => xrOrigin = value; }
    public Transform LeftHandController { get => leftHandController; set => leftHandController = value; }
    public Transform RightHandController { get => rightHandController; set => rightHandController = value; }
    public Transform Headset { get => headset; set => headset = value; }
    
    public Transform HumanoidRoot { get => humanoidRoot; set => humanoidRoot = value; }
    public Transform HumanoidHead { get => humanoidHead; set => humanoidHead = value; }
    public Transform HumanoidLeftHand { get => humanoidLeftHand; set => humanoidLeftHand = value; }
    public Transform HumanoidRightHand { get => humanoidRightHand; set => humanoidRightHand = value; }
    
    public Vector3 RootPositionOffset { get => rootPositionOffset; set => rootPositionOffset = value; }
    public Vector3 HeadPositionOffset { get => headPositionOffset; set => headPositionOffset = value; }
    public Vector3 LeftHandPositionOffset { get => leftHandPositionOffset; set => leftHandPositionOffset = value; }
    public Vector3 RightHandPositionOffset { get => rightHandPositionOffset; set => rightHandPositionOffset = value; }
    
    public Vector3 RootRotationOffset { get => rootRotationOffset; set => rootRotationOffset = value; }
    public Vector3 HeadRotationOffset { get => headRotationOffset; set => headRotationOffset = value; }
    public Vector3 LeftHandRotationOffset { get => leftHandRotationOffset; set => leftHandRotationOffset = value; }
    public Vector3 RightHandRotationOffset { get => rightHandRotationOffset; set => rightHandRotationOffset = value; }
    
    public float ModelScale { get => modelScale; set => modelScale = value; }
    public bool UseIK { get => useIK; set => useIK = value; }
    public float IKWeight { get => ikWeight; set => ikWeight = value; }

    void Start()
    {
        if (humanoidRoot != null)
        {
            humanoidAnimator = humanoidRoot.GetComponent<Animator>();
            initialRootPosition = humanoidRoot.position;
            initialRootRotation = humanoidRoot.rotation;
        }
        else
        {
            Debug.LogError("Humanoid root is not assigned. Please assign a humanoid model root transform.");
        }
    }

    void LateUpdate()
    {
        if (humanoidRoot == null || xrOrigin == null)
            return;

        // Adjust the root position and rotation
        UpdateRootTransform();
        
        // Update the head position and rotation
        UpdateHeadTransform();
        
        // Update the hands positions and rotations
        UpdateHandTransforms();
        
        // Apply additional IK if needed
        if (useIK && humanoidAnimator != null)
        {
            ApplyIK();
        }
    }
    
    private void UpdateRootTransform()
    {
        // Position the humanoid root based on the XR origin
        Vector3 targetPosition = xrOrigin.position + xrOrigin.TransformDirection(rootPositionOffset);
        targetPosition.y = initialRootPosition.y; // Keep the same height
        
        // Apply rotation from the headset's forward direction (but only around Y axis)
        Vector3 headsetForward = headset.forward;
        headsetForward.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(headsetForward, Vector3.up);
        
        // Apply rotation offset
        targetRotation *= Quaternion.Euler(rootRotationOffset);
        
        // Apply position and rotation
        humanoidRoot.position = targetPosition;
        humanoidRoot.rotation = targetRotation;
        
        // Apply scaling
        humanoidRoot.localScale = Vector3.one * modelScale;
    }
    
    private void UpdateHeadTransform()
    {
        if (humanoidHead == null || headset == null)
            return;
            
        // Apply the headset position and rotation to the humanoid head
        Vector3 headOffset = headset.TransformDirection(headPositionOffset);
        humanoidHead.position = headset.position + headOffset;
        
        // Apply rotation with offset
        humanoidHead.rotation = headset.rotation * Quaternion.Euler(headRotationOffset);
    }
    
    private void UpdateHandTransforms()
    {
        // Update left hand
        if (humanoidLeftHand != null && leftHandController != null)
        {
            Vector3 leftOffset = leftHandController.TransformDirection(leftHandPositionOffset);
            humanoidLeftHand.position = leftHandController.position + leftOffset;
            humanoidLeftHand.rotation = leftHandController.rotation * Quaternion.Euler(leftHandRotationOffset);
        }
        
        // Update right hand
        if (humanoidRightHand != null && rightHandController != null)
        {
            Vector3 rightOffset = rightHandController.TransformDirection(rightHandPositionOffset);
            humanoidRightHand.position = rightHandController.position + rightOffset;
            humanoidRightHand.rotation = rightHandController.rotation * Quaternion.Euler(rightHandRotationOffset);
        }
    }
    
    private void ApplyIK()
    {
        // This method would use Unity's Animator IK capabilities to smoothly position limbs
        // Requires a properly rigged humanoid model with an Animator component
        
        // Example implementation (would need to be expanded for a complete solution):
        if (humanoidAnimator != null)
        {
            // Set the IK position and rotation of the hands
            if (leftHandController != null)
            {
                humanoidAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
                humanoidAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);
                humanoidAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandController.position + leftHandController.TransformDirection(leftHandPositionOffset));
                humanoidAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandController.rotation * Quaternion.Euler(leftHandRotationOffset));
            }
            
            if (rightHandController != null)
            {
                humanoidAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
                humanoidAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
                humanoidAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandController.position + rightHandController.TransformDirection(rightHandPositionOffset));
                humanoidAnimator.SetIKRotation(AvatarIKGoal.RightHand, rightHandController.rotation * Quaternion.Euler(rightHandRotationOffset));
            }
        }
    }
} 