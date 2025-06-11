using UnityEngine;
using Unity.XR.CoreUtils;
using Photon.Pun;

/// <summary>
/// VR 플레이어의 안전한 이동을 관리하는 스크립트
/// CharacterController와 XR Origin 간의 동기화를 처리하고 바닥 뚫림을 방지합니다.
/// 프리팹에서 Locomotion이 비활성화되어 있으므로 충돌 없이 작동합니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class VRMovementController : MonoBehaviourPun
{
    [Header("VR 이동 설정")]
    public bool enableVRMovement = true;
    public float gravity = -9.81f;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundMask = -1;
    
    [Header("VR 추적 설정")]
    public bool enableHeadTracking = true;
    public float maxTrackingDistance = 2f; // 한 프레임에 허용되는 최대 이동 거리
    
    [Header("디버그")]
    public bool showDebugInfo = false;
    
    private CharacterController characterController;
    private Transform cameraOffset;
    private Camera vrCamera;
    
    // 이동 관련
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 lastCameraPosition;
    private Vector3 lastValidPosition;
    
    // VR 추적
    private bool hasInitialized = false;
    
    void Start()
    {
        // 로컬 플레이어만 활성화
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }
        
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();
        
        // Camera Offset과 VR Camera 찾기
        cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            Transform cameraTransform = cameraOffset.Find("Main Camera");
            if (cameraTransform != null)
            {
                vrCamera = cameraTransform.GetComponent<Camera>();
                
                if (vrCamera != null)
                {
                    lastCameraPosition = vrCamera.transform.position;
                    lastValidPosition = transform.position;
                    hasInitialized = true;
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[VRMovementController] 초기화 완료");
            Debug.Log($"  - CharacterController: {characterController != null}");
            Debug.Log($"  - Camera Offset: {cameraOffset != null}");
            Debug.Log($"  - VR Camera: {vrCamera != null}");
            Debug.Log($"  - 초기화 성공: {hasInitialized}");
        }
    }
    
    void Update()
    {
        if (!enableVRMovement || !photonView.IsMine || !hasInitialized) return;
        
        HandleVRMovement();
        ApplyGravity();
    }
    
    void HandleVRMovement()
    {
        if (vrCamera == null || !enableHeadTracking) return;
        
        // VR 카메라의 실제 이동량 계산
        Vector3 currentCameraPosition = vrCamera.transform.position;
        Vector3 cameraMovement = currentCameraPosition - lastCameraPosition;
        
        // Y축 이동은 제외 (중력으로 처리)
        cameraMovement.y = 0;
        
        // 비정상적으로 큰 이동량 필터링 (텔레포트나 오류 방지)
        if (cameraMovement.magnitude > maxTrackingDistance)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[VRMovementController] 비정상적인 이동량 감지: {cameraMovement.magnitude:F2}m, 무시됨");
            }
            
            // 카메라 위치만 업데이트하고 이동은 무시
            lastCameraPosition = currentCameraPosition;
            return;
        }
        
        // CharacterController로 이동 적용
        if (cameraMovement.magnitude > 0.001f)
        {
            // 이동 전 위치 저장
            Vector3 beforePosition = transform.position;
            
            // CharacterController로 이동
            characterController.Move(cameraMovement);
            
            // 이동 후 위치 확인
            Vector3 afterPosition = transform.position;
            Vector3 actualMovement = afterPosition - beforePosition;
            
            if (showDebugInfo)
            {
                Debug.Log($"[VRMovementController] VR 이동 - 요청: {cameraMovement}, 실제: {actualMovement}");
            }
            
            // 실제로 이동했다면 유효 위치 업데이트
            if (actualMovement.magnitude > 0.001f)
            {
                lastValidPosition = afterPosition;
            }
        }
        
        // 카메라 위치 업데이트
        lastCameraPosition = currentCameraPosition;
    }
    
    void ApplyGravity()
    {
        // 바닥 체크
        Vector3 checkPosition = transform.position + characterController.center - Vector3.up * (characterController.height * 0.5f);
        isGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 바닥에 붙어있도록
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
        
        // 중력 적용
        characterController.Move(velocity * Time.deltaTime);
        
        if (showDebugInfo)
        {
            Debug.Log($"[VRMovementController] 중력 - isGrounded: {isGrounded}, velocity.y: {velocity.y:F2}");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (characterController == null) return;
        
        // 바닥 체크 영역 표시
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 sphereCenter = transform.position + characterController.center - Vector3.up * (characterController.height * 0.5f);
        Gizmos.DrawWireSphere(sphereCenter, groundCheckRadius);
        
        // CharacterController 영역 표시
        Gizmos.color = Color.blue;
        Vector3 controllerCenter = transform.position + characterController.center;
        float halfHeight = characterController.height * 0.5f;
        
        // 상단과 하단 원
        Gizmos.DrawWireSphere(controllerCenter + Vector3.up * halfHeight, characterController.radius);
        Gizmos.DrawWireSphere(controllerCenter - Vector3.up * halfHeight, characterController.radius);
        
        // 측면 선들
        Vector3[] directions = {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right
        };
        
        foreach (Vector3 dir in directions)
        {
            Vector3 offset = dir * characterController.radius;
            Vector3 topPoint = controllerCenter + Vector3.up * halfHeight + offset;
            Vector3 bottomPoint = controllerCenter - Vector3.up * halfHeight + offset;
            Gizmos.DrawLine(topPoint, bottomPoint);
        }
        
        // 마지막 유효 위치 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastValidPosition, 0.1f);
    }
    
    /// <summary>
    /// 안전한 위치로 텔레포트
    /// </summary>
    public void TeleportToSafePosition(Vector3 safePosition)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = safePosition;
            characterController.enabled = true;
            
            velocity = Vector3.zero;
            lastValidPosition = safePosition;
            
            if (vrCamera != null)
            {
                lastCameraPosition = vrCamera.transform.position;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[VRMovementController] 안전 위치로 텔레포트: {safePosition}");
            }
        }
    }
    
    /// <summary>
    /// VR 추적 활성화/비활성화
    /// </summary>
    public void SetHeadTrackingEnabled(bool enabled)
    {
        enableHeadTracking = enabled;
        
        if (showDebugInfo)
        {
            Debug.Log($"[VRMovementController] VR 추적 {(enabled ? "활성화" : "비활성화")}");
        }
    }
} 