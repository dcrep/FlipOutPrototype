using UnityEngine;
using UnityEngine.UI;
using TMPro;

//! TODO: Fix chat for wraparound? Or at least show the last letter when scroll right
// Then add 1. Log with objects and 2. Online functionality

public class ChatTestUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform contentArea;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private ScrollRect scrollRect;

    private void Start()
    {
        sendButton.onClick.AddListener(SendMessage);

        inputField.onSubmit.AddListener((text) => SendMessage());

        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        inputField.textComponent.overflowMode = TextOverflowModes.Overflow;
        //inputField.textComponent.color = Color.black;
        
        // Activate the input field
        inputField.ActivateInputField();
    }

    private void Update()
    {
        // Press Enter to send message
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMessage();
        }
    }

    private void SendMessage()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Instantiate new message
        GameObject newMessage = Instantiate(messagePrefab, contentArea);
        TMP_Text messageText = newMessage.GetComponent<TMP_Text>();
        messageText.text = text;
        //messageText.color = Color.white;

        // Clear input
        inputField.text = "";
        inputField.ActivateInputField();

        // Auto-scroll to bottom
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}

