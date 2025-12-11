using UnityEngine;
using UnityEngine.UI;

public class ResultsMenuUI : MonoBehaviour
{

    public Text wonText;
    public Text[] playersScoreText;

    void Awake()
    {
        int totalPlayers = GameManager.Instance.finalScoredPlayers;

        wonText.text = "Player " + GameManager.Instance.finalPlayers[GameManager.Instance.finalWinnerPlayerNum] + "Won";

        for (int i = 0; i < GameManager.Instance.finalScoredPlayers; i++)
        {
            playersScoreText[i].text = GameManager.Instance.finalPlayers[i] + ": " + GameManager.Instance.finalScores[i];
        }
        if (totalPlayers < 5)
        {
            for (int i = totalPlayers; i < 5; i++)
            {
                playersScoreText[i].text = "--";
            }
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { }

    // Update is called once per frame
    void Update()
    { }

    public void OnMenuButton()
    {
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }
}
