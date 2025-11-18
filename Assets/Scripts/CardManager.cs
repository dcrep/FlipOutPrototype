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
    [SerializeField] private List<CardObject> deckObjects = null;

    GameObject deckParentGO;
    //[SerializeField] private List<CardObject> deckCardObjects = null;
    private GameObject cardPrefab;

    private Vector3 deckOffscreenPosition = new Vector3(-1000, -1000, 0);

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
            // New card game object, parent -> _Cards (deckParentGO)
            GameObject cardGO = Instantiate(cardPrefab, deckOffscreenPosition, Quaternion.identity, deckParentGO.transform);
            
            // Grab CardObject component and set ID/name
            CardObject cardObject = cardGO.GetComponent<CardObject>();
            cardObject.SetId(i);
            //cardGO.name = string.Format("Card{0:D2}", i); // set in SetId()

            // Attach Card POD to CardObject
            card.state = cardState.drawPile;
            cardObject.SetCardPOD(card);

            // and put in deckObjects list
            deckObjects.Add(cardObject);
        }
        GameManager.Instance.SetDrawPile(deckObjects);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {

    }

    void OnDestroy()
    {
        // Unsubscribe from static event
        //CardObject.onCardClicked -= OnCardClicked;
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

}
