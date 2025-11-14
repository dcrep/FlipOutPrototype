using UnityEngine;

public enum cardState { invalid, drawPile, playerHolder, scorePile }
public enum cardColor { red, green, blue, purple, yellow }
public enum cardFace { sideA, sideB }

[System.Serializable]
public class CardPF //: MonoBehaviour
{
    public cardState state = cardState.invalid;
    //[SerializeField] private PlayerPF playerOwner = null;
    public GameObject cardGO = null;
    public CardObject cardObject = null;

    public cardFace facingPlayer = cardFace.sideA; 

    public cardColor cardSideAColor = cardColor.red;
    public cardColor cardSideBColor = cardColor.red;

    public void SetCardObject(GameObject obj, bool notInPlay = false)
    {
        cardGO = obj;
        cardObject = obj.GetComponent<CardObject>();
        SetSprites(notInPlay);
    }

    public void SetSprites(bool notInPlay = false)
    {
        if (cardObject != null)
        {
            cardObject.SetSprites(this, notInPlay);
        }
    }

    public void FlipCard()
    {
        cardObject.FlipCard();
        facingPlayer = cardObject.facing;
    }
}