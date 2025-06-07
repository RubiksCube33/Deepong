using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 네트워크 멀티플레이어 환경에서 플레이어 네트워크 동기화 설정을 담당합니다.
/// 기존의 GameManager/CourtManager/RoomManager와 함께 사용됩니다.
/// </summary>
public class NetworkPlayerManager : MonoBehaviourPunCallbacks
{
    [Header("기존 매니저 참조")]
    [SerializeField] private GameManager gameManager; // 기존 GameManager 참조
    [SerializeField] private CourtManager courtManager; // 기존 CourtManager 참조
    [SerializeField] private RoomManager roomManager; // 기존 RoomManager 참조
    
    [Header("네트워크 설정")]
    [SerializeField] private bool useExistingManagers = true; // 기존 매니저들을 사용할지 여부
    [SerializeField] private bool enableNetworkSync = true; // 네트워크 동기화 활성화 여부
    
    [Header("로컬 카메라 설정")]
    [SerializeField] private string[] componentsToDisableForRemotePlayers = { "VRHumanoidController", "XRPlayerArmatureSetup" }; // 원격 플레이어에서 비활성화할 컴포넌트들
    
    private Dictionary<int, GameObject> networkPlayers = new Dictionary<int, GameObject>();

    void Start()
    {
        // 기존 매니저들 자동 찾기
        if (useExistingManagers)
        {
            FindExistingManagers();
        }
        
        // 네트워크가 연결되어 있다면 기존 플레이어들에게 네트워크 동기화 추가
        if (PhotonNetwork.IsConnected && enableNetworkSync)
        {
            SetupExistingPlayersForNetwork();
        }
    }
    
    void FindExistingManagers()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
                Debug.Log("GameManager를 자동으로 찾았습니다.");
        }
        
        if (courtManager == null)
        {
            courtManager = FindObjectOfType<CourtManager>();
            if (courtManager != null)
                Debug.Log("CourtManager를 자동으로 찾았습니다.");
        }
        
        if (roomManager == null)
        {
            roomManager = FindObjectOfType<RoomManager>();
            if (roomManager != null)
                Debug.Log("RoomManager를 자동으로 찾았습니다.");
        }
    }
    
    /// <summary>
    /// 기존 씬의 플레이어들에게 네트워크 동기화 컴포넌트를 추가합니다.
    /// </summary>
    void SetupExistingPlayersForNetwork()
    {
        // GameManager에서 관리하는 플레이어들 찾기
        GameObject[] existingPlayers = FindExistingPlayers();
        
        foreach (GameObject player in existingPlayers)
        {
            if (player != null)
            {
                AddNetworkSyncToPlayer(player);
            }
        }
    }
    
    /// <summary>
    /// 기존 씬에서 플레이어 오브젝트들을 찾습니다.
    /// </summary>
    GameObject[] FindExistingPlayers()
    {
        List<GameObject> players = new List<GameObject>();
        
        // GameManager에서 플레이어 찾기
        if (gameManager != null)
        {
            if (gameManager.player1Object != null)
                players.Add(gameManager.player1Object);
            if (gameManager.player2Object != null)
                players.Add(gameManager.player2Object);
        }
        
        // CourtManager에서 플레이어 찾기 (GameManager에서 못찾은 경우)
        if (players.Count == 0 && courtManager != null)
        {
            // CourtManager의 private 필드에 접근하기 위해 리플렉션 사용하거나
            // 혹은 플레이어 오브젝트를 태그나 이름으로 찾기
            GameObject[] playerCandidates = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject candidate in playerCandidates)
            {
                if (candidate.name.ToLower().Contains("player") && 
                    !candidate.name.ToLower().Contains("eye") &&
                    candidate.activeInHierarchy)
                {
                    players.Add(candidate);
                }
            }
        }
        
        // 일반적인 플레이어 이름 패턴으로 찾기
        if (players.Count == 0)
        {
            string[] playerNames = {"Player_Origin", "player1", "Player2", "PlayerArmature"};
            
            foreach (string playerName in playerNames)
            {
                GameObject found = GameObject.Find(playerName);
                if (found != null && !players.Contains(found))
                {
                    players.Add(found);
                }
            }
        }
        
        Debug.Log($"기존 플레이어 {players.Count}명을 찾았습니다.");
        return players.ToArray();
    }
    
    /// <summary>
    /// 기존 플레이어 오브젝트에 네트워크 동기화 컴포넌트를 추가합니다.
    /// </summary>
    void AddNetworkSyncToPlayer(GameObject player)
    {
        // PhotonView 컴포넌트 확인 및 추가
        PhotonView pv = player.GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = player.AddComponent<PhotonView>();
            pv.Synchronization = ViewSynchronization.UnreliableOnChange;
            pv.ObservedComponents = new List<Component>();
        }
        
        // PlayerNetworkSync 컴포넌트 확인 및 추가
        PlayerNetworkSync networkSync = player.GetComponent<PlayerNetworkSync>();
        if (networkSync == null)
        {
            networkSync = player.AddComponent<PlayerNetworkSync>();
        }
        
        // PlayerSetup 컴포넌트 확인 및 추가
        PlayerSetup playerSetup = player.GetComponent<PlayerSetup>();
        if (playerSetup == null)
        {
            playerSetup = player.AddComponent<PlayerSetup>();
        }
        
        // PlayerAnimationSync 컴포넌트 추가 (Animator가 있는 경우)
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            PlayerAnimationSync animSync = player.GetComponent<PlayerAnimationSync>();
            if (animSync == null)
            {
                animSync = player.AddComponent<PlayerAnimationSync>();
                pv.ObservedComponents.Add(animSync);
            }
        }
        
        // PhotonView에 네트워크 동기화 컴포넌트들 추가
        if (!pv.ObservedComponents.Contains(networkSync))
        {
            pv.ObservedComponents.Add(networkSync);
        }
        
        // 네트워크 플레이어 딕셔너리에 추가
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        networkPlayers[actorNumber] = player;
        
        Debug.Log($"플레이어 {player.name}에 네트워크 동기화 컴포넌트를 추가했습니다.");
    }
    
    /// <summary>
    /// 원격 플레이어의 설정을 진행합니다.
    /// </summary>
    void SetupRemotePlayer(GameObject player)
    {
        // 원격 플레이어에서는 입력 관련 컴포넌트들을 비활성화
        foreach (string componentName in componentsToDisableForRemotePlayers)
        {
            Component comp = player.GetComponent(componentName);
            if (comp != null)
            {
                if (comp is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                }
                Debug.Log($"원격 플레이어에서 {componentName} 컴포넌트를 비활성화했습니다.");
            }
        }
        
        // 원격 플레이어의 카메라들 비활성화
        Camera[] cameras = player.GetComponentsInChildren<Camera>();
        foreach (Camera cam in cameras)
        {
            cam.enabled = false;
            Debug.Log($"원격 플레이어의 카메라 {cam.name}을 비활성화했습니다.");
        }
        
        // 원격 플레이어에서 오디오 리스너 비활성화
        AudioListener[] listeners = player.GetComponentsInChildren<AudioListener>();
        foreach (AudioListener listener in listeners)
        {
            listener.enabled = false;
        }
        
        Debug.Log($"원격 플레이어 설정 완료: {player.name}");
    }

    #region Photon Callbacks
    
    public override void OnJoinedRoom()
    {
        Debug.Log("방에 입장했습니다. 기존 플레이어들에게 네트워크 동기화를 설정합니다.");
        
        // 기존 플레이어들에게 네트워크 동기화 컴포넌트 추가
        if (enableNetworkSync)
        {
            SetupExistingPlayersForNetwork();
        }
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("방에서 나갔습니다.");
        
        // 네트워크 플레이어 정리
        networkPlayers.Clear();
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"새로운 플레이어가 입장했습니다: {newPlayer.NickName}");
        
        // 기존 매니저들이 새 플레이어를 처리하도록 함
        // NetworkPlayerManager는 동기화만 담당
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"플레이어가 퇴장했습니다: {otherPlayer.NickName}");
        
        // 해당 플레이어의 오브젝트를 딕셔너리에서 제거
        if (networkPlayers.ContainsKey(otherPlayer.ActorNumber))
        {
            networkPlayers.Remove(otherPlayer.ActorNumber);
        }
    }
    
    #endregion

    /// <summary>
    /// 기존 플레이어들에게 네트워크 동기화를 수동으로 추가합니다.
    /// </summary>
    [ContextMenu("Setup Network Sync for Existing Players")]
    public void ManualSetupNetworkSync()
    {
        SetupExistingPlayersForNetwork();
    }
    
    /// <summary>
    /// 현재 네트워크 플레이어 정보를 출력합니다.
    /// </summary>
    [ContextMenu("Debug Network Players")]
    public void DebugNetworkPlayers()
    {
        Debug.Log($"=== NetworkPlayerManager Debug Info ===");
        Debug.Log($"현재 방에 있는 플레이어 수: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");
        Debug.Log($"네트워크 플레이어 수: {networkPlayers.Count}");
        Debug.Log($"사용 중인 매니저들:");
        Debug.Log($"  - GameManager: {(gameManager != null ? "활성" : "없음")}");
        Debug.Log($"  - CourtManager: {(courtManager != null ? "활성" : "없음")}");
        Debug.Log($"  - RoomManager: {(roomManager != null ? "활성" : "없음")}");
        
        foreach (var kvp in networkPlayers)
        {
            GameObject player = kvp.Value;
            PlayerNetworkSync sync = player.GetComponent<PlayerNetworkSync>();
            PlayerSetup setup = player.GetComponent<PlayerSetup>();
            
            Debug.Log($"ActorNumber {kvp.Key}: {player.name}");
            Debug.Log($"  - NetworkSync: {(sync != null ? "있음" : "없음")}");
            Debug.Log($"  - PlayerSetup: {(setup != null ? "있음" : "없음")}");
            Debug.Log($"  - PhotonView: {(player.GetComponent<PhotonView>() != null ? "있음" : "없음")}");
        }
    }
    
    /// <summary>
    /// 특정 플레이어 오브젝트에 수동으로 네트워크 동기화를 추가합니다.
    /// </summary>
    /// <param name="player">동기화를 추가할 플레이어 오브젝트</param>
    public void AddNetworkSyncToSpecificPlayer(GameObject player)
    {
        if (player != null)
        {
            AddNetworkSyncToPlayer(player);
        }
        else
        {
            Debug.LogError("플레이어 오브젝트가 null입니다!");
        }
    }
} 