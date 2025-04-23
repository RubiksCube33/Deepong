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
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;

        // 초당 20번 업데이트, Serialize 콜은 초당 15번
        PhotonNetwork.SendRate = 20;
        PhotonNetwork.SerializationRate = 15;
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("서버 연결됨. 룸 입장 시도 중...");
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("룸 입장 성공! 현재 룸: " + PhotonNetwork.CurrentRoom.Name);
    }
}
