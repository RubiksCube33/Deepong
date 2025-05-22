using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

/// <summary>
/// Makes the PlayerArmature follow Player_Origin while maintaining animations
/// </summary>
public class VROriginFollower : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The VR Origin to follow (e.g., Player_Origin_Test_1)")]
    [SerializeField] private Transform vrOrigin;
    
    [Tooltip("Reference to the model armature")]
    [SerializeField] private Transform PlayerArmature;

    [Tooltip("Reference to the main camera")]
    [SerializeField] private Camera mainCamera;
    
    [Header("Position Settings")]
    [Tooltip("Only animate the player but don't move it with VR origin")]
    [SerializeField] private bool animateOnly = false;
    
    [Tooltip("Offset from the VR origin")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("Position follow speed multiplier (higher = faster)")]
    [SerializeField] private float positionFollowSpeed = 10f;
    
    [Header("Rotation Settings")]
    [Tooltip("Whether to rotate the player")]
    [SerializeField] private bool enableRotation = true;
    
    [Tooltip("Follow VR Origin rotation instead of camera direction")]
    [SerializeField] private bool followOriginRotation = true;
    
    [Tooltip("Rotation follow speed multiplier (higher = faster)")]
    [SerializeField] private float rotationFollowSpeed = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // References to required components
    private Animator animator;
    private StarterAssetsInputs starterAssetsInputs;
    private ThirdPersonController thirdPersonController;
    private CharacterController characterController;
    
    // Stored last position for movement calculation
    private Vector3 lastOriginPosition;
    private bool isInitialized = false;
    
    void Start()
    {
        // Get component references
        animator = GetComponent<Animator>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        
        // Find VR origin if not assigned
        if (vrOrigin == null)
        {
            GameObject originObj = GameObject.Find("Player_Origin 2");
            if (originObj != null)
            {
                vrOrigin = originObj.transform;
                Debug.Log("[VROriginFollower] Found VR Origin: " + vrOrigin.name);
            }
            else
            {
                Debug.LogError("[VROriginFollower] VR Origin not found! Please assign it manually.");
            }
        }
        
        // Find main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[VROriginFollower] Main camera not found!");
            }
        }
        
        // Initialize
        if (vrOrigin != null)
        {
            lastOriginPosition = vrOrigin.position;
            isInitialized = true;
            
            // Set initial position and rotation
            if (!animateOnly)
            {
                Vector3 initialPos = vrOrigin.position + positionOffset;
                initialPos.y = PlayerArmature.position.y; // Keep original Y position
                PlayerArmature.position = initialPos;
                
                if (enableRotation && followOriginRotation)
                {
                    // Only copy Y rotation (around up axis)
                    Vector3 eulerAngles = PlayerArmature.eulerAngles;
                    eulerAngles.y = vrOrigin.eulerAngles.y;
                    PlayerArmature.eulerAngles = eulerAngles;
                }
            }
        }
        
        // Disable PlayerInput component
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
            Debug.Log("[VROriginFollower] Disabled PlayerInput component");
        }
        
        // Disable VRInputToThirdPerson if present
        var vrInput = GetComponent<VRInputToThirdPerson>();
        if (vrInput != null)
        {
            vrInput.enabled = false;
            Debug.Log("[VROriginFollower] Disabled VRInputToThirdPerson component");
        }
        
        // Ensure CharacterController is enabled
        if (characterController != null && !characterController.enabled)
        {
            characterController.enabled = true;
        }
    }
    
    // 테스트용: vrOrigin 위치를 직접 변경하는 메서드
    private void MoveVROriginForTesting()
    {
        // 방법 1: 시간 기반 자동 이동 (활성화)
        // 시간 기반 원형 움직임 (반지름 1, 30초 주기)
        float angle = (Time.time * 12f) * Mathf.Deg2Rad; // 30초에 한 바퀴
        float radius = 1.0f;
        
        Vector3 initialPos = new Vector3(0, 0, -3.5f); // 초기 위치
        Vector3 circularMotion = new Vector3(
            Mathf.Cos(angle) * radius, 
            0, 
            Mathf.Sin(angle) * radius
        );
        
        Vector3 newPosition = initialPos + circularMotion;
        
        // 방법 5: CharacterController를 통한 이동 (Player_Origin 2에 CharacterController가 있는 경우)
        CharacterController originCC = vrOrigin.GetComponent<CharacterController>();
        if (originCC != null)
        {
            Vector3 moveVector = newPosition - vrOrigin.position;
            originCC.Move(moveVector);
            //Debug.Log($"[TEST] Moving with CharacterController: {moveVector}");
        }
        else
        {
            // 방법 6: Rigidbody를 통한 이동 (Player_Origin 2에 Rigidbody가 있는 경우)
            Rigidbody originRB = vrOrigin.GetComponent<Rigidbody>();
            if (originRB != null)
            {
                originRB.MovePosition(newPosition);
                //Debug.Log($"[TEST] Moving with Rigidbody: {newPosition}");
            }
            else
            {
                // 방법 7: TransformPoint 및 강제 위치 설정
                vrOrigin.position = newPosition;
                //Debug.Log($"[TEST] Forced position change: {newPosition}");
            }
        }
        
        //Debug.Log($"[TEST] Desired Position: {newPosition}, Current Position: {vrOrigin.position}");
    }
    
    void LateUpdate() {
        if (!isInitialized || vrOrigin == null)
            return;
            
        // VR Origin 테스트 이동 (필요 시)
        MoveVROriginForTesting();
        
        // 캐릭터 위치를 VR Origin에 즉시 동기화
        Vector3 targetPosition = vrOrigin.position + positionOffset;
        
        // Y축 유지 (필요한 경우)
        if (!followFullPosition) {
            targetPosition.y = PlayerArmature.position.y; // 기존 Y축 위치 유지
        }
        
        // CharacterController 처리
        if (characterController != null && characterController.enabled) {
            // 방법 1: 텔레포트 방식
            characterController.enabled = false;
            PlayerArmature.position = targetPosition;
            characterController.enabled = true;
            
            // 중력 적용 (필요한 경우)
            if (applyGravity && thirdPersonController != null) {
                Vector3 verticalVelocity = new Vector3(0, thirdPersonController.Gravity * Time.deltaTime, 0);
                characterController.Move(verticalVelocity * Time.deltaTime);
            }
        } else {
            // 방법 2: 직접 위치 설정
            PlayerArmature.position = targetPosition;
        }
        
        // 회전 동기화
        if (enableRotation) {
            if (followOriginRotation) {
                // Y축 회전만 동기화 (FPS 게임 스타일)
                Vector3 eulerAngles = PlayerArmature.eulerAngles;
                eulerAngles.y = vrOrigin.eulerAngles.y;
                PlayerArmature.eulerAngles = eulerAngles;
            } else {
                // 전체 회전 동기화 (VR 손 추적 스타일)
                PlayerArmature.rotation = vrOrigin.rotation;
            }
        }
        
        // 애니메이션 업데이트 (필요한 경우)
        UpdateAnimations();
    }
    
    // 애니메이션 관련 코드만 분리 (필요한 경우)
    private void UpdateAnimations() {
        if (starterAssetsInputs == null || animator == null)
            return;
            
        // 이동 감지 (현재 프레임과 이전 프레임의 위치 차이)
        Vector3 movementDelta = vrOrigin.position - lastOriginPosition;
        Vector3 horizontalMovement = new Vector3(movementDelta.x, 0, movementDelta.z);
        float movementMagnitude = horizontalMovement.magnitude / Time.deltaTime;
        
        // 애니메이션 입력 설정
        if (movementMagnitude > 0.1f) {
            Vector3 localMovement = PlayerArmature.InverseTransformDirection(horizontalMovement.normalized);
            starterAssetsInputs.move = new Vector2(localMovement.x, localMovement.z);
            starterAssetsInputs.sprint = movementMagnitude > 3.0f;
        } else {
            starterAssetsInputs.move = Vector2.zero;
            starterAssetsInputs.sprint = false;
        }
        
        // 현재 위치 저장
        lastOriginPosition = vrOrigin.position;
    }
    
    // 추가 설정 (Inspector에서 조정 가능)
    [Tooltip("Y축 위치도 함께 따라갈지 여부")]
    [SerializeField] private bool followFullPosition = false;
    
    [Tooltip("중력을 적용할지 여부")]
    [SerializeField] private bool applyGravity = true;
    
    void Update()
    {
        /* 기존 코드 주석 처리
        if (!isInitialized || vrOrigin == null)
            return;
        
        // 테스트용: vrOrigin 위치를 직접 변경 (키보드 입력 또는 시간 기반 이동)
        MoveVROriginForTesting();
        
        // 디버그: vrOrigin 위치 변화 로그 추가
        Debug.Log($"VR Origin Position: {vrOrigin.position}");

        // 여기서 마지막 위치와 현재 위치의 차이를 계산합니다
        Vector3 originMovementDelta = vrOrigin.position - lastOriginPosition;
        Vector3 horizontalMovement = new Vector3(originMovementDelta.x, 0, originMovementDelta.z);
        
        // Update animations based on movement
        if (starterAssetsInputs != null && animator != null)
        {
            float movementMagnitude = horizontalMovement.magnitude / Time.deltaTime;
            
            // Set input based on movement magnitude
            if (movementMagnitude > 0.1f)
            {
                // Convert to local movement
                Vector3 localMovement = transform.InverseTransformDirection(horizontalMovement.normalized);
                starterAssetsInputs.move = new Vector2(localMovement.x, localMovement.z);
                
                // Detect sprint (if moving fast enough)
                starterAssetsInputs.sprint = movementMagnitude > 3.0f;
            }
            else
            {
                starterAssetsInputs.move = Vector2.zero;
                starterAssetsInputs.sprint = false;
            }
        }
        
        // Follow VR Origin if not in animate-only mode
        if (!animateOnly)
        {
            // 즉각적인 동기화를 위해 수정된 부분 ---------------
            
            // 1. 타겟 위치 계산 (Y축 유지)
            Vector3 targetPosition = vrOrigin.position + positionOffset;
            targetPosition.y = transform.position.y; // 캐릭터의 Y 위치 유지
            
            // 2. 즉각적인 위치 동기화 방법
            if (characterController != null && characterController.enabled)
            {
                // 방법 A: CharacterController가 있는 경우 - 텔레포트 방식으로 이동
                // (중요: 이 방법은 CharacterController의 충돌 체크를 무시합니다)
                characterController.enabled = false;
                transform.position = targetPosition;
                characterController.enabled = true;
                
                // 중력 적용 (필요한 경우)
                if (thirdPersonController != null)
                {
                    Vector3 verticalVelocity = new Vector3(0, thirdPersonController.Gravity * Time.deltaTime, 0);
                    characterController.Move(verticalVelocity * Time.deltaTime);
                }
            }
            else
            {
                // 방법 B: CharacterController가 없는 경우 - 직접 위치 설정
                transform.position = targetPosition;
            }
            
            // 회전 업데이트
            if (enableRotation)
            {
                if (followOriginRotation)
                {
                    // VR Origin의 회전을 따름 (Y축만)
                    Vector3 eulerAngles = transform.eulerAngles;
                    eulerAngles.y = vrOrigin.eulerAngles.y;
                    transform.eulerAngles = eulerAngles;
                }
                else
                {
                    // 카메라 방향을 따름
                    Vector3 cameraForward = mainCamera.transform.forward;
                    cameraForward.y = 0f;
                    cameraForward.Normalize();
                    
                    if (cameraForward.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(cameraForward);
                    }
                }
            }
            // ----------------------------------------------
        }
        
        // Store current position for next frame
        lastOriginPosition = vrOrigin.position;
        */
    }
    
    // For debugging
    void OnGUI()
    {
        if (!showDebugInfo)
            return;
            
        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.Label("VR Origin Follower Status:");
        GUILayout.Label($"Initialized: {isInitialized}");
        GUILayout.Label($"Animate Only: {animateOnly}");
        GUILayout.Label($"Rotation Mode: {(followOriginRotation ? "Follow Origin" : "Follow Camera")}");
        
        if (starterAssetsInputs != null)
        {
            GUILayout.Label($"Move Input: {starterAssetsInputs.move}");
            GUILayout.Label($"Sprint: {starterAssetsInputs.sprint}");
        }
        
        if (vrOrigin != null)
        {
            GUILayout.Label($"Origin Position: {vrOrigin.position.ToString("F2")}");
            GUILayout.Label($"Origin Rotation: {vrOrigin.eulerAngles.ToString("F1")}");
            GUILayout.Label($"Player Position: {PlayerArmature.position.ToString("F2")}");
            GUILayout.Label($"Player Rotation: {PlayerArmature.eulerAngles.ToString("F1")}");
            GUILayout.Label($"Distance: {Vector3.Distance(vrOrigin.position, PlayerArmature.position).ToString("F2")}");
        }
        
        GUILayout.EndArea();
    }
} 