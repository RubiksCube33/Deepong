using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 네트워크 멀티플레이어에서 플레이어의 기본 설정을 담당합니다.
/// PlayerSpawnManager와 함께 사용되어 로컬/원격 플레이어를 구분하여 설정합니다.
/// Robot 에셋(팔다리 없음)과 휴머노이드 모델 모두 지원
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerSetup : MonoBehaviourPunCallbacks
{
    [Header("플레이어 타입")]
    public bool isLocalPlayer = false; // 로컬 플레이어 여부 (PlayerSpawnManager에서 설정)
    public bool isPlayerOne = false; // true면 player1, false면 player2
    
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
    }

    void SetupNetworkPlayer()
    {
        // 네트워크에 연결된 경우
        if (PhotonNetwork.IsConnected)
        {
            // PhotonView.IsMine으로 로컬/원격 플레이어 구분
            isLocalPlayer = photonView.IsMine;
            
            // 액터 번호에 따라 플레이어 위치 결정 (1번이 player1, 2번이 player2)
            isPlayerOne = photonView.Owner.ActorNumber == 1;
            
            // 로컬 플레이어와 원격 플레이어 설정
            if (isLocalPlayer)
            {
                SetupLocalPlayer();
            }
            else
            {
                SetupRemotePlayer();
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
        
        // XR Rig 활성화
        EnableXRRig(true);
        
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
        
        Debug.Log("로컬 플레이어 설정 완료 - 모든 입력 및 XR 컴포넌트 활성화");
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
        
        // XR Rig 비활성화
        EnableXRRig(false);
        
        // 카메라와 오디오 리스너 비활성화
        DisableCameraAndAudio();
        
        // VR 컴포넌트 오브젝트가 따로 있다면 비활성화
        if (vrComponents != null)
        {
            vrComponents.SetActive(false);
        }
        
        // 입력 관련 컴포넌트들 비활성화
        DisableInputComponents();
        
        Debug.Log("원격 플레이어 설정 완료 - 모든 입력 및 XR 컴포넌트 비활성화");
    }
    
    /// <summary>
    /// XR Rig 활성화/비활성화
    /// </summary>
    private void EnableXRRig(bool enable)
    {
        // XR Origin 또는 XR Rig 찾기
        Transform xrOrigin = transform.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
            xrOrigin = transform.Find("XR Rig");
        if (xrOrigin == null)
            xrOrigin = transform.Find("XR Origin");
        
        if (xrOrigin != null)
        {
            xrOrigin.gameObject.SetActive(enable);
            Debug.Log($"XR Rig {(enable ? "활성화" : "비활성화")}: {xrOrigin.name}");
        }
        else
        {
            Debug.LogWarning("XR Rig를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 카메라와 오디오 리스너 비활성화
    /// </summary>
    private void DisableCameraAndAudio()
    {
        // 카메라 비활성화
        Camera[] cameras = GetComponentsInChildren<Camera>();
        foreach (Camera cam in cameras)
        {
            cam.enabled = false;
        }
        
        // 오디오 리스너 비활성화
        AudioListener[] listeners = GetComponentsInChildren<AudioListener>();
        foreach (AudioListener listener in listeners)
        {
            listener.enabled = false;
        }
    }
    
    /// <summary>
    /// 입력 관련 컴포넌트들 비활성화
    /// </summary>
    private void DisableInputComponents()
    {
        // 기타 입력 관련 컴포넌트들 비활성화
        MonoBehaviour[] inputComponents = GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour component in inputComponents)
        {
            if (component == this) continue; // 자기 자신은 제외
            
            string componentName = component.GetType().Name;
            if (componentName.Contains("Input") || 
                componentName.Contains("Controller") ||
                componentName.Contains("XR") ||
                componentName.Contains("Interaction"))
            {
                component.enabled = false;
                Debug.Log($"입력 컴포넌트 비활성화: {componentName}");
            }
        }
    }
    
    // 네트워크에서 플레이어가 참가했을 때 호출
    public override void OnJoinedRoom()
    {
        // PlayerSpawnManager에서 위치를 관리하므로 여기서는 처리하지 않음
        Debug.Log($"OnJoinedRoom 호출됨: {gameObject.name} - 위치는 PlayerSpawnManager에서 관리");
    }
    
    /// <summary>
    /// 플레이어 정보를 반환합니다.
    /// </summary>
    public string GetPlayerInfo()
    {
        string modeInfo = isRobotMode ? "Robot" : "Humanoid";
        string playerType = isLocalPlayer ? "Local" : "Remote";
        string playerPosition = isPlayerOne ? "Player1" : "Player2";
        
        return $"Player {photonView.Owner.ActorNumber} ({photonView.Owner.NickName}) - " +
               $"{playerPosition} - " +
               $"{playerType} - " +
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
    
    /// <summary>
    /// 외부에서 로컬 플레이어 여부를 설정할 때 사용 (PlayerSpawnManager에서 호출)
    /// </summary>
    public void SetAsLocalPlayer(bool isLocal)
    {
        isLocalPlayer = isLocal;
        
        if (isLocal)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }
    }
}