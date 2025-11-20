using System.Collections.Generic;
using UnityEngine;

public class PlayerX : MonoBehaviour
{
    
    public string playerName = "PlayerX";
    public int playerId = -1;
    //[SerializeField] private HandX playerHand = null;

    public CardObject[] hand = new CardObject[6];

    // Network connection info?
    // NetManager...

    public List<CardObject> scorePile = null;

    void Awake()
    {
        //playerHand = new HandX();
        scorePile = new List<CardObject>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { }

    // Update is called once per frame
    void Update()
    { }

    public int GetIndexOfCard(CardPOD cardPOD)
    {
        if (hand == null)
        {
            Debug.LogError("PlayerX->IndexOfCard(): hand is null for player " + playerId);
            return -1;
        }
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i].cardPOD.cardID == cardPOD.cardID)
                return i;
        }
        return -1;
    }
}
