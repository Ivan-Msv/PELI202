using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using TMPro;
using Unity.Services.Authentication;
using System;
using Unity.Services.Core;

public enum MenuState
{
    MainMenu, SettingsMenu, LobbySelectionMenu, CurrentLobbyMenu, DebugMenu
}

public class MainMenuUI : MonoBehaviour
{
    public static MainMenuUI instance;

    [SerializeField] private GameObject mainMenu, settingsMenu, lobbySelectionMenu, currentLobbyMenu, debugMenu;
    [SerializeField] private Button returnButton;
    [Space]

    [Header("Main Menu")]
    [SerializeField] private bool enableDebug;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button debugButton;
    [SerializeField] private GameObject errorMessageLog;
    [SerializeField] private GameObject errorMessagePrefab;
    [Space]

    [Header("Settings Menu")]
    [SerializeField] private TextMeshProUGUI currentName;
    [SerializeField] private TMP_InputField changeNameInput;
    [Space]

    [Header("Debug Menu")]
    [SerializeField] private Button loadDebugBoard;
    [SerializeField] private Button loadCustomScene;
    [SerializeField] private GameObject customSceneMenu;
    [SerializeField] private TMP_InputField customSceneInput;
    [SerializeField] private Button customSceneHost, customSceneJoin;
    [Space]

    [Header("Lobby Selection Menu")]
    [SerializeField] private Button createLobby;
    [SerializeField] private Button joinLobby;
    [SerializeField] private GameObject joinMenu;
    [SerializeField] private TMP_InputField lobbyCode;
    [SerializeField] private Button confirmJoinLobby;

    [field: SerializeField] public Sprite[] PlayerIcons { get; private set; }
    [field: SerializeField] public RuntimeAnimatorController[] PlayerAnimators { get; private set; }
    [field: SerializeField] public RuntimeAnimatorController GhostAnimator { get; private set; }
    [Space]

    private MenuState currentState;
    private Stack<MenuState> stateStack = new Stack<MenuState>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        // Main Menu & overall
        returnButton.onClick.AddListener(() => { ReturnToPreviousMenu(); });
        playButton.onClick.AddListener(() => { OpenNewMenu(MenuState.LobbySelectionMenu); });
        settingsButton.onClick.AddListener(() => { OpenNewMenu(MenuState.SettingsMenu); });
        debugButton.onClick.AddListener(() => { OpenNewMenu(MenuState.DebugMenu); });
        exitButton.onClick.AddListener(() => { Application.Quit(); });

        // Settings Menu
        changeNameInput.onEndEdit.AddListener((text) => { AttemptChangeName(text); });

        // Lobby Selection Menu
        createLobby.onClick.AddListener(() => { LobbyManager.instance.CreateLobby("New Lobby"); createLobby.interactable = false; });
        joinLobby.onClick.AddListener(() => { OpenSubMenu(joinMenu); });
        confirmJoinLobby.onClick.AddListener(() => { AttemptJoinLobby(); });

        // Debug Menu
        loadCustomScene.onClick.AddListener(() => { OpenSubMenu(customSceneMenu); });
        loadDebugBoard.onClick.AddListener(() => { LobbyManager.instance.HostCustomScene("DebugBoard"); });
        customSceneHost.onClick.AddListener(() => { LobbyManager.instance.HostCustomScene(customSceneInput.text); });
        customSceneJoin.onClick.AddListener(() => { LobbyManager.instance.JoinRelay(customSceneInput.text); });
    }
    public void ReturnToPreviousMenu()
    {
        stateStack.Pop();
        currentState = stateStack.Peek();
        MenuScreen();
    }
    public void OpenNewMenu(MenuState givenState)
    {
        stateStack.Push(givenState);
        currentState = givenState;
        MenuScreen();
    }
    public void ShowErrorMessage(string message, int timeToExpire = 3)
    {
        errorMessageLog.SetActive(true);

        var messageObject = Instantiate(errorMessagePrefab, errorMessageLog.transform);
        var messageComponent = messageObject.GetComponent<ErrorMessage>();
        messageComponent.errorMessage = message;
        messageComponent.lifeTimeSeconds = timeToExpire;
    }
    private void MenuScreen()
    {
        DisableAllMenus();
        ResetSubMenus();
        if (currentState != MenuState.MainMenu && currentState != MenuState.CurrentLobbyMenu)
        {
            returnButton.gameObject.SetActive(true);
        }

        debugButton.gameObject.SetActive(enableDebug);

        switch (currentState)
        {
            case MenuState.MainMenu:
                mainMenu.SetActive(true);
                break;
            case MenuState.SettingsMenu:
                settingsMenu.SetActive(true);
                break;
            case MenuState.LobbySelectionMenu:
                lobbySelectionMenu.SetActive(true);
                break;
            case MenuState.CurrentLobbyMenu:
                currentLobbyMenu.SetActive(true);
                break;
            case MenuState.DebugMenu:
                debugMenu.SetActive(true);
                break;
        }
    }
    private void DisableAllMenus()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
    private void OpenSubMenu(GameObject menu)
    {
        menu.SetActive(!menu.activeSelf);
    }
    private void ResetSubMenus()
    {
        // teen manuaalisesti kosk ei jaksa tehä kunnon funktion
        joinLobby.gameObject.SetActive(true);
        joinMenu.SetActive(false);
        joinMenu.GetComponentInChildren<TMP_InputField>().text = string.Empty;

        createLobby.interactable = true;
    }
    private async void AttemptJoinLobby()
    {
        JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions()
        {
            Player = LobbyManager.instance.CreatePlayer()
        };

        try
        {
            await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.text, joinOptions);
            OpenNewMenu(MenuState.CurrentLobbyMenu);
        }
        catch (LobbyServiceException e)
        {
            switch (e.Reason)
            {
                case LobbyExceptionReason.InvalidJoinCode:
                    ShowErrorMessage($"Couldn't find a lobby using the code ({lobbyCode.text.ToUpper()})", 3);
                    break;
                case LobbyExceptionReason.LobbyNotFound:
                    ShowErrorMessage($"Lobby not found (Recently deleted)", 3);
                    break;
                case LobbyExceptionReason.LobbyFull:
                    ShowErrorMessage($"Lobby is already full", 3);
                    break;
                default:
                    Debug.LogWarning(e.Reason);
                    break;
            }
        }
        catch (ArgumentException ae)
        {
            Debug.LogError(ae);
        }
    }
    private async void AttemptChangeName(string newName)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            currentName.text = newName;
            changeNameInput.text = null;
        }
        catch (RequestFailedException e)
        {
            // switch ei toimi error koodeissa jostain syystä (ei oo constant int)
            if (e.ErrorCode == AuthenticationErrorCodes.InvalidParameters)
            {
                ShowErrorMessage(e.Message, 5);
            }
        }
    }
    public async void SetNameAndStartMenu(bool tokenExists)
    {
        string randomName = $"Player_{UnityEngine.Random.Range(1, 100)}";

        if (!tokenExists)
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(randomName);
            currentName.text = randomName;
        }
        else
        {
            currentName.text = AuthenticationService.Instance.PlayerName.Substring(0, AuthenticationService.Instance.PlayerName.Length - 5);
        }

        // Laittaa näkyviin vasta sen jälkeen kun asettaa nimen
        OpenNewMenu(MenuState.MainMenu);
    }
    public static Color GetColor(int colorIndex)
    {
        var newColor = (Colors)colorIndex;
        Color assignedColor;
        switch (newColor)
        {
            case Colors.None:
                assignedColor = new Color(1, 1, 1, 0);
                break;
            case Colors.White:
                assignedColor = Color.white;
                break;
            case Colors.Red:
                assignedColor = Color.red;
                break;
            case Colors.Green:
                assignedColor = Color.green;
                break;
            case Colors.Magenta:
                assignedColor = Color.magenta;
                break;
            default:
                assignedColor = new Color(1, 1, 1, 0);
                break;
        }

        return assignedColor;
    }
}
