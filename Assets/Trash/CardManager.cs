using UnityEngine;
using System.Collections.Generic;
/*
public class CardManager : MonoBehaviour
{
    // Each color appears 6 times (including both sides)
    private CardPOD[] deckCardRange = new CardPOD[15]
    {
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.red },
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.green },
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.blue },
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.purple },
        new() { cardSideAColor = CardColor.red, cardSideBColor = CardColor.yellow },

        new() { cardSideAColor = CardColor.green, cardSideBColor = CardColor.green },
        new() { cardSideAColor = CardColor.green, cardSideBColor = CardColor.blue },
        new() { cardSideAColor = CardColor.green, cardSideBColor = CardColor.purple },
        new() { cardSideAColor = CardColor.green, cardSideBColor = CardColor.yellow },

        new() { cardSideAColor = CardColor.blue, cardSideBColor =  CardColor.blue },
        new() { cardSideAColor = CardColor.blue, cardSideBColor =  CardColor.purple },
        new() { cardSideAColor = CardColor.blue, cardSideBColor =  CardColor.yellow },

        new() { cardSideAColor = CardColor.purple, cardSideBColor =  CardColor.purple },
        new() { cardSideAColor = CardColor.purple, cardSideBColor =  CardColor.yellow },

        new() { cardSideAColor = CardColor.yellow, cardSideBColor =  CardColor.yellow }
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

        //cardPrefab = Resources.Load<GameObject>("Prefabs/CardPF");
        //GameManager.Instance.SetDrawPile(deckObjects);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {}

    // Update is called once per frame
    void Update()
    { }

    void FixedUpdate()
    { }

    void OnDestroy()
    { }

    // Simple Deck initialization and shuffle
    // NOTE: No game objects should be attached yet
    void InitAndShuffleDeck()
    {
        int cardID = 0;        
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
            deckPull[index].facing = Random.value > 0.5f ? CardFace.sideA : CardFace.sideB;

            CardPOD cardCopy = deckPull[index].Clone(); // not necessary since Cloned above..
            cardCopy.cardID = cardID;
            cardCopy.state = CardState.drawPile;    // by default
            deck.Add(cardCopy);
            deckPull.RemoveAt(index);
            cardID++;
        }

        // Ensure correct number of each card (6 * 6 = 36)
        if (false)
        {
            int totalRed = 0, totalGreen = 0, totalBlue = 0, totalPurple = 0, totalYellow = 0;
            for (int i = 0; i < deck.Count; i++)
            {
                switch (deck[i].cardSideAColor)
                {
                    case CardColor.red:
                        totalRed++;
                        break;
                    case CardColor.green:
                        totalGreen++;
                        break;
                    case CardColor.blue:
                        totalBlue++;
                        break;
                    case CardColor.purple:
                        totalPurple++;
                        break;
                    case CardColor.yellow:
                        totalYellow++;
                        break;
                }
                switch (deck[i].cardSideBColor)
                {
                    case CardColor.red:
                        totalRed++;
                        break;
                    case CardColor.green:
                        totalGreen++;
                        break;
                    case CardColor.blue:
                        totalBlue++;
                        break;
                    case CardColor.purple:
                        totalPurple++;
                        break;
                    case CardColor.yellow:
                        totalYellow++;
                        break;
                }
            }
            Debug.Log($"Deck Composition - Red: {totalRed}, Green: {totalGreen}, Blue: {totalBlue}, Purple: {totalPurple}, Yellow: {totalYellow}");
        }
    }

}
*/
