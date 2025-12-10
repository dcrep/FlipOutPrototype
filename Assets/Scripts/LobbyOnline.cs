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
        public int playerId = -1;
        public bool isReady = false;
    }

    public TMP_InputField hostIPInputField;
    public Button hostButton, connectButton;

    [SerializeField] private List<PlayerLobbySlot> lobbySlots = new List<PlayerLobbySlot>();

    public GameObject[] playerSlotObjects;

    public Button startButton;

    public string debugPlayerName = "PlayerX";
    public bool debugAddPlayer = false;

    private string localIPAddress = "127.0.0.1";

    public string GetLocalIPAddress()
    {
        string localIP = null;
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
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
        return localIP;
    }

    void Awake()
    {
        string localIP = GetLocalIPAddress();
        Debug.Log("Local IP: " + localIP);
        hostIPInputField.text = localIP;
    }

    private void Start()
    {
        startButton.onClick.AddListener(OnStartGame);

        AddPlayerToLobby("LocalPlayer1", 0);

        for (int i = 1; i < playerSlotObjects.Length; i++)
        {
            playerSlotObjects[i].SetActive(false);
        }
    }

    void Update()
    {
        CheckAndUpdatePlayerSlots();
        if (debugAddPlayer)
        {
            debugAddPlayer = false;
            AddPlayerToLobby(debugPlayerName + (lobbySlots.Count + 1), lobbySlots.Count);
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
    }

    public void AddPlayerToLobby(string playerName, int playerId)
    {
        if (lobbySlots.Count >= 4)
        {
            Debug.LogWarning("Lobby is full; cannot add more players.");
            return;
        }
        int numSlots = lobbySlots.Count;

        PlayerLobbySlot newPlayer = new PlayerLobbySlot();
        newPlayer.isConnected = true;
        newPlayer.name = playerName;
        newPlayer.playerId = playerId;
        newPlayer.isReady = false;
        lobbySlots.Add(newPlayer);

        playerSlotObjects[numSlots].SetActive(true);
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

    public void OnCheckBoxToggle()
    {
        lobbySlots[0].isReady = !lobbySlots[0].isReady;
    }

    public void OnStartGame()
    {
        var activePlayers = new List<string>();

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
        }

        //GameManager.Instance.StartOnlineGame(activePlayers.Count, activePlayers.ToArray());
    }

    public void OnMenuButton()
    {
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }

    public void OnHost()
    {
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
        //! Commence with the HOSTing
    }

    public void OnConnect()
    {
        string ipAddress = hostIPInputField.text;
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
        //! Commence with the CONNECTing
    }


    private bool IsValidIP(string ip)
    {
        System.Net.IPAddress address;
        return System.Net.IPAddress.TryParse(ip, out address);
    }
}

