using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerSession
{
    public ulong playerNetworkId;
    public string playerName;
    public string ipAddress;   // server-only
    public bool isConnected;
    public bool isReady;
    public int playerServerId; // server-only
}

[System.Serializable]
public class PlayerSessionManager
{
    public Dictionary<ulong, PlayerSession> sessions = new Dictionary<ulong, PlayerSession>();

    public void AddSession(ulong playerId, string playerName, string ipAddress)
    {
        sessions[playerId] = new PlayerSession
        {
            playerNetworkId = playerId,
            playerName = playerName,
            ipAddress = ipAddress,
            isConnected = true,
            playerServerId = -1,
            isReady = false
        };
    }

    public void AddLocalSession(string playerName, uint playerId, bool isReady = false)
    {
        sessions[(ulong)playerId] = new PlayerSession
        {
            playerNetworkId = (ulong)playerId,
            playerName = playerName,
            ipAddress = "localhost",
            isConnected = true,
            playerServerId = (int)playerId,
            isReady = isReady
        };
    }

    public PlayerSession GetPlayerSession(ulong playerId)
    {
        return sessions.TryGetValue(playerId, out var session) ? session : null;
    }

    public List<PlayerSession> GetAllSessions()
    {
        return new List<PlayerSession>(sessions.Values);
    }

    public void RemovePlayerSession(ulong playerId)
    {
        sessions.Remove(playerId);
    }


    public void SetPlayerReady(ulong playerId, bool ready)
    {
        if (sessions.TryGetValue(playerId, out var session))
        {
            session.isReady = ready;
        }
    }

    public bool AreAllPlayersReady()
    {
        if (sessions.Count == 0) return false;
        foreach (var session in sessions.Values)
        {
            if (!session.isReady) return false;
        }
        return true;
    }
}

