using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[System.Serializable]
public class CardObject : MonoBehaviour
{
    public CardSpritesSO cardSpritesSO;
    //[SerializeField]
    private SpriteRenderer spriteRenderer = null;
    //private CardManager deck = null;

    [SerializeField] private Sprite CardFace = null;

    // General game data about the card:
    public CardPODClient cardPOD = null;

    int id = -1;

    // Delegate and Event for card click - static so all instances share the same event
    public delegate void OnCardClicked(CardObject card);
    public static event OnCardClicked onCardClicked;

    private static GameObject highlightPrefab = null;
    private GameObject highlightInstance = null;

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        //deck = FindFirstObjectByType<CardManager>();
        //sideA = spriteRenderer.sprite;
        //sideB = sideA;
    }

    void OnEnable()
    {

    }
    void OnDisable()
    {
        if (highlightInstance != null)
        {
            Destroy(highlightInstance);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (CardFace != null)
            SetSprite();
    }

    // Update is called once per frame
    void Update()
    { }

    public void SetCardPOD(CardPODClient pod)
    {
        cardPOD = pod;
        SetSprite();
        gameObject.name = "Card" + cardPOD.cardID.ToString("D2");
        // !! This info should be unneccesary but for ease-of-use/debugging purposes...
        cardPOD.cardObject = this;
        cardPOD.cardGO = this.gameObject;
    }
    public void SetId(int newId)
    {
        id = newId;
        gameObject.name = "Card" + id.ToString("D2");
    }

    public void SetData(CardPODClient cardPODInit)
    {
        SetCardPOD(cardPODInit); 
    }

    public void UpdateColor(CardColor newColor)
    {
        cardPOD.color = newColor;
        SetSprite();
    }

    public void SetSprite()
    {
        switch (cardPOD.color)
        {
            case CardColor.red:
                CardFace = cardSpritesSO.redCard;
                break;
            case CardColor.green:
                CardFace = cardSpritesSO.greenCard;
                break;
            case CardColor.blue:
                CardFace = cardSpritesSO.blueCard;
                break;
            case CardColor.purple:
                CardFace = cardSpritesSO.purpleCard;
                break;
            case CardColor.yellow:
                CardFace = cardSpritesSO.yellowCard;
                break;
            case CardColor.invalid:
                CardFace = cardSpritesSO.whiteCard; // not a valid color but a safeguard
                break;
        }
        if (spriteRenderer == null)
        {
            // Forced setting data before object is in play?
            //if (notInPlay)
            //    return;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("CardObject: SetSprites() - spriteRenderer is still null!");
                return;
            }
        }
        spriteRenderer.sprite = CardFace;
    }
    
    public void SetSortingLayerName(string layerName)
    {
		spriteRenderer.sortingLayerName = layerName;
    }
    
    public void SetSortingOrder(int sortingOrder)
    {
		spriteRenderer.sortingOrder = sortingOrder;
    }

    public void OnMouseUpAsButton()
    {
        //Debug.Log("Card: OnMouseUpAsButton() - " + gameObject.name + " clicked!");
        onCardClicked?.Invoke(this);
    }

    public void SetLocalPosition(Vector3 pos)
    {
        gameObject.transform.localPosition = pos;
    }

    public void SetLocalScale(Vector3 scale)
    {
        gameObject.transform.localScale = scale;
    }

    public void HighlightCardToggle()
    {
        if (highlightPrefab == null)
        {
            highlightPrefab = Resources.Load<GameObject>("Prefabs/CircleHighlightPF");
        }
        if (highlightInstance != null)
        {
            Destroy(highlightInstance);
            highlightInstance = null;
            return;
        }
        highlightInstance = Instantiate(highlightPrefab, this.transform);
        highlightInstance.transform.localPosition = Vector3.zero; //new Vector3(0, 0, -2f);
        highlightInstance.transform.localScale = Vector3.one;
        // Make highlight render in front of the card
        SpriteRenderer highlightRenderer = highlightInstance.GetComponent<SpriteRenderer>();
        if (highlightRenderer != null)
        {
            highlightRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
            //highlightRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            highlightRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
    }

}
