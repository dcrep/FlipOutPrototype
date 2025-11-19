using System.Collections.Generic;
using UnityEngine;

public class PlayerX : MonoBehaviour
{
    
    public string playerName = "PlayerX";
    public int playerId = -1;
    [SerializeField] private HandX playerHand = null;

    // Network connection info?
    // NetManager...

    public List<CardObject> scorePile = null;

    void Awake()
    {
        playerHand = new HandX();
        scorePile = new List<CardObject>();
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
