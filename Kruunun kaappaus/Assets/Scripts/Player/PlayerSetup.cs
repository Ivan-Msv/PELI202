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
    private PlayerState currentState;
    public Dictionary<string, PlayerDataObject> SavedData { get; private set; }

    private void Start()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }
        currentState = PlayerState.Menu;
        SavedData = FindObjectsByType<PlayerLobbyInfo>(FindObjectsSortMode.None).FirstOrDefault(player => player.name == AuthenticationService.Instance.PlayerId).playerData;
        SceneManager.sceneLoaded += ClearSubPlayers;
        //NetworkManager.OnClientStopped += ReturnToLobby;
        NetworkManager.OnClientDisconnectCallback += ReturnToLobby;
    }
    private void ReturnToLobby(ulong action)
    {
        NetworkManager.OnClientDisconnectCallback -= ReturnToLobby;
        SceneManager.sceneLoaded -= ClearSubPlayers;
        NetworkManager.Shutdown();
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
        StartCoroutine(DelayedErrorMessage());
    }
    private IEnumerator DelayedErrorMessage()
    {
        yield return new WaitForSeconds(0.1f);
        MainMenuUI.instance.ShowErrorMessage("Test2");
    }
    private void UpdatePlayerState(Scene newScene)
    {
        if (newScene.name.Contains("level", System.StringComparison.OrdinalIgnoreCase))
        {
            currentState = PlayerState.Side;
        }
        else if (newScene.name.Contains("board", System.StringComparison.OrdinalIgnoreCase))
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
    [ServerRpc(RequireOwnership = false)]
    private void DestroyPreviousChildrenServerRpc(ulong transformId)
    {
        Transform givenTransform = NetworkManager.SpawnManager.SpawnedObjects.FirstOrDefault(transform => transform.Key == transformId).Value.transform;
        foreach (Transform child in givenTransform)
        {
            Destroy(child.gameObject);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerObjectServerRpc(ulong clientId, PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Topdown:
                var playerTD = NetworkObject.InstantiateAndSpawn(playerTopDown, NetworkManager, clientId);
                var clientObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                playerTD.TrySetParent(clientObject);
                break;
            case PlayerState.Side:
                var player2d = NetworkObject.InstantiateAndSpawn(player2D, NetworkManager, clientId);
                var player2dObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                player2d.TrySetParent(player2dObject);
                break;
            default:
                break;
        }
    }
}
