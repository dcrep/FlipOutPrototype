using UnityEngine;

public enum CardState { invalid, drawPile, playerHolder, scorePile }
public enum CardColor { red, green, blue, purple, yellow , invalid }
public enum CardFace { sideA, sideB }

// Data container for CardObject

[System.Serializable]
public class CardPODClient //: MonoBehaviour
{
    public CardState state = CardState.invalid;
    public CardColor color = CardColor.invalid;

    public int cardID = -1;   // unique identifier for this card

    //[SerializeField] private PlayerX playerOwner = null;
    public int ownerPlayerID = -1;  // which player owns this card

// !! These aren't *truly* needed but saves dictionary lookup (or component/object searches)
    [System.NonSerialized]
    public GameObject cardGO = null;    // link to GameObject that the CardObject script is attached to
    [System.NonSerialized]
    public CardObject cardObject = null;    // link to CardObject (owner) script object

    // Clone to create a copy (rather than a reference)
    // This whole class might better be a struct which is a value type
    public CardPODClient Clone()
    {
        return (CardPODClient)this.MemberwiseClone();
    }
}

[System.Serializable]
public class CardPODServer
{
    public CardState state = CardState.invalid;
    public CardFace facingOwner = CardFace.sideA;

    public CardColor cardSideAColor = CardColor.invalid;
    public CardColor cardSideBColor = CardColor.invalid;

    public int cardID = -1;   // unique identifier for this card

    //[SerializeField] private PlayerX playerOwner = null;
    public int ownerPlayerID = -1;  // which player owns this card

    public void UpdatePODColor(int playerID)
    {
        if (facingOwner == CardFace.sideA)
            cardSideAColor = ColorBasedOnPlayer(playerID);
        else
            cardSideBColor = ColorBasedOnPlayer(playerID);
    }

    public CardColor ColorBasedOnPlayer(int playerID)
    {
        return (ownerPlayerID == playerID || playerID == -1) ? GetFacingColor() : GetOppositeColor();
    }

    public CardFace GetOppositeFace()
    {
        if (facingOwner == CardFace.sideA)
            return CardFace.sideB;
        else
            return CardFace.sideA;
    }

    public CardColor GetFacingColor()
    {
        if (facingOwner == CardFace.sideA)
            return cardSideAColor;
        else
            return cardSideBColor;
    }

    public CardColor GetOppositeColor()
    {
        if (facingOwner == CardFace.sideA)
            return cardSideBColor;
        else
            return cardSideAColor;
    }

    public void FlipCard()
    {
        if (facingOwner == CardFace.sideA)
            facingOwner = CardFace.sideB;
        else
            facingOwner = CardFace.sideA;
    }

    // Clone to create a copy (rather than a reference)
    // This whole class might better be a struct which is a value type
    public CardPODServer Clone()
    {
        return (CardPODServer)this.MemberwiseClone();
    }

    public CardPODClient CopyToClientCard(int playerID = -1)
    {
        CardPODClient pod = new CardPODClient();
        pod.state = this.state;
        pod.color = this.ColorBasedOnPlayer(playerID);
        pod.cardID = this.cardID;
        pod.ownerPlayerID = this.ownerPlayerID;
        // These are set client-side:
        //pod.cardGO = null;
        //pod.cardObject = null;
        return pod;
    }
}