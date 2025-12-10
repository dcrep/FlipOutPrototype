using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [SerializeField] private List<PlayerLobbySlot> lobbySlots = new List<PlayerLobbySlot>();

    public GameObject[] playerSlotObjects;

    public Button startButton;

    public string debugPlayerName = "PlayerX";
    public bool debugAddPlayer = false;

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
}

