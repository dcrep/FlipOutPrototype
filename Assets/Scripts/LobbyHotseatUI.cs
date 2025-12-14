using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyHotseatUI : MonoBehaviour
{
    [System.Serializable]
    public class PlayerSlot
    {
        public TMP_InputField nameInput;
        public Toggle enableToggle;
    }

    public PlayerSlot[] playerSlots; // Assign in Inspector
    public Button startButton;

    private void Start()
    {
        startButton.onClick.AddListener(OnStartGame);
        playerSlots[0].nameInput.text = PlayerPreferences.Instance.playerName;
    }

    void OnStartGame()
    {
        var activePlayers = new List<string>();

        foreach (var slot in playerSlots)
        {
            if (slot.enableToggle.isOn) // Only include if checked
            {
                string playerName = string.IsNullOrEmpty(slot.nameInput.text)
                    ? "Player" + (System.Array.IndexOf(playerSlots, slot) + 1) // fallback name
                    : slot.nameInput.text;
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

        GameManager.Instance.StartHotseatGame(activePlayers.Count, activePlayers.ToArray());
        //GameManager.Instance.LoadScene(Scenes.Game);
    }
    public void OnMenuButton()
    {
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }
}

