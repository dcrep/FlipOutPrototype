using UnityEngine;

public class HandX  // : MonoBehaviour
{
    //[SerializeField] private CardPOD[] handCards = new CardPOD[6];
    private CardObject[] handCardObjects = new CardObject[6];
    [SerializeField] private PlayerX handOwner = null;
    

    void SetHand(CardObject[] newHandCardObjs)
    {
        if (newHandCardObjs.Length != 6)
        {
            Debug.LogError("HandX->SetHand(): newHandCardObjs length is not 6!");
            return;
        }
        handCardObjects = newHandCardObjs;
    }

    CardObject RemoveCardFromHand(int cardIndex)
    {
        CardObject removedCard = handCardObjects[cardIndex];
        handCardObjects[cardIndex] = null;
        return removedCard;
    }

    void AddCardToHand(CardObject newCardObj, int cardIndex)
    {
        handCardObjects[cardIndex] = newCardObj;
    }

    // Helper to set the owner from other scripts (keeps the field private for the inspector)
    public void SetOwner(PlayerX owner)
    {
        handOwner = owner;
    }
}
