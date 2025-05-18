using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Controls a humanoid model in VR by mapping XR controller positions to the model's limbs
/// and moving the character based on headset movement
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
    
    [Header("Movement Settings")]
    [SerializeField] private bool enableHeadsetMovement = true;
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private float gravity = -15.0f;
    [SerializeField] private float fallTimeout = 0.15f;
    [SerializeField] private float movementThreshold = 0.01f;
    [SerializeField] private float movementMultiplier = 10f;
    [SerializeField] private float heightOffset = 0.0f; // 캐릭터 높이 조정

    [Header("Ground Settings")]
    [SerializeField] private bool grounded = true;
    [SerializeField] private float groundedOffset = -0.14f;
    [SerializeField] private float groundedRadius = 0.28f;
    [SerializeField] private LayerMask groundLayers;

    private Animator humanoidAnimator;
    private CharacterController characterController;
    private Vector3 initialRootPosition;
    private Quaternion initialRootRotation;
    
    // Movement variables
    private Vector3 previousHeadPosition;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private float terminalVelocity = 53.0f;
    private float fallTimeoutDelta;
    private float rotationVelocity;

    // Animation IDs
    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;

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
            characterController = humanoidRoot.GetComponent<CharacterController>();
            if (characterController == null)
            {
                // 없으면 추가
                characterController = humanoidRoot.gameObject.AddComponent<CharacterController>();
                characterController.center = new Vector3(0, 1.0f, 0);
                characterController.height = 2.0f;
                characterController.radius = 0.3f;
            }
            
            // Set up animation parameters
            AssignAnimationIDs();
            
            // Initialize movement values
            previousHeadPosition = headset != null ? headset.position : Vector3.zero;
            fallTimeoutDelta = fallTimeout;
        }
        else
        {
            Debug.LogError("Humanoid root is not assigned. Please assign a humanoid model root transform.");
        }
    }
    
    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    void Update()
    {
        if (humanoidRoot == null || xrOrigin == null)
            return;
            
        if (enableHeadsetMovement)
        {
            GroundedCheck();
            HandleMovement();
            ApplyGravity();
        }
    }

    void LateUpdate()
    {
        if (humanoidRoot == null || xrOrigin == null)
            return;

        // 로봇 몸체를 먼저 배치하고
        UpdateRootPosition();

        // 그 다음에 머리와 손 위치 업데이트
        UpdateHeadTransform();
        UpdateHandTransforms();
        
        // Apply additional IK if needed
        if (useIK && humanoidAnimator != null)
        {
            ApplyIK();
        }
    }
    
    private void UpdateRootPosition()
    {
        if (enableHeadsetMovement)
        {
            // 캐릭터 컨트롤러가 이미 위치를 관리함
            return;
        }

        // 캐릭터 컨트롤러가 없는 경우에만 수동으로 위치 업데이트
        // 헤드셋 위치 기준으로 로봇 위치 조정 (높이는 initialRootPosition.y 유지)
        Vector3 targetPosition = xrOrigin.position + rootPositionOffset;
        targetPosition.y = initialRootPosition.y + heightOffset; 
        
        // 헤드셋의 전방 방향을 기준으로 로봇 회전
        Vector3 headsetForward = headset.forward;
        headsetForward.y = 0;
        headsetForward.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(headsetForward, Vector3.up);
        targetRotation *= Quaternion.Euler(rootRotationOffset);
        
        // 위치와 회전 적용
        humanoidRoot.position = targetPosition;
        humanoidRoot.rotation = targetRotation;
        
        // 크기 적용
        humanoidRoot.localScale = Vector3.one * modelScale;
    }
    
    private void GroundedCheck()
    {
        if (characterController == null) return;
        
        // Set sphere position, with offset
        Vector3 spherePosition = new Vector3(humanoidRoot.position.x, humanoidRoot.position.y - groundedOffset, humanoidRoot.position.z);
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

        // Update animator if using character
        if (humanoidAnimator != null)
        {
            humanoidAnimator.SetBool(animIDGrounded, grounded);
        }
    }
    
    private void HandleMovement()
    {
        if (headset == null || characterController == null) return;
        
        // Calculate horizontal movement from head position change
        Vector3 currentHeadPosition = headset.position;
        Vector3 headDelta = currentHeadPosition - previousHeadPosition;
        
        // Only use horizontal movement (ignore vertical movement)
        headDelta.y = 0;
        
        // Project the movement based on the camera's forward direction
        Vector3 forward = headset.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 right = headset.right;
        right.y = 0;
        right.Normalize();
        
        // Check if there's significant horizontal head movement
        if (headDelta.magnitude > movementThreshold)
        {            
            // Project movement onto forward/right plane
            moveDirection = Vector3.zero;
            moveDirection += forward * Vector3.Dot(headDelta, forward);
            moveDirection += right * Vector3.Dot(headDelta, right);
            
            // Rotate character to match movement direction
            if (moveDirection.sqrMagnitude > movementThreshold)
            {
                float targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float rotation = Mathf.SmoothDampAngle(humanoidRoot.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
                
                // Apply rotation to humanoid root
                humanoidRoot.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                
                // Scale movement speed based on head movement speed
                float currentSpeed = Mathf.Clamp(moveDirection.magnitude * movementMultiplier, 0, moveSpeed);
                
                // Apply movement - 여기서 이동하는 부분
                characterController.Move(moveDirection.normalized * currentSpeed * Time.deltaTime + new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);
                
                // Update animator
                if (humanoidAnimator != null)
                {
                    humanoidAnimator.SetFloat(animIDSpeed, currentSpeed / moveSpeed);
                    humanoidAnimator.SetFloat(animIDMotionSpeed, 1f);
                }
            }
            else
            {
                if (humanoidAnimator != null)
                {
                    humanoidAnimator.SetFloat(animIDSpeed, 0);
                    humanoidAnimator.SetFloat(animIDMotionSpeed, 0);
                }
            }
        }
        else
        {
            // No movement but still apply gravity
            if (characterController.isGrounded == false)
            {
                characterController.Move(new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);
            }
            
            // Update animator
            if (humanoidAnimator != null)
            {
                humanoidAnimator.SetFloat(animIDSpeed, 0);
                humanoidAnimator.SetFloat(animIDMotionSpeed, 0);
            }
        }
        
        // Update previous head position for next frame
        previousHeadPosition = currentHeadPosition;
    }

    private void ApplyGravity()
    {
        if (characterController == null) return;
        
        if (grounded)
        {
            // Reset the fall timeout timer
            fallTimeoutDelta = fallTimeout;

            // Update animator
            if (humanoidAnimator != null)
            {
                humanoidAnimator.SetBool(animIDFreeFall, false);
            }

            // Stop our velocity dropping infinitely when grounded
            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }
        }
        else
        {
            // Fall timeout
            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // Update animator if using character
                if (humanoidAnimator != null)
                {
                    humanoidAnimator.SetBool(animIDFreeFall, true);
                }
            }
        }

        // Apply gravity over time
        if (verticalVelocity < terminalVelocity)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
    
    private void UpdateHeadTransform()
    {
        if (humanoidHead == null || headset == null)
            return;
            
        // 머리가 늘어나는 문제 해결: 캐릭터 기준으로 상대적인 헤드 오프셋만 적용
        float headHeight = 1.6f; // 캐릭터의 머리 높이 (기본값, 조정 필요)
        
        // 헤드셋의 회전만 적용하고, 위치는 로봇 기준 상대적 위치 사용
        Vector3 targetHeadPosition = humanoidRoot.position + new Vector3(0, headHeight, 0) + headPositionOffset;
        humanoidHead.position = targetHeadPosition;
        
        // 회전은 헤드셋의 회전 적용
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
    
    private void OnDrawGizmosSelected()
    {
        if (!enableHeadsetMovement) return;
        
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // When selected, draw a gizmo for the grounded check
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z),
            groundedRadius);
    }
} 