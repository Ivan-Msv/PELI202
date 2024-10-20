using System;
using System.Collections;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager instance;
    [field: SerializeField] public Vector2 playerSpawnPoint { get; private set; }

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        StartConnection();
    }
    private void StartConnection()
    {
        var allPlayers = FindObjectsOfType<PlayerLobbyInfo>();
        var currentPlayer = allPlayers.FirstOrDefault(player => player.name == AuthenticationService.Instance.PlayerId);

        NetworkManager.OnClientConnectedCallback += (clientid) =>
        {
            var player = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientid);
            if (player.IsOwner)
            {
                RenamePlayerServerRpc(clientid, currentPlayer.name);
            }
        };

        if (currentPlayer.IsHost)
        {
            NetworkManager.StartHost();
        }
        else
        {
            NetworkManager.StartClient();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RenamePlayerServerRpc(ulong clientid, string newPlayerName)
    {
        if (IsServer)
        {
            RenamePlayerClientRpc(clientid, newPlayerName);
        }
    }

    [ClientRpc]
    private void RenamePlayerClientRpc(ulong clientid, string newPlayerName)
    {
        NetworkManager.SpawnManager.GetPlayerNetworkObject(clientid).name = newPlayerName;
    }
}
