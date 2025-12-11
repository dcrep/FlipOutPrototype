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
}

[System.Serializable]
public class PlayerSessionManager
{

    const ulong PLAYER_ID_LOCAL = 1000;
    public Dictionary<ulong, PlayerSession> sessions = new Dictionary<ulong, PlayerSession>();

    public void AddSession(ulong playerId, string playerName, string ipAddress)
    {
        sessions[playerId] = new PlayerSession
        {
            playerNetworkId = playerId,
            playerName = playerName,
            ipAddress = ipAddress,
            isConnected = true
        };
    }

    public PlayerSession GetPlayerSession(ulong playerId)
    {
        return sessions.TryGetValue(playerId, out var session) ? session : null;
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

