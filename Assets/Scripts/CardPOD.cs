using UnityEngine;

public enum cardState { invalid, drawPile, playerHolder, scorePile }
public enum cardColor { red, green, blue, purple, yellow }
public enum cardFace { sideA, sideB }

[System.Serializable]
public class CardPOD //: MonoBehaviour
{
    public cardState state = cardState.invalid;
    //[SerializeField] private PlayerPF playerOwner = null;
    public GameObject cardGO = null;
    public CardObject cardObject = null;

    //public cardFace facingPlayer = cardFace.sideA;
    public cardFace facing = cardFace.sideA;

    public cardColor cardSideAColor = cardColor.red;
    public cardColor cardSideBColor = cardColor.red;

    public void SetCardObject(GameObject obj, bool notInPlay = false)
    {
        cardGO = obj;
        cardObject = obj.GetComponent<CardObject>();
        cardObject.SetData(this, notInPlay);
    }

    public void SetData(bool notInPlay = false)
    {
        if (cardObject != null)
        {
            cardObject.SetData(this, notInPlay);
        }
    }

    public void SetLocalPosition(Vector3 pos)
    {
        cardGO.transform.localPosition = pos;
    }

    public void SetPosition(Vector3 pos)
    {
        if (cardGO != null)
        {
            cardGO.transform.position = pos;
        }
    }

    //public void HideCard()

    public void FlipCard()
    {
        cardObject.FlipCard();
        //facing = (facing == cardFace.sideA) ? cardFace.sideB : cardFace.sideA;
    }

    public void SetSortingLayerName(string layerName)
    {
		cardObject.SetSortingLayerName(layerName);
    }
    
    public void SetSortingOrder(int sortingOrder)
    {
		cardObject.SetSortingOrder(sortingOrder);
    }

    public CardPOD Clone()
    {
        return (CardPOD)this.MemberwiseClone();
    }
}