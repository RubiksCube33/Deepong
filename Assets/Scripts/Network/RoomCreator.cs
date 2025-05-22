using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class RoomCreator : MonoBehaviourPunCallbacks
{
    [Header("UI 요소")]
    public TMP_InputField roomNameInput;
    public TMP_InputField passwordInput; // 비밀번호 입력 필드
    public Button createRoomButton;
    public TextMeshProUGUI errorText;

    [Header("방 설정")]
    public byte maxPlayers = 2;
    public string gameSceneName = "Court_Testing";

    private MatchMakingManager matchMakingManager;
    private string defaultRoomName = "Room";

    private void Start()
    {
        // 필요한 매니저 참조 가져오기
        matchMakingManager = FindObjectOfType<MatchMakingManager>();
        
        // 초기 UI 설정
        if (errorText != null)
            errorText.gameObject.SetActive(false);
        
        // 버튼 클릭 이벤트 등록
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(CreateRoom);
        
        // 기본 방 이름 설정
        if (roomNameInput != null)
            roomNameInput.text = defaultRoomName + Random.Range(1000, 10000);
    }

    // 방 생성 메서드
    public void CreateRoom()
    {
        // 입력된 방 이름이 없으면 기본 이름 사용
        string roomName = string.IsNullOrEmpty(roomNameInput.text) 
            ? defaultRoomName + Random.Range(1000, 10000) 
            : roomNameInput.text;
        
        // 방 옵션 설정
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        
        // 비밀번호가 입력되었다면 커스텀 속성으로 설정
        if (passwordInput != null && !string.IsNullOrEmpty(passwordInput.text))
        {
            // 커스텀 속성 생성 및 비밀번호 설정
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties.Add("Password", passwordInput.text);
            roomOptions.CustomRoomProperties = customProperties;
            
            // 비밀번호 속성을 공개하여 로비에서 비밀번호 방임을 표시
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "Password" };
        }
        
        // 방 생성 시도
        Debug.Log("방 생성 시도: " + roomName + (passwordInput != null && !string.IsNullOrEmpty(passwordInput.text) ? " (비밀번호 설정됨)" : ""));
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    // 방 생성 성공 콜백
    public override void OnCreatedRoom()
    {
        Debug.Log("방 생성 성공: " + PhotonNetwork.CurrentRoom.Name);
        
        // 게임 씬으로 이동
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("마스터 클라이언트이므로 게임 씬으로 이동합니다: " + gameSceneName);
            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    // 방 생성 실패 콜백
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message} (코드: {returnCode})");
        
        // 오류 메시지 표시
        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = $"방 생성에 실패했습니다: {message}";
        }
    }
}