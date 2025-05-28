using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MakingRoomUI : MonoBehaviour
{
    [Header("방 생성 UI 요소들")]
    public TMP_InputField roomNameInput;
    public TMP_InputField passwordInput;
    public Button createRoomButton;
    public Button backButton;
    
    [Header("UI 패널들")]
    public TextMeshProUGUI errorMessageText;
    public GameObject errorPanel;
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }
    
    private void InitializeUI()
    {
        if (roomNameInput != null)
            roomNameInput.text = "New Room " + Random.Range(1000, 9999);
        
        if (errorPanel != null)
            errorPanel.SetActive(false);
        
        // 비밀번호 입력 필드는 항상 활성화 상태로 유지
        if (passwordInput != null)
        {
            passwordInput.text = "";
            passwordInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Password (Optional)";
        }
    }
    
    private void SetupEventListeners()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }
    
    public void OnCreateRoomClicked()
    {
        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text.Trim() : "";
        
        if (string.IsNullOrEmpty(roomName))
        {
            ShowError("Please enter a room name!");
            return;
        }
        
        if (roomName.Length > 20)
        {
            ShowError("Room name must be 20 characters or less!");
            return;
        }
        
        // 비밀번호가 입력되었다면 길이 체크
        if (!string.IsNullOrEmpty(password) && password.Length > 10)
        {
            ShowError("Password must be 10 characters or less!");
            return;
        }
        
        CreateAndSaveRoom(roomName, password);
    }
    
    private void CreateAndSaveRoom(string roomName, string password)
    {
        if (RoomDataManager.Instance == null)
        {
            GameObject manager = new GameObject("RoomDataManager");
            manager.AddComponent<RoomDataManager>();
        }
        
        // 비밀번호가 있으면 비밀방, 없으면 공개방
        RoomData newRoom = new RoomData(roomName, password, 2);
        RoomDataManager.Instance.AddRoom(newRoom);
        
        string roomType = string.IsNullOrEmpty(password) ? "Public" : "Private";
        Debug.Log($"방 생성 완료: {roomName} ({roomType})");
        
        SceneManager.LoadScene("ChoosingRoomScene");
    }
    
    public void OnBackClicked()
    {
        SceneManager.LoadScene("WaitingRoomScene");
    }
    
    private void ShowError(string message)
    {
        if (errorMessageText != null)
            errorMessageText.text = message;
            
        if (errorPanel != null)
            errorPanel.SetActive(true);
            
        Invoke("HideError", 3f);
    }
    
    public void HideError()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);
    }
} 