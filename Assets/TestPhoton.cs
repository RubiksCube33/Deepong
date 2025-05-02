using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon; // 커스텀 룸 프로퍼티를 위해 필요

public class PhotonTest : MonoBehaviourPunCallbacks
{
    // 매칭 타입과 최대 플레이어 수 설정 (Inspector에서 변경 가능)
    public string matchType = "Deathmatch";
    public byte maxPlayersPerRoom = 2;

    // 게임 시작 시 Photon 서버에 연결합니다.
    void Start()
    {
        Debug.Log("Photon 서버에 연결 중...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // 마스터 서버에 연결되었을 때 호출됩니다.
    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon 마스터 서버에 연결되었습니다.");
        // 매칭 타입을 이용하여 조건에 맞는 룸에 참여를 시도합니다.
        Hashtable expectedRoomProperties = new Hashtable() { { "match", matchType } };
        PhotonNetwork.JoinRandomRoom(expectedRoomProperties, maxPlayersPerRoom);
    }

    // 무작위 룸 참여 실패 시 호출됩니다.
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("무작위 룸 참여 실패: " + message);
        // 매칭 타입 속성을 포함한 룸 옵션을 설정합니다.
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.CustomRoomProperties = new Hashtable() { { "match", matchType } };
        // 로비에서 해당 프로퍼티를 노출시켜 룸 검색에 사용합니다.
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "match" };

        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    // 룸에 성공적으로 입장하면 호출됩니다.
    public override void OnJoinedRoom()
    {
        Debug.Log("룸에 입장했습니다: " + PhotonNetwork.CurrentRoom.Name);
        // 현재 룸의 매칭 타입 출력
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("match"))
        {
            Debug.Log("매칭 타입: " + PhotonNetwork.CurrentRoom.CustomProperties["match"]);
        }
    }

    // 연결이 끊어졌을 때 호출됩니다.
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Photon 연결 끊김: " + cause.ToString());
    }
}