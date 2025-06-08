using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;

public class Launcher : MonoBehaviourPunCallbacks
{
    [Header("게임 중 UI 요소")]
    [SerializeField] private Text debugText; // 디버그 정보를 표시할 텍스트
    [SerializeField] private Button leaveRoomButton; // 방 나가기 버튼
    [SerializeField] private GameObject networkStatusPanel; // 네트워크 상태 패널
    
    [Header("에디터 테스트용 설정")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private KeyCode leaveRoomKey = KeyCode.Escape;
    
    // 마지막 로그 메시지 (콘솔 출력용)
    private string lastLogMessage;
    
    void Start()
    {
        // 버튼 이벤트 설정
        if (leaveRoomButton) 
            leaveRoomButton.onClick.AddListener(LeaveRoom);
        
        // 네트워크 상태 패널 초기화
        if (networkStatusPanel != null)
            networkStatusPanel.SetActive(showDebugInfo);
        
        // 디버그 텍스트 초기화
        UpdateDebugText("게임 시작...");
        
        // Photon 연결 상태 확인
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Photon에 연결되지 않은 상태에서 게임 씬이 로드되었습니다.");
            UpdateDebugText("서버 연결 없음 - 싱글플레이어 모드");
        }
        else if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("방에 참가하지 않은 상태에서 게임 씬이 로드되었습니다.");
            UpdateDebugText("방에 참가하지 않음");
        }
        else
        {
            UpdateDebugText($"방 '{PhotonNetwork.CurrentRoom.Name}'에서 게임 시작");
        }
    }
    
    private void Update()
    {
        // 현재 연결 상태를 계속 업데이트
        if (showDebugInfo && debugText)
        {
            string connectionStatus = GetConnectionStatus();
            UpdateDebugText(connectionStatus);
        }
        
        // 에디터 테스트용 키 입력 처리
        #if UNITY_EDITOR
        if (Input.GetKeyDown(leaveRoomKey))
        {
            LeaveRoom();
        }
        #endif
    }
    
    private string GetConnectionStatus()
    {
        if (!PhotonNetwork.IsConnected)
        {
            return "연결되지 않음 - 싱글플레이어 모드";
        }
        
        string status = $"연결됨: {PhotonNetwork.CloudRegion} 지역, 핑: {PhotonNetwork.GetPing()}ms\n";
        
        if (PhotonNetwork.InRoom)
        {
            status += $"현재 방: {PhotonNetwork.CurrentRoom.Name}\n";
            status += $"플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}\n";
            status += $"방장: {(PhotonNetwork.IsMasterClient ? "나" : "상대방")}\n";
            
            // 플레이어 목록
            status += "플레이어 목록:\n";
            foreach (var player in PhotonNetwork.PlayerList)
            {
                status += $"- {player.NickName} {(player.IsMasterClient ? "(방장)" : "")}\n";
            }
        }
        else
        {
            status += "방에 참가하지 않음";
        }
        
        return status;
    }
    
    private void UpdateDebugText(string message)
    {
        if (debugText && showDebugInfo)
        {
            debugText.text = message;
        }
        
        if (message != lastLogMessage)
        {
            Debug.Log("[Game Network Status] " + message);
            lastLogMessage = message;
        }
    }
    
    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("방을 나갑니다.");
            UpdateDebugText("방에서 나가는 중...");
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            Debug.Log("싱글플레이어 모드에서 메인 메뉴로 돌아갑니다.");
            LoadMainMenu();
        }
    }
    
    private void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }
    
    // Photon 콜백들
    public override void OnLeftRoom()
    {
        Debug.Log("방에서 나갔습니다. 메인 메뉴로 이동합니다.");
        UpdateDebugText("방에서 나가는 중...");
        LoadMainMenu();
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"플레이어 입장: {newPlayer.NickName}");
        UpdateDebugText($"{newPlayer.NickName}님이 게임에 참가했습니다!");
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"플레이어 퇴장: {otherPlayer.NickName}");
        UpdateDebugText($"{otherPlayer.NickName}님이 게임에서 나갔습니다.");
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"서버와 연결이 끊어졌습니다: {cause}");
        UpdateDebugText($"연결 끊어짐: {cause}");
        
        // 자동으로 메인 메뉴로 이동
        Invoke("LoadMainMenu", 2f);
    }
    
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"방장이 변경되었습니다: {newMasterClient.NickName}");
        UpdateDebugText($"새로운 방장: {newMasterClient.NickName}");
    }
    
    // UI 토글 메서드 (디버그용)
    public void ToggleDebugInfo()
    {
        showDebugInfo = !showDebugInfo;
        
        if (networkStatusPanel != null)
            networkStatusPanel.SetActive(showDebugInfo);
    }
}