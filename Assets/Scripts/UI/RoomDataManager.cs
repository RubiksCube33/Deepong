using UnityEngine;
using System.Collections.Generic;

// 방 목록을 관리하는 싱글톤 매니저
public class RoomDataManager : MonoBehaviour
{
    public static RoomDataManager Instance { get; private set; }
    
    public List<RoomData> roomList = new List<RoomData>();
    
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
    
    // 새 방 추가
    public void AddRoom(RoomData room)
    {
        roomList.Add(room);
        OnRoomListUpdated?.Invoke();
        Debug.Log($"새 방 추가됨: {room.roomName}");
    }
    
    // 방 제거
    public void RemoveRoom(string roomId)
    {
        roomList.RemoveAll(r => r.roomId == roomId);
        OnRoomListUpdated?.Invoke();
        Debug.Log($"방 제거됨: {roomId}");
    }
    
    // 방 찾기
    public RoomData FindRoom(string roomId)
    {
        return roomList.Find(r => r.roomId == roomId);
    }
    
    // 모든 방 목록 가져오기
    public List<RoomData> GetAllRooms()
    {
        return new List<RoomData>(roomList);
    }
}