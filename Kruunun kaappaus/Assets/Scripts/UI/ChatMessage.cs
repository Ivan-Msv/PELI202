using TMPro;
using UnityEngine;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;

    public void SetupMessage(string text, string owner, Color messageColor, Color? ownerColor = null)
    {
        // Set the message color
        textComponent.color = messageColor;

        // If optional color not given, use the same color
        // And format it so it can be used in rich text
        var formattedColor = ColorUtility.ToHtmlStringRGB(ownerColor ?? messageColor);

        // Format the chat message to contain both message text and the owner of the message.
        var formattedText = $"<color=#{formattedColor}>{owner}</color>: {text}";
        textComponent.text = formattedText;
    }
}
