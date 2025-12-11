using UnityEngine;
using UnityEngine.UI;

public class ResultsMenuUI : MonoBehaviour
{

    public Text wonText;
    public Text[] playersScoreText;

    void Awake()
    {
        wonText.text = "Player " + GameManager.Instance.finalPlayers[GameManager.Instance.finalWinnerPlayerNum] + "Won";
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
