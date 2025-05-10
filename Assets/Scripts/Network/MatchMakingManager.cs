using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class MatchMakingManager : MonoBehaviourPunCallbacks
{
    [Header("매치메이킹 설정")]
    public string gameVersion = "1.0"; // 게임 버전 (같은 버전끼리만 매칭)
    public byte maxPlayersPerRoom = 2; // 방당 최대 플레이어 수
    public string gameSceneName = "Court_Testing"; // 게임 씬 이름
    
    [Header("UI 요소")]
    public GameObject matchmakingUI; // 매치메이킹 UI
    public GameObject errorPanel; // 오류 표시 패널
    public TMPro.TextMeshProUGUI errorText; // 오류 메시지 텍스트
    
    // 이벤트 델리게이트
    public delegate void RoomEvent();
    public static event RoomEvent OnRoomCreated;
    public static event RoomEvent OnRoomJoined;
    public static event RoomEvent OnRoomCreateFailed;
    public static event RoomEvent OnRoomJoinFailed;
    
    private string defaultRoomName = "Room"; // 기본 방 이름
    private bool isConnecting = false; // 연결 중 상태

    private void Awake()
    {
        // 씬 전환 시에도 이 매니저 유지
        DontDestroyOnLoad(this.gameObject);
        
        // 게임 버전 설정
        PhotonNetwork.GameVersion = gameVersion;
    }
    
    private void Start()
    {
        // UI 초기화
        if (matchmakingUI != null)
            matchmakingUI.SetActive(false);
        if (errorPanel != null)
            errorPanel.SetActive(false);
    }

    #region PHOTON_CALLBACKS
    
    // 방 생성 성공 콜백
    public override void OnCreatedRoom()
    {
        Debug.Log("방 생성 성공: " + PhotonNetwork.CurrentRoom.Name);
        if (OnRoomCreated != null) OnRoomCreated();
    }
    
    // 방 생성 실패 콜백
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        isConnecting = false;
        Debug.LogError($"방 생성 실패: {message} (코드: {returnCode})");
        
        ShowError($"방 생성에 실패했습니다: {message}");
        if (OnRoomCreateFailed != null) OnRoomCreateFailed();
    }
    
    // 방 참가 성공 콜백
    public override void OnJoinedRoom()
    {
        Debug.Log("방 참가 성공: " + PhotonNetwork.CurrentRoom.Name);
        
        // 게임 씬으로 이동
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("마스터 클라이언트이므로 게임 씬으로 이동합니다: " + gameSceneName);
            PhotonNetwork.LoadLevel(gameSceneName);
        }
        
        if (OnRoomJoined != null) OnRoomJoined();
    }
    
    // 랜덤 방 참가 실패 콜백
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("랜덤 방 참가 실패: " + message);
        
        // 랜덤 방 참가 실패 시 새 방 생성
        Debug.Log("새로운 방을 생성합니다.");
        CreateRoom();
    }
    
    // 특정 방 참가 실패 콜백
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        isConnecting = false;
        Debug.LogError($"방 참가 실패: {message} (코드: {returnCode})");
        
        ShowError($"방 참가에 실패했습니다: {message}");
        if (OnRoomJoinFailed != null) OnRoomJoinFailed();
    }
    
    // 연결 끊김 콜백
    public override void OnDisconnected(DisconnectCause cause)
    {
        isConnecting = false;
        Debug.LogWarning($"서버와 연결이 끊겼습니다: {cause}");
        
        if (cause != DisconnectCause.DisconnectByClientLogic)
        {
            ShowError($"서버와 연결이 끊겼습니다: {cause}");
        }
    }
    
    #endregion
    
    #region PUBLIC_METHODS
    
    // "방 생성" 버튼 클릭 시 호출되는 메서드
    public void CreateRoom()
    {
        if (isConnecting) return;
        isConnecting = true;
        
        // 기본 방 이름 + 랜덤 숫자로 방 이름 생성
        string randomRoomName = defaultRoomName + Random.Range(1000, 10000);
        
        // 방 옵션 설정
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        
        // 방 생성 요청
        Debug.Log("방 생성 시도: " + randomRoomName);
        PhotonNetwork.CreateRoom(randomRoomName, roomOptions);
    }
    
    // 특정 이름의 방에 참가하는 메서드
    public void JoinRoom(string roomName)
    {
        if (isConnecting) return;
        isConnecting = true;
        
        Debug.Log("방 참가 시도: " + roomName);
        PhotonNetwork.JoinRoom(roomName);
    }
    
    // "랜덤 매칭" 버튼 클릭 시 호출되는 메서드
    public void JoinRandomRoom()
    {
        if (isConnecting) return;
        isConnecting = true;
        
        Debug.Log("랜덤 방 참가 시도");
        PhotonNetwork.JoinRandomRoom();
    }
    
    // 오류 패널 표시
    private void ShowError(string message)
    {
        if (errorPanel != null && errorText != null)
        {
            errorText.text = message;
            errorPanel.SetActive(true);
        }
    }
    
    // 오류 패널 닫기
    public void CloseErrorPanel()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }
    
    #endregion
}