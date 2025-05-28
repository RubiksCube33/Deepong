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
            roomNameInput.text = "새로운 방 " + Random.Range(1000, 9999);
        
        if (errorPanel != null)
            errorPanel.SetActive(false);
        
        // 비밀번호 입력 필드는 항상 활성화 상태로 유지
        if (passwordInput != null)
        {
            passwordInput.text = "";
            passwordInput.placeholder.GetComponent<TextMeshProUGUI>().text = "비밀번호 (선택사항)";
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
            ShowError("방 이름을 입력해주세요!");
            return;
        }
        
        if (roomName.Length > 20)
        {
            ShowError("방 이름은 20자 이하로 입력해주세요!");
            return;
        }
        
        // 비밀번호가 입력되었다면 길이 체크
        if (!string.IsNullOrEmpty(password) && password.Length > 10)
        {
            ShowError("비밀번호는 10자 이하로 입력해주세요!");
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
        
        string roomType = string.IsNullOrEmpty(password) ? "공개방" : "비밀방";
        Debug.Log($"방 생성 완료: {roomName} ({roomType})");
        
        SceneManager.LoadScene("ChoosingRoomScene");
    }
    
    public void OnBackClicked()
    {
        SceneManager.LoadScene("MainMenuScene");
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