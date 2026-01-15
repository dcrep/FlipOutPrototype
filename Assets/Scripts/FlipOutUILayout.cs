using UnityEngine;
using UnityEditor;
using TMPro;

[System.Serializable]
public struct FlipoutUIPlayerLayout
{
    public Vector3 position;
    public float rotationZ;
    public float scale;
    public float objectOffsetX;
    public float scorePileOffsetX;
}

public class FlipOutUILayout : MonoBehaviour
{

    [SerializeField] public int numberOfPlayers = 2; // 2 to 5 players

    [SerializeField] FlipOutUILayoutSO layoutSO;

    Vector3 deckPosition= new Vector3(6f, 0f, 0f);
    float deckRotationZ = 0f;
    float deckScale = 1f;
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

    [SerializeField] private Canvas canvas = null;

    void Awake()
    {
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        canvasTextParentGO = new GameObject("CanvasTextParent");
        canvasTextParentGO.transform.SetParent(canvas.transform, false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            UpdateLayout();
        }
    }

    GameObject cardsParentGO = null;
    GameObject cardPrefab = null;

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

        cardGO.transform.localScale = new Vector3(scale, scale, 1f);
        cardGO.transform.rotation = Quaternion.Euler(0, 0, rotationZ);       // Z rotation only
        CardObject cardObject = cardGO.GetComponent<CardObject>();

        CardPODClient cardPOD = new CardPODClient { color = color };

        cardObject.SetCardPOD(cardPOD);

        //cardsInPlay.Add(cardObject);

        return cardObject;
    }


    private GameObject canvasTextParentGO = null;
    private GameObject[] playerTextGO = new GameObject[5];
    private GameObject[] scoreKeeperGO = new GameObject[5];
    private TextMeshProUGUI[] playerText = new TextMeshProUGUI[5];
    [SerializeField] private TextMeshProUGUI[] scoreText = new TextMeshProUGUI[5];
    private Vector3[] playerScoreTextPositions = new Vector3[5]
    {
        new(-864, -320, 0),    // Player 1 - Bottom left
        new(-864, 320, 0),     // Player 2 - Top left
        new(-9, -1, 0),    // Player 3 - Left center back        
        new(9, -1, 0),     // Player 4 - Right center back
        new(0, 5, 0)      // Player 5 - Center top (?!!)
    };

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



    public void UpdateLayout()
    {
        //Debug.Log("Button Clicked!");
        if (cardsParentGO != null)
        {
            DestroyImmediate(cardsParentGO);
            cardsParentGO = null;
        }
        if (canvasTextParentGO != null)
        {
            DestroyImmediate(canvasTextParentGO);
            canvasTextParentGO = new GameObject("CanvasTextParent");
            canvasTextParentGO.transform.SetParent(canvas.transform, false);
        }

        FlipoutUIPlayerLayout[] playerLayouts;

        FlipoutUIPlayerLayout[] uiTransforms = null;
        switch (numberOfPlayers)
        {
            case 2:
                uiTransforms = layoutSO.two2Players;
                playerLayouts = playerLayoutFor2;
                break;
            case 3:
                uiTransforms = layoutSO.three3Players;
                playerLayouts = playerLayoutFor3;
                break;
            case 4:
                uiTransforms = layoutSO.four4Players;
                playerLayouts = playerLayoutFor4;
                break;
            case 5:
                uiTransforms = layoutSO.five5Players;
                playerLayouts = playerLayoutFor5;
                break;
            default:
                Debug.LogWarning("Invalid number of players: " + numberOfPlayers);
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
            DealCard(playerTableNum, 4, CardColor.red);


            DrawScorePile(playerTableNum);
            // reset pos
            //pos = uIPlayerLayout.position;
            // move to score pile position
            //pos.x += uIPlayerLayout.scorePileOffsetX;
            //InstantiateCardObject(color, pos, rotZ, scale * 0.66f);

            AddPlayerText(playerTableNum);
        }

        UpdateScoresDisplay();

        CalculateDeckPosition();

        InstantiateCardObject(CardColor.invalid, deckPosition, deckRotationZ, deckScale);

        // 'Highlight' current player
        playerText[0].color = Color.cyan;
        playerText[0].fontStyle = FontStyles.Bold;
    }

    private FlipoutUIPlayerLayout[] GetUIPlayerLayouts()
    {
        FlipoutUIPlayerLayout[] playerLayouts;
        switch (numberOfPlayers)
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
                Debug.LogWarning("Invalid number of players: " + numberOfPlayers);
                return playerLayoutFor2;
        }
        return playerLayouts;
    }

    private FlipoutUIPlayerLayout GetUIPlayerLayout(int playerTableNum)
    {
        return GetUIPlayerLayouts()[playerTableNum];
    }

    private FlipoutUIPlayerLayout GetUIPlayerLayoutAtCardIdx(int playerTableNum, int cardIndex)
    {
        FlipoutUIPlayerLayout uIPlayerLayout = GetUIPlayerLayout(playerTableNum);
        Vector3 pos = uIPlayerLayout.position;
        pos.x += uIPlayerLayout.objectOffsetX * cardIndex;
        uIPlayerLayout.position = pos;
        return uIPlayerLayout;
    }

    private void DealCard(int playerTableNum, int cardIndex, CardColor color)
    {
        // Logic to deal a card to the specified player
        Debug.Log($"Dealing card {cardIndex} to player {playerTableNum}");

        FlipoutUIPlayerLayout uIPlayerLayout = GetUIPlayerLayoutAtCardIdx(playerTableNum, cardIndex);

        CardObject card = InstantiateCardObject(color, uIPlayerLayout.position, uIPlayerLayout.rotationZ, uIPlayerLayout.scale);
        card.SetSortingOrder(20);
    }

    private void DrawScorePile(int playerTableNum)
    {
        // Logic to draw score pile for the specified player
        Debug.Log($"Drawing score pile for player {playerTableNum}");

        FlipoutUIPlayerLayout uIPlayerLayout = GetUIPlayerLayout(playerTableNum);
        Vector3 pos = uIPlayerLayout.position;
        pos.x += uIPlayerLayout.scorePileOffsetX;

        CardObject card = InstantiateCardObject(CardColor.invalid, pos, uIPlayerLayout.rotationZ, uIPlayerLayout.scale * 0.66f);
        card.SetSortingOrder(10);
    }

    private void UpdateScoresDisplay()
    {
        for (int i = 0; i < numberOfPlayers; i++)
        {
            scoreText[i].text = $"Score: {Random.Range(0, 50)}";
        }
    }

    private void CalculateDeckPosition()
    {
        FlipoutUIPlayerLayout[] playerLayouts = GetUIPlayerLayouts();
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

    private void AddPlayerText(int playerTableNum)
    {
        FlipoutUIPlayerLayout uIPlayerLayout = GetUIPlayerLayout(playerTableNum);
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

        pos *= 100; //ppi
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
}


#if UNITY_EDITOR

[CustomEditor(typeof(FlipOutUILayout))]
[CanEditMultipleObjects]
public class MyObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (targets.Length > 1)
        {
            EditorGUILayout.HelpBox("Select only one object to use this button.", MessageType.Info);
            GUI.enabled = false;
        }

        if (GUILayout.Button("Update Layout"))
        {
            ((FlipOutUILayout)target).UpdateLayout();
        }

        GUI.enabled = true;
    }

}
#endif
