using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : NetworkBehaviour
{
    public static LobbyUI instance;
    public BoardMaps boardMap;
    private Lobby currentLobby;
    private string currentLobbyId;
    private float pollingTimer;
    private float codeTimer;
    private bool connecting;
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI loadingScreen;
    [SerializeField] private TextMeshProUGUI playerCount;
    [SerializeField] private TextMeshProUGUI lobbyCodeVisual;
    [SerializeField] private TextMeshProUGUI copyCodeVisual;
    [Header("Buttons")]
    [SerializeField] private Button leaveLobby;
    [SerializeField] private Button startGame;
    [SerializeField] private Button copyCode;
    [Header("Player")]
    [SerializeField] private GameObject playerTab;
    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        leaveLobby.onClick.AddListener(() => { MainMenuUI.instance.ReturnToPreviousMenu(); });
        startGame.onClick.AddListener(() => { HostGame(); AudioManager.instance.PlaySound(SoundType.Open); });
        copyCode.onClick.AddListener(() => { CopyToClipboard(currentLobby.LobbyCode); AudioManager.instance.PlaySound(SoundType.Click); });
    }
    async void OnEnable()
    {
        pollingTimer = 0;
        var joinedLobbies = await LobbyService.Instance.GetJoinedLobbiesAsync();
        currentLobbyId = joinedLobbies.Last();
        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e.InnerException);
        }
        finally
        {
            ActivateLobby();
        }
    }
    void OnDisable()
    {
        LeaveLobby();
    }
    public override void OnDestroy()
    {
        DisableListeners();
        base.OnDestroy();
    }
    private void Update()
    {
        HandleLobbyPollForUpdates();
        CheckStartMatch();
        CopiedCodeTimer();
        //CheckLobbyCount();
    }
    public async void SelectSprite(string playerId, int colorIndex, int iconIndex)
    {
        var newData = new Dictionary<string, PlayerDataObject>()
        {
            { "PlayerIconIndex", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, iconIndex.ToString()) },
            { "PlayerColor", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, colorIndex.ToString()) }
        };
        
        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, playerId, new UpdatePlayerOptions() { Data = newData });
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e.InnerException);
        }
    }
    private async void HandleLobbyPollForUpdates()
    {
        if (currentLobby != null)
        {
            pollingTimer += Time.deltaTime;

            if (pollingTimer >= 1.1f)
            {
                pollingTimer = 0;
                try
                {
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
                }
                catch (LobbyServiceException e)
                {
                    switch (e.Reason)
                    {
                        case LobbyExceptionReason.LobbyNotFound:
                            MainMenuUI.instance.ReturnToPreviousMenu();
                            MainMenuUI.instance.ShowErrorMessage("Lobby not found (Host Left?)");
                            break;
                    }
                }
                finally
                {
                    UpdatePlayerList();
                    CheckIfServerStarted();
                }
            }
        }
    }
    private void UpdatePlayerList()
    {
        playerCount.text = string.Format("Players: {0}/{1}", currentLobby.Players.Count, currentLobby.MaxPlayers);
        RefreshPlayerTab();
    }

    private void CheckLobbyCount()
    {
        if (currentLobby == null || currentLobby.HostId != AuthenticationService.Instance.PlayerId)
        {
            return;
        }

        startGame.interactable = currentLobby.Players.Count > 1;
        startGame.GetComponentInChildren<TextMeshProUGUI>().text = startGame.interactable ? "Start game" : "Can't start without at least 1 player";
    }
    private void ActivateLobby()
    {
        playerCount.text = string.Format(playerCount.text, currentLobby.Players.Count, currentLobby.MaxPlayers);
        lobbyCodeVisual.text = currentLobby.LobbyCode;

        lobbyCodeVisual.gameObject.SetActive(true);
        playerCount.gameObject.SetActive(true);

        if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            startGame.interactable = true;
        }
        else
        {
            startGame.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Only host can start match";
            startGame.interactable = false;
        }
        leaveLobby.interactable = true;

        UpdatePlayerList();

        loadingScreen.gameObject.SetActive(false);
    }
    private void DeactivateLobby()
    {
        leaveLobby.interactable = false;
        startGame.interactable = false;
        lobbyCodeVisual.gameObject.SetActive(false);
        playerCount.gameObject.SetActive(false);
        ClearPlayerList();

        loadingScreen.gameObject.SetActive(true);
    }
    private void RefreshPlayerTab()
    {
        // Tekee uuden pelaaja infon jos ei ole vielä olemassa
        foreach (var player in currentLobby.Players)
        {
            if (GameObject.Find($"{player.Id}") == null)
            {
                GameObject newPlayer = Instantiate(playerPrefab, playerTab.transform);
                PlayerLobbyInfo playerInfo = newPlayer.GetComponent<PlayerLobbyInfo>();
                playerInfo.playerData = player.Data;
                newPlayer.name = player.Id;
            }

            GameObject playerObject = GameObject.Find($"{player.Id}");
            if (playerObject == null)
            {
                return;
            }
            // päivittää player datan jos se vaihtu
            playerObject.GetComponent<PlayerLobbyInfo>().playerData = player.Data;
            
            var lobbyInfo = playerObject.GetComponent<PlayerLobbyInfo>();
            bool currentPlayer = player.Id == AuthenticationService.Instance.PlayerId;
            // laittaa sprite valikko napin päälle jos on pelaajan id
            lobbyInfo.SpriteSelectionButton.interactable = currentPlayer;

            // päivittää pelaajan spriten muille pelaajille
            if (!currentPlayer)
            {
                lobbyInfo.UpdateSprite();
            }

            // laittaa pelaajalle tekstin (host) jos on lobbyn host
            bool playerIsHost = player.Id == currentLobby.HostId;
            lobbyInfo.SetPlayerAsHost(playerIsHost);
            LobbyManager.instance.HostLobby = playerIsHost ? currentLobby : null;
        }

        //Poistaa ylimääräiset pelaajat
        if (playerTab.transform.childCount > currentLobby.Players.Count)
        {
            foreach (Transform playerTab in playerTab.transform)
            {
                bool playerExists = currentLobby.Players.Any(player => player.Id == playerTab.name);

                if (!playerExists)
                {
                    Destroy(playerTab.gameObject);
                }
            }
        }
    }
    private void ClearPlayerList()
    {
        foreach (Transform child in playerTab.transform)
        {
            Destroy(child.gameObject);
        }
    }
    private async void LeaveLobby()
    {
        bool isHost = AuthenticationService.Instance.PlayerId == currentLobby.HostId;
        try
        {
            switch (isHost)
            {
                case true:
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobbyId);
                    break;
                case false:
                    await LobbyService.Instance.RemovePlayerAsync(currentLobbyId, AuthenticationService.Instance.PlayerId);
                    break;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning(e);
        }
        finally
        {
            if (!this.IsUnityNull())
            {
                DeactivateLobby();
                LobbyManager.instance.HostLobby = null;
            }
        }
    }
    private async void HostGame()
    {
        startGame.interactable = false;
        leaveLobby.interactable = false;
        startGame.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Starting";

        AudioManager.instance.EnableLoading(true);
        BlackScreen.instance.screenFade.StartFade(BlackScreen.instance.transform, true, 1);

        var host = currentLobby.Players.Find(player => player.Id == currentLobby.HostId);

        string allocationCode = await LobbyManager.instance.CreateRelay();

        var newData = new Dictionary<string, PlayerDataObject>()
        {
            { "ServerStarted", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "1") },
            { "RandomizeTiles", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, boardMap.RandomizeMap().ToString()) },
            { "AllocationCode", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, allocationCode.ToString()) }
        };
        await LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, currentLobby.HostId, new UpdatePlayerOptions() { Data = newData });
    }
    private void CheckIfServerStarted()
    {
        if (connecting)
        {
            return;
        }

        var host = currentLobby.Players.Find(player => player.Id == currentLobby.HostId);
        if (host.Data["ServerStarted"].Value != "0" && !NetworkManager.IsConnectedClient)
        {
            AudioManager.instance.EnableLoading(true);
            BlackScreen.instance.screenFade.StartFade(BlackScreen.instance.transform, true, 1);
            leaveLobby.interactable = false;
            LobbyManager.instance.JoinRelay(host.Data["AllocationCode"].Value);
            connecting = true;
        }
    }
    private void CheckStartMatch()
    {
        if (!IsHost)
        {
            return;
        }

        if (!NetworkManager.IsConnectedClient)
        {
            return;
        }

        if (NetworkManager.ConnectedClients.Count == currentLobby.Players.Count)
        {
            NetworkManager.SceneManager.LoadScene(boardMap.GetCurrentMap(), LoadSceneMode.Single);
        }
    }
    private void DisableListeners()
    {
        startGame.onClick.RemoveAllListeners();
        copyCode.onClick.RemoveAllListeners();
    }
    private void CopyToClipboard(string text)
    {
        TextEditor textEditor = new TextEditor();
        textEditor.text = text;
        textEditor.SelectAll();
        textEditor.Copy();

        // Laittaa timer päälle, joka näyttää et koodi on kopioitu
        codeTimer = 1;
    }

    private void CopiedCodeTimer()
    {
        if (codeTimer <= 0)
        {
            copyCodeVisual.text = "Click to copy";
            return;
        }

        codeTimer -= Time.deltaTime;
        copyCodeVisual.text = "Copied!";
    }

    private void OnApplicationQuit()
    {
        ClearPlayerList();
    }
}
