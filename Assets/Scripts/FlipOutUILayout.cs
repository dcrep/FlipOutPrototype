using UnityEngine;
using UnityEditor;

public class FlipOutUILayout : MonoBehaviour
{

    [SerializeField] int numberOfPlayers = 2; // 2 to 5 players

    [SerializeField] FlipOutUILayoutSO layoutSO;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    GameObject cardsParentGO = null;
    GameObject cardPrefab = null;

   private CardObject InstantiateCardObject(CardColor color, Vector3 position, Quaternion rotation, Vector3 scale)
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

        cardGO.transform.localScale = scale;
        rotation.x = 0f;  // lock rotation on X axis
        rotation.y = 0f;  // lock rotation on Y axis
        cardGO.transform.rotation = rotation;       // Z rotation only
        CardObject cardObject = cardGO.GetComponent<CardObject>();

        CardPODClient cardPOD = new CardPODClient { color = color };

        cardObject.SetCardPOD(cardPOD);

        //cardsInPlay.Add(cardObject);

        return cardObject;
    }


    public void ShowMsg()
    {
        Debug.Log("Button Clicked!");
        if (cardsParentGO != null)
        {
            DestroyImmediate(cardsParentGO);
            cardsParentGO = null;
        }

        UITransform[] uiTransforms = null;
        switch (numberOfPlayers)
        {
            case 2:
                uiTransforms = layoutSO.two2Players;
                break;
            case 3:
                uiTransforms = layoutSO.three3Players;
                break;
            case 4:
                uiTransforms = layoutSO.four4Players;
                break;
            case 5:
                uiTransforms = layoutSO.five5Players;
                break;
            default:
                Debug.LogError("Invalid number of players: " + numberOfPlayers);
                return;
        }

        for (int playerNum = 0; playerNum < uiTransforms.Length; playerNum++)
        {
            UITransform uiTransform = uiTransforms[playerNum];
            Vector3 pos = uiTransform.position;
            Quaternion rot = uiTransform.rotation;
            Vector3 scale = uiTransform.scale;

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
                InstantiateCardObject(color, pos, rot, scale);
                pos.x += uiTransform.objectOffsetX;
            }
        }
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
            ((FlipOutUILayout)target).ShowMsg();
        }

        GUI.enabled = true;
    }

}
#endif
