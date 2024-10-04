using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    private ILobbyEvents lobbyEvents;
    private Lobby currentLobby;
    private string currentLobbyId;
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
        var eventCallbacks = new LobbyEventCallbacks();

        await Task.Delay(100); // Annetaan lobbyn valmistautuu kunnolla
        var joinedLobbies = await LobbyService.Instance.GetJoinedLobbiesAsync();
        currentLobbyId = joinedLobbies[0];
        currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
        lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(currentLobby.Id, eventCallbacks);

        eventCallbacks.PlayerJoined += OnPlayerJoined;
        eventCallbacks.PlayerLeft += OnPlayerLeft;
        ActivateLobby();
    }
    async void OnDisable()
    {
        string playerId = AuthenticationService.Instance.PlayerId;
        DeactivateLobby();
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            await lobbyEvents.UnsubscribeAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning(e);
        }
    }
    private void UpdatePlayerList()
    {
        playerCount.text = string.Format("Players: {0}/{1}", currentLobby.Players.Count, currentLobby.MaxPlayers);
        RefreshPlayerTab();
    }
    private async void OnPlayerLeft(List<int> left)
    {
        currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
        UpdatePlayerList();
    }
    private async void OnPlayerJoined(List<LobbyPlayerJoined> joined)
    {
        currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
        UpdatePlayerList();
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

        RefreshPlayerTab();

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
        //Poistaa ylimääräiset pelaajat
        if (playerBorder.transform.childCount > currentLobby.Players.Count)
        {
            List<string> removablePlayerTabs = new()
            {
                FindAnyObjectByType<PlayerLobbyInfo>().name
            };

            for (int i = 0; i < currentLobby.Players.Count; i++)
            {
                if (string.Compare(removablePlayerTabs[i], currentLobby.Players[i].Id) == 0)
                {
                    removablePlayerTabs.Remove(removablePlayerTabs[i]);
                }
                else
                {
                    Debug.Log("?");
                }
            }

            foreach (var removableObject in removablePlayerTabs)
            {
                Destroy(GameObject.Find(removableObject));
            }
        }

        // Tekee uuden pelaaja infon jos ei ole vielä olemassa
        foreach (var player in currentLobby.Players)
        {
            if (GameObject.Find($"{player.Id}") == null)
            {
                GameObject newPlayer = Instantiate(playerPrefab, playerBorder.transform);
                newPlayer.GetComponent<PlayerLobbyInfo>().playerName = player.Data["PlayerName"].Value;
                newPlayer.name = player.Id;
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
    private void OnApplicationQuit()
    {
        string playerId = AuthenticationService.Instance.PlayerId;
        LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
    }

}
