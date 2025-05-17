using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;

public class Launcher : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject connectingInfoPanel;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private InputField roomNameInput;
    [SerializeField] private Transform roomListContent; // 방 목록을 표시할 부모 오브젝트
    [SerializeField] private GameObject roomListItemPrefab; // 방 목록 항목 프리팹
    [SerializeField] private Text debugText; // 디버그 정보를 표시할 텍스트
    
    // 에디터 테스트용 설정
    [Header("에디터 테스트용 설정")]
    [SerializeField] private bool autoCreateRoomOnStart = true;
    [SerializeField] private string testRoomName = "TestRoom";
    [SerializeField] private KeyCode createRoomKey = KeyCode.F1;
    [SerializeField] private KeyCode joinRoomKey = KeyCode.F2;
    
    // 룸 설정을 위한 기본값
    private const string GAME_VERSION = "1.0";
    private const int MAX_PLAYERS = 2;
    
    // 방 목록 캐시
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();
    
    // 마지막 로그 메시지 (콘솔 출력용)
    private string lastLogMessage;
    
    void Start()
    {
        // 게임 시작 시 Photon 서버에 연결
        ConnectToPhoton();
        
        // 버튼 이벤트 설정
        if (createRoomButton) createRoomButton.onClick.AddListener(CreateRoom);
        if (joinRoomButton) joinRoomButton.onClick.AddListener(JoinRoom);
        
        // 디버그 텍스트 초기화
        UpdateDebugText("시작 중...");
    }
    
    private void Update()
    {
        // 현재 연결 상태를 계속 업데이트
        if (debugText)
        {
            string connectionStatus = "";
            if (PhotonNetwork.IsConnected)
            {
                connectionStatus += $"연결됨: {PhotonNetwork.CloudRegion} 지역, 핑: {PhotonNetwork.GetPing()}ms\n";
                connectionStatus += $"방 개수: {cachedRoomList.Count}\n";
                
                if (PhotonNetwork.InRoom)
                {
                    connectionStatus += $"현재 방: {PhotonNetwork.CurrentRoom.Name}\n";
                    connectionStatus += $"플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}\n";
                    connectionStatus += $"방장인가요?: {PhotonNetwork.IsMasterClient}\n";
                }
            }
            else
            {
                connectionStatus = "연결되지 않음";
            }
            
            UpdateDebugText(connectionStatus);
        }
        
        // 에디터 테스트용 키 입력 처리
        #if UNITY_EDITOR
        if (Input.GetKeyDown(createRoomKey))
        {
            CreateTestRoom();
        }
        else if (Input.GetKeyDown(joinRoomKey))
        {
            JoinTestRoom();
        }
        #endif
    }
    
    private void UpdateDebugText(string message)
    {
        if (debugText)
        {
            debugText.text = message;
        }
        
        if (message != lastLogMessage)
        {
            Debug.Log("[Photon 상태] " + message);
            lastLogMessage = message;
        }
    }
    
    private void ConnectToPhoton()
    {
        // 패널이 있다면 활성화
        if (connectingInfoPanel) connectingInfoPanel.SetActive(true);
        
        // 이미 연결되어 있다면 다시 연결하지 않음
        if (PhotonNetwork.IsConnected) return;
        
        // 호스트 모드를 위한 설정
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = GAME_VERSION;
        
        // 닉네임 설정 (고유값이 필요)
        PhotonNetwork.NickName = "Player_" + Random.Range(1000, 10000);
        
        // Photon 서버에 연결
        PhotonNetwork.ConnectUsingSettings();
        UpdateDebugText($"Photon 서버에 연결 시도 중... (닉네임: {PhotonNetwork.NickName})");
    }
    
    // 방 생성 메서드
    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            UpdateDebugText("서버에 연결되지 않았습니다. 방 생성 불가");
            return;
        }
        
        // 방 옵션 설정 (호스트 모드)
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = MAX_PLAYERS,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true
        };
        
        string roomName = string.IsNullOrEmpty(roomNameInput?.text) ? "Room_" + Random.Range(1000, 10000) : roomNameInput.text;
        
        // 방 생성
        PhotonNetwork.CreateRoom(roomName, roomOptions);
        UpdateDebugText("방 생성 시도: " + roomName);
    }
    
    // 에디터 테스트용 방 생성 메서드
    public void CreateTestRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            UpdateDebugText("서버에 연결되지 않았습니다. 테스트 방 생성 불가");
            return;
        }
        
        if (PhotonNetwork.InRoom)
        {
            UpdateDebugText("이미 방에 있습니다. 테스트 방을 새로 생성하려면 먼저 방을 나가세요.");
            return;
        }
        
        // 방 옵션 설정 (테스트용)
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = MAX_PLAYERS,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true
        };
        
        // 테스트 방 생성
        PhotonNetwork.CreateRoom(testRoomName, roomOptions);
        UpdateDebugText($"테스트 방 생성 시도: {testRoomName} (키: {createRoomKey})");
    }
    
    // 에디터 테스트용 방 참가 메서드
    public void JoinTestRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            UpdateDebugText("서버에 연결되지 않았습니다. 테스트 방 참가 불가");
            return;
        }
        
        if (PhotonNetwork.InRoom)
        {
            UpdateDebugText("이미 방에 있습니다. 다른 테스트 방에 참가하려면 먼저 방을 나가세요.");
            return;
        }
        
        // 테스트 방 참가
        PhotonNetwork.JoinRoom(testRoomName);
        UpdateDebugText($"테스트 방 참가 시도: {testRoomName} (키: {joinRoomKey})");
    }
    
    // 방 참가 메서드
    public void JoinRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            UpdateDebugText("서버에 연결되지 않았습니다. 방 참가 불가");
            return;
        }
        
        string roomName = roomNameInput?.text;
        
        if (string.IsNullOrEmpty(roomName))
        {
            UpdateDebugText("참가할 방 이름이 비어있습니다!");
            return;
        }
        
        // 방 참가
        PhotonNetwork.JoinRoom(roomName);
        UpdateDebugText("방 참가 시도: " + roomName);
    }
    
    // 랜덤 방 참가
    public void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            UpdateDebugText("서버에 연결되지 않았습니다. 방 참가 불가");
            return;
        }
        
        // 랜덤 방 참가
        PhotonNetwork.JoinRandomRoom();
        UpdateDebugText("랜덤 방 참가 시도 중...");
    }
    
    // 방 목록 갱신 메서드
    private void UpdateRoomList()
    {
        // 방 목록 UI가 없으면 리턴
        if (roomListContent == null || roomListItemPrefab == null) return;
        
        // 기존 방 목록 UI 삭제
        foreach (GameObject item in roomListItems.Values)
        {
            Destroy(item);
        }
        roomListItems.Clear();
        
        // 새 방 목록 생성
        foreach (RoomInfo roomInfo in cachedRoomList.Values)
        {
            if (roomInfo.RemovedFromList || !roomInfo.IsVisible || !roomInfo.IsOpen)
                continue;
                
            GameObject roomItem = Instantiate(roomListItemPrefab, roomListContent);
            
            // 방 이름과 플레이어 수 설정 (Text 컴포넌트가 있다고 가정)
            Text[] texts = roomItem.GetComponentsInChildren<Text>();
            if (texts.Length > 0) texts[0].text = roomInfo.Name;
            if (texts.Length > 1) texts[1].text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
            
            // 방 참가 버튼 이벤트 설정
            Button roomButton = roomItem.GetComponentInChildren<Button>();
            if (roomButton)
            {
                string roomName = roomInfo.Name;
                roomButton.onClick.AddListener(() => 
                {
                    roomNameInput.text = roomName;
                    JoinRoom();
                });
            }
            
            roomListItems.Add(roomInfo.Name, roomItem);
        }
        
        UpdateDebugText($"방 목록 갱신 완료: {roomListItems.Count}개 방 표시됨");
    }
    
    // 마스터 서버에 연결되었을 때 호출되는 콜백
    public override void OnConnectedToMaster()
    {
        UpdateDebugText($"Photon 마스터 서버에 연결되었습니다. 닉네임: {PhotonNetwork.NickName}");
        
        // 로비에 참가
        PhotonNetwork.JoinLobby();
        UpdateDebugText("로비 참가 시도 중...");
    }
    
    // 로비 참가 성공 시 호출되는 콜백
    public override void OnJoinedLobby()
    {
        UpdateDebugText("로비 참가 성공!");
        
        // 패널이 있다면 비활성화
        if (connectingInfoPanel) connectingInfoPanel.SetActive(false);
        
        // 방 목록 초기화
        cachedRoomList.Clear();
        UpdateRoomList();
        
        // 에디터 테스트 모드인 경우 자동으로 방 생성 또는 참가
        #if UNITY_EDITOR
        if (autoCreateRoomOnStart)
        {
            // 이미 같은 이름의 방이 있는지 확인
            bool roomExists = false;
            foreach (RoomInfo roomInfo in cachedRoomList.Values)
            {
                if (roomInfo.Name == testRoomName && !roomInfo.RemovedFromList)
                {
                    roomExists = true;
                    break;
                }
            }
            
            if (roomExists)
            {
                // 방이 이미 존재하면 참가
                JoinTestRoom();
            }
            else
            {
                // 방이 없으면 생성
                CreateTestRoom();
            }
        }
        #endif
    }
    
    // 방 목록 업데이트 콜백
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateDebugText($"방 목록 업데이트됨: {roomList.Count}개 방");
        
        foreach (RoomInfo roomInfo in roomList)
        {
            // 방이 목록에서 제거된 경우
            if (roomInfo.RemovedFromList)
            {
                cachedRoomList.Remove(roomInfo.Name);
            }
            else // 방이 새로 생성되거나 업데이트된 경우
            {
                cachedRoomList[roomInfo.Name] = roomInfo;
            }
        }
        
        UpdateRoomList();
        
        // 에디터 테스트 모드인 경우 자동으로 방 참가 시도
        #if UNITY_EDITOR
        if (autoCreateRoomOnStart && !PhotonNetwork.InRoom)
        {
            foreach (RoomInfo roomInfo in roomList)
            {
                if (roomInfo.Name == testRoomName && !roomInfo.RemovedFromList)
                {
                    JoinTestRoom();
                    break;
                }
            }
        }
        #endif
    }
    
    // 방 생성 성공 시 호출되는 콜백
    public override void OnCreatedRoom()
    {
        UpdateDebugText("방 생성 성공!");
    }
    
    // 방 참가 성공 시 호출되는 콜백
    public override void OnJoinedRoom()
    {
        UpdateDebugText($"방 참가 성공! 방 이름: {PhotonNetwork.CurrentRoom.Name}, 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        
        // 방장(마스터 클라이언트)인 경우, 게임 시작 가능
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateDebugText("당신은 방장입니다. 게임을 시작할 수 있습니다.");
            // 게임 시작 로직 추가 (예: LoadLevel 메서드 호출)
        }
    }
    
    // 방 생성 실패 시 호출되는 콜백
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        UpdateDebugText($"방 생성 실패: {message} (코드: {returnCode})");
        
        // 테스트 중이고 방 생성 실패 시 (아마도 이미 방이 존재하는 경우)
        #if UNITY_EDITOR
        if (autoCreateRoomOnStart)
        {
            // 방이 이미 있다면 참가 시도
            JoinTestRoom();
        }
        #endif
    }
    
    // 방 참가 실패 시 호출되는 콜백
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        UpdateDebugText($"방 참가 실패: {message} (코드: {returnCode})");
        
        // 테스트 중이고 방 참가 실패 시 (아마도 방이 존재하지 않는 경우)
        #if UNITY_EDITOR
        if (autoCreateRoomOnStart)
        {
            // 방이 없다면 생성 시도
            CreateTestRoom();
        }
        #endif
    }
    
    // 랜덤 방 참가 실패 시 호출되는 콜백
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        UpdateDebugText($"랜덤 방 참가 실패: {message} (코드: {returnCode})");
        
        // 방이 없으면 새로 생성
        string roomName = "Room_" + Random.Range(1000, 10000);
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = MAX_PLAYERS };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
        UpdateDebugText("참가할 방이 없어 새로 생성: " + roomName);
    }
    
    // 다른 플레이어가 방에 참가했을 때 호출되는 콜백
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateDebugText($"플레이어 입장: {newPlayer.NickName}, 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }
    
    // 다른 플레이어가 방에서 나갔을 때 호출되는 콜백
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateDebugText($"플레이어 퇴장: {otherPlayer.NickName}, 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }
    
    // 연결 실패 시 호출되는 콜백
    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateDebugText($"Photon 서버와 연결이 끊어졌습니다. 이유: {cause}");
        
        // 패널이 있다면 비활성화
        if (connectingInfoPanel) connectingInfoPanel.SetActive(false);
    }
}