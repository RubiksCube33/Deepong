using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomListItemUI : MonoBehaviour
{
    [Header("방 아이템 UI")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI statusText;
    public Image statusIcon;
    public Image backgroundImage;
    public Button selectButton;
    
    [Header("스타일 설정")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.cyan;
    public Color hoverColor = Color.gray;
    public Sprite lockIcon;
    public Sprite unlockIcon;
    
    private RoomData roomData;
    private ChoosingRoomUI parentUI;
    private bool isSelected = false;
    
    private void Awake()
    {
        // 버튼이 없으면 자동으로 추가
        if (selectButton == null)
            selectButton = GetComponent<Button>();
            
        if (selectButton == null)
            selectButton = gameObject.AddComponent<Button>();
    }
    
    public void SetupRoomItem(RoomData room, ChoosingRoomUI parent)
    {
        roomData = room;
        parentUI = parent;
        
        UpdateDisplay();
        SetupButton();
    }
    
    private void UpdateDisplay()
    {
        if (roomData == null) return;
        
        // 방 이름 설정
        if (roomNameText != null)
            roomNameText.text = roomData.roomName;
        
        // 플레이어 수 설정
        if (playerCountText != null)
            playerCountText.text = $"{roomData.currentPlayers}/{roomData.maxPlayers}";
        
        // 방 상태 설정
        if (statusText != null)
        {
            string status = roomData.hasPassword ? "Private" : "Public";
            statusText.text = status;
        }
        
        // 상태 아이콘 설정
        if (statusIcon != null)
        {
            if (roomData.hasPassword && lockIcon != null)
                statusIcon.sprite = lockIcon;
            else if (!roomData.hasPassword && unlockIcon != null)
                statusIcon.sprite = unlockIcon;
        }
        
        // 배경색 설정
        SetBackgroundColor(normalColor);
    }
    
    private void SetupButton()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnRoomItemClicked);
            
            // 호버 효과를 위한 이벤트 트리거 추가
            var eventTrigger = selectButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
                eventTrigger = selectButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            // 마우스 엔터 이벤트
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { OnMouseEnter(); });
            eventTrigger.triggers.Add(enterEntry);
            
            // 마우스 이탈 이벤트
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { OnMouseExit(); });
            eventTrigger.triggers.Add(exitEntry);
        }
    }
    
    private void OnRoomItemClicked()
    {
        if (parentUI != null && roomData != null)
        {
            parentUI.OnRoomItemClicked(roomData);
            SetSelected(true);
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selected)
            SetBackgroundColor(selectedColor);
        else
            SetBackgroundColor(normalColor);
    }
    
    private void OnMouseEnter()
    {
        if (!isSelected)
            SetBackgroundColor(hoverColor);
    }
    
    private void OnMouseExit()
    {
        if (!isSelected)
            SetBackgroundColor(normalColor);
    }
    
    private void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }
    
    // 방 정보 업데이트 (플레이어 수 변경 등)
    public void UpdateRoomInfo(RoomData updatedRoom)
    {
        roomData = updatedRoom;
        UpdateDisplay();
    }
    
    // 방 아이템 스타일 커스터마이징
    public void SetCustomStyle(Color bg, Color text)
    {
        normalColor = bg;
        SetBackgroundColor(bg);
        
        if (roomNameText != null)
            roomNameText.color = text;
        if (playerCountText != null)
            playerCountText.color = text;
        if (statusText != null)
            statusText.color = text;
    }
} 