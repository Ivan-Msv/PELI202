using System;
using System.Collections;
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
    public bool randomizeTiles;
    private PlayerState currentState;
    public Dictionary<string, PlayerDataObject> SavedData { get; private set; }

    private void Start()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }
        randomizeTiles = LobbyUI.instance.boardMap.RandomizeMap();
        currentState = PlayerState.Menu;
        SavedData = TryGetSavedData();
    }

    private void TryGetEarlyPlayer()
    {
        ClearSubPlayers(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }


    public override void OnNetworkSpawn()
    {
        SceneManager.sceneLoaded += ClearSubPlayers;
        NetworkManager.OnClientDisconnectCallback += ReturnToMenu;
        base.OnNetworkSpawn();
        TryGetEarlyPlayer();
    }

    private Dictionary<string, PlayerDataObject> TryGetSavedData()
    {
        var getData = FindObjectsByType<PlayerLobbyInfo>(FindObjectsSortMode.None).FirstOrDefault(player => player.name == AuthenticationService.Instance.PlayerId)?.playerData;

        if (getData == null)
        {
            getData = LobbyManager.instance.CreateData();
        }

        return getData;
    }

    public void ReturnToMenu(ulong action)
    {
        NetworkManager.Shutdown();
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.OnClientDisconnectCallback -= ReturnToMenu;
        SceneManager.sceneLoaded -= ClearSubPlayers;
    }

    private void UpdatePlayerState(Scene newScene)
    {
        if (newScene.name.Contains("level", StringComparison.OrdinalIgnoreCase))
        {
            currentState = PlayerState.Side;
        }
        else if (newScene.name.Contains("board", StringComparison.OrdinalIgnoreCase))
        {
            currentState = PlayerState.Topdown;
        }
        else
        {
            Debug.LogError("Error in updating player");
        }
    }
    private void ClearSubPlayers(Scene next, LoadSceneMode sceneMode)
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        UpdatePlayerState(next);

        DestroyPreviousChildrenServerRpc(NetworkObject.NetworkObjectId);

        SpawnPlayerObjectServerRpc(NetworkObject.OwnerClientId, currentState);
    }

    [Rpc(SendTo.Server)]
    private void DestroyPreviousChildrenServerRpc(ulong transformId)
    {
        Transform givenTransform = NetworkManager.SpawnManager.SpawnedObjects.FirstOrDefault(transform => transform.Key == transformId).Value.transform;
        foreach (Transform child in givenTransform)
        {
            Destroy(child.gameObject);
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerObjectServerRpc(ulong clientId, PlayerState state)
    {
        NetworkObject playerObject;
        switch (state)
        {
            case PlayerState.Topdown:
                playerObject = NetworkObject.InstantiateAndSpawn(playerTopDown, NetworkManager, clientId);
                var clientObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                playerObject.TrySetParent(clientObject);
                break;
            case PlayerState.Side:
                playerObject = NetworkObject.InstantiateAndSpawn(player2D, NetworkManager, clientId);
                var player2dObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                playerObject.TrySetParent(player2dObject);
                break;
            default:
                playerObject = null;
                break;
        }
    }
}
