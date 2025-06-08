using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

// 방 목록을 관리하는 싱글톤 매니저
public class RoomDataManager : MonoBehaviourPunCallbacks
{
    public static RoomDataManager Instance { get; private set; }
    
    // 로컬 방 목록과 Photon 방 목록을 분리 관리
    public List<RoomData> localRoomList = new List<RoomData>();
    public List<RoomData> photonRoomList = new List<RoomData>();
    
    // 이벤트: 방 목록이 업데이트될 때 호출
    public System.Action OnRoomListUpdated;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Photon 로비 입장 (연결되어 있는 경우에만)
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("Photon 로비에 입장합니다.");
        }
    }
    
    // 새 로컬 방 추가
    public void AddRoom(RoomData room)
    {
        localRoomList.Add(room);
        OnRoomListUpdated?.Invoke();
        Debug.Log($"새 로컬 방 추가됨: {room.roomName}");
    }
    
    // 로컬 방 제거
    public void RemoveRoom(string roomId)
    {
        localRoomList.RemoveAll(r => r.roomId == roomId);
        OnRoomListUpdated?.Invoke();
        Debug.Log($"로컬 방 제거됨: {roomId}");
    }
    
    // 방 찾기 (로컬 + Photon)
    public RoomData FindRoom(string roomId)
    {
        RoomData room = localRoomList.Find(r => r.roomId == roomId);
        if (room == null)
            room = photonRoomList.Find(r => r.roomId == roomId);
        return room;
    }
    
    // 모든 방 목록 가져오기 (로컬 + Photon)
    public List<RoomData> GetAllRooms()
    {
        List<RoomData> allRooms = new List<RoomData>();
        allRooms.AddRange(localRoomList);
        allRooms.AddRange(photonRoomList);
        return allRooms;
    }
    
    // Photon 방 목록 업데이트 콜백
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        photonRoomList.Clear();
        
        foreach (var roomInfo in roomList)
        {
            if (!roomInfo.RemovedFromList)
            {
                RoomData photonRoom = CreateRoomDataFromPhoton(roomInfo);
                photonRoomList.Add(photonRoom);
            }
        }
        
        OnRoomListUpdated?.Invoke();
        Debug.Log($"Photon 방 목록 업데이트: {photonRoomList.Count}개의 방");
    }
    
    // Photon RoomInfo를 RoomData로 변환
    private RoomData CreateRoomDataFromPhoton(RoomInfo roomInfo)
    {
        RoomData roomData = new RoomData(roomInfo.Name, "", roomInfo.MaxPlayers);
        
        // Photon 관련 정보 설정
        roomData.photonRoomName = roomInfo.Name;
        roomData.isPhotonRoom = true;
        roomData.currentPlayers = roomInfo.PlayerCount;
        roomData.roomId = roomInfo.Name;
        
        // 커스텀 프로퍼티에서 비밀번호 정보 가져오기
        if (roomInfo.CustomProperties.ContainsKey("hasPassword"))
        {
            roomData.hasPassword = (bool)roomInfo.CustomProperties["hasPassword"];
            if (roomData.hasPassword && roomInfo.CustomProperties.ContainsKey("password"))
            {
                roomData.password = roomInfo.CustomProperties["password"].ToString();
            }
        }
        
        return roomData;
    }
    
    public override void OnJoinedLobby()
    {
        Debug.Log("Photon 로비에 입장했습니다.");
    }
    
    public override void OnLeftLobby()
    {
        Debug.Log("Photon 로비에서 나갔습니다.");
        photonRoomList.Clear();
        OnRoomListUpdated?.Invoke();
    }
}