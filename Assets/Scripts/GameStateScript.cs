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

// "GameState" conflicts with enum GameState (probably should be AppState)
[System.Serializable]
public class GameStateScript //: MonoBehaviour
{
    public List<CardPOD> serverDrawPile = null;
    //public CardPOD topDrawCard = null;

// Each color appears 6 times (including both sides)
    private CardPOD[] deckCardRange = new CardPOD[15]
    {
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.red },
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.green },
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.blue },
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.purple },
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.yellow },

        new() { cardSideAColor = cardColor.green, cardSideBColor = cardColor.green },
        new() { cardSideAColor = cardColor.green, cardSideBColor = cardColor.blue },
        new() { cardSideAColor = cardColor.green, cardSideBColor = cardColor.purple },
        new() { cardSideAColor = cardColor.green, cardSideBColor = cardColor.yellow },

        new() { cardSideAColor = cardColor.blue, cardSideBColor =  cardColor.blue },
        new() { cardSideAColor = cardColor.blue, cardSideBColor =  cardColor.purple },
        new() { cardSideAColor = cardColor.blue, cardSideBColor =  cardColor.yellow },

        new() { cardSideAColor = cardColor.purple, cardSideBColor =  cardColor.purple },
        new() { cardSideAColor = cardColor.purple, cardSideBColor =  cardColor.yellow },

        new() { cardSideAColor = cardColor.yellow, cardSideBColor =  cardColor.yellow }
    };
    [SerializeField] private CardPOD[] deckPure = new CardPOD[90];

    //private Vector3 deckOffscreenPosition = new Vector3(-1000, -1000, 0);

    // Server-side initialization
    public void InitServer()
    {
        // Set up Pure deck
        int index = 0;
        for (int i = 0; i < deckCardRange.Length; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                deckPure[i * 6 + j] = deckCardRange[index];
                index++;
                if (index == deckCardRange.Length)
                    index = 0;
            }
        }
        serverDrawPile = new List<CardPOD>();
        // Shuffle deck into 'serverDrawPile'
        InitAndShuffleDeck();
    }

    /*public void InitClient()
    {
    }*/

    public void Cleanup()
    {
        serverDrawPile = null;
        //topDrawCard = null;
        if (serverDrawPile != null)
        {
            serverDrawPile.Clear();
            serverDrawPile = null;
        }
    }

    public CardPOD DrawCard()
    {
        // end game?
        if (serverDrawPile.Count == 0)
        {
            Debug.LogError("DrawCard: draw pile empty!");
            return null;
        }

        CardPOD drawnPOD = serverDrawPile[0].Clone();
        serverDrawPile.RemoveAt(0);
        return drawnPOD;
    }


    // Draw numCards from draw pile - server side
    public List<CardPOD> DrawCards(int numCards)
    {
        if (numCards < 0 || numCards > 6)
        {
            Debug.LogError("DrawCards: invalid numCards " + numCards);
            return null;
        }
        
        // Endgame?
        if (serverDrawPile.Count < numCards)
        {
            Debug.Log("DrawCards: requested " + numCards + " cards, only " + serverDrawPile.Count + " left in draw pile - end game reached");
            return null;
        }

        List<CardPOD> drawnCards = new List<CardPOD>();
        for (int i = 0; i < numCards; i++)
        {
            CardPOD drawnPOD = serverDrawPile[0].Clone();
            serverDrawPile.RemoveAt(0);
            drawnCards.Add(drawnPOD);
        }
        return drawnCards;
    }

    // Each person has their own score pile
    //public List<CardObject> scorePile = null;

    // PlayerX includes id, HandX, and scoring pile for that person
    [SerializeField] private PlayerX[] players = new PlayerX[5];

    //public DrawCard
    //public GetHand

    void ActionTaken(TurnAction action)
    {
        // Update state based on action
    }



    // Server: Simple serverDrawPile initialization and shuffle
    // NOTE: plain old data - no game objects here
    void InitAndShuffleDeck()
    {
        int cardID = 0;        
        List<CardPOD> deckPull = new();
        // Have to manually clone each card to avoid reference issues
        // If we change CardPOD to a struct (value semantics), this can be simplified to a ToList conversion
        foreach(var card in deckPure)
        {
            deckPull.Add(card.Clone());
        }
        serverDrawPile.Clear();
        System.Random rand = new();

        while (deckPull.Count > 0)
        {
            int index = rand.Next(0, deckPull.Count);
            // More randomness..
            deckPull[index].facing = Random.value > 0.5f ? cardFace.sideA : cardFace.sideB;

            CardPOD cardCopy = deckPull[index].Clone(); // not necessary since Cloned above..
            cardCopy.cardID = cardID;
            cardCopy.state = cardState.drawPile;    // by default
            serverDrawPile.Add(cardCopy);
            deckPull.RemoveAt(index);
            cardID++;
        }

        // Ensure correct number of each card (6 * 6 = 36)
        if (false)
        {
            int totalRed = 0, totalGreen = 0, totalBlue = 0, totalPurple = 0, totalYellow = 0;
            for (int i = 0; i < serverDrawPile.Count; i++)
            {
                switch (serverDrawPile[i].cardSideAColor)
                {
                    case cardColor.red:
                        totalRed++;
                        break;
                    case cardColor.green:
                        totalGreen++;
                        break;
                    case cardColor.blue:
                        totalBlue++;
                        break;
                    case cardColor.purple:
                        totalPurple++;
                        break;
                    case cardColor.yellow:
                        totalYellow++;
                        break;
                }
                switch (serverDrawPile[i].cardSideBColor)
                {
                    case cardColor.red:
                        totalRed++;
                        break;
                    case cardColor.green:
                        totalGreen++;
                        break;
                    case cardColor.blue:
                        totalBlue++;
                        break;
                    case cardColor.purple:
                        totalPurple++;
                        break;
                    case cardColor.yellow:
                        totalYellow++;
                        break;
                }
            }
            Debug.Log($"Deck Composition - Red: {totalRed}, Green: {totalGreen}, Blue: {totalBlue}, Purple: {totalPurple}, Yellow: {totalYellow}");
        }
    }

}
