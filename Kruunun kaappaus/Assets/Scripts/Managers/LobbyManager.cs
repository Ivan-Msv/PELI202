using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using Unity.Networking.Transport.Relay;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance;
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

        // delete later
        AuthenticationService.Instance.ClearSessionToken();

        bool tokenExists = AuthenticationService.Instance.SessionTokenExists;
        AuthenticationService.Instance.SignedIn += () =>
        {
            MainMenuUI.instance.SetNameAndStartMenu(tokenExists);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
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
            switch (e.Reason)
            {
                case LobbyExceptionReason.RequestTimeOut:
                    MainMenuUI.instance.ReturnToPreviousMenu();
                    string errorMsg = $"Request timeout ({e.ErrorCode})";
                    MainMenuUI.instance.ShowErrorMessage(errorMsg);
                    break;
            }
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
        string name = AuthenticationService.Instance.PlayerName.Substring(0, AuthenticationService.Instance.PlayerName.Length - 5);
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, name) },
                        { "PlayerIconIndex", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0")},
                        { "PlayerColor", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0")},
                        { "AllocationCode", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "")},
                        { "ServerStarted", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0")}
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

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

            var returnString = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.StartHost();
            Debug.Log("Started host.");

            return returnString;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
        return null;
    }

    public async void JoinRelay(string allocationCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(allocationCode);

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));

        NetworkManager.StartClient();
        Debug.Log("Started Client");
    }
}
