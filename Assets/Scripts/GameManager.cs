using System;
using System.Collections.Generic;
using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public enum Scenes
{
    LoadingScreen,
    MainMenu,
    Game,
    GameOver,
    DCExperiments
}

// AppState ? (avoids collision with GameState script)
[Serializable]
public enum GameStatus
{
    Loading,
    Playing,
    Paused,
    UI,
    GameOver,
    Win,
    Lose
 };

 [Serializable]
 public enum MultiplayerMode
 {
    Disconnected,
    LocalHotseat,
    Online
 };

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public InputManager inputManager;
    //AudioClip clickSound;

    [SerializeField] private PlayerSessionManager sessionManager = new PlayerSessionManager();
    [SerializeField] public GameStateServer gameStateServer = new GameStateServer();

    // Only for Editor inspection:
    [SerializeField] public GameStateClient gameStateClient;
    public GameStateClient gameStateClient2;

    public ServerDispatch serverDispatch = new ServerDispatch();

    public GameStatus currentGameState = GameStatus.Loading;

    public static ScenesSO scenesSO;

    public Scenes currentScene = Scenes.LoadingScreen;

    public MultiplayerMode currentMultiplayerMode = MultiplayerMode.Disconnected;    

    //[SerializeField] private PlayerX[] players = new PlayerX[5];

    GameObject playersParentGO = null;
    //private int localPlayer1Index = 0;
    //private int currentPlayerIndex = 0;
    //private int totalPlayers = 1;

    [SerializeField] private Vector3[] playerPositions = new Vector3[5]
    {
    
        new(-6, -3, 0),    // Player 1 - Bottom center
        new(-6, 3, 0),     // Player 2 - Top center
        new(-7, 0, 0),    // Player 3 - Left center        
        new(7, 0, 0),     // Player 4 - Right center
        new(0, 0, 0)      // Player 5 - Center (?!!)
    };
    [SerializeField] private Vector3 cardHolderOffset = new Vector3(2.5f, 0, 0);

// CARDS
    //private CardManager cardManager;

    //private List<CardObject> drawPile = null;
    bool cardsShowing = false;

    Vector3 drawPileDefaultPosition = new Vector3(0, 0, 0);   //(-6, -3, 0);
    private Vector3 deckOffscreenPosition = new Vector3(-1000, -1000, 0);

    GameObject cardPrefab;
    GameObject cardsParentGO;

    [SerializeField] private CardObject drawPileTop;

    List<CardObject> cardsInPlay;

    // Debugging purposes
    //Vector3 moveToPosition = new Vector3(1, 1, 0);
    //int cardsMoved = 0;

    public bool forceHotseat = true;

    // Awake - Called before first Scene, not destroyed or recreated on Scene load
    void Awake()
    {
        Debug.Log("GameManager->Awake()");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);        
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start - Called before the first frame of first Scene, not destroyed/recreated on Scene load
    void Start()
    {
        Debug.Log("GameManager->Start()");

        AudioClip clip = Resources.Load<AudioClip>("Audio/OVS_CorporateVol2BeyontheBlueprintCut30");
        if (clip == null)
        {
            Debug.LogError("Failed to load audio clip from Resources folder.");
            return;
        }
        AudioManager.Play(clip, 0.25f);
        /*clickSound = Resources.Load<AudioClip>("Audio/OVS_Clicky");
        if (clickSound == null)
        {
            Debug.LogError("Failed to load click sound clip from Resources folder.");
            return;
        }*/
        
        //AudioManager.Loop();
        Debug.Log("Playing music: " + clip.name);
#if UNITY_EDITOR
        // Keep current editor level if in Editor
        //LevelCurrentInternalInit();
#else
        //LoadLevel(GameManager.Level.MainMenu);
#endif
    }

    public void LoadScene(Scenes scene)
    {
        Debug.Log("GameManager->LoadScene(): " + scene.ToString());
        if (currentGameState == GameStatus.Playing)
        {
            EndGameCleanup();
        }
        switch (scene)
        {
            case Scenes.MainMenu:
                SceneManager.LoadScene(scenesSO.mainMenuScene);
                //currentScene = Scenes.MainMenu;
                currentScene = scenesSO.mainMenuSceneEnum;
                currentGameState = GameStatus.UI;
                break;
            case Scenes.Game:
                SceneManager.LoadScene(scenesSO.gameScene);
                //currentScene = Scenes.Game;
                currentScene = scenesSO.gameSceneEnum;
                currentGameState = GameStatus.Playing;
                break;
            case Scenes.GameOver:
                SceneManager.LoadScene(scenesSO.gameOverScene);
                //currentScene = Scenes.GameOver;
                currentScene = scenesSO.gameOverSceneEnum;                
                currentGameState = GameStatus.GameOver;
                break;
            case Scenes.DCExperiments:
                SceneManager.LoadScene(scenesSO.DCExperimentsScene);
                //currentScene = Scenes.DCExperiments;
                currentScene = scenesSO.DCExperimentsSceneEnum;
                currentGameState = GameStatus.Playing;
                break;
            default:
                Debug.LogError("Unknown scene: " + scene);
                break;
        }
    }

    public void VerifyCurrentScene()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == scenesSO.mainMenuScene)
        {
            if (currentScene != Scenes.MainMenu)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.MainMenu.ToString());
                currentScene = Scenes.MainMenu;
                currentGameState = GameStatus.UI;
            }
        }
        else if (activeSceneName == scenesSO.gameScene)
        {
            if (currentScene != Scenes.Game)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.Game.ToString());
                currentScene = Scenes.Game;
                currentGameState = GameStatus.Playing;
            }
        }
        else if (activeSceneName == scenesSO.gameOverScene)
        {
            if (currentScene != Scenes.GameOver)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.GameOver.ToString());
                currentScene = Scenes.GameOver;
                currentGameState = GameStatus.GameOver;
            }
        }
        else if (activeSceneName == scenesSO.DCExperimentsScene)
        {
            if (currentScene != Scenes.DCExperiments)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.DCExperiments.ToString());
                currentScene = Scenes.Game; // !!
                currentGameState = GameStatus.Playing;
            }
        }
        else
        {
            Debug.LogWarning("Active scene does not match any known scenes in ScenesSO: " + activeSceneName);
        }
    }

    //public void LoadScene(string sceneName)
    //{
    //    SceneManager.LoadScene(sceneName);
    //}

    void OnMultiplayerConnect()
    {
        Debug.Log("GameManager->OnServerConnected()");
        currentMultiplayerMode = MultiplayerMode.Online;
    
    }

    void OnMultiplayerDisconnect()
    {
        Debug.Log("GameManager->OnServerDisconnected()");
        currentMultiplayerMode = MultiplayerMode.Disconnected;
    }

    // Scene -> Scene script (in each level) calls the following Awake/Start/Destroyed functions

    public void SceneAwake()
    {
        VerifyCurrentScene();
        Debug.Log("GameManager->SceneAwake() for scene: " + SceneManager.GetActiveScene().name + " currentScene: " + currentScene.ToString());
        if (currentScene == Scenes.Game)
        {
            //Debug.Log("Current Multiplayer mode: " + currentMultiplayerMode.ToString());
            CardObject.onCardClicked += OnCardClicked;

            if (forceHotseat && currentMultiplayerMode == MultiplayerMode.Disconnected)
            {
                //StartHotseatGame(2, new string[] { Environment.UserName, "Player2" });
                StartHotseatGame(2, new string[] { "PlayerUNO", "Player2" });
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SceneStart()
    {
        Debug.Log("GameManager->SceneStart() for scene: " + SceneManager.GetActiveScene().name);
        if (currentScene == Scenes.Game)
        {
            // Initialize game components
        }
    }

    public void SceneDestroyed()
    {
        Debug.Log("GameManager->SceneDestroyed() for scene: " + SceneManager.GetActiveScene().name);
        if (currentScene == Scenes.Game)
        {
            // Unsubscribe from static Click event
            CardObject.onCardClicked -= OnCardClicked;
            EndGameCleanup();
            // 'Unloading'?
            //currentGameState = GameStatus.Loading;
        }
    }

   // Called by NetworkManager when online game is ready to start (?)
    void StartOnlineGame(int[] playerIds, string[] playerNames)
    {
        if (currentScene != Scenes.Game)
        {
            Debug.LogError("GameManager->StartOnlineGame(): Not in Game scene!");
            return;
        }
        if (playerIds.Length != playerNames.Length)
        {
            Debug.LogError("GameManager->StartOnlineGame(): playerIds length does not match length of playerNames array!");
        }
        Debug.Log("GameManager->StartOnlineGame()");
        //totalPlayers = numPlayers;
        //currentPlayerIndex = NetworkManager.Instance.GetLocalPlayerIndex();

        // if (IsHost)
        gameStateServer.InitGameStateServer(playerIds, playerNames);
        currentMultiplayerMode = MultiplayerMode.Online;
        currentGameState = GameStatus.Playing;
    }

    // Called by UI to start hotseat game (input number of players and player names)
    void StartHotseatGame(int numPlayers, string[] playerNames)
    {
        if (currentScene != Scenes.Game)
        {
            Debug.LogError("GameManager->StartHotseatGame(): Not in Game scene!");
            return;
        }
        if (numPlayers != playerNames.Length)
        {
            Debug.LogError("GameManager->StartHotseatGame(): numPlayers does not match length of playerNames array!");
        }
        Debug.Log("GameManager->StartHotseatGame()");
        Debug.Log("First name: " + playerNames[0]);


        // Player Ids are separate from player numbers but for hotseat they are basically the same
        int[] playerIds = new int[numPlayers];
        
        for (int i = 0; i < numPlayers; i++)
        {
            playerIds[i] = i;
            sessionManager.AddSession(i, playerNames[i], "LocalHost");
        }

        // if (IsHost)
        //gameStateServer.InitGameStateServer(playerIds,playerNames);
        GameStateClient.InitGameStateClient(playerIds, playerNames);

        gameStateClient = GameStateClient.GetHotseatGameStateForPlayerNumber(0);
        gameStateClient2 = GameStateClient.GetHotseatGameStateForPlayerNumber(1);

        //totalPlayers = numPlayers;
        //currentPlayerIndex = localPlayer1Index;
        currentMultiplayerMode = MultiplayerMode.LocalHotseat;
        currentGameState = GameStatus.Playing;

        cardsInPlay = new List<CardObject>();

        playersParentGO = new GameObject("_Players");
        for (int i = 0; i < numPlayers; i++)
        {
            GameObject playerGO = new GameObject("Player" + i); //, typeof(PlayerXClient));
            playerGO.transform.SetParent(playersParentGO.transform);
        }

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

    public void EndGameClient()
    {
        Debug.Log("GameManager->EndGameClient()");
        EndGameCleanup();
        //LoadScene(Scenes.GameOver);
    }

    public void EndTurnClient()
    {
        Debug.Log("GameManager->EndTurnClient()");
        
        if (currentMultiplayerMode == MultiplayerMode.LocalHotseat)
        {
            // Clear board
            // (draw pile top card?)
            // Clear cards in play
            ClearObjectsInPlay();
        }
    }

    void EndGameCleanup()
    {
        Debug.Log("GameManager->EndGameCleanup()");
        playersParentGO = null;
        cardsShowing = false;
        drawPileTop = null;
    
        if (cardsInPlay != null)
        {
            cardsInPlay.Clear();
            cardsInPlay = null;
        }
        gameStateServer.Cleanup();
        GameStateClient.CleanupClients();
        currentMultiplayerMode = MultiplayerMode.Disconnected;
    }

    void SetDrawPileTopCard(CardColor color)
    {
        if (color == CardColor.invalid)
        {
            Debug.LogWarning("GameManager->SetDrawPileTopCard(): color is invalid, removing drawPileTop card.");
            if (drawPileTop != null)
            {
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
        }
        else
        {
            drawPileTop.SetCardPOD(topPOD);
            drawPileTop.cardPOD.state = CardState.drawPile;
        }
        return;
    }

    //[Rpc(SendTo.Server)]
    //[Rpc(SendTo.NotServer)]
    //[Rpc(SendTo.ClientsAndHost)]

    //[ClientRpc]

    public void ShowOpponentFullHandClient(int playerNum, CardPODClient[] hand)
    {
        // Show opponent's full hand to local player
        DealFullHandClient(playerNum, hand, true);
    }

    public void DealAllHandsClientFromState()
    {
        for (int playerNum = 0; playerNum < GameStateClient.GetTotalPlayers(); playerNum++)
        {
            var hand = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(playerNum).hand;
            //! Can't create cardObjects for other players, just cardPODs
            CardObject[] cardObjects = new CardObject[6];

            int ownerPlayerID = GameStateClient.CurrentGameStateClient.GetPlayerIDByNumber(playerNum);;

            for (int i = 0; i < hand.Length; i++)
            {
                cardObjects[i] = CardObjectFromPODClient(ownerPlayerID, hand[i].Clone());
                // Set card position to player position
                cardObjects[i].SetLocalPosition(playerPositions[playerNum] + cardHolderOffset * i);
                // Slight offset for visibility
                cardObjects[i].SetSortingOrder(50);
                // Set card state to playerHolder
                cardObjects[i].cardPOD.state = CardState.playerHolder;
            }
        }
        SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
    }

    // Client-side
    public void DealFullHandClient(int playerNum, CardPODClient[] hand, bool bOpponent = false)
    {
        if (hand.Length != 6)
        {
            Debug.LogError("GameManager->SetLocalPlayerHand(): hand length is not 6!");
            return;
        }
        // Ignoring hand that is not the active player (unless bOpponent is true, which means show opponent's deck)
        if (playerNum != GameStateClient.GetActivePlayerNumber())
        {
            if (!bOpponent)
            {
                Debug.Log("GameManager->SetLocalPlayerHand(): ownerPlayerID does not match local active player ID! & bOpponent is false, so ignoring.");
                return;
            }
        }
        // ownerPlayerId DOES match active player, so we will NOT show what opponent sees
        else if (bOpponent)
        {
            Debug.Log("GameManager->SetLocalPlayerHand(): Cannot show opponent deck for local active player!");
            return;
        }

        //! Can't create cardObjects for other players, just cardPODs
        CardObject[] cardObjects = new CardObject[6];

        int ownerPlayerID = GameStateClient.CurrentGameStateClient.GetPlayerIDByNumber(playerNum);;

        for (int i = 0; i < hand.Length; i++)
        {
            cardObjects[i] = CardObjectFromPODClient(ownerPlayerID, hand[i].Clone());
            // Set card position to player position
            cardObjects[i].SetLocalPosition(playerPositions[playerNum] + cardHolderOffset * i);
            // Slight offset for visibility
            cardObjects[i].SetSortingOrder(50);
            // Set card state to playerHolder
            cardObjects[i].cardPOD.state = CardState.playerHolder;

            //gameStateClient.playersClient[playerNum].hand[i] = cardObjects[i].cardPOD;
        }
        SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
        // Animate from deck to player/position (?)
        //GameStateClient.CurrentGameStateClient.SetCardsForPlayer(playerNum, hand);
    }

    public void StartPlayerTurnClient(int playerNum, int playerId, TurnAction availableActions)
    {
        Debug.Log("GameManager->StartPlayerTurnClient(): Player " + playerId + "'s turn started.");
        // This should be done at TurnEnd:
        //ClearObjectsInPlay();

        if (currentMultiplayerMode == MultiplayerMode.LocalHotseat)
        {
            DealAllHandsClientFromState();
        }
        //StartTurnClient(playerId, availableActions);
    }

    public void ClearObjectsInPlay()
    {
        if (cardsInPlay != null)
        {
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

    public void EndPlayerTurnClient()
    {
        Debug.Log("GameManager->EndPlayerTurnClient()");
        ClearObjectsInPlay();

    }


#region Client-Side
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
        
        CardObject cardObject = cardGO.GetComponent<CardObject>();

        // Attach Card POD to CardObject
        cardPOD.state = newState;
        cardPOD.ownerPlayerID = playerID;
        cardObject.SetCardPOD(cardPOD);

        return cardObject;
    }
#endregion

    public CardObject CardObjectFromPODClient(int playerID, CardPODClient cardPOD)
    {
        return InstantiateCardObjectFromPOD(cardPOD, deckOffscreenPosition, CardState.playerHolder, playerID);
    }


    public static void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit(); // For standalone builds
#endif
        Debug.Log("Player Has Quit the Game");
    }

    // Update is called once per frame
    void Update()
    {
        // Quick test: press L to load the configured scene
        if (Input.GetKeyDown(KeyCode.L))
        {
            //LoadScene("xDCExperiments");
            LoadScene(Scenes.DCExperiments);
        }
        // workaround for Start() timing issue (avoiding Script Execution Order change)
        if (currentScene == Scenes.Game && gameStateServer.serverDrawPile != null && !cardsShowing)
        {
            //UpdateDrawPile();
            //DrawPileDisplayTopCard();
            cardsShowing = true;
        }
    }


    // Called when a card is clicked - responds based on player turn, action, etc.
    void OnCardClicked(CardObject card)
    {
        AudioManager.PlaySoundAt(AudioManager.audioSourcesSO.clickCard, 1f);
        Debug.Log("GameManager->OnCardClicked - Card clicked: " + card.gameObject.name + " currentPlayerIndex: " + gameStateServer.GetActivePlayerNumber());

        if (card.cardPOD.state == CardState.playerHolder)
        {
            Debug.Log("Actions available: " + string.Join(", ", GameStateClient.CurrentGameStateClient.GetAvailableActionsForCard(card.cardPOD)));
        }
        else
        {
            Debug.Log("Max run player 0: " + GameStateClient.GetTotalAdjacentColorCount(GameStateClient.CurrentGameStateClient.GetPlayerByNumber(0)));
            Debug.Log("Max run player 1: " + GameStateClient.GetTotalAdjacentColorCount(GameStateClient.CurrentGameStateClient.GetPlayerByNumber(1)));
        }
    }

#region Client-Server
    /*void TurnEnd()
    {
        if (currentMultiplayerMode == MultiplayerMode.LocalHotseat)
        {
            currentPlayerIndex++;
            if (currentPlayerIndex >= totalPlayers)
                currentPlayerIndex = 0;
            Debug.Log("GameManager->TurnEnd(): Current player is now Player " + currentPlayerIndex);
            inputManager.activePlayer = players[currentPlayerIndex];
            return;
        }
        //else - online mode
    }*/


    // Client-side
    public void RequestFlipCard(int playerId, int cardId)
    {
        SendServerRpc_FlipCard(playerId, cardId);
    }
    // Send message to server to flip card
    public void SendServerRpc_FlipCard(int playerId, int cardId)
    {
        // This calls the actual ServerRpc defined below
        FlipCardServerRpc(playerId, cardId);
    }
    // Server-side
    //[ServerRpc]
    public void FlipCardServerRpc(int playerId, int cardId)
    {
        Debug.Log("GameManager->FlipCardServerRpc(): Player " + playerId + " requested flip of cardID " + cardId);
        // Validate action
        // CanPlayerFlipCard() // no need yet - debugging only
        //gameStateServer.FlipCard(playerId, cardId);
        if (true)
        {
            //BroadcastFlipCardClientRpc(playerId, cardId, gameStateServer.GetCardPODByID(cardId).facingOwner);
        }
        else
        {
            Debug.LogWarning("GameManager->FlipCardServerRpc(): Player " + playerId + " not allowed to flip cardID " + cardId);
            //SendActionRejectedClientRpc(playerId, TurnAction.FlipCard, cardId);
            return;
        }
    }

#endregion
    public void FlipCard(int cardID)
    {
        // Find the CardObject with the given cardID
        CardObject cardToFlip = null;
        foreach (Transform cardTransform in cardsParentGO.transform)
        {
            CardObject cardObject = cardTransform.GetComponent<CardObject>();
            if (cardObject != null && cardObject.cardPOD.cardID == cardID)
            {
                cardToFlip = cardObject;
                break;
            }
        }

        if (cardToFlip != null)
        {
            //cardToFlip.FlipCard();
        }
        else
        {
            Debug.LogError("GameManager->FlipCard(): No card found with cardID " + cardID);
        }
    }

}

    /*
    // !REMNANT CODE FROM GAMEMANAGER - Useful for debugging/viewing entire deck in staggered pile
    // Updates the deck DrawPile - uses basic algorithm that Prospector Solitaire used
    //  Layering a deck of cards with sorting layer/order and Z-order 
	void UpdateDrawPile()
    {
        const float STAGGER_X = 0.05f;
        CardObject card;

        Debug.Log("GameManager->UpdateDrawPile() - Updating draw pile with " + drawPile.Count + " cards.");

        for (int i = 0; i < drawPile.Count; i++)
        {
            card = drawPile[i];
            Vector3 cardPos = drawPileDefaultPosition;
            cardPos.x += STAGGER_X * i;
            cardPos.z = 0.1f * i;
            //Debug.Log("Setting local position of " + card.cardObject.name + "  to " + cardPos);
            card.SetLocalPosition(cardPos);
            //card.SetSortingLayerName("Drawpile");
            card.SetSortingOrder(-10 * i);
        }
    }*/