using UnityEngine;
using System.Collections.Generic;

// FlipOut Game rules @ https://www.ultraboardgames.com/flipout/game-rules.php
// TurnActions - 2 per turn!
// Flip - choose your own or opponent's card to flip
// Switch - choose 2 cards to switch positions - can be your own or opponent's
// Swap1 - swap one of your cards with another player's - no flipping
// Swap2 requires 2 adjacent same color cards from each player
// Score/Swipe require drawing more cards after action is complete
public enum TurnAction
{
    Flip,      // flip your own or opponent's card
    Switch,    // switch one card's position with another - your own or opponent's hand
    Swap1,     // swap one of your cards with another player's - WITHOUT flipping either card
    Swap2,     // swap 2 adjacent same-color cards of yours with another player's 2 adjacent same-color cards
               // (doesn't have to be the same colors as yours)
    Score,     // score a set of 4 to 6 adjacent same-color cards from your hand, redraw up to 6
    Swipe      // score a set of 4 to 6 adjacent same-color cards from another player's hand
               // - you score total-1 (in scoring pile), they score 1 (in their scoring pile), both redraw up to 6
}

public class GameStateScript : MonoBehaviour
{
    public List<CardObject> drawPile = null;

    // Each person has their own score pile
    //public List<CardObject> scorePile = null;

    // PlayerX includes id, HandX, and scoring pile for that person
    [SerializeField] private PlayerX[] players = new PlayerX[5];

    void Awake()
    {
        // This will be set after deck creation and shuffle:
        //drawPile = new List<CardObject>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { }

    // Update is called once per frame
    void Update()
    { }

    //public DrawCard
    //public GetHand

    void ActionTaken(TurnAction action)
    {
        // Update state based on action
    }

}
