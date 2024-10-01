using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;
    [SerializeField] private float maxLobbyDecayTime = 15;
    [field: SerializeField] public Lobby HostLobby { get; private set; }
    private float lobbyDecayTimer;
    async void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () => { Debug.Log($"Signed in {AuthenticationService.Instance.PlayerId}"); };
        // delete later
        AuthenticationService.Instance.ClearSessionToken();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    void Update()
    {
        LobbyDecay();
    }
    public async void CreateLobby(string lobbyName, int maxPlayers = 4)
    {
        Lobby newLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
        HostLobby = newLobby;
        Debug.Log($"Created new lobby: {newLobby.Name}, and code being {newLobby.LobbyCode}");
    }
    private async void ListAllLobbies()
    {
        QueryLobbiesOptions lobbyOptions = new QueryLobbiesOptions
        {
            // laittaa lobbyt järjestykseen
            Order = new List<QueryOrder>() { new(false, QueryOrder.FieldOptions.Created) }
        };
        QueryResponse lobbyQuery = await LobbyService.Instance.QueryLobbiesAsync(lobbyOptions);

        // vaiha myöhemmin jotta toimii UI:n kanssa
        Debug.Log($"Lobbies found: {lobbyQuery.Results.Count}");
        foreach (Lobby lobby in lobbyQuery.Results)
        {
            Debug.Log($"{lobby.Name}, player count: {lobby.Players.Count}");
        }
    }
    public async Task<bool> CheckForExistingLobby(string givenCode)
    {
        QueryResponse lobbyQuery = await LobbyService.Instance.QueryLobbiesAsync();
        foreach (Lobby lobby in lobbyQuery.Results)
        {
            Debug.Log($"? {lobby.LobbyCode} ?? {lobby.Id}");
            if (lobby.LobbyCode == givenCode)
            {
                return true;
            }
        }

        return false;
    }
    private async void LobbyDecay()
    {
        if (HostLobby != null)
        {
            lobbyDecayTimer += Time.deltaTime;
            if (lobbyDecayTimer >= maxLobbyDecayTime)
            {
                lobbyDecayTimer = 0;
                // Pingataan lobby ettei se katoo kunhan host on vielä lobbyssä
                await LobbyService.Instance.SendHeartbeatPingAsync(HostLobby.Id);
            }
        }
    }
}
