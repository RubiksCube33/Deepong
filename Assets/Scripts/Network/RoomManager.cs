using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("플레이어 설정")]
    public GameObject playerPrefab; // 플레이어 프리팹
    public Transform[] spawnPoints; // 플레이어 스폰 위치
    
    [Header("게임 설정")]
    public float gameStartDelay = 2f; // 게임 시작 전 대기 시간
    public bool autoStartGame = true; // 모든 플레이어가 입장하면 자동으로 게임 시작
    
    [Header("UI 요소")]
    public GameObject roomUI; // 방 UI
    
    private GameObject localPlayerInstance; // 로컬 플레이어 인스턴스
    private bool isGameStarted = false; // 게임 시작 여부
    
    void Awake()
    {
        // 씬 전환 시에도 이 매니저 유지
        DontDestroyOnLoad(this.gameObject);
    }
    
    void Start()
    {
        // UI 초기화
        if (roomUI != null)
            roomUI.SetActive(false);
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
        
        // 플레이어 인스턴스 생성
        InstantiateLocalPlayer();
    }
    
    // 다른 플레이어가 방에 입장했을 때 호출
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"플레이어가 입장했습니다: {newPlayer.NickName} (ID: {newPlayer.ActorNumber})");
        
        // 플레이어가 모두 입장했는지 확인
        CheckAllPlayersJoined();
    }
    
    // 로컬 플레이어 인스턴스 생성
    private void InstantiateLocalPlayer()
    {
        // 이미 인스턴스가 있으면 생성하지 않음
        if (localPlayerInstance != null) return;
        
        // 스폰 포인트 결정
        Transform spawnPoint = GetSpawnPoint();
        
        // 플레이어 프리팹이 없으면 Resources 폴더에서 로드
        if (playerPrefab == null)
        {
            playerPrefab = Resources.Load<GameObject>("PlayerPrefab");
            
            if (playerPrefab == null)
            {
                Debug.LogError("플레이어 프리팹을 찾을 수 없습니다!");
                return;
            }
        }
        
        // 플레이어 인스턴스 생성
        localPlayerInstance = PhotonNetwork.Instantiate(
            playerPrefab.name, 
            spawnPoint.position, 
            spawnPoint.rotation);
        
        Debug.Log("로컬 플레이어 인스턴스 생성: " + localPlayerInstance.name);
    }
    
    // 플레이어 스폰 포인트 결정
    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // 스폰 포인트가 없으면 게임 오브젝트 위치 사용
            Debug.LogWarning("스폰 포인트가 설정되지 않았습니다. 기본 위치를 사용합니다.");
            return transform;
        }
        
        // 플레이어 번호에 따라 스폰 포인트 할당
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        playerIndex = Mathf.Clamp(playerIndex, 0, spawnPoints.Length - 1);
        
        return spawnPoints[playerIndex];
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
}