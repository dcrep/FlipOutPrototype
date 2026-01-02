using UnityEngine;
using UnityEngine.UI;

public class CardActionMenu : MonoBehaviour
{
    public CardObject OwnerCard { get; private set; }

    public Button flipBtn, SwitchBtn, Swap1Btn, Swap2Btn, ScoreBtn, SwipeBtn;

    void Awake()
    {
        flipBtn = transform.Find("Flip").GetComponent<Button>();
        SwitchBtn = transform.Find("Switch").GetComponent<Button>();
        Swap1Btn = transform.Find("Swap1").GetComponent<Button>();
        Swap2Btn = transform.Find("Swap2").GetComponent<Button>();
        ScoreBtn = transform.Find("Score").GetComponent<Button>();
        SwipeBtn = transform.Find("Swipe").GetComponent<Button>();
    }

    public void Initialize(CardObject card)
    {
        OwnerCard = card;

        var availableActions = FlipOutGame.GetAvailableActionsForCard(card.cardPOD);
        
        // Flip, Switch, Swap1 always allowed
        /*if (availableActions.HasFlag(TurnAction.Flip))
            flipBtn.gameObject.SetActive(true);
        else
            flipBtn.gameObject.SetActive(false);

        if (availableActions.HasFlag(TurnAction.Switch))
            SwitchBtn.gameObject.SetActive(true);
        else
            SwitchBtn.gameObject.SetActive(false);
        if (availableActions.HasFlag(TurnAction.Swap1))
            Swap1Btn.gameObject.SetActive(true);
        else
            Swap1Btn.gameObject.SetActive(false);*/

        // Context-based actions
        if (availableActions.HasFlag(TurnAction.Swap2))
            Swap2Btn.gameObject.SetActive(true);
        else
            Swap2Btn.gameObject.SetActive(false);
        if (availableActions.HasFlag(TurnAction.Score))
            ScoreBtn.gameObject.SetActive(true);
        else
            ScoreBtn.gameObject.SetActive(false);
        if (availableActions.HasFlag(TurnAction.Swipe))
            SwipeBtn.gameObject.SetActive(true);
        else
            SwipeBtn.gameObject.SetActive(false);

        // Propagate to buttons
        foreach (ActionButton button in GetComponentsInChildren<ActionButton>())
        {
            button.SetOwnerCard(card);
        }
    }
}