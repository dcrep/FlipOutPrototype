using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

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

    public TMP_InputField hostIPInputField;
    public TMP_InputField playerNameInputField;
    public Button hostButton, hostLocalButton, connectButton;

    public Button startButton;
    public Button mainMenuButton;
    public Toggle readyCheckbox;

    public GameObject[] playerSlotObjects;

    public string debugPlayerName = "PlayerX";
    public bool debugAddPlayer = false;

    private string localIPAddress = "127.0.0.1";
    [SerializeField] private string publicIPAddress = "0.0.0.0";

    void Awake()
    {
        string localIP = GetLocalIPAddress();
        Debug.Log("Local IP: " + localIP);
        hostIPInputField.text = localIP;
        StartCoroutine(FetchPublicIP());
    }

    private void Start()
    {
        hostButton.onClick.AddListener(OnHost);
        hostLocalButton.onClick.AddListener(OnHostLocal);
        connectButton.onClick.AddListener(OnConnect);
        startButton.onClick.AddListener(OnStartGame);
        readyCheckbox.onValueChanged.AddListener(OnReadyToggle);
        mainMenuButton.onClick.AddListener(OnMenuButton);

        // Subscribe to lobby manager events
        lobbyManager.OnPlayerJoined += HandlePlayerJoined;
        lobbyManager.OnPlayerNameChanged += HandlePlayerNameChanged;
        lobbyManager.OnPlayerLeft += HandlePlayerLeft;
        lobbyManager.OnPlayerReadyChanged += HandlePlayerReadyChanged;
        lobbyManager.OnAllPlayersReady += HandleGameStart;

        //if (readyCheckbox != null)
        //readyCheckbox.interactable = false;

        //AddPlayerToLobby("LocalPlayer", 0);

        //AddPlayerToLobby("LocalPlayer1", 0);

        for (int i = 1; i < playerSlotObjects.Length; i++)
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
        /*string localIP = null;
        var addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
        
        // Debug: log all addresses
        foreach (var addr in addresses)
        {
            Debug.Log("Found address: " + addr.ToString() + " Family: " + addr.AddressFamily);
        }
        
        foreach (var ip in addresses)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        if (localIP == null)
            localIP = "127.0.0.1";

        localIPAddress = localIP;
        return localIP;*/
    }

    private void HandlePlayerJoined(PlayerSession player)
    {
        AddPlayerToLobby(player.playerName, player.playerNetworkId);
        UpdateStartButton();
    }

    private void HandlePlayerNameChanged(ulong playerNetworkId, string playerName)
    {
        var slot = lobbySlots.Find(s => s.playerNetworkId == playerNetworkId);
        if (slot != null)
        {
            slot.name = playerName;
            UpdatePlayerSlots();
        }
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
        UpdatePlayerSlots();
        UpdateStartButton();
    }

    public void AddPlayerToLobby(string playerName, ulong playerNetworkId)
    {
        if (lobbySlots.Count >= playerSlotObjects.Length)   // 4 [5 in future maybe]
        {
            Debug.LogWarning("Lobby is full; cannot add more players.");
            return;
        }

        // Check if player already exists
        if (lobbySlots.Exists(slot => slot.playerNetworkId == playerNetworkId))
        {
            return;
        }

        int numSlots = lobbySlots.Count;

        var newPlayer = new PlayerLobbySlot
        {
            isConnected = true,
            name = playerName,
            playerNetworkId = playerNetworkId,
            isReady = false
        };

        lobbySlots.Add(newPlayer);

        playerSlotObjects[numSlots].SetActive(true);
        UpdatePlayerSlots();
    
        // Enable ready checkbox if this is the local player
        if (playerNetworkId == Unity.Netcode.NetworkManager.Singleton.LocalClientId)
        {
            readyCheckbox.interactable = true;
        }
    }

    private void UpdatePlayerSlots()
    {
        for (int i = 0; i < playerSlotObjects.Length; i++)
        {
            if (i < lobbySlots.Count)
            {
                playerSlotObjects[i].SetActive(true);
                var slot = lobbySlots[i];
                
                Transform nameTfm = playerSlotObjects[i].transform.Find("Player" + (i + 1) + "Name");
                if (nameTfm != null)
                    nameTfm.GetComponent<TextMeshProUGUI>().text = slot.name;

                Transform readyTfm = playerSlotObjects[i].transform.Find("Player" + (i + 1) + "Toggle");
                if (readyTfm != null)
                    readyTfm.GetComponent<Toggle>().isOn = slot.isReady;
                //if (readyTfm != null)
                //    readyTfm.GetComponent<TextMeshProUGUI>().text = slot.isReady ? "✓" : "○";
            }
            else
            {
                playerSlotObjects[i].SetActive(false);
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

        //int readyCount = 0;
        //foreach (var slot in lobbySlots)
        //{
        //    if (slot.isConnected && slot.isReady) readyCount++;
        //}

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

                Transform playerNameTfm = playerSlotObjects[i].transform.Find("Player" + (i + 1) + "Name");
                if (playerNameTfm != null) 
                {
                    playerNameTfm.GetComponent<TextMeshProUGUI>().text = lobbySlots[i].name;
                }
                else
                {
                    Debug.LogError("Player name object not found for slot " + (i + 1));
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
        //if (lobbySlots.Count > 0)
        //{
        //    lobbySlots[0].isReady = !lobbySlots[0].isReady;
        //    lobbyManager.LocalPlayerClickedReady();
        //}
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

        /*var activePlayers = new List<string>();

        for (int i = 0; i < lobbySlots.Count; i++)
        {
            if (lobbySlots[i].isReady) // Only include if checked
            {
                string playerName = string.IsNullOrEmpty(lobbySlots[i].name)
                    ? "Player" + (i + 1) // fallback name
                    : lobbySlots[i].name;
                activePlayers.Add(playerName);
            }
        }

        if (activePlayers.Count < 2)
        {
            Debug.LogWarning("At least two players must be enabled to start the game.");
            return;
        }

        // Debug: print active players
        foreach (var name in activePlayers)
        {
            Debug.Log("Active Player: " + name);
        }*/

        //GameManager.Instance.StartOnlineGame(activePlayers.Count, activePlayers.ToArray());
    }

    public void OnMenuButton()
    {
        Debug.Log("Menu button clicked - disconnecting from network");
        
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            // Host: tell all clients to disconnect and load menu
            lobbyManager.HostInitiatedShutdown();
            // HostInitiatedShutdown's ClientRpc will handle loading MainMenu for everyone
        }
        else
        {
            // Client: just disconnect and load menu
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }
        }
        // Small delay to allow shutdown to complete
        //Invoke(nameof(LoadMainMenu), 0.1f);
        
        GameManager.Instance.LoadScene(Scenes.MainMenu);
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

        Debug.Log("Host button clicked. IP: " + ipAddress + " Name: " + playerName);
        
        hostButton.interactable = false;
        hostLocalButton.interactable = false;
        connectButton.interactable = false;

        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.StartHost();
            Debug.Log("Started as Host");
            
            // Set local player name
            ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            lobbyManager.SetPlayerName(localClientId, playerName);
        }
        /*
        string ipAddress = hostIPInputField.text;
        Debug.Log("Host button clicked. IP Address: " + ipAddress);
        
        if (ipAddress == "127.0.0.1")
        {
            Debug.Log("Hosting on localhost @ 127.0.0.1");
        }
        else
        {
            string localIP = GetLocalIPAddress();
            if (ipAddress == localIP)
            {
                Debug.Log("Hosting on local IP address @ " + localIP);
            }
            else
            {
                Debug.LogError("Host IP address " + ipAddress + " does not match localhost 127.0.0.1 or local IP " + localIP);
            }
        }
        hostButton.interactable = false;
        connectButton.interactable = false;
        //! Commence with the HOSTing
        
        // Start the host (server + client)
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.StartHost();
            Debug.Log("Started as Host");
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton is null - ensure NetworkManager exists in scene");
        }*/
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
        connectButton.interactable = false;

        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.StartClient();
            Debug.Log("Started as Client connecting to " + ipAddress);
            
            // Set local player name after connection
            StartCoroutine(SetPlayerNameAfterConnection(playerName));
        }
        /*string ipAddress = hostIPInputField.text;
        Debug.Log("Connect button clicked. IP Address: " + ipAddress);
        if (ipAddress == localIPAddress && ipAddress != "127.0.0.1")
        {
            Debug.LogError("Attempting to connect to local network IP address. Please use 127.0.0.1");
            return;
        }
        else if (ipAddress == "127.0.0.1")
        {
            Debug.Log("Connecting to localhost @ 127.0.0.1");
        }
        else
        {
            if (IsValidIP(ipAddress))
            {
                Debug.Log("Connecting to remote host @ " + ipAddress);
            }
            else
            {
                Debug.LogError("Invalid IP address: " + ipAddress);
                return;
            }
            Debug.Log("Connecting to remote host @ " + ipAddress);
        }
        hostButton.interactable = false;
        connectButton.interactable = false;
        //! Commence with the CONNECTing
        var transport = Unity.Netcode.NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(ipAddress, 7777); // 7777 is default port
        }      
        // Start the client
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            // Set the connection address if needed (depends on your transport setup)
            // For Unity Transport, you'd set the connection data here
            Unity.Netcode.NetworkManager.Singleton.StartClient();
            Debug.Log("Started as Client, attempting to connect to " + ipAddress);
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton is null - ensure NetworkManager exists in scene");
        }*/
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

