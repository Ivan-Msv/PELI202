using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

enum PlayerState
{
    Menu, Topdown, Side
}

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private GameObject player2D;
    [SerializeField] private GameObject playerTopDown;
    private PlayerState currentState;
    public Dictionary<string, PlayerDataObject> SavedData { get; private set; }

    private void Start()
    {
        if (NetworkObject.IsOwner)
        {
            Debug.Log("test");
            currentState = PlayerState.Menu;
            SavedData = FindObjectsByType<PlayerLobbyInfo>(FindObjectsSortMode.None).FirstOrDefault(player => player.name == AuthenticationService.Instance.PlayerId).playerData;
            SceneManager.activeSceneChanged += ClearSubPlayers;
        }
    }

    private void UpdatePlayerState(Scene newScene)
    {
        if (newScene.name.Contains("level", System.StringComparison.OrdinalIgnoreCase))
        {
            currentState = PlayerState.Side;
        }
        else if (newScene.name.Contains("game", System.StringComparison.OrdinalIgnoreCase))
        {
            currentState = PlayerState.Topdown;
        }
        else
        {
            Debug.LogError("Error in updating player");
        }
    }
    private void ClearSubPlayers(Scene current, Scene next)
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        UpdatePlayerState(next);

        SpawnPlayerObjectServerRpc(OwnerClientId, currentState);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerObjectServerRpc(ulong clientId, PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Topdown:
                break;
            case PlayerState.Side:
                var playerPrefab = NetworkObject.InstantiateAndSpawn(player2D, NetworkManager, clientId);
                var clientObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                playerPrefab.TrySetParent(clientObject);
                break;
            default:
                break;
        }
    }
}
