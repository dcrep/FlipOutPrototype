using UnityEngine;
using UnityEngine.UI;

public class ResultsMenuUI : MonoBehaviour
{

    public Text wonText;
    public Text[] playersScoreText;

    void Awake()
    {
        //GameStateClient.currentMultiplayerMode;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameResults results = GameStateClient.gameResults;
        int totalPlayers = results.numberOfPlayers;

        wonText.text = "Player " + results.playerNames[results.winningPlayerNum] + " Won";

        for (int i = 0; i < results.numberOfPlayers; i++)
        {
            playersScoreText[i].text = results.playerNames[i] + ": " + results.finalScores[i];
        }
        if (totalPlayers < 5)
        {
            for (int i = totalPlayers; i < 5; i++)
            {
                playersScoreText[i].text = "";
            }
        }
    }

    // Update is called once per frame
    void Update()
    { }

    public void OnMenuButton()
    {
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }
}
