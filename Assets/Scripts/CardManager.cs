using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CardManager : MonoBehaviour
{
    // Each color appears 6 times (including both sides)
    private CardPOD[] deckCardRange = new CardPOD[15]
    {
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.red },
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.green },
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.blue },
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.purple },
        new() { cardSideAColor = cardColor.red, cardSideBColor = cardColor.yellow },

        new() { cardSideAColor = cardColor.green, cardSideBColor = cardColor.green },
        new() { cardSideAColor = cardColor.green, cardSideBColor = cardColor.blue },
        new() { cardSideAColor = cardColor.green, cardSideBColor = cardColor.purple },
        new() { cardSideAColor = cardColor.green, cardSideBColor = cardColor.yellow },

        new() { cardSideAColor = cardColor.blue, cardSideBColor =  cardColor.blue },
        new() { cardSideAColor = cardColor.blue, cardSideBColor =  cardColor.purple },
        new() { cardSideAColor = cardColor.blue, cardSideBColor =  cardColor.yellow },

        new() { cardSideAColor = cardColor.purple, cardSideBColor =  cardColor.purple },
        new() { cardSideAColor = cardColor.purple, cardSideBColor =  cardColor.yellow },

        new() { cardSideAColor = cardColor.yellow, cardSideBColor =  cardColor.yellow }
    };
    [SerializeField] private CardPOD[] deckPure = new CardPOD[90];
    [SerializeField] private List<CardPOD> deck = null;

    GameObject deckParentGO;
    //[SerializeField] private List<CardObject> deckCardObjects = null;
    private GameObject cardPrefab;

    Vector3 deckDefaultPosition = new Vector3(-6, -3, 0);
    private Vector3 deckOffscreenPosition = new Vector3(-1000, -1000, 0);

    Vector3 moveToPosition = new Vector3(1, 1, 0);
    private int cardsMoved = 0;

    bool cardsShowing = false;

    void Awake()
    {
        // Set up a pure, in-order deck first
        // 6 sets of deckCardRange (15) = 90 total
        int index = 0;
        for (int i = 0; i < deckCardRange.Length; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                deckPure[i * 6 + j] = deckCardRange[index];
                index++;
                if (index == deckCardRange.Length)
                    index = 0;
            }
        }
        InitAndShuffleDeck();

        cardPrefab = Resources.Load<GameObject>("Prefabs/CardPF");

        deckParentGO = new GameObject("_Cards");
        for (int i = 0; i < deck.Count; i++)
        {
            CardPOD card = deck[i];
            GameObject cardGO = Instantiate(cardPrefab, deckOffscreenPosition, Quaternion.identity, deckParentGO.transform);
            //cardGO.name = string.Format("Card{0:D2}", i);
            // Set object and Sprites immediately even though the object won't be in play until
            // next update cycle
            CardObject cardObject = cardGO.GetComponent<CardObject>();
            cardObject.SetId(i);
            
            card.state = cardState.drawPile;
            cardObject.SetCardPOD(card);
            //card.SetCardObject(cardGO, true);
            //card.state = cardState.drawPile;

            //card.SetPosition(new Vector3(-5, 5, 0));
            //card.SetSprites(true);
            //cardGO.transform.SetParent(deckParentGO.transform);
        }
        // Subscribe to static Click event once:
        CardObject.onCardClicked += OnCardClicked;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Problem: We need to have this run AFTER all the cards 'Start' methods have run
        // Solution: Either do some counting, wait for an Update()/FixedUpdate() cycle,
        // or change Script Execution Order:
        // Edit > Project Settings, Script Execution Order + dropdown, CardObject script,
        // change value to -50
        // That solution is hacky though so instead we'll do it on 1st update
        //UpdateDrawPile();
    }

    // Update is called once per frame
    void Update()
    {
        if (!cardsShowing)
        {
            UpdateDrawPile();
            cardsShowing = true;
        }
    }

    void FixedUpdate()
    {

    }

    void OnDestroy()
    {
        // Unsubscribe from static event
        CardObject.onCardClicked -= OnCardClicked;
    }

    public CardPOD DrawCard()
    {
        if (deck.Count > 0)
        {
            CardPOD drawnCard = deck[0];
            deck.RemoveAt(0);
            return drawnCard;
        }
        else
        {
            Debug.LogWarning("CardManager: DrawCard - No more cards to draw!");
            return null;
        }
    }

    public List<CardPOD> DrawCards(int numCards)
    {
        List<CardPOD> drawnCards = new();
        for (int i = 0; i < numCards; i++)
        {
            if (deck.Count > 0)
            {
                drawnCards.Add(deck[0]);
                deck.RemoveAt(0);
            }
            else
            {
                Debug.LogWarning("CardManager: DrawCards - No more cards to draw!");
                break;
            }
        }
        return drawnCards;
    }

    // Simple Deck initialization and shuffle
    // NOTE: No game objects should be attached yet
    void InitAndShuffleDeck()
    {
        List<CardPOD> deckPull = new();
        // Have to manually clone each card to avoid reference issues
        // If we change CardPOD to a struct (value semantics), this can be simplified to a ToList conversion
        foreach(var card in deckPure)
        {
            deckPull.Add(card.Clone());
        }
        deck.Clear();
        System.Random rand = new();

        while (deckPull.Count > 0)
        {
            int index = rand.Next(0, deckPull.Count);
            // More randomness..
            deckPull[index].facing = Random.value > 0.5f ? cardFace.sideA : cardFace.sideB;
            deck.Add(deckPull[index]);
            deckPull.RemoveAt(index);
        }

        // Ensure correct number of each card (6 * 6 = 36)
        if (false)
        {
            int totalRed = 0, totalGreen = 0, totalBlue = 0, totalPurple = 0, totalYellow = 0;
            for (int i = 0; i < deck.Count; i++)
            {
                switch (deck[i].cardSideAColor)
                {
                    case cardColor.red:
                        totalRed++;
                        break;
                    case cardColor.green:
                        totalGreen++;
                        break;
                    case cardColor.blue:
                        totalBlue++;
                        break;
                    case cardColor.purple:
                        totalPurple++;
                        break;
                    case cardColor.yellow:
                        totalYellow++;
                        break;
                }
                switch (deck[i].cardSideBColor)
                {
                    case cardColor.red:
                        totalRed++;
                        break;
                    case cardColor.green:
                        totalGreen++;
                        break;
                    case cardColor.blue:
                        totalBlue++;
                        break;
                    case cardColor.purple:
                        totalPurple++;
                        break;
                    case cardColor.yellow:
                        totalYellow++;
                        break;
                }
            }
            Debug.Log($"Deck Composition - Red: {totalRed}, Green: {totalGreen}, Blue: {totalBlue}, Purple: {totalPurple}, Yellow: {totalYellow}");
        }
    }

  // Updates the deck DrawPile - uses basic algorithm that Prospector Solitaire used
  //  Layering a deck of cards with sorting layer/order and Z-order 
	void UpdateDrawPile()
    {
        const float STAGGER_X = 0.05f;
        CardPOD card;
        for (int i = 0; i < deck.Count; i++)
        {
            card = deck[i];
            Vector3 cardPos = deckDefaultPosition;
            cardPos.x += STAGGER_X * i;
            cardPos.z = 0.1f * i;
            //Debug.Log("Setting local position of " + card.cardObject.name + "  to " + cardPos);
            card.SetLocalPosition(cardPos);
            //card.SetSortingLayerName("Drawpile");
            card.SetSortingOrder(-10 * i);
        }
    }

    void OnCardClicked(CardObject card)
    {
        Debug.Log("CardManager: OnCardClicked - Card clicked: " + card.gameObject.name);
        if (card.cardPOD.state == cardState.drawPile)
        {
            card.SetLocalPosition(moveToPosition);
            card.SetSortingOrder(cardsMoved * 10 + -100);
            cardsMoved++;
            card.cardPOD.state = cardState.scorePile;
        }
        else
            card.FlipCard();
    }

}
