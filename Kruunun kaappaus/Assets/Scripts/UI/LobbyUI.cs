using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    private Lobby currentLobby;
    private string currentLobbyId;
    private float pollingTimer;
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI loadingScreen;
    [SerializeField] private TextMeshProUGUI playerCount;
    [SerializeField] private TextMeshProUGUI lobbyCodeVisual;
    [Header("Buttons")]
    [SerializeField] private Button leaveLobby;
    [SerializeField] private Button startGame;
    [Header("Player")]
    [SerializeField] private GameObject playerBorder;
    [SerializeField] private GameObject playerPrefab;

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
    private void Update()
    {
        HandleLobbyPollForUpdates();
    }
    private void PlayerList()
    {
        foreach (var player in currentLobby.Players)
        {
            Debug.Log($"Player {player.Data["PlayerName"].Value}");
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
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
                UpdatePlayerList();
                PlayerList();
            }
        }
    }
    private void UpdatePlayerList()
    {
        playerCount.text = string.Format("Players: {0}/{1}", currentLobby.Players.Count, currentLobby.MaxPlayers);
        RefreshPlayerTab();
    }
    private void ActivateLobby()
    {
        playerCount.text = string.Format(playerCount.text, currentLobby.Players.Count, currentLobby.MaxPlayers);
        lobbyCodeVisual.text = currentLobby.LobbyCode;

        lobbyCodeVisual.gameObject.SetActive(true);
        playerCount.gameObject.SetActive(true);

        if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            startGame.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Start Match";
        }else
        {
            startGame.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Only host can start match";
        }

        startGame.interactable = false;
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
            Debug.Log(player.AllocationId);
            if (GameObject.Find($"{player.Id}") == null)
            {
                GameObject newPlayer = Instantiate(playerPrefab, playerBorder.transform);
                newPlayer.GetComponent<PlayerLobbyInfo>().playerName = player.Data["PlayerName"].Value;
                newPlayer.name = player.Id;
            }


            GameObject playerObject = GameObject.Find($"{player.Id}");
            if (playerObject != null)
            {
                if (player.Id == currentLobby.HostId)
                {
                    playerObject.GetComponent<PlayerLobbyInfo>().hostText.gameObject.SetActive(true);
                    LobbyManager.instance.HostLobby = currentLobby;
                }
                else
                {
                    playerObject.GetComponent<PlayerLobbyInfo>().hostText.gameObject.SetActive(false);
                    LobbyManager.instance.HostLobby = null;
                }
            }
        }

        //Poistaa ylimääräiset pelaajat
        foreach (Transform playerTab in playerBorder.transform)
        {
            bool playerExists = currentLobby.Players.Any(player => player.Id == playerTab.name);

            if (!playerExists)
            {
                Destroy(playerTab.gameObject);
            }
        }
    }
    private void ClearPlayerList()
    {
        foreach (Transform child in playerBorder.transform)
        {
            Destroy(child.gameObject);
        }
    }
    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobbyId, AuthenticationService.Instance.PlayerId);
            Debug.Log("Left lobby");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning(e);
        }
        finally
        {
            DeactivateLobby();
            Debug.Log($"Disabled: {currentLobby.Name}");
            LobbyManager.instance.HostLobby = null;
        }
    }
    private void OnApplicationQuit()
    {
        LeaveLobby();
    }

}
