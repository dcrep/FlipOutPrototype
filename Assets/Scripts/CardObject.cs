using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[System.Serializable]
public class CardObject : MonoBehaviour
{
    public CardSpritesSO cardSpritesSO;
    //[SerializeField]
    private SpriteRenderer spriteRenderer = null;

    [SerializeField] private Sprite sideA = null, sideB = null;

    public cardFace facing { get; private set; } = cardFace.sideA;

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        //sideA = spriteRenderer.sprite;
        //sideB = sideA;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (sideA != null && sideB != null)
            SetSprites(sideA, sideB, facing);
    }

    // Update is called once per frame
    void Update()
    { }

    public void SetSprites(CardPF cardPF, bool notInPlay = false)
    {
        SetSprites(cardPF.cardSideAColor, cardPF.cardSideBColor, cardPF.facingPlayer, notInPlay);
        facing = cardPF.facingPlayer;
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
        if (spriteRenderer.sprite == sideA)
        {
            spriteRenderer.sprite = sideB;
            facing = cardFace.sideB;
        }
        else
        {
            spriteRenderer.sprite = sideA;
            facing = cardFace.sideA;
        }
    }

    public void OnMouseUpAsButton()
    {
        Debug.Log("CardPF: OnMouseUpAsButton() - Card clicked!");
    }

}
