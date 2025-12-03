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
        // Hotseat only:
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

    // GameManager should be calling this?
    // !If not - GameManager.Instance.Cleanup() ?
    public void EndGame()
    {
        if (!isServer)
        {
            Debug.LogError("EndGameServer: not server!");
            return;
        }
        Debug.Log("ServerDispatch->EndGame(): Ending game on server...");
        gameStateServer.Cleanup();
        if (isHotseatGame)
        {
            GameStateClient.CleanupClients();
            isHotseatGame = false;
        }
        isServer = false;
        gameStateServer = null;
    }

    public void StartGameServer()
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

    public void DealCardsToPlayers()
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
                positions
            );
            FlipOutActions dealActionForOpponents = FlipOutActions.CreateDealAction(
                gameStateServer.playersServer[playerNum].playerId,
                dealtCardInfosForOpponents,
                positions
            );
            // Apply to GameStateServer
            //! GameStateServer won't 'see' both sides of cards in these actions, but can lookup based on cardID
            gameStateServer.AddUncountedActionTaken(dealAction);
            gameStateServer.AssignCardsToPlayerHand(playerNum, cards, positions);

            if (isHotseatGame)
            {
                GameStateClient.CurrentGameStateClient.AddUncountedActionTaken(dealAction);
                GameStateClient.GetHotseatGameStateForPlayerNumber(playerNum).AssignCardsToPlayerHand(playerNum, cardsForPlayer, positions);

                // Update GameStateClient for each player (static method for obvious reasons)
                GameStateClient.AssignCardsToPlayerHandForOpponentViews(playerNum, cardsForOpponentViews, positions);
            }
            else
            {
                SendCardsToPlayer(playerId, dealAction);

                // opponents see opposite sides of cards
                ShowDealtCardsToOpponents(playerId, dealActionForOpponents);
            }
        }
        // Draw once all hands are configured
        if (isHotseatGame)
        {
            // Can do animation here as well
            GameManager.Instance.DealAllHandsClientFromState();
        }
    }

    public void SendCardsToPlayer(int playerId, FlipOutActions dealAction)
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
    public void ShowDealtCardsToOpponents(int dealtPlayerId, FlipOutActions dealActionForOpponent)
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

    public void StartTurn()
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
        // Notify clients of turn start?
        GameManager.Instance.StartPlayerTurnClient(gameStateServer.GetActivePlayerNumber(), gameStateServer.GetActivePlayer().playerId, gameStateServer.GetAvailableActionsForPlayer(gameStateServer.GetActivePlayer()));
    }

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
        
        int nextPlayerNum = gameStateServer.AdvanceToNextPlayer();
        if (isHotseatGame)
        {
            GameStateClient.CurrentGameStateClient.AddUncountedActionTaken(endTurnAction);   //!propagate..
            GameStateClient.CurrentGameStateClient.AdvanceToNextPlayer();
        }
        StartTurn();
    }

    //public void TurnActionComplete() {}


    // On Netcode methods, these would be used to indicate where to send the RPC
    //[Rpc(SendTo.Server)]
    //[Rpc(SendTo.NotServer)]
    //[Rpc(SendTo.ClientsAndHost)]

    //[ClientRpc]

}
