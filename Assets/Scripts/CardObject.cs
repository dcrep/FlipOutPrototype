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

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        //deck = FindFirstObjectByType<CardManager>();
        //sideA = spriteRenderer.sprite;
        //sideB = sideA;
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

    public void FlipCardDEBUG()
    {
        if (cardPOD == null)
        {
            Debug.LogError("CardObject: FlipCard() - cardPOD is null!");
            return;
        }
        //cardPOD.RequestFlipCard();
   }

}
