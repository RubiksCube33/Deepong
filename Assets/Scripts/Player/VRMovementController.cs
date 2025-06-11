using UnityEngine;
using Unity.XR.CoreUtils;
using Photon.Pun;

/// <summary>
/// VR 플레이어의 안전한 이동을 관리하는 스크립트
/// CharacterController와 XR Origin 간의 동기화를 처리하고 바닥 뚫림을 방지합니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class VRMovementController : MonoBehaviourPun
{
    [Header("VR 이동 설정")]
    public bool enableVRMovement = true;
    public float gravity = -9.81f;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundMask = -1;
    
    [Header("디버그")]
    public bool showDebugInfo = false;
    
    private CharacterController characterController;
    private MonoBehaviour xrOrigin;
    private Transform cameraOffset;
    private Camera vrCamera;
    
    // 이동 관련
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 lastCameraPosition;
    
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
        
        // XR Origin 찾기
        var allComponents = GetComponents<MonoBehaviour>();
        foreach (var comp in allComponents)
        {
            if (comp.GetType().Name.Contains("XROrigin") || comp.GetType().Name.Contains("Origin"))
            {
                xrOrigin = comp;
                break;
            }
        }
        
        // Camera Offset과 VR Camera 찾기
        cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            Transform cameraTransform = cameraOffset.Find("Main Camera");
            if (cameraTransform != null)
            {
                vrCamera = cameraTransform.GetComponent<Camera>();
                lastCameraPosition = vrCamera.transform.position;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[VRMovementController] 초기화 완료");
            Debug.Log($"  - CharacterController: {characterController != null}");
            Debug.Log($"  - XROrigin: {xrOrigin != null}");
            Debug.Log($"  - Camera Offset: {cameraOffset != null}");
            Debug.Log($"  - VR Camera: {vrCamera != null}");
        }
    }
    
    void Update()
    {
        if (!enableVRMovement || !photonView.IsMine) return;
        
        HandleVRMovement();
        ApplyGravity();
    }
    
    void HandleVRMovement()
    {
        if (vrCamera == null || cameraOffset == null) return;
        
        // VR 카메라의 실제 이동량 계산
        Vector3 cameraMovement = vrCamera.transform.position - lastCameraPosition;
        
        // Y축 이동은 제외 (중력으로 처리)
        cameraMovement.y = 0;
        
        // CharacterController로 이동 적용
        if (cameraMovement.magnitude > 0.001f)
        {
            characterController.Move(cameraMovement);
            
            if (showDebugInfo)
            {
                Debug.Log($"[VRMovementController] VR 이동: {cameraMovement}");
            }
        }
        
        // 카메라 위치 업데이트
        lastCameraPosition = vrCamera.transform.position;
    }
    
    void ApplyGravity()
    {
        // 바닥 체크
        isGrounded = Physics.CheckSphere(
            transform.position + characterController.center - Vector3.up * (characterController.height * 0.5f),
            groundCheckRadius,
            groundMask
        );
        
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
        
        // CharacterController 영역 표시 (캡슐 대신 실린더 형태로)
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
            
            if (showDebugInfo)
            {
                Debug.Log($"[VRMovementController] 안전 위치로 텔레포트: {safePosition}");
            }
        }
    }
} 