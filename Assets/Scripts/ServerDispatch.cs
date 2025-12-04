using System.Collections.Generic;
using UnityEngine;

public class ServerDispatch
{
    public bool isServer = false; // Placeholder for server check
    private bool isHotseatGame = false;

    PlayerSessionManager sessionManager = new PlayerSessionManager();

    GameStateServer gameStateServer = null;
    

    // GameManager calls this
    public void StartHotseatGame(int[] playerIds, string[] playerNames)
    {
        Debug.Log("ServerDispatch->StartHotseatGame(): Starting hotseat game...");

        isServer = true;
        isHotseatGame = true;

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

        //StartGameServer();
    }

    // Call this directly
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
            GameStateClient.CleanupClients();
            isHotseatGame = false;
        }
        isServer = false;
        gameStateServer = null;
        // if isHotseat
        GameManager.Instance.EndGameClient();
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
                // Send message server->client
                SendCardsToPlayer(playerId, dealAction);

                // opponents see opposite sides of cards (server->client message)
                ShowDealtCardsToOpponents(playerId, dealActionForOpponents);
            }
        }
        // Draw once all hands are configured
        //!No - StartTurn() will construct cards this way
        //if (isHotseatGame) { GameManager.Instance.DealAllHandsClientFromState(); }
    }

    // Server-Client message (actions inside are when received):
    private void SendCardsToPlayer(int playerId, FlipOutActions dealAction)
    {
        // Send to specific player only
        //GameManager.Instance.networkManager.SendFlipOutActionToPlayer(playerId, dealAction);
        int playerNum = gameStateServer.GetPlayerNumberByID(playerId);

        CardPODClient[] hand = new CardPODClient[6];
        for (int i = 0; i < dealAction.cardSourceInfos.Length; i++)
        { 
            hand[dealAction.positions[i]] = new CardPODClient
            {
                cardID = dealAction.cardSourceInfos[i].cardID,
                color = dealAction.cardSourceInfos[i].cardColor,
                state = CardState.playerHolder,
                ownerPlayerID = playerId
            };
        }

        GameManager.Instance.DealFullHandClient(playerNum, hand);
    }
    
    // Server-Client message (actions inside are when received):
    private void ShowDealtCardsToOpponents(int dealtPlayerId, FlipOutActions dealActionForOpponent)
    {
        int dealtPlayerNum = gameStateServer.GetPlayerNumberByID(dealtPlayerId);
        CardPODClient[] hand = new CardPODClient[6];
        for (int i = 0; i < dealActionForOpponent.cardSourceInfos.Length; i++)
        {
            hand[dealActionForOpponent.positions[i]] = new CardPODClient
            {
                cardID = dealActionForOpponent.cardSourceInfos[i].cardID,
                color = dealActionForOpponent.cardSourceInfos[i].cardColor,
                state = CardState.playerHolder,
                ownerPlayerID = dealtPlayerId
            };
        }
        // Send to all other players except 'dealingPlayerId'
        for (int playerNum = 0; playerNum < gameStateServer.GetTotalPlayers(); playerNum++)
        {
            int opponentPlayerId = gameStateServer.GetPlayerIDByNumber(playerNum);
            if (dealtPlayerNum != playerNum)
            {
                GameManager.Instance.ShowOpponentFullHandClient(dealtPlayerNum, hand);
            }
        }
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
            GameManager.Instance.StartPlayerTurnClient(
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
            GameManager.Instance.EndTurnClient();
        }
        //! online - any messages to clients?
       
        int nextPlayerNum = gameStateServer.AdvanceToNextPlayer();
        if (isHotseatGame)
        {
            GameStateClient.CurrentGameStateClient.AdvanceToNextPlayer();
        }
        StartTurn();
    }

    // call directly or roundabout through network message:
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
        CardActionInfo flippedCardInfo = new CardActionInfo
        {
            cardID = cardPOD.cardID,
            cardColor = (playerId == owningPlayerId) ? cardPOD.GetFacingColor() : cardPOD.GetOppositeColor()
        };
        CardActionInfo oppositeSideInfo = new CardActionInfo
        {
            cardID = cardPOD.cardID,
            cardColor = (playerId == owningPlayerId) ? cardPOD.GetOppositeColor() : cardPOD.GetFacingColor()
        };
        FlipOutActions flipAction = FlipOutActions.CreateFlipAction(
            playerId,
            owningPlayerId,
            flippedCardInfo,    // source - current facing-player color
            oppositeSideInfo    // dest - opposite color
        );
        FlipOutActions flipActionForOpponents = FlipOutActions.CreateFlipAction(
            playerId,
            owningPlayerId,
            oppositeSideInfo,
            flippedCardInfo
        );

        cardPOD.FlipCard();

        // Apply to GameStateServer
        gameStateServer.AddPlayerActionTaken(gameStateServer.GetActivePlayerNumber(), flipAction);

        // Apply to GameStateClient(s)
        if (isHotseatGame)
        {
            GameStateClient.CurrentGameStateClient.AddPlayerActionTaken(
                playerId,
                flipAction
            );
            
            GameStateClient.AddPlayerActionTakenForOpponentViews(
                playerId,
                flipActionForOpponents, false
            );

            //GameManager.Instance.FlipCardClient(cardId, oppositeSideInfo.cardColor);
            //FlipOutActions.ActOnFlipOutActionForCurrentPlayer(flipAction);
            //GameStateClient.CurrentGameStateClient.ClearActionsSinceLastTurn();
            FlipOutActions.ActOnFlipOutActionsForCurrentPlayer();
        }
        else
        {
            // Send message server->client
            //GameManager.Instance.networkManager.SendFlipOutActionToAllClients(flipAction);
        }
    }

    //public void SwitchCards()


    //private void FlipCardClient(int playerId, FlipOutActions flipAction)

    //public void TurnActionComplete() {}


    // On Netcode methods, these would be used to indicate where to send the RPC
    //[Rpc(SendTo.Server)]
    //[Rpc(SendTo.NotServer)]
    //[Rpc(SendTo.ClientsAndHost)]

    //[ClientRpc]

}
