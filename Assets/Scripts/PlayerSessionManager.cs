using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerSession
{
    public int playerId;
    public string playerName;
    public string ipAddress;   // server-only
    public bool isConnected;
}

[System.Serializable]
public class PlayerSessionManager
{
    public Dictionary<int, PlayerSession> sessions = new Dictionary<int, PlayerSession>();

    public void AddSession(int playerId, string playerName, string ipAddress)
    {
        sessions[playerId] = new PlayerSession
        {
            playerId = playerId,
            playerName = playerName,
            ipAddress = ipAddress,
            isConnected = true
        };
    }

    public PlayerSession GetPlayerSession(int playerId)
    {
        return sessions.TryGetValue(playerId, out var session) ? session : null;
    }

    public void RemovePlayerSession(int playerId)
    {
        sessions.Remove(playerId);
    }
}

