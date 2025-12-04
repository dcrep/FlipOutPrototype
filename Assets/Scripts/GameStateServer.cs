using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using NUnit.Framework;
using System;
using System.Linq;

// FlipOut Game rules @ https://www.ultraboardgames.com/flipout/game-rules.php
// TurnActions - 2 per turn!
// Flip - choose your own or opponent's card to flip
// Switch - choose 2 cards to switch positions - can be your own or opponent's
// Swap1 - swap one of your cards with another player's - no flipping
// Swap2 requires 2 adjacent same color cards from each player
// Score/Swipe require drawing more cards after action is complete
[Flags]
public enum TurnAction
{
    None  = 0x00,   // invalid
    Flip   = 0x01,  // flip your own or opponent's card
    Switch = 0x02,  // switch one card's position with another - your own or opponent's hand
    Swap1  = 0x04,  // swap one of your cards with another player's - WITHOUT flipping either card
    Swap2  = 0x08,  // swap 2 adjacent same-color cards of yours with another player's 2 adjacent same-color cards
                    // (doesn't have to be the same colors as yours)
    Score  = 0x10,  // score a set of 4 to 6 adjacent same-color cards from your hand, redraw up to 6
    Swipe  = 0x20   // score a set of 4 to 6 adjacent same-color cards from another player's hand
               // - you score total-1 (in scoring pile), they score 1 (in their scoring pile), both redraw up to 6
}

// "GameState" conflicts with enum GameState (probably should be AppState)
[System.Serializable]
public class GameStateServer
{
#region Server-Data
    // Each color appears 6 times (including both sides)
    private CardPODServer[] deckCardRange = new CardPODServer[15]
    {
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.red },
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.green },
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.blue },
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.purple },
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.yellow },

        new() { cardSideAColor = CardColor.green, cardSideBColor = CardColor.green },
        new() { cardSideAColor = CardColor.green, cardSideBColor = CardColor.blue },
        new() { cardSideAColor = CardColor.green, cardSideBColor = CardColor.purple },
        new() { cardSideAColor = CardColor.green, cardSideBColor = CardColor.yellow },

        new() { cardSideAColor = CardColor.blue, cardSideBColor =  CardColor.blue },
        new() { cardSideAColor = CardColor.blue, cardSideBColor =  CardColor.purple },
        new() { cardSideAColor = CardColor.blue, cardSideBColor =  CardColor.yellow },

        new() { cardSideAColor = CardColor.purple, cardSideBColor =  CardColor.purple },
        new() { cardSideAColor = CardColor.purple, cardSideBColor =  CardColor.yellow },

        new() { cardSideAColor = CardColor.yellow, cardSideBColor =  CardColor.yellow }
    };
    [SerializeField] private CardPODServer[] deckPure = new CardPODServer[90];
    [SerializeField] private CardPODServer[] cardsInPlay = new CardPODServer[90];

    //public CardPODServer topDrawCard = null;

    public List<int> serverDrawPile = new List<int>();

    //! Usefulness?
    [SerializeField] private bool isServer = false;
#endregion

#region Server-Data-Altered-In-Client
     [SerializeField] public PlayerXServer[] playersServer = new PlayerXServer[5];
#endregion

#region Server-Client-Propagated-Data

    private int currentPlayerIndex = 0;
    private int totalPlayers = 0;

    private int currentPlayerActionsTaken = 0;

    public List<FlipOutActions> actionsTakenFull = new List<FlipOutActions>();

#endregion

    // Server-side initialization
    public void InitGameStateServer(int[] playerIds,string[] playerNames)
    {
        if (totalPlayers > 0)
        {
            Debug.LogError("InitGameStateClient: already initialized!");
            return;
        }
        isServer = true;

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
 
        // Shuffle deck into 'serverDrawPile'
        InitAndShuffleDeck();

        AssignPlayersServer(playerIds, playerNames);

        currentPlayerIndex = 0;
        currentPlayerActionsTaken = 0;
        totalPlayers = playerNames.Length;
    }

    public void Cleanup()
    {
        //topDrawCard = null;
        serverDrawPile.Clear();
        serverDrawPile = new List<int>();   // reset

        //cardsInPlay.Clear();
        cardsInPlay = new CardPODServer[90];    // reset

        actionsTakenFull.Clear();
        actionsTakenFull = new List<FlipOutActions>();  // reset

        DestroyPlayers();
        isServer = false;

        currentPlayerActionsTaken = 0;
        currentPlayerIndex = 0;

        // Shared host/client data cleanup? - no, leave this to GameManager
        //GameStateClient.CleanupClients();
    }
    void DestroyPlayers()
    {
        for (int playerNum = 0; playerNum < 5; playerNum++)
        {
            playersServer[playerNum] = new PlayerXServer(); // reset
        }
        totalPlayers = 0;
    }

    public void AssignPlayersServer(int[] playerIds, string[] playerNames)
    {
        if (!isServer)
        {
            Debug.LogError("AssignPlayersServer: not server!");
            return;
        }
        if (playerNames == null || playerNames.Length == 0)
        {
            Debug.LogError("AssignPlayersServer: playerNames is null or empty!");
            return;
        }
        totalPlayers = playerNames.Length;
        for (int playerNum = 0; playerNum < playerNames.Length; playerNum++)
        {
            playersServer[playerNum] = new PlayerXServer();
            playersServer[playerNum].playerId = playerIds[playerNum];
            playersServer[playerNum].playerName = playerNames[playerNum];
            playersServer[playerNum].playerNumber = playerNum;
            Debug.Log("Player " + playerNum + " name set to: " + playersServer[playerNum].playerName);
        }
        currentPlayerIndex = 0;
        //localPlayer1Index = 0;

        //! This needs to be done separately on client side in InitGameStateClient()
        //GameManager.Instance.gameStateClient.AssignPlayersClient(playerIds, playerNames);
    }

    public void AddUncountedActionTaken(FlipOutActions action)
    {
        actionsTakenFull.Add(action);
    }

    public void AddPlayerActionTaken(int playerNum, FlipOutActions action)
    {
        if (!isServer)
        {
            Debug.LogError("AddPlayerActionTaken: not server!");
            return;
        }
        if (playerNum != currentPlayerIndex)
        {
            Debug.LogError("AddPlayerActionTaken: NOT for current player - called for player # " + playerNum);
            return;
        }
        if (currentPlayerActionsTaken >= 2)
        {
            Debug.LogError("AddPlayerActionTaken: current player has already taken 2 actions this turn!");
            return;
        }
        actionsTakenFull.Add(action);
        // Could track per-player actions if needed
        currentPlayerActionsTaken++;
    }

    public int GetCurrentPlayerActionsTaken()
    {
        return currentPlayerActionsTaken;
    }

    public void AssignCardsToPlayerHand(int playerNum, List<CardPODServer> cards, int[] positions)
    {
        if (playerNum < 0 || playerNum >= totalPlayers)
        {
            Debug.LogError("AssignCardsToPlayerHand: invalid playerNum " + playerNum);
            return;
        }
        if (cards == null || positions == null || cards.Count != positions.Length)
        {
            Debug.LogError("AssignCardsToPlayerHand: invalid card/position list for player " + playerNum);
            return;
        }
        for (int i = 0; i < 6; i++)
        {
            playersServer[playerNum].hand[positions[i]] = cards[i];
        }
    }

    // This is in Client version, but not used so..
    //public void SetHandForPlayer(int playerNum, CardPODClient[] handCards)

    public int AdvanceToNextPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
        currentPlayerActionsTaken = 0;
        return currentPlayerIndex;
    }

    /*public void StartTurnServer()
    {
        if (!isServer)
        {
            Debug.LogError("StartTurnServer: not server!");
            return;
        }
        Debug.Log("GameState: Starting turn for player " + currentPlayerIndex + " (" + playersServer[currentPlayerIndex].playerName + ")");
        // Notify clients of turn start?
        GameManager.Instance.StartPlayerTurnClientRpc(currentPlayerIndex, GetAvailableActionsForPlayer(playersServer[currentPlayerIndex]));
    }*/

    //public void TurnActionEnd() {   }

    //public void TurnContinue() { // currentPlayerTurnActionsLeft--; GetAvailableActionsForPlayer }

    /*public void EndTurnServer()
    {
        if (!isServer)
        {
            Debug.LogError("EndTurnServer: not server!");
            return;
        }
        Debug.Log("GameState: Ending turn for player " + currentPlayerIndex + " (" + playersServer[currentPlayerIndex].playerName + ")");
        // Advance to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
        StartTurnServer();
    }*/

    public CardPODServer GetCardByID(int cardID)
    {
        if (!isServer)
        {
            Debug.LogError("GameState->GetCardByID(): not server!");
            return null;
        }
        if (cardID < 0 || cardID >= cardsInPlay.Length)
        {
            Debug.LogError("GameState->GetCardByID(): invalid cardID " + cardID);
            return null;
        }
        // 'lookup' -> cardID is simply an index into cardsInPlay array
        return cardsInPlay[cardID];
    }

    public int GetActivePlayerNumber()
    {
        return currentPlayerIndex;
    }
    public int GetTotalPlayers()
    {
        return totalPlayers;
    }

   public PlayerXServer GetActivePlayer()
    {
        if (!isServer)
        {
            Debug.LogError("GameState->GetActivePlayer(): not server!");
            return null;
        }
        return playersServer[currentPlayerIndex];
    }

    public int GetPlayerNumberByID(int playerID)
    {
        if (!isServer)
        {
            Debug.LogError("GameState->GetPlayerNumberByID(): not server!");
            return -1;
        }
        for (int playerNum = 0; playerNum < totalPlayers; playerNum++)
        {
            if (playersServer[playerNum].playerId == playerID)
                return playerNum;
        }
        Debug.LogError("GameState->GetPlayerNumberByID(): could not find playerID " + playerID);
        return -1;
    }

    public PlayerXServer GetPlayerByNumber(int playerNum)
    {
        if (playerNum < 0 || playerNum >= totalPlayers)
        {
            Debug.LogError("GameStateS->GetPlayerByNumber(): invalid playerNum " + playerNum);
            return null;
        }
        return playersServer[playerNum];
    }

    public int GetPlayerIDByNumber(int playerNum)
    {
        if (!isServer)
        {
            Debug.LogError("GameState->GetPlayerIDByNumber(): not server!");
            return -1;
        }
        if (playerNum < 0 || playerNum >= totalPlayers)
        {
            Debug.LogError("GameState->GetPlayerIDByNumber(): invalid playerNum " + playerNum);
            return -1;
        }
        return playersServer[playerNum].playerId;
    }

    public PlayerXServer GetPlayerByID(int playerID)
    {
        if (!isServer)
        {
            Debug.LogError("GameState->GetPlayerByID(): not server!");
            return null;
        }
        int index = GetPlayerNumberByID(playerID);
        if (index == -1)
        {
            Debug.LogError("GameState->GetPlayerByID(): Invalid playerID " + playerID);
            return null;
        }
        return playersServer[index];
    }

    public List<PlayerXServer> GetActivePlayers()
    {
        if (!isServer)
        {
            Debug.LogError("GameState->GetActivePlayers(): not server!");
            return null;
        }
        List<PlayerXServer> activePlayers = new List<PlayerXServer>();
        for (int playerNum = 0; playerNum < totalPlayers; playerNum++)
        {
            activePlayers.Add(playersServer[playerNum]);
        }
        return activePlayers;
    }

    public CardPODServer PeekTopDrawCard()
    {
        if (!isServer)
        {
            Debug.LogError("AssignPlayersServer: not server!");
            return null;
        }
        if (serverDrawPile.Count == 0)
        {
            Debug.LogError("GameState->PeekTopDrawCard: draw pile empty!");
            return null;
        }
        return cardsInPlay[serverDrawPile[0]];
    }

    public CardColor PeekTopDrawCardColor()
    {
        if (!isServer)
        {
            Debug.LogError("PeekTopDrawCardColor: not server!");
            return CardColor.invalid;
        }
        if (serverDrawPile.Count == 0)
        {
            Debug.LogError("PeekTopDrawCardColor: draw pile empty!");
            return CardColor.invalid;
        }
        return cardsInPlay[serverDrawPile[0]].GetFacingColor();
    }

    public int GetDrawPileCount()
    {
        if (!isServer)
        {
            Debug.LogError("GetDrawPileCount: not server!");
            return -1;
        }
        return serverDrawPile.Count;
    }

    public CardPODServer DrawCard(int playerID)
    {
        if (!isServer)
        {
            Debug.LogError("DrawCard: not server!");
            return null;
        }
        // end game?
        if (serverDrawPile.Count == 0)
        {
            Debug.LogError("DrawCard: draw pile empty!");
            return null;
        }

        CardPODServer drawnPOD = cardsInPlay[serverDrawPile[0]];
        serverDrawPile.RemoveAt(0);
        drawnPOD.ownerPlayerID = playerID;   

        return drawnPOD;
    }


    // Draw numCards from draw pile - server side
    public List<CardPODServer> DrawCards(int numCards, int playerID)
    {
        if (!isServer)
        {
            Debug.LogError("DrawCards: not server!");
            return null;
        }
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

        List<CardPODServer> drawnCards = new List<CardPODServer>();
        for (int i = 0; i < numCards; i++)
        {
            CardPODServer drawnPOD = cardsInPlay[serverDrawPile[0]];
            serverDrawPile.RemoveAt(0);
            drawnPOD.ownerPlayerID = playerID;
            drawnCards.Add(drawnPOD);
        }
        return drawnCards;
    }
    

    //void ActionTaken(TurnAction action) { } // Update state based on action

    // Server: Simple serverDrawPile initialization and shuffle
    // NOTE: plain old data - no game objects here
    void InitAndShuffleDeck()
    {
        if (!isServer)
        {
            Debug.LogError("InitAndShuffleDeck: not server!");
            return;
        }

        int cardID = 0;        
        List<CardPODServer> deckPull = new();
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
            deckPull[index].facingOwner = rand.NextDouble() > 0.5 ? CardFace.sideA : CardFace.sideB;

            CardPODServer cardCopy = deckPull[index].Clone(); // not necessary since Cloned above..
            cardCopy.cardID = cardID;
            cardCopy.state = CardState.drawPile;    // by default
            serverDrawPile.Add(cardCopy.cardID);
            deckPull.RemoveAt(index);
            cardsInPlay[cardID] = cardCopy;
            cardID++;
        }

        // Ensure correct number of each card (6 * 6 = 36)
        //VerifyDeckComposition();
    }

    void VerifyDeckComposition()
    {
        // Ensure correct number of each card (6 * 6 = 36)

            int totalRed = 0, totalGreen = 0, totalBlue = 0, totalPurple = 0, totalYellow = 0;
            for (int i = 0; i < serverDrawPile.Count; i++)
            {
                switch (cardsInPlay[serverDrawPile[i]].cardSideAColor)
                {
                    case CardColor.red:
                        totalRed++;
                        break;
                    case CardColor.green:
                        totalGreen++;
                        break;
                    case CardColor.blue:
                        totalBlue++;
                        break;
                    case CardColor.purple:
                        totalPurple++;
                        break;
                    case CardColor.yellow:
                        totalYellow++;
                        break;
                }
                switch (cardsInPlay[serverDrawPile[i]].cardSideBColor)
                {
                    case CardColor.red:
                        totalRed++;
                        break;
                    case CardColor.green:
                        totalGreen++;
                        break;
                    case CardColor.blue:
                        totalBlue++;
                        break;
                    case CardColor.purple:
                        totalPurple++;
                        break;
                    case CardColor.yellow:
                        totalYellow++;
                        break;
                }
            }
            Debug.Log($"Deck Composition - Red: {totalRed}, Green: {totalGreen}, Blue: {totalBlue}, Purple: {totalPurple}, Yellow: {totalYellow}");
    }

#region Actions-Available-Checks
    public TurnAction GetAvailableActionsForCard(CardPODServer cardPOD)
    {
        //List<TurnAction> actions = new List<TurnAction>();
        TurnAction availableActions = TurnAction.None;
        PlayerXServer ownerPlayer = GetPlayerByID(cardPOD.ownerPlayerID);
        if (ownerPlayer == null)
        {
            Debug.LogError("AvailableActionsForCard: could not find owner player for cardID " + cardPOD.cardID);
            return availableActions;
        }

        var allPlayers = GetActivePlayers();

        availableActions |= TurnAction.Flip | TurnAction.Switch | TurnAction.Swap1;
        // Flip always available -> current player's or oppenent's card
        //actions.Add(TurnAction.Flip);
        // Switch always available -> current player's or opponent's card
        //actions.Add(TurnAction.Switch);
        // Swap1 always available -> current player's card with either theirs or opponent's
        //actions.Add(TurnAction.Swap1);

        // Swap2 requires 2 adjacent same color cards from 2 players
        if (IsThere2AdjacentCardsOfSameColorAsThis(ownerPlayer, cardPOD))
        {
            // Check other players for adjacent same color cards
            foreach (var player in allPlayers)
            {
                if (player != ownerPlayer && IsThereAny2AdjacentCardsOfSameColor(player))
                {
                    //actions.Add(TurnAction.Swap2);
                    availableActions |= TurnAction.Swap2;
                    break;
                }
            }
        }
        // Score requires 4-6 adjacent same color cards from current player's hand
        if (IsThere4To6AdjacentCardsOfSameColorAsThis(ownerPlayer, cardPOD))
        {
            //actions.Add(TurnAction.Score);
            availableActions |= TurnAction.Score;
        }
        if (IsSwipeAvailableForPlayer(ownerPlayer))
        {
            //actions.Add(TurnAction.Swipe);
            availableActions |= TurnAction.Swipe;
        }

        return availableActions;
    }


    public bool IsSwap2Available()
    {
        var allPlayers = GetActivePlayers();
        foreach (var player in allPlayers)
        {
            if (IsThereAny2AdjacentCardsOfSameColor(player))
            {
                // Check other players for adjacent same color cards
                foreach (var otherPlayer in allPlayers)
                {
                    if (otherPlayer != player && IsThereAny2AdjacentCardsOfSameColor(otherPlayer))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool IsSwap2AvailableForPlayer(PlayerXServer player)
    {
        if (player == null)
            return false;

        if (IsThereAny2AdjacentCardsOfSameColor(player))
        {
            var allPlayers = GetActivePlayers();
            // Check other players for adjacent same color cards
            foreach (var otherPlayer in allPlayers)
            {
                if (otherPlayer != player && IsThereAny2AdjacentCardsOfSameColor(otherPlayer))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsThereAny2AdjacentCardsOfSameColor(PlayerXServer player)
    {
        if (player == null)
            return false;

        for (int i = 0; i < 6 - 1; i++)
        {
            CardColor thisColor = player.hand[i].GetFacingColor();
            CardColor nextColor = player.hand[i + 1].GetFacingColor();
            if (thisColor == nextColor)
            {
                Debug.Log("IsThereAnyAdjacentCardsOfSameColor: found adjacent same color cards in player " + player.playerId + "'s hand at index " + i);
                return true;
            }
        }
        return false;
    }

    public bool IsThere2AdjacentCardsOfSameColorAsThis(PlayerXServer player, CardPODServer cardPOD)
    {
        if (player == null)
            return false;

        CardColor selectedCard = cardPOD.GetFacingColor();

        int cardIndex = player.GetIndexOfCard(cardPOD);
        if (cardIndex < 0)
        {
            Debug.LogError("IsThereAdjacentCardsOfSameColorAsThis: could not find cardID " + cardPOD.cardID + " in player " + player.playerId + "'s hand");
            return false;
        }
        // Check right if can
        if (cardIndex + 1 < 6)
        {
            if (player.hand[cardIndex + 1].GetFacingColor() == selectedCard)
                return true;
        }
        // Check left if can
        if (cardIndex - 1 >= 0)
        {
            if (player.hand[cardIndex - 1].GetFacingColor() == selectedCard)
                return true;
        }

        return false;
    }

    public bool IsThere4To6AdjacentCardsOfSameColorAsThis(PlayerXServer player, CardPODServer cardPOD)
    {
        if (player == null)
            return false;
            
        CardColor color = cardPOD.GetFacingColor();
        int cardIndex = player.GetIndexOfCard(cardPOD);
        if (cardIndex < 0)
        {
            Debug.LogError("IsThere4To6AdjacentCardsOfSameColorAsThis: could not find cardID " + cardPOD.cardID + " in player " + player.playerId + "'s hand");
            return false;
        }
        int leftCount = 0;
        int rightCount = 0;
        // Check left
        for (int i = cardIndex - 1; i >= 0; i--)
        {
            if (player.hand[i].GetFacingColor() == color)
                leftCount++;
            else
                break;
        }
        // Check right
        for (int i = cardIndex + 1; i < 6; i++)
        {
            if (player.hand[i].GetFacingColor() == color)
                rightCount++;
            else
                break;
        }
        int totalAdjacent = leftCount + rightCount + 1;
        return totalAdjacent >= 4 && totalAdjacent <= 6;
    }

    public bool IsThereAny4To6AdjacentCardsOfSameColor(PlayerXServer player)
    {
        if (player == null)
            return false;

        CardColor lastColor = player.hand[0].GetFacingColor();
        int sameColorCount = 1;
        for (int i = 1; i < 6; i++)
        {
            CardColor thisColor = player.hand[i].GetFacingColor();
            if (thisColor == lastColor)
            {
                sameColorCount++;
            }
            else
            {
                // break in previous run; check if we met or exceeded 4 in a row
                if (sameColorCount >= 4)
                {
                    //Debug.Log("IsThereAny4To6AdjacentCardsOfSameColor: player " + player.playerId + " found adjacent same color count = " + sameColorCount);
                    return true;
                }
                sameColorCount = 1;
                lastColor = thisColor;
            }
        }
        //Debug.Log("IsThereAny4To6AdjacentCardsOfSameColor: player " + player.playerId + " max adjacent same color count = " + sameColorCount);
        return sameColorCount >= 4;
    }

    public bool IsScoreAvailableForPlayer(PlayerXServer player)
    {
        return IsThereAny4To6AdjacentCardsOfSameColor(player);
    }

    public bool IsThereAny4To6AdjacentCardsOfSameColorForAnyPlayer()
    {
        var allPlayers = GetActivePlayers();
        foreach (var player in allPlayers)
        {
            if (IsThereAny4To6AdjacentCardsOfSameColor(player))
                return true;
        }
        return false;
    }

    public bool IsSwipeAvailableForPlayer(PlayerXServer player)
    {
        var allPlayers = GetActivePlayers();
        foreach (var otherPlayer in allPlayers)
        {
            if (otherPlayer != player && IsThereAny4To6AdjacentCardsOfSameColor(otherPlayer))
            {
                return true;
            }
        }
        return false;
    }

    public TurnAction GetAvailableActionsForPlayer(PlayerXServer ownerPlayer)
    {
        if (ownerPlayer == null)
            return TurnAction.None;

        // Flip always available -> current player's or oppenent's card
        // Switch always available -> current player's or opponent's card
        // Swap1 always available -> current player's card with opponent's
        TurnAction availableActions = TurnAction.Flip | TurnAction.Switch | TurnAction.Swap1;

        // a player can Swipe if any *other* player has 4-6 adjacent same color cards
        if (IsSwipeAvailableForPlayer(ownerPlayer))
        {
            //actions.Add(TurnAction.Swipe);
            availableActions |= TurnAction.Swipe;
        }

        int maxAdjacentColors = GetTotalAdjacentColorCount(ownerPlayer);
        // Score requires 4-6 adjacent same color cards from current player's hand
        if (maxAdjacentColors >= 4)
        {
            availableActions |= TurnAction.Score;
        }
        // Swap2 requires 2 adjacent same color cards from this player's hand and another player's hand
        if (maxAdjacentColors >= 2)
        {
            var allPlayers = GetActivePlayers();
            // Check other players for adjacent same color cards
            foreach (var player in allPlayers)
            {
                if (player != ownerPlayer && IsThereAny2AdjacentCardsOfSameColor(player))
                {
                    //actions.Add(TurnAction.Swap2);
                    availableActions |= TurnAction.Swap2;
                    break;
                }
            }
        }
        return availableActions;
    }

    //! Problems: Swap2 requires looking at other player hands (need current playerId)
    //!           Swipe requires looking at other player hands (again, need current playerId)
    public TurnAction GetAvailableActionsForHand(CardColor[] handColors)
    {
        if (handColors == null || handColors.Length != 6)
        {
            Debug.LogError("GetAvailableActionsForHand: invalid handColors array");
            return TurnAction.None;
        }

        TurnAction availableActions = TurnAction.Flip | TurnAction.Switch | TurnAction.Swap1;

        // Check for Swipe availability
        /*if (IsThereAny4To6AdjacentColorsInArray(handColors))
        {
            availableActions |= TurnAction.Swipe;
        }*/

        int maxAdjacentColors = GetTotalAdjacentColorCountInArray(handColors);
        if (maxAdjacentColors >= 4)
        {
            availableActions |= TurnAction.Score;
        }
        // Swap2 requires 2 adjacent same color cards from 2 players
        if (maxAdjacentColors >= 2)
        {
          /*var allPlayers = GetActivePlayers();
            // Check other players for adjacent same color cards
            foreach (var player in allPlayers)
            {
                if (player != ownerPlayer && IsThereAny2AdjacentCardsOfSameColor(player))
                {
                    //actions.Add(TurnAction.Swap2);
                    availableActions |= TurnAction.Swap2;
                    break;
                }
            }*/
        }
        return availableActions;
    }


    public int GetTotalAdjacentColorCountInArray(CardColor[] handColors)
    {
        if (handColors == null || handColors.Length != 6)
        {
            Debug.LogError("GetTotalAdjacentColorCountInArray: invalid handColors array");
            return -1;
        }

        CardColor lastColor = handColors[0];
        int sameColorCount = 1;
        int maxSameColorCount = 1;
        for (int i = 1; i < 6; i++)
        {
            CardColor thisColor = handColors[i];
            //Debug.Log("GetTotalAdjacentColorCountInArray: checking card index " + i + " color " + thisColor);
            if (thisColor == lastColor)
            {
                sameColorCount++;
            }
            else
            {
                // break in previous run; keep previous max
                maxSameColorCount = sameColorCount > maxSameColorCount ? sameColorCount : maxSameColorCount;
                sameColorCount = 1;
                lastColor = thisColor;
            }
        }
        if (sameColorCount > maxSameColorCount)
            maxSameColorCount = sameColorCount;
        //Debug.Log("GetTotalAdjacentColorCountInArray: max adjacent same color count = " + maxSameColorCount);
        return maxSameColorCount;
    }

    public int GetTotalAdjacentColorCount(PlayerXServer player)
    {
        if (player == null)
            return -1;

        CardColor lastColor = player.hand[0].GetFacingColor();
        int sameColorCount = 1;
        int maxSameColorCount = 1;
        for (int i = 1; i < 6; i++)
        {
            CardColor thisColor = player.hand[i].GetFacingColor();
            //Debug.Log("GetTotalAdjacentColorCount: player " + player.playerId + " checking card index " + i + " color " + thisColor);
            if (thisColor == lastColor)
            {
                sameColorCount++;
            }
            else
            {
                // break in previous run; keep previous max
                maxSameColorCount = sameColorCount > maxSameColorCount ? sameColorCount : maxSameColorCount;
                sameColorCount = 1;
                lastColor = thisColor;
            }
        }
        if (sameColorCount > maxSameColorCount)
            maxSameColorCount = sameColorCount;
        //Debug.Log("GetTotalAdjacentColorCount: player " + player.playerId + " max adjacent same color count = " + maxSameColorCount);
        return maxSameColorCount;
    }
#endregion
}
