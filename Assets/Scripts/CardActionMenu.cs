using UnityEngine;

public class CardActionMenu : MonoBehaviour
{
    public CardObject OwnerCard { get; private set; }

    public void Initialize(CardObject card)
    {
        OwnerCard = card;

        // Propagate to buttons
        foreach (ActionButton button in GetComponentsInChildren<ActionButton>())
        {
            button.SetOwnerCard(card);
        }
    }
}