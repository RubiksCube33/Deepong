using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Sets up a PlayerArmature model to work with XR controllers
/// </summary>
public class XRPlayerArmatureSetup : MonoBehaviour
{
    [Header("XR References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform cameraOffset;
    [SerializeField] private Transform headCamera;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;
    
    [Header("Player Armature")]
    [SerializeField] private GameObject playerArmaturePrefab;
    [SerializeField] private bool instantiateOnStart = true;
    [SerializeField] private bool useExistingArmature = false;
    [SerializeField] private GameObject existingArmature;
    [SerializeField] private Vector3 positionOffset = new Vector3(0, -0.9f, 0);
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private float scaleFactor = 1.0f;
    
    [Header("Body Parts - Auto-assigned if left empty")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;
    
    [Header("Body Visibility")]
    [SerializeField] private bool hideHeadMesh = true;
    
    private GameObject instantiatedArmature;
    private VRHumanoidController humanoidController;
    private Animator armatureAnimator;
    
    void Start()
    {
        if (useExistingArmature && existingArmature != null)
        {
            SetupExistingPlayerArmature();
        }
        else if (instantiateOnStart && playerArmaturePrefab != null)
        {
            InstantiatePlayerArmature();
        }
    }
    
    /// <summary>
    /// Sets up an existing player armature in the scene
    /// </summary>
    public void SetupExistingPlayerArmature()
    {
        if (existingArmature == null)
        {
            // Try to find armature as a child if not explicitly set
            existingArmature = FindArmatureInChildren();
            
            if (existingArmature == null)
            {
                Debug.LogError("Existing armature is not found or assigned!");
                return;
            }
        }
        
        if (xrOrigin == null)
        {
            xrOrigin = transform;
        }
        
        if (cameraOffset == null)
        {
            // Try to find Camera Offset as a child of XR Origin
            cameraOffset = xrOrigin.Find("Camera Offset");
            if (cameraOffset == null)
            {
                Debug.LogError("Camera Offset not found!");
                return;
            }
        }
        
        if (headCamera == null)
        {
            // Try to find Main Camera as a child of Camera Offset
            headCamera = cameraOffset.Find("Main Camera");
            if (headCamera == null)
            {
                Debug.LogError("Main Camera not found!");
                return;
            }
        }
        
        if (leftController == null)
        {
            // Try to find Left Controller as a child of Camera Offset
            leftController = cameraOffset.Find("Left Controller");
            if (leftController == null)
            {
                Debug.LogWarning("Left Controller not found! Some VR functionality may be limited.");
            }
        }
        
        if (rightController == null)
        {
            // Try to find Right Controller as a child of Camera Offset
            rightController = cameraOffset.Find("Right Controller");
            if (rightController == null)
            {
                Debug.LogWarning("Right Controller not found! Some VR functionality may be limited.");
            }
        }
        
        instantiatedArmature = existingArmature;
        
        // Position, rotate, and scale it correctly
        instantiatedArmature.transform.localPosition = positionOffset;
        instantiatedArmature.transform.localRotation = Quaternion.Euler(rotationOffset);
        instantiatedArmature.transform.localScale = Vector3.one * scaleFactor;
        
        // Add VRHumanoidController component if not already present
        humanoidController = instantiatedArmature.GetComponent<VRHumanoidController>();
        if (humanoidController == null)
        {
            humanoidController = instantiatedArmature.AddComponent<VRHumanoidController>();
        }
        
        // Get the animator
        armatureAnimator = instantiatedArmature.GetComponent<Animator>();
        if (armatureAnimator == null)
        {
            Debug.LogWarning("Armature has no Animator component! Adding one...");
            armatureAnimator = instantiatedArmature.AddComponent<Animator>();
        }
        
        // Find the key transforms if not manually assigned
        FindKeyTransforms();
        
        // Set up the VRHumanoidController
        SetupHumanoidController();
        
        // Hide head mesh if requested (to avoid seeing it from the inside when in VR)
        if (hideHeadMesh && headTransform != null)
        {
            ToggleHeadMeshVisibility(false);
        }
        
        // Disable ThirdPersonController if present to avoid conflicts
        StarterAssets.ThirdPersonController tpc = instantiatedArmature.GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null)
        {
            tpc.enabled = false;
        }
        
        Debug.Log("Existing Player Armature setup complete!");
    }
    
    /// <summary>
    /// Finds a PlayerArmature or similar humanoid model in children
    /// </summary>
    private GameObject FindArmatureInChildren()
    {
        // First try to find a GameObject named "PlayerArmature"
        Transform playerArmature = transform.Find("PlayerArmature");
        if (playerArmature != null)
        {
            return playerArmature.gameObject;
        }
        
        // Look for any child with an Animator component
        Animator[] childAnimators = GetComponentsInChildren<Animator>();
        if (childAnimators.Length > 0)
        {
            return childAnimators[0].gameObject;
        }
        
        return null;
    }
    
    /// <summary>
    /// Instantiates the player armature and sets up the connection with XR controllers
    /// </summary>
    public void InstantiatePlayerArmature()
    {
        if (playerArmaturePrefab == null)
        {
            Debug.LogError("Player armature prefab is not assigned!");
            return;
        }
        
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin is not assigned!");
            return;
        }
        
        // Instantiate the armature as a child of the XR Origin
        instantiatedArmature = Instantiate(playerArmaturePrefab, xrOrigin.position, xrOrigin.rotation);
        
        // Parent it to the XR Origin
        instantiatedArmature.transform.SetParent(cameraOffset);
        
        // Position, rotate, and scale it correctly
        instantiatedArmature.transform.localPosition = positionOffset;
        instantiatedArmature.transform.localRotation = Quaternion.Euler(rotationOffset);
        instantiatedArmature.transform.localScale = Vector3.one * scaleFactor;
        
        // Add VRHumanoidController component if not already present
        humanoidController = instantiatedArmature.GetComponent<VRHumanoidController>();
        if (humanoidController == null)
        {
            humanoidController = instantiatedArmature.AddComponent<VRHumanoidController>();
        }
        
        // Get the animator
        armatureAnimator = instantiatedArmature.GetComponent<Animator>();
        
        // Find the key transforms if not manually assigned
        FindKeyTransforms();
        
        // Set up the VRHumanoidController
        SetupHumanoidController();
        
        // Hide head mesh if requested (to avoid seeing it from the inside when in VR)
        if (hideHeadMesh && headTransform != null)
        {
            ToggleHeadMeshVisibility(false);
        }
    }
    
    /// <summary>
    /// Finds key transforms in the humanoid model if not assigned
    /// </summary>
    private void FindKeyTransforms()
    {
        if (armatureAnimator == null || !armatureAnimator.isHuman)
        {
            Debug.LogWarning("Armature has no Animator component or is not a humanoid!");
            return;
        }
        
        // Find head if not assigned
        if (headTransform == null)
        {
            headTransform = armatureAnimator.GetBoneTransform(HumanBodyBones.Head);
        }
        
        // Find hands if not assigned
        if (leftHandTransform == null)
        {
            leftHandTransform = armatureAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        }
        
        if (rightHandTransform == null)
        {
            rightHandTransform = armatureAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        }
    }
    
    /// <summary>
    /// Sets up the VRHumanoidController with the correct references
    /// </summary>
    private void SetupHumanoidController()
    {
        if (humanoidController != null)
        {
            // Use properties instead of reflection
            humanoidController.XROrigin = xrOrigin;
            humanoidController.Headset = headCamera;
            humanoidController.LeftHandController = leftController;
            humanoidController.RightHandController = rightController;
            
            humanoidController.HumanoidRoot = instantiatedArmature.transform;
            humanoidController.HumanoidHead = headTransform;
            humanoidController.HumanoidLeftHand = leftHandTransform;
            humanoidController.HumanoidRightHand = rightHandTransform;
            
            // Set default offsets if needed
            humanoidController.ModelScale = scaleFactor;
        }
    }
    
    /// <summary>
    /// Toggles the visibility of the head mesh
    /// </summary>
    private void ToggleHeadMeshVisibility(bool visible)
    {
        if (headTransform != null)
        {
            // Check for SkinnedMeshRenderer on children
            SkinnedMeshRenderer[] renderers = headTransform.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = visible;
            }
            
            // Check for MeshRenderer on children
            MeshRenderer[] meshRenderers = headTransform.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers)
            {
                renderer.enabled = visible;
            }
        }
    }
} 