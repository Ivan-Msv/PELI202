using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using WebSocketSharp;


// Global chat means both the server and player
// Server chat means only server output (so error messages, events etc)
// And player chat means only player messages (shocker)
public enum ChatType
{
    Global, Server, Player, Error
}

public class ChatManager : NetworkBehaviour
{
    public static ChatManager instance;
    [SerializeField] private Animator anim;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Scrollbar chatScrollbar;
    [SerializeField] private GameObject chatContentTab;
    [SerializeField] private ChatMessage messagePrefab;
    [SerializeField] private Image[] foregroundRenderers;
    [SerializeField] private Image[] backgroundRenderers;
    [SerializeField] private Sprite toggleChatSpriteOff, toggleChatSpriteOn;
    [SerializeField] private Button toggleChatButton, globalChatButton, serverChatButton, playerChatButton;
    [Space]

    [Header("Chat Box Settings")]
    [SerializeField] private ChatType selectedType;
    [SerializeField] private Color chatBoxForegroundColor;
    [SerializeField] private Color chatBoxBackgroundColor;
    [SerializeField] private int maxMessageAmount;
    private bool chatEnabled;

    private Dictionary<GameObject, ChatType> messageDictionary = new();

    // This gets called when you update any of the chat box settings
    // Basically to check if the color fits in editor
    private void OnValidate()
    {
        UpdateChatboxColors();
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        messageInput.onSubmit.AddListener(input => { SendMessageInput(input); });

        toggleChatButton.onClick.AddListener(() => { ToggleChat(); });
        globalChatButton.onClick.AddListener(() => { SelectChatType(ChatType.Global); });
        serverChatButton.onClick.AddListener(() => { SelectChatType(ChatType.Server); });
        playerChatButton.onClick.AddListener(() => { SelectChatType(ChatType.Player); });

        UpdateChatboxColors();
    }

    private void Update()
    {
        // Debug delete later
        if (Input.GetKeyDown(KeyCode.X))
        {
            SendChatMessage(ChatType.Server, "Test", ownerColor: Color.red);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            SendChatMessage(ChatType.Server, "Test long 46239624898926489246896248924689468924689892489246892648926894");
        }
    }

    // Override network spawn and despawn to see when you can send messages
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        messageInput.interactable = true;
        messageInput.placeholder.GetComponent<TextMeshProUGUI>().text = ". . .";
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        messageInput.interactable = false;
        messageInput.placeholder.GetComponent<TextMeshProUGUI>().text = "";
    }

    private void ToggleChat()
    {
        chatEnabled = !chatEnabled;

        var animatorState = anim.GetCurrentAnimatorStateInfo(0);

        // subtract from 1 to get flipped time, so the animation "continues" from previous state
        var startLength = 1 - animatorState.normalizedTime;

        // If the animation already ended or is in an empty state, start from square one... no, from ZERO
        if (animatorState.IsName("Empty State") || animatorState.normalizedTime >= 1)
        {
            startLength = 0;
        }

        var animation = chatEnabled ? "Enable_Chat" : "Disable_Chat";

        anim.Play(animation, 0, startLength);
    }

    private void UpdateChatboxColors()
    {
        // Change colors but keep the alpha

        foreach (var renderer in foregroundRenderers)
        {
            renderer.color = new(chatBoxForegroundColor.r, chatBoxForegroundColor.g, chatBoxForegroundColor.b, renderer.color.a);
        }

        foreach (var renderer in backgroundRenderers)
        {
            renderer.color = new(chatBoxBackgroundColor.r, chatBoxBackgroundColor.g, chatBoxBackgroundColor.b, renderer.color.a);
        }
    }

    private void SelectChatType(ChatType givenType)
    {
        // Replace currently selected type to the given one
        selectedType = givenType;

        // Disable all messages but not delete them
        // Global should enable all messages, so for that type just return after enabling instead
        bool globalMessage = givenType == ChatType.Global;

        foreach (Transform child in chatContentTab.transform)
        {
            child.gameObject.SetActive(globalMessage);
        }

        // since it would enable all messages when global type, you can return safely here
        if (globalMessage) { return; }

        // If global messages didn't exist, this would suffice to be fair...
        foreach (var message in messageDictionary)
        {
            message.Key.SetActive(message.Value == givenType);
        }
    }

    private void SendMessageInput(string message)
    {
        SendMessageRpc(ChatType.Player, message, AuthenticationService.Instance.PlayerName);

        // Clear input from the message
        messageInput.text = string.Empty;

        // Keep chat input active
        messageInput.ActivateInputField();

        // Set the scrollbar to the bottom whenever new chat is sent
        chatScrollbar.value = 0;
    }

    [Rpc(SendTo.Everyone)]
    public void SendMessageRpc(ChatType messageType, string message, string messageOwner = null)
    {
        // does the exact same thing just for every client.
        SendChatMessage(messageType, message, messageOwner);
    }

    public void SendChatMessage(ChatType messageType, string message, string messageOwner = null, Color? ownerColor = null)
    {
        if (message.IsNullOrEmpty()) { return; }

        // If no message owner declared, assume it's server message
        messageOwner ??= "[Server]";
        Color messageColor = GetMessageColor(messageType);

        var newMessage = Instantiate(messagePrefab, chatContentTab.transform);
        messageDictionary.Add(newMessage.gameObject, messageType);
        newMessage.SetupMessage(message, messageOwner, messageColor, ownerColor);

        // Set the message active based on currently selected chat type
        // Ignore it if it's global
        bool typeMatch = selectedType == messageType || selectedType == ChatType.Global;
        newMessage.gameObject.SetActive(typeMatch);

        // Check if the message count exceeds the maximum amount
        // If so, clear the earliest message
        if (chatContentTab.transform.childCount > maxMessageAmount)
        {
            var childObject = chatContentTab.transform.GetChild(0).gameObject;
            messageDictionary.Remove(childObject);
            Destroy(childObject);
        }
    }

    private Color GetMessageColor(ChatType messageType)
    {
        switch (messageType)
        {
            case ChatType.Server:
                // Orange color
                return new Color32(230, 120, 0, 255);
            case ChatType.Player:
                return Color.white;
            case ChatType.Error:
                // Dark Red Color
                return new Color32(165, 0, 0, 255);
            default:
                // In case you didn't add any custom colors
                return Color.white;
        }
    }
}
