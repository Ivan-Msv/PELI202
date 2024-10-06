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
    private string randomName;
    [SerializeField] private float maxLobbyDecayTime = 15;
    public Lobby HostLobby;
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
        randomName = $"Player {Random.Range(1, 100)}";
    }
    void Update()
    {
        LobbyDecay();
    }
    public async void CreateLobby(string lobbyName, int maxPlayers = 4)
    {
        CreateLobbyOptions lobbyOptions = new CreateLobbyOptions()
        {
            Player = CreatePlayer()
        };
        try
        {
            Lobby newLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);
            HostLobby = newLobby;
            Debug.Log($"Created new lobby: {newLobby.Name}, and code being {newLobby.LobbyCode}");
        } 
        catch (LobbyServiceException e)
        {
            string errorMessage = $"Unexpected error, couldn't create lobby ({e.ErrorCode})";
            MainMenuUI.instance.ShowErrorMessage(errorMessage);
        }
        finally
        {
            MainMenuUI.instance.OpenNewMenu(MenuState.CurrentLobbyMenu);
        }
    }
    public Player CreatePlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, randomName) },
                    }
        };
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
                Debug.LogWarning($"Sent heartbeat ping to lobby {HostLobby.Name}");
            }
        }
    }
}
