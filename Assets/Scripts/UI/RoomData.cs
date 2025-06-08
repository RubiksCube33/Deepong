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
    
    public RoomData(string name, string pass = "", int maxPl = 2)
    {
        roomName = name;
        password = pass;
        hasPassword = !string.IsNullOrEmpty(pass);
        currentPlayers = 0;
        maxPlayers = maxPl;
        roomId = System.Guid.NewGuid().ToString();
        createdTime = DateTime.Now;
    }
}