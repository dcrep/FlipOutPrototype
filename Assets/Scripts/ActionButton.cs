using UnityEngine;
using UnityEngine.UI;

public struct CardActionRequest
{
    public TurnAction actionType;
    public CardObject sourceCard;
}

[RequireComponent(typeof(Button))]
public class ActionButton : MonoBehaviour
{
    [Header("Action this button performs")]
    public TurnAction actionType;

    private CardObject ownerCard;

    private UIManager uiManager = null;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }

    void Start()
    {
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
    }

    /// <summary>
    /// Called by CardActionMenu when the menu opens
    /// </summary>
    public void SetOwnerCard(CardObject card)
    {
        ownerCard = card;
    }

    private void OnClicked()
    {
        if (ownerCard == null)
        {
            Debug.LogError("ActionButton clicked but ownerCard is NULL.");
            return;
        }

        EmitActionRequest();
        uiManager.ToggleSelectionExternal(ownerCard);
    }

    private void EmitActionRequest()
    {
        UISignals.OnCardActionRequested?.Invoke(
            new CardActionRequest
            {
                actionType = actionType,
                sourceCard = ownerCard
            }
        );
    }
}
public static class UISignals
{
    public static System.Action<CardActionRequest> OnCardActionRequested;
}