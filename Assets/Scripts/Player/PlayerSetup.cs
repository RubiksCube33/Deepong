using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 네트워크 멀티플레이어에서 플레이어의 기본 설정을 담당합니다.
/// PlayerNetworkSync와 함께 사용되어 플레이어의 초기 위치와 설정을 관리합니다.
/// Robot 에셋(팔다리 없음)과 휴머노이드 모델 모두 지원
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerSetup : MonoBehaviourPunCallbacks
{
    [Header("플레이어 설정")]
    public bool isPlayerOne = false; // true면 player1, false면 player2
    
    [Header("스폰 위치 (NetworkPlayerManager에서 관리)")]
    public Vector3 player1Position = new Vector3(-1.31f, 1f, -5.81f); // 게임매니저와 동일한 위치
    public Vector3 player2Position = new Vector3(-0.98f, 1f, 10.207f);  // 게임매니저와 동일한 위치
    
    [Header("VR 설정")]
    [SerializeField] private bool isVRPlayer = true; // VR 플레이어인지 여부
    [SerializeField] private GameObject vrComponents; // VR 관련 컴포넌트들의 부모 오브젝트
    
    [Header("Robot Mode 설정")]
    [SerializeField] private bool isRobotMode = false; // Robot 모드 여부 (자동 감지)

    private Rigidbody rb;
    private PlayerNetworkSync networkSync;
    private VRHumanoidController vrController;
    private Animator playerAnimator;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkSync = GetComponent<PlayerNetworkSync>();
        vrController = GetComponent<VRHumanoidController>();
        playerAnimator = GetComponent<Animator>();
        
        // Robot 모드 자동 감지
        DetectRobotMode();
    }
    
    /// <summary>
    /// Robot 모드 자동 감지
    /// </summary>
    private void DetectRobotMode()
    {
        // VRHumanoidController에서 Robot 모드 확인
        if (vrController != null)
        {
            isRobotMode = vrController.IsRobotMode;
        }
        else
        {
            // VRHumanoidController가 없는 경우, Animator로 판단
            isRobotMode = playerAnimator == null || !playerAnimator.isHuman;
        }
        
        Debug.Log($"PlayerSetup - Robot 모드 감지됨: {isRobotMode}");
    }

    void Start()
    {
        // 네트워크 플레이어 설정
        SetupNetworkPlayer();
        
        // 게임 시작 시 플레이어를 자기 위치로 이동 (내 플레이어인 경우에만)
        if (photonView.IsMine)
        {
            SetInitialPosition();
        }
    }

    void SetupNetworkPlayer()
    {
        // 네트워크에 연결된 경우
        if (PhotonNetwork.IsConnected)
        {
            // 액터 번호에 따라 플레이어 결정 (1번이 player1, 2번이 player2)
            isPlayerOne = photonView.Owner.ActorNumber == 1;
            
            // 내 플레이어가 아닌 경우 입력 관련 컴포넌트들 비활성화
            if (!photonView.IsMine)
            {
                SetupRemotePlayer();
            }
            else
            {
                SetupLocalPlayer();
            }
        }
    }
    
    void SetupLocalPlayer()
    {
        // 로컬 플레이어 설정
        Debug.Log($"로컬 플레이어 설정: {gameObject.name} (Actor: {photonView.Owner.ActorNumber}) - Robot 모드: {isRobotMode}");
        
        // VR 컴포넌트들이 활성화되어 있는지 확인
        if (isVRPlayer && vrController != null)
        {
            vrController.enabled = true;
            
            // Robot 모드 설정 동기화
            if (isRobotMode)
            {
                vrController.IsRobotMode = true;
                Debug.Log("VRHumanoidController가 Robot 모드로 설정되었습니다.");
            }
        }
        
        // 네트워크 동기화 컴포넌트 설정
        if (networkSync == null)
        {
            networkSync = gameObject.AddComponent<PlayerNetworkSync>();
        }
        
        // PlayerAnimationSync 컴포넌트 확인 및 설정
        PlayerAnimationSync animSync = GetComponent<PlayerAnimationSync>();
        if (animSync == null && playerAnimator != null)
        {
            animSync = gameObject.AddComponent<PlayerAnimationSync>();
        }
    }
    
    void SetupRemotePlayer()
    {
        // 원격 플레이어 설정
        Debug.Log($"원격 플레이어 설정: {gameObject.name} (Actor: {photonView.Owner.ActorNumber}) - Robot 모드: {isRobotMode}");
        
        // VR 입력 관련 컴포넌트들 비활성화 (Robot 모드에서도 동일)
        if (vrController != null)
        {
            // VR 컨트롤러는 비활성화하지만 Robot 모드 설정은 유지
            vrController.enabled = false;
            
            if (isRobotMode)
            {
                Debug.Log("원격 플레이어 - Robot 모드가 감지되어 VR 입력만 비활성화됩니다.");
            }
        }
        
        // XR 관련 컴포넌트 비활성화
        XRPlayerArmatureSetup xrSetup = GetComponent<XRPlayerArmatureSetup>();
        if (xrSetup != null)
        {
            xrSetup.enabled = false;
        }
        
        // 카메라와 오디오 리스너 비활성화
        Camera[] cameras = GetComponentsInChildren<Camera>();
        foreach (Camera cam in cameras)
        {
            cam.enabled = false;
        }
        
        AudioListener[] listeners = GetComponentsInChildren<AudioListener>();
        foreach (AudioListener listener in listeners)
        {
            listener.enabled = false;
        }
        
        // VR 컴포넌트 오브젝트가 따로 있다면 비활성화
        if (vrComponents != null)
        {
            vrComponents.SetActive(false);
        }
        
        // Robot 모드인 경우 특별한 처리는 필요 없음 (시각화는 자동으로 비활성화됨)
    }
    
    void SetInitialPosition()
    {
        // 네트워크 환경에서는 기존 매니저들이 위치를 관리하므로 여기서는 처리하지 않음
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log($"네트워크 환경에서는 기존 매니저가 플레이어 위치를 관리합니다: {gameObject.name}");
            return;
        }
        
        // 로컬 환경에서만 위치 설정
        Vector3 targetPosition;
        
        if (isPlayerOne)
        {
            targetPosition = player1Position;
            Debug.Log("Player 1이 왼쪽 위치에 배치되었습니다.");
        }
        else
        {
            targetPosition = player2Position;
            Debug.Log("Player 2가 오른쪽 위치에 배치되었습니다.");
        }
        
        // 위치 설정
        transform.position = targetPosition;
        
        // 위치 이동 후 물리 시뮬레이션 안정화
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    // 네트워크에서 플레이어가 참가했을 때 호출
    public override void OnJoinedRoom()
    {
        if (photonView.IsMine)
        {
            SetInitialPosition();
        }
    }
    
    /// <summary>
    /// 플레이어 정보를 반환합니다.
    /// </summary>
    public string GetPlayerInfo()
    {
        string modeInfo = isRobotMode ? "Robot" : "Humanoid";
        return $"Player {photonView.Owner.ActorNumber} ({photonView.Owner.NickName}) - " +
               $"{(isPlayerOne ? "Player1" : "Player2")} - " +
               $"{(photonView.IsMine ? "Local" : "Remote")} - " +
               $"Mode: {modeInfo} - " +
               $"Position: {transform.position}";
    }
    
    /// <summary>
    /// 디버깅용 - 플레이어 정보 출력
    /// </summary>
    [ContextMenu("Print Player Info")]
    public void PrintPlayerInfo()
    {
        Debug.Log(GetPlayerInfo());
    }
}