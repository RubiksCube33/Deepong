using UnityEngine;
using System;

[System.Serializable]
public class RoomData
{
    public string roomName;
    public string password;
    public bool hasPassword;
    public int currentPlayers;
    public int maxPlayers;
    public string roomId;
    public DateTime createdTime;
    
    // Photon 관련 필드 추가
    public bool isPhotonRoom = false;
    public string photonRoomName = "";
    
    public RoomData(string name, string pass = "", int maxPl = 2)
    {
        roomName = name;
        password = pass;
        hasPassword = !string.IsNullOrEmpty(pass);
        currentPlayers = 0;
        maxPlayers = maxPl;
        roomId = System.Guid.NewGuid().ToString();
        createdTime = DateTime.Now;
        isPhotonRoom = false;
        photonRoomName = "";
    }
}