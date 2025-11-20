using System;
using System.Collections.Generic;
using Unity.Multiplayer.Playmode;
using Unity.VisualScripting;
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
public enum GameState
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

    public GameState currentGameState = GameState.Loading;

    public static ScenesSO scenesSO;

    public Scenes currentScene = Scenes.LoadingScreen;

    [SerializeField]private GameStateScript gameStateScript = new GameStateScript();
    public MultiplayerMode currentMultiplayerMode = MultiplayerMode.Disconnected;    

    [SerializeField] private PlayerX[] players = new PlayerX[5];

    GameObject playersParentGO = null;
    private int localPlayer1Index = 0;
    private int currentPlayerIndex = 0;
    private int totalPlayers = 1;

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
    GameObject deckParentGO;

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
        switch (scene)
        {
            case Scenes.MainMenu:
                LoadScene(scenesSO.mainMenuScene);
                currentScene = Scenes.MainMenu;
                currentGameState = GameState.UI;
                break;
            case Scenes.Game:
                LoadScene(scenesSO.gameScene);
                currentScene = Scenes.Game;
                currentGameState = GameState.Playing;
                break;
            case Scenes.GameOver:
                LoadScene(scenesSO.gameOverScene);
                currentScene = Scenes.GameOver;
                currentGameState = GameState.GameOver;
                break;
            case Scenes.DCExperiments:
                LoadScene(scenesSO.DCExperimentsScene);
                currentScene = Scenes.Game; // !!
                currentGameState = GameState.Playing;
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
                currentGameState = GameState.UI;
            }
        }
        else if (activeSceneName == scenesSO.gameScene)
        {
            if (currentScene != Scenes.Game)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.Game.ToString());
                currentScene = Scenes.Game;
                currentGameState = GameState.Playing;
            }
        }
        else if (activeSceneName == scenesSO.gameOverScene)
        {
            if (currentScene != Scenes.GameOver)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.GameOver.ToString());
                currentScene = Scenes.GameOver;
                currentGameState = GameState.GameOver;
            }
        }
        else if (activeSceneName == scenesSO.DCExperimentsScene)
        {
            if (currentScene != Scenes.DCExperiments)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + currentScene.ToString() + "; updating to " + Scenes.DCExperiments.ToString());
                currentScene = Scenes.Game; // !!
                currentGameState = GameState.Playing;
            }
        }
        else
        {
            Debug.LogWarning("Active scene does not match any known scenes in ScenesSO: " + activeSceneName);
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
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
            //currentGameState = GameState.Loading;
        }
    }

   // Called by NetworkManager when online game is ready to start (?)
    void StartOnlineGame(int numPlayers)
    {
        if (currentScene != Scenes.Game)
        {
            Debug.LogError("GameManager->StartOnlineGame(): Not in Game scene!");
            return;
        }
        Debug.Log("GameManager->StartOnlineGame()");
        totalPlayers = numPlayers;
        //currentPlayerIndex = NetworkManager.Instance.GetLocalPlayerIndex();
        currentMultiplayerMode = MultiplayerMode.Online;
        currentGameState = GameState.Playing;
    }

    // Called by UI to start hotseat game (input number of players and player names)
    void StartHotseatGame(int numPlayers, string[] playerNames)
    {
        if (currentScene != Scenes.Game)
        {
            Debug.LogError("GameManager->StartHotseatGame(): Not in Game scene!");
            return;
        }
        Debug.Log("GameManager->StartHotseatGame()");
        Debug.Log("First name: " + playerNames[0]);
        gameStateScript.InitServer();
        totalPlayers = numPlayers;
        currentPlayerIndex = localPlayer1Index;
        currentMultiplayerMode = MultiplayerMode.LocalHotseat;
        currentGameState = GameState.Playing;

        if (numPlayers != playerNames.Length)
        {
            Debug.LogWarning("GameManager->StartHotseatGame(): numPlayers does not match length of playerNames array!");
        }

        cardsInPlay = new List<CardObject>();

        playersParentGO = new GameObject("_Players");
        for (int i = 0; i < numPlayers; i++)
        {
            GameObject playerGO = new GameObject("Player" + i, typeof(PlayerX));
            playerGO.transform.SetParent(playersParentGO.transform);
            //playerGO.AddComponent<PlayerX>();
            
            players[i] = playerGO.GetComponent<PlayerX>();
            players[i].playerName = playerNames[i];
            players[i].playerId = i;
            Debug.Log("Player " + i + " name set to: " + players[i].playerName);
        }

        inputManager.activePlayer = players[currentPlayerIndex];

        //drawPileTop = InstantiateCardObjectFromPOD(gameStateScript.serverDrawPile[0], drawPileDefaultPosition, cardState.drawPile);
        drawPileTop = InstantiateCardObjectFromPOD(new CardPOD(), drawPileDefaultPosition, cardState.drawPile, -1);

        DealCardsToPlayers();
        //TurnStart();
    }

    void EndGame()
    {
        Debug.Log("GameManager->EndGame()");
        EndGameCleanup();
        //LoadScene(Scenes.GameOver);
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
        DestroyPlayers();
        gameStateScript.Cleanup();
        currentMultiplayerMode = MultiplayerMode.Disconnected;
    }

    public PlayerX GetActivePlayer()
    {
        return players[currentPlayerIndex];
    }

    public PlayerX GetPlayerByID(int playerID)
    {
        if (playerID < 0 || playerID >= totalPlayers)
        {
            Debug.LogError("GameManager->GetPlayerByID(): Invalid playerID " + playerID);
            return null;
        }
        return players[playerID];
    }

    public List<PlayerX> GetActivePlayers()
    {
        List<PlayerX> activePlayers = new List<PlayerX>();
        for (int i = 0; i < totalPlayers; i++)
        {
            activePlayers.Add(players[i]);
        }
        return activePlayers;
    }

    void DestroyPlayers()
    {
        for (int i = 0; i < 5; i++)
        {
            players[i] = null;
        }
    }

    void SetDrawPileTopCard()
    {
        if (gameStateScript.serverDrawPile.Count == 0)
        {
            if (drawPileTop != null)
            {
                Destroy(drawPileTop.gameObject);
                drawPileTop = null;
            }
            Debug.LogWarning("SetDrawPileTopCard: draw pile empty!");
            return;
        }
        CardPOD topPOD = gameStateScript.serverDrawPile[0];
        drawPileTop.SetCardPOD(topPOD);
        drawPileTop.cardPOD.state = cardState.drawPile;
        return;
    }

    void DealCardsToPlayers()
    {
        const int CARDS_PER_PLAYER = 6;
        for (int p = 0; p < totalPlayers; p++)
        {
            List<CardObject> dealtCards = DrawCardsAsObjects(CARDS_PER_PLAYER, p, true);
            if (dealtCards == null)
            {
                Debug.LogError("GameManager->DealCardsToPlayers(): Failed to draw cards for Player " + p);
                return;
            }
            for (int c = 0; c < CARDS_PER_PLAYER; c++)
            {
                // Set card position to player position
                dealtCards[c].SetLocalPosition(playerPositions[p] + cardHolderOffset * c);
                // Slight offset for visibility
                dealtCards[c].SetSortingOrder(50);
                // Set card state to playerHolder
                dealtCards[c].cardPOD.state = cardState.playerHolder;
                if (p != currentPlayerIndex)
                {
                    // other players cards show back side to current player
                    // !Need to flip these back when another player becomes current player!!
                    dealtCards[c].FlipCard();
                }
                players[p].hand[c] = dealtCards[c];
                players[p].hand[c].cardPOD.ownerPlayerID = p;
                // For now, just add to cardsInPlay list
                //cardsInPlay.Add(dealtCards[c]);
            }
        }
    }

    private CardObject InstantiateCardObjectFromPOD(CardPOD cardPOD, Vector3 position, cardState newState = cardState.playerHolder, int playerID = -1)
    {
        if (deckParentGO == null)
        {
            deckParentGO = new GameObject("_Cards");            
        }
        if (cardPrefab == null)
        {
            cardPrefab = Resources.Load<GameObject>("Prefabs/CardPF");
        }

        GameObject cardGO = GameObject.Instantiate(cardPrefab, position, Quaternion.identity, deckParentGO.transform);
        
        CardObject cardObject = cardGO.GetComponent<CardObject>();

        // Attach Card POD to CardObject
        cardPOD.state = newState;
        cardPOD.ownerPlayerID = playerID;
        cardObject.SetCardPOD(cardPOD);

        return cardObject;
    }

    // Draw a single card from the draw pile
    // (needs to be expanded for player id (optional for hotseat?) and multiplayer sync)
    public CardObject DrawCardAsObject(int playerID, bool ignorePlayerId = false)
    {
       if (!ignorePlayerId && playerID > 0 && currentPlayerIndex != playerID)
        {
            Debug.LogError("GameManager->DrawCardsAsObjects(): It's not Player " + playerID + "'s turn!");
            return null;
        }
        if (currentMultiplayerMode == MultiplayerMode.Disconnected)
        {
            Debug.LogError("GameManager: DrawCard - Not in multiplayer mode!");
            return null;
        }
        if (currentMultiplayerMode == MultiplayerMode.LocalHotseat)
        {
            CardPOD cardPOD = gameStateScript.DrawCard();
            CardObject cardObject = InstantiateCardObjectFromPOD(cardPOD, deckOffscreenPosition, cardState.playerHolder, playerID);
            SetDrawPileTopCard();
            return cardObject;
        }
        else
        {
            Debug.LogError("GameManager: DrawCard - Online multiplayer not yet implemented!");
            return null;
        }
    }

   // Draw numCards from draw pile and create local CardObjects - client side
    public List<CardObject> DrawCardsAsObjects(int numCards, int playerID, bool ignorePlayerId = false)
    {
        if (!ignorePlayerId && playerID > 0 && currentPlayerIndex != playerID)
        {
            Debug.LogError("GameManager->DrawCardsAsObjects(): It's not Player " + playerID + "'s turn!");
            return null;
        }
        if (currentMultiplayerMode == MultiplayerMode.Disconnected)
        {
            Debug.LogError("GameManager->DrawCardsAsObjects(): Not in multiplayer mode!");
            return null;
        }
        if (currentMultiplayerMode == MultiplayerMode.LocalHotseat)
        {
            List<CardPOD> cardPODs = gameStateScript.DrawCards(numCards);

            // endgame reached
            if (cardPODs == null)
                return null;
            
            if (deckParentGO == null)
            {
                deckParentGO = new GameObject("_Cards");            
            }
            if (cardPrefab == null)
            {
                cardPrefab = Resources.Load<GameObject>("Prefabs/CardPF");
            }

            List<CardObject> drawnCards = new List<CardObject>();
            for (int i = 0; i < numCards; i++)
            {
                //GameObject cardGO = GameObject.Instantiate(cardPrefab, deckOffscreenPosition, Quaternion.identity, deckParentGO.transform);
                CardObject cardObject = InstantiateCardObjectFromPOD(cardPODs[i], deckOffscreenPosition, cardState.playerHolder, playerID);
                // and put in deckObjects list
                drawnCards.Add(cardObject);
            }
            SetDrawPileTopCard();
            return drawnCards;
        }
        else
        {
            Debug.LogError("GameManager->DrawCardsAsObjects(): Online multiplayer not yet implemented!");
            return null;
        }
    }

    void TurnEnd()
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
        if (currentScene == Scenes.Game && gameStateScript.serverDrawPile != null && !cardsShowing)
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
        Debug.Log("GameManager->OnCardClicked - Card clicked: " + card.gameObject.name + " currentPlayerIndex: " + currentPlayerIndex);

        // !Just for testing purposes - move or flip:
        /*if (card.cardPOD.state == cardState.drawPile)
        {
            var cardObject = DrawCardAsObject(currentPlayerIndex);
            cardObject.SetLocalPosition(playerPositions[currentPlayerIndex]);
            //moveToLocation.x += cardsMoved * 0.2f; // slight offset for visibility
            cardObject.SetSortingOrder(cardsMoved * 10 + -100);
            cardsMoved++;
            cardObject.cardPOD.state = cardState.scorePile;
        }
        else
            card.FlipCard();*/
        //card.FlipCard();

        if (card.cardPOD.state == cardState.playerHolder)
        {
            Debug.Log("Actions available: " + string.Join(", ", gameStateScript.GetAvailableActionsForCard(card.cardPOD)));
        }
        else
        {
            Debug.Log("Max run player 0: " + gameStateScript.GetTotalAdjacentColorCount(players[0]));
            Debug.Log("Max run player 1: " + gameStateScript.GetTotalAdjacentColorCount(players[1]));
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