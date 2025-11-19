using UnityEngine;

public class PlayerX : MonoBehaviour
{
    
    public string playerName = "PlayerX";
    public int playerId = -1;
    [SerializeField] private HandX playerHand = null;

    void Awake()
    {
        playerHand = new HandX();
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
