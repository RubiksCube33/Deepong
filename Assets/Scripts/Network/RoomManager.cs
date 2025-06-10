using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("플레이어 스폰 매니저")]
    public PlayerSpawnManager playerSpawnManager; // 플레이어 스폰 매니저 참조
    
    [Header("게임 설정")]
    public float gameStartDelay = 2f; // 게임 시작 전 대기 시간
    public bool autoStartGame = true; // 모든 플레이어가 입장하면 자동으로 게임 시작
    
    [Header("UI 요소")]
    public GameObject roomUI; // 방 UI
    
    private bool isGameStarted = false; // 게임 시작 여부
    
    void Awake()
    {
        // 씬 전환 시에도 이 매니저 유지
        DontDestroyOnLoad(this.gameObject);
        
        // PlayerSpawnManager 자동 찾기
        if (playerSpawnManager == null)
        {
            playerSpawnManager = FindObjectOfType<PlayerSpawnManager>();
        }
    }
    
    void Start()
    {
        // UI 초기화
        if (roomUI != null)
            roomUI.SetActive(false);
            
        // PlayerSpawnManager 확인
        if (playerSpawnManager == null)
        {
            Debug.LogError("RoomManager: PlayerSpawnManager를 찾을 수 없습니다!");
        }
    }
    
    // 방에 입장했을 때 호출
    public override void OnJoinedRoom()
    {
        Debug.Log("방에 입장했습니다: " + PhotonNetwork.CurrentRoom.Name);
        
        // 방 UI 활성화
        if (roomUI != null)
            roomUI.SetActive(true);
        
        // 플레이어가 모두 입장했는지 확인
        CheckAllPlayersJoined();
        
        // 플레이어 스폰은 PlayerSpawnManager에서 처리
        Debug.Log("플레이어 스폰은 PlayerSpawnManager에서 처리됩니다.");
    }
    
    // 다른 플레이어가 방에 입장했을 때 호출
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"플레이어가 입장했습니다: {newPlayer.NickName} (ID: {newPlayer.ActorNumber})");
        
        // 플레이어가 모두 입장했는지 확인
        CheckAllPlayersJoined();
    }
    
    // 모든 플레이어가 입장했는지 확인
    private void CheckAllPlayersJoined()
    {
        if (!PhotonNetwork.IsMasterClient || !autoStartGame || isGameStarted)
            return;
            
        Room room = PhotonNetwork.CurrentRoom;
        if (room.PlayerCount == room.MaxPlayers)
        {
            // 모든 플레이어가 입장했으므로 게임 시작
            Debug.Log("모든 플레이어가 입장했습니다. 게임을 시작합니다.");
            StartGame();
        }
    }
    
    // 게임 시작
    private void StartGame()
    {
        if (isGameStarted) return;
        isGameStarted = true;
        
        Debug.Log("게임이 시작되었습니다!");
        
        // 게임 시작 관련 추가 로직을 여기에 구현
        // 예: 게임 타이머 시작, UI 업데이트 등
    }
    
    // 방 나가기
    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("방을 나갑니다...");
            PhotonNetwork.LeaveRoom();
        }
    }
    
    // 방을 나갔을 때 호출
    public override void OnLeftRoom()
    {
        Debug.Log("방을 나갔습니다.");
        
        // UI 비활성화
        if (roomUI != null)
            roomUI.SetActive(false);
            
        // 게임 상태 초기화
        isGameStarted = false;
    }
    
    // 플레이어가 방을 나갔을 때 호출
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"플레이어가 나갔습니다: {otherPlayer.NickName} (ID: {otherPlayer.ActorNumber})");
        
        // 게임 중이었다면 일시정지 또는 종료 처리
        if (isGameStarted)
        {
            Debug.Log("게임 중 플레이어가 나갔습니다. 게임을 일시정지합니다.");
            // 게임 일시정지 로직 구현
        }
    }
    
    // 방장이 바뀌었을 때 호출
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"방장이 바뀌었습니다: {newMasterClient.NickName}");
        
        // 새로운 방장이 게임 상태를 관리하도록 설정
        if (PhotonNetwork.IsMasterClient && !isGameStarted)
        {
            CheckAllPlayersJoined();
        }
    }
}