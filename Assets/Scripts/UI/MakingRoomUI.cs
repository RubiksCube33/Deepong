using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class MakingRoomUI : MonoBehaviourPunCallbacks
{
    [Header("방 생성 UI 요소들")]
    public TMP_InputField roomNameInput;
    public TMP_InputField passwordInput;
    public Button createRoomButton;
    public Button backButton;
    
    [Header("UI 패널들")]
    public TextMeshProUGUI errorMessageText;
    public GameObject errorPanel;
    
    [Header("네트워크 설정")]
    public bool createPhotonRoom = true; // true면 Photon 온라인 방, false면 로컬 방
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        
        // Photon 연결 상태 확인
        CheckPhotonConnectionStatus();
    }
    
    private void Update()
    {
        // F12 키로 실시간 Photon 상태 확인
        if (Input.GetKeyDown(KeyCode.F12))
        {
            CheckPhotonConnectionStatus();
        }
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
        Debug.Log("🔹 방 생성 시작 🔹");
        Debug.Log($"방 이름: {roomName}");
        Debug.Log($"비밀번호: {(string.IsNullOrEmpty(password) ? "없음" : "있음")}");
        Debug.Log($"createPhotonRoom 설정: {createPhotonRoom}");
        Debug.Log($"PhotonNetwork.IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"PhotonNetwork.ConnectionState: {PhotonNetwork.NetworkClientState}");
        
        if (createPhotonRoom && PhotonNetwork.IsConnected)
        {
            Debug.Log("✅ Photon 온라인 방 생성 모드");
            
            // 실제 Photon 방 생성
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 2,
                IsVisible = true,
                IsOpen = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "password", password },
                    { "hasPassword", !string.IsNullOrEmpty(password) }
                }
            };
            
            PhotonNetwork.CreateRoom(roomName, roomOptions);
            Debug.Log($"🚀 Photon 방 생성 요청 전송: {roomName}");
        }
        else
        {
            Debug.LogWarning("❌ 로컬 방 생성 모드로 전환");
            
            if (!createPhotonRoom)
            {
                Debug.Log("원인: createPhotonRoom이 false로 설정됨");
            }
            else if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("원인: PhotonNetwork.IsConnected가 false!");
                Debug.LogError($"현재 연결 상태: {PhotonNetwork.NetworkClientState}");
                Debug.LogError("해결방법: Launcher 스크립트로 Photon 연결 필요");
            }
            
            // 로컬 방 생성 (테스트용)
            if (RoomDataManager.Instance == null)
            {
                GameObject manager = new GameObject("RoomDataManager");
                manager.AddComponent<RoomDataManager>();
            }
            
            RoomData newRoom = new RoomData(roomName, password, 2);
            RoomDataManager.Instance.AddRoom(newRoom);
            
            string roomType = string.IsNullOrEmpty(password) ? "Public" : "Private";
            Debug.Log($"📍 로컬 방 생성 완료: {roomName} ({roomType})");
            
            SceneManager.LoadScene("ChoosingRoomScene");
        }
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
    
    // Photon 콜백 메서드들 추가
    public override void OnCreatedRoom()
    {
        Debug.Log($"Photon 방 생성 성공: {PhotonNetwork.CurrentRoom.Name}");
        // 방 생성 후 ChoosingRoomScene으로 돌아가서 자신이 만든 방을 확인
        SceneManager.LoadScene("ChoosingRoomScene");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ShowError($"방 생성 실패: {message}");
        Debug.LogError($"방 생성 실패: {message} (코드: {returnCode})");
    }

    private void CheckPhotonConnectionStatus()
    {
        Debug.Log("=== Photon 연결 상태 체크 ===");
        Debug.Log($"PhotonNetwork.IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"PhotonNetwork.ConnectionState: {PhotonNetwork.NetworkClientState}");
        Debug.Log($"PhotonNetwork.InLobby: {PhotonNetwork.InLobby}");
        Debug.Log($"PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"createPhotonRoom 설정: {createPhotonRoom}");
        
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log($"서버 지역: {PhotonNetwork.CloudRegion}");
            Debug.Log($"핑: {PhotonNetwork.GetPing()}ms");
            Debug.Log($"닉네임: {PhotonNetwork.NickName}");
        }
        else
        {
            Debug.LogWarning("⚠️ Photon에 연결되지 않음!");
            Debug.Log("가능한 원인:");
            Debug.Log("1. Photon App ID가 설정되지 않음");
            Debug.Log("2. 네트워크 연결 문제");
            Debug.Log("3. Launcher 스크립트가 실행되지 않음");
        }
        Debug.Log("========================");
    }
} 