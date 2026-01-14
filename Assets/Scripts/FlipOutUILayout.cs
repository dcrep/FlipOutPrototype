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
                scale = 0.7f, objectOffsetX = 1.8f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-4.25f, -1.25f, 0), rotationZ = 0f,
                scale = 0.7f, objectOffsetX = 1.8f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-4.25f, 1.25f, 0), rotationZ = 0f,
                scale = 0.7f, objectOffsetX = 1.8f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-4.25f, 3.75f, 0), rotationZ = 0f,
                scale = 0.7f, objectOffsetX = 1.8f, scorePileOffsetX = -2.0f }
    };
    FlipoutUIPlayerLayout[] playerLayoutFor5 = new FlipoutUIPlayerLayout[5]
    {
        new() { position = new Vector3(-3.25f, -4f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-3.25f, -2f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-3.25f, 0f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-3.25f, 2f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -2.0f },
        new() { position = new Vector3(-3.25f, 4f, 0), rotationZ = 0f,
                scale = 0.55f, objectOffsetX = 1.4f, scorePileOffsetX = -2.0f }
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

        FlipoutUIPlayerLayout[] playerLayouts = null;

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

        for (int playerNum = 0; playerNum < playerLayouts.Length; playerNum++)
        {
            FlipoutUIPlayerLayout uIPlayerLayout = playerLayouts[playerNum];
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
            // reset pos
            pos = uIPlayerLayout.position;
            // move to score pile position
            pos.x += uIPlayerLayout.scorePileOffsetX;
            InstantiateCardObject(color, pos, rotZ, scale * 0.66f);

            AddPlayerText(pos, playerNum);
        }

        // 'Highlight' current player
        playerText[0].color = Color.cyan;
        playerText[0].fontStyle = FontStyles.Bold;
    }

    private void AddPlayerText(Vector3 position, int playerNum)
    {
        Vector3 pos = position;
        playerTextGO[playerNum] = new GameObject($"Player{playerNum}_Name", typeof(RectTransform));   //, typeof(RectTransform));
        scoreKeeperGO[playerNum] = new GameObject($"Player{playerNum}_Score", typeof(RectTransform));   //, typeof(RectTransform));
        // IMPORTANT: false keeps local UI coordinates correct
        //scoreKeeperGO[playerNum].transform.SetParent(canvas.transform, false); //, false);
        playerTextGO[playerNum].transform.SetParent(canvasTextParentGO.transform, false); //, false);
        scoreKeeperGO[playerNum].transform.SetParent(canvasTextParentGO.transform, false); //, false);
        RectTransform rtPlayer = playerTextGO[playerNum].GetComponent<RectTransform>();
        RectTransform rt = scoreKeeperGO[playerNum].GetComponent<RectTransform>();
        // Use anchoredPosition for UI placement
        //rt.anchoredPosition = playerScorePilePositions[i];\

        rtPlayer.sizeDelta = new Vector2(250, 50);
        rt.sizeDelta = new Vector2(200, 50);

        pos *= 100; //ppi
        pos.x -= 50;
        rt.anchoredPosition = pos;    // playerScoreTextPositions[playerNum];

        pos.y += 60;
        Debug.Log("Player Score Text Position: " + pos);    // playerScoreTextPositions[playerNum]);
        rtPlayer.anchoredPosition = pos;    // playerScoreTextPositions[playerNum];

        //rt.localScale = Vector3.one;
        //Vector3 pos = playerScorePilePositions[i];
        //Vector3 pos = playerScoreTextPositions[playerNum];
        //pos.z = -0.5f;
        //scoreKeeperGO[i].transform.localPosition = pos;
        //scoreKeeperGO[i].transform.localScale = Vector3.one;
        playerTextGO[playerNum].layer = LayerMask.NameToLayer("UI");
        scoreKeeperGO[playerNum].layer = LayerMask.NameToLayer("UI");
        
        playerText[playerNum] = playerTextGO[playerNum].AddComponent<TextMeshProUGUI>();
        scoreText[playerNum] = scoreKeeperGO[playerNum].AddComponent<TextMeshProUGUI>();
        //scoreText[i].GetComponent<Renderer>().sortingLayerName = "UI";
        //scoreText[i].GetComponent<Renderer>().sortingOrder = 150; // Optional: set render order

        playerText[playerNum].text = "Player Name";
        playerText[playerNum].fontSize = 32;
        playerText[playerNum].alignment = TextAlignmentOptions.Left;
        playerText[playerNum].color = Color.darkBlue;

        scoreText[playerNum].text = "Score: 00";
        scoreText[playerNum].fontSize = 32;
        scoreText[playerNum].alignment = TextAlignmentOptions.Center;
        scoreText[playerNum].color = Color.black;
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
