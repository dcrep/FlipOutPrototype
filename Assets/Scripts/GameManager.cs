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
    DCExperiments,
    UITest,
    UILayout
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

    [SerializeField] public PlayerSessionManager sessionManager = new PlayerSessionManager();
    [SerializeField] public GameStateServer gameStateServer = new GameStateServer();

    // (currently) Only for Editor inspection:
    [SerializeField] public GameStateClient gameStateClient;
    //public GameStateClient gameStateClient2;

    public ServerDispatch serverDispatch = new ServerDispatch();

    public GameStatus currentGameState = GameStatus.Loading;

    public static ScenesSO scenesSO;

    public Scenes currentScene = Scenes.LoadingScreen;
    public Scenes previousScene = Scenes.LoadingScreen;

    public MultiplayerMode currentMultiplayerMode = MultiplayerMode.Disconnected;    

    [SerializeField] public UIManager uiManager = null;

    [SerializeField] public FlipOutGame flipOutGame = null;

    public bool forceHotseat = true;

    public List<string> hotseatPlayerNames = new List<string>() { "PlayerUNO", "Player2" };


    //private bool bDelayingEndTurn = false;

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

        /*AudioClip clip = Resources.Load<AudioClip>("Audio/OVS_CorporateVol2BeyontheBlueprintCut30");
        if (clip == null)
        {
            Debug.LogError("Failed to load audio clip from Resources folder.");
            return;
        }
        AudioManager.Play(clip, 0.10f);*/
        AudioManager.Play(AudioManager.audioSourcesSO.musicClips[0], 1f);
        AudioManager.Loop();
        
        //Debug.Log("Playing music: " + clip.name);
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
        if (currentGameState == GameStatus.Paused)
        {
            Debug.LogWarning("GameManager->LoadScene(): Game is paused, closing pause menu.");
            uiManager.PauseMenuClose();
            flipOutGame.EndGameCleanup();
        }
        if (currentGameState == GameStatus.Playing)
        {
            flipOutGame.EndGameCleanup();
        }
        previousScene = currentScene;
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
            case Scenes.UITest:
                SceneManager.LoadScene(scenesSO.UITestScene);
                //currentScene = Scenes.UITest;
                currentScene = scenesSO.UITestSceneEnum;
                currentGameState = GameStatus.Playing;
                break;
            case Scenes.UILayout:
                SceneManager.LoadScene(scenesSO.UILayoutScene);
                //currentScene = Scenes.UILayout;
                currentScene = scenesSO.UILayoutSceneEnum;
                currentGameState = GameStatus.UI;
                break;
            default:
                Debug.LogError("Unknown scene: " + scene);
                break;
        }
    }

    public void VerifyCurrentScene()
    {
        previousScene = currentScene;
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
        else if (activeSceneName == scenesSO.UITestScene)
        {
            if (currentScene != Scenes.UITest)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.UITest.ToString());
                currentScene = Scenes.Game; // !!
                currentGameState = GameStatus.Playing;
            }
        }
        else if (activeSceneName == scenesSO.UILayoutScene)
        {
            if (currentScene != Scenes.UILayout)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.UILayout.ToString());
                currentScene = Scenes.UILayout;
                currentGameState = GameStatus.UI;
            }
        }
        else
        {
            Debug.LogWarning("Active scene does not match any known scenes in ScenesSO: " + activeSceneName);
        }
    }

   // Scene -> Scene script (in each level) calls the following Awake/Start/Destroyed functions

    public void SceneAwake()
    {
        VerifyCurrentScene();
        Debug.Log("GameManager->SceneAwake() for scene: " + SceneManager.GetActiveScene().name + " currentScene: " + currentScene.ToString());
        if (currentScene == Scenes.Game)
        {
            // For now, we can have these in the level or created here if missing
            //! GameManager should be detached from this in the future
            if (uiManager == null)
            {
                var uIManagerGO = GameObject.Find("UIManager");
                if (uIManagerGO == null)
                {
                    //! Could be problematic depending on Editor-defined variables:
                    uIManagerGO = new GameObject("UIManager", typeof(UIManager));
                }
                uiManager = uIManagerGO.GetComponent<UIManager>();
            }
            if (flipOutGame == null)
            {
                var flipOutGO = GameObject.Find("FlipOutGame");
                if (flipOutGO == null)
                {
                    flipOutGO = new GameObject("FlipOutGame", typeof(FlipOutGame));
                }
                flipOutGame = flipOutGO.GetComponent<FlipOutGame>();
                //! Shared UI Manager for now (should ignore completely in GameManager in the future)
                flipOutGame.uiManager = uiManager;
                flipOutGame.sessionManager = sessionManager;
                flipOutGame.serverDispatch = serverDispatch;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SceneStart()
    {
        Debug.Log("GameManager->SceneStart() for scene: " + SceneManager.GetActiveScene().name);
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
                //forceHotseat
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

    public void SceneDestroyed()
    {
        Debug.Log("GameManager->SceneDestroyed() for scene: " + SceneManager.GetActiveScene().name + " currentScene: " + currentScene.ToString());
        if (previousScene == Scenes.Game)
        {
            // Unsubscribe from static Click event
            CardObject.onCardClicked -= OnCardClicked;
            flipOutGame.EndGameCleanup();
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
        // Previous workaround for Start() timing issue (avoiding Script Execution Order change)
        //!! Check logic
        /*if (currentScene == Scenes.Game && gameStateServer.serverDrawPile != null && !cardsShowing)
        {
            //UpdateDrawPile();
            //DrawPileDisplayTopCard();
            cardsShowing = true;
        }*/
        /*if (bDelayingEndTurn && uiManager.animationsInProgress == 0)
        {
            Debug.Log("Delaying EndTurn completed, proceeding with EndTurn.");
            bDelayingEndTurn = false;
            EndTurnClient();
        }*/
    }

    public void PauseGame()
    {
        if (currentGameState != GameStatus.Playing)
        {
            Debug.LogWarning("GameManager->PauseGame(): Cannot pause, game is not in Playing state.");
            return;
        }
        //flipOutGame.GameEventSaveStateAndTransition(FlipOutGameEvents.Paused);
        // Show pause menu UI
        uiManager.PauseMenuOpen();
        currentGameState = GameStatus.Paused;
        // Freeze game time
        //Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (currentGameState != GameStatus.Paused)
        {
            Debug.LogWarning("GameManager->ResumeGame(): Cannot resume, game is not in Paused state.");
            return;
        }
        //flipOutGame.GameEventRestoreState();
        // Hide pause menu UI
        uiManager.PauseMenuClose();
        currentGameState = GameStatus.Playing;
        // Resume game time
        //Time.timeScale = 1f;
    }

    public void SetLocalPlayerName(string name)
    {
        GameStateClient.localPlayerName = name;
    }

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


   // !TODO Called by NetworkManager when online game is ready to start (?)
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
        // else in Game scene

        if (numPlayers != playerNames.Length)
        {
            Debug.LogError("GameManager->StartHotseatGame(): numPlayers does not match length of playerNames array!");
        }

        Debug.Log("GameManager->StartHotseatGame()");
        Debug.Log("First name: " + playerNames[0]);

        // This should be setup before a call to this nethod
        if (uiManager == null)
        {
            Debug.LogError("GameManager->StartHotseatGame(): uiManager is null!");
            return;
        }

           // Player Ids are separate from player numbers but for hotseat they are basically the same
        uint[] playerIds = new uint[numPlayers];
        
        for (uint i = 0; i < numPlayers; i++)
        {
            playerIds[i] = i;
            sessionManager.AddLocalSession(playerNames[i], i, true);
        }

        currentMultiplayerMode = MultiplayerMode.LocalHotseat;
        currentGameState = GameStatus.Playing;

        flipOutGame.StartHotseatGame();

        gameStateClient = GameStateClient.CurrentGameStateClient;

/*
        // if (IsHost)
        //gameStateServer.InitGameStateServer(playerIds,playerNames);
        GameStateClient.InitGameStateClient(playerIds, playerNames);

        gameStateClient = GameStateClient.GetHotseatGameStateForPlayerNumber(0);
        gameStateClient2 = GameStateClient.GetHotseatGameStateForPlayerNumber(1);


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
        //TurnStart();*/
    }

    #region Dispatch-to-Game

    public void EndGameClient(int playerId)
    {
        if (currentScene != Scenes.Game)
        {
            Debug.LogError("GameManager->EndGameClient(): Not in Game scene!");
            return;
        }
        if (currentGameState != GameStatus.Playing)
        {
            Debug.LogError("GameManager->EndGameClient(): Game is not in Playing state!");
            return;
        }
        // currentGameScene = game, state = playing

        flipOutGame.EndGameClient(playerId);

        currentGameState = GameStatus.GameOver;

        //GameStateClient.GatherResults();
        Debug.Log("GameManager->EndGameClient()");
        //EndGameCleanup();

        LoadScene(Scenes.GameOver);
    }

    public void EndTurnClient()
    {
        flipOutGame.EndTurnClient();
    }

    public void StateCleanup()
    {
        gameStateServer.Cleanup();
        GameStateClient.CleanupClients();
    }

    // private void AdvanceToNextPlayerClient()
    // {
    //     Debug.Log("GameManager->AdvanceToNextPlayerClient()");
    //     // Clear board
    //     // (draw pile top card?)
    //     // Clear cards in play
    //     ClearObjectsInPlay();
    //     serverDispatch.AdvanceToNextPlayer();
    // }
#endregion

    // Called when a card is clicked - responds based on player turn, action, etc.
    void OnCardClicked(CardObject card)
    {
        //!TODO: Move this (forgot it ended up here)
        AudioManager.PlaySoundAt(AudioManager.audioSourcesSO.clickCard, 1f);
        
        Debug.Log("GameManager->OnCardClicked - Card clicked: " + card.gameObject.name + " currentPlayerIndex: " + gameStateServer.GetActivePlayerNumber());

        if (card.cardPOD.state == CardState.playerHolder)
        {
            Debug.Log("Actions available: " + string.Join(", ", FlipOutGame.GetAvailableActionsForCard(card.cardPOD)));
            /*if (cardsHighlighted.Contains(card))
            {
                card.HighlightCardToggle();
                cardsHighlighted.Remove(card);
                return;
            }
            else {
                card.HighlightCardToggle();
                cardsHighlighted.Add(card);
            }*/
        }
        else if (card.cardPOD.state == CardState.scorePile)
        {
            Debug.Log("Score pile count: " + GameStateClient.CurrentGameStateClient.GetPlayerByID(card.cardPOD.ownerPlayerID).scorePile.Count);
        }
        else {
            Debug.Log("Max run player 0: " + FlipOutGame.GetTotalAdjacentColorCount(GameStateClient.CurrentGameStateClient.GetPlayerByNumber(0)));
            Debug.Log("Max run player 1: " + FlipOutGame.GetTotalAdjacentColorCount(GameStateClient.CurrentGameStateClient.GetPlayerByNumber(1)));
        }
    }

}
