using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 플레이어 오리진 할당 및 관리 클래스
/// 방장은 1P, 참가자는 2P를 할당받습니다.
/// 각 플레이어는 자신의 오리진만 제어하고, 다른 플레이어의 오리진은 네트워크로 동기화하여 표시합니다.
/// </summary>
public class PlayerOriginManager : MonoBehaviourPunCallbacks
{
    [Header("플레이어 오리진 참조")]
    [SerializeField] private GameObject player1Origin; // 1P_Player_Origin
    [SerializeField] private GameObject player2Origin; // 2P_Player_Origin
    
    [Header("자동 검색 설정")]
    [SerializeField] private bool autoFindOrigins = true; // 자동으로 오리진 검색
    
    [Header("디버그 설정")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 현재 로컬 플레이어가 제어하는 오리진
    private GameObject localPlayerOrigin;
    private GameObject remotePlayerOrigin;
    
    // 네트워크 동기화 컴포넌트들
    private PlayerNetworkSync localNetworkSync;
    private PlayerNetworkSync remoteNetworkSync;
    
    // 플레이어 할당 상태
    private bool isOriginAssigned = false;
    
    void Start()
    {
        // 네트워크 연결 상태 확인
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("PlayerOriginManager: 네트워크에 연결되지 않은 상태입니다. 로컬 모드에서는 GameManager가 플레이어를 관리합니다.");
            gameObject.SetActive(false);
            return;
        }
        
        // 오리진 자동 검색
        if (autoFindOrigins)
        {
            FindPlayerOrigins();
        }
        
        // 오리진 할당
        AssignPlayerOrigin();
    }
    
    /// <summary>
    /// 씬에서 플레이어 오리진들을 자동으로 찾습니다.
    /// </summary>
    void FindPlayerOrigins()
    {
        if (player1Origin == null)
        {
            GameObject found = GameObject.Find("1P_Player_Origin");
            if (found != null)
            {
                player1Origin = found;
                Debug.Log("PlayerOriginManager: 1P_Player_Origin을 자동으로 찾았습니다.");
            }
        }
        
        if (player2Origin == null)
        {
            GameObject found = GameObject.Find("2P_Player_Origin");
            if (found != null)
            {
                player2Origin = found;
                Debug.Log("PlayerOriginManager: 2P_Player_Origin을 자동으로 찾았습니다.");
            }
        }
        
        // 찾지 못한 경우 경고
        if (player1Origin == null || player2Origin == null)
        {
            Debug.LogError($"PlayerOriginManager: 플레이어 오리진을 찾을 수 없습니다! 1P: {(player1Origin != null ? "찾음" : "없음")}, 2P: {(player2Origin != null ? "찾음" : "없음")}");
        }
    }
    
    /// <summary>
    /// 플레이어 역할에 따라 오리진을 할당합니다.
    /// </summary>
    void AssignPlayerOrigin()
    {
        if (player1Origin == null || player2Origin == null)
        {
            Debug.LogError("PlayerOriginManager: 플레이어 오리진이 설정되지 않았습니다!");
            return;
        }
        
        // 방장인지 확인하여 오리진 할당
        if (PhotonNetwork.IsMasterClient)
        {
            // 방장 = 1P
            localPlayerOrigin = player1Origin;
            remotePlayerOrigin = player2Origin;
            
            if (showDebugInfo)
                Debug.Log($"PlayerOriginManager: 방장으로 1P_Player_Origin 할당됨 ({PhotonNetwork.LocalPlayer.NickName})");
        }
        else
        {
            // 참가자 = 2P
            localPlayerOrigin = player2Origin;
            remotePlayerOrigin = player1Origin;
            
            if (showDebugInfo)
                Debug.Log($"PlayerOriginManager: 참가자로 2P_Player_Origin 할당됨 ({PhotonNetwork.LocalPlayer.NickName})");
        }
        
        // 오리진 설정 적용
        SetupPlayerOrigins();
        isOriginAssigned = true;
    }
    
    /// <summary>
    /// 플레이어 오리진들을 설정합니다.
    /// </summary>
    void SetupPlayerOrigins()
    {
        // 로컬 플레이어 오리진 설정
        SetupLocalPlayerOrigin();
        
        // 원격 플레이어 오리진 설정
        SetupRemotePlayerOrigin();
    }
    
    /// <summary>
    /// 로컬 플레이어가 제어할 오리진을 설정합니다.
    /// </summary>
    void SetupLocalPlayerOrigin()
    {
        if (localPlayerOrigin == null) return;
        
        // XR Origin 컴포넌트들이 활성화되도록 설정
        localPlayerOrigin.SetActive(true);
        
        // XROriginController 컴포넌트 추가/설정
        XROriginController xrOriginController = localPlayerOrigin.GetComponent<XROriginController>();
        if (xrOriginController == null)
        {
            xrOriginController = localPlayerOrigin.AddComponent<XROriginController>();
        }
        
        // PlayerNetworkSync 컴포넌트 추가/설정
        localNetworkSync = localPlayerOrigin.GetComponent<PlayerNetworkSync>();
        if (localNetworkSync == null)
        {
            localNetworkSync = localPlayerOrigin.AddComponent<PlayerNetworkSync>();
        }
        
        // PhotonView 컴포넌트 추가/설정
        PhotonView photonView = localPlayerOrigin.GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = localPlayerOrigin.AddComponent<PhotonView>();
        }
        
        // PhotonView 설정 - PUN2 올바른 API 사용
        if (photonView.ViewID == 0)
        {
            // AllocateViewID를 PhotonView에 직접 적용
            PhotonNetwork.AllocateViewID(photonView);
        }
        
        // PUN2에서는 synchronization 속성이 제거됨 - 자동으로 처리됨
        photonView.ObservedComponents.Clear();
        photonView.ObservedComponents.Add(localNetworkSync);
        
        // 오너십 설정
        photonView.OwnershipTransfer = OwnershipOption.Takeover;
        
        // 로컬 플레이어이므로 입력 활성화
        xrOriginController.SetInputControl(true);
        
        if (showDebugInfo)
            Debug.Log($"PlayerOriginManager: 로컬 플레이어 오리진 설정 완료 ({localPlayerOrigin.name})");
    }
    
    /// <summary>
    /// 원격 플레이어의 오리진을 설정합니다.
    /// </summary>
    void SetupRemotePlayerOrigin()
    {
        if (remotePlayerOrigin == null) return;
        
        // 원격 플레이어 오리진도 활성화 (네트워크 동기화로 표시용)
        remotePlayerOrigin.SetActive(true);
        
        // XROriginController 컴포넌트 추가/설정
        XROriginController xrOriginController = remotePlayerOrigin.GetComponent<XROriginController>();
        if (xrOriginController == null)
        {
            xrOriginController = remotePlayerOrigin.AddComponent<XROriginController>();
        }
        
        // PlayerNetworkSync 컴포넌트 추가/설정
        remoteNetworkSync = remotePlayerOrigin.GetComponent<PlayerNetworkSync>();
        if (remoteNetworkSync == null)
        {
            remoteNetworkSync = remotePlayerOrigin.AddComponent<PlayerNetworkSync>();
        }
        
        // PhotonView 컴포넌트 추가/설정
        PhotonView photonView = remotePlayerOrigin.GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = remotePlayerOrigin.AddComponent<PhotonView>();
        }
        
        // 원격 플레이어용 설정 - 입력 비활성화
        xrOriginController.SetInputControl(false);
        
        // ViewID는 할당하지 않음 (원격 플레이어가 할당할 것)
        
        if (showDebugInfo)
            Debug.Log($"PlayerOriginManager: 원격 플레이어 오리진 설정 완료 ({remotePlayerOrigin.name})");
    }
    
    /// <summary>
    /// 방장이 변경되었을 때 오리진 재할당
    /// </summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"PlayerOriginManager: 방장이 변경되었습니다. 새 방장: {newMasterClient.NickName}");
        
        // 오리진 재할당
        if (isOriginAssigned)
        {
            AssignPlayerOrigin();
        }
    }
    
    /// <summary>
    /// 플레이어가 방에서 나갔을 때 처리
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"PlayerOriginManager: 플레이어 {otherPlayer.NickName}이 방에서 나갔습니다.");
        
        // 혼자 남은 경우 원격 오리진 비활성화
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            if (remotePlayerOrigin != null)
            {
                // 원격 오리진의 XR 관련 컴포넌트들만 비활성화 (오브젝트는 유지)
                DisableRemoteOriginInput();
            }
        }
    }
    
    /// <summary>
    /// 새 플레이어가 방에 들어왔을 때 처리
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"PlayerOriginManager: 새 플레이어 {newPlayer.NickName}이 방에 들어왔습니다.");
        
        // 원격 오리진 다시 활성화
        if (remotePlayerOrigin != null)
        {
            EnableRemoteOriginInput();
        }
    }
    
    /// <summary>
    /// 원격 오리진의 입력을 비활성화합니다.
    /// </summary>
    void DisableRemoteOriginInput()
    {
        if (remotePlayerOrigin == null) return;
        
        // XR Input 컴포넌트들 비활성화
        var characterController = remotePlayerOrigin.GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;
            
        // InputActionManager 컴포넌트 찾기 (타입 이름으로 검색)
        var components = remotePlayerOrigin.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp.GetType().Name.Contains("InputAction") || comp.GetType().Name.Contains("ActionManager"))
            {
                comp.enabled = false;
                break;
            }
        }
    }
    
    /// <summary>
    /// 원격 오리진의 입력을 활성화합니다.
    /// </summary>
    void EnableRemoteOriginInput()
    {
        if (remotePlayerOrigin == null) return;
        
        // XR Input 컴포넌트들 활성화 (단, 원격 플레이어가 제어)
        var characterController = remotePlayerOrigin.GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = true;
            
        // InputActionManager 컴포넌트 찾기 (타입 이름으로 검색)
        var components = remotePlayerOrigin.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp.GetType().Name.Contains("InputAction") || comp.GetType().Name.Contains("ActionManager"))
            {
                comp.enabled = true;
                break;
            }
        }
    }
    
    /// <summary>
    /// 현재 로컬 플레이어의 오리진을 반환합니다.
    /// </summary>
    public GameObject GetLocalPlayerOrigin()
    {
        return localPlayerOrigin;
    }
    
    /// <summary>
    /// 현재 원격 플레이어의 오리진을 반환합니다.
    /// </summary>
    public GameObject GetRemotePlayerOrigin()
    {
        return remotePlayerOrigin;
    }
    
    /// <summary>
    /// 오리진이 할당되었는지 확인합니다.
    /// </summary>
    public bool IsOriginAssigned()
    {
        return isOriginAssigned;
    }
    
    /// <summary>
    /// 에디터에서 오리진을 수동으로 찾는 메서드
    /// </summary>
    [ContextMenu("Find Player Origins")]
    void FindPlayerOriginsManually()
    {
        FindPlayerOrigins();
        Debug.Log("PlayerOriginManager: 수동으로 플레이어 오리진을 검색했습니다.");
    }
} 