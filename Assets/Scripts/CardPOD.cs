using UnityEngine;

public enum cardState { invalid, drawPile, playerHolder, scorePile }
public enum cardColor { red, green, blue, purple, yellow }
public enum cardFace { sideA, sideB }

// Data container for CardObject

[System.Serializable]
public class CardPOD //: MonoBehaviour
{
    public cardState state = cardState.invalid;
    //[SerializeField] private PlayerX playerOwner = null;

    //public cardFace facingPlayer = cardFace.sideA;
    public cardFace facing = cardFace.sideA;

    public cardColor cardSideAColor = cardColor.red;
    public cardColor cardSideBColor = cardColor.red;

    public int cardID = -1;   // unique identifier for this card

    // !! These aren't needed, but useful for debugging
    public GameObject cardGO = null;    // link to GameObject that the CardObject script is attached to
    public CardObject cardObject = null;    // link to CardObject (owner) script object

    // Clone to create a copy (rather than a reference)
    // This whole class might better be a struct which is a value type
    public CardPOD Clone()
    {
        return (CardPOD)this.MemberwiseClone();
    }
}