using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class FlipOutUI
{

    [System.Serializable]
    public struct FlipoutUIPlayerLayout
    {
        public Vector3 position;
        public float rotationZ;
        public float scale;
        public float objectOffsetX;
        public float scorePileOffsetX;
    }

    //[SerializeField] FlipOutUILayoutSO layoutSO;

    public Vector3 deckPosition= new Vector3(6f, 0f, 0f);
    public float deckRotationZ = 0f;
    public float deckScale = 1f;

    public float scorePileScaleMultiplier = 0.66f;
    FlipoutUIPlayerLayout[] playerLayoutFor2 = new FlipoutUIPlayerLayout[2]
    {
        new() { position = new Vector3(-5.8f, -3, 0), rotationZ = 0f,
                scale = 1f, objectOffsetX = 2.55f, scorePileOffsetX = -2.15f },
        new() { position = new Vector3(-5.8f, 3, 0), rotationZ = 0f,
                scale = 1f, objectOffsetX = 2.55f, scorePileOffsetX = -2.15f }
    };
    FlipoutUIPlayerLayout[] playerLayoutFor3 = new FlipoutUIPlayerLayout[3]
    {
        new() { position = new Vector3(-5.75f, -3.25f, 0), rotationZ = 0f,
                scale = 0.85f, objectOffsetX = 2.15f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-5.75f, 0, 0), rotationZ = 0f,
                scale = 0.85f, objectOffsetX = 2.15f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-5.75f, 3.25f, 0), rotationZ = 0f,
                scale = 0.85f, objectOffsetX = 2.15f, scorePileOffsetX = -2.0f }
    };
    FlipoutUIPlayerLayout[] playerLayoutFor4 = new FlipoutUIPlayerLayout[4]
    {
        new() { position = new Vector3(-4.25f, -3.75f, 0), rotationZ = 0f,
                scale = 0.7f, objectOffsetX = 1.8f, scorePileOffsetX = -1.75f },
        new() { position = new Vector3(-4.25f, -1.25f, 0), rotationZ = 0f,
                scale = 0.7f, objectOffsetX = 1.8f, scorePileOffsetX = -1.75f },
        new() { position = new Vector3(-4.25f, 1.25f, 0), rotationZ = 0f,
                scale = 0.7f, objectOffsetX = 1.8f, scorePileOffsetX = -1.75f },
        new() { position = new Vector3(-4.25f, 3.75f, 0), rotationZ = 0f,
                scale = 0.7f, objectOffsetX = 1.8f, scorePileOffsetX = -1.75f }
    };
    FlipoutUIPlayerLayout[] playerLayoutFor5 = new FlipoutUIPlayerLayout[5]
    {
        new() { position = new Vector3(-3.25f, -4f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -1.5f },
        new() { position = new Vector3(-3.25f, -2f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -1.5f },
        new() { position = new Vector3(-3.25f, 0f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -1.5f },
        new() { position = new Vector3(-3.25f, 2f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -1.5f },
        new() { position = new Vector3(-3.25f, 4f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -1.5f }
    };

    private Vector3 deckOffscreenPosition = new Vector3(-1000, -1000, 0);
    private Vector3[] playerScoreTextPositions = new Vector3[5]
    {
        Vector3.zero,
        Vector3.zero,
        Vector3.zero,
        Vector3.zero,
        Vector3.zero
    };

    [SerializeField] private Canvas canvas = null;
    private float canvasPPU = 100f;
    GameObject cardsParentGO = null;
    GameObject cardPrefab = null;

    CardObject drawPileTop = null;

    int nextSortOrder = 1;

    private GameObject canvasTextParentGO = null;
    
    private GameObject[] playerTextGO = new GameObject[5];
    private GameObject[] scoreKeeperGO = new GameObject[5];
    private TextMeshProUGUI[] playerText = new TextMeshProUGUI[5];
    [SerializeField] private TextMeshProUGUI[] scoreText = new TextMeshProUGUI[5];

    UIManager uiManager
    {
        get
        {
            return GameManager.Instance.uiManager;
        }
    }

    public FlipOutUI()
    {
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        canvasPPU = canvas.referencePixelsPerUnit;
        canvasTextParentGO = new GameObject("CanvasTextParent");
        canvasTextParentGO.transform.SetParent(canvas.transform, false);
    }

    public void Cleanup()
    {
        //!TODO: Determine if need to or is useful to track cards in play (?)
        // (will need to create on game start, clear here, and add in InstatiateCardObjectFromPOD)
        /*if (cardsInPlay != null)
            ClearHighlightedCards();
            foreach (CardObject card in cardsInPlay)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            cardsInPlay.Clear();
        }*/
        /*if (cardsHighlighted != null)
        {
            cardsHighlighted.Clear();
            cardsHighlighted = null;
        }*/
        if (cardsParentGO != null)
        {
            GameObject.Destroy(cardsParentGO);
            cardsParentGO = null;
        }
        if (canvasTextParentGO != null)
        {
            GameObject.Destroy(canvasTextParentGO);
            canvasTextParentGO = null;
            playerTextGO = new GameObject[5];
            scoreKeeperGO = new GameObject[5];
            playerText = new TextMeshProUGUI[5];
            scoreText = new TextMeshProUGUI[5];
        }
        /*if (playersParentGO != null)
        {
            Destroy(playersParentGO);
            playersParentGO = null;
        }*/
        if (drawPileTop != null)
        {
            GameObject.Destroy(drawPileTop.gameObject);
            drawPileTop = null;
        }
    }

#region Deal-or-Show Hands
    public void DealAllHandsClientFromState()
    {
        for (int playerNum = 0; playerNum < GameStateClient.GetTotalPlayers(); playerNum++)
        {
            DealFullHandClientFromState(GameStateClient.CurrentGameStateClient.GetPlayerIDByNumber(playerNum));
        }
        //SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
    }

    // This doesn't animate dealing cards, just creates them in their positions

    public void DealFullHandClientFromState(int targetPlayerId)
    {
        var player = GameStateClient.CurrentGameStateClient.GetPlayerByID(targetPlayerId);
        //! Can't create cardObjects for other players, just cardPODs
        CardObject[] cardObjects = new CardObject[6];

        int playerNum = player.playerNumber;
        FlipoutUIPlayerLayout layout = GetUIPlayerLayout(GameStateClient.GetTotalPlayers(), playerNum);

        
        for (int i = 0; i < player.hand.Length; i++)
        {
            cardObjects[i] = InstantiateCardObjectFromPOD(player.hand[i], layout.position + new Vector3(layout.objectOffsetX * i, 0, 0), layout.scale, layout.rotationZ, CardState.playerHolder, targetPlayerId);
            // Set card position to player position
            //! UI Stuff
            //cardObjects[i].SetLocalPosition(layout.position + new Vector3(layout.objectOffsetX * i, 0, 0));
            // Slight offset for visibility
            cardObjects[i].SetSortingOrder(1);
            // Set card state to playerHolder
            //cardObjects[i].cardPOD.state = CardState.playerHolder;
        }
        SetDrawPileTopCard(GameStateClient.GetDeckTopCardColor());
    }

  
    public void DealNewCardsToClient(int targetPlayerId, List<CardPODClient> dealtCards, int[] positions, CardColor deckTopColor)
    {
        CardObject[] cardObjects = new CardObject[dealtCards.Count];
        int playerNum = GameStateClient.CurrentGameStateClient.GetPlayerTableNumberByID(targetPlayerId);
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

        //!TODO: This only needs to be done once per (game [turn?])
        CalculateDeckPosition(GameStateClient.GetTotalPlayers());

        FlipoutUIPlayerLayout layout = GetUIPlayerLayout(GameStateClient.GetTotalPlayers(), playerNum);

        for (int i = 0; i < dealtCards.Count; i++)
        {
            int cardIndex = positions[i];

            CardObject cardObject = InstantiateCardObjectFromPOD(dealtCards[i], deckPosition, deckScale, deckRotationZ, CardState.playerHolder, targetPlayerId);

            // Set card position to player position
            //cardObject.transform.SetPositionAndRotation(deckPosition, Quaternion.Euler(0, 0, deckRotationZ));
            // Set card state to playerHolder
            //cardObject.cardPOD.state = CardState.playerHolder;
            // Slight offset for visibility while animating (shows over all other cards)
            cardObject.SetSortingOrder(sortOrder);
            sortOrder--;
            //cardObject.SetLocalPosition(layout.position + new Vector3(layout.objectOffsetX * cardIndex, 0, 0));

            //Debug.Log("Some object reference is failing here. CardObject: " + (cardObject != null ? cardObject.gameObject.name : "null") +
            //          " uiManager: " + (uiManager != null ? uiManager.gameObject.name : "null") +
            //          " animationManager: " + (uiManager.animationManager != null ? uiManager.animationManager.gameObject.name : "null"));

            Vector3 cardDestPos = layout.position + new Vector3(layout.objectOffsetX * cardIndex, 0, 0);
            Vector3 cardDestScale = new Vector3(layout.scale, layout.scale, 1);
            Quaternion cardDestRot = Quaternion.Euler(0, 0, layout.rotationZ);
            
            uiManager.animationManager.AddSequential( 
                new AnimationTask { Routine = uiManager.AnimateCardMovementScaleAndRotation(cardObject,
                                              cardDestPos,
                                              cardDestScale, cardDestRot), DelayAfter = 0.0f } 
            );
        }
        // Run and reset sorting order afterwards (to keep dealing cards on top during animation)
        //uiManager.animationManager.Run(ResetCardSortingOrdersAfterDeal);

        // Called in FlipOutActions (should I do it here instead?):
        //GameStateClient.CurrentGameStateClient.AssignCardsToPlayerHand(targetPlayerId, dealtCards, positions);

        SetDrawPileTopCard(deckTopColor);
    }
#endregion Deal-or-Show Hands
    
#region Draw and Score Piles

    public void BuildScorePile()
    {
        FlipoutUIPlayerLayout[] layouts = GetUIPlayerLayouts(GameStateClient.GetTotalPlayers());

        for (int playerTableNum = 0; playerTableNum < GameStateClient.GetTotalPlayers(); playerTableNum++)
        {
            PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(playerTableNum);
            Vector3 scorePilePosition = layouts[playerTableNum].position + new Vector3(layouts[playerTableNum].scorePileOffsetX, 0, 0);

            for (int i = 0; i < player.scorePile.Count; i++)
            {
                CardPODClient cardPOD = player.scorePile[i];
                CardObject cardObject = InstantiateCardObjectFromPOD(cardPOD, scorePilePosition,
                    layouts[playerTableNum].scale * scorePileScaleMultiplier, layouts[playerTableNum].rotationZ,
                    CardState.scorePile, player.playerId
                );
                //CardObjectFromPODClient(player.playerId, layouts[playerTableNum].scale * scorePileScaleMultiplier, layouts[playerTableNum].rotationZ, cardPOD);
                cardPOD.cardObject = cardObject;

                //Vector3 targetPosition = scorePilePosition;

                //cardObject.SetLocalPosition(targetPosition);
                //cardObject.SetLocalScale(Vector3.one * scorePileScaleMultiplier); // Slightly smaller
                cardObject.SetSortingOrder((i+2) * 2); // On top of score pile
                // Set card state to scorePile
                cardObject.cardPOD.state = CardState.scorePile;
            }
        }
    }
    public void SetDrawPileTopCard(CardColor color)
    {
        if (color == CardColor.invalid)
        {
            if (drawPileTop != null)
            {
                Debug.LogWarning("FlipOut->SetDrawPileTopCard(): color is invalid, removing drawPileTop card.");
                GameObject.Destroy(drawPileTop.gameObject);
                drawPileTop = null;
            }
            return;
        }
        //else

        //!TODO: This only needs to be done once per (game [turn?])
        CalculateDeckPosition(GameStateClient.GetTotalPlayers());

        CardPODClient topPOD = new CardPODClient();
        // topPOD.cardID = ownerPlayerID = -1; // defaults
        topPOD.color = color;
        if (drawPileTop == null)
        {
            drawPileTop = InstantiateCardObjectFromPOD(topPOD, deckPosition, deckScale, deckRotationZ, CardState.drawPile, -1);
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
#endregion Draw and Score Piles

#region CardObject Creation
   private CardObject InstantiateCardObjectFromPOD(CardPODClient cardPOD, Vector3 position, float scale, float rotationZ,CardState newState = CardState.playerHolder, int playerID = -1)
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

        //cardObject.SetLocalPosition(position);
        cardObject.SetLocalScale(new Vector3(scale, scale, 1) );
        cardObject.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, rotationZ));

        // Attach Card POD to CardObject
        cardPOD.state = newState;
        cardPOD.ownerPlayerID = playerID;
        cardObject.SetCardPOD(cardPOD);

        //cardsInPlay.Add(cardObject);

        return cardObject;
    }


    public CardObject CardObjectFromPODClient(int playerID, float scale, float rotationZ, CardPODClient cardPOD)
    {
        return InstantiateCardObjectFromPOD(cardPOD, deckOffscreenPosition, scale, rotationZ, CardState.playerHolder, playerID);
    }

#endregion CardObject Creation



#region UI-Layout

    private FlipoutUIPlayerLayout[] GetUIPlayerLayouts(int numPlayers)
    {
        FlipoutUIPlayerLayout[] playerLayouts;
        switch (numPlayers)
        {
            case 2:
                playerLayouts = playerLayoutFor2;
                break;
            case 3:
                playerLayouts = playerLayoutFor3;
                break;
            case 4:
                playerLayouts = playerLayoutFor4;
                break;
            case 5:
                playerLayouts = playerLayoutFor5;
                break;
            default:
                Debug.LogWarning("Invalid number of players: " + numPlayers);
                return playerLayoutFor2;
        }
        return playerLayouts;
    }

    public FlipoutUIPlayerLayout GetUIPlayerLayout(int numPlayers, int playerTableNum)
    {
        return GetUIPlayerLayouts(numPlayers)[playerTableNum];
    }

    private FlipoutUIPlayerLayout GetUIPlayerLayoutAtCardIdx(int numPlayers, int playerTableNum, int cardIndex)
    {
        FlipoutUIPlayerLayout uIPlayerLayout = GetUIPlayerLayout(numPlayers, playerTableNum);
        Vector3 pos = uIPlayerLayout.position;
        pos.x += uIPlayerLayout.objectOffsetX * cardIndex;
        uIPlayerLayout.position = pos;
        return uIPlayerLayout;
    }


    public void UpdateScoresDisplay()
    {
        for (int playerNum = 0; playerNum < GameStateClient.GetTotalPlayers(); playerNum++)
        {
            PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(playerNum);
            scoreText[playerNum].text = "Score: " + player.scorePile.Count.ToString();
        }
    }

    private void CalculateDeckPosition(int numPlayers)
    {
        FlipoutUIPlayerLayout[] playerLayouts = GetUIPlayerLayouts(numPlayers);
        deckRotationZ = 0f;
        deckScale = 1f;
        // Deck X position for most cases; adjusting Y as needed
        //float deckPosXOffset = 0f;
        if (playerLayouts.Length < 3)   // 2 players
        {
            // Adjust deck position for 2 players
            deckPosition = new Vector3(0f, 0f, 0f);
            deckRotationZ = 90f;
            deckScale = 0.85f;
        }
        else    // 3 - 5 players
        {
            // placing deck to right, either in middle or between 2 & 3 players (index 1 & 2)

            int playerAtMidIndex = playerLayouts.Length / 2;
            deckPosition = playerLayouts[playerAtMidIndex].position;
            deckPosition.x += playerLayouts[playerAtMidIndex].objectOffsetX * 6 + playerLayouts[playerAtMidIndex].objectOffsetX * 0.15f;
            deckScale = playerLayouts[playerAtMidIndex].scale * 1.25f;
            if (playerLayouts.Length == 4)  // adjust Y between players 2 & 3
            {
                // average Y of two middle players
                Vector3 pos1 = playerLayouts[playerAtMidIndex - 1].position;
                Vector3 pos2 = playerLayouts[playerAtMidIndex].position;
                deckPosition.y = (pos1.y + pos2.y) / 2f;
            }
        }
    }

    private void AddPlayerText(int numPlayers, int playerTableNum)
    {
        if (canvasTextParentGO == null)
        {
            canvasTextParentGO = new GameObject("CanvasTextParent");
            canvasTextParentGO.transform.SetParent(canvas.transform, false);
        }
        FlipoutUIPlayerLayout uIPlayerLayout = GetUIPlayerLayout(numPlayers, playerTableNum);
        Vector3 position = uIPlayerLayout.position;
        Vector3 pos = position;
        pos.x += uIPlayerLayout.scorePileOffsetX;
        playerTextGO[playerTableNum] = new GameObject($"Player{playerTableNum}_Name", typeof(RectTransform));   //, typeof(RectTransform));
        scoreKeeperGO[playerTableNum] = new GameObject($"Player{playerTableNum}_Score", typeof(RectTransform));   //, typeof(RectTransform));
        // IMPORTANT: false keeps local UI coordinates correct
        //scoreKeeperGO[playerTableNum].transform.SetParent(canvas.transform, false); //, false);
        playerTextGO[playerTableNum].transform.SetParent(canvasTextParentGO.transform, false); //, false);
        scoreKeeperGO[playerTableNum].transform.SetParent(canvasTextParentGO.transform, false); //, false);
        RectTransform rtPlayer = playerTextGO[playerTableNum].GetComponent<RectTransform>();
        RectTransform rt = scoreKeeperGO[playerTableNum].GetComponent<RectTransform>();
        // Use anchoredPosition for UI placement
        //rt.anchoredPosition = playerScorePilePositions[i];\

        rtPlayer.sizeDelta = new Vector2(250, 50);
        rt.sizeDelta = new Vector2(200, 50);

        pos *= canvasPPU; //ppu
        pos.x -= 50;
        rt.anchoredPosition = pos;    // playerScoreTextPositions[playerTableNum];

        pos.y += 60;
        Debug.Log("Player Score Text Position: " + pos);    // playerScoreTextPositions[playerTableNum]);
        rtPlayer.anchoredPosition = pos;    // playerScoreTextPositions[playerTableNum];

        //rt.localScale = Vector3.one;
        //Vector3 pos = playerScorePilePositions[i];
        //Vector3 pos = playerScoreTextPositions[playerTableNum];
        //pos.z = -0.5f;
        //scoreKeeperGO[i].transform.localPosition = pos;
        //scoreKeeperGO[i].transform.localScale = Vector3.one;
        playerTextGO[playerTableNum].layer = LayerMask.NameToLayer("UI");
        scoreKeeperGO[playerTableNum].layer = LayerMask.NameToLayer("UI");
        
        playerText[playerTableNum] = playerTextGO[playerTableNum].AddComponent<TextMeshProUGUI>();
        scoreText[playerTableNum] = scoreKeeperGO[playerTableNum].AddComponent<TextMeshProUGUI>();
        //scoreText[i].GetComponent<Renderer>().sortingLayerName = "UI";
        //scoreText[i].GetComponent<Renderer>().sortingOrder = 150; // Optional: set render order

        playerText[playerTableNum].text = "Player Name";
        playerText[playerTableNum].fontSize = 32;
        playerText[playerTableNum].alignment = TextAlignmentOptions.Left;
        playerText[playerTableNum].color = Color.darkBlue;

        scoreText[playerTableNum].text = "Score: 00";
        scoreText[playerTableNum].fontSize = 32;
        scoreText[playerTableNum].alignment = TextAlignmentOptions.Center;
        scoreText[playerTableNum].color = Color.black;
    }

    public void AddPlayersText(int numPlayers, int playerTableNum)
    {
        if (canvasTextParentGO == null)
        {
            canvasTextParentGO = new GameObject("CanvasTextParent");
            canvasTextParentGO.transform.SetParent(canvas.transform, false);
        }
        FlipoutUIPlayerLayout[] uIPlayerLayout = GetUIPlayerLayouts(numPlayers);
        for (int playerNum = 0; playerNum < numPlayers; playerNum++)
        {
            
            Vector3 position = uIPlayerLayout[playerNum].position;
            Vector3 pos = position;
            pos.x += uIPlayerLayout[playerNum].scorePileOffsetX;
            playerTextGO[playerNum] = new GameObject($"Player{playerNum}_Name", typeof(RectTransform));   //, typeof(RectTransform));
            scoreKeeperGO[playerNum] = new GameObject($"Player{playerNum}_Score", typeof(RectTransform));   //, typeof(RectTransform));
            // IMPORTANT: false keeps local UI coordinates correct
            //scoreKeeperGO[playerTableNum].transform.SetParent(canvas.transform, false); //, false);
            playerTextGO[playerNum].transform.SetParent(canvasTextParentGO.transform, false); //, false);
            scoreKeeperGO[playerNum].transform.SetParent(canvasTextParentGO.transform, false); //, false);
            RectTransform rtPlayer = playerTextGO[playerNum].GetComponent<RectTransform>();
            RectTransform rt = scoreKeeperGO[playerNum].GetComponent<RectTransform>();
            // Use anchoredPosition for UI placement
            //rt.anchoredPosition = playerScorePilePositions[i];\

            rtPlayer.sizeDelta = new Vector2(250, 50);
            rt.sizeDelta = new Vector2(200, 50);

            pos *= 100; //ppu
            pos.x -= 50;
            rt.anchoredPosition = pos;    // playerScoreTextPositions[playerTableNum];

            pos.y += 60;
            Debug.Log("Player Score Text Position: " + pos);    // playerScoreTextPositions[playerTableNum]);
            rtPlayer.anchoredPosition = pos;    // playerScoreTextPositions[playerTableNum];

            //rt.localScale = Vector3.one;
            //Vector3 pos = playerScorePilePositions[i];
            //Vector3 pos = playerScoreTextPositions[playerTableNum];
            //pos.z = -0.5f;
            //scoreKeeperGO[i].transform.localPosition = pos;
            //scoreKeeperGO[i].transform.localScale = Vector3.one;
            playerTextGO[playerNum].layer = LayerMask.NameToLayer("UI");
            scoreKeeperGO[playerNum].layer = LayerMask.NameToLayer("UI");
            
            playerText[playerNum] = playerTextGO[playerNum].AddComponent<TextMeshProUGUI>();
            scoreText[playerNum] = scoreKeeperGO[playerNum].AddComponent<TextMeshProUGUI>();
            //scoreText[i].GetComponent<Renderer>().sortingLayerName = "UI";
            //scoreText[i].GetComponent<Renderer>().sortingOrder = 150; // Optional: set render order

            playerText[playerNum].text = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(playerNum).playerName;
            playerText[playerNum].fontSize = 32;
            playerText[playerNum].alignment = TextAlignmentOptions.Left;
            playerText[playerNum].color = Color.darkBlue;

            scoreText[playerNum].text = "Score: 00";
            scoreText[playerNum].fontSize = 32;
            scoreText[playerNum].alignment = TextAlignmentOptions.Center;
            scoreText[playerNum].color = Color.black;
        }
        // 'Highlight' current player
        playerText[playerTableNum].color = Color.cyan;
        playerText[playerTableNum].fontStyle = FontStyles.Bold;
    }
#endregion UI-Layout


#region TRASH
/*
    private void xUpdateScoresDisplayx(int numPlayers)
    {
        for (int i = 0; i < numPlayers; i++)
        {
            scoreText[i].text = $"Score: {Random.Range(0, 50)}";
        }
    }

    private void DealCard(int numPlayers, int playerTableNum, int cardIndex, CardColor color)
    {
        // Logic to deal a card to the specified player
        Debug.Log($"Dealing card {cardIndex} to player {playerTableNum}");

        FlipoutUIPlayerLayout uIPlayerLayout = GetUIPlayerLayoutAtCardIdx(numPlayers, playerTableNum, cardIndex);

        CardObject card = InstantiateCardObject(color, uIPlayerLayout.position, uIPlayerLayout.rotationZ, uIPlayerLayout.scale);
        card.SetSortingOrder(20);
    }

    private void DrawScorePile(int numPlayers, int playerTableNum)
    {
        // Logic to draw score pile for the specified player
        Debug.Log($"Drawing score pile for player {playerTableNum}");

        FlipoutUIPlayerLayout uIPlayerLayout = GetUIPlayerLayout(numPlayers, playerTableNum);
        Vector3 pos = uIPlayerLayout.position;
        pos.x += uIPlayerLayout.scorePileOffsetX;

        CardObject card = InstantiateCardObject(CardColor.invalid, pos, uIPlayerLayout.rotationZ, uIPlayerLayout.scale * scorePileScaleMultiplier);
        card.SetSortingOrder(10);
    }

    public void UpdateLayout()
    {
        int numPlayers = 3; // Example: set number of players here


        //Debug.Log("Button Clicked!");
        if (cardsParentGO != null)
        {
            GameObject.DestroyImmediate(cardsParentGO);
            cardsParentGO = null;
        }
        if (canvasTextParentGO != null)
        {
            GameObject.DestroyImmediate(canvasTextParentGO);
            canvasTextParentGO = new GameObject("CanvasTextParent");
            canvasTextParentGO.transform.SetParent(canvas.transform, false);
        }

        FlipoutUIPlayerLayout[] playerLayouts;

        switch (numPlayers)
        {
            case 2:
                playerLayouts = playerLayoutFor2;
                break;
            case 3:
                playerLayouts = playerLayoutFor3;
                break;
            case 4:
                playerLayouts = playerLayoutFor4;
                break;
            case 5:
                playerLayouts = playerLayoutFor5;
                break;
            default:
                Debug.LogWarning("Invalid number of players: " + numPlayers);
                return;
        }

        for (int playerTableNum = 0; playerTableNum < playerLayouts.Length; playerTableNum++)
        {
            FlipoutUIPlayerLayout uIPlayerLayout = playerLayouts[playerTableNum];
            Vector3 pos = uIPlayerLayout.position;
            float rotZ = uIPlayerLayout.rotationZ;
            float scale = uIPlayerLayout.scale;

            CardColor color = CardColor.invalid;
            color = Random.Range(0, 5) switch
            {
                0 => CardColor.red,
                1 => CardColor.blue,
                2 => CardColor.green,
                3 => CardColor.yellow,
                4 => CardColor.purple,
                _ => CardColor.invalid,
            };

            for (int i = 0; i < 6; i++)
            {
                InstantiateCardObject(color, pos, rotZ, scale);
                pos.x += uIPlayerLayout.objectOffsetX;
            }

            // TEST: Randomly deal one card to each player
            DealCard(numPlayers, playerTableNum, 4, CardColor.red);


            DrawScorePile(numPlayers, playerTableNum);
            // reset pos
            //pos = uIPlayerLayout.position;
            // move to score pile position
            //pos.x += uIPlayerLayout.scorePileOffsetX;
            //InstantiateCardObject(color, pos, rotZ, scale * 0.66f);

            AddPlayerText(numPlayers, playerTableNum);
        }

        xUpdateScoresDisplayx(numPlayers);

        CalculateDeckPosition(numPlayers);

        InstantiateCardObject(CardColor.invalid, deckPosition, deckRotationZ, deckScale);

        // 'Highlight' current player
        playerText[0].color = Color.cyan;
        playerText[0].fontStyle = FontStyles.Bold;
    }

   private CardObject InstantiateCardObject(CardColor color, Vector3 position, float rotationZ, float scale)
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

        cardObject.SetLocalPosition(position);
        cardObject.SetLocalScale(new Vector3(scale, scale, 1) );
        cardObject.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, rotationZ));


        

        CardPODClient cardPOD = new CardPODClient { color = color };

        cardObject.SetCardPOD(cardPOD);

        //cardsInPlay.Add(cardObject);

        return cardObject;
    }
    public void SetupPlayerUI(int numPlayers, string[] playerNames)
    {
        for (int i = 0; i < numPlayers; i++)
        {
            scoreKeeperGO[i] = new GameObject($"Player{i}_Score", typeof(RectTransform));   //, typeof(RectTransform));
            // IMPORTANT: false keeps local UI coordinates correct
            scoreKeeperGO[i].transform.SetParent(canvas.transform, false); //, false);
            //scoreKeeperGO[i].transform.SetParent(canvasTextParentGO.transform, false); //, false);
            RectTransform rt = scoreKeeperGO[i].GetComponent<RectTransform>();
            // Use anchoredPosition for UI placement
            //rt.anchoredPosition = playerScorePilePositions[i];
            Debug.Log("Player Score Text Position: " + playerScoreTextPositions[i]);
            rt.anchoredPosition = playerScoreTextPositions[i];
            rt.sizeDelta = new Vector2(200, 50);
            //rt.localScale = Vector3.one;
            //Vector3 pos = playerScorePilePositions[i];
            Vector3 pos = playerScoreTextPositions[i];
            //pos.z = -0.5f;
            //scoreKeeperGO[i].transform.localPosition = pos;
            //scoreKeeperGO[i].transform.localScale = Vector3.one;
            scoreKeeperGO[i].layer = LayerMask.NameToLayer("UI");
            scoreText[i] = scoreKeeperGO[i].AddComponent<TextMeshProUGUI>();
            //scoreText[i].GetComponent<Renderer>().sortingLayerName = "UI";
            //scoreText[i].GetComponent<Renderer>().sortingOrder = 150; // Optional: set render order
            scoreText[i].text = "Score: 0";
            scoreText[i].fontSize = 32;
            scoreText[i].alignment = TextAlignmentOptions.Center;
            scoreText[i].color = Color.white;
        }
        //UpdateScoresDisplay();
    }
*/
#endregion TRASH
}
