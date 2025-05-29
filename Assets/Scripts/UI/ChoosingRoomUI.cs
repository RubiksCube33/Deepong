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
        
        // 바로 방 입장 처리
        if (room.hasPassword)
        {
            ShowPasswordPopup();
        }
        else
        {
            JoinRoom(room);
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
    
    private void HidePasswordPopup()
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
} 