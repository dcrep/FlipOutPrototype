using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Deck : MonoBehaviour
{
    // Each color appears 6 times (including both sides)
    private CardPF[] deckCardRange = new CardPF[15]
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
    [SerializeField] private CardPF[] deckPure = new CardPF[90];
    [SerializeField] private List<CardPF> deck = null;

    GameObject deckParentGO;
    //[SerializeField] private List<CardObject> deckCardObjects = null;
    private GameObject cardPrefab;

    Vector3 deckDefaultPosition = new Vector3(0, 0, 0);

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
        ShuffleDeck();

        cardPrefab = Resources.Load<GameObject>("Prefabs/CardPF");

        deckParentGO = new GameObject("_Deck");
        for (int i = 0; i < deck.Count; i++)
        {
            CardPF card = deck[i];
            GameObject cardGO = Instantiate(cardPrefab, deckDefaultPosition, Quaternion.identity, deckParentGO.transform);
            //Instantiate(cardPrefab, new Vector3(3, 0, 0), Quaternion.identity);
            //CardObject cardObj = cardGO.GetComponent<CardObject>();
            // Set Sprite immediately even though the object won't be in play until
            // next update cycle
            card.SetCardObject(cardGO, true);
            //card.SetSprites(true);
            //cardGO.transform.SetParent(deckParentGO.transform);
        }
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
        if (deck != null && deck.Count > 0)
        {
            deck[Random.Range(0, deck.Count)].cardGO.transform.position += 
                        new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
            
            deck[Random.Range(0, deck.Count)].FlipCard();
        }
    }

    void ShuffleDeck()
    {
        List<CardPF> deckPull = deckPure.ToList();
        deck.Clear();
        System.Random rand = new();

        while (deckPull.Count > 0)
        {
            int index = rand.Next(0, deckPull.Count);
            // More randomness..
            deckPull[index].facingPlayer = Random.value > 0.5f ? cardFace.sideA : cardFace.sideB;
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
