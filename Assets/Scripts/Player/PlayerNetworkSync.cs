using Photon.Pun;
using UnityEngine;

/// <summary>
/// PUN2를 사용하여 플레이어의 위치, 회전, 애니메이션을 동기화합니다.
/// VR 플레이어의 머리, 양손, 몸체의 움직임을 네트워크를 통해 동기화합니다.
/// Robot 에셋(팔다리 없음)과 휴머노이드 모델 모두 지원
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerNetworkSync : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("동기화할 Transform들")]
    [SerializeField] private Transform playerRoot; // 플레이어 루트 (몸체)
    [SerializeField] private Transform headTransform; // 머리
    [SerializeField] private Transform leftHandTransform; // 왼손
    [SerializeField] private Transform rightHandTransform; // 오른손
    
    [Header("Robot Mode 설정")]
    [SerializeField] private bool useVirtualHands = false; // Robot 모드에서 가상 손 사용
    
    [Header("동기화 설정")]
    [SerializeField] private float positionLerpRate = 10f; // 위치 보간 속도
    [SerializeField] private float rotationLerpRate = 10f; // 회전 보간 속도
    [SerializeField] private float teleportThreshold = 5f; // 순간이동 임계값
    
    [Header("애니메이션 동기화")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private bool syncAnimationParams = true;
    
    // 네트워크에서 수신받은 데이터들
    private Vector3 networkRootPosition;
    private Quaternion networkRootRotation;
    private Vector3 networkHeadPosition;
    private Quaternion networkHeadRotation;
    private Vector3 networkLeftHandPosition;
    private Quaternion networkLeftHandRotation;
    private Vector3 networkRightHandPosition;
    private Quaternion networkRightHandRotation;
    
    // 애니메이션 파라미터
    private float networkSpeed;
    private bool networkGrounded;
    private float networkMotionSpeed;
    
    // 초기화 플래그
    private bool hasReceivedData = false;

    
    // 원격 플레이어 VR 컨트롤러 시각화
    [Header("원격 플레이어 시각화")]
    [SerializeField] private GameObject leftControllerVisualizer;
    [SerializeField] private GameObject rightControllerVisualizer;
    [SerializeField] private bool createControllerVisualizers = true;

    void Awake()
    {
        // 자동으로 Transform 참조들을 찾기 시도
        if (playerRoot == null)
            playerRoot = transform;
            
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
        
        // 초기값 설정
        InitializeNetworkValues();
        
        // 원격 플레이어용 컨트롤러 시각화 오브젝트 생성
        if (!photonView.IsMine && createControllerVisualizers)
        {
            CreateControllerVisualizers();
        }
    }
    
    /// <summary>
    /// XR Origin에서 직접 컨트롤러 참조를 찾습니다.
    /// </summary>
    void FindXRControllerReferences()
    {
        // XR Origin 찾기
        Transform xrOrigin = transform.root;
        
        // Camera Offset 찾기
        Transform cameraOffset = xrOrigin.Find("Camera Offset");
        if (cameraOffset == null)
        {
            // 다른 이름으로 시도
            cameraOffset = xrOrigin.Find("XR Origin/Camera Offset");
        }
        
        if (cameraOffset != null)
        {
            // 헤드셋 찾기
            if (headTransform == null)
            {
                headTransform = cameraOffset.Find("Main Camera");
                if (headTransform == null)
                    headTransform = cameraOffset.Find("CenterEyeAnchor");
            }
            
            // 왼쪽 컨트롤러 찾기
            if (leftHandTransform == null)
            {
                leftHandTransform = cameraOffset.Find("LeftHand Controller");
                if (leftHandTransform == null)
                    leftHandTransform = cameraOffset.Find("Left Controller");
                if (leftHandTransform == null)
                    leftHandTransform = cameraOffset.Find("LeftHandAnchor");
            }
            
            // 오른쪽 컨트롤러 찾기
            if (rightHandTransform == null)
            {
                rightHandTransform = cameraOffset.Find("RightHand Controller");
                if (rightHandTransform == null)
                    rightHandTransform = cameraOffset.Find("Right Controller");
                if (rightHandTransform == null)
                    rightHandTransform = cameraOffset.Find("RightHandAnchor");
            }
            
            Debug.Log($"XR 컨트롤러 직접 참조 설정: Head={headTransform?.name}, LeftHand={leftHandTransform?.name}, RightHand={rightHandTransform?.name}");
        }
        else
        {
            Debug.LogWarning("Camera Offset을 찾을 수 없습니다. VR 컨트롤러 동기화가 제한됩니다.");
        }
    }
    
    void InitializeNetworkValues()
    {
        if (playerRoot != null)
        {
            networkRootPosition = playerRoot.position;
            networkRootRotation = playerRoot.rotation;
        }
        
        if (headTransform != null)
        {
            networkHeadPosition = headTransform.position;
            networkHeadRotation = headTransform.rotation;
        }
    }

    void Update()
    {
        // 내가 소유한 플레이어가 아니고, 네트워크 데이터를 받은 경우
        if (!photonView.IsMine && hasReceivedData)
        {
            // 현재는 수신한 데이터를 Transform에 적용하지 않음 (변수에만 저장)
            // SyncTransforms(); // 비활성화
            
            if (syncAnimationParams && playerAnimator != null)
            {
                //SyncAnimationParameters();
            }
        }
    }
    
    void SyncTransforms()
    {
        float deltaTime = Time.deltaTime;
        
        // 플레이어 루트 동기화
        if (playerRoot != null)
        {
            // 거리가 임계값을 초과하면 순간이동
            float rootDistance = Vector3.Distance(playerRoot.position, networkRootPosition);
            if (rootDistance > teleportThreshold)
            {
                playerRoot.position = networkRootPosition;
                playerRoot.rotation = networkRootRotation;
                Debug.LogWarning($"Player {photonView.Owner.NickName} teleported: distance was {rootDistance:F2}");
            }
            else
            {
                // 부드럽게 보간
                playerRoot.position = Vector3.Lerp(playerRoot.position, networkRootPosition, 
                                                  deltaTime * positionLerpRate);
                playerRoot.rotation = Quaternion.Lerp(playerRoot.rotation, networkRootRotation, 
                                                     deltaTime * rotationLerpRate);
            }
        }
        
        // 머리 동기화
        if (headTransform != null)
        {
            headTransform.position = Vector3.Lerp(headTransform.position, networkHeadPosition, 
                                                 deltaTime * positionLerpRate);
            headTransform.rotation = Quaternion.Lerp(headTransform.rotation, networkHeadRotation, 
                                                    deltaTime * rotationLerpRate);
        }
        
        // VR 컨트롤러 시각화 동기화 (원격 플레이어용)
        if (leftControllerVisualizer != null)
        {
            leftControllerVisualizer.transform.position = Vector3.Lerp(leftControllerVisualizer.transform.position, networkLeftHandPosition, 
                                                                      deltaTime * positionLerpRate);
            leftControllerVisualizer.transform.rotation = Quaternion.Lerp(leftControllerVisualizer.transform.rotation, networkLeftHandRotation, 
                                                                         deltaTime * rotationLerpRate);
        }
        
        if (rightControllerVisualizer != null)
        {
            rightControllerVisualizer.transform.position = Vector3.Lerp(rightControllerVisualizer.transform.position, networkRightHandPosition, 
                                                                       deltaTime * positionLerpRate);
            rightControllerVisualizer.transform.rotation = Quaternion.Lerp(rightControllerVisualizer.transform.rotation, networkRightHandRotation, 
                                                                          deltaTime * rotationLerpRate);
        }
    }
    
    void SyncAnimationParameters()
    {
        // 애니메이션 파라미터 동기화 (존재하는 파라미터만)
        if (HasAnimatorParameter("Speed"))
            playerAnimator.SetFloat("Speed", networkSpeed);
        if (HasAnimatorParameter("MotionSpeed"))
            playerAnimator.SetFloat("MotionSpeed", networkMotionSpeed);
        if (HasAnimatorParameter("Grounded"))
            playerAnimator.SetBool("Grounded", networkGrounded);
    }
    
    /// <summary>
    /// Animator에 특정 파라미터가 존재하는지 확인
    /// </summary>
    private bool HasAnimatorParameter(string parameterName)
    {
        if (playerAnimator == null) return false;
        
        foreach (AnimatorControllerParameter parameter in playerAnimator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }
        return false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 플레이어의 정보를 다른 클라이언트에게 전송
            
            // 플레이어 루트 위치/회전
            if (playerRoot != null)
            {
                stream.SendNext(playerRoot.position);
                stream.SendNext(playerRoot.rotation);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
            
            // 머리 위치/회전
            if (headTransform != null)
            {
                stream.SendNext(headTransform.position);
                stream.SendNext(headTransform.rotation);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
            
            // VR 컨트롤러 위치/회전 전송 (항상 실제 위치 사용)
            Vector3 leftPos = Vector3.zero;
            Quaternion leftRot = Quaternion.identity;
            Vector3 rightPos = Vector3.zero;
            Quaternion rightRot = Quaternion.identity;
            
            if (leftHandTransform != null)
            {
                leftPos = leftHandTransform.position;
                leftRot = leftHandTransform.rotation;
                stream.SendNext(leftPos);
                stream.SendNext(leftRot);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
            
            if (rightHandTransform != null)
            {
                rightPos = rightHandTransform.position;
                rightRot = rightHandTransform.rotation;
                stream.SendNext(rightPos);
                stream.SendNext(rightRot);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
            
            // 디버깅: 내 XR Origin 데이터 전송 확인 (5초마다)
            if (Time.time % 5f < Time.deltaTime)
            {
                Vector3 headPos = headTransform != null ? headTransform.position : Vector3.zero;
                Debug.Log($"[전송] {photonView.Owner.NickName}의 XR Origin 데이터 전송 중 - Head: {headPos}, L: {leftPos}, R: {rightPos}");
                Debug.Log($"[전송] {photonView.Owner.NickName}는 자신의 XR Origin 데이터만 전송합니다");
            }
            
            // 애니메이션 파라미터들 (안전하게 전송)
            if (syncAnimationParams && playerAnimator != null)
            {
                stream.SendNext(HasAnimatorParameter("Speed") ? playerAnimator.GetFloat("Speed") : 0f);
                stream.SendNext(HasAnimatorParameter("MotionSpeed") ? playerAnimator.GetFloat("MotionSpeed") : 0f);
                stream.SendNext(HasAnimatorParameter("Grounded") ? playerAnimator.GetBool("Grounded") : true);
            }
            else
            {
                stream.SendNext(0f); // Speed
                stream.SendNext(0f); // MotionSpeed
                stream.SendNext(true); // Grounded
            }
        }
        else
        {
            // 다른 클라이언트로부터 XR Origin 데이터를 수신 (변수에만 저장)
            
            // 플레이어 루트 위치/회전 수신
            networkRootPosition = (Vector3)stream.ReceiveNext();
            networkRootRotation = (Quaternion)stream.ReceiveNext();
            
            // 머리 위치/회전 수신
            networkHeadPosition = (Vector3)stream.ReceiveNext();
            networkHeadRotation = (Quaternion)stream.ReceiveNext();
            
            // 손 위치/회전 수신
            networkLeftHandPosition = (Vector3)stream.ReceiveNext();
            networkLeftHandRotation = (Quaternion)stream.ReceiveNext();
            networkRightHandPosition = (Vector3)stream.ReceiveNext();
            networkRightHandRotation = (Quaternion)stream.ReceiveNext();
            
            // 애니메이션 파라미터 수신
            networkSpeed = (float)stream.ReceiveNext();
            networkMotionSpeed = (float)stream.ReceiveNext();
            networkGrounded = (bool)stream.ReceiveNext();
            
            hasReceivedData = true;
            
            // 디버깅: 누구의 데이터를 받았는지 확인 (5초마다)
            if (Time.time % 5f < Time.deltaTime)
            {
                Debug.Log($"[수신] {photonView.Owner.NickName}의 XR Origin 데이터 수신됨 - Head: {networkHeadPosition}, L: {networkLeftHandPosition}, R: {networkRightHandPosition}");
                Debug.Log($"[수신] 현재 내 XR Origin 수신한 데이터는 Transform에 적용되지 않음 (변수에만 저장됨)");
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!photonView.IsMine)
        {
            // 원격 플레이어의 네트워크 동기화 상태 시각화
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(networkRootPosition, 0.5f);
            
            if (hasReceivedData)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(networkHeadPosition, 0.1f);
                
                if (!useVirtualHands)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(networkLeftHandPosition, 0.05f);
                    Gizmos.DrawWireSphere(networkRightHandPosition, 0.05f);
                }
            }
        }
    }
    
    /// <summary>
    /// 원격 플레이어의 VR 컨트롤러를 시각화하는 오브젝트를 생성합니다.
    /// </summary>
    void CreateControllerVisualizers()
    {
        // 왼쪽 컨트롤러 시각화 오브젝트 생성
        if (leftControllerVisualizer == null)
        {
            leftControllerVisualizer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftControllerVisualizer.name = $"{photonView.Owner.NickName}_LeftController";
            leftControllerVisualizer.transform.localScale = new Vector3(0.1f, 0.1f, 0.15f);
            
            // 머티리얼 설정 (빨간색)
            Renderer leftRenderer = leftControllerVisualizer.GetComponent<Renderer>();
            if (leftRenderer != null)
            {
                Material leftMat = new Material(Shader.Find("Standard"));
                leftMat.color = Color.red;
                leftMat.SetFloat("_Metallic", 0.5f);
                leftMat.SetFloat("_Smoothness", 0.8f);
                leftRenderer.material = leftMat;
            }
            
            // 콜라이더 제거 (시각화용이므로)
            Collider leftCollider = leftControllerVisualizer.GetComponent<Collider>();
            if (leftCollider != null) DestroyImmediate(leftCollider);
        }
        
        // 오른쪽 컨트롤러 시각화 오브젝트 생성
        if (rightControllerVisualizer == null)
        {
            rightControllerVisualizer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightControllerVisualizer.name = $"{photonView.Owner.NickName}_RightController";
            rightControllerVisualizer.transform.localScale = new Vector3(0.1f, 0.1f, 0.15f);
            
            // 머티리얼 설정 (파란색)
            Renderer rightRenderer = rightControllerVisualizer.GetComponent<Renderer>();
            if (rightRenderer != null)
            {
                Material rightMat = new Material(Shader.Find("Standard"));
                rightMat.color = Color.blue;
                rightMat.SetFloat("_Metallic", 0.5f);
                rightMat.SetFloat("_Smoothness", 0.8f);
                rightRenderer.material = rightMat;
            }
            
            // 콜라이더 제거 (시각화용이므로)
            Collider rightCollider = rightControllerVisualizer.GetComponent<Collider>();
            if (rightCollider != null) DestroyImmediate(rightCollider);
        }
        
        Debug.Log($"원격 플레이어 {photonView.Owner.NickName}의 VR 컨트롤러 시각화 오브젝트 생성 완료");
    }
    
    /// <summary>
    /// 컨트롤러 시각화 오브젝트들을 제거합니다.
    /// </summary>
    void DestroyControllerVisualizers()
    {
        if (leftControllerVisualizer != null)
        {
            DestroyImmediate(leftControllerVisualizer);
            leftControllerVisualizer = null;
        }
        
        if (rightControllerVisualizer != null)
        {
            DestroyImmediate(rightControllerVisualizer);
            rightControllerVisualizer = null;
        }
    }
    
    void OnDestroy()
    {
        // 오브젝트가 파괴될 때 시각화 오브젝트들도 정리
        DestroyControllerVisualizers();
    }
    
    #region Public Getters for Network Data
    
    /// <summary>
    /// 네트워크로부터 수신한 상대방의 XR Origin 데이터에 접근하기 위한 Getter들
    /// 실제 Transform에는 적용되지 않고 변수 값으로만 저장됨
    /// </summary>
    
    public bool HasReceivedNetworkData => hasReceivedData;
    public Vector3 NetworkRootPosition => networkRootPosition;
    public Quaternion NetworkRootRotation => networkRootRotation;
    public Vector3 NetworkHeadPosition => networkHeadPosition;
    public Quaternion NetworkHeadRotation => networkHeadRotation;
    public Vector3 NetworkLeftHandPosition => networkLeftHandPosition;
    public Quaternion NetworkLeftHandRotation => networkLeftHandRotation;
    public Vector3 NetworkRightHandPosition => networkRightHandPosition;
    public Quaternion NetworkRightHandRotation => networkRightHandRotation;
    public float NetworkSpeed => networkSpeed;
    public float NetworkMotionSpeed => networkMotionSpeed;
    public bool NetworkGrounded => networkGrounded;
    
    #endregion
} 