using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[System.Serializable]
public class CardObject : MonoBehaviour
{
    public CardSpritesSO cardSpritesSO;
    //[SerializeField]
    private SpriteRenderer spriteRenderer = null;
    //private Deck deck = null;

    [SerializeField] private Sprite sideA = null, sideB = null;

    //public cardFace facing { get; private set; } = cardFace.sideA;
    public CardPF cardPOD = null;

    // Delegate and Event for card click - static so all instances share the same event
    public delegate void OnCardClicked(CardObject card);
    public static event OnCardClicked onCardClicked;

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        //deck = FindFirstObjectByType<Deck>();
        //sideA = spriteRenderer.sprite;
        //sideB = sideA;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (sideA != null && sideB != null)
            SetSprites(sideA, sideB, cardPOD.facing);
    }

    // Update is called once per frame
    void Update()
    { }

    public void SetData(CardPF cardPF, bool notInPlay = false)
    {
        SetSprites(cardPF.cardSideAColor, cardPF.cardSideBColor, cardPF.facing, notInPlay);
        cardPOD = cardPF;
    }

    public void SetSprites(cardColor colorA, cardColor colorB, cardFace facing, bool notInPlay = false)
    {
        switch (colorA)
        {
            case cardColor.red:
                sideA = cardSpritesSO.redCard;
                break;
            case cardColor.green:
                sideA = cardSpritesSO.greenCard;
                break;
            case cardColor.blue:
                sideA = cardSpritesSO.blueCard;
                break;
            case cardColor.purple:
                sideA = cardSpritesSO.purpleCard;
                break;
            case cardColor.yellow:
                sideA = cardSpritesSO.yellowCard;
                break;
        }

        switch (colorB)
        {
            case cardColor.red:
                sideB = cardSpritesSO.redCard;
                break;
            case cardColor.green:
                sideB = cardSpritesSO.greenCard;
                break;
            case cardColor.blue:
                sideB = cardSpritesSO.blueCard;
                break;
            case cardColor.purple:
                sideB = cardSpritesSO.purpleCard;
                break;
            case cardColor.yellow:
                sideB = cardSpritesSO.yellowCard;
                break;
        }
        SetSprites(sideA, sideB, facing, notInPlay);
    }

    public void SetSprites(Sprite a, Sprite b, cardFace facing, bool notInPlay = false)
    {
        sideA = a;
        sideB = b;

        // Due to potential instantiation/setSprite timing issues:
        if (spriteRenderer == null)
        {
            // Forced setting data before object is in play?
            if (notInPlay)
                return;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("CardObject: SetSprites() - spriteRenderer is still null!");
                return;
            }
        }

        if (facing == cardFace.sideA)
            spriteRenderer.sprite = sideA;
        else
            spriteRenderer.sprite = sideB;
    }

    public void FlipCard()
    {
        if (cardPOD.facing == cardFace.sideA)
        {
            spriteRenderer.sprite = sideB;
            cardPOD.facing = cardFace.sideB;
        }
        else
        {
            spriteRenderer.sprite = sideA;
            cardPOD.facing = cardFace.sideA;
        }
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
        Debug.Log("Card: OnMouseUpAsButton() - " + gameObject.name + " clicked!");
        onCardClicked?.Invoke(this);
    }

    public void SetLocalPosition(Vector3 pos)
    {
        gameObject.transform.localPosition = pos;
    }

}
