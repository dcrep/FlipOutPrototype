using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public enum FlipOutGameEvents
{
    Idle,
    CardSelected,
    ActionSelected,
    SelectingCards,
    SubmittingAction,
    ActionCompleted,
    ProcessingActions,
    UpdatingScores,
    StartingTurn,
    EndingTurn,
    Paused,
    EndingGame
}

//!TODO: UI stuff should be moved to UIManager
//!TODO: Make multi-file partial class to reduce code-per-file
public class FlipOutGame : MonoBehaviour
{

    // Delegate and Event for FlipOut event changes
    public delegate void OnFlipOutEvent(FlipOutGameEvents gameEvent);
    public static event OnFlipOutEvent onFlipOutEvent;

    // Don't touch this backing field directly (limitation of C# properties)
    [SerializeField] private FlipOutGameEvents _currentGameEvent = FlipOutGameEvents.Idle;
    public FlipOutGameEvents currentGameEvent 
    { 
        get => _currentGameEvent;
        set 
        {
            if (_currentGameEvent != value)
            {
                _currentGameEvent = value;
                onFlipOutEvent?.Invoke(value);
            }
        }
    }
    private FlipOutGameEvents previousGameEvent = FlipOutGameEvents.Idle;

    public void GameEventSaveStateForTransition()
    {
        previousGameEvent = currentGameEvent;
    }
    public void GameEventSaveStateAndTransition(FlipOutGameEvents newEvent)
    {
        previousGameEvent = currentGameEvent;
        currentGameEvent = newEvent;
    }
    public void GameEventRestoreState()
    {
        currentGameEvent = previousGameEvent;
    }

    //! TODO: Move this UI manipulation to UIManager
    TextMeshProUGUI uiText = null;

    //bool cardsShowing = false;

    bool playInitiated = false;
    [SerializeField] public ServerDispatch serverDispatch = null;

    [SerializeField] public UIManager uiManager;
    [SerializeField] public PlayerSessionManager sessionManager;

    GameStateClient gameStateClient = null;
    GameStateClient gameStateClient2 = null;

//! More UI stuff
    Vector3 drawPileDefaultPosition = new Vector3(0, 0, 0);   //(-6, -3, 0);
    private Vector3 deckOffscreenPosition = new Vector3(-1000, -1000, 0);

    [SerializeField] private Vector3[] playerPositions = new Vector3[5]
    {
        new(-6, -3, 0),    // Player 1 - Bottom center
        new(-6, 3, 0),     // Player 2 - Top center
        new(-7, 0, 0),    // Player 3 - Left center        
        new(7, 0, 0),     // Player 4 - Right center
        new(0, 0, 0)      // Player 5 - Center (?!!)
    };
    [SerializeField] private Vector3 cardHolderOffset = new Vector3(2.5f, 0, 0);
    [SerializeField] private Vector3[] playerScorePilePositions = new Vector3[5]
    {
        new(-8, -3, 0),    // Player 1 - Bottom left
        new(-8, 3, 0),     // Player 2 - Top left
        new(-9, 0, 0),    // Player 3 - Left center back        
        new(9, 0, 0),     // Player 4 - Right center back
        new(0, 4, 0)      // Player 5 - Center top (?!!)
    };

    CardObject drawPileTop = null;



    GameObject cardPrefab;

    GameObject cardsParentGO = null;
    
    GameObject playersParentGO = null;


    [SerializeField] private List<CardObject> cardsInPlay;

    [SerializeField] public List<CardObject> cardsHighlighted = new List<CardObject>();

    bool bDelayingEndTurn = false;
    int nextSortOrder = 1;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   }

    // Update is called once per frame
    void Update()
    {
        if (bDelayingEndTurn && !uiManager.animationManager.IsRunningAnimations())
        {
            Debug.Log("Delaying EndTurn completed, proceeding with EndTurn.");
            bDelayingEndTurn = false;
            EndTurnClient();
        }
    }

    //void FixedUpdate() { }


    public void StartHotseatGame()
    {
        if (GameManager.Instance.currentScene != Scenes.Game)
        {
            Debug.LogError("FlipOutGame->StartHotseatGame(): Current scene is not Game!");
            return;
        }

        // Sessions manager should already have local sessions added
        var playerSessions = sessionManager.GetAllSessions();
        int numPlayers = playerSessions.Count;
        string[] playerNames = new string[numPlayers];
        int[] playerIds = new int[numPlayers];
        for (int i = 0; i < numPlayers; i++)
        {
            Debug.Log("FlipOutGame->StartHotseatGame(): Setting up player " + i + " with name " + playerSessions[i].playerName);
            playerIds[i] = i;
            playerNames[i] = playerSessions[i].playerName;
            playerSessions[i].isReady = true;
            playerSessions[i].playerServerId = i;
        }

        //FlipOutActions.flipOutGame = this;

        uiManager.SetupPlayerUI(numPlayers, playerNames);

        // if (IsHost)
        //gameStateServer.InitGameStateServer(playerIds,playerNames);
        GameStateClient.InitGameStateClient(playerIds, playerNames);

        gameStateClient = GameStateClient.GetHotseatGameStateForPlayerNumber(0);
        gameStateClient2 = GameStateClient.GetHotseatGameStateForPlayerNumber(1);

        //totalPlayers = numPlayers;
        //currentPlayerIndex = localPlayer1Index;
        //GameManager.Instance.currentMultiplayerMode = MultiplayerMode.LocalHotseat;
        //GameManager.Instance.currentGameState = GameStatus.Playing;

        cardsInPlay = new List<CardObject>();
        cardsHighlighted = new List<CardObject>();

        playersParentGO = new GameObject("_Players");
        for (int i = 0; i < numPlayers; i++)
        {
            GameObject playerGO = new GameObject("Player" + i); //, typeof(PlayerXClient));
            playerGO.transform.SetParent(playersParentGO.transform);
        }

        playInitiated = true;

        //inputManager.activePlayerId = gameStateServer.GetActivePlayerNumber();

        //drawPileTop = InstantiateCardObjectFromPOD(gameStateServer.serverDrawPile[0], drawPileDefaultPosition, CardState.drawPile);

        // This card object is special and doesn't need to be tracked as an 'in-play' card - it only sits on 'top' of the draw pile
        //drawPileTop = InstantiateCardObjectFromPOD(new CardPODClient(), drawPileDefaultPosition, CardState.drawPile, -1);
        
        //drawPileTop.transform.SetParent(null);

        // This will call GameStateClient.InitGameStateClient() which will result in a log-error.
        // Not sure how I should do the order of calls as I need gameStateClient setup
        serverDispatch.StartHotseatGame(playerIds, playerNames);
        SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
        //TurnStart();
    }

    public void EndGameClient(int playerId)
    {
        if (!playInitiated)
        {
            Debug.LogError("->EndGameClient(): Play has not been initiated!");
            return;
        }
        // currentGameScene = game, state = playing

        //GameManager.Instance.currentGameState = GameStatus.GameOver;
        GameStateClient.GatherResults();
        Debug.Log("->EndGameClient()");
        EndGameCleanup();

        //GameManager.Instance.LoadScene(Scenes.GameOver);
    }

    public void EndTurnClient()
    {
        Debug.Log("->EndTurnClient()");
        
        if (GameManager.Instance.currentMultiplayerMode == MultiplayerMode.LocalHotseat)
        {
            //if (uiManager.animationsInProgress > 0)
            if (uiManager.animationManager.IsRunningAnimations())
            {
                Debug.Log("EndTurnClient: Animations still in progress, delaying EndTurn.");
                bDelayingEndTurn = true;
                return;
            }
            currentGameEvent = FlipOutGameEvents.EndingTurn;
            Invoke(nameof(AdvanceToNextPlayerClient), 0.5f);
        }
    }

    private void AdvanceToNextPlayerClient()
    {
        Debug.Log("FlipOut->AdvanceToNextPlayerClient()");
        // Clear board
        // (draw pile top card?)
        // Clear cards in play
        ClearObjectsInPlay();
        serverDispatch.AdvanceToNextPlayer();
    }

    public void StartPlayerTurnClient(int playerNum, int playerId, TurnAction availableActions)
    {
        Debug.Log("FlipOut->StartPlayerTurnClient(): Player " + playerId + "'s turn started.");

        PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(playerId);
        UpdatePlayerInfoText("Player " + playerId + "'s " + (playerNum == 1 ? "^" : "v") + " (" + player.playerName + ") Turn");

        currentGameEvent = FlipOutGameEvents.UpdatingScores;
        uiManager.UpdateScoresDisplay();
        currentGameEvent = FlipOutGameEvents.StartingTurn;

        // This should be done at TurnEnd:
        //ClearObjectsInPlay();

        if (GameManager.Instance.currentMultiplayerMode == MultiplayerMode.LocalHotseat)
        {
            if (GameStateClient.CurrentGameStateClient.handsDealt)
            {
                DealAllHandsClientFromState();
                BuildScorePile();
                ActOnFlipOutActionsForCurrentPlayer();
                //Debug.Log("StartPlayerTurn-> Current game event: " + currentGameEvent);
            }
            else
            {
                ActOnFlipOutActionsForCurrentPlayer();
                // Dealing is done through calls to DealFullHandClientFromState in FlipOutActions
                //DealAllHandsClientFromState();
                BuildScorePile();
                GameStateClient.CurrentGameStateClient.handsDealt = true;
                //Debug.Log("StartPlayerTurn(else)-> Current game event: " + currentGameEvent);
            }
            
        }
        //StartTurnClient(playerId, availableActions);
    }

#region Cleanup

    public void ClearObjectsInPlay()
    {
        if (cardsInPlay != null)
        {
            ClearHighlightedCards();
            foreach (CardObject card in cardsInPlay)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            cardsInPlay.Clear();
        }
        Destroy(cardsParentGO);
        cardsParentGO = null;
        drawPileTop = null;
    }

    public void EndGameCleanup()
    {
        Debug.Log("->EndGameCleanup()");
        playersParentGO = null;
        //cardsShowing = false;
        drawPileTop = null;
    
        if (cardsInPlay != null)
        {
            cardsInPlay.Clear();
            cardsInPlay = null;
        }
        if (cardsHighlighted != null)
        {
            cardsHighlighted.Clear();
            cardsHighlighted = null;
        }
        GameManager.Instance.StateCleanup();
        //gameStateServer.Cleanup();
        //GameStateClient.CleanupClients();
        GameManager.Instance.currentMultiplayerMode = MultiplayerMode.Disconnected;
    }

#endregion


#region CardObject Creation
   private CardObject InstantiateCardObjectFromPOD(CardPODClient cardPOD, Vector3 position, CardState newState = CardState.playerHolder, int playerID = -1)
    {
        if (cardsParentGO == null)
        {
            cardsParentGO = new GameObject("_Cards");            
        }
        if (cardPrefab == null)
        {
            cardPrefab = Resources.Load<GameObject>("Prefabs/CardPF");
        }

        GameObject cardGO = GameObject.Instantiate(cardPrefab, position, Quaternion.identity, cardsParentGO.transform);
        //cardGO.layer = LayerMask.NameToLayer("Cards");
        cardGO.GetComponent<Renderer>().sortingLayerName = "Cards";
        CardObject cardObject = cardGO.GetComponent<CardObject>();

        // Attach Card POD to CardObject
        cardPOD.state = newState;
        cardPOD.ownerPlayerID = playerID;
        cardObject.SetCardPOD(cardPOD);

        cardsInPlay.Add(cardObject);

        return cardObject;
    }


    public CardObject CardObjectFromPODClient(int playerID, CardPODClient cardPOD)
    {
        return InstantiateCardObjectFromPOD(cardPOD, deckOffscreenPosition, CardState.playerHolder, playerID);
    }

#endregion

#region UI Updates

    public void ClearHighlightedCards()
    {
        if (cardsHighlighted != null)
        {
            foreach (CardObject card in cardsHighlighted)
            {
                if (card != null)
                {
                    card.HighlightCardToggle();
                }
            }
            cardsHighlighted.Clear();
        }
    }

    public void UpdatePlayerInfoText(string info)
    {
        if (uiText == null)
        {
            uiText = GameObject.Find("PlayerInfo").GetComponent<TextMeshProUGUI>();
        }
        if (uiText != null)
        {
            uiText.text = info;
        }
    }
    /*void UpdateScoresDisplay()
    {
        for (int playerNum = 0; playerNum < GameStateClient.GetTotalPlayers(); playerNum++)
        {
            PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(playerNum);
            scoreText[playerNum].text = "Score: " + player.scorePile.Count.ToString();
        }
    }*/
#endregion

#region Draw and Score Piles

    void BuildScorePile()
    {
        for (int playerNum = 0; playerNum < GameStateClient.GetTotalPlayers(); playerNum++)
        {
            PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(playerNum);
            Vector3 scorePilePosition = playerScorePilePositions[playerNum];

            for (int i = 0; i < player.scorePile.Count; i++)
            {
                CardPODClient cardPOD = player.scorePile[i];
                CardObject cardObject = CardObjectFromPODClient(player.playerId, cardPOD);
                cardPOD.cardObject = cardObject;

                Vector3 targetPosition = scorePilePosition;

                cardObject.SetLocalPosition(targetPosition);
                cardObject.SetLocalScale(Vector3.one * 0.5f); // Slightly smaller
                cardObject.SetSortingOrder((i+2) * 2); // On top of score pile
                // Set card state to scorePile
                cardObject.cardPOD.state = CardState.scorePile;
            }
        }
    }

    void SetDrawPileTopCard(CardColor color)
    {
        if (color == CardColor.invalid)
        {
            if (drawPileTop != null)
            {
                Debug.LogWarning("FlipOut->SetDrawPileTopCard(): color is invalid, removing drawPileTop card.");
                Destroy(drawPileTop.gameObject);
                drawPileTop = null;
            }
            return;
        }
        //else

        CardPODClient topPOD = new CardPODClient();
        // topPOD.cardID = ownerPlayerID = -1; // defaults
        topPOD.color = color;
        if (drawPileTop == null)
        {
            drawPileTop = InstantiateCardObjectFromPOD(topPOD, drawPileDefaultPosition, CardState.drawPile, -1);
            drawPileTop.SetLocalScale(new Vector3(0.75f, 0.75f, 1) );
            drawPileTop.transform.SetPositionAndRotation(drawPileDefaultPosition, Quaternion.Euler(0, 0, 90));
            drawPileTop.SetSortingOrder(0);
        }
        else
        {
            drawPileTop.SetCardPOD(topPOD);
            // scale isn't changing but keeping commented-out line from Chris' changes
            //drawPileTop.transform.localScale = new Vector3(0.5f, 0.5f, 1);
            drawPileTop.cardPOD.state = CardState.drawPile;
        }
        return;
    }
#endregion

#region Deal-or-Show Hands

    public void ShowOpponentFullHandClient(int playerNum, CardPODClient[] hand)
    {
        // Show opponent's full hand to local player
        DealFullHandClient(playerNum, hand, true);
    }

    public void DealAllHandsClientFromState()
    {
        for (int playerNum = 0; playerNum < GameStateClient.GetTotalPlayers(); playerNum++)
        {
            DealFullHandClientFromState(playerNum);
        }
        //SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
    }

    public void DealFullHandClientFromState(int targetPlayerId)
    {
        var player = GameStateClient.CurrentGameStateClient.GetPlayerByID(targetPlayerId);
        //! Can't create cardObjects for other players, just cardPODs
        CardObject[] cardObjects = new CardObject[6];

        int playerNum = player.playerNumber;
        for (int i = 0; i < player.hand.Length; i++)
        {
            cardObjects[i] = CardObjectFromPODClient(targetPlayerId, player.hand[i]); //.Clone());
            // Set card position to player position
            //! UI Stuff
            cardObjects[i].SetLocalPosition(playerPositions[playerNum] + cardHolderOffset * i);
            // Slight offset for visibility
            cardObjects[i].SetSortingOrder(1);
            // Set card state to playerHolder
            cardObjects[i].cardPOD.state = CardState.playerHolder;
        }
        SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
    }

    public void DealNewCardsToClient(int targetPlayerId, List<CardPODClient> dealtCards, int[] positions, CardColor deckTopColor)
    {
        CardObject[] cardObjects = new CardObject[dealtCards.Count];
        int playerNum = GameStateClient.CurrentGameStateClient.GetPlayerNumberByID(targetPlayerId);
        if (playerNum == -1)
        {
            Debug.LogError("FlipOut->DealNewCardsToClient(): Could not find player number for playerId " + targetPlayerId);
            return;
        }

        nextSortOrder += 10;
        if (nextSortOrder > 30)
        {
            nextSortOrder = 1;
        }
        int sortOrder = dealtCards.Count * nextSortOrder;
        for (int i = 0; i < dealtCards.Count; i++)
        {
            int cardIndex = positions[i];

            CardObject cardObject = CardObjectFromPODClient(targetPlayerId, dealtCards[i]);
            // Set card position to player position
            cardObject.SetLocalPosition(drawPileDefaultPosition);
            cardObject.SetLocalScale(new Vector3(0.75f, 0.75f, 1) );
            cardObject.transform.SetPositionAndRotation(drawPileDefaultPosition, Quaternion.Euler(0, 0, 90));
            // Set card state to playerHolder
            cardObject.cardPOD.state = CardState.playerHolder;
            // Slight offset for visibility while animating (shows over all other cards)
            cardObject.SetSortingOrder(sortOrder);
            sortOrder--;
            //cardObject.SetLocalPosition(playerPositions[playerNum] + cardHolderOffset * cardIndex);

            //Debug.Log("Some object reference is failing here. CardObject: " + (cardObject != null ? cardObject.gameObject.name : "null") +
            //          " uiManager: " + (uiManager != null ? uiManager.gameObject.name : "null") +
            //          " animationManager: " + (uiManager.animationManager != null ? uiManager.animationManager.gameObject.name : "null"));
            
            uiManager.animationManager.AddSequential( 
                new AnimationTask { Routine = uiManager.AnimateCardMovementScaleAndRotation(cardObject,
                                              playerPositions[playerNum] + cardHolderOffset * cardIndex,
                                              Vector3.one, Quaternion.identity), DelayAfter = 0.0f } 
            );
        }
        // Run and reset sorting order afterwards (to keep dealing cards on top during animation)
        //uiManager.animationManager.Run(ResetCardSortingOrdersAfterDeal);

        // Called in FlipOutActions (should I do it here instead?):
        //GameStateClient.CurrentGameStateClient.AssignCardsToPlayerHand(targetPlayerId, dealtCards, positions);

        SetDrawPileTopCard(deckTopColor);
    }

/*
    // Was to be a callback on animation end, but the playback and callback system changed:
    private void ResetCardSortingOrdersAfterDeal()
    {
        int totalPlayers = GameStateClient.GetTotalPlayers();
        for (int p = 0; p < totalPlayers; p++)
        {
            var hand = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(p).hand;
            for (int i = 0; i < hand.Length; i++)
            {
                var cardPOD = hand[i];
                if (cardPOD != null && cardPOD.cardObject != null)
                {
                    cardPOD.cardObject.SetSortingOrder(1);
                }
            }
        }
    }
*/

    public void DealNewCardsToClient(int targetPlayerId, List<CardPODClient> dealtCards, int[] dealtCardIndices)
    {
        var player = GameStateClient.CurrentGameStateClient.GetPlayerByID(targetPlayerId);
        if (player == null)
        {
            Debug.LogError("FlipOut->DealNewCardsToClient(): Could not find player number for playerId " + targetPlayerId);
            return;
        }

        int playerNum = player.playerNumber;
        for (int i = 0; i < dealtCards.Count; i++)
        {
            int cardIndex = dealtCardIndices[i];
            CardPODClient cardPOD = dealtCards[i];
            CardObject cardObject = CardObjectFromPODClient(targetPlayerId, cardPOD);
            // Set card position to player position
            cardObject.SetLocalPosition(playerPositions[playerNum] + cardHolderOffset * cardIndex);
            // Slight offset for visibility
            cardObject.SetSortingOrder(1);
            // Set card state to playerHolder
            cardObject.cardPOD.state = CardState.playerHolder;
        }
        SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
    }


    // Client-side
    public void DealFullHandClient(int playerNum, CardPODClient[] hand, bool bOpponent = false)
    {
        if (hand.Length != 6)
        {
            Debug.LogError("FlipOut->SetLocalPlayerHand(): hand length is not 6!");
            return;
        }
        // Ignoring hand that is not the active player (unless bOpponent is true, which means show opponent's deck)
        if (playerNum != GameStateClient.GetActivePlayerNumber())
        {
            if (!bOpponent)
            {
                Debug.Log("FlipOut->SetLocalPlayerHand(): ownerPlayerID does not match local active player ID! & bOpponent is false, so ignoring.");
                return;
            }
        }
        // ownerPlayerId DOES match active player, so we will NOT show what opponent sees
        else if (bOpponent)
        {
            Debug.Log("FlipOut->SetLocalPlayerHand(): Cannot show opponent deck for local active player!");
            return;
        }

        //! Can't create cardObjects for other players, just cardPODs
        CardObject[] cardObjects = new CardObject[6];

        int ownerPlayerID = GameStateClient.CurrentGameStateClient.GetPlayerIDByNumber(playerNum);;

        for (int i = 0; i < hand.Length; i++)
        {
            cardObjects[i] = CardObjectFromPODClient(ownerPlayerID, hand[i]);  //.Clone());
            // Set card position to player position
            cardObjects[i].SetLocalPosition(playerPositions[playerNum] + cardHolderOffset * i);
            // Slight offset for visibility
            cardObjects[i].SetSortingOrder(1);
            // Set card state to playerHolder
            cardObjects[i].cardPOD.state = CardState.playerHolder;

            //gameStateClient.playersClient[playerNum].hand[i] = cardObjects[i].cardPOD;
        }
        SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
        // Animate from deck to player/position (?)
        //GameStateClient.CurrentGameStateClient.SetCardsForPlayer(playerNum, hand);
    }


 #endregion

#region FllpOut Actions

    public void PlayerActionRejected(int playerId)
    {
        Debug.Log("FlipOut->PlayerActionRejected(): Player " + playerId + "'s action was rejected by server.");
        if (GameStateClient.CurrentGameStateClient.GetActivePlayer().playerId == playerId)
        {
            //Rejected, then Idle?
            currentGameEvent = FlipOutGameEvents.Idle;
        }
    }

    public void FlipCardClient(int cardID, CardColor newColor)
    {
        // Find the CardObject with the given cardID
        CardObject cardToFlip = null;
        /*foreach (Transform cardTransform in cardsParentGO.transform)
        {
            CardObject cardObject = cardTransform.GetComponent<CardObject>();
            if (cardObject != null && cardObject.cardPOD.cardID == cardID)
            {
                cardToFlip = cardObject;
                break;
            }
        }*/

        PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByCardId(cardID);
        if (player == null)
        {
            Debug.LogError("FlipOut->FlipCard(): Could not find owner player for cardID " + cardID);
            return;
        }
        int index = player.GetIndexOfCardByID(cardID);
        cardToFlip = player.hand[index].cardObject;

        if (cardToFlip != null)
        {
            Debug.Log("FlipOut->FlipCard(): Flipping card with cardID " + cardID + " to color " + newColor.ToString());
            //cardToFlip.FlipCard();
            //cardToFlip.UpdateColor(newColor);
            //StartCoroutine(uiManager.AnimateFlip(cardToFlip, newColor));
            uiManager.animationManager.AddSequential( new AnimationTask { Routine = uiManager.AnimateFlip(cardToFlip, newColor), DelayAfter = 0.1f } );
            //uiManager.animationManager.Run();
        }
        else
        {
            Debug.LogError("FlipOut->FlipCard(): No card found with cardID " + cardID);
        }
    }

    public void SwitchCardsClient(int cardID1, int cardID2)
    {
        Debug.Log("Switch Cards Started");
        // Find the CardObjects with the given cardIDs
        CardObject card1 = null;
        CardObject card2 = null;

        PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByCardId(cardID1);
        if (player == null)
        {
            Debug.LogError("FlipOut->SwitchCards(): Could not find owner player for cardID " + cardID1);
            return;
        }

        int index1 = player.GetIndexOfCardByID(cardID1);
        int index2 = player.GetIndexOfCardByID(cardID2);

        card1 = player.hand[index1].cardObject;
        card2 = player.hand[index2].cardObject;

        if (card1 != null && card2 != null)
        {
            // Swap positions
            //Vector3 tempPosition = card1.transform.position;
            //card1.transform.position = card2.transform.position;
            //card2.transform.position = tempPosition;
            //StartCoroutine(uiManager.AnimateCardMovement(card1, card2.transform.position));
            //StartCoroutine(uiManager.AnimateCardMovement(card2, card1.transform.position));
            uiManager.animationManager.AddParallel( new List<AnimationTask> {
                new AnimationTask { Routine = uiManager.AnimateCardMovement(card1, card2.transform.position), DelayAfter = 0.1f },
                new AnimationTask { Routine = uiManager.AnimateCardMovement(card2, card1.transform.position), DelayAfter = 0.1f }
            } );
            //uiManager.animationManager.Run();
            // Index of card in player's hand:
            int cardsOwnerId = card1.cardPOD.ownerPlayerID;
            //GameStateClient.CurrentGameStateClient.GetPlayerByID(cardsOwnerId).GetIndexOfCardByID(cardID1);
            //GameStateClient.CurrentGameStateClient.GetPlayerByID(cardsOwnerId).GetIndexOfCardByID(cardID2);
            
            GameStateClient.CurrentGameStateClient.SwitchCardsInPlayerHand(cardsOwnerId, cardID1, cardID2);
            //GameStateClient.CurrentGameStateClient.GetPlayerByID(cardsOwnerId).SwitchCardsInHandByID(cardID1,cardID2);
        }
        else
        {
            Debug.LogError("FlipOut->SwitchCards(): Could not find both cards with IDs " + cardID1 + " and " + cardID2);
        }
    }

    public void SwapCards1Client(int playerSwappingId, int playerSwapWithId, int cardSwappingID1, int cardSwapWithID1, CardColor swappingNewColor, CardColor swapWithNewColor)
    {
        PlayerXClient playerSwapping = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerSwappingId);
        PlayerXClient playerSwapWith = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerSwapWithId);

        if (playerSwapping == null || playerSwapWith == null)
        {
            Debug.LogError("FlipOut->SwapCards1Client(): Could not find one of the players for swapping: " + playerSwappingId + " or " + playerSwapWithId);
            return;
        }

        int indexSwappingCard1 = playerSwapping.GetIndexOfCardByID(cardSwappingID1);
        int indexSwapWithCard1 = playerSwapWith.GetIndexOfCardByID(cardSwapWithID1);

        if (indexSwappingCard1 == -1 || indexSwapWithCard1 == -1)
        {
            Debug.LogError("FlipOut->SwapCards1Client(): Could not find one of the cards for swapping: " + cardSwappingID1 + " or " + cardSwapWithID1);
            return;
        }

        CardObject cardSwapping1 = playerSwapping.hand[indexSwappingCard1].cardObject;
        CardObject cardSwapWith1 = playerSwapWith.hand[indexSwapWithCard1].cardObject;

        if (cardSwapping1 != null && cardSwapWith1 != null)
        {
            // Only update color if neither player is the local player (appears as a flip+move)
            if (playerSwappingId != GameStateClient.GetCurrentPlayerId() && playerSwapWithId != GameStateClient.GetCurrentPlayerId())
            {
                // Update color of both cards (appears as a flip+move)
                cardSwapping1.UpdateColor(swappingNewColor);
                cardSwapWith1.UpdateColor(swapWithNewColor);                
            }

            Debug.Log("FlipOut->SwapCards1Client(): Swapping card " + cardSwappingID1 + " of player " + playerSwappingId +
                      " with card " + cardSwapWithID1 + " of player " + playerSwapWithId + ", current player # is " + GameStateClient.GetCurrentPlayerNumber() +
                      " cardswapping1 transform pos: " + cardSwapping1.transform.position + " cardswapwith1 transform pos: " + cardSwapWith1.transform.position);
            
            // Swap positions
            //Vector3 tempPosition = cardSwapping1.transform.position;
            //cardSwapping1.transform.position = cardSwapWith1.transform.position;
            //cardSwapWith1.transform.position = tempPosition;
            //StartCoroutine(uiManager.AnimateCardMovement(cardSwapping1, cardSwapWith1.transform.position));
            //StartCoroutine(uiManager.AnimateCardMovement(cardSwapWith1, cardSwapping1.transform.position));
            uiManager.animationManager.AddParallel( new List<AnimationTask> {
                new AnimationTask { Routine = uiManager.AnimateCardMovement(cardSwapping1, cardSwapWith1.transform.position), DelayAfter = 0.1f },
                new AnimationTask { Routine = uiManager.AnimateCardMovement(cardSwapWith1, cardSwapping1.transform.position), DelayAfter = 0.1f }
            } );
            //uiManager.animationManager.Run();

            // Update GameStateClient hands
            GameStateClient.CurrentGameStateClient.Swap1CardBetweenPlayers(playerSwappingId, playerSwapWithId, cardSwappingID1, cardSwapWithID1);
        }
        else
        {
            Debug.LogError("FlipOut->SwapCards1Client(): Could not find both cards with IDs " + cardSwappingID1 + " and " + cardSwapWithID1);
        }
    }

    public void SwapCards2Client(int playerSwappingId, int playerSwapWithId, int cardId1, int cardId2, int cardSwapWithID1, int cardSwapWithID2,
         CardColor swapping1NewColor, CardColor swapping2NewColor, CardColor swapWith1NewColor, CardColor swapWith2NewColor)
    {
        PlayerXClient playerSwapping = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerSwappingId);
        PlayerXClient playerSwapWith = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerSwapWithId);

        if (playerSwapping == null || playerSwapWith == null)
        {
            Debug.LogError("FlipOut->SwapCards2Client(): Could not find one of the players for swapping: " + playerSwappingId + " or " + playerSwapWithId);
            return;
        }

        int indexSwapCard1 = playerSwapping.GetIndexOfCardByID(cardId1);
        int indexSwapCard2 = playerSwapping.GetIndexOfCardByID(cardId2);
        int indexSwapWithCard1 = playerSwapWith.GetIndexOfCardByID(cardSwapWithID1);
        int indexSwapWithCard2 = playerSwapWith.GetIndexOfCardByID(cardSwapWithID2);

        if (indexSwapCard1 == -1 || indexSwapWithCard1 == -1 || indexSwapCard2 == -1 || indexSwapWithCard2 == -1)
        {
            Debug.LogError("FlipOut->SwapCards2Client(): Could not find one of the cards for swapping.");
            return;
        }
        // Enforce consecutive order
        if (indexSwapCard1 > indexSwapCard2)
        {
            int temp = indexSwapCard1;
            indexSwapCard1 = indexSwapCard2;
            indexSwapCard2 = temp;
        }
        if (indexSwapWithCard1 > indexSwapWithCard2)
        {
            int temp = indexSwapWithCard1;
            indexSwapWithCard1 = indexSwapWithCard2;
            indexSwapWithCard2 = temp;
        }

        CardObject cardSwapping1 = playerSwapping.hand[indexSwapCard1].cardObject;
        CardObject cardSwapping2 = playerSwapping.hand[indexSwapCard2].cardObject;
        CardObject cardSwapWith2 = playerSwapWith.hand[indexSwapWithCard2].cardObject;
        CardObject cardSwapWith1 = playerSwapWith.hand[indexSwapWithCard1].cardObject;

        if (cardSwapping1 != null && cardSwapWith1 != null && cardSwapping2 != null && cardSwapWith2 != null)
        {
            // Only update color if neither player is the local player (appears as a flip+move)
            if (playerSwappingId != GameStateClient.GetCurrentPlayerId() && playerSwapWithId != GameStateClient.GetCurrentPlayerId())
            {
                // Update color of both cards (appears as a flip+move)
                cardSwapping1.UpdateColor(swapping1NewColor);
                cardSwapping2.UpdateColor(swapping2NewColor);
                cardSwapWith1.UpdateColor(swapWith1NewColor);
                cardSwapWith2.UpdateColor(swapWith2NewColor);                
            }
            // Swap positions of first pair
            //Vector3 tempPosition = cardSwapping1.transform.position;
            //cardSwapping1.transform.position = cardSwapWith1.transform.position;
            //cardSwapWith1.transform.position = tempPosition;
            //StartCoroutine(uiManager.AnimateCardMovement(cardSwapping1, cardSwapWith1.transform.position));
            //StartCoroutine(uiManager.AnimateCardMovement(cardSwapWith1, cardSwapping1.transform.position));

            // Swap positions of second pair
            //tempPosition = cardSwapping2.transform.position;
            //cardSwapping2.transform.position = cardSwapWith2.transform.position;
            //cardSwapWith2.transform.position = tempPosition;
            //StartCoroutine(uiManager.AnimateCardMovement(cardSwapping2, cardSwapWith2.transform.position));
            //StartCoroutine(uiManager.AnimateCardMovement(cardSwapWith2, cardSwapping2.transform.position));

            uiManager.animationManager.AddParallel( new List<AnimationTask> {
                new AnimationTask { Routine = uiManager.AnimateCardMovement(cardSwapping1, cardSwapWith1.transform.position), DelayAfter = 0.1f },
                new AnimationTask { Routine = uiManager.AnimateCardMovement(cardSwapWith1, cardSwapping1.transform.position), DelayAfter = 0.1f },
                new AnimationTask { Routine = uiManager.AnimateCardMovement(cardSwapping2, cardSwapWith2.transform.position), DelayAfter = 0.1f },
                new AnimationTask { Routine = uiManager.AnimateCardMovement(cardSwapWith2, cardSwapping2.transform.position), DelayAfter = 0.1f }
            } );
            //uiManager.animationManager.Run();
            // Update GameStateClient hands (note we pass ids that haven't had consecutive hand-order enforced)
            GameStateClient.CurrentGameStateClient.Swap2CardsBetweenPlayers(playerSwappingId, playerSwapWithId, cardId1, cardId2, cardSwapWithID1, cardSwapWithID2);
        }
        else
        {
            Debug.LogError("FlipOut->SwapCards2Client(): Could not find all four cards for swapping.");
        }
    }

    public void MoveCardsToScorePile(int playerId, int[] handIndices, CardColor cardColor)
    {
        PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerId);
        if (player == null)
        {
            Debug.LogError("FlipOut->MoveCardsToScorePile(): Could not find player number for playerId " + playerId);
            return;
        }
        if (handIndices.Length == 0 || handIndices[0] == -1)
        {
            Debug.LogWarning("FlipOut->MoveCardsToScorePile(): handIndices is empty for playerId " + playerId);
            return;
        }

        int playerNum = player.playerNumber;
        Vector3 scorePilePosition = playerScorePilePositions[playerNum];

        List<AnimationTask> animationTasks = new List<AnimationTask>();
        for (int i = 0; i < handIndices.Length; i++)
        {
            int handIndex = handIndices[i];
            int cardID = player.hand[handIndex].cardID;

            CardPODClient cardPOD = player.hand[handIndex];
            
            if (cardPOD != null)
            {
                CardObject cardObject = cardPOD.cardObject;
                player.hand[handIndex] = new CardPODClient(); // Clear from player's hand

                // Set card state to scorePile
                cardObject.cardPOD.state = CardState.scorePile;
                player.scorePile.Add(cardPOD); // Add to player's score pile

                if (playerNum != GameStateClient.GetActivePlayerNumber())
                {
                    // for opponents, we need to 'flip' the card to the correct color
                    //cardObject.UpdateColor(cardColor);
                    uiManager.animationManager.AddSequential( 
                        new AnimationTask { Routine = uiManager.AnimateFlip(cardObject, cardColor), DelayAfter = 0.03f } 
                        );
                }

                // Move card to score pile position
                Vector3 targetPosition = scorePilePosition;
                
                //cardObject.SetLocalPosition(targetPosition);
                //cardObject.SetLocalScale(Vector3.one * 0.5f); // Slightly smaller
                cardObject.SetSortingOrder((player.scorePile.Count + 1) * 2); // On top of score pile
                //StartCoroutine(uiManager.AnimateCardMovementAndScale(cardObject, targetPosition, Vector3.one * 0.5f));
                animationTasks.Add( new AnimationTask { Routine = uiManager.AnimateCardMovementAndScale(cardObject, targetPosition, Vector3.one * 0.5f), DelayAfter = 1f } );
                //uiManager.animationManager.AddSequential( 
                //    new AnimationTask { Routine = uiManager.AnimateCardMovementAndScale(cardObject, targetPosition, Vector3.one * 0.5f), DelayAfter = 0f } 
                //    );                
            }
            else
            {
                Debug.LogError("FlipOut->MoveCardsToScorePile(): No card found with cardID " + cardID);
            }
        }
        uiManager.animationManager.AddParallel(animationTasks);
        var prevEvent = currentGameEvent;;
        currentGameEvent = FlipOutGameEvents.UpdatingScores;
        //uiManager.animationManager.Run();
        uiManager.UpdateScoresDisplay();
        currentGameEvent = prevEvent;
        //this is called along with Score/Swipe to create/queue deal action:
        // GameManager.Instance.serverDispatch.DealCardsToPlayerHandIndices(playerId, handIndices);
    }

    public void SwipeCardsToScorePiles(int playerId, int targetPlayerId, int[] handIndices, CardColor cardColor)
    {
        // Similar to MoveCardsToScorePile but moving all but 1 to player's pile, and final to target player's pile
        PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerId);
        PlayerXClient targetPlayer = GameStateClient.CurrentGameStateClient.GetPlayerByID(targetPlayerId);
        if (player == null || targetPlayer == null)
        {
            Debug.LogError("FlipOut->SwipeCardsToScorePiles(): Could not find player for one of the playerIds " + playerId + " or " + targetPlayerId);
            return;
        }
        if (handIndices.Length == 0 || handIndices[0] == -1)
        {
            Debug.LogWarning("FlipOut->SwipeCardsToScorePiles(): handIndices is empty for targetPlayerId " + targetPlayerId);
            return;
        }

        int playerNum = player.playerNumber;
        int playerTargetNum = targetPlayer.playerNumber;
        Vector3 scorePilePosition = playerScorePilePositions[playerNum];
        Vector3 targetScorePilePosition = playerScorePilePositions[playerTargetNum];
        List<AnimationTask> animationTasks = new List<AnimationTask>();

        // Final card goes to target player's score pile
        for (int i = 0; i < handIndices.Length - 1; i++)
        {
            int handIndex = handIndices[i];
            int cardID = targetPlayer.hand[handIndex].cardID;

            CardPODClient cardPOD = targetPlayer.hand[handIndex];
            
            if (cardPOD != null)
            {
                CardObject cardObject = cardPOD.cardObject;
                targetPlayer.hand[handIndex] = new CardPODClient(); // Clear from player's hand

                // update id, state
                cardPOD.ownerPlayerID = playerId;
                cardPOD.state = CardState.scorePile;
                player.scorePile.Add(cardPOD); // Add to player's score pile

                if (playerTargetNum == GameStateClient.GetActivePlayerNumber())
                {
                    // for owner, we need to 'flip' the card (to show opposite side) to the correct color
                    //cardObject.UpdateColor(cardColor);
                    uiManager.animationManager.AddSequential( 
                        new AnimationTask { Routine = uiManager.AnimateFlip(cardObject, cardColor), DelayAfter = 0.03f } 
                        );
                }

                // Move card to score pile position
                Vector3 targetPosition = scorePilePosition;

                //cardObject.SetLocalPosition(targetPosition);
                //cardObject.SetLocalScale(Vector3.one * 0.5f); // Slightly smaller
                cardObject.SetSortingOrder((player.scorePile.Count + 1) * 2); // On top of score pile
                animationTasks.Add( new AnimationTask { Routine = uiManager.AnimateCardMovementAndScale(cardObject, targetPosition, Vector3.one * 0.5f), DelayAfter = 0.10f } );
                //StartCoroutine(uiManager.AnimateCardMovementAndScale(cardObject, targetPosition, Vector3.one * 0.5f));
            }
            else
            {
                Debug.LogError("FlipOut->SwipeCardsToScorePiles(): No card found with cardID " + cardID);
            }
        }
        
        
        // Final card goes into target player's score pile
        int finalHandIndex = handIndices[handIndices.Length - 1];
        int finalCardID = targetPlayer.hand[finalHandIndex].cardID;
        CardPODClient finalCardPOD = targetPlayer.hand[finalHandIndex];
        if (finalCardPOD != null)
        {
            CardObject finalCardObject = finalCardPOD.cardObject;
            targetPlayer.hand[finalHandIndex] = new CardPODClient(); // Clear from player's hand

            // update id, state
            //finalCardPOD.ownerPlayerID = targetPlayerId;  // stays the same
            finalCardPOD.state = CardState.scorePile;
            targetPlayer.scorePile.Add(finalCardPOD); // Add to target player's score pile

            if (playerTargetNum == GameStateClient.GetActivePlayerNumber())
            {
                // for owner, we need to 'flip' the card (to show opposite side) to the correct color
                //finalCardObject.UpdateColor(cardColor);
                uiManager.animationManager.AddSequential( 
                    new AnimationTask { Routine = uiManager.AnimateFlip(finalCardObject, cardColor), DelayAfter = 0.03f } 
                    );
            }

            // Move card to score pile position
            Vector3 targetPosition = targetScorePilePosition;
            
            //finalCardObject.SetLocalPosition(targetPosition);
            //finalCardObject.SetLocalScale(Vector3.one * 0.5f); // Slightly smaller
            finalCardObject.SetSortingOrder((targetPlayer.scorePile.Count + 1) * 2); // On top of score pile
            //uiManager.animationManager.AddSequential( 
            //    new AnimationTask { Routine = uiManager.AnimateCardMovementAndScale(finalCardObject, targetPosition, Vector3.one * 0.5f), DelayAfter = 1f } 
            //    );
            animationTasks.Add( new AnimationTask { Routine = uiManager.AnimateCardMovementAndScale(finalCardObject, targetPosition, Vector3.one * 0.5f), DelayAfter = 0.10f } );
            //animationTasks.Add( new AnimationTask { Routine = uiManager.AnimateCardMovementAndScale(finalCardObject, targetPosition, Vector3.one * 0.5f), DelayAfter = 1f } );
            //StartCoroutine(uiManager.AnimateCardMovementAndScale(finalCardObject, targetPosition, Vector3.one * 0.5f));
            uiManager.animationManager.AddParallel(animationTasks);
        }
        else
        {
            Debug.LogError("FlipOut->SwipeCardsToScorePiles(): No card found with cardID " + finalCardID);
        }
        var prevEvent = currentGameEvent;;
        currentGameEvent = FlipOutGameEvents.UpdatingScores;
        //uiManager.animationManager.Run();
        uiManager.UpdateScoresDisplay();
        currentGameEvent = prevEvent;
        //this is called along with Score/Swipe to create/queue deal action:
        // GameManager.Instance.serverDispatch.DealCardsToPlayerHandIndices(playerId, handIndices);
    }

#endregion

#region Playback

    //!TODO: Refactor some of this as it was pulled from FlipOutActions

    private void ActOnFlipOutActionForCurrentPlayer(FlipOutActions action)
    {
        Debug.Log("ActOnFlipOutActionForCurrentPlayer: Acting on action " + action.actionTaken.ToString() + " for current player (#" + GameStateClient.GetCurrentPlayerNumber() + ").");

            switch (action.actionTaken)
            {
                case FlipOutAction.Flip:
                    // Flip action is defined opposite(src)->(dest)facing-player side,
                    // Only the owning player will switch this around to facing->opposite (which means using Source color as destination color)
                    CardColor destColor = (GameStateClient.GetCurrentPlayerId() == action.playerTargetId) ? 
                        action.cardSourceInfos[0].cardColor : action.cardDestInfos[0].cardColor;
                    FlipCardClient(action.cardDestInfos[0].cardID, destColor);
                    ClearHighlightedCards();
                    break;
                case FlipOutAction.Switch:
                    SwitchCardsClient(action.cardSourceInfos[0].cardID, action.cardDestInfos[0].cardID);
                    ClearHighlightedCards();
                    break;
                case FlipOutAction.Swap1:
                    // Colors only necessary for non-participating players which will see flip+move effect
                    Debug.Log("ActOnFlipOutActionForCurrentPlayer: Processing Swap1 action between player " + action.playerTakingActionId +
                              " and player " + action.playerTargetId +
                              " swapping card " + action.cardSourceInfos[0].cardID +
                              " with card " + action.cardDestInfos[0].cardID);
                    SwapCards1Client(
                        action.playerTakingActionId,
                        action.playerTargetId,
                        action.cardSourceInfos[0].cardID,
                        action.cardDestInfos[0].cardID,
                        action.cardSourceInfos[0].cardColor,
                        action.cardDestInfos[0].cardColor);
                    ClearHighlightedCards();
                    break;
                case FlipOutAction.Swap2:
                    // Colors only necessary for non-participating players which will see flip+move effect
                    SwapCards2Client(
                        action.playerTakingActionId,
                        action.playerTargetId,
                        action.cardSourceInfos[0].cardID,
                        action.cardSourceInfos[1].cardID,
                        action.cardDestInfos[0].cardID,
                        action.cardDestInfos[1].cardID,
                        action.cardSourceInfos[0].cardColor,
                        action.cardSourceInfos[1].cardColor,
                        action.cardDestInfos[0].cardColor,
                        action.cardDestInfos[1].cardColor);
                    ClearHighlightedCards();
                    break;
                case FlipOutAction.Score:
                    MoveCardsToScorePile(action.playerTakingActionId, action.positions, action.cardSourceInfos[0].cardColor);
                    break;
                case FlipOutAction.Swipe:
                    SwipeCardsToScorePiles(
                        action.playerTakingActionId,
                        action.playerTargetId,
                        action.positions,
                        action.cardSourceInfos[0].cardColor);
                    break;
                case FlipOutAction.Deal:
                    //! Note: Dealing actually flips for the owner player, but we're showing the deck top
                    // as the opposite color (facing away from the player) so it looks correct
                    List<CardPODClient> dealtCards = new List<CardPODClient>();
                    for (int j = 0; j < action.cardSourceInfos.Length; j++)
                    {
                        CardPODClient cardPOD = new CardPODClient
                        {
                            cardID = action.cardSourceInfos[j].cardID,
                            color = action.cardSourceInfos[j].cardColor,
                            state = CardState.playerHolder,
                            ownerPlayerID = action.playerTargetId
                        };
                        dealtCards.Add(cardPOD);
                    }
                    GameStateClient.CurrentGameStateClient.AssignCardsToPlayerHand(
                        action.playerTargetId,
                        dealtCards,
                        action.positions);
                    
                    //GameManager.Instance.DealFullHandClientFromState(action.playerTargetId);
                    DealNewCardsToClient(action.playerTargetId, dealtCards, action.positions, action.cardDestInfos[0].cardColor);
                    break;
                case FlipOutAction.TurnEnd:
                    // act on if current player
                    if (GameStateClient.GetCurrentPlayerId() == action.playerTakingActionId)
                    {
                        Debug.Log("ActOnFlipOutActionsForCurrentPlayer: Ending current player's turn as per action.");
                        GameStateClient.CurrentGameStateClient.ClearCurrentPlayerActionsTaken();
                        GameManager.Instance.uiManager.animationManager.SetActionCompleteCallback(() => GameManager.Instance.EndTurnClient(), true);
                    }
                    break;
                case FlipOutAction.EndGame:
                    // ignore - game is already ended by previous player (i think)
                    //! (or should I trigger end-game notification here?)
                    GameManager.Instance.EndGameClient(action.playerTakingActionId);
                    break;
                default:
                    Debug.LogWarning("ActOnFlipOutActionsForCurrentPlayer: Unknown action encountered.");
                    break;
            }
        GameManager.Instance.uiManager.animationManager.Run();
    }

    public void ActOnFlipOutActionsForCurrentPlayer()
    {
        currentGameEvent = FlipOutGameEvents.ProcessingActions;
        StartCoroutine(ActOnFlipOutActionsForCurrentPlayerCoroutine());
    }

    private IEnumerator ActOnFlipOutActionsForCurrentPlayerCoroutine()
    {
        List<FlipOutActions> listofActions = GameStateClient.CurrentGameStateClient.GetListOfActionsSinceLastTurn();
        Debug.Log("ActOnFlipOutActionsForCurrentPlayerCoroutine: Acting on " + listofActions.Count + " actions for current player.");

        for (int i = 0; i < listofActions.Count; i++)
        {
            Debug.Log("ActOnFlipOutActionsForCurrentPlayerCoroutine: Action " + i + " is " + listofActions[i].actionTaken.ToString());

            currentGameEvent = FlipOutGameEvents.ProcessingActions;
            ActOnFlipOutActionForCurrentPlayer(listofActions[i]);

            // Wait until all animations complete before proceeding to next action
            while (GameManager.Instance.uiManager.animationManager.IsRunningAnimations())
            {
                yield return null;
            }
            currentGameEvent = FlipOutGameEvents.ActionCompleted;
            //GameManager.Instance.uiManager.UpdateScoresDisplay();
        }

        //!TODO: Move this to ServerDispatch -> after adding action, check # actions, add EndTurn action at 2
        GameStateClient.CurrentGameStateClient.ClearActionsSinceLastTurn();
        if (GameStateClient.CurrentGameStateClient.GetCurrentPlayerActionsTaken() >= 2)
        {
            Debug.Log("ActOnFlipOutActionsForCurrentPlayerCoroutine: Current player has taken 2 actions, ending turn.");
            currentGameEvent = FlipOutGameEvents.EndingTurn;
            GameManager.Instance.serverDispatch.EndTurn();
        }
        else
            currentGameEvent = FlipOutGameEvents.Idle;
    }

#endregion

#region Actions-Available
    public static TurnAction GetAvailableActionsForCard(CardPODClient cardPOD)
    {
        //List<TurnAction> actions = new List<TurnAction>();
        TurnAction availableActions = TurnAction.None;

        PlayerXClient currentPlayer = GameStateClient.CurrentGameStateClient.GetActivePlayer();
        PlayerXClient ownerPlayer = GameStateClient.CurrentGameStateClient.GetPlayerByID(cardPOD.ownerPlayerID);
        if (ownerPlayer == null)
        {
            Debug.LogError("AvailableActionsForCard: could not find owner player for cardID " + cardPOD.cardID);
            return availableActions;
        }

        var allPlayers = GameStateClient.CurrentGameStateClient.GetActivePlayers();

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
        if (ownerPlayer == currentPlayer)
        {
            if (IsThere4To6AdjacentCardsOfSameColorAsThis(ownerPlayer, cardPOD))
            {
                //actions.Add(TurnAction.Score);
                availableActions |= TurnAction.Score;
            }
        }
        else
        {
            if (IsThere4To6AdjacentCardsOfSameColorAsThis(ownerPlayer, cardPOD))
            {
                //actions.Add(TurnAction.Swipe);
                availableActions |= TurnAction.Swipe;
            }
        }

        return availableActions;
    }


    public bool IsSwap2Available()
    {
        var allPlayers = GameStateClient.CurrentGameStateClient.GetActivePlayers();
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
            var allPlayers = GameStateClient.CurrentGameStateClient.GetActivePlayers();
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
        var allPlayers = GameStateClient.CurrentGameStateClient.GetActivePlayers();
        foreach (var player in allPlayers)
        {
            if (IsThereAny4To6AdjacentCardsOfSameColor(player))
                return true;
        }
        return false;
    }

    public bool IsSwipeAvailableForPlayer(PlayerXClient player)
    {
        var allPlayers = GameStateClient.CurrentGameStateClient.GetActivePlayers();
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
            var allPlayers = GameStateClient.CurrentGameStateClient.GetActivePlayers();
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
         PlayerXClient ownerPlayer = GameStateClient.CurrentGameStateClient.GetPlayerByCardId(cardId);
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

#region Actions-Available-Server
  
    public static bool IsThereAny2AdjacentCardsOfSameColor(PlayerXServer player)
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

    public static bool IsThereAny4To6AdjacentCardsOfSameColor(PlayerXServer player)
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

    public static bool IsSwipeAvailableForPlayer(PlayerXServer player)
    {
        var allPlayers = GameManager.Instance.gameStateServer.GetActivePlayers();
        foreach (var otherPlayer in allPlayers)
        {
            if (otherPlayer != player && IsThereAny4To6AdjacentCardsOfSameColor(otherPlayer))
            {
                return true;
            }
        }
        return false;
    }

    public static TurnAction GetAvailableActionsForPlayer(PlayerXServer ownerPlayer)
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
            var allPlayers = GameManager.Instance.gameStateServer.GetActivePlayers();
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

    public static int GetTotalAdjacentColorCount(PlayerXServer player)
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
