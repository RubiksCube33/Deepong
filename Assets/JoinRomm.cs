using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class JoinRomm : MonoBehaviourPunCallbacks
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 1) 이미 연결 중이거나 연결된 상태라면 중복 호출 방지
        if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        // 씬 자동 동기화
        PhotonNetwork.AutomaticallySyncScene = true;

        // 네트워크 업데이트 빈도 설정
        PhotonNetwork.SendRate = 20;           // 초당 20번
        PhotonNetwork.SerializationRate = 15;  // 초당 15번
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("서버 연결됨. 룸 입장 시도 중...");
        var options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("룸 입장 성공! 현재 룸: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // 연결이 끊기면 자동 재접속
        Debug.LogWarning($"Disconnected: {cause}. 재연결 시도...");
        PhotonNetwork.ConnectUsingSettings();
    }
}