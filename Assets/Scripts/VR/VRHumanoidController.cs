using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Controls a humanoid model in VR by mapping XR controller positions to the model's limbs
/// and moving the character based on headset movement
/// Robot 에셋(팔다리 없음)과 휴머노이드 모델 모두 지원
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
    
    [Header("Robot Mode Settings (팔다리 없는 모델용)")]
    [SerializeField] private bool isRobotMode = false; // Robot 모드 활성화
    [SerializeField] private GameObject leftHandVisualizer; // 왼손 시각화 오브젝트
    [SerializeField] private GameObject rightHandVisualizer; // 오른손 시각화 오브젝트
    [SerializeField] private bool createHandVisualizers = true; // 손 시각화 오브젝트 자동 생성
    [SerializeField] private float handVisualizerSize = 0.1f; // 손 시각화 크기
    
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

    // 가상 손 위치 (Robot 모드용)
    public Vector3 VirtualLeftHandPosition { get; private set; }
    public Vector3 VirtualRightHandPosition { get; private set; }
    public Quaternion VirtualLeftHandRotation { get; private set; }
    public Quaternion VirtualRightHandRotation { get; private set; }

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
    
    public bool IsRobotMode { get => isRobotMode; set => isRobotMode = value; }

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
            
            // Robot 모드 자동 감지
            DetectRobotMode();
            
            // Robot 모드인 경우 손 시각화 오브젝트 생성
            if (isRobotMode && createHandVisualizers)
            {
                CreateHandVisualizers();
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
    
    /// <summary>
    /// 자동으로 Robot 모드인지 감지
    /// </summary>
    private void DetectRobotMode()
    {
        // 손 Transform이 없거나 Humanoid가 아닌 경우 Robot 모드로 설정
        bool hasHands = humanoidLeftHand != null && humanoidRightHand != null;
        bool isHumanoid = humanoidAnimator != null && humanoidAnimator.isHuman;
        
        if (!hasHands || !isHumanoid)
        {
            isRobotMode = true;
            Debug.Log($"Robot 모드 자동 감지됨: hasHands={hasHands}, isHumanoid={isHumanoid}");
        }
    }
    
    /// <summary>
    /// Robot 모드용 손 시각화 오브젝트 생성
    /// </summary>
    private void CreateHandVisualizers()
    {
        if (leftHandController != null && leftHandVisualizer == null)
        {
            leftHandVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftHandVisualizer.name = "LeftHandVisualizer";
            leftHandVisualizer.transform.SetParent(transform);
            leftHandVisualizer.transform.localScale = Vector3.one * handVisualizerSize;
            
            // 반투명한 파란색 Material 생성
            Renderer leftRenderer = leftHandVisualizer.GetComponent<Renderer>();
            if (leftRenderer != null)
            {
                Material leftMat = new Material(Shader.Find("Standard"));
                leftMat.color = new Color(0f, 0.5f, 1f, 0.7f);
                leftMat.SetFloat("_Mode", 3); // Transparent mode
                leftMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                leftMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                leftMat.SetInt("_ZWrite", 0);
                leftMat.DisableKeyword("_ALPHATEST_ON");
                leftMat.EnableKeyword("_ALPHABLEND_ON");
                leftMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                leftMat.renderQueue = 3000;
                leftRenderer.material = leftMat;
            }
        }
        
        if (rightHandController != null && rightHandVisualizer == null)
        {
            rightHandVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightHandVisualizer.name = "RightHandVisualizer";
            rightHandVisualizer.transform.SetParent(transform);
            rightHandVisualizer.transform.localScale = Vector3.one * handVisualizerSize;
            
            // 반투명한 빨간색 Material 생성
            Renderer rightRenderer = rightHandVisualizer.GetComponent<Renderer>();
            if (rightRenderer != null)
            {
                Material rightMat = new Material(Shader.Find("Standard"));
                rightMat.color = new Color(1f, 0.5f, 0f, 0.7f);
                rightMat.SetFloat("_Mode", 3); // Transparent mode
                rightMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                rightMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                rightMat.SetInt("_ZWrite", 0);
                rightMat.DisableKeyword("_ALPHATEST_ON");
                rightMat.EnableKeyword("_ALPHABLEND_ON");
                rightMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                rightMat.renderQueue = 3000;
                rightRenderer.material = rightMat;
            }
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
        
        if (isRobotMode)
        {
            UpdateVirtualHandPositions(); // Robot 모드: 가상 손 위치 업데이트
        }
        else
        {
            UpdateHandTransforms(); // 일반 모드: 실제 손 Transform 업데이트
        }
        
        // Apply additional IK if needed (Robot 모드에서는 건너뜀)
        if (useIK && humanoidAnimator != null && !isRobotMode)
        {
            ApplyIK();
        }
    }
    
    private void UpdateRootPosition()
    {
        if (enableHeadsetMovement)
        {
            // Get current position of the humanoid root
            Vector3 currentPosition = humanoidRoot.position;
            
            // Calculate the appropriate floor height - use a fixed ground offset
            // Maintain the robot at a consistent height from the ground
            float groundHeight = 0.0f; // Assuming 0 is the floor level
            float desiredRootHeight = groundHeight + heightOffset;
            
            // Adjust X and Z to follow headset but keep Y at a fixed height
            currentPosition.x = headset.position.x;
            currentPosition.z = headset.position.z;
            currentPosition.y = desiredRootHeight;
            
            // Update the character position
            if (characterController != null)
            {
                characterController.height = 2.0f; 
                characterController.center = new Vector3(0, 1.0f, 0);
                
                // Apply the position directly to ensure consistent height
                humanoidRoot.position = currentPosition;
            }
            else
            {
                humanoidRoot.position = currentPosition;
            }
        }
    }
    
    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset,
            transform.position.z);
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
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
            
        // Calculate the offset between humanoid root and head
        // The head should be positioned relative to the body, maintaining proper anatomy
        float headHeight = 1.6f; // Approximate height of the head from the root
        
        // Position the head above the root plus the offset to match the headset's rotation
        Vector3 targetHeadPosition = new Vector3(
            headset.position.x,
            humanoidRoot.position.y + headHeight,
            headset.position.z
        ) + headPositionOffset;
        
        humanoidHead.position = targetHeadPosition;
        
        // Apply the headset's rotation to the head
        humanoidHead.rotation = headset.rotation * Quaternion.Euler(headRotationOffset);
    }
    
    /// <summary>
    /// Robot 모드: 가상 손 위치 업데이트 (실제 Transform은 없지만 위치 추적)
    /// </summary>
    private void UpdateVirtualHandPositions()
    {
        // 가상 왼손 위치 업데이트
        if (leftHandController != null)
        {
            Vector3 leftOffset = leftHandController.TransformDirection(leftHandPositionOffset);
            VirtualLeftHandPosition = leftHandController.position + leftOffset;
            VirtualLeftHandRotation = leftHandController.rotation * Quaternion.Euler(leftHandRotationOffset);
            
            // 시각화 오브젝트 업데이트
            if (leftHandVisualizer != null)
            {
                leftHandVisualizer.transform.position = VirtualLeftHandPosition;
                leftHandVisualizer.transform.rotation = VirtualLeftHandRotation;
            }
        }
        
        // 가상 오른손 위치 업데이트
        if (rightHandController != null)
        {
            Vector3 rightOffset = rightHandController.TransformDirection(rightHandPositionOffset);
            VirtualRightHandPosition = rightHandController.position + rightOffset;
            VirtualRightHandRotation = rightHandController.rotation * Quaternion.Euler(rightHandRotationOffset);
            
            // 시각화 오브젝트 업데이트
            if (rightHandVisualizer != null)
            {
                rightHandVisualizer.transform.position = VirtualRightHandPosition;
                rightHandVisualizer.transform.rotation = VirtualRightHandRotation;
            }
        }
    }
    
    /// <summary>
    /// 일반 모드: 실제 손 Transform 업데이트
    /// </summary>
    private void UpdateHandTransforms()
    {
        // Update left hand (null 체크 강화)
        if (humanoidLeftHand != null && leftHandController != null)
        {
            Vector3 leftOffset = leftHandController.TransformDirection(leftHandPositionOffset);
            humanoidLeftHand.position = leftHandController.position + leftOffset;
            humanoidLeftHand.rotation = leftHandController.rotation * Quaternion.Euler(leftHandRotationOffset);
        }
        
        // Update right hand (null 체크 강화)
        if (humanoidRightHand != null && rightHandController != null)
        {
            Vector3 rightOffset = rightHandController.TransformDirection(rightHandPositionOffset);
            humanoidRightHand.position = rightHandController.position + rightOffset;
            humanoidRightHand.rotation = rightHandController.rotation * Quaternion.Euler(rightHandRotationOffset);
        }
    }
    
    /// <summary>
    /// IK 적용 (휴머노이드 모델에서만 사용)
    /// </summary>
    private void ApplyIK()
    {
        // Robot 모드에서는 IK 사용하지 않음
        if (isRobotMode || humanoidAnimator == null || !humanoidAnimator.isHuman)
            return;
            
        // This method would use Unity's Animator IK capabilities to smoothly position limbs
        // Requires a properly rigged humanoid model with an Animator component
        
        // Example implementation (would need to be expanded for a complete solution):
        if (humanoidAnimator != null)
        {
            // Set the IK position and rotation of the hands (null 체크 추가)
            if (leftHandController != null && humanoidLeftHand != null)
            {
                humanoidAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
                humanoidAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);
                humanoidAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandController.position + leftHandController.TransformDirection(leftHandPositionOffset));
                humanoidAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandController.rotation * Quaternion.Euler(leftHandRotationOffset));
            }
            
            if (rightHandController != null && humanoidRightHand != null)
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