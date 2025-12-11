using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using NUnit.Framework;
using System;

//!! GameStateClient is duplicated (except for static data) for each hotseat player

[Serializable]
public class GameResults
{
    public int numberOfPlayers = 0;
    public int winningPlayerNum = 0;
    public int winningPlayerId = -1;
    public int winningScore = 0;
    public int[] finalScores = new int[5];
    public string[] playerNames;
}

[System.Serializable]
public class GameStateClient
{

#region Client-Only-Data
    //! No use for cardsInPlayClient now - cards are only looked for in players' hands
    // similar to server cardsInPlay, but only cards generated/known to client (and specific to that client view)
    [SerializeField] public List<CardPODClient> cardsInPlayClient = new List<CardPODClient>();

    // Hotseat-only:
    [SerializeField] public int localPlayerNumber = 0;

    [SerializeField] public static string localPlayerName = "Player";

    [SerializeField] private static GameStateClient[] hotseatGameStates = new GameStateClient[5];

    // This reference is important and is setup on Init
    public static GameStateClient CurrentGameStateClient;   // = hotseatGameStates[0];

    public bool handsDealt = false;

    public static GameResults gameResults = new GameResults();

    public static MultiplayerMode currentMultiplayerMode = MultiplayerMode.LocalHotseat;
#endregion

#region Client-Data-Altered-From-Server
    [SerializeField] public PlayerXClient[] playersClient = new PlayerXClient[5];
#endregion

#region Server-Client-Propagated-Data

    [SerializeField] private static int currentPlayerIndex = 0;
    [SerializeField] private static int totalPlayers = 0;

    private int currentPlayerActionsTaken = 0;
    [SerializeField] public List<FlipOutActions> actionsTakenFull = new List<FlipOutActions>();

    TurnAction actionsAvailableThisTurn = TurnAction.None;

    public static CardColor deckTopCardColor = CardColor.invalid;

#endregion

   // Client-side initialization
    public static void InitGameStateClient(int[] playerIds, string[] playerNames)
    {
        if (totalPlayers > 0)
        {
            Debug.LogWarning("InitGameStateClient: already initialized!");
            return;
        }
        
        for (int i = 0; i < playerIds.Length; i++)
        {
            hotseatGameStates[i] = new GameStateClient();
            hotseatGameStates[i].localPlayerNumber = i;
            hotseatGameStates[i].AssignPlayersClient(playerIds, playerNames);
            hotseatGameStates[i].currentPlayerActionsTaken = 0;
        }

        CurrentGameStateClient = hotseatGameStates[0];

        currentPlayerIndex = 0;
        //currentPlayerActionsTaken = 0;    // set per-client
        totalPlayers = playerNames.Length; 
    }

    public static void CleanupClients()
    {
        for (int i = 0; i < hotseatGameStates.Length; i++)
        {
            if (hotseatGameStates[i] != null)
            {
                hotseatGameStates[i].CleanupClient();
                hotseatGameStates[i].DestroyPlayers();
                hotseatGameStates[i].currentPlayerActionsTaken = 0;
                hotseatGameStates[i].actionsTakenFull.Clear();
                hotseatGameStates[i].actionsTakenFull = new List<FlipOutActions>();
                hotseatGameStates[i].handsDealt = false;
            }
        }
        CurrentGameStateClient = hotseatGameStates[0];
        //DestroyPlayers();
        //actionsTakenFull.Clear();
        //actionsTakenFull = new List<FlipOutActions>();
        //currentPlayerActionsTaken = 0;    // set per-client
        currentPlayerIndex = 0;
        totalPlayers = 0;
    }
    public void CleanupClient()
    {
        cardsInPlayClient.Clear();
        cardsInPlayClient = new List<CardPODClient>();  // null isnt debug friendly
    }
    void DestroyPlayers()
    {
        for (int playerNum = 0; playerNum < 5; playerNum++)
        {
            playersClient[playerNum] = new PlayerXClient(); // null isnt debug friendly
        }
        totalPlayers = 0;
    }

    public void AssignPlayersClient(int[] playerIds, string[] playerNames)
    {
        if (playerNames == null || playerNames.Length == 0)
        {
            Debug.LogError("AssignPlayersClient: playerNames is null or empty!");
            return;
        }
        totalPlayers = playerNames.Length;
        for (int playerNum = 0; playerNum < playerNames.Length; playerNum++)
        {
            playersClient[playerNum] = new PlayerXClient();
            playersClient[playerNum].playerId = playerIds[playerNum];
            playersClient[playerNum].playerName = playerNames[playerNum];
            playersClient[playerNum].playerNumber = playerNum;
            Debug.Log("Player " + playerNum + " name set to: " + playersClient[playerNum].playerName);
        }
        //currentPlayerIndex = 0;
        //localPlayerNumber = 0;
    }

    public static void GatherResults()
    {
        if (totalPlayers > 0)
        {
            int winnerIndex = -1;
            int winnerScore = -1;
            int winnerId = -1;
            int[] finalScores = new int[totalPlayers];
            string[] finalPlayers = new string[totalPlayers];
            for (int i = 0; i < totalPlayers; i++)
            {
                PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(i);
                int score = player.scorePile.Count;
                if (score > winnerScore)
                {
                    winnerScore = score;
                    winnerIndex = i;
                    winnerId = player.playerId;
                }
                finalScores[i] = player.scorePile.Count;
                finalPlayers[i] = player.playerName;
                Debug.Log("Final score for Player " + i + " (" + player.playerName + "): " + finalScores[i]);
            }
            gameResults = new GameResults()
            {
                numberOfPlayers = totalPlayers,
                winningPlayerNum = winnerIndex,
                winningPlayerId = winnerId,
                winningScore = winnerScore,
                finalScores = finalScores,
                playerNames = finalPlayers
            };
        }
    }

    public void AddUncountedActionTaken(FlipOutActions action)
    {
        actionsTakenFull.Add(action);
    }

    public static void AddUncountedActionTakenForAll(FlipOutActions action)
    {
        for (int playerIdx = 0; playerIdx < totalPlayers; playerIdx++)
        {
            Debug.Log("AddUncountedActionTakenForAll: adding uncounted action (" + action.actionTaken.ToString() + ") for player " + playerIdx);
            hotseatGameStates[playerIdx].AddUncountedActionTaken(action);
        }
    }

    public bool AddPlayerActionTaken(int playerNum, FlipOutActions action)
    {
        // Tracking other player actions?
        if (playerNum != currentPlayerIndex)
        {
            actionsTakenFull.Add(action);
            return true;
        }
        // else: current player action

        if (currentPlayerActionsTaken >= 2)
        {
            Debug.LogError("AddPlayerActionTaken: current player has already taken 2 actions this turn!");
            return false;
        }

        actionsTakenFull.Add(action);

        // Could track per-player actions if needed
        currentPlayerActionsTaken++;
        return true;
    }

    public static bool AddPlayerActionTakenForAll(FlipOutActions action)
    {
        for (int playerIdx = 0; playerIdx < totalPlayers; playerIdx++)
        {
            Debug.Log("AddPlayerActionTakenForAll: adding action (" + action.actionTaken.ToString() + ") for player " + playerIdx);
            if (!hotseatGameStates[playerIdx].AddPlayerActionTaken(playerIdx, action))
            {
                Debug.LogError("AddPlayerActionTakenForAll: failed to add action " + action.actionTaken.ToString() + " for player #" + playerIdx);
                return false;
            }
        }
        return true;
    }

    public static void AddPlayerActionTakenForOpponentViews(int playerNum, FlipOutActions action, bool isUncounted)
    {
        for (int playerIdx = 0; playerIdx < totalPlayers; playerIdx++)
        {
            if (playerIdx != playerNum)
            {
                Debug.Log("AddPlayerActionTakenForOpponentViews: adding action (" + action.actionTaken.ToString() + ") for player " + playerIdx + " view of player " + playerNum);
                if (isUncounted)
                {
                    hotseatGameStates[playerIdx].AddUncountedActionTaken(action);
                }
                else
                {
                    hotseatGameStates[playerIdx].AddPlayerActionTaken(playerIdx, action);
                }
            }
        }
    }
    public static void AddUncountedActionTakenForOpponentViews(int playerNum, FlipOutActions action)
    {
        AddPlayerActionTakenForOpponentViews(playerNum, action, true);
    }

    public int GetCurrentPlayerActionsTaken()
    {
        return currentPlayerActionsTaken;
    }

    public void AssignCardsToPlayerHand(int playerNum, List<CardPODClient> cards, int[] positions)
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
        for (int i = 0; i < cards.Count; i++)
        {
            playersClient[playerNum].hand[positions[i]] = cards[i];
            //! cardsInPlayClient.Add(cards[i]);
        }
    }
/*
    //! ? Use? - also, do we update cardsInPlayClient here?
    public void SetHandForPlayer(int playerNum, CardPODClient[] handCards)
    {
        if (playerNum < 0 || playerNum >= totalPlayers)
        {
            Debug.LogError("SetHandForPlayer: invalid playerNum " + playerNum);
            return;
        }
        if (handCards == null || handCards.Length != 6)
        {
            Debug.LogError("SetHandForPlayer: invalid handCards array for player " + playerNum);
            return;
        }
        for (int handIdx = 0; handIdx < 6; handIdx++)
        {
            playersClient[playerNum].hand[handIdx] = handCards[handIdx].Clone();    //! CLone ??
        }
    }*/

    /*public static void AssignCardsToPlayerHandForOpponentViews(int playerNum, List<CardPODClient> cardsForOpponentViews, int[] positions)
    {
        for (int playerIdx = 0; playerIdx < totalPlayers; playerIdx++)
        {
            if (playerIdx != playerNum)
            {
                Debug.Log("AssignCardsToPlayerHandForOpponentViews: assigning cards to player " + playerIdx + " view of player " + playerNum);
                hotseatGameStates[playerIdx].AssignCardsToPlayerHand(playerNum, cardsForOpponentViews, positions);
            }
        }
    }*/

    public PlayerXClient GetPlayerByCardId(int cardID)
    {
        for (int playerNum = 0; playerNum < totalPlayers; playerNum++)
        {
            PlayerXClient player = playersClient[playerNum];
            int handIdx = player.GetIndexOfCardByID(cardID);
            if (handIdx != -1)
            {
                return player;
            }
        }
        Debug.LogError("GetPlayerByCardId: could not find cardID " + cardID + " in any player's hand");
        return null;
    }

    public void SwitchCardsInPlayerHand(int owningPlayerId, int cardId1, int cardId2)
    {
        PlayerXClient player = GetPlayerByID(owningPlayerId);
        if (player == null)
        {
            Debug.LogError("SwitchCardsInPlayerHand: player not found for owningPlayerId " + owningPlayerId);
            return;
        }
        player.SwitchCardsInHandByID(cardId1, cardId2);
    }

    public void Swap1CardBetweenPlayers(int player1Id, int playerSwapWithId, int cardId1, int cardSwapWith1)
    {
        if (player1Id == playerSwapWithId)
        {
            Debug.LogError("Swap1CardBetweenPlayers: cannot swap cards between the same player ID " + player1Id);
            return;
        }
        PlayerXClient player1 = GetPlayerByID(player1Id);
        PlayerXClient playerSwapWith = GetPlayerByID(playerSwapWithId);
        if (player1 == null || playerSwapWith == null)
        {
            Debug.LogError("Swap1CardBetweenPlayers: could not find both players for IDs " + player1Id + " and " + playerSwapWithId);
            return;
        }
        if (player1.playerId != player1Id || playerSwapWith.playerId != playerSwapWithId)
        {
            Debug.LogError("Swap1CardBetweenPlayers: player IDs need to be in same order as card ids!");
            return;
        }
        int card1Index = player1.GetIndexOfCardByID(cardId1);
        int cardSwappingWithIndex = playerSwapWith.GetIndexOfCardByID(cardSwapWith1);
        if (card1Index == -1 || cardSwappingWithIndex == -1)
        {
            Debug.LogError("Swap1CardBetweenPlayers: could not find both cards for IDs " + cardId1 + " and " + cardSwapWith1);
            return;
        }
        CardPODClient card1POD = player1.hand[card1Index];
        CardPODClient card2POD = playerSwapWith.hand[cardSwappingWithIndex];
        // update ownerPlayerID
        card1POD.ownerPlayerID = playerSwapWithId;
        card2POD.ownerPlayerID = player1Id;
        // Swap hands (no temp needed because we have the references)
        player1.hand[card1Index] = card2POD;
        playerSwapWith.hand[cardSwappingWithIndex] = card1POD;
    }

    public void Swap2CardsBetweenPlayers(int player1Id, int playerSwapWithId, int cardId1, int cardId2, int cardSwapWith1, int cardSwapWith2)
    {
        if (player1Id == playerSwapWithId)
        {
            Debug.LogError("Swap2CardsBetweenPlayers: cannot swap cards between the same player ID " + player1Id);
            return;
        }
        PlayerXClient player1 = GetPlayerByID(player1Id);
        PlayerXClient playerSwapWith = GetPlayerByID(playerSwapWithId);
        if (player1 == null || playerSwapWith == null)
        {
            Debug.LogError("Swap2CardsBetweenPlayers: could not find both players for IDs " + player1Id + " and " + playerSwapWithId);
            return;
        }
        if (player1.playerId != player1Id || playerSwapWith.playerId != playerSwapWithId)
        {
            Debug.LogError("Swap2CardsBetweenPlayers: player IDs need to be in same order as card ids!");
            return;
        }
        int card1Index = player1.GetIndexOfCardByID(cardId1);
        int card2Index = player1.GetIndexOfCardByID(cardId2);
        int cardSwappingWith1Index = playerSwapWith.GetIndexOfCardByID(cardSwapWith1);
        int cardSwappingWith2Index = playerSwapWith.GetIndexOfCardByID(cardSwapWith2);
        if (card1Index == -1 || card2Index == -1 || cardSwappingWith1Index == -1 || cardSwappingWith2Index == -1)
        {
            Debug.LogError("Swap2CardsBetweenPlayers: could not find all cards for IDs " + cardId1 + ", " + cardId2 + ", " + cardSwapWith1 + ", " + cardSwapWith2);
            return;
        }
        if (Math.Abs(card1Index - card2Index) != 1 ||
            Math.Abs(cardSwappingWith1Index - cardSwappingWith2Index) != 1)
        {
            Debug.LogError("Swap2CardsBetweenPlayers: card pairs to be swapped are not adjacent in hands!");
            return;
        }
        // Consecutive order enforcement
        if (card1Index > card2Index)
        {
            int temp = card1Index;
            card1Index = card2Index;
            card2Index = temp;
        }
        if (cardSwappingWith1Index > cardSwappingWith2Index)
        {
            int temp = cardSwappingWith1Index;
            cardSwappingWith1Index = cardSwappingWith2Index;
            cardSwappingWith2Index = temp;
        }
        CardPODClient card1POD = player1.hand[card1Index];
        CardPODClient card2POD = player1.hand[card2Index];
        CardPODClient cardSwapWith1POD = playerSwapWith.hand[cardSwappingWith1Index];
        CardPODClient cardSwapWith2POD = playerSwapWith.hand[cardSwappingWith2Index];
        //xx - This is not what we do here, especially as the colors will be different on playback for other clients
        /*if (card1POD.color != card2POD.color ||
            cardSwapWith1POD.color != cardSwapWith2POD.color)
        {
            Debug.LogError("Swap2CardsBetweenPlayers: card pairs to be swapped are not adjacent matching colors!");
            return;
        }*/

        // update ownerPlayerID
        card1POD.ownerPlayerID = playerSwapWithId;
        card2POD.ownerPlayerID = playerSwapWithId;
        cardSwapWith1POD.ownerPlayerID = player1Id;
        cardSwapWith2POD.ownerPlayerID = player1Id;
        // Swap hands (no temp needed because we have the references)
        player1.hand[card1Index] = cardSwapWith1POD;
        player1.hand[card2Index] = cardSwapWith2POD;
        playerSwapWith.hand[cardSwappingWith1Index] = card1POD;
        playerSwapWith.hand[cardSwappingWith2Index] = card2POD;
    }

    public void ScoreCardsFromPlayerHand(int playerId, int[] adjacentCardIndices)
    {
        PlayerXClient player = GetPlayerByID(playerId);
        if (player == null)
        {
            Debug.LogError("ScoreCardsFromPlayerHand: player not found for playerId " + playerId);
            return;
        }
        foreach (int handIdx in adjacentCardIndices)
        {
            if (handIdx < 0 || handIdx >= player.hand.Length)
            {
                Debug.LogError("ScoreCardsFromPlayerHand: invalid hand index " + handIdx + " for playerId " + playerId);
                continue;
            }
            CardPODClient cardPOD = player.hand[handIdx];
            if (cardPOD == null)
            {
                Debug.LogError("ScoreCardsFromPlayerHand: no card found at hand index " + handIdx + " for playerId " + playerId);
                continue;
            }
            cardPOD.state = CardState.scorePile;
            player.scorePile.Add(cardPOD); // Add to player's score pile
            player.hand[handIdx] = new CardPODClient();
        }
    }
    public void SwipeCardsFromPlayerHand(int playerSwipingId, int targetPlayerId, int[] adjacentCardIndices)
    {
        PlayerXClient swipingPlayer = GetPlayerByID(playerSwipingId);
        PlayerXClient targetPlayer = GetPlayerByID(targetPlayerId);
        if (swipingPlayer == null || targetPlayer == null)
        {
            Debug.LogError("SwipeCardsFromPlayerHand: player not found for given ids " + playerSwipingId + " or " + targetPlayerId);
            return;
        }
        // the last card goes into target player's score pile

        //foreach (int handIdx in adjacentCardIndices - 1)
        for (int handIdx = 0; handIdx < adjacentCardIndices.Length - 1; handIdx++)
        {
            CardPODClient cardPOD = targetPlayer.hand[adjacentCardIndices[handIdx]];
            if (cardPOD == null)
            {
                Debug.LogError("SwipeCardsFromPlayerHand: no card found at hand index " + handIdx + " for playerId " + targetPlayerId);
                continue;
            }
            // update id
            cardPOD.ownerPlayerID = playerSwipingId;
            cardPOD.state = CardState.scorePile;
            swipingPlayer.scorePile.Add(cardPOD); // Add to player's score pile
            targetPlayer.hand[adjacentCardIndices[handIdx]] = new CardPODClient();
        }
        // Last card moves to target player's score pile
        int lastHandIdx = adjacentCardIndices[adjacentCardIndices.Length - 1];
        CardPODClient lastCardPOD = targetPlayer.hand[lastHandIdx];
        if (lastCardPOD == null)
        {
            Debug.LogError("SwipeCardsFromPlayerHand: no card found at last hand index " + lastHandIdx + " for playerId " + targetPlayerId);
            return;
        }
        lastCardPOD.state = CardState.scorePile;
        targetPlayer.scorePile.Add(lastCardPOD); // Add to target player's score pile
        targetPlayer.hand[lastHandIdx] = new CardPODClient();
    }

    public static void AssignDeckTopCard(CardColor cardColor)
    {
        deckTopCardColor = cardColor;
    }
    public static CardColor GetDeckTopCardColor()
    {
        return deckTopCardColor;
    }

    public void SetActionsAvailableThisTurn(TurnAction actions)
    {
        actionsAvailableThisTurn = actions;
    }
    public TurnAction GetActionsAvailableThisTurn()
    {
        return actionsAvailableThisTurn;
    }

    public int AdvanceToNextPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
        currentPlayerActionsTaken = 0;
        CurrentGameStateClient = hotseatGameStates[currentPlayerIndex];
        return currentPlayerIndex;
    }

    public List<FlipOutActions> GetListOfActionsSinceLastTurn()
    {
        return actionsTakenFull;
    }

    public void ClearActionsSinceLastTurn()
    {
        actionsTakenFull.Clear();
        Debug.Log("ClearActionsSinceLastTurn: cleared actions taken list; count is now " + actionsTakenFull.Count);
    }

    public static GameStateClient GetHotseatGameStateForPlayerNumber(int localPlayerNum)
    {
        if (localPlayerNum < 0 || localPlayerNum >= 5)
        {
            Debug.LogError("GetHotseatGameStateForPlayer: invalid localPlayerNum " + localPlayerNum);
            return null;
        }
        if (hotseatGameStates[localPlayerNum] == null)
        {
            Debug.LogError("GetHotseatGameStateForPlayer: hotseatGameStates[" + localPlayerNum + "] is null");
            return null;
        }
        return hotseatGameStates[localPlayerNum];
    }

    public static void SetCurrentHotseatGameState(int localPlayerNum)
    {
        if (localPlayerNum < 0 || localPlayerNum >= 5)
        {
            Debug.LogError("SetCurrentHotseatGameState: invalid localPlayerNum " + localPlayerNum);
            return;
        }
        if (hotseatGameStates[localPlayerNum] == null)
        {
            Debug.LogError("SetCurrentHotseatGameState: hotseatGameStates[" + localPlayerNum + "] is null");
            return;
        }
        CurrentGameStateClient = hotseatGameStates[localPlayerNum];
    }

    public CardPODClient GetCardByID(int cardID)
    {
        if (cardID < 0 || cardID >= 90)
        {
            Debug.LogError("GameStateC->GetCardByID(): invalid cardID " + cardID);
            return null;
        }
        // Unlike server, cardID isn't also an index in Client
        for (int i = 0; i < cardsInPlayClient.Count; i++)
        {
            if (cardsInPlayClient[i].cardID == cardID)
                return cardsInPlayClient[i];
        }
        Debug.LogError("GameStateC->GetCardByID(): could not find cardID " + cardID);
        return null;
    }

    public static int GetActivePlayerNumber()
    {
        return currentPlayerIndex;
    }
    public static int GetCurrentPlayerNumber()
    {
        return currentPlayerIndex;
    }
    public static int GetCurrentPlayerId()
    {
        return CurrentGameStateClient.playersClient[currentPlayerIndex].playerId;
    }
    public static int GetTotalPlayers()
    {
        return totalPlayers;
    }

   public PlayerXClient GetActivePlayer()
    {
        return playersClient[currentPlayerIndex];
    }

   public int GetPlayerNumberByID(int playerID)
    {
        for (int playerNum = 0; playerNum < totalPlayers; playerNum++)
        {
            if (playersClient[playerNum].playerId == playerID)
                return playerNum;
        }
        Debug.LogError("GameStateC->GetPlayerNumberByID(): could not find playerID " + playerID);
        return -1;
    }

    public PlayerXClient GetPlayerByNumber(int playerNum)
    {
        if (playerNum < 0 || playerNum >= totalPlayers)
        {
            Debug.LogError("GameStateC->GetPlayerByNumber(): invalid playerNum " + playerNum);
            return null;
        }
        return playersClient[playerNum];
    }

    public int GetPlayerIDByNumber(int playerNum)
    {
        if (playerNum < 0 || playerNum >= totalPlayers)
        {
            Debug.LogError("GameState->GetPlayerIDByNumber(): invalid playerNum " + playerNum);
            return -1;
        }
        return playersClient[playerNum].playerId;
    }

    public PlayerXClient GetPlayerByID(int playerID)
    {
        int index = GetPlayerNumberByID(playerID);
        if (index == -1)
        {
            Debug.LogError("GameStateC->GetPlayerByID(): Invalid playerID " + playerID);
            return null;
        }
        return playersClient[index];
    }

    public List<PlayerXClient> GetActivePlayers()
    {
        List<PlayerXClient> activePlayers = new List<PlayerXClient>();
        for (int playerNum = 0; playerNum < totalPlayers; playerNum++)
        {
            activePlayers.Add(playersClient[playerNum]);
        }
        return activePlayers;
    }

#region Actions-Available-Checks-Client
    public TurnAction GetAvailableActionsForCard(CardPODClient cardPOD)
    {
        //List<TurnAction> actions = new List<TurnAction>();
        TurnAction availableActions = TurnAction.None;
        PlayerXClient ownerPlayer = GetPlayerByID(cardPOD.ownerPlayerID);
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

    public bool IsSwap2AvailableForPlayer(PlayerXClient player)
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

    public static bool IsThereAny2AdjacentCardsOfSameColor(PlayerXClient player)
    {
        if (player == null)
            return false;

        for (int i = 0; i < 6 - 1; i++)
        {
            CardColor thisColor = player.hand[i].color;
            CardColor nextColor = player.hand[i + 1].color;
            if (thisColor == nextColor)
            {
                Debug.Log("IsThereAnyAdjacentCardsOfSameColor: found adjacent same color cards in player " + player.playerId + "'s hand at index " + i);
                return true;
            }
        }
        return false;
    }

    public static bool IsThere2AdjacentCardsOfSameColorAsThis(PlayerXClient player, CardPODClient cardPOD)
    {
        if (player == null)
            return false;

        CardColor selectedCard = cardPOD.color;

        int cardIndex = player.GetIndexOfCard(cardPOD);
        if (cardIndex < 0)
        {
            Debug.LogError("IsThereAdjacentCardsOfSameColorAsThis: could not find cardID " + cardPOD.cardID + " in player " + player.playerId + "'s hand");
            return false;
        }
        // Check right if can
        if (cardIndex + 1 < 6)
        {
            if (player.hand[cardIndex + 1].color == selectedCard)
                return true;
        }
        // Check left if can
        if (cardIndex - 1 >= 0)
        {
            if (player.hand[cardIndex - 1].color == selectedCard)
                return true;
        }

        return false;
    }

    public static bool IsThere4To6AdjacentCardsOfSameColorAsThis(PlayerXClient player, CardPODClient cardPOD)
    {
        if (player == null)
            return false;
            
        CardColor color = cardPOD.color;
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
            if (player.hand[i].color == color)
                leftCount++;
            else
                break;
        }
        // Check right
        for (int i = cardIndex + 1; i < 6; i++)
        {
            if (player.hand[i].color == color)
                rightCount++;
            else
                break;
        }
        int totalAdjacent = leftCount + rightCount + 1;
        return totalAdjacent >= 4 && totalAdjacent <= 6;
    }

    public static bool IsThereAny4To6AdjacentCardsOfSameColor(PlayerXClient player)
    {
        if (player == null)
            return false;

        CardColor lastColor = player.hand[0].color;
        int sameColorCount = 1;
        for (int i = 1; i < 6; i++)
        {
            CardColor thisColor = player.hand[i].color;
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

    public static bool IsScoreAvailableForPlayer(PlayerXClient player)
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

    public bool IsSwipeAvailableForPlayer(PlayerXClient player)
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

    public TurnAction GetAvailableActionsForPlayer(PlayerXClient ownerPlayer)
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
    /*public static TurnAction GetAvailableActionsForHand(CardColor[] handColors)
    {
        if (handColors == null || handColors.Length != 6)
        {
            Debug.LogError("GetAvailableActionsForHand: invalid handColors array");
            return TurnAction.None;
        }

        TurnAction availableActions = TurnAction.Flip | TurnAction.Switch | TurnAction.Swap1;

        // Check for Swipe availability
        //if (IsThereAny4To6AdjacentColorsInArray(handColors))
        //{
        //    availableActions |= TurnAction.Swipe;
        //}

        int maxAdjacentColors = GetTotalAdjacentColorCountInArray(handColors);
        if (maxAdjacentColors >= 4)
        {
            availableActions |= TurnAction.Score;
        }
        // Swap2 requires 2 adjacent same color cards from 2 players
        if (maxAdjacentColors >= 2)
        {
          //var allPlayers = GetActivePlayers();
            // Check other players for adjacent same color cards
            //foreach (var player in allPlayers)
            //{
            //    if (player != ownerPlayer && IsThereAny2AdjacentCardsOfSameColor(player))
            //    {
            //        //actions.Add(TurnAction.Swap2);
            //        availableActions |= TurnAction.Swap2;
            //        break;
            //    }
            //}
        }
        return availableActions;
    }*/


    public static int GetTotalAdjacentColorCountInArray(CardColor[] handColors)
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

    public static int GetTotalAdjacentColorCount(PlayerXClient player)
    {
        if (player == null)
            return -1;

        CardColor lastColor = player.hand[0].color;
        int sameColorCount = 1;
        int maxSameColorCount = 1;
        for (int i = 1; i < 6; i++)
        {
            CardColor thisColor = player.hand[i].color;
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

    public static int[] GetStartAndEndIndicesOfAdjacentColorsBasedOnCardId(int cardId)
    {
         PlayerXClient ownerPlayer = CurrentGameStateClient.GetPlayerByCardId(cardId);
        if (ownerPlayer == null)
        {
            Debug.LogError("GetStartAndEndIndicesOfAdjacentColorBasedOnCardId: could not find owner player for cardID " + cardId);
            return new int[2] { -1, -1 };
        }
        int cardIndex = ownerPlayer.GetIndexOfCardByID(cardId);
        CardColor color = ownerPlayer.hand[cardIndex].color;

        // Check left
        int startIndex = cardIndex;
        for (int i = cardIndex - 1; i >= 0; i--)
        {
            if (ownerPlayer.hand[i].color == color)
                startIndex = i;
            else
                break;
        }
        // Check right
        int endIndex = cardIndex;
        for (int i = cardIndex + 1; i < 6; i++)
        {
            if (ownerPlayer.hand[i].color == color)
                endIndex = i;
            else
                break;
        }
        return new int[2] { startIndex, endIndex };
    }

    public static int[] GetAdjacentColorsIndicesBasedOnCardId(int cardId)
    {
        int[] startEnd = GetStartAndEndIndicesOfAdjacentColorsBasedOnCardId(cardId);
        if (startEnd[0] == -1)
        {
            return new int[1] { -1 };
        }
        int[] returnIndices = new int[startEnd[1] - startEnd[0] + 1];
        for (int i = startEnd[0]; i <= startEnd[1]; i++)
        {
            returnIndices[i - startEnd[0]] = i;
        }
        return returnIndices;
    }
#endregion

}
