using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Attribs: Original code from Chris, modified and added to by Daniel C

public class UIManager : MonoBehaviour
{
    public int animationsInProgress = 0;
    //public static UIManager Instance;

    private CardObject selectedCard = null;
    private GameObject Outline = null;

    [Header("Outline Prefab")]
    public GameObject outlinePrefab;   // Assign in Inspector
    [Header("Outline Settings")]
    public float scaleMultiplier = 1.10f;
    public Vector3 behindOffset = new Vector3(0f, 0f, 1f);
    [Tooltip("Sprite used for the white silhouette")]
    public Sprite whiteSprite;

    [Header("selected Settings")]
    public float selectedScaleMultiplier = 1.25f;
    public Color darkenColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color normalColor = Color.white;

    [Header("UI Position References")]
    [Tooltip("Assign one element per player. Each element contains hand slots and score pile transform.")]
    public UIHolder[] playerHolders;

    [Tooltip("Location of the draw pile in the scene.")]
    public Transform drawPileTransform;

    [Header("Card Spacing")]
    public float cardSpacing = 2.5f;

    [Header("Movement Settings")]
    public float moveDuration = 0.35f;               // This affects the movement tween speed
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Action Menu")]
    public GameObject actionMenuPrefab;
    public Vector3 actionMenuOffset = new Vector3(3f, 0f, 0f);

    public GameObject leftactionMenuPrefab;
    public Vector3 leftactionMenuOffset = new Vector3(-3f, 0f, 0f);
    private GameObject activeActionMenu;
    // sorting order control
    public int frontOffset = 1000;
    public int outlineRelativeOffset = 999;

    private Dictionary<CardObject, int> originalSortingOrders = new Dictionary<CardObject, int>();
    private Dictionary<CardObject, string> originalSortingLayerNames = new Dictionary<CardObject, string>();
    private Dictionary<CardObject, Vector3> originalScale = new Dictionary<CardObject, Vector3>();

#region UI_FROM_GM
   //Select a canvas in inspector to be the parent for almost all UI Instances (ScoreCount, Turn Indicator, Avatars, etc.)
    public GameObject UICanvas;
    public bool IsInHighlightMode = false;

    private GameObject[] scoreKeeperGO = new GameObject[5];
    [SerializeField] private TextMeshProUGUI[] scoreText = new TextMeshProUGUI[5];
    //GameObject playersParentGO = null;

    [SerializeField] public List<CardObject> cardsHighlighted = new List<CardObject>();

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
    private Vector3[] playerScoreTextPositions = new Vector3[5]
    {
        new(-864, -320, 0),    // Player 1 - Bottom left
        new(-864, 320, 0),     // Player 2 - Top left
        new(-9, -1, 0),    // Player 3 - Left center back        
        new(9, -1, 0),     // Player 4 - Right center back
        new(0, 5, 0)      // Player 5 - Center top (?!!)
    };
#endregion
    private TurnAction pendingAction;
    private CardObject actionSourceCard;

    public AnimationManager animationManager;


   void Awake()
    {
        if (animationManager == null)
        {
            animationManager = gameObject.GetComponent<AnimationManager>();
            if (animationManager == null)
            {
                animationManager = gameObject.AddComponent<AnimationManager>();
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void OnEnable()
    {
        UISignals.OnCardActionRequested += HandleCardActionRequest;
        CardObject.OnHoverEnter += CreateOutline;
        CardObject.OnHoverExit += DestroyOutline;
        CardObject.onCardClicked += OnCardClicked;
    }

    private void OnDisable()
    {
        UISignals.OnCardActionRequested -= HandleCardActionRequest;
        CardObject.OnHoverEnter -= CreateOutline;
        CardObject.OnHoverExit -= DestroyOutline;
        CardObject.onCardClicked -= OnCardClicked;
    }

    void Update()
    {
        if (selectedCard == null || activeActionMenu == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!IsClickOnSelectedCardOrMenu())
            {
                ClearSelection();
            }
        }
    }

    public void SetupPlayerUI(int numPlayers, string[] playerNames)
    {
        for (int i = 0; i < numPlayers; i++)
        {
            scoreKeeperGO[i] = new GameObject($"Player{i}_Score", typeof(RectTransform));   //, typeof(RectTransform));
            // IMPORTANT: false keeps local UI coordinates correct
            scoreKeeperGO[i].transform.SetParent(UICanvas.transform, false); //, false);
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

    public void UpdateScoresDisplay()
    {
        for (int playerNum = 0; playerNum < GameStateClient.GetTotalPlayers(); playerNum++)
        {
            PlayerXClient player = GameStateClient.CurrentGameStateClient.GetPlayerByNumber(playerNum);
            scoreText[playerNum].text = "Score: " + player.scorePile.Count.ToString();
        }
    }


    void OnCardClicked(CardObject card)
    {
        if (card.cardPOD.state != CardState.playerHolder)
            return;

        if (animationManager.IsRunningAnimations())
            return;
        // If we're resolving an action, route to highlight logic
        if (IsInHighlightMode)
        {
            bool consumed = HandleHighlightedCardClicked(card);
            if (consumed)
                return;
        }
        ToggleSelectionExternal(card);
 
    }

    //Handles logic revolving around highlight mode and the cardsHighlighted list.
    bool HandleHighlightedCardClicked(CardObject card)
    {
        if (!IsInHighlightMode)
            return false;

        if (actionSourceCard != null && !cardsHighlighted.Contains(actionSourceCard))
            cardsHighlighted.Add(actionSourceCard);

        if (cardsHighlighted.Contains(card))
            return true; // click was consumed

        cardsHighlighted.Add(card);
        Debug.Log("Card highlighted: " + card.cardPOD.cardID);
        card.HighlightCardToggle();

        int required = GetRequiredHighlightCount(pendingAction);

        Debug.Log($"[HighlightMode] Action: {pendingAction} | Highlighted: {cardsHighlighted.Count}/{required}");

        if (cardsHighlighted.Count >= required)
        {
            ExecutePendingAction();
            return true;
        }

        return true; // click used for highlighting
    }

    public void HandleCardActionRequest(CardActionRequest request)
    {
        Debug.Log($"Action requested: {request.actionType} from {request.sourceCard.name}");

        pendingAction = request.actionType;
        actionSourceCard = request.sourceCard;

        HideActionMenu();
        // 2+ cards required?
        if (pendingAction == TurnAction.Switch || pendingAction == TurnAction.Swap1 || pendingAction == TurnAction.Swap2)
        {
            EnterHighlightMode();          
        }
        else
        {
            RestoreScale();
            CardObject card = request.sourceCard;
            switch (request.actionType)
            {                
                case TurnAction.Flip:
                {                    
                    //CardColor newColor = FakeFlipColor(card.cardPOD.color);

                    //!!
                    //Debug.LogWarning("This shouldn't be done this way... check into..");
                    GameManager.Instance.serverDispatch.FlipCard(GameStateClient.CurrentGameStateClient.GetActivePlayer().playerId, card.cardPOD.cardID);
                    break;
                }

                case TurnAction.Score:
                {
                    int[] adjacentCardIndices = GameStateClient.GetAdjacentColorsIndicesBasedOnCardId(card.cardPOD.cardID);
                    if (adjacentCardIndices.Length < 4)
                    {
                        Debug.Log("Need to highlight a card that has at least 4 adjacent same-color cards to score!");
                        //return;
                    }
                    else
                    {
                        // This will fail if current player doesn't own the highlighted card
                        GameManager.Instance.serverDispatch.ScoreCards(GameStateClient.GetCurrentPlayerId(),
                            card.cardPOD.cardID);
                        GameManager.Instance.flipOutGame.ClearHighlightedCards();
                    }
                    // Optional if Score is 1-card
                    break;
                }
                case TurnAction.Swipe:
                {
                    //TurnAction.Swipe  // score a set of 4 to 6 adjacent same-color cards from another player's hand
                    int[] adjacentCardIndices = GameStateClient.GetAdjacentColorsIndicesBasedOnCardId(card.cardPOD.cardID);
                    if (adjacentCardIndices.Length < 4)
                    {
                        Debug.Log("Need to highlight a card that has at least 4 adjacent same-color cards to score!");
                        //return;
                    }
                    else
                    {
                        // This will fail if current player owns the highlighted card
                        GameManager.Instance.serverDispatch.SwipeCards(GameStateClient.GetCurrentPlayerId(),
                            card.cardPOD.cardID);
                        GameManager.Instance.flipOutGame.ClearHighlightedCards();

                    }
                    break;
                }
            }
        }
    
    }


    int GetRequiredHighlightCount(TurnAction action)
    {
        switch (action)
        {
            case TurnAction.Flip:   return 1;
            case TurnAction.Switch: return 2;
            case TurnAction.Swap1:  return 2;
            case TurnAction.Swap2:  return 4;
            case TurnAction.Score:  return 1;
            case TurnAction.Swipe:  return 1;
            default: return 0;
        }
    }

    void EnterHighlightMode()
    {
        IsInHighlightMode = true;
        cardsHighlighted.Clear();

        // Always highlight the source card
        cardsHighlighted.Add(actionSourceCard);
        actionSourceCard.HighlightCardToggle();
    }

    CardColor FakeFlipColor(CardColor current)
    {
        return current switch
        {
            CardColor.red => CardColor.green,
            CardColor.green => CardColor.blue,
            CardColor.blue => CardColor.purple,
            CardColor.purple => CardColor.yellow,
            CardColor.yellow => CardColor.red,
            _ => CardColor.invalid
        };
    }

    void ExecutePendingAction()
    {
        Debug.Log($"Executing action: {pendingAction} on {cardsHighlighted.Count} highlighted cards.");
        switch (pendingAction)
        {
            case TurnAction.Flip:
            {
                CardObject card = cardsHighlighted[0];
                //CardColor newColor = FakeFlipColor(card.cardPOD.color);
                //!! 2nd flip?
                GameManager.Instance.serverDispatch.FlipCard(GameStateClient.CurrentGameStateClient.GetActivePlayer().playerId, card.cardPOD.cardID);
                break;
            }

            case TurnAction.Switch:
            {
                //!!
                //Debug.LogWarning("This shouldn't be done this way... check into..");
                Debug.Log("Cards highlighted at Switch: " + cardsHighlighted.Count);
                //TurnAction.Switch // switch 1 card with another of yours, or 1 of opponents with another of opponent's
                //if (cardsHighlighted.Count != 2)
                if (cardsHighlighted.Count != 2)
                {
                    Debug.Log("Need to highlight exactly 2 cards to switch (2 of yours or 2 of another player's)!");
                    break;
                }
                GameManager.Instance.serverDispatch.SwitchCards(
                    GameStateClient.CurrentGameStateClient.GetActivePlayer().playerId,
                    cardsHighlighted[0].cardPOD.cardID,
                    cardsHighlighted[1].cardPOD.cardID);
                GameManager.Instance.flipOutGame.ClearHighlightedCards();
                break;
            }
            
            case TurnAction.Swap1:
            {
                //TurnAction.Swap1 // swap 1 of your cards with another player's
                if (cardsHighlighted.Count != 2)
                {
                    Debug.Log("Need to highlight exactly 2 cards to swap (1 of yours and 1 of another player's)!");
                    break;
                }
                GameManager.Instance.serverDispatch.SwapCards1(
                    GameStateClient.CurrentGameStateClient.GetActivePlayer().playerId,
                    cardsHighlighted[0].cardPOD.cardID,
                    cardsHighlighted[1].cardPOD.cardID);

                GameManager.Instance.flipOutGame.ClearHighlightedCards();
                break;
            }

            case TurnAction.Swap2:
            {
                //TurnAction.Swap2 // swap 2 adjacent same-color cards of yours with another player's 2 adjacent same-color cards
                if (cardsHighlighted.Count != 4)
                {
                    Debug.Log("Need to highlight exactly 4 cards to swap (2 of yours and 2 of another player's)!");
                    break;
                }
                GameManager.Instance.serverDispatch.SwapCards2(
                    GameStateClient.CurrentGameStateClient.GetActivePlayer().playerId,
                    cardsHighlighted[0].cardPOD.cardID,
                    cardsHighlighted[1].cardPOD.cardID,
                    cardsHighlighted[2].cardPOD.cardID,
                    cardsHighlighted[3].cardPOD.cardID);
                GameManager.Instance.flipOutGame.ClearHighlightedCards();
                break;
            }

            case TurnAction.Score:
            {
                //TurnAction.Score // score a set of 4 to 6 adjacent same-color cards from your hand, redraw up to 6
                if (cardsHighlighted.Count != 1)
                {
                    Debug.Log("Need to highlight 1 card to score!");
                    break;
                }
                int[] adjacentCardIndices = GameStateClient.GetAdjacentColorsIndicesBasedOnCardId(cardsHighlighted[0].cardPOD.cardID);
                if (adjacentCardIndices.Length < 4)
                {
                    Debug.Log("Need to highlight a card that has at least 4 adjacent same-color cards to score!");
                    break;
                }
                else
                {
                    // This will fail if current player doesn't own the highlighted card
                    GameManager.Instance.serverDispatch.ScoreCards(GameStateClient.GetCurrentPlayerId(),
                        cardsHighlighted[0].cardPOD.cardID);
                    GameManager.Instance.flipOutGame.ClearHighlightedCards();
                }
                break;
            }

            case TurnAction.Swipe:
            {
                //TurnAction.Swipe  // score a set of 4 to 6 adjacent same-color cards from another player's hand
                if (cardsHighlighted.Count != 1)
                {
                    Debug.Log("Need to highlight 1 card to swipe score!");
                    break;
                }
                int[] adjacentCardIndices = GameStateClient.GetAdjacentColorsIndicesBasedOnCardId(cardsHighlighted[0].cardPOD.cardID);
                if (adjacentCardIndices.Length < 4)
                {
                    Debug.Log("Need to highlight a card that has at least 4 adjacent same-color cards to score!");
                    break;
                }
                else
                {
                    // This will fail if current player owns the highlighted card
                    GameManager.Instance.serverDispatch.SwipeCards(GameStateClient.GetCurrentPlayerId(),
                        cardsHighlighted[0].cardPOD.cardID);
                    GameManager.Instance.flipOutGame.ClearHighlightedCards();

                }
                break;
            }
        }
        ExitHighlightMode();
        
    }


    void ExitHighlightMode()
    {
        foreach (var card in cardsHighlighted)
            card.HighlightCardToggle();

        cardsHighlighted.Clear();
        IsInHighlightMode = false;
        pendingAction = default;
        actionSourceCard = null;
    }


    //This checks if the player clicked off a menu. If they did, the action menu closes.
    private bool IsClickOnSelectedCardOrMenu()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(mouseWorld.x, mouseWorld.y);

        // Check selected card
        Collider2D cardCol = selectedCard.GetComponent<Collider2D>();
        if (cardCol != null && cardCol.OverlapPoint(point))
            return true;

        // Check menu and its children
        Collider2D[] menuColliders = activeActionMenu.GetComponentsInChildren<Collider2D>();
        foreach (var col in menuColliders)
        {
            if (col.OverlapPoint(point))
                return true;
        }

        return false;
    }

    private void CreateOutline(CardObject card)
    {
        if (card == null) return;
        if (selectedCard != null) return;
        if (card.cardPOD.state != CardState.playerHolder) return;
        if (card.gameObject.tag == "invalid") return;
        if (animationManager.IsRunningAnimations()) return;

        SpriteRenderer originalSR = card.GetComponent<SpriteRenderer>();
        if (originalSR == null)
        {
            Debug.LogWarning("CreateOutline: card missing SpriteRenderer");
            return;
        }

        // Prevent duplicate outlines
        if (Outline != null)
            Destroy(Outline);

        // --- Instantiate outline prefab ---
        Outline = Instantiate(outlinePrefab);
        Outline.name = "Outline";
        Outline.tag = "Outline";

        // Parent it to card
        Outline.transform.SetParent(card.transform, true);

        // Position behind card
        Outline.transform.position = card.transform.position + behindOffset;

        // Scale slightly larger
        Outline.transform.localScale = card.transform.localScale * scaleMultiplier;



        // Configure sprite sorting
        SpriteRenderer outlineSR = Outline.GetComponent<SpriteRenderer>();
        if (outlineSR != null)
        {
            outlineSR.sortingLayerName = originalSR.sortingLayerName;
            outlineSR.sortingOrder = 999;
        }

        // Save original card sorting so we can restore later
        if (!originalSortingOrders.ContainsKey(card))
        {
            originalSortingOrders[card] = originalSR.sortingOrder;
            originalSortingLayerNames[card] = originalSR.sortingLayerName;
        }

        // Bring card to front
        originalSR.sortingOrder += frontOffset;
    }
    private void DestroyOutline(CardObject card)
    {
        if (card == null)
        {
            RestoreAllAndClear();
            return;
        }
        if (card.cardPOD.state != CardState.playerHolder) return;

        if (originalSortingOrders.TryGetValue(card, out int originalOrder) && (card.gameObject.tag != "selected"))
        {
            SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = originalOrder;
                if (originalSortingLayerNames.TryGetValue(card, out string originalLayer))
                    sr.sortingLayerName = originalLayer;
            }

            originalSortingOrders.Remove(card);
            originalSortingLayerNames.Remove(card);
        }

        if (Outline != null)
        {
            Destroy(Outline);
            Outline = null;
        }
    }

    private void RestoreAllAndClear()
    {
        foreach (var kv in originalSortingOrders)
        {
            var card = kv.Key;
            if (card == selectedCard)
            continue;
            var originalOrder = kv.Value;
            if (card != null)
            {
                var sr = card.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = originalOrder;
            }
        }

        originalSortingOrders.Clear();
        originalSortingLayerNames.Clear();

        if (Outline != null)
        {
            Destroy(Outline);
            Outline = null;
        }
    }

    private void ToggleSelection(CardObject card)
    {
        if (card == null) return;

        // Clicking the currently selected card unselects it
        if (selectedCard == card)
        {
            ClearSelection();
            return;
        }
    
    if(IsInHighlightMode != true)
        SetselectedCard(card); // Selecting a new card
    }

    public void ToggleSelectionExternal(CardObject card)
    {
        ToggleSelection(card);
        Debug.Log("Selection Toggle Activated");
    }


    private void SetselectedCard(CardObject card)
    {
        if (card.gameObject.tag == "invalid") return;
        selectedCard = card;
        if (!originalScale.ContainsKey(card))
            {
                originalScale[card] = card.transform.localScale;
            }    
        //Scale selected card up
        SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
 
            card.transform.localScale = Vector3.one * selectedScaleMultiplier;

            //Bring to front
            sr.sortingOrder = 5000; 
        }

        ShowActionMenu(card);

        //Apply tagging rules
        ApplySelectionTags(card);
        //Darken every OTHER card
        DarkenAllExcept(card);

        //Remove hover copy (hovering makes no sense while selected)
        RestoreAllAndClear();

    }

    public void ApplySelectionTags(CardObject selectedCard)
    {
        if (selectedCard == null)
            return;

        int selectedOwnerId = selectedCard.cardPOD.ownerPlayerID;

        GameStateClient gameState = GameStateClient.CurrentGameStateClient;
        if (gameState == null)
            return;

        int totalPlayers = GameStateClient.GetTotalPlayers();

        for (int playerNum = 0; playerNum < totalPlayers; playerNum++)
        {
            PlayerXClient player = gameState.GetPlayerByNumber(playerNum);
            if (player == null || player.hand == null)
                continue;

            bool isselectedPlayersHand = player.playerId == selectedOwnerId;

            for (int i = 0; i < player.hand.Length; i++)
            {
                CardPODClient pod = player.hand[i];
                if (pod == null || pod.cardObject == null)
                    continue;

                CardObject card = pod.cardObject;

                if (isselectedPlayersHand)
                {
                    // Same player's hand
                    if (card == selectedCard)
                    {
                        card.gameObject.tag = "selected";
                    }
                    else
                    {
                        // Do NOT override selected
                        if (card.gameObject.tag != "selected")
                            card.gameObject.tag = "invalid";
                    }
                }
                else
                {
                    // Other players' cards
                    card.gameObject.tag = "valid";
                }
            }
        }
    }

    private void DarkenAllExcept(CardObject keepLit)
    {
        var allCards = GameObject.FindGameObjectsWithTag("invalid");

        foreach (var obj in allCards)
        {
            var c = obj.GetComponent<CardObject>();
            if (c == null) continue;

            SpriteRenderer sr = c.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (c == keepLit)
                sr.color = normalColor;   // selected stays normal
            else
                sr.color = darkenColor;   // others darken
        }
    }

    private void RestoreScale()
    {
        if (selectedCard == null)
            return;

        if (originalScale.TryGetValue(selectedCard, out Vector3 original))
        {
            selectedCard.transform.localScale = original;
        }
    }

    private void ClearSelection()
    {
        if (selectedCard != null)
        {
            RestoreScale();

            SpriteRenderer sr = selectedCard.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = 0;
        }

        selectedCard = null;

        HideActionMenu();
        RestoreAllCardColors();
        ResetAllCardTags();
        
    if (GameManager.Instance != null)
        {
            GameManager.Instance.flipOutGame.ClearHighlightedCards();
            Debug.Log("Cleared Highlighted Cards");
        }
    }

    private void RestoreAllCardColors()
    {
        var allCards = GameObject.FindGameObjectsWithTag("invalid");

        foreach (var obj in allCards)
        {
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = normalColor;
        }

    }

    private void ResetAllCardTags()
    {
        var invalidCards = GameObject.FindGameObjectsWithTag("invalid");
        var selectedCards = GameObject.FindGameObjectsWithTag("selected");
        foreach (var obj in invalidCards)
            obj.tag = "valid";  // Default tag
        foreach (var obj in selectedCards)
            obj.tag = "valid";  // Default tag
    }

    public void MoveCard(CardObject card, Transform from, Transform toHolder, int slotIndex = -1)
    {
        if (card == null || toHolder == null)
        {
            Debug.LogWarning("MoveCard called with missing references!");
            return;
        }

        // Default target position is the holder's position
        Vector3 targetPos = toHolder.position;

        if (slotIndex >= 0)
        {
            // Look through each UIPlayerHolder
            foreach (var holder in playerHolders)
            {
                // Check hand slots
                if (holder.slots != null && slotIndex < holder.slots.Length && holder.slots[slotIndex] == toHolder)
                {
                    targetPos = holder.slots[slotIndex].position;
                    break;
                }

                // Check player hand anchors
                if (holder.playerHandHolders != null && slotIndex < holder.playerHandHolders.Length && holder.playerHandHolders[slotIndex] == toHolder)
                {
                    targetPos = holder.playerHandHolders[slotIndex].position;
                    break;
                }

                // Check score slots
                if (holder.playerScoreHolders != null && slotIndex < holder.playerScoreHolders.Length && holder.playerScoreHolders[slotIndex] == toHolder)
                {
                    targetPos = holder.playerScoreHolders[slotIndex].position;
                    break;
                }
            }

            // Check draw pile
            foreach (var holder in playerHolders)
            {
                if (holder.drawPileHolder != null && holder.drawPileHolder == toHolder)
                {
                    targetPos = holder.drawPileHolder.position;
                    break;
                }
            }
        }

        // Start the movement animation
        StartCoroutine(AnimateCardMovement(card, targetPos));
    }

    public IEnumerator AnimateCardMovement(CardObject card, Vector3 targetPos)
    {
        if (card == null)
            yield break;

        animationsInProgress++;

        Debug.Log($"Animating movement of card {card.cardPOD.cardID} to {targetPos}");

        Transform t = card.transform;
        Vector3 start = t.position;
        float time = 0f;

        while (time < moveDuration)
        {
            float p = time / moveDuration;
            float curve = moveCurve.Evaluate(p);

            t.position = Vector3.Lerp(start, targetPos, curve);

            time += Time.deltaTime;
            yield return null; // correct Unity coroutine yield
        }

        t.position = targetPos; // ensure final exact position
        animationsInProgress--;
        Debug.Log($"Animation of card {card.cardPOD.cardID} completed, final position: {targetPos}");
    }

    public IEnumerator AnimateCardMovementAndScale(CardObject card, Vector3 targetPos, Vector3 targetScale)
    {
        if (card == null)
            yield break;

        animationsInProgress++;
        Debug.Log($"Animating movement and scale of card {card.cardPOD.cardID} to {targetPos} with scale {targetScale}");
        Transform t = card.transform;
        Vector3 startPos = t.position;
        Vector3 startScale = t.localScale;
        float time = 0f;

        while (time < moveDuration)
        {
            float p = time / moveDuration;
            float curve = moveCurve.Evaluate(p);

            t.position = Vector3.Lerp(startPos, targetPos, curve);
            t.localScale = Vector3.Lerp(startScale, targetScale, curve);

            time += Time.deltaTime;
            yield return null; // correct Unity coroutine yield
        }

        t.position = targetPos; // ensure final exact position
        t.localScale = targetScale; // ensure final exact scale
        animationsInProgress--;
        Debug.Log($"Animation of card {card.cardPOD.cardID} completed, final position: {targetPos}, scale: {targetScale}");
    }

    public IEnumerator AnimateCardMovementScaleAndRotation(CardObject card, Vector3 targetPos, Vector3 targetScale, Quaternion targetRot)
    {
        if (card == null)
            yield break;

        animationsInProgress++;
        Debug.Log($"Animating movement, scale, and rotation of card {card.cardPOD.cardID} to {targetPos} with scale {targetScale} and rotation {targetRot.eulerAngles}");
        Transform t = card.transform;
        Vector3 startPos = t.position;
        Vector3 startScale = t.localScale;
        Quaternion startRot = t.rotation;
        float time = 0f;

        while (time < moveDuration)
        {
            float p = time / moveDuration;
            float curve = moveCurve.Evaluate(p);

            t.position = Vector3.Lerp(startPos, targetPos, curve);
            t.localScale = Vector3.Lerp(startScale, targetScale, curve);
            t.rotation = Quaternion.Slerp(startRot, targetRot, curve);

            time += Time.deltaTime;
            yield return null; // correct Unity coroutine yield
        }

        t.position = targetPos; // ensure final exact position
        t.localScale = targetScale; // ensure final exact scale
        t.rotation = targetRot; // ensure final exact rotation
        animationsInProgress--;
        Debug.Log($"Animation of card {card.cardPOD.cardID} completed, final position: {targetPos}, scale: {targetScale}, rotation: {targetRot.eulerAngles}");
    }

    public IEnumerator AnimateFlip(CardObject card, CardColor dest)
    {
        if (card == null)
            yield break;

        animationsInProgress++;
        Debug.Log($"Animating flip of card {card.cardPOD.cardID} to color {dest}");
        float halfDuration = moveDuration / 2f;
        float time = 0f;

        Vector3 originalScale = card.transform.localScale;
        Vector3 squishedScale = new Vector3(0f, originalScale.y, originalScale.z);

        // First half: scale X to 0
        while (time < halfDuration)
        {
            float p = time / halfDuration;
            float curve = moveCurve.Evaluate(p);

            card.transform.localScale = Vector3.Lerp(originalScale, squishedScale, curve);

            time += Time.deltaTime;
            yield return null;
        }

        card.transform.localScale = squishedScale;

        // Change card color at midpoint
        card.UpdateColor(dest);

        // Second half: scale X back to original
        time = 0f;
        while (time < halfDuration)
        {
            float p = time / halfDuration;
            float curve = moveCurve.Evaluate(p);

            card.transform.localScale = Vector3.Lerp(squishedScale, originalScale, curve);

            time += Time.deltaTime;
            yield return null;
        }

        card.transform.localScale = originalScale; // ensure final exact scale

        animationsInProgress--;
        Debug.Log($"Flip animation of card {card.cardPOD.cardID} completed, final color: {dest}");
    }

    public Transform[] GenerateHandSlots(Transform parent, Vector3 localOffset, int numSlots = 6)
    {
        Transform[] slots = new Transform[numSlots];

        
        float half = (numSlots - 1) / 2f;

        for (int i = 0; i < numSlots; i++)
        {
            GameObject slot = new GameObject($"HandSlot_{i}");
            slot.transform.SetParent(parent);

            // Centered offset: positions go from -half to +half
            float xOffset = (i - half) * cardSpacing;

            slot.transform.localPosition = new Vector3(xOffset, 0f, -0.01f * i) + localOffset;

            slots[i] = slot.transform;
        }

        return slots;
    }

    private void ShowActionMenu(CardObject card)
    {
        PlayerXClient player =
            GameStateClient.CurrentGameStateClient
                .GetPlayerByID(card.cardPOD.ownerPlayerID);

        if (player == null) return;
        HideActionMenu();
        int slotIndex = player.GetIndexOfCardByID(card.cardPOD.cardID);

        if (slotIndex == 4 || slotIndex == 5)
        {
        activeActionMenu = Instantiate(leftactionMenuPrefab);
        activeActionMenu.name = "CardActionMenu";

        // Parent to card so it follows movement
        activeActionMenu.transform.SetParent(card.transform, false);

        // Position it to the RIGHT of the card
        activeActionMenu.transform.localPosition = leftactionMenuOffset;

        // Sorting: above outline, below card
        SpriteRenderer[] renderers = activeActionMenu.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.sortingOrder = card.GetComponent<SpriteRenderer>().sortingOrder - 1;
        }
        CardActionMenu menu = activeActionMenu.GetComponent<CardActionMenu>();
        menu.Initialize(card);
        if (activeActionMenu == null)
        {
            Debug.LogWarning("DID NOT CREATE MENU");
        }
        else
        {
            Debug.Log("Menu Created!");
        }
        }
        else
        {        
        activeActionMenu = Instantiate(actionMenuPrefab);
        activeActionMenu.name = "CardActionMenu";

        // Parent to card so it follows movement
        activeActionMenu.transform.SetParent(card.transform, false);

        // Position it to the RIGHT of the card
        activeActionMenu.transform.localPosition = actionMenuOffset;

        // Sorting: above outline, below card
        SpriteRenderer[] renderers = activeActionMenu.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.sortingOrder = card.GetComponent<SpriteRenderer>().sortingOrder - 1;
        }
        CardActionMenu menu = activeActionMenu.GetComponent<CardActionMenu>();
        menu.Initialize(card);
        if (activeActionMenu == null)
        {
            Debug.LogWarning("DID NOT CREATE MENU");
        }
        else
        {
            Debug.Log("Menu Created!");
        }
        }
    }

    public void HideActionMenu()
    {
        if (activeActionMenu != null)
        {
            Destroy(activeActionMenu);
            activeActionMenu = null;
        }
    }
}


[System.Serializable]
public class UIHolder
{
    [Header("Root of this player's UI hierarchy")]
    public Transform holderRoot;

    [Header("Hand slots")]
    public Transform[] slots;

    [Header("Player Anchors")]
    public Transform[] playerHandHolders;
    public Transform[] playerScoreHolders;

    [Header("Draw Pile Anchor")]
    public Transform drawPileHolder;
}