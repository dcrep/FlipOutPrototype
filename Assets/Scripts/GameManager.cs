using System;
using System.Collections.Generic;
using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[Serializable]
public enum Scenes
{
    LoadingScreen,
    MainMenu,
    LobbyLocal,
    LobbyOnline,
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
    [SerializeField] private Vector3[] playerScorePilePositions = new Vector3[5]
    {
    
        new(-8, -3, 0),    // Player 1 - Bottom left
        new(-8, 3, 0),     // Player 2 - Top left
        new(-9, 0, 0),    // Player 3 - Left center back        
        new(9, 0, 0),     // Player 4 - Right center back
        new(0, 4, 0)      // Player 5 - Center top (?!!)
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

    [SerializeField] private List<CardObject> cardsInPlay;

    [SerializeField] public List<CardObject> cardsHighlighted = new List<CardObject>();

    // Debugging purposes
    //Vector3 moveToPosition = new Vector3(1, 1, 0);
    //int cardsMoved = 0;

    public bool forceHotseat = true;

    public List<string> hotseatPlayerNames = new List<string>() { "PlayerUNO", "Player2" };

    public TextMeshProUGUI uiText; 

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
            case Scenes.LobbyLocal:
                SceneManager.LoadScene(scenesSO.HotseatLobbyScene);
                //currentScene = Scenes.LobbyLocal;
                currentScene = scenesSO.HotseatLobbySceneEnum;
                currentGameState = GameStatus.UI;
                break;
            case Scenes.LobbyOnline:
                SceneManager.LoadScene(scenesSO.OnlineLobbyScene);
                //currentScene = Scenes.LobbyOnline;
                currentScene = scenesSO.OnlineLobbySceneEnum;
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
        else if (activeSceneName == scenesSO.HotseatLobbyScene)
        {
            if (currentScene != Scenes.LobbyLocal)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.LobbyLocal.ToString());
                currentScene = Scenes.LobbyLocal;
                currentGameState = GameStatus.UI;
            }
        }
        else if (activeSceneName == scenesSO.OnlineLobbyScene)
        {
            if (currentScene != Scenes.LobbyOnline)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.LobbyOnline.ToString());
                currentScene = Scenes.LobbyOnline;
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
            if (currentMultiplayerMode == MultiplayerMode.Disconnected)
            {
                if (!forceHotseat)
                {
                    Debug.Log("Game scene loaded but in Disconnected mode.");
                    LoadScene(Scenes.MainMenu);
                    return;
                }
                currentMultiplayerMode = MultiplayerMode.LocalHotseat;
            }
            //else (connected) (or forced Hotseat)

            Debug.Log("Starting in Multiplayer mode: " + currentMultiplayerMode.ToString());

            if (currentMultiplayerMode == MultiplayerMode.Online)
            {
                // Register for network events
                //NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => OnMultiplayerConnect();
                //NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) => OnMultiplayerDisconnect();
                Debug.LogError("Online multiplayer not yet implemented!");
                LoadScene(Scenes.MainMenu);
                return;
            }
            // else - LocalHotseat
            //Debug.Log("Current Multiplayer mode: " + currentMultiplayerMode.ToString());
            CardObject.onCardClicked += OnCardClicked;

            // Hotseat
            StartHotseatGame(hotseatPlayerNames.Count, hotseatPlayerNames.ToArray());
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
        if (Input.GetKeyDown(KeyCode.Slash))
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
            if (cardsHighlighted.Contains(card))
            {
                card.HighlightCardToggle();
                cardsHighlighted.Remove(card);
                return;
            }
            else {
                card.HighlightCardToggle();
                cardsHighlighted.Add(card);
            }
        }
        else if (card.cardPOD.state == CardState.scorePile)
        {
            Debug.Log("Score pile count: " + GameStateClient.CurrentGameStateClient.GetPlayerByID(card.cardPOD.ownerPlayerID).scorePile.Count);
        }
        else {
            Debug.Log("Max run player 0: " + GameStateClient.GetTotalAdjacentColorCount(GameStateClient.CurrentGameStateClient.GetPlayerByNumber(0)));
            Debug.Log("Max run player 1: " + GameStateClient.GetTotalAdjacentColorCount(GameStateClient.CurrentGameStateClient.GetPlayerByNumber(1)));
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

    // Called by UI to start hotseat game, or by Game scene
    //  (input number of players and player names)
    public void StartHotseatGame(int numPlayers, string[] playerNames)
    {
        if (currentScene == Scenes.LobbyLocal)
        {
            hotseatPlayerNames.Clear();
            for (int i = 0; i < numPlayers; i++)
            {
                Debug.Log("Hotseat Lobby - Player " + i + " name: " + playerNames[i]);
                hotseatPlayerNames.Add(playerNames[i]);
            }
            LoadScene(Scenes.Game);
            return;
        }
        // else should be in Game scene
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
        cardsHighlighted = new List<CardObject>();

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

    #region Methods-dispatched-to

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
    /*public void EndPlayerTurnClient()
    {
        Debug.Log("GameManager->EndPlayerTurnClient()");
        ClearObjectsInPlay();
    }*/

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
        if (cardsHighlighted != null)
        {
            cardsHighlighted.Clear();
            cardsHighlighted = null;
        }
        gameStateServer.Cleanup();
        GameStateClient.CleanupClients();
        currentMultiplayerMode = MultiplayerMode.Disconnected;
    }
    public void StartPlayerTurnClient(int playerNum, int playerId, TurnAction availableActions)
    {
        Debug.Log("GameManager->StartPlayerTurnClient(): Player " + playerId + "'s turn started.");

        if (uiText == null)
        {
            uiText = GameObject.Find("PlayerInfo").GetComponent<TextMeshProUGUI>();
        }
        if (uiText != null)
        {
            uiText.text = "Player " + playerId + "'s " + (playerNum == 1 ? "^" : "v") + " Turn";
        }
        // This should be done at TurnEnd:
        //ClearObjectsInPlay();

        if (currentMultiplayerMode == MultiplayerMode.LocalHotseat)
        {
            if (GameStateClient.CurrentGameStateClient.handsDealt)
            {
                DealAllHandsClientFromState();
                BuildScorePile();
                FlipOutActions.ActOnFlipOutActionsForCurrentPlayer();
            }
            else
            {
                FlipOutActions.ActOnFlipOutActionsForCurrentPlayer();
                // Dealing is done through calls to DealFullHandClientFromState in FlipOutActions
                //DealAllHandsClientFromState();
                BuildScorePile();
                GameStateClient.CurrentGameStateClient.handsDealt = true;
            }
            
        }
        //StartTurnClient(playerId, availableActions);
    }

    void SetDrawPileTopCard(CardColor color)
    {
        if (color == CardColor.invalid)
        {
            if (drawPileTop != null)
            {
                Debug.LogWarning("GameManager->SetDrawPileTopCard(): color is invalid, removing drawPileTop card.");
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
            cardObjects[i].SetLocalPosition(playerPositions[playerNum] + cardHolderOffset * i);
            // Slight offset for visibility
            cardObjects[i].SetSortingOrder(1);
            // Set card state to playerHolder
            cardObjects[i].cardPOD.state = CardState.playerHolder;
        }
        SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
    }

    public void DealNewCardsToClient(int targetPlayerId, List<CardPODClient> dealtCards, int[] dealtCardIndices)
    {
        var player = GameStateClient.CurrentGameStateClient.GetPlayerByID(targetPlayerId);
        if (player == null)
        {
            Debug.LogError("GameManager->DealNewCardsToClient(): Could not find player number for playerId " + targetPlayerId);
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

    public void MoveCardsToScorePile(int playerId, int[] handIndices, CardColor cardColor)
    {
        PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerId);
        if (player == null)
        {
            Debug.LogError("GameManager->MoveCardsToScorePile(): Could not find player number for playerId " + playerId);
            return;
        }
        if (handIndices.Length == 0 || handIndices[0] == -1)
        {
            Debug.LogWarning("GameManager->MoveCardsToScorePile(): handIndices is empty for playerId " + playerId);
            return;
        }

        int playerNum = player.playerNumber;
        Vector3 scorePilePosition = playerScorePilePositions[playerNum];

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
                    cardObject.UpdateColor(cardColor);
                }

                // Move card to score pile position
                Vector3 targetPosition = scorePilePosition;
                
                cardObject.SetLocalPosition(targetPosition);
                cardObject.SetLocalScale(Vector3.one * 0.5f); // Slightly smaller
                cardObject.SetSortingOrder((player.scorePile.Count -1) * 2); // On top of score pile
            }
            else
            {
                Debug.LogError("GameManager->MoveCardsToScorePile(): No card found with cardID " + cardID);
            }
        }
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
            Debug.LogError("GameManager->SwipeCardsToScorePiles(): Could not find player for one of the playerIds " + playerId + " or " + targetPlayerId);
            return;
        }
        if (handIndices.Length == 0 || handIndices[0] == -1)
        {
            Debug.LogWarning("GameManager->SwipeCardsToScorePiles(): handIndices is empty for targetPlayerId " + targetPlayerId);
            return;
        }

        int playerNum = player.playerNumber;
        int playerTargetNum = targetPlayer.playerNumber;
        Vector3 scorePilePosition = playerScorePilePositions[playerNum];
        Vector3 targetScorePilePosition = playerScorePilePositions[playerTargetNum];

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
                    cardObject.UpdateColor(cardColor);
                }

                // Move card to score pile position
                Vector3 targetPosition = scorePilePosition;
                
                cardObject.SetLocalPosition(targetPosition);
                cardObject.SetLocalScale(Vector3.one * 0.5f); // Slightly smaller
                cardObject.SetSortingOrder((player.scorePile.Count -1) * 2); // On top of score pile
            }
            else
            {
                Debug.LogError("GameManager->SwipeCardsToScorePiles(): No card found with cardID " + cardID);
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
                finalCardObject.UpdateColor(cardColor);
            }

            // Move card to score pile position
            Vector3 targetPosition = targetScorePilePosition;
            
            finalCardObject.SetLocalPosition(targetPosition);
            finalCardObject.SetLocalScale(Vector3.one * 0.5f); // Slightly smaller
            finalCardObject.SetSortingOrder((targetPlayer.scorePile.Count -1) * 2); // On top of score pile
        }
        else
        {
            Debug.LogError("GameManager->SwipeCardsToScorePiles(): No card found with cardID " + finalCardID);
        }
        //this is called along with Score/Swipe to create/queue deal action:
        // GameManager.Instance.serverDispatch.DealCardsToPlayerHandIndices(playerId, handIndices);
    }

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
                cardObject.SetSortingOrder(i * 2); // On top of score pile
                // Set card state to scorePile
                cardObject.cardPOD.state = CardState.scorePile;
            }
        }
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
            cardObjects[i] = CardObjectFromPODClient(ownerPlayerID, hand[i]);  //.Clone());
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

        cardsInPlay.Add(cardObject);

        return cardObject;
    }


    public CardObject CardObjectFromPODClient(int playerID, CardPODClient cardPOD)
    {
        return InstantiateCardObjectFromPOD(cardPOD, deckOffscreenPosition, CardState.playerHolder, playerID);
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
            Debug.LogError("GameManager->FlipCard(): Could not find owner player for cardID " + cardID);
            return;
        }
        int index = player.GetIndexOfCardByID(cardID);
        cardToFlip = player.hand[index].cardObject;

        if (cardToFlip != null)
        {
            Debug.Log("GameManager->FlipCard(): Flipping card with cardID " + cardID + " to color " + newColor.ToString());
            //cardToFlip.FlipCard();
            cardToFlip.UpdateColor(newColor);
        }
        else
        {
            Debug.LogError("GameManager->FlipCard(): No card found with cardID " + cardID);
        }
    }

    public void SwitchCardsClient(int cardID1, int cardID2)
    {
        // Find the CardObjects with the given cardIDs
        CardObject card1 = null;
        CardObject card2 = null;

        PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByCardId(cardID1);
        if (player == null)
        {
            Debug.LogError("GameManager->SwitchCards(): Could not find owner player for cardID " + cardID1);
            return;
        }

        int index1 = player.GetIndexOfCardByID(cardID1);
        int index2 = player.GetIndexOfCardByID(cardID2);

        card1 = player.hand[index1].cardObject;
        card2 = player.hand[index2].cardObject;

        if (card1 != null && card2 != null)
        {
            // Swap positions
            Vector3 tempPosition = card1.transform.position;
            card1.transform.position = card2.transform.position;
            card2.transform.position = tempPosition;

            // Index of card in player's hand:
            int cardsOwnerId = card1.cardPOD.ownerPlayerID;
            //GameStateClient.CurrentGameStateClient.GetPlayerByID(cardsOwnerId).GetIndexOfCardByID(cardID1);
            //GameStateClient.CurrentGameStateClient.GetPlayerByID(cardsOwnerId).GetIndexOfCardByID(cardID2);
            
            GameStateClient.CurrentGameStateClient.SwitchCardsInPlayerHand(cardsOwnerId, cardID1, cardID2);
            //GameStateClient.CurrentGameStateClient.GetPlayerByID(cardsOwnerId).SwitchCardsInHandByID(cardID1,cardID2);
        }
        else
        {
            Debug.LogError("GameManager->SwitchCards(): Could not find both cards with IDs " + cardID1 + " and " + cardID2);
        }
    }

    public void SwapCards1Client(int playerSwappingId, int playerSwapWithId, int cardSwappingID1, int cardSwapWithID1, CardColor swappingNewColor, CardColor swapWithNewColor)
    {
        PlayerXClient playerSwapping = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerSwappingId);
        PlayerXClient playerSwapWith = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerSwapWithId);

        if (playerSwapping == null || playerSwapWith == null)
        {
            Debug.LogError("GameManager->SwapCards1Client(): Could not find one of the players for swapping: " + playerSwappingId + " or " + playerSwapWithId);
            return;
        }

        int indexSwappingCard1 = playerSwapping.GetIndexOfCardByID(cardSwappingID1);
        int indexSwapWithCard1 = playerSwapWith.GetIndexOfCardByID(cardSwapWithID1);

        if (indexSwappingCard1 == -1 || indexSwapWithCard1 == -1)
        {
            Debug.LogError("GameManager->SwapCards1Client(): Could not find one of the cards for swapping: " + cardSwappingID1 + " or " + cardSwapWithID1);
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
            
            // Swap positions
            Vector3 tempPosition = cardSwapping1.transform.position;
            cardSwapping1.transform.position = cardSwapWith1.transform.position;
            cardSwapWith1.transform.position = tempPosition;

            // Update GameStateClient hands
            GameStateClient.CurrentGameStateClient.Swap1CardBetweenPlayers(playerSwappingId, playerSwapWithId, cardSwappingID1, cardSwapWithID1);
        }
        else
        {
            Debug.LogError("GameManager->SwapCards1Client(): Could not find both cards with IDs " + cardSwappingID1 + " and " + cardSwapWithID1);
        }
    }

    public void SwapCards2Client(int playerSwappingId, int playerSwapWithId, int cardId1, int cardId2, int cardSwapWithID1, int cardSwapWithID2,
         CardColor swapping1NewColor, CardColor swapping2NewColor, CardColor swapWith1NewColor, CardColor swapWith2NewColor)
    {
        PlayerXClient playerSwapping = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerSwappingId);
        PlayerXClient playerSwapWith = GameStateClient.CurrentGameStateClient.GetPlayerByID(playerSwapWithId);

        if (playerSwapping == null || playerSwapWith == null)
        {
            Debug.LogError("GameManager->SwapCards2Client(): Could not find one of the players for swapping: " + playerSwappingId + " or " + playerSwapWithId);
            return;
        }

        int indexSwapCard1 = playerSwapping.GetIndexOfCardByID(cardId1);
        int indexSwapCard2 = playerSwapping.GetIndexOfCardByID(cardId2);
        int indexSwapWithCard1 = playerSwapWith.GetIndexOfCardByID(cardSwapWithID1);
        int indexSwapWithCard2 = playerSwapWith.GetIndexOfCardByID(cardSwapWithID2);

        if (indexSwapCard1 == -1 || indexSwapWithCard1 == -1 || indexSwapCard2 == -1 || indexSwapWithCard2 == -1)
        {
            Debug.LogError("GameManager->SwapCards2Client(): Could not find one of the cards for swapping.");
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
            Vector3 tempPosition = cardSwapping1.transform.position;
            cardSwapping1.transform.position = cardSwapWith1.transform.position;
            cardSwapWith1.transform.position = tempPosition;

            // Swap positions of second pair
            tempPosition = cardSwapping2.transform.position;
            cardSwapping2.transform.position = cardSwapWith2.transform.position;
            cardSwapWith2.transform.position = tempPosition;
            // Update GameStateClient hands (note we pass ids that haven't had consecutive hand-order enforced)
            GameStateClient.CurrentGameStateClient.Swap2CardsBetweenPlayers(playerSwappingId, playerSwapWithId, cardId1, cardId2, cardSwapWithID1, cardSwapWithID2);
        }
        else
        {
            Debug.LogError("GameManager->SwapCards2Client(): Could not find all four cards for swapping.");
        }
    }

#endregion



#region Client-Server


    // Client-side
    //public void RequestFlipCard(int playerId, int cardId)
    //{
    //    SendServerRpc_FlipCard(playerId, cardId);
    ////}

#endregion

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