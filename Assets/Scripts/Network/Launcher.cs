using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject connectingInfoPanel;
    
    void Start()
    {
        // 게임 시작 시 Photon 서버에 연결
        ConnectToPhoton();
    }
    
    private void ConnectToPhoton()
    {
        // 패널이 있다면 활성화
        if (connectingInfoPanel) connectingInfoPanel.SetActive(true);
        
        // 이미 연결되어 있다면 다시 연결하지 않음
        if (PhotonNetwork.IsConnected) return;
        
        // Photon 서버에 연결
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Photon 서버에 연결 시도 중...");
    }
    
    // 마스터 서버에 연결되었을 때 호출되는 콜백
    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon 마스터 서버에 연결되었습니다.");
        
        // 로비에 참가
        PhotonNetwork.JoinLobby();
        Debug.Log("로비 참가 시도 중...");
    }
    
    // 로비 참가 성공 시 호출되는 콜백
    public override void OnJoinedLobby()
    {
        Debug.Log("로비 참가 성공!");
        
        // 패널이 있다면 비활성화
        if (connectingInfoPanel) connectingInfoPanel.SetActive(false);
    }
    
    // 연결 실패 시 호출되는 콜백
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Photon 서버와 연결이 끊어졌습니다. 이유: {cause}");
        
        // 패널이 있다면 비활성화
        if (connectingInfoPanel) connectingInfoPanel.SetActive(false);
    }
}