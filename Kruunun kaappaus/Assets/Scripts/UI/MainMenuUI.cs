using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;
using System.Net.Security;
using System.Net;
using Unity.Services.Authentication;

enum MenuState
{
    MainMenu, SettingsMenu, LobbySelectionMenu, CurrentLobbyMenu
}

public class MainMenuUI : NetworkBehaviour
{
    [SerializeField] private GameObject mainMenu, settingsMenu, lobbySelectionMenu, currentLobbyMenu;
    [SerializeField] private Button returnButton;
    [Space]

    [Header("Main Menu")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button openSettings;
    [Space]

    [Header("Settings Menu")]
    // Lisää myöhemmin asetuksia
    [Space]

    [Header("Lobby Selection Menu")]
    [SerializeField] private Button createLobby;
    [SerializeField] private Button joinLobby;
    [SerializeField] private GameObject joinMenu;
    [SerializeField] private TMP_InputField lobbyCode;
    [SerializeField] private Button confirmJoinLobby;
    [SerializeField] private TextMeshProUGUI errorText;
    [Space]

    [Header("Current Lobby Buttons")]
    [SerializeField] private Button startGameButton;

    private MenuState currentState;
    private Stack<MenuState> stateStack = new Stack<MenuState>();

    private void Awake()
    {
        // Main Menu & overall
        returnButton.onClick.AddListener(() => { ReturnToPreviousMenu(); });
        playButton.onClick.AddListener(() => { OpenNewMenu(MenuState.LobbySelectionMenu); });
        openSettings.onClick.AddListener(() => { OpenNewMenu(MenuState.SettingsMenu); });

        // Lobby Selection Menu
        createLobby.onClick.AddListener(() => { LobbyManager.instance.CreateLobby("New Lobby"); OpenNewMenu(MenuState.CurrentLobbyMenu); });
        joinLobby.onClick.AddListener(() => { OpenSubMenu(joinLobby.gameObject, joinMenu); });
        confirmJoinLobby.onClick.AddListener(() => { AttemptJoinLobby(); });
        //startButton.onClick.AddListener(() => { NetworkManager.SceneManager.LoadScene("GameScene", LoadSceneMode.Single); });
    }
    private void Start()
    {
        // start the game on main menu
        currentState = MenuState.MainMenu;
        stateStack.Push(MenuState.MainMenu);
        MenuScreen();
    }
    private void Update()
    {
        if (currentState == MenuState.CurrentLobbyMenu)
        {
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                CheckForGameButton();
            }
        }
    }
    private void MenuScreen()
    {
        DisableAllMenus();
        ResetSubMenus();
        if (currentState != MenuState.MainMenu && currentState != MenuState.CurrentLobbyMenu)
        {
            returnButton.gameObject.SetActive(true);
        }
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
        }
    }
    private void OpenNewMenu(MenuState givenState)
    {
        stateStack.Push(givenState);
        currentState = givenState;
        MenuScreen();
    }
    private void ReturnToPreviousMenu()
    {
        stateStack.Pop();
        currentState = stateStack.Peek();
        MenuScreen();
    }
    private void DisableAllMenus()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
    private void OpenSubMenu(GameObject originalObject, GameObject menu)
    {
        originalObject.SetActive(false);
        menu.SetActive(true);
    }
    private void ResetSubMenus()
    {
        // teen manuaalisesti kosk ei jaksa tehä kunnon funktion
        joinLobby.gameObject.SetActive(true);
        joinMenu.SetActive(false);
        errorText.gameObject.SetActive(false);
    }
    private async void AttemptJoinLobby()
    {
        try
        {
            await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.text);
            OpenNewMenu(MenuState.CurrentLobbyMenu);
        }
        catch
        {
            errorText.text = $"Failed to join a lobby using the code ({lobbyCode.text})";
            errorText.color = Color.red;
            errorText.gameObject.SetActive(true);
        }
    }
    private async void CheckForGameButton()
    {
        var lobbyId = await LobbyService.Instance.GetJoinedLobbiesAsync();
        // later
    }
}
