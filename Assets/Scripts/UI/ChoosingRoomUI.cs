using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class ChoosingRoomUI : MonoBehaviourPunCallbacks
{
    [Header("방 목록 UI")]
    public GameObject roomListPanel;
    public Transform roomListContent;
    public GameObject roomItemPrefab;
    public ScrollRect roomScrollRect;
    
    [Header("상단 UI")]
    public Button backButton;
    public Button refreshButton; // 새로고침 버튼 추가
    
    [Header("비밀번호 입력 팝업")]
    public GameObject passwordPopup;
    public TMP_InputField passwordInputPopup;
    public Button confirmPasswordButton;
    public Button cancelPasswordButton;
    public TextMeshProUGUI passwordErrorText;
    
    [Header("상태/대기 UI")]
    public GameObject statusPanel;
    public TextMeshProUGUI statusText;
    
    private List<GameObject> roomItemObjects = new List<GameObject>();
    private RoomData selectedRoom = null;
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private bool isWaitingForPlayers = false;
    private RoomData currentJoinedRoom = null;
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        ConnectToPhotonAndJoinLobby();
    }
    
    private void InitializeUI()
    {
        if (passwordPopup != null)
            passwordPopup.SetActive(false);
            
        if (passwordErrorText != null)
            passwordErrorText.gameObject.SetActive(false);
            
        // 초기에는 로딩 상태만 표시
        if (statusPanel != null)
            statusPanel.SetActive(true);
            
        UpdateStatusText("서버에 연결 중...");
    }
    
    private void SetupEventListeners()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
            
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshRoomList);
            
        if (confirmPasswordButton != null)
            confirmPasswordButton.onClick.AddListener(OnConfirmPassword);
            
        if (cancelPasswordButton != null)
            cancelPasswordButton.onClick.AddListener(OnCancelPassword);
    }
    
    private void ConnectToPhotonAndJoinLobby()
    {
        // NetworkManager 확인 및 생성
        if (NetworkManager.Instance == null)
        {
            GameObject networkManagerGO = new GameObject("NetworkManager");
            networkManagerGO.AddComponent<NetworkManager>();
        }
        
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InLobby)
            {
                Debug.Log("이미 로비에 연결되어 있습니다.");
                UpdateStatusText("방 목록을 불러오는 중...");
                RefreshRoomList();
            }
            else if (PhotonNetwork.InRoom)
            {
                Debug.Log("현재 방에 있습니다. 방에서 나가서 로비로 이동합니다.");
                UpdateStatusText("방에서 나가는 중...");
                PhotonNetwork.LeaveRoom();
            }
            else
            {
                Debug.Log("로비에 참가합니다.");
                UpdateStatusText("로비에 참가 중...");
                NetworkManager.Instance.JoinLobbyIfNeeded();
            }
        }
        else
        {
            Debug.Log("Photon 서버에 연결 중...");
            UpdateStatusText("서버에 연결 중...");
            NetworkManager.Instance.ConnectToPhoton();
        }
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon 마스터 서버에 연결되었습니다.");
        UpdateStatusText("로비에 참가 중...");
        PhotonNetwork.JoinLobby();
    }
    
    public override void OnJoinedLobby()
    {
        Debug.Log($"로비에 참가했습니다. 현재 로비: {PhotonNetwork.CurrentLobby}");
        UpdateStatusText("방 목록을 불러오는 중...");
        
        // 방 목록을 즉시 갱신 요청
        Debug.Log("방 목록 갱신 요청 중...");
        
        // 잠시 대기 후 방 목록 갱신 (Photon이 방 목록을 보내줄 시간을 줌)
        Invoke("DelayedRefresh", 0.5f);
    }
    
    private void DelayedRefresh()
    {
        Debug.Log($"지연된 새로고침 실행. 현재 캐시된 방 개수: {cachedRoomList.Count}");
        RefreshRoomList();
    }
    
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"방 목록 업데이트: {roomList.Count}개의 방 정보 수신");
        
        // 수신된 각 방의 정보를 로그로 출력
        foreach (var room in roomList)
        {
            if (room.RemovedFromList)
            {
                Debug.Log($"방 제거됨: {room.Name}");
            }
            else
            {
                Debug.Log($"방 정보: {room.Name} ({room.PlayerCount}/{room.MaxPlayers}) - Open: {room.IsOpen}");
            }
        }
        
        UpdateCachedRoomList(roomList);
        RefreshRoomList();
    }
    
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // 삭제된 방은 목록에서 제거
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                // 존재하는 방 정보 업데이트 또는 새 방 추가
                cachedRoomList[info.Name] = info;
            }
        }
    }
    
    public void RefreshRoomList()
    {
        ClearRoomList();
        
        if (cachedRoomList.Count == 0)
        {
            UpdateStatusText("사용 가능한 방이 없습니다.");
            // 방이 없어도 로딩 상태는 종료
            HideLoadingState();
            return;
        }
        
        // 방 목록 로딩 완료 - 로딩 상태 숨기기
        HideLoadingState();
        
        foreach (var room in cachedRoomList.Values)
        {
            // 닫혀있거나 꽉 찬 방은 표시하지 않음
            if (room.IsOpen && room.PlayerCount < room.MaxPlayers)
            {
                CreateRoomItem(room);
            }
        }
    }
    
    private void ClearRoomList()
    {
        foreach (GameObject item in roomItemObjects)
        {
            if (item != null)
                DestroyImmediate(item);
        }
        roomItemObjects.Clear();
    }
    
    private void CreateRoomItem(RoomInfo roomInfo)
    {
        if (roomItemPrefab == null || roomListContent == null) return;
        
        GameObject roomItem = Instantiate(roomItemPrefab, roomListContent);
        roomItemObjects.Add(roomItem);
        
        // 방 아이템 UI 설정
        RoomListItemUI roomItemUI = roomItem.GetComponent<RoomListItemUI>();
        if (roomItemUI != null)
        {
            // RoomInfo를 RoomData로 변환
            RoomData roomData = ConvertRoomInfoToRoomData(roomInfo);
            roomItemUI.SetupRoomItem(roomData, this);
        }
        else
        {
            // 기본 설정
            SetupBasicRoomItem(roomItem, roomInfo);
        }
    }
    
    private RoomData ConvertRoomInfoToRoomData(RoomInfo roomInfo)
    {
        string password = "";
        bool hasPassword = false;
        
        if (roomInfo.CustomProperties.ContainsKey("password"))
        {
            password = roomInfo.CustomProperties["password"].ToString();
        }
        
        if (roomInfo.CustomProperties.ContainsKey("hasPassword"))
        {
            hasPassword = (bool)roomInfo.CustomProperties["hasPassword"];
        }
        
        return new RoomData(roomInfo.Name, password, roomInfo.MaxPlayers)
        {
            currentPlayers = roomInfo.PlayerCount,
            hasPassword = hasPassword
        };
    }
    
    private void SetupBasicRoomItem(GameObject roomItem, RoomInfo roomInfo)
    {
        // 기본적인 방 아이템 설정
        TextMeshProUGUI[] texts = roomItem.GetComponentsInChildren<TextMeshProUGUI>();
        Button button = roomItem.GetComponent<Button>();
        
        if (texts.Length > 0)
        {
            bool hasPassword = roomInfo.CustomProperties.ContainsKey("hasPassword") && 
                              (bool)roomInfo.CustomProperties["hasPassword"];
            string statusIcon = hasPassword ? "🔒" : "🔓";
            string playerInfo = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
            texts[0].text = $"{statusIcon} {roomInfo.Name} ({playerInfo})";
        }
        
        if (button != null)
        {
            button.onClick.AddListener(() => OnRoomItemClicked(ConvertRoomInfoToRoomData(roomInfo)));
        }
    }
    
    public void OnRoomItemClicked(RoomData room)
    {
        selectedRoom = room;
        
        // 방 상태 확인
        if (room.currentPlayers >= room.maxPlayers)
        {
            // 방이 가득 참
            ShowRoomFullMessage();
            return;
        }
        
        // 비밀번호가 있는 방이면 비밀번호 입력 팝업 표시
        if (room.hasPassword)
        {
            ShowPasswordPopup();
            return;
        }
        
        // 비밀번호가 없는 방이면 바로 참가
        JoinRoom(room);
    }
    
    private void ShowPasswordPopup()
    {
        if (passwordPopup != null)
        {
            passwordPopup.SetActive(true);
            
            if (passwordInputPopup != null)
                passwordInputPopup.text = "";
                
            if (passwordErrorText != null)
                passwordErrorText.gameObject.SetActive(false);
        }
    }
    
    public void OnConfirmPassword()
    {
        if (selectedRoom == null || passwordInputPopup == null) return;
        
        string inputPassword = passwordInputPopup.text.Trim();
        
        if (string.IsNullOrEmpty(inputPassword))
        {
            ShowPasswordError("비밀번호를 입력해주세요!");
            return;
        }
        
        if (inputPassword == selectedRoom.password)
        {
            HidePasswordPopup();
            JoinRoom(selectedRoom);
        }
        else
        {
            ShowPasswordError("비밀번호가 틀렸습니다!");
        }
    }
    
    public void OnCancelPassword()
    {
        HidePasswordPopup();
    }
    
    public void HidePasswordPopup()
    {
        if (passwordPopup != null)
            passwordPopup.SetActive(false);
    }
    
    private void ShowPasswordError(string message)
    {
        if (passwordErrorText != null)
        {
            passwordErrorText.text = message;
            passwordErrorText.gameObject.SetActive(true);
        }
    }
    
    private void JoinRoom(RoomData room)
    {
        if (string.IsNullOrEmpty(room.roomName))
        {
            UpdateStatusText("방 이름이 유효하지 않습니다.");
            return;
        }
        
        if (statusPanel != null)
            statusPanel.SetActive(true);
            
        UpdateStatusText($"'{room.roomName}' 방에 참가 중...");
        
        Debug.Log($"Photon 방 참가 시도: {room.roomName}");
        PhotonNetwork.JoinRoom(room.roomName);
    }
    
    private RoomData GetCurrentRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            // Photon 방 정보를 RoomData로 변환
            RoomInfo roomInfo = PhotonNetwork.CurrentRoom;
            return ConvertRoomInfoToRoomData(roomInfo);
        }
        return currentJoinedRoom;
    }
    

    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Photon 연결이 끊어졌습니다: {cause}");
        UpdateStatusText($"서버 연결이 끊어졌습니다: {cause}");
    }
    
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[ChoosingRoomUI] {message}");
    }
    
    private void HideLoadingState()
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
            Debug.Log("로딩 상태 숨김 - 방 목록 표시 완료");
        }
    }
    
    public void OnBackClicked()
    {
        // Photon 방에서 나가기 (만약 방에 있다면)
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        
        SceneManager.LoadScene("WaitingRoomScene");
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제는 더 이상 필요 없음 (Photon 콜백 사용)
    }
    
    private void ShowWaitingState()
    {
        // 실제로 방에 입장한 상태에서만 대기 화면 표시
        if (PhotonNetwork.InRoom && statusPanel != null)
        {
            statusPanel.SetActive(true);
            UpdateStatusText("다른 플레이어를 기다리는 중...");
            
            // 대기 상태 시작
            StartWaitingForPlayers();
            Debug.Log("대기 상태 표시 - 방에 입장함");
        }
        else
        {
            Debug.Log("대기 상태 표시 안함 - 방에 입장하지 않음");
        }
    }
    
    public void HideWaitingState()
    {
        if (statusPanel != null)
            statusPanel.SetActive(false);
            
        // 대기 취소 처리
        StopWaitingForPlayers();
        
        // 방에서 나가기 (대기 취소 시)
        LeaveCurrentRoom();
    }
    
    private void StartWaitingForPlayers()
    {
        isWaitingForPlayers = true;
        
        // Photon 이벤트로 실시간 플레이어 수 확인
        if (PhotonNetwork.InRoom)
        {
            UpdateWaitingText();
            // 참고: OnJoinedRoom에서 이미 처리하므로 여기서는 중복 체크 안함
        }
        else
        {
            Debug.Log("플레이어를 기다리는 중...");
        }
    }
    
    private void StopWaitingForPlayers()
    {
        isWaitingForPlayers = false;
        // 대기 상태 취소 처리
    }
    
    private void LeaveCurrentRoom()
    {
        if (currentJoinedRoom != null)
        {
            if (currentJoinedRoom.isPhotonRoom)
            {
                // Photon 방에서 나가기
                if (PhotonNetwork.InRoom)
                {
                    Debug.Log($"Photon 방에서 나갑니다: {currentJoinedRoom.roomName}");
                    PhotonNetwork.LeaveRoom();
                }
            }
            else
            {
                // 로컬 방에서 나가기 (플레이어 수 감소)
                if (currentJoinedRoom.currentPlayers > 0)
                {
                    currentJoinedRoom.currentPlayers--;
                    Debug.Log($"로컬 방에서 나갑니다: {currentJoinedRoom.roomName} (남은 플레이어: {currentJoinedRoom.currentPlayers})");
                    
                    // 방 목록 업데이트
                    RefreshRoomList();
                }
            }
            
            currentJoinedRoom = null;
        }
    }
    
    private void UpdateWaitingText()
    {
        if (statusText != null && PhotonNetwork.InRoom)
        {
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            UpdateStatusText($"다른 플레이어를 기다리는 중...\n({currentPlayers}/{maxPlayers})");
        }
    }

    private void ShowRoomFullMessage()
    {
        Debug.Log("방이 가득 찼습니다!");
        ShowError("방이 가득 차서 입장할 수 없습니다!");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"방 참가 성공: {PhotonNetwork.CurrentRoom.Name} (현재 플레이어: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");
        
        // 플레이어 수에 따른 로직
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            // 2명이 되면 바로 게임 시작
            Debug.Log("2명이 모였습니다! 게임을 시작합니다.");
            HideWaitingState(); // 대기 패널 숨기기
            SceneManager.LoadScene("CourtScene");
        }
        else
        {
            // 1명이면 대기 패널 표시 (현재 화면에서 대기)
            Debug.Log("아직 1명뿐입니다. 다른 플레이어를 기다립니다.");
            ShowWaitingState(); // 대기 패널 표시
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        HideWaitingState();
        ShowError($"방 참가 실패: {message}");
        Debug.LogError($"방 참가 실패: {message} (코드: {returnCode})");
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"새 플레이어 입장: {newPlayer.NickName} (총 플레이어: {PhotonNetwork.CurrentRoom.PlayerCount})");
        
        // 2명이 되면 게임 시작 (대기실에서 실행되는 로직)
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            Debug.Log("2명이 모였습니다! 게임을 시작합니다.");
            SceneManager.LoadScene("CourtScene");
        }
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"플레이어 퇴장: {otherPlayer.NickName} (남은 플레이어: {PhotonNetwork.CurrentRoom.PlayerCount})");
        
        if (isWaitingForPlayers)
        {
            UpdateWaitingText();
        }
    }
    
    private void ShowError(string message)
    {
        if (passwordErrorText != null)
        {
            passwordErrorText.text = message;
            passwordErrorText.gameObject.SetActive(true);
        }
        Debug.LogError(message);
    }
}