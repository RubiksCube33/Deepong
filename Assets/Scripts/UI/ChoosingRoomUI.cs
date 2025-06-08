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
    
    [Header("비밀번호 입력 팝업")]
    public GameObject passwordPopup;
    public TMP_InputField passwordInputPopup;
    public Button confirmPasswordButton;
    public Button cancelPasswordButton;
    public TextMeshProUGUI passwordErrorText;
    
    [Header("대기 상태 UI")]
    public GameObject waitingOverlay;  // 반투명 패널
    public TextMeshProUGUI waitingText;  // "대기중..." 텍스트
    
    private List<GameObject> roomItemObjects = new List<GameObject>();
    private RoomData selectedRoom = null;
    private bool isWaitingForPlayers = false;
    private RoomData currentJoinedRoom = null; // 현재 입장한 방 추적
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        RefreshRoomList();
    }
    
    private void InitializeUI()
    {
        if (passwordPopup != null)
            passwordPopup.SetActive(false);
            
        if (passwordErrorText != null)
            passwordErrorText.gameObject.SetActive(false);
        
        if (waitingOverlay != null)
            waitingOverlay.SetActive(false);
    }
    
    private void SetupEventListeners()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
            
        if (confirmPasswordButton != null)
            confirmPasswordButton.onClick.AddListener(OnConfirmPassword);
            
        if (cancelPasswordButton != null)
            cancelPasswordButton.onClick.AddListener(OnCancelPassword);
            
        // RoomDataManager 이벤트 구독
        if (RoomDataManager.Instance != null)
            RoomDataManager.Instance.OnRoomListUpdated += RefreshRoomList;
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (RoomDataManager.Instance != null)
            RoomDataManager.Instance.OnRoomListUpdated -= RefreshRoomList;
    }
    
    public void RefreshRoomList()
    {
        ClearRoomList();
        
        if (RoomDataManager.Instance == null)
        {
            return;
        }
        
        List<RoomData> rooms = RoomDataManager.Instance.GetAllRooms();
        
        foreach (RoomData room in rooms)
        {
            CreateRoomItem(room);
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
    
    private void CreateRoomItem(RoomData room)
    {
        if (roomItemPrefab == null || roomListContent == null) return;
        
        GameObject roomItem = Instantiate(roomItemPrefab, roomListContent);
        roomItemObjects.Add(roomItem);
        
        // 방 아이템 UI 설정
        RoomListItemUI roomItemUI = roomItem.GetComponent<RoomListItemUI>();
        if (roomItemUI != null)
        {
            roomItemUI.SetupRoomItem(room, this);
        }
        else
        {
            // RoomListItemUI가 없으면 기본 설정
            SetupBasicRoomItem(roomItem, room);
        }
    }
    
    private void SetupBasicRoomItem(GameObject roomItem, RoomData room)
    {
        // 기본적인 방 아이템 설정 (RoomListItemUI 스크립트가 없을 경우)
        TextMeshProUGUI[] texts = roomItem.GetComponentsInChildren<TextMeshProUGUI>();
        Button button = roomItem.GetComponent<Button>();
        
        if (texts.Length > 0)
        {
            string statusIcon = room.hasPassword ? "🔒" : "🔓";
            string playerInfo = $"{room.currentPlayers}/{room.maxPlayers}";
            texts[0].text = $"{statusIcon} {room.roomName} ({playerInfo})";
        }
        
        if (button != null)
        {
            button.onClick.AddListener(() => OnRoomItemClicked(room));
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
            ShowPasswordError("Please enter password!");
            return;
        }
        
        if (inputPassword == selectedRoom.password)
        {
            HidePasswordPopup();
            JoinRoom(selectedRoom);
        }
        else
        {
            ShowPasswordError("Wrong password!");
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
        if (room.isPhotonRoom)
        {
            // Photon 온라인 방 참가
            PhotonNetwork.JoinRoom(room.photonRoomName);
            Debug.Log($"Photon 방 참가 시도: {room.roomName}");
            currentJoinedRoom = room; // 현재 입장한 방 기록
        }
        else
        {
            // 로컬 방인 경우 (테스트용)
            Debug.Log($"로컬 방 참가: {room.roomName} (현재 플레이어: {room.currentPlayers})");
            
            // 플레이어 수 증가
            room.currentPlayers++;
            currentJoinedRoom = room; // 현재 입장한 방 기록
            
            // 플레이어 수에 따른 처리
            if (room.currentPlayers >= room.maxPlayers)
            {
                // 2명이 되면 바로 게임 시작
                Debug.Log("2명이 모였습니다! 게임을 시작합니다.");
                SceneManager.LoadScene("CourtScene");
            }
            else
            {
                // 아직 1명뿐이면 대기 패널 표시
                Debug.Log("아직 1명뿐입니다. 다른 플레이어를 기다립니다.");
                ShowWaitingState();
            }
            
            // 방 목록 업데이트
            RefreshRoomList();
        }
    }
    
    public void OnBackClicked()
    {
        // 현재 방에서 나가기 처리
        LeaveCurrentRoom();
        SceneManager.LoadScene("WaitingRoomScene");
    }
    
    private void ShowWaitingState()
    {
        if (waitingOverlay != null)
        {
            waitingOverlay.SetActive(true);
            if (waitingText != null)
                waitingText.text = "...Waiting for other player...";
            
            // 대기 상태 시작
            StartWaitingForPlayers();
        }
    }
    
    public void HideWaitingState()
    {
        if (waitingOverlay != null)
            waitingOverlay.SetActive(false);
            
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
        if (waitingText != null && PhotonNetwork.InRoom)
        {
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            waitingText.text = $"...Waiting for other player...\n({currentPlayers}/{maxPlayers})";
        }
    }

    private void ShowRoomFullMessage()
    {
        Debug.Log("방이 가득 찼습니다!");
        ShowError("방이 가득 차서 입장할 수 없습니다!");
    }

    // Photon 콜백 메서드들 추가
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