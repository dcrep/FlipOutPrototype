using System;
using System.Collections.Generic;
using UnityEngine;

//!TODO: Debug.LogErrors -> warnings? logic elsewhere? or return failures?

public class ServerDispatch
{

    FlipOutGame flipOutGame = null;
    public bool isServer = false; // Placeholder for server check
    private bool isHotseatGame = false;

    GameStateServer gameStateServer = null;
    

    // GameManager calls this
    public void StartHotseatGame(int[] playerIds, string[] playerNames)
    {
        Debug.Log("ServerDispatch->StartHotseatGame(): Starting hotseat game...");

        isServer = true;
        isHotseatGame = true;
        flipOutGame = GameManager.Instance.flipOutGame;

        gameStateServer = GameManager.Instance.gameStateServer;

        // GameStateServer initialize, prepare for new game
        gameStateServer.InitGameStateServer(playerIds, playerNames);
        // Host/hotseat
        GameStateClient.InitGameStateClient(playerIds, playerNames);

        StartGameServer();
    }

    public void StartOnlineGame()   // (PlayerSession localPlayer)
    {
        //Debug.Log("ServerDispatch->StartOnlineGame(): Starting online game...");

        /*string[] playerNames = new string[sessionManager.sessions.Count];
        int[] playerIds = new int[sessionManager.sessions.Count];
        int index = 0;
        foreach (var session in sessionManager.sessions.Values)
        {
            playerIds[index] = session.playerId;
            playerNames[index] = session.playerName;
            index++;
        }

        // GameStateServer initialize, prepare for new game
        GameManager.Instance.gameStateServer.InitGameStateServer(playerIds, playerNames);*/

        isServer = true;
        isHotseatGame = false;
        flipOutGame = GameManager.Instance.flipOutGame;

        //StartGameServer();
    }

    // Call this directly ~
    public void EndGame()
    {
        if (!isServer)
        {
            Debug.LogError("EndGameServer: not server!");
            return;
        }

        FlipOutActions endGameAction = FlipOutActions.CreateEndGameAction(
            gameStateServer.GetActivePlayer().playerId
        );

        //if (isHotseatGame) // no Hotseat check required here, if 1 client, 1 action tracked
        GameStateClient.AddUncountedActionTakenForAll(endGameAction);

        Debug.Log("ServerDispatch->EndGame(): Ending game on server...");
        gameStateServer.Cleanup();
        if (isHotseatGame)
        {
            // handled later (and needs to be):
            //GameStateClient.CleanupClients();
            //isHotseatGame = false;
        }
        isServer = false;
        gameStateServer = null;
        // if isHotseat
        //GameManager.Instance.EndGameClient(-1);

    }

    // private (called by Hotseat or Online start)
    private void StartGameServer()
    {
        if (!isServer)
        {
            Debug.LogError("StartGameStateServer: not server!");
            return;
        }
        // part of InitGameStateServer:
        //GameManager.Instance.gameStateServer.currentplayerIndex = 0;

        // Deal cards to players
        DealCardsToPlayers();
        StartTurn();
    }

    private bool DealCardsToPlayerHandIndices(int playerId, int[] handIndices)
    {
        int playerNum = gameStateServer.GetPlayerNumberByID(playerId);
        List<CardPODServer> cards = gameStateServer.DrawCards(handIndices.Length, playerId);

        if (cards == null)
        {
            Debug.LogWarning("ServerDispatch->DealCardsToPlayerHandIndices(): end game - not enough draw cards for player " + playerId + "!");
            //EndGame();
            return false;
        }

        CardColor deckTopColor = gameStateServer.PeekTopDrawCardColor();

        CardActionInfo[] dealtCardInfos = new CardActionInfo[handIndices.Length];
        CardActionInfo[] dealtCardInfosForOpponents = new CardActionInfo[handIndices.Length];

        for (int i = 0; i < handIndices.Length; i++)
        {
            dealtCardInfos[i] = new CardActionInfo
            {
                cardID = cards[i].cardID,
                cardColor = cards[i].ColorBasedOnPlayer(playerId)
            };
            dealtCardInfosForOpponents[i] = new CardActionInfo
            {
                cardID = cards[i].cardID,
                cardColor = cards[i].ColorBasedOnPlayer(-9999) // unknown to opponent
            };

            cards[i].state = CardState.playerHolder;
            //cards[i].ownerPlayerID = playerId;    // already set in DrawCards()
        }
        // Create Deal action for player
        FlipOutActions dealAction = FlipOutActions.CreateDealAction(
            playerId,
            dealtCardInfos,
            handIndices, deckTopColor
        );
        FlipOutActions dealActionForOpponents = FlipOutActions.CreateDealAction(
            playerId,
            dealtCardInfosForOpponents,
            handIndices, deckTopColor
        );
        // Apply to GameStateServer
        //! GameStateServer won't 'see' both sides of cards in these actions, but can lookup based on cardID
        gameStateServer.AddUncountedActionTaken(dealAction);
        gameStateServer.AssignCardsToPlayerHand(playerNum, cards, handIndices);

        GameStateClient.AssignDeckTopCard(deckTopColor);

        if (isHotseatGame)
        {
            GameStateClient.GetHotseatGameStateForPlayerNumber(playerNum).AddUncountedActionTaken(dealAction);
            GameStateClient.AddUncountedActionTakenForOpponentViews(playerNum, dealActionForOpponents);

            //GameStateClient.GetHotseatGameStateForPlayerNumber(playerNum).AssignCardsToPlayerHand(playerNum, cardsForPlayer, positions);
            // Update GameStateClient for each player (static method for obvious reasons)
            //GameStateClient.AssignCardsToPlayerHandForOpponentViews(playerNum, cardsForOpponentViews, positions);
        }
        else
        {
            //
        }
        return true;
    }

    private void DealCardsToPlayers()
    {
        if (!isServer)
        {
            Debug.LogError("DealCardsToPlayersServer: not server!");
            return;
        }
        Debug.Log("ServerDispatch->DealCardsToPlayers(): Dealing cards to players...");

        int clientPlayerNum = GameStateClient.CurrentGameStateClient.localPlayerNumber;

        for (int playerNum = 0; playerNum < gameStateServer.GetTotalPlayers(); playerNum++)
        {
            int playerId = gameStateServer.playersServer[playerNum].playerId;

            List<CardPODServer> cards = gameStateServer.DrawCards(6, playerId);
            CardColor deckTopColor = gameStateServer.PeekTopDrawCardColor();
            List<CardPODClient> cardsForPlayer = new List<CardPODClient>(cards.Count);
            List<CardPODClient> cardsForOpponentViews = new List<CardPODClient>(cards.Count);

            CardActionInfo[] dealtCardInfos = new CardActionInfo[6];
            CardActionInfo[] dealtCardInfosForOpponents = new CardActionInfo[6];

            int[] positions = new int[6];

            for (int i = 0; i < 6; i++)
            {
                // left to right 0 - 5
                positions[i] = i;

                dealtCardInfos[i] = new CardActionInfo
                {
                    cardID = cards[i].cardID,
                    cardColor = cards[i].ColorBasedOnPlayer(playerId)
                };
                dealtCardInfosForOpponents[i] = new CardActionInfo
                {
                    cardID = cards[i].cardID,
                    cardColor = cards[i].ColorBasedOnPlayer(-9999) // unknown to opponent
                };

                //Debug.Log("Position " + i + " dealtcard colors for player " + playerNum + ": " + dealtCardInfos[i].cardColor +
                //        ", for opponents: " + dealtCardInfosForOpponents[i].cardColor);

                cards[i].state = CardState.playerHolder;
                //cards[i].ownerPlayerID = playerId;    // already set in DrawCards()

                // Tailor for client view
                cardsForPlayer.Add(cards[i].CopyToClientCard(-1));
                // And opponent views
                cardsForOpponentViews.Add(cards[i].CopyToClientCard(-9999));
            }

            // Create Deal action for player
            FlipOutActions dealAction = FlipOutActions.CreateDealAction(
                gameStateServer.playersServer[playerNum].playerId,
                dealtCardInfos,
                positions, deckTopColor
            );
            FlipOutActions dealActionForOpponents = FlipOutActions.CreateDealAction(
                gameStateServer.playersServer[playerNum].playerId,
                dealtCardInfosForOpponents,
                positions, deckTopColor
            );
            // Apply to GameStateServer
            //! GameStateServer won't 'see' both sides of cards in these actions, but can lookup based on cardID
            gameStateServer.AddUncountedActionTaken(dealAction);
            gameStateServer.AssignCardsToPlayerHand(playerNum, cards, positions);

            GameStateClient.AssignDeckTopCard(deckTopColor);

            if (isHotseatGame)
            {
                GameStateClient.GetHotseatGameStateForPlayerNumber(playerNum).AddUncountedActionTaken(dealAction);
                GameStateClient.AddUncountedActionTakenForOpponentViews(playerNum, dealActionForOpponents);

                //GameStateClient.GetHotseatGameStateForPlayerNumber(playerNum).AssignCardsToPlayerHand(playerNum, cardsForPlayer, positions);
                // Update GameStateClient for each player (static method for obvious reasons)
                //GameStateClient.AssignCardsToPlayerHandForOpponentViews(playerNum, cardsForOpponentViews, positions);
            }
            else
            {
                //
            }
        }
        // Draw once all hands are configured
        //!No - StartTurn() will construct cards this way
        //if (isHotseatGame) { GameManager.Instance.DealAllHandsClientFromState(); }
    }


    // called internally:
    private void StartTurn()
    {
        if (!isServer)
        {
            Debug.LogError("StartTurnServer: not server!");
            return;
        }
        //Debug.Log("ServerDispatch->StartTurnServer(): Starting turn for player index " + gameStateServer.currentPlayerIndex);

        //int currentPlayerId = gameStateServer.playersServer[gameStateServer.currentPlayerIndex].playerId;

        // Notify GameManager of new turn
        //GameManager.Instance.StartPlayerTurn(currentPlayerId);
        Debug.Log("ServerDispatch: Starting turn for player " + gameStateServer.GetActivePlayerNumber() + " (" + gameStateServer.GetActivePlayer().playerName + ")");

        // This seemed unnecessary since we just log end of turns (which suggests there is a turn start next):
        //FlipOutActions startTurnAction = FlipOutActions.CreateTurnStartAction();

        GameStateClient.CurrentGameStateClient.SetActionsAvailableThisTurn(
            gameStateServer.GetAvailableActionsForPlayer(gameStateServer.GetActivePlayer())
        );

        if (isHotseatGame)
        {
            GameManager.Instance.flipOutGame.StartPlayerTurnClient(
                gameStateServer.GetActivePlayerNumber(),
                gameStateServer.GetActivePlayer().playerId,
                gameStateServer.GetAvailableActionsForPlayer(gameStateServer.GetActivePlayer())
            );
        }
        // Notify clients of turn start?
        //GameManager.Instance.StartPlayerTurnClient(gameStateServer.GetActivePlayerNumber(), gameStateServer.GetActivePlayer().playerId, gameStateServer.GetAvailableActionsForPlayer(gameStateServer.GetActivePlayer()));
        //SetActionsAvailableThisTurn
    }

    // call directly or roundabout through network message:
    public void EndTurn()
    {
        if (!isServer)
        {
            Debug.LogError("EndTurnServer: not server!");
            return;
        }
        Debug.Log("GameState: Ending turn for player " + gameStateServer.GetActivePlayerNumber() + " (" + gameStateServer.GetActivePlayer().playerName + ")");

        FlipOutActions endTurnAction = FlipOutActions.CreateTurnEndAction(
            gameStateServer.GetActivePlayer().playerId
        );
        gameStateServer.AddUncountedActionTaken(endTurnAction);

        //if (isHotseatGame) // no Hotseat check required here, if 1 client, 1 action tracked
        GameStateClient.AddUncountedActionTakenForAll(endTurnAction);

        if (isHotseatGame)
        {
            flipOutGame.ActOnFlipOutActionsForCurrentPlayer();
            //GameManager.Instance.EndTurnClient();
        }
        //! online - any messages to clients?      
    }

    public void AdvanceToNextPlayer()
    {
        int nextPlayerNum = gameStateServer.AdvanceToNextPlayer();
        if (isHotseatGame)
        {
            GameStateClient.CurrentGameStateClient.AdvanceToNextPlayer();
        }
        StartTurn();  
    }

    // call directly or roundabout through network message:
    //! Note: FlipCard action on playback is unique - it will reverse flip action
    //!       if cardId is owned by the current player, otherwise we assume opposite-side as source for all others
    public void FlipCard(int playerId, int cardId)
    {
        if (!isServer)
        {
            Debug.LogError("FlipCardServer: not server!");
            return;
        }
        if (gameStateServer.GetActivePlayer().playerId != playerId)
        {
            Debug.LogError("ServerDispatch->FlipCard(): It's not player " + playerId + "'s turn!");
            return;
        }
        
        if (gameStateServer.GetCurrentPlayerActionsTaken() == 2)
        {
            Debug.LogError("ServerDispatch->FlipCard(): Player " + playerId + " has already taken 2 actions this turn!");
            return;
        }
        Debug.Log("ServerDispatch->FlipCard(): Player " + playerId + " flipping card " + cardId);

        // Validate (in this case, flip card is always available)
        /*if (!gameStateServer.GetAvailableActionsForPlayer(gameStateServer.GetPlayerByID(playerId)).HasFlag(TurnAction.Flip))
        {
            Debug.LogError("ServerDispatch->FlipCard(): Player " + playerId + " cannot flip card now!");
            return;
        }*/

        PlayerXServer player = gameStateServer.GetPlayerByID(playerId);
        CardPODServer cardPOD = gameStateServer.GetCardByID(cardId);
        if (player == null || cardPOD == null)
        {
            Debug.LogError("ServerDispatch->FlipCard(): invalid player or card!");
            return;
        }

        int owningPlayerId = cardPOD.ownerPlayerID;        

        // Create Flip action
        //! We ignore playerId and set this up for opposite-side flip to facing-side
        //  and handle playback in reverse if current player owns the card at time of playback
        CardActionInfo facingOtherPlayersInfo = new CardActionInfo
        {
            cardID = cardPOD.cardID,
            cardColor = cardPOD.GetOppositeColor()
        };
        CardActionInfo facingOwnerPlayerInfo = new CardActionInfo
        {
            cardID = cardPOD.cardID,
            cardColor = cardPOD.GetFacingColor()
        };
        FlipOutActions flipAction = FlipOutActions.CreateFlipAction(
            playerId,
            owningPlayerId,
            facingOtherPlayersInfo,    // source - facing-other-players color
            facingOwnerPlayerInfo    // dest - facing-owner color
        );
        //! Only single flipAction created and used for both current player and opponents

        // flip on the server (always correct)
        cardPOD.FlipCard();

        // Apply to GameStateServer
        gameStateServer.AddPlayerActionTaken(gameStateServer.GetActivePlayerNumber(), flipAction);

        // Apply to GameStateClient(s)
        if (isHotseatGame)
        {
            GameStateClient.AddPlayerActionTakenForAll(flipAction);
            
            //GameStateClient.AddPlayerActionTakenForOpponentViews(playerId, flipActionForOpponents, false);

            //GameManager.Instance.FlipCardClient(cardId, oppositeSideInfo.cardColor);
            //flipOutGame.ActOnFlipOutActionForCurrentPlayer(flipAction);
            //GameStateClient.CurrentGameStateClient.ClearActionsSinceLastTurn();
            flipOutGame.ActOnFlipOutActionsForCurrentPlayer();
        }
        else
        {
            // Send message server->client
            //GameManager.Instance.networkManager.SendFlipOutActionToAllClients(flipAction);
        }
    }

    public void SwitchCards(int playerId, int cardId1, int cardId2)
    {
        if (!isServer)
        {
            Debug.LogError("SwitchCards: not server!");
            return;
        }
        if (gameStateServer.GetActivePlayer().playerId != playerId)
        {
            Debug.LogError("ServerDispatch->SwitchCards(): It's not player " + playerId + "'s turn!");
            return;
        }
        
        if (gameStateServer.GetCurrentPlayerActionsTaken() == 2)
        {
            Debug.LogError("ServerDispatch->SwitchCards(): Player " + playerId + " has already taken 2 actions this turn!");
            return;
        }
        Debug.Log("ServerDispatch->SwitchCards(): Player " + playerId + " switching cards " + cardId1 + " and " + cardId2);

        // Validate (in this case, switch cards is always available)
        /*if (!gameStateServer.GetAvailableActionsForPlayer(gameStateServer.GetPlayerByID(playerId)).HasFlag(TurnAction.Switch))
        {
            Debug.LogError("ServerDispatch->SwitchCards(): Player " + playerId + " cannot switch cards now!");
            return;
        }*/

        PlayerXServer player = gameStateServer.GetPlayerByID(playerId);
        CardPODServer cardPOD1 = gameStateServer.GetCardByID(cardId1);
        CardPODServer cardPOD2 = gameStateServer.GetCardByID(cardId2);

        if (player == null || cardPOD1 == null || cardPOD2 == null)
        {
            Debug.LogError("ServerDispatch->SwitchCards(): invalid player or card(s)!");
            return;
        }

        int owningPlayerId = cardPOD1.ownerPlayerID;
        if (cardPOD1.ownerPlayerID != cardPOD2.ownerPlayerID)
        {
            Debug.LogError("ServerDispatch->SwitchCards(): card'1s owner player id (" + owningPlayerId + 
                ") != card2 owner player id (" + cardPOD2.ownerPlayerID + ")!");
            return;
        }

        
        // Create Flip action
        CardActionInfo switchCard1Info = new CardActionInfo
        {
            cardID = cardId1,
            cardColor = (playerId == owningPlayerId) ? cardPOD1.GetFacingColor() : cardPOD1.GetOppositeColor()
        };
        CardActionInfo oppositeSide1Info = new CardActionInfo
        {
            cardID = cardId1,
            cardColor = (playerId == owningPlayerId) ? cardPOD1.GetOppositeColor() : cardPOD1.GetFacingColor()
        };
        CardActionInfo switchCard2Info = new CardActionInfo
        {
            cardID = cardId2,
            cardColor = (playerId == owningPlayerId) ? cardPOD2.GetFacingColor() : cardPOD2.GetOppositeColor()
        };
        CardActionInfo oppositeSide2Info = new CardActionInfo
        {
            cardID = cardId2,
            cardColor = (playerId == owningPlayerId) ? cardPOD2.GetOppositeColor() : cardPOD2.GetFacingColor()
        };
        FlipOutActions switchAction = FlipOutActions.CreateSwitchAction(
            playerId,
            owningPlayerId,
            switchCard1Info,
            switchCard2Info
        );
        FlipOutActions switchActionForOpponents = FlipOutActions.CreateSwitchAction(
            playerId,
            owningPlayerId,
            oppositeSide1Info,
            oppositeSide2Info
        );

        // Apply to GameStateServer
        gameStateServer.AddPlayerActionTaken(gameStateServer.GetActivePlayerNumber(), switchAction);

        //! Apply hand-switch to GameStateServer
        gameStateServer.SwitchCardsInPlayerHand(owningPlayerId, cardId1, cardId2);

        // Apply to GameStateClient(s)
        if (isHotseatGame)
        {
            GameStateClient.CurrentGameStateClient.AddPlayerActionTaken(
                playerId,
                switchAction
            );
            
            GameStateClient.AddPlayerActionTakenForOpponentViews(
                playerId,
                switchActionForOpponents, false
            );

            //GameManager.Instance.SwitchCardsClient(cardId1, cardId2);
            //flipOutGame.ActOnFlipOutActionForCurrentPlayer(switchAction);
            //GameStateClient.CurrentGameStateClient.ClearActionsSinceLastTurn();
            flipOutGame.ActOnFlipOutActionsForCurrentPlayer();
        }
        else
        {
            // Send message server->client
            //GameManager.Instance.networkManager.SendFlipOutActionToAllClients(switchAction);
        }
    }

    //! Important: card swapping appears to all other players as both a flip and swap
    // but to the players involved in the swap, the cards don't change colors (just ownership)
    // the card data on the server swaps facing/opposite sides though
    public void SwapCards1(int playerId, int cardId1, int cardSwapWith1)
    {
        if (!isServer)
        {
            Debug.LogError("SwapCards: not server!");
            return;
        }
        if (gameStateServer.GetActivePlayer().playerId != playerId)
        {
            Debug.LogError("ServerDispatch->SwapCards1(): It's not player " + playerId + "'s turn!");
            return;
        }
        
        if (gameStateServer.GetCurrentPlayerActionsTaken() == 2)
        {
            Debug.LogError("ServerDispatch->SwapCards1(): Player " + playerId + " has already taken 2 actions this turn!");
            return;
        }
        Debug.Log("ServerDispatch->SwapCards1(): Player " + playerId + " swapping cards " + cardId1 + " and " + cardSwapWith1);

        // Validate (in this case, swap1 cards is always available)
        /*if (!gameStateServer.GetAvailableActionsForPlayer(gameStateServer.GetPlayerByID(playerId)).HasFlag(TurnAction.Swap1))
        {
            Debug.LogError("ServerDispatch->SwapCards1(): Player " + playerId + " cannot swap cards now!");
            return;
        }*/

        PlayerXServer playerSwapping = gameStateServer.GetPlayerByCardId(cardId1);
        PlayerXServer playerSwapWith = gameStateServer.GetPlayerByCardId(cardSwapWith1);

        if (playerSwapping == null || playerSwapWith == null)
        {
            Debug.LogError("ServerDispatch->SwapCards1(): invalid player(s)!");
            return;
        }        
        if (playerSwapping.playerId != playerId && playerSwapWith.playerId != playerId)
        {
            Debug.LogWarning("ServerDispatch->SwapCards1(): Player " + playerId + " does not own either card " + cardId1 + " or card " + cardSwapWith1 + "!");
            return;
        }
        if (playerSwapping == playerSwapWith)
        {
            Debug.LogWarning("ServerDispatch->SwapCards1(): both cards belong to same player (only Switch can be used for that)!");
            return;
        }
        // Establish playerSwapping as owner of cardId1, playerSwapWith as owner of cardSwapWith1
        if (playerSwapping.playerId != playerId)
        {
            //playerSwapWith is playerId
            // swap players and ids for clarity's sake
            PlayerXServer tempPlayer = playerSwapping;
            playerSwapping = playerSwapWith;
            playerSwapWith = tempPlayer;
            // Now playerSwapping.playerId == playerId. But cardId1 belongs to playerSwapWith
            int tempId = cardId1;
            cardId1 = cardSwapWith1;
            cardSwapWith1 = tempId;
        }

        CardPODServer cardPOD1 = playerSwapping.GetCardInHandByID(cardId1);
        CardPODServer cardPOD2 = playerSwapWith.GetCardInHandByID(cardSwapWith1);
    
        // Create Swap action
        CardActionInfo swapCard1Info = new CardActionInfo
        {
            cardID = cardId1,
            cardColor = cardPOD1.GetFacingColor()
        };
        CardActionInfo oppositeSide1Info = new CardActionInfo
        {
            cardID = cardId1,
            cardColor = cardPOD1.GetOppositeColor()
        };
        CardActionInfo swapWithCard1Info = new CardActionInfo
        {
            cardID = cardSwapWith1,
            cardColor = cardPOD2.GetFacingColor()
        };
        CardActionInfo oppositeSideSwapWith1Info = new CardActionInfo
        {
            cardID = cardSwapWith1,
            cardColor = cardPOD2.GetOppositeColor()
        };
        FlipOutActions swap1Action = FlipOutActions.CreateSwap1Action(
            playerId,
            playerSwapWith.playerId,
            swapCard1Info,
            swapWithCard1Info
        );
        /*FlipOutActions swap1ActionForOpponents = FlipOutActions.CreateSwap1Action(
            playerId,
            playerSwapWith.playerId,
            oppositeSide1Info,
            oppositeSideSwapWith1Info
        );*/

        // Apply to GameStateServer
        gameStateServer.AddPlayerActionTaken(gameStateServer.GetActivePlayerNumber(), swap1Action);

        //! Apply hand-swap to GameStateServer
        gameStateServer.Swap1CardBetweenPlayers(playerId, playerSwapWith.playerId, cardId1, cardSwapWith1);
        
        // Apply to GameStateClient(s)
        if (isHotseatGame)
        {
            GameStateClient.AddPlayerActionTakenForAll(swap1Action);
            
            //GameStateClient.AddPlayerActionTakenForOpponentViews(playerId, swap1ActionForOpponents, false);

            //GameManager.Instance.SwitchCardsClient(cardId1, cardId2);
            //flipOutGame.ActOnFlipOutActionForCurrentPlayer(swapAction);
            //GameStateClient.CurrentGameStateClient.ClearActionsSinceLastTurn();
            flipOutGame.ActOnFlipOutActionsForCurrentPlayer();
        }
        else
        {
            // Send message server->client
            //GameManager.Instance.networkManager.SendFlipOutActionToAllClients(swapAction);
        }
    }

    //! Important: card swapping appears to all other players as both a flip and swap
    // but to the players involved in the swap, the cards don't change colors (just ownership)
    // the card data on the server swaps facing/opposite sides though
    public void SwapCards2(int playerId, int cardId1, int cardId2, int cardSwapWith1, int cardSwapWith2)
    {
        if (!isServer)
        {
            Debug.LogError("SwapCards2: not server!");
            return;
        }
        if (gameStateServer.GetActivePlayer().playerId != playerId)
        {
            Debug.LogError("ServerDispatch->SwapCards2(): It's not player " + playerId + "'s turn!");
            return;
        }
        
        if (gameStateServer.GetCurrentPlayerActionsTaken() == 2)
        {
            Debug.LogError("ServerDispatch->SwapCards2(): Player " + playerId + " has already taken 2 actions this turn!");
            return;
        }
        Debug.Log("ServerDispatch->SwapCards2(): Player " + playerId + " swapping cards " + cardId1 + " & " + cardId2 + " with " + cardSwapWith1 + " & " + cardSwapWith2);

        // Validate (we do the same by checking that the cardId's belong to given players 1st, then check matching colors)
        /*if (!gameStateServer.GetAvailableActionsForPlayer(gameStateServer.GetPlayerByID(playerId)).HasFlag(TurnAction.Swap2))
        {
            Debug.LogError("ServerDispatch->SwapCards2(): Player " + playerId + " cannot swap cards now!");
            return;
        }*/

        PlayerXServer playerSwapping = gameStateServer.GetPlayerByCardId(cardId1);
        PlayerXServer playerSwapWith = gameStateServer.GetPlayerByCardId(cardSwapWith1);

        if (playerSwapping == null || playerSwapWith == null)
        {
            Debug.LogError("ServerDispatch->SwapCards2(): invalid player(s)!");
            return;
        }
        // if cardId1 and cardSwapWith1 belong to same player, rearrange players/ids
        if (playerSwapping == playerSwapWith)
        {
            // cardId1 and cardSwapWith1 belong to same player, cardId2 and cardSwapWith2 must belong to different player
            playerSwapWith = gameStateServer.GetPlayerByCardId(cardSwapWith2);
            // not found OR same player again
            if (playerSwapWith == null || playerSwapWith == playerSwapping)
            {
                Debug.LogError("ServerDispatch->SwapCards2(): invalid player(s)!");
                return;
            }
            // known: cardId1 and cardSwapWith1 belong to playerSwapping, cardSwapWith2 belongs to playerSwapWith
            // not known but need to verify:
            //     cardId2  *should* belong to playerSwapWith
            if (playerSwapWith.GetIndexOfCardByID(cardId2) == -1)
            {
                Debug.LogError("ServerDispatch->SwapCards2(): cards do not belong to expected players!");
                return;
            }
            // lets rearrange ids for clarity's sake
            int tempId = cardSwapWith1;
            cardSwapWith1 = cardId2;
            cardId2 = tempId;
            // Now:
            // playerSwapWith now corresponds to cardSwapWith1 and cardSwapWith2
            // playerSwapping now corresponds to cardId1 and cardId2
        }
        else    // known: cardId1 and cardSwapWith1 belong to different players
        {
            // figure out which players cardId2 and cardSwapWith2 belong to
            int index = playerSwapping.GetIndexOfCardByID(cardId2);
            if (index == -1)
            {
                // cardId2 must belong to playerSwapWith
                index = playerSwapWith.GetIndexOfCardByID(cardId2);
                if (index == -1)
                {
                    Debug.LogError("ServerDispatch->SwapCards2(): cards do not belong to expected players!");
                    return;
                }
                // verify cardSwapWith2 belongs to playerSwapping
                if (playerSwapping.GetIndexOfCardByID(cardSwapWith2) == -1)
                {
                    Debug.LogError("ServerDispatch->SwapCards2(): cards do not belong to expected players!");
                    return;
                }

                // swap card ids for clarity's sake
                int tempId = cardSwapWith2;
                cardSwapWith2 = cardId2;
                cardId2 = tempId;
                // Now:
                // playerSwapWith now corresponds to cardSwapWith1 and cardSwapWith2
                // playerSwapping now corresponds to cardId1 and cardId2
            }
            else    // playerSwapping owns cardId1 and cardId2
            {
                // verify playerSwapWith owns cardSwapWith2
                if (playerSwapWith.GetIndexOfCardByID(cardSwapWith2) == -1)
                {
                    Debug.LogError("ServerDispatch->SwapCards2(): cards do not belong to expected players!");
                    return;
                }
                // Now:
                // playerSwapWith now corresponds to cardSwapWith1 and cardSwapWith2
                // playerSwapping now corresponds to cardId1 and cardId2
            }
        }
        // Now:
        // playerSwapWith now corresponds to cardSwapWith1 and cardSwapWith2
        // playerSwapping now corresponds to cardId1 and cardId2
        // Unknown is which is playerId
        if (playerSwapping.playerId != playerId && playerSwapWith.playerId != playerId)
        {
            Debug.LogError("ServerDispatch->SwapCards2(): Player " + playerId + " does not own either card " + cardId1 + " or card " + cardSwapWith1 + "!");
            return;
        }

        // Establish playerSwapping as owner of cardId1 & cardId2, playerSwapWith as owner of cardSwapWith1 & cardSwapWith2
        if (playerSwapping.playerId != playerId)
        {
            //playerSwapWith is playerId
            // swap players and ids for clarity's sake
            PlayerXServer tempPlayer = playerSwapping;
            playerSwapping = playerSwapWith;
            playerSwapWith = tempPlayer;
            // Now playerSwapping.playerId == playerId. But cardId1, cardId2 belongs to playerSwapWith
            int tempId = cardId1;
            cardId1 = cardSwapWith1;
            cardSwapWith1 = tempId;

            tempId = cardId2;
            cardId2 = cardSwapWith2;
            cardSwapWith2 = tempId;
            // Now order is correct!
        }

        CardPODServer cardPOD1 = playerSwapping.GetCardInHandByID(cardId1);
        CardPODServer cardPOD2 = playerSwapping.GetCardInHandByID(cardId2);
        CardPODServer cardSwapWithPOD1 = playerSwapWith.GetCardInHandByID(cardSwapWith1);
        CardPODServer cardSwapWithPOD2 = playerSwapWith.GetCardInHandByID(cardSwapWith2);

        // Verify matching colors
        if (cardPOD1.GetFacingColor() != cardPOD2.GetFacingColor() ||
            cardSwapWithPOD1.GetOppositeColor() != cardSwapWithPOD2.GetOppositeColor())
        {
            Debug.LogError("ServerDispatch->SwapCards2(): card pairs to be swapped are not adjacent matching colors!");
            return;
        }
        // Verify cards are adjacent in hands
        if (Math.Abs(playerSwapping.GetIndexOfCardByID(cardId1) - playerSwapping.GetIndexOfCardByID(cardId2)) != 1 ||
            Math.Abs(playerSwapWith.GetIndexOfCardByID(cardSwapWith1) - playerSwapWith.GetIndexOfCardByID(cardSwapWith2)) != 1)
        {
            Debug.LogError("ServerDispatch->SwapCards2(): card pairs to be swapped are not adjacent in hands!");
            return;
        }

        // Create Swap2 action
        CardActionInfo swapCard1Info = new CardActionInfo
        {
            cardID = cardId1,
            cardColor = cardPOD1.GetFacingColor()
        };
        CardActionInfo swapCard2Info = new CardActionInfo
        {
            cardID = cardId2,
            cardColor = cardPOD2.GetFacingColor()
        };
        CardActionInfo oppositeSide1Info = new CardActionInfo
        {
            cardID = cardId1,
            cardColor = (playerId == cardPOD1.ownerPlayerID) ? cardPOD1.GetOppositeColor() : cardPOD1.GetFacingColor()
        };
        CardActionInfo oppositeSide2Info = new CardActionInfo
        {
            cardID = cardId2,
            cardColor = (playerId == cardPOD2.ownerPlayerID) ? cardPOD2.GetOppositeColor() : cardPOD2.GetFacingColor()
        };
        CardActionInfo swapWithCard1Info = new CardActionInfo
        {
            cardID = cardSwapWith1,
            cardColor = cardSwapWithPOD1.GetFacingColor()
        };
        CardActionInfo swapWithCard2Info = new CardActionInfo
        {
            cardID = cardSwapWith2,
            cardColor = cardSwapWithPOD2.GetFacingColor()
        };
        CardActionInfo oppositeSideSwapWith1Info = new CardActionInfo
        {
            cardID = cardSwapWith1,
            cardColor = (playerId == cardSwapWithPOD1.ownerPlayerID) ? cardSwapWithPOD1.GetOppositeColor() : cardSwapWithPOD1.GetFacingColor()
        };
        CardActionInfo oppositeSideSwapWith2Info = new CardActionInfo
        {
            cardID = cardSwapWith2,
            cardColor = (playerId == cardSwapWithPOD2.ownerPlayerID) ? cardSwapWithPOD2.GetOppositeColor() : cardSwapWithPOD2.GetFacingColor()
        };

        // CreateSwap2Action(int playerTakingActionId, int playerTargetId, 
        //                                           CardActionInfo cardSourceInfo1, CardActionInfo cardSourceInfo2,
        //                                           CardActionInfo cardDestInfo1, CardActionInfo cardDestInfo2)
        FlipOutActions swap2Action = FlipOutActions.CreateSwap2Action(
            playerId,
            playerSwapWith.playerId,
            swapCard1Info, swapCard2Info,
            swapWithCard1Info, swapWithCard2Info
        );
        FlipOutActions swap2ActionForOpponents = FlipOutActions.CreateSwap2Action(
            playerId,
            playerSwapWith.playerId,
            oppositeSide1Info, oppositeSide2Info,
            oppositeSideSwapWith1Info, oppositeSideSwapWith2Info
        );

        // Apply to GameStateServer
        gameStateServer.AddPlayerActionTaken(gameStateServer.GetActivePlayerNumber(), swap2Action);

        //! Apply hand-swap2 to GameStateServer
        gameStateServer.Swap2CardsBetweenPlayers(playerId, playerSwapWith.playerId, cardId1, cardId2, cardSwapWith1, cardSwapWith2);
        
        // Apply to GameStateClient(s)
        if (isHotseatGame)
        {
            GameStateClient.AddPlayerActionTakenForAll(swap2Action);
            
            //GameStateClient.AddPlayerActionTakenForOpponentViews(playerId, swap2ActionForOpponents, false);

            //GameManager.Instance.SwitchCardsClient(cardId1, cardId2);
            //flipOutGame.ActOnFlipOutActionForCurrentPlayer(swapAction);
            //GameStateClient.CurrentGameStateClient.ClearActionsSinceLastTurn();
            flipOutGame.ActOnFlipOutActionsForCurrentPlayer();
        }
        else
        {
            // Send message server->client
            //GameManager.Instance.networkManager.SendFlipOutActionToAllClients(swapAction);
        }
    }

    public void ScoreCards(int playerId, int cardId)
    {
        if (!isServer)
        {
            Debug.LogError("ScoreCards: not server!");
            return;
        }
        Debug.Log("ServerDispatch->ScoreCards(): Player " + playerId + " scoring cards based on: " + cardId);


        PlayerXServer player = gameStateServer.GetPlayerByID(playerId);
        if (player == null)
        {
            Debug.LogError("ServerDispatch->ScoreCards(): invalid player!");
            return;
        }

        if (player.GetIndexOfCardByID(cardId) == -1)    //! Difference between this and swipe (swipe must be another player)
        {
            Debug.LogError("ServerDispatch->ScoreCards(): player " + playerId + " does not own card " + cardId + "!");
            return;
        }
    
        //! This action needs to change for swipe (looking at opposite side colors)
        int[] adjacentCardIndices = GameStateClient.GetAdjacentColorsIndicesBasedOnCardId(cardId);
        if (adjacentCardIndices.Length < 4)
        {
            Debug.Log("ServerDispatch->ScoreCards(): need at least 4 adjacent same-color cards to score!");
            return;
        }
 
        CardActionInfo[] cardInfos = new CardActionInfo[adjacentCardIndices.Length];;
        for (int i = 0; i < adjacentCardIndices.Length; i++)
        {
            CardPODServer cardPOD = player.hand[adjacentCardIndices[i]];

            cardInfos[i] = new CardActionInfo
            {
                cardID = cardPOD.cardID,
                cardColor = cardPOD.GetFacingColor()   //! Main difference between this and swipe - facing/opposite cardface
            };
        }

        FlipOutActions scoreAction = FlipOutActions.CreateScoreAction(
            playerId,
            cardInfos,
            adjacentCardIndices
        );

        // Apply to GameStateServer
        gameStateServer.AddPlayerActionTaken(player.playerNumber, scoreAction);
        gameStateServer.ScoreCardsFromPlayerHand(playerId, adjacentCardIndices);        

        if (isHotseatGame)
        {
            GameStateClient.AddPlayerActionTakenForAll(scoreAction);
        }
        //else {}

        // 2nd action:
        if (!DealCardsToPlayerHandIndices(playerId, adjacentCardIndices))
        {
            Debug.LogWarning("ServerDispatch->ScoreCards(): end game reached");
            EndGame();
            //return;
        }

        if (isHotseatGame)
        {
            flipOutGame.ActOnFlipOutActionsForCurrentPlayer();
        }
    }

    public void SwipeCards(int playerId, int cardId)
    {
        if (!isServer)
        {
            Debug.LogError("SwipeCards: not server!");
            return;
        }
        Debug.Log("ServerDispatch->SwipeCards(): Player " + playerId + " swiping cards based on: " + cardId);


        PlayerXServer player = gameStateServer.GetPlayerByID(playerId);
        PlayerXServer targetPlayer = gameStateServer.GetPlayerByCardId(cardId);
        if (player == null || targetPlayer == null)
        {
            Debug.LogError("ServerDispatch->SwipeCards(): one or more invalid players!");
            return;
        }

        if (player.GetIndexOfCardByID(cardId) != -1)    // Player must not own card (swipe must be another player)
        {
            Debug.LogError("ServerDispatch->SwipeCards(): player " + playerId + " owns card " + cardId + "!");
            return;
        }
    
        //! This action ??needs to change?? in swipe (looking at opposite side colors)
        // Thinking this through - the player sees the opposite-side colors and its represented as such in the
        //  gamestateclient as opposite-side colors, soo it should be fine?
        int[] adjacentCardIndices = GameStateClient.GetAdjacentColorsIndicesBasedOnCardId(cardId);
        if (adjacentCardIndices.Length < 4)
        {
            Debug.Log("ServerDispatch->SwipeCards(): need at least 4 adjacent same-color cards to score!");
            return;
        }
 
        CardActionInfo[] cardInfos = new CardActionInfo[adjacentCardIndices.Length];;
        for (int i = 0; i < adjacentCardIndices.Length; i++)
        {
            CardPODServer cardPOD = targetPlayer.hand[adjacentCardIndices[i]];

            cardInfos[i] = new CardActionInfo
            {
                cardID = cardPOD.cardID,
                cardColor = cardPOD.GetOppositeColor()   //! Main difference between this and score - facing vs opposite cardface
            };
        }

        //CreateSwipeAction(int playerTakingActionId, int playerTargetId, CardActionInfo[] cardInfos, int[] positions)
        FlipOutActions swipeAction = FlipOutActions.CreateSwipeAction(
            playerId, targetPlayer.playerId,
            cardInfos,
            adjacentCardIndices
        );

        // Apply to GameStateServer
        gameStateServer.AddPlayerActionTaken(player.playerNumber, swipeAction);
        gameStateServer.SwipeCardsFromPlayerHand(playerId, targetPlayer.playerId, adjacentCardIndices);        

        if (isHotseatGame)
        {
            GameStateClient.AddPlayerActionTakenForAll(swipeAction);
        }
        //else {}

        // 2nd action:
        if (!DealCardsToPlayerHandIndices(targetPlayer.playerId, adjacentCardIndices))
        {
            Debug.LogError("ServerDispatch->SwipeCards(): end game reached");
            EndGame();
            //return;
        }

        if (isHotseatGame)
        {
            flipOutGame.ActOnFlipOutActionsForCurrentPlayer();
        } 
    }


    //public void TurnActionComplete() {}

    // On Netcode methods, these would be used to indicate where to send the RPC
    //[Rpc(SendTo.Server)]
    //[Rpc(SendTo.NotServer)]
    //[Rpc(SendTo.ClientsAndHost)]

    //[ClientRpc]

}
