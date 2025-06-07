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
    
    // VR 컨트롤러 참조 (Robot 모드용)
    private VRHumanoidController vrController;

    void Awake()
    {
        // 자동으로 Transform 참조들을 찾기 시도
        if (playerRoot == null)
            playerRoot = transform;
            
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
            
        // VRHumanoidController가 있다면 해당 컴포넌트에서 Transform 참조들을 가져오기
        vrController = GetComponent<VRHumanoidController>();
        if (vrController != null)
        {
            if (headTransform == null)
                headTransform = vrController.HumanoidHead;
            if (leftHandTransform == null)
                leftHandTransform = vrController.HumanoidLeftHand;
            if (rightHandTransform == null)
                rightHandTransform = vrController.HumanoidRightHand;
                
            // Robot 모드 자동 감지
            useVirtualHands = vrController.IsRobotMode;
        }
        
        // 초기값 설정
        InitializeNetworkValues();
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
        
        // Robot 모드인 경우 가상 손 위치 사용, 아니면 실제 Transform 사용
        if (useVirtualHands && vrController != null)
        {
            networkLeftHandPosition = vrController.VirtualLeftHandPosition;
            networkLeftHandRotation = vrController.VirtualLeftHandRotation;
            networkRightHandPosition = vrController.VirtualRightHandPosition;
            networkRightHandRotation = vrController.VirtualRightHandRotation;
        }
        else
        {
            if (leftHandTransform != null)
            {
                networkLeftHandPosition = leftHandTransform.position;
                networkLeftHandRotation = leftHandTransform.rotation;
            }
            
            if (rightHandTransform != null)
            {
                networkRightHandPosition = rightHandTransform.position;
                networkRightHandRotation = rightHandTransform.rotation;
            }
        }
    }

    void Update()
    {
        // 내가 소유한 플레이어가 아니고, 네트워크 데이터를 받은 경우에만 동기화
        if (!photonView.IsMine && hasReceivedData)
        {
            SyncTransforms();
            
            if (syncAnimationParams && playerAnimator != null)
            {
                SyncAnimationParameters();
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
        
        // Robot 모드가 아닌 경우에만 실제 손 Transform 동기화
        if (!useVirtualHands)
        {
            // 왼손 동기화
            if (leftHandTransform != null)
            {
                leftHandTransform.position = Vector3.Lerp(leftHandTransform.position, networkLeftHandPosition, 
                                                         deltaTime * positionLerpRate);
                leftHandTransform.rotation = Quaternion.Lerp(leftHandTransform.rotation, networkLeftHandRotation, 
                                                            deltaTime * rotationLerpRate);
            }
            
            // 오른손 동기화
            if (rightHandTransform != null)
            {
                rightHandTransform.position = Vector3.Lerp(rightHandTransform.position, networkRightHandPosition, 
                                                          deltaTime * positionLerpRate);
                rightHandTransform.rotation = Quaternion.Lerp(rightHandTransform.rotation, networkRightHandRotation, 
                                                             deltaTime * rotationLerpRate);
            }
        }
        // Robot 모드인 경우 VRHumanoidController의 가상 손 위치는 자동으로 업데이트됨
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
            
            // 손 위치/회전 (Robot 모드 고려)
            if (useVirtualHands && vrController != null)
            {
                // Robot 모드: 가상 손 위치 전송
                stream.SendNext(vrController.VirtualLeftHandPosition);
                stream.SendNext(vrController.VirtualLeftHandRotation);
                stream.SendNext(vrController.VirtualRightHandPosition);
                stream.SendNext(vrController.VirtualRightHandRotation);
            }
            else
            {
                // 일반 모드: 실제 손 Transform 전송
                if (leftHandTransform != null)
                {
                    stream.SendNext(leftHandTransform.position);
                    stream.SendNext(leftHandTransform.rotation);
                }
                else
                {
                    stream.SendNext(Vector3.zero);
                    stream.SendNext(Quaternion.identity);
                }
                
                if (rightHandTransform != null)
                {
                    stream.SendNext(rightHandTransform.position);
                    stream.SendNext(rightHandTransform.rotation);
                }
                else
                {
                    stream.SendNext(Vector3.zero);
                    stream.SendNext(Quaternion.identity);
                }
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
            // 다른 클라이언트로부터 애니메이션 정보를 수신
            
            // 플레이어 루트 위치/회전 수신
            networkRootPosition = (Vector3)stream.ReceiveNext();
            networkRootRotation = (Quaternion)stream.ReceiveNext();
            
            // 머리 위치/회전 수신
            networkHeadPosition = (Vector3)stream.ReceiveNext();
            networkHeadRotation = (Quaternion)stream.ReceiveNext();
            
            // 손 위치/회전 수신 (Robot 모드와 상관없이 항상 수신)
            networkLeftHandPosition = (Vector3)stream.ReceiveNext();
            networkLeftHandRotation = (Quaternion)stream.ReceiveNext();
            networkRightHandPosition = (Vector3)stream.ReceiveNext();
            networkRightHandRotation = (Quaternion)stream.ReceiveNext();
            
            // 애니메이션 파라미터 수신
            networkSpeed = (float)stream.ReceiveNext();
            networkMotionSpeed = (float)stream.ReceiveNext();
            networkGrounded = (bool)stream.ReceiveNext();
            
            hasReceivedData = true;
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
} 