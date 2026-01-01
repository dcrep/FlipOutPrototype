using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Collections;

//!TODO: Disable name changing after connect. (also disable input field in UI)

public class LobbyManager : NetworkBehaviour
{
    private const int MIN_PLAYERS = 2;
    private bool nameChangeDisabled = false;
    private PlayerSessionManager sessionManager = new PlayerSessionManager();
    
    // Events (UI subscribes to these)
    public delegate void PlayerJoinedDelegate(PlayerSession player);
    public event PlayerJoinedDelegate OnPlayerJoined;
    
    public delegate void PlayerLeftDelegate(ulong playerNetworkId);
    public event PlayerLeftDelegate OnPlayerLeft;
    
    public delegate void PlayerReadyDelegate(ulong playerNetworkId, bool ready);
    public event PlayerReadyDelegate OnPlayerReadyChanged;

    public delegate void PlayerNameChangedDelegate(ulong playerNetworkId, string playerName);
    public event PlayerNameChangedDelegate OnPlayerNameChanged;
    
    public delegate void GameStartDelegate();
    public event GameStartDelegate OnAllPlayersReady;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            EnsureHostSession();
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void EnsureHostSession()
    {
        ulong hostId = Unity.Netcode.NetworkManager.ServerClientId;
        if (sessionManager.GetPlayerSession(hostId) == null)
        {
            sessionManager.AddSession(hostId, $"Player{hostId}"); //, "127.0.0.1");
        }

    }

    private void OnClientConnected(ulong clientId)
    {
        EnsureHostSession();
        // Add the connecting client if missing
        if (sessionManager.GetPlayerSession(clientId) == null)
        {
            string playerName = $"Player{clientId}";
            //string ipAddress = "127.0.0.1";
            sessionManager.AddSession(clientId, playerName); //, ipAddress);
            NotifyPlayerJoinedClientRpc(clientId, playerName);
        }
        // Broadcast updated roster to ALL clients so everyone knows about the new player
        BroadcastRosterToAllClients();
    }

    private void BroadcastRosterToAllClients()
    {
        var list = new List<PlayerSession>(sessionManager.sessions.Values);
        list.Sort((a, b) => a.playerNetworkId.CompareTo(b.playerNetworkId));
        int count = list.Count;
        ulong[] ids = new ulong[count];
        FixedString64Bytes[] names = new FixedString64Bytes[count];
        bool[] ready = new bool[count];

        for (int i = 0; i < count; i++)
        {
            ids[i] = list[i].playerNetworkId;
            names[i] = list[i].playerName;
            ready[i] = list[i].isReady;
        }

        SyncAllPlayersClientRpc(ids, names, ready);
    }

    private void SendFullRosterToClient(ulong targetClientId)
    {
        var list = new List<PlayerSession>(sessionManager.sessions.Values);
        // Sort by playerNetworkId to ensure consistent order across all clients
        list.Sort((a, b) => a.playerNetworkId.CompareTo(b.playerNetworkId));
        int count = list.Count;
        ulong[] ids = new ulong[count];
        FixedString64Bytes[] names = new FixedString64Bytes[count];
        bool[] ready = new bool[count];

        for (int i = 0; i < count; i++)
        {
            ids[i] = list[i].playerNetworkId;
            names[i] = list[i].playerName;
            ready[i] = list[i].isReady;
        }

        ClientRpcParams p = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
        };
        SyncAllPlayersClientRpc(ids, names, ready, p);
    }

    [ClientRpc]
    private void SyncAllPlayersClientRpc(ulong[] playerNetworkIds, FixedString64Bytes[] playerNames, bool[] readyStates, ClientRpcParams rpcParams = default)
    {
        // Re-hydrate client UI state
        if (playerNetworkIds == null || playerNames == null || readyStates == null) return;

        for (int i = 0; i < playerNetworkIds.Length; i++)
        {
            OnPlayerJoined?.Invoke(new PlayerSession
            {
                playerNetworkId = playerNetworkIds[i],
                playerName = playerNames[i].ToString(),
                isConnected = true,
                isReady = readyStates[i],
                playerServerId = -1
            });
        }

        for (int i = 0; i < playerNetworkIds.Length; i++)
        {
            OnPlayerReadyChanged?.Invoke(playerNetworkIds[i], readyStates[i]);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        ulong playerNetworkId = clientId;
        sessionManager.RemovePlayerSession(playerNetworkId);
        NotifyPlayerLeftClientRpc(playerNetworkId);
    }

    public void SetPlayerName(ulong playerNetworkId, string playerName)
    {
        SetPlayerNameServerRpc(playerNetworkId, playerName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(ulong playerNetworkId, string playerName, ServerRpcParams rpcParams = default)
    {
        // Don't allow name changes after game starts
        if (nameChangeDisabled)
        {
            Debug.LogWarning($"Cannot change player name after game has started");
            return;
        }
        Debug.Log($"Server received name update for {playerNetworkId}: {playerName}");
        // Verify the sender is only updating their own name
        if (rpcParams.Receive.SenderClientId != playerNetworkId)
        {
            Debug.LogWarning($"Client {rpcParams.Receive.SenderClientId} tried to change name for player {playerNetworkId}");
            return;
        }
        var session = sessionManager.GetPlayerSession(playerNetworkId);
        if (session != null)
        {
            session.playerName = playerName;
            UpdatePlayerNameClientRpc(playerNetworkId, playerName);
        }
    }

    public void LocalPlayerClickedReady()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Cannot click ready - NetworkManager not started");
            return;
        }

        ulong clientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        ulong playerNetworkId = clientId;
        NotifyPlayerReadyServerRpc(playerNetworkId, true);
    }

    public void LocalPlayerClickedUnready()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Cannot click unready - NetworkManager not started");
            return;
        }
        ulong clientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        ulong playerNetworkId = clientId;
        NotifyPlayerReadyServerRpc(playerNetworkId, false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyPlayerReadyServerRpc(ulong playerNetworkId, bool ready, ServerRpcParams rpcParams = default)
    {
        sessionManager.SetPlayerReady(playerNetworkId, ready);
        UpdatePlayerReadyStateClientRpc(playerNetworkId, ready);
        
 //       if (CanStartGame())
 //       {
 //           AllPlayersReadyClientRpc();
 //       }
    }

    public void HostClickedStartGame()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only the host can start the game");
            return;
        }

        if (!CanStartGame())
        {
            Debug.LogWarning("Cannot start game - not all players ready or minimum players not met");
            return;
        }

        StartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        if (IsServer && CanStartGame())
        {
            AllPlayersReadyClientRpc();
        }
    }

    private bool CanStartGame()
    {
        // Need at least MIN_PLAYERS players connected
        if (sessionManager.sessions.Count < MIN_PLAYERS)
        {
            return false;
        }
        
        // All players must be ready
        return sessionManager.AreAllPlayersReady();
    }

    [ClientRpc]
    private void NotifyPlayerJoinedClientRpc(ulong playerNetworkId, string playerName)
    {
        OnPlayerJoined?.Invoke(new PlayerSession 
        { 
            playerNetworkId = playerNetworkId, 
            playerName = playerName, 
            isConnected = true,
            isReady = false,
            playerServerId = -1
        });
    }

    [ClientRpc]
    private void NotifyPlayerLeftClientRpc(ulong playerNetworkId)
    {
        OnPlayerLeft?.Invoke(playerNetworkId);
    }

    [ClientRpc]
    private void UpdatePlayerNameClientRpc(ulong playerNetworkId, string playerName)
    {
        OnPlayerNameChanged?.Invoke(playerNetworkId, playerName);
        /*OnPlayerJoined?.Invoke(new PlayerSession 
        { 
            playerNetworkId = playerNetworkId, 
            playerName = playerName, 
            isConnected = true,
            isReady = false
        });*/
    }

    [ClientRpc]
    private void UpdatePlayerReadyStateClientRpc(ulong playerNetworkId, bool ready)
    {
        OnPlayerReadyChanged?.Invoke(playerNetworkId, ready);
    }

    [ClientRpc]
    private void AllPlayersReadyClientRpc()
    {
        nameChangeDisabled = true;
        OnAllPlayersReady?.Invoke();
        // handle scene loading in event subscriber
    }

    public void HostInitiatedShutdown(bool bLoadMainMenu = true)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only the host can initiate shutdown");
            return;
        }
        
        ShutdownAllClientsClientRpc(bLoadMainMenu);
    }

    [ClientRpc]
    private void ShutdownAllClientsClientRpc(bool bLoadMainMenu)
    {
        Debug.Log("Server initiated shutdown - returning to main menu");
        
        // Disconnect
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }
        
        // Load main menu (small delay to allow shutdown)
        if (bLoadMainMenu)
        {
            GameManager.Instance.LoadScene(Scenes.MainMenu);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}