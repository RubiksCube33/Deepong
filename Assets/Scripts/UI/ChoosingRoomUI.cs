using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class ChoosingRoomUI : MonoBehaviour
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
        else if (room.currentPlayers < 2)
        {
            // 2명 미만이면 대기
            ShowWaitingState();
            return;
        }
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
        Debug.Log($"방 참가: {room.roomName}");
        
        // 실제로는 여기서 네트워킹 처리 후 대기실로 이동
        // 지금은 UI만 구현하므로 WaitingRoomScene으로 이동
        SceneManager.LoadScene("WaitingRoomScene");
    }
    
    public void OnBackClicked()
    {
        SceneManager.LoadScene("WaitingRoomScene");
    }
    
    private void ShowWaitingState()
    {
        if (waitingOverlay != null)
        {
            waitingOverlay.SetActive(true);
            if (waitingText != null)
                waitingText.text = "...waiting for match...";
            
            // 실제로는 여기서 서버 폴링이나 이벤트 리스너 시작
            StartWaitingForPlayers();
        }
    }
    
    public void HideWaitingState()
    {
        if (waitingOverlay != null)
            waitingOverlay.SetActive(false);
            
        // 대기 취소 처리
        StopWaitingForPlayers();
    }
    
    private void StartWaitingForPlayers()
    {
        // 서버에서 방 상태 업데이트를 받을 때까지 대기
        // 실제 구현에서는 서버 이벤트 리스너나 폴링 시작
    }
    
    private void StopWaitingForPlayers()
    {
        // 대기 상태 취소 처리
    }
    
    // 서버에서 방이 가득 찼다는 알림을 받았을 때 호출
    public void OnRoomReadyToJoin(RoomData room)
    {
        HideWaitingState();
        
        if (room.hasPassword)
        {
            ShowPasswordPopup();
        }
        else
        {
            JoinRoom(room);
        }
    }

    private void ShowRoomFullMessage()
    {
        Debug.Log("방이 가득 찼습니다!");
        // TODO: 방 가득참 팝업 표시하거나 토스트 메시지
    }
}