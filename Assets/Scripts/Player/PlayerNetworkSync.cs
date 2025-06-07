using Photon.Pun;
using UnityEngine;

/// <summary>
/// PUN2를 사용하여 플레이어의 위치, 회전, 애니메이션을 동기화합니다.
/// VR 플레이어의 머리, 양손, 몸체의 움직임을 네트워크를 통해 동기화합니다.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerNetworkSync : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("동기화할 Transform들")]
    [SerializeField] private Transform playerRoot; // 플레이어 루트 (몸체)
    [SerializeField] private Transform headTransform; // 머리
    [SerializeField] private Transform leftHandTransform; // 왼손
    [SerializeField] private Transform rightHandTransform; // 오른손
    
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

    void Awake()
    {
        // 자동으로 Transform 참조들을 찾기 시도
        if (playerRoot == null)
            playerRoot = transform;
            
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
            
        // VRHumanoidController가 있다면 해당 컴포넌트에서 Transform 참조들을 가져오기
        VRHumanoidController vrController = GetComponent<VRHumanoidController>();
        if (vrController != null)
        {
            if (headTransform == null)
                headTransform = vrController.HumanoidHead;
            if (leftHandTransform == null)
                leftHandTransform = vrController.HumanoidLeftHand;
            if (rightHandTransform == null)
                rightHandTransform = vrController.HumanoidRightHand;
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
    
    void SyncAnimationParameters()
    {
        // 애니메이션 파라미터 동기화
        playerAnimator.SetFloat("Speed", networkSpeed);
        playerAnimator.SetFloat("MotionSpeed", networkMotionSpeed);
        playerAnimator.SetBool("Grounded", networkGrounded);
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
            
            // 왼손 위치/회전
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
            
            // 오른손 위치/회전
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
            
            // 애니메이션 파라미터들
            if (syncAnimationParams && playerAnimator != null)
            {
                stream.SendNext(playerAnimator.GetFloat("Speed"));
                stream.SendNext(playerAnimator.GetFloat("MotionSpeed"));
                stream.SendNext(playerAnimator.GetBool("Grounded"));
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
            // 다른 클라이언트로부터 플레이어 정보를 수신
            
            // 플레이어 루트 위치/회전
            networkRootPosition = (Vector3)stream.ReceiveNext();
            networkRootRotation = (Quaternion)stream.ReceiveNext();
            
            // 머리 위치/회전
            networkHeadPosition = (Vector3)stream.ReceiveNext();
            networkHeadRotation = (Quaternion)stream.ReceiveNext();
            
            // 왼손 위치/회전
            networkLeftHandPosition = (Vector3)stream.ReceiveNext();
            networkLeftHandRotation = (Quaternion)stream.ReceiveNext();
            
            // 오른손 위치/회전
            networkRightHandPosition = (Vector3)stream.ReceiveNext();
            networkRightHandRotation = (Quaternion)stream.ReceiveNext();
            
            // 애니메이션 파라미터들
            networkSpeed = (float)stream.ReceiveNext();
            networkMotionSpeed = (float)stream.ReceiveNext();
            networkGrounded = (bool)stream.ReceiveNext();
            
            hasReceivedData = true;
        }
    }
    
    // 디버깅용 기즈모
    void OnDrawGizmosSelected()
    {
        if (!photonView.IsMine && hasReceivedData)
        {
            // 네트워크 위치들을 시각화
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(networkRootPosition, 0.1f);
            
            Gizmos.color = Color.blue;
            if (networkHeadPosition != Vector3.zero)
                Gizmos.DrawWireSphere(networkHeadPosition, 0.05f);
                
            Gizmos.color = Color.green;
            if (networkLeftHandPosition != Vector3.zero)
                Gizmos.DrawWireSphere(networkLeftHandPosition, 0.03f);
            if (networkRightHandPosition != Vector3.zero)
                Gizmos.DrawWireSphere(networkRightHandPosition, 0.03f);
        }
    }
} 