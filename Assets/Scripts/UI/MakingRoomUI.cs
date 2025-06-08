using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

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
    public GameObject loadingPanel; // 로딩 표시용
    
    [Header("Photon 설정")]
    public string gameVersion = "1.0";
    public byte maxPlayersPerRoom = 2;
    
    private bool isCreatingRoom = false;
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        ConnectToPhoton();
    }
    
    private void InitializeUI()
    {
        if (roomNameInput != null)
            roomNameInput.text = "New Room " + Random.Range(1000, 9999);
        
        if (errorPanel != null)
            errorPanel.SetActive(false);
            
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        // 비밀번호 입력 필드는 항상 활성화 상태로 유지
        if (passwordInput != null)
        {
            passwordInput.text = "";
            passwordInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Password (Optional)";
        }
        
        // 처음에는 버튼 비활성화 (Photon 연결될 때까지)
        if (createRoomButton != null)
            createRoomButton.interactable = false;
    }
    
    private void SetupEventListeners()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }
    
    private void ConnectToPhoton()
    {
        // NetworkManager 확인 및 생성
        if (NetworkManager.Instance == null)
        {
            GameObject networkManagerGO = new GameObject("NetworkManager");
            networkManagerGO.AddComponent<NetworkManager>();
        }
        
        // NetworkManager를 통해 연결
        NetworkManager.Instance.ConnectToPhoton();
        
        Debug.Log("Photon 서버에 연결 중...");
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon 마스터 서버에 연결되었습니다.");
        EnableCreateButton();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Photon 연결이 끊어졌습니다: {cause}");
        if (createRoomButton != null)
            createRoomButton.interactable = false;
        
        ShowError($"서버 연결이 끊어졌습니다: {cause}");
    }
    
    private void EnableCreateButton()
    {
        if (createRoomButton != null)
            createRoomButton.interactable = true;
    }
    
    public void OnCreateRoomClicked()
    {
        if (isCreatingRoom) return;
        
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
        
        if (!PhotonNetwork.IsConnected)
        {
            ShowError("서버에 연결되지 않았습니다. 잠시 후 다시 시도해주세요.");
            return;
        }
        
        CreatePhotonRoom(roomName, password);
    }
    
    private void CreatePhotonRoom(string roomName, string password)
    {
        isCreatingRoom = true;
        
        // 로딩 패널 표시
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
            
        // 버튼 비활성화
        if (createRoomButton != null)
            createRoomButton.interactable = false;
        
        // Photon 방 옵션 설정
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true
        };
        
        // 커스텀 프로퍼티에 비밀번호 정보 저장
        Hashtable customProperties = new Hashtable();
        if (!string.IsNullOrEmpty(password))
        {
            customProperties["password"] = password;
            customProperties["hasPassword"] = true;
        }
        else
        {
            customProperties["hasPassword"] = false;
        }
        
        roomOptions.CustomRoomProperties = customProperties;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "hasPassword" };
        
        Debug.Log($"Photon 방 생성 시도: {roomName}");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public override void OnCreatedRoom()
    {
        Debug.Log($"Photon 방 생성 성공: {PhotonNetwork.CurrentRoom.Name}");
        
        // 로컬 방 데이터도 저장
        SaveLocalRoomData(PhotonNetwork.CurrentRoom);
        
        // 방에서 나와서 로비로 돌아가기
        Debug.Log("방에서 나가서 로비로 이동 중...");
        PhotonNetwork.LeaveRoom();
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("방에서 나갔습니다. 로비로 이동 중...");
        
        // 로비로 이동 (방 목록을 보기 위해)
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            // 이미 로비에 있다면 바로 씬 이동
            MoveToChoosingRoomScene();
        }
    }
    
    public override void OnJoinedLobby()
    {
        Debug.Log("로비에 입장했습니다. ChoosingRoomScene으로 이동합니다.");
        MoveToChoosingRoomScene();
    }
    
    private void MoveToChoosingRoomScene()
    {
        SceneManager.LoadScene("ChoosingRoomScene");
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message} (코드: {returnCode})");
        
        isCreatingRoom = false;
        
        // 로딩 패널 숨기기
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
            
        // 버튼 다시 활성화
        if (createRoomButton != null)
            createRoomButton.interactable = true;
        
        ShowError($"방 생성에 실패했습니다: {message}");
    }
    
    private void SaveLocalRoomData(Room room)
    {
        if (RoomDataManager.Instance == null)
        {
            GameObject manager = new GameObject("RoomDataManager");
            manager.AddComponent<RoomDataManager>();
        }
        
        // Photon 방 정보를 로컬 RoomData로 변환
        string password = "";
        bool hasPassword = false;
        
        if (room.CustomProperties.ContainsKey("password"))
        {
            password = room.CustomProperties["password"].ToString();
        }
        
        if (room.CustomProperties.ContainsKey("hasPassword"))
        {
            hasPassword = (bool)room.CustomProperties["hasPassword"];
        }
        
        RoomData newRoom = new RoomData(room.Name, password, room.MaxPlayers)
        {
            currentPlayers = room.PlayerCount,
            hasPassword = hasPassword
        };
        
        RoomDataManager.Instance.AddRoom(newRoom);
        
        string roomType = hasPassword ? "Private" : "Public";
        Debug.Log($"로컬 방 데이터 저장 완료: {room.Name} ({roomType})");
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