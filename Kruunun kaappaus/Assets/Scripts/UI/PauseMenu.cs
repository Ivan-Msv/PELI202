using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


enum PauseScenes
{
    MainMenu, GameLoop, EndScreen
}

enum PauseStates
{
    Disabled, Pause, Settings, GeneralSettings, AudioSettings, VideoSettings, MainMenuConfirm
}

public class PauseMenu : MonoBehaviour
{
    [Header("Main Pause Menu")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseLayout;
    [Space]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button returnButton;

    [Header("Settings Menu")]
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject generalMenu;
    [SerializeField] private GameObject videoMenu;
    [SerializeField] private GameObject audioMenu;
    [SerializeField] private Button generalMenuButton;
    [SerializeField] private Button videoMenuButton;
    [SerializeField] private Button audioMenuButton;

    [Header("Main Menu Confirmation")]
    [SerializeField] private GameObject confirmMainMenu;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private MusicLayer previousLayer;

    private PauseStates currentMenuState;
    private Stack<PauseStates> pauseStateStack = new();
    public bool isMainMenuScene;

    private void Awake()
    {
        SetButtons();

        SceneManager.sceneLoaded += UpdateCurrentScene;
    }

    private void UpdateCurrentScene(Scene currentScene, LoadSceneMode loadMode)
    {
        isMainMenuScene = currentScene.name.Contains("Menu");
    }

    private void SetButtons()
    {
        // Pause Menu Buttons
        resumeButton.onClick.AddListener(() => { CloseMenu(); });
        respawnButton.onClick.AddListener(() => { Respawn(); });
        pauseButton.onClick.AddListener(() => { PauseGame(); });
        settingsButton.onClick.AddListener(() => { OpenMenu(PauseStates.Settings); });
        mainMenuButton.onClick.AddListener(() => { OpenMenu(PauseStates.MainMenuConfirm); });
        returnButton.onClick.AddListener(() => { PreviousMenu(); });

        // Settings Buttons
        generalMenuButton.onClick.AddListener(() => { OpenMenu(PauseStates.GeneralSettings, true); });
        videoMenuButton.onClick.AddListener(() => { OpenMenu(PauseStates.VideoSettings, true); });
        audioMenuButton.onClick.AddListener(() => { OpenMenu(PauseStates.AudioSettings, true); });


        // Main Menu Confirm Buttons
        confirmButton.onClick.AddListener(() => { ReturnToMainMenuScene(); });
        cancelButton.onClick.AddListener(() => { PreviousMenu(); });
    }

    void Update()
    {
        if (isMainMenuScene)
        {
            return;
        }

        PauseHotkey();
    }

    private void PauseHotkey()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenPauseMenu();
        }
    }


    // consider changing the switch into dictionary
    private void UpdateMenu()
    {
        var mainPause = currentMenuState == PauseStates.Pause;
        var confirmMenu = currentMenuState == PauseStates.MainMenuConfirm;
        pauseLayout.SetActive(mainPause);
        returnButton.gameObject.SetActive(!mainPause && !confirmMenu);

        switch (currentMenuState)
        {
            case PauseStates.Disabled:
                pauseMenu.SetActive(false);
                AudioManager.instance.ChangeMusicLayer(previousLayer);
                break;
            case PauseStates.Pause:
                pauseMenu.SetActive(true);
                previousLayer = AudioManager.instance.GetCurrentLayer();
                AudioManager.instance.ChangeMusicLayer(MusicLayer.LightLayer);
                break;
            case PauseStates.Settings:
                settingsMenu.SetActive(true);
                break;
            case PauseStates.GeneralSettings:
                generalMenu.SetActive(true);
                break;
            case PauseStates.VideoSettings:
                videoMenu.SetActive(true);
                break;
            case PauseStates.AudioSettings:
                audioMenu.SetActive(true);
                break;
            case PauseStates.MainMenuConfirm:
                confirmMainMenu.SetActive(true);
                break;
        }
    }

    private void DisableCurrentMenu()
    {
        switch (currentMenuState)
        {
            case PauseStates.Disabled:
                pauseMenu.SetActive(true);
                break;
            case PauseStates.Pause:
                pauseMenu.SetActive(false);
                break;
            default:
                foreach (Transform child in pauseMenu.transform)
                {
                    child.gameObject.SetActive(false);
                }
                break;
        }
    }

    private void OpenPauseMenu()
    {
        switch (currentMenuState)
        {
            case PauseStates.Pause:
                CloseMenu();
                break;
            case PauseStates.Disabled:
                OpenMenu(PauseStates.Pause);
                break;
            default:
                PreviousMenu();
                break;
        }
    }

    private void OpenMenu(PauseStates menuState, bool disablePrevious = false)
    {
        if (disablePrevious) { DisableCurrentMenu(); }
        currentMenuState = menuState;
        pauseStateStack.Push(currentMenuState);

        UpdateMenu();
    }

    private void PreviousMenu()
    {
        DisableCurrentMenu();

        if (pauseStateStack.Count > 0)
        {
            pauseStateStack.Pop();
            currentMenuState = pauseStateStack.Peek();
        }
        else
        {
            currentMenuState = PauseStates.Disabled;
        }

        UpdateMenu();
    }

    private void CloseMenu()
    {
        pauseStateStack.Clear();
        PreviousMenu();
    }

    private void ReturnToMainMenuScene()
    {
        CloseMenu();

        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
    }

    private void PauseGame()
    {

    }

    private void Respawn()
    {

    }
}
