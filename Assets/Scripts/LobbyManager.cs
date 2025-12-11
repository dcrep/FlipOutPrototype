using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    private const int MIN_PLAYERS = 2;
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
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        ulong playerNetworkId = clientId;
        string playerName = $"Player{playerNetworkId}";
        string ipAddress = "127.0.0.1";
        
        sessionManager.AddSession(playerNetworkId, playerName, ipAddress);
        NotifyPlayerJoinedClientRpc(playerNetworkId, playerName);
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
            isReady = false
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
        OnAllPlayersReady?.Invoke();
        //SceneManager.LoadScene("Game");
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }

    public void HostInitiatedShutdown()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only the host can initiate shutdown");
            return;
        }
        
        ShutdownAllClientsClientRpc();
    }

    [ClientRpc]
    private void ShutdownAllClientsClientRpc()
    {
        Debug.Log("Server initiated shutdown - returning to main menu");
        
        // Disconnect
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }
        
        // Load main menu (small delay to allow shutdown)
        GameManager.Instance.LoadScene(Scenes.MainMenu);
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