using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LobbyOnlineUI : MonoBehaviour
{
    [System.Serializable]
    public class PlayerLobbySlot
    {
        public bool isConnected = false;
        public string name;
        public ulong playerNetworkId = 0;
        public bool isReady = false;
    }

    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private List<PlayerLobbySlot> lobbySlots = new List<PlayerLobbySlot>();

    public TMP_Text localLanIPText, onlineIPText;
    public TMP_InputField hostIPInputField;
    public TMP_InputField playerNameInputField;
    public Button hostButton, hostLocalButton, hostInternetButton, connectButton, connectLocalButton;

    public Button startButton, cancelButton;
    public Button mainMenuButton;
    public Toggle localReadyCheckbox;
    public Toggle hostReadyCheckbox;

    [SerializeField] private GameObject localPlayerObject;
    [SerializeField] private GameObject hostSlotObject;
    public GameObject[] playerSlotObjects;

    public string debugPlayerName = "PlayerX";
    public bool debugAddPlayer = false;

    private string localIPAddress = "127.0.0.1";
    [SerializeField] private string publicIPAddress = "0.0.0.0";
    private const ushort defaultPort = 7777;

    void Awake()
    {
        string localIP = GetLocalIPAddress();
        Debug.Log("Local IP: " + localIP);
        localLanIPText.text = "LAN IP: " + localIP + ":" + defaultPort;
        onlineIPText.text = "Public IP: Fetching...";
        hostIPInputField.text = localIP;
        startButton.interactable = false;
        playerNameInputField.text = PlayerPreferences.Instance.playerName;
        StartCoroutine(FetchPublicIP());
    }

    private void Start()
    {
        hostButton.onClick.AddListener(OnHost);
        hostLocalButton.onClick.AddListener(OnHostLocal);
        hostInternetButton.onClick.AddListener(OnHostInternet);
        connectButton.onClick.AddListener(OnConnect);
        connectLocalButton.onClick.AddListener(OnConnectLocal);
        
        localReadyCheckbox.onValueChanged.AddListener(OnReadyToggle);
        if (hostReadyCheckbox != null)
        {
            hostReadyCheckbox.onValueChanged.AddListener(OnReadyToggle);
            hostReadyCheckbox.interactable = false;
        }
        startButton.onClick.AddListener(OnStartGame);
        cancelButton.onClick.AddListener(OnCancelButton);
        mainMenuButton.onClick.AddListener(OnMenuButton);

        // Subscribe to lobby manager events
        lobbyManager.OnPlayerJoined += HandlePlayerJoined;
        lobbyManager.OnPlayerNameChanged += HandlePlayerNameChanged;
        lobbyManager.OnPlayerLeft += HandlePlayerLeft;
        lobbyManager.OnPlayerReadyChanged += HandlePlayerReadyChanged;
        lobbyManager.OnAllPlayersReady += HandleGameStart;

        // Subscribe to network disconnect events
        var netMan = Unity.Netcode.NetworkManager.Singleton;
        if (netMan != null)
        {
            netMan.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // Hide all slots initially
        localPlayerObject.SetActive(true);
        hostSlotObject.SetActive(false);
        localReadyCheckbox.interactable = false;

        for (int i = 0; i < playerSlotObjects.Length; i++)
        {
            playerSlotObjects[i].SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnPlayerJoined -= HandlePlayerJoined;
            lobbyManager.OnPlayerNameChanged -= HandlePlayerNameChanged;
            lobbyManager.OnPlayerLeft -= HandlePlayerLeft;
            lobbyManager.OnPlayerReadyChanged -= HandlePlayerReadyChanged;
            lobbyManager.OnAllPlayersReady -= HandleGameStart;
        }
        var netMan = Unity.Netcode.NetworkManager.Singleton;
        if (netMan != null)
        {
            netMan.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    /*void Update()
    {
        CheckAndUpdatePlayerSlots();
        if (debugAddPlayer)
        {
            debugAddPlayer = false;
            AddPlayerToLobby(debugPlayerName + (lobbySlots.Count + 1), (ulong)lobbySlots.Count);
        }
        int readyCount = 0;
        foreach (var slot in lobbySlots)
        {
            if (slot.isConnected && slot.isReady)
            {
                readyCount++;
            }
        }

        // Example: require at least 2 ready players
        startButton.interactable = readyCount == lobbySlots.Count && readyCount >= 2;
    }*/

private System.Collections.IEnumerator FetchPublicIP()
{
    using (var req = UnityEngine.Networking.UnityWebRequest.Get("https://api.ipify.org"))
    {
        yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogWarning("Failed to get public IP: " + req.error);
            yield break;
        }

        publicIPAddress = req.downloadHandler.text.Trim();
        Debug.Log("Public IP: " + publicIPAddress);
        onlineIPText.text = "Public IP: " + publicIPAddress + ":" + defaultPort;
    }
}

    public string GetLocalIPAddress()
    {
    foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
    {
        if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
            continue;

        var type = nic.NetworkInterfaceType;
        if (type != System.Net.NetworkInformation.NetworkInterfaceType.Ethernet &&
            type != System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
            continue;

        // Skip virtual/loopback-style adapters
        string desc = nic.Description.ToLowerInvariant();
        if (desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vEthernet".ToLower()) ||
            desc.Contains("loopback") || desc.Contains("docker") || desc.Contains("vmware"))
            continue;

        var props = nic.GetIPProperties();
        bool hasGateway = props.GatewayAddresses != null && props.GatewayAddresses.Count > 0;

        foreach (var ua in props.UnicastAddresses)
        {
            if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                continue;
            var ip = ua.Address.ToString();
            // Skip APIPA/link-local
            if (ip.StartsWith("169.254.")) continue;

            if (hasGateway)
            {
                localIPAddress = ip;
                return localIPAddress;
            }
        }
    }
    // Fallback: first non-loopback IPv4
    foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
    {
        foreach (var ua in nic.GetIPProperties().UnicastAddresses)
        {
            if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                !ua.Address.ToString().StartsWith("169.254.") &&
                !IPAddress.IsLoopback(ua.Address))
            {
                localIPAddress = ua.Address.ToString();
                return localIPAddress;
            }
        }
    }
        return "127.0.0.1";
    }

    private void HandlePlayerJoined(PlayerSession player)
    {
        AddPlayerToLobby(player.playerName, player.playerNetworkId);
        UpdateStartButton();
    }

    private void HandlePlayerNameChanged(ulong playerNetworkId, string playerName)
    {
        Debug.Log($"HandlePlayerNameChanged: playerNetworkId={playerNetworkId}, playerName={playerName}");
        var slot = lobbySlots.Find(s => s.playerNetworkId == playerNetworkId);
        if (slot == null)
        {
            Debug.LogWarning($"Slot not found for {playerNetworkId}, adding new slot");
            // Late-arriving name update before join event; add slot then render
            AddPlayerToLobby(playerName, playerNetworkId);
            return;
        }

        slot.name = playerName;
        Debug.Log($"Updated slot name to {playerName}, calling UpdatePlayerSlots");
        UpdatePlayerSlots();
    }

    private void HandlePlayerLeft(ulong playerNetworkId)
    {
        lobbySlots.RemoveAll(slot => slot.playerNetworkId == playerNetworkId);
        UpdatePlayerSlots();
        UpdateStartButton();
    }

    private void HandlePlayerReadyChanged(ulong playerNetworkId, bool ready)
    {
        var slot = lobbySlots.Find(s => s.playerNetworkId == playerNetworkId);
        if (slot != null) slot.isReady = ready;

        var netMan = Unity.Netcode.NetworkManager.Singleton;
        if (netMan != null && playerNetworkId == Unity.Netcode.NetworkManager.ServerClientId)
        {
            // Host ready state also updates the host slot UI
            UpdateHostSlotUI(slot);
        }

        UpdatePlayerSlots();
        UpdateStartButton();
    }

    public void AddPlayerToLobby(string playerName, ulong playerNetworkId)
    {
        var netMan = Unity.Netcode.NetworkManager.Singleton;
        ulong hostId = netMan != null ? Unity.Netcode.NetworkManager.ServerClientId : playerNetworkId;
        bool isHost = playerNetworkId == hostId;

        // Capacity check for non-host slots only
        int nonHostCount = lobbySlots.FindAll(s => s.playerNetworkId != hostId).Count;
        if (!isHost && nonHostCount >= playerSlotObjects.Length)
        {
            Debug.LogWarning("Lobby is full; cannot add more players.");
            return;
        }

        // Avoid duplicates
        if (lobbySlots.Exists(slot => slot.playerNetworkId == playerNetworkId))
        {
            return;
        }

        var newPlayer = new PlayerLobbySlot
        {
            isConnected = true,
            name = playerName,
            playerNetworkId = playerNetworkId,
            isReady = false
        };

        lobbySlots.Add(newPlayer);
        // Sort by playerNetworkId to maintain consistent order across all clients
        lobbySlots.Sort((a, b) => a.playerNetworkId.CompareTo(b.playerNetworkId));

        UpdatePlayerSlots();

        // Local player can ready up
        if (netMan != null && playerNetworkId == netMan.LocalClientId)
        {
            localReadyCheckbox.interactable = true;
        }

    }

    private void UpdatePlayerSlots()
    {
        var netMan = Unity.Netcode.NetworkManager.Singleton;
        ulong hostId = netMan != null ? Unity.Netcode.NetworkManager.ServerClientId : 0;

        // Update the dedicated host slot UI
        var hostSlot = lobbySlots.Find(s => s.playerNetworkId == hostId);
        UpdateHostSlotUI(hostSlot);

        // Render non-host players into Player2+ slots
        var nonHostSlots = lobbySlots.FindAll(s => s.playerNetworkId != hostId);
        Debug.Log($"UpdatePlayerSlots: nonHostSlots.Count={nonHostSlots.Count}");
        for (int i = 0; i < playerSlotObjects.Length; i++)
        {
            if (i < nonHostSlots.Count)
            {
                playerSlotObjects[i].SetActive(true);
                var slot = nonHostSlots[i];
                Debug.Log($"Slot {i}: name={slot.name}");

                Transform nameTfm = playerSlotObjects[i].transform.Find("Player" + (i + 2) + "Name");
                if (nameTfm != null)
                {
                    nameTfm.GetComponent<TextMeshProUGUI>().text = slot.name;
                    Debug.Log($"Set Player{(i + 2)}Name to {slot.name}");
                }
                else
                {
                    Debug.LogWarning($"Could not find Player{(i + 2)}Name transform");
                }

                Transform readyTfm = playerSlotObjects[i].transform.Find("Player" + (i + 2) + "Toggle");
                if (readyTfm != null)
                {
                    Toggle readyToggle = readyTfm.GetComponent<Toggle>();
                    // Remove old listeners to avoid duplicate calls
                    readyToggle.onValueChanged.RemoveAllListeners();
                    // Set without notifying to avoid triggering the callback
                    readyToggle.SetIsOnWithoutNotify(slot.isReady);
                    // Make toggle interactable for the local client
                    bool isLocalPlayer = netMan != null && slot.playerNetworkId == netMan.LocalClientId;
                    readyToggle.interactable = isLocalPlayer;
                    // Re-add listener only if this is the local player
                    if (isLocalPlayer)
                    {
                        readyToggle.onValueChanged.AddListener(OnReadyToggle);
                    }
                }
            }
            else
            {
                playerSlotObjects[i].SetActive(false);
            }
        }
    }

    private void UpdateHostSlotUI(PlayerLobbySlot hostSlot)
    {
        if (hostSlotObject == null) return;

        if (hostSlot != null)
        {
            hostSlotObject.SetActive(true);
            var nameTfm = hostSlotObject.transform.Find("HostName");
            if (nameTfm != null)
                nameTfm.GetComponent<TextMeshProUGUI>().text = hostSlot.name;

            if (hostReadyCheckbox != null)
            {
                // Keep host toggle in sync without re-firing the ready handler
                hostReadyCheckbox.SetIsOnWithoutNotify(hostSlot.isReady);
                hostReadyCheckbox.interactable = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer;
            }
            else
            {
                var readyTfm = hostSlotObject.transform.Find("HostToggle");
                if (readyTfm != null)
                    readyTfm.GetComponent<Toggle>().isOn = hostSlot.isReady;
            }
        }
        else
        {
            hostSlotObject.SetActive(false);
            if (hostReadyCheckbox != null)
            {
                hostReadyCheckbox.interactable = false;
            }
        }
    }

    private void UpdateStartButton()
    {
        // Need at least 2 players
        if (lobbySlots.Count < 2)
        {
            startButton.interactable = false;
            return;
        }

        // All players must be ready
        bool allReady = true;
        foreach (var slot in lobbySlots)
        {
            if (!slot.isReady)
            {
                allReady = false;
                break;
            }
        }
        //startButton.interactable = readyCount == lobbySlots.Count && readyCount >= 2;
        startButton.interactable = allReady && Unity.Netcode.NetworkManager.Singleton.IsServer;
    }

    public void CheckAndUpdatePlayerSlots()
    {
        for (int i = lobbySlots.Count - 1; i >= 0; i--)
        {
            if (!lobbySlots[i].isConnected)
            {
                lobbySlots.RemoveAt(i);
            }
        }
        int slotCount = lobbySlots.Count;
        // skip player 1 (#0)
        for (int i = 1; i < playerSlotObjects.Length; i++)
        {
            if (i < slotCount)
            {
                playerSlotObjects[i].SetActive(true);

                Transform playerNameTfm = playerSlotObjects[i].transform.Find("Player" + (i + 2) + "Name");
                if (playerNameTfm != null) 
                {
                    playerNameTfm.GetComponent<TextMeshProUGUI>().text = lobbySlots[i].name;
                }
                else
                {
                    Debug.LogError("Player name object not found for slot " + (i + 2));
                }
            }
            else
            {
                playerSlotObjects[i].SetActive(false);
            }
        }
    }

    public void OnReadyToggle(bool isReady)
    {
        if (isReady)
        {
            lobbyManager.LocalPlayerClickedReady();
        }
        else
        {
            lobbyManager.LocalPlayerClickedUnready();
        }
    }
    
    private void HandleGameStart()
    {
        Debug.Log("Game starting!");
    }

    public void OnStartGame()
    {
        Debug.Log("OnStartGame button clicked.");
        
        if (lobbyManager != null)
        {
            lobbyManager.HostClickedStartGame();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        var netMan = Unity.Netcode.NetworkManager.Singleton;
        
        // If we're a client and we got disconnected, reset the UI
        if (netMan != null && !netMan.IsServer && clientId == netMan.LocalClientId)
        {
            Debug.Log("Client disconnected from host - resetting UI");
            ResetLobbyUI();
        }
    }

    private void DisconnectAndShutdownNetwork()
    {
        var netMan = Unity.Netcode.NetworkManager.Singleton;
        
        if (netMan != null && netMan.IsListening)
        {
            if (netMan.IsServer)
            {
                // Host: shutdown and notify all clients
                Debug.Log("Shutting down as Host");
                lobbyManager.HostInitiatedShutdown(false);
            }
            else if (netMan.IsClient)
            {
                // Client: disconnect from server
                Debug.Log("Disconnecting as Client");
                netMan.Shutdown();
            }
        }
    }

    public void OnMenuButton()
    {
        Debug.Log("Menu button clicked - disconnecting from network");
        
        DisconnectAndShutdownNetwork();
        // Small delay to allow shutdown to complete (?)
        //Invoke(nameof(LoadMainMenu), 0.1f);
        
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }

    public void OnCancelButton()
    {
        Debug.Log("Cancel button clicked - disconnecting from network");
        
        DisconnectAndShutdownNetwork();
        
        // Reset UI state
        ResetLobbyUI();
    }

    private void ResetLobbyUI()
    {
        // Re-enable connection buttons
        hostButton.interactable = true;
        hostLocalButton.interactable = true;
        hostInternetButton.interactable = true;
        connectButton.interactable = true;
        connectLocalButton.interactable = true;
        
        // Reset visibility
        localPlayerObject.SetActive(true);
        hostSlotObject.SetActive(false);
        
        // Clear lobby slots
        lobbySlots.Clear();
        
        // Reset ready checkboxes
        localReadyCheckbox.interactable = false;
        localReadyCheckbox.isOn = false;
        if (hostReadyCheckbox != null)
        {
            hostReadyCheckbox.interactable = false;
            hostReadyCheckbox.isOn = false;
        }
        
        // Hide all player slots
        for (int i = 0; i < playerSlotObjects.Length; i++)
        {
            playerSlotObjects[i].SetActive(false);
        }
        
        // Disable start button
        startButton.interactable = false;
    }

    public void OnHost()
    {
        string ipAddress = hostIPInputField.text;
        string playerName = playerNameInputField.text;

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogError("Please enter a player name");
            return;
        }

        if (!IsValidIP(ipAddress))
        {
            Debug.LogError("Please enter a valid host IP address");
            return;
        }

        Debug.Log("Host button clicked. IP: " + ipAddress + " Name: " + playerName);

        hostButton.interactable = false;
        hostLocalButton.interactable = false;
        hostInternetButton.interactable = false;
        connectButton.interactable = false;
        connectLocalButton.interactable = false;

        var netMan = Unity.Netcode.NetworkManager.Singleton;
        if (netMan != null)
        {
            var transport = netMan.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                Debug.LogError("UnityTransport not found on NetworkManager");
                return;
            }

            string listenAddress = IPAddress.IsLoopback(IPAddress.Parse(ipAddress)) ? "127.0.0.1" : "0.0.0.0";
            transport.SetConnectionData(listenAddress, defaultPort);

            netMan.StartHost();
            Debug.Log("Started as Host @ " + listenAddress + ":" + defaultPort);

            // Swap UI: hide local placeholder, show host slot
            localPlayerObject.SetActive(false);
            hostSlotObject.SetActive(true);

            ulong localClientId = netMan.LocalClientId;
            lobbyManager.SetPlayerName(localClientId, playerName);

            // Ensure host entry exists at index 0
            if (!lobbySlots.Exists(s => s.playerNetworkId == localClientId))
            {
                lobbySlots.Insert(0, new PlayerLobbySlot
                {
                    isConnected = true,
                    name = playerName,
                    playerNetworkId = localClientId,
                    isReady = false
                });
            }
            else
            {
                var hostSlot = lobbySlots.Find(s => s.playerNetworkId == localClientId);
                hostSlot.name = playerName;
            }

            UpdateHostSlotUI(lobbySlots[0]);
            localReadyCheckbox.interactable = false;
            if (hostReadyCheckbox != null)
            {
                hostReadyCheckbox.interactable = true;
                hostReadyCheckbox.SetIsOnWithoutNotify(false);
            }
            UpdatePlayerSlots();
        }
    }

    public void OnHostInternet()
    {
        if (publicIPAddress == "0.0.0.0")
        {
            Debug.LogError("Public IP not yet fetched. Wait a moment and try again.");
            return;
        }
        hostIPInputField.text = publicIPAddress;
        OnHost(); // Reuse existing host logic
    }

    public void OnHostLocal()
    {
        hostIPInputField.text = "127.0.0.1";
        OnHost(); // Same as OnHost for localhost
    }

    public void OnConnect()
    {
        string ipAddress = hostIPInputField.text;
        string playerName = playerNameInputField.text;

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogError("Please enter a player name");
            return;
        }

        if (string.IsNullOrEmpty(ipAddress))
        {
            Debug.LogError("Please enter a host IP address");
            return;
        }

        Debug.Log("Connect: IP " + ipAddress + " Name: " + playerName);

        hostButton.interactable = false;
        hostLocalButton.interactable = false;
        hostInternetButton.interactable = false;
        connectButton.interactable = false;
        connectLocalButton.interactable = false;

        var netMan = Unity.Netcode.NetworkManager.Singleton;
        if (netMan != null)
        {
            var transport = netMan.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                Debug.LogError("UnityTransport not found on NetworkManager");
                return;
            }

            transport.SetConnectionData(ipAddress, defaultPort);

            netMan.StartClient();
            Debug.Log("Started as Client connecting to " + ipAddress + ":" + defaultPort);

            // Swap UI: hide local placeholder, show host slot (will be populated when data arrives)
            localPlayerObject.SetActive(false);
            hostSlotObject.SetActive(true);
            localReadyCheckbox.interactable = false;
            if (hostReadyCheckbox != null)
            {
                hostReadyCheckbox.interactable = false;
                hostReadyCheckbox.SetIsOnWithoutNotify(false);
            }

            StartCoroutine(SetPlayerNameAfterConnection(playerName));
        }
    }

    public void OnConnectLocal()
    {
        hostIPInputField.text = "127.0.0.1";
        OnConnect(); // Same as OnConnect for localhost
    }

    private System.Collections.IEnumerator SetPlayerNameAfterConnection(string playerName)
    {
        // Wait for client to connect
        yield return new WaitUntil(() => Unity.Netcode.NetworkManager.Singleton.IsConnectedClient);
        
        ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        lobbyManager.SetPlayerName(localClientId, playerName);
    }


    private bool IsValidIP(string ip)
    {
        System.Net.IPAddress address;
        return System.Net.IPAddress.TryParse(ip, out address);
    }
}

