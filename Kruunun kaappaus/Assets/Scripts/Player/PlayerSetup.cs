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
            SavedData = FindObjectsOfType<PlayerLobbyInfo>().FirstOrDefault(player => player.name == AuthenticationService.Instance.PlayerId).playerData;
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

        SpawnPlayerObjectServerRpc(OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerObjectServerRpc(ulong clientId)
    {
        switch (currentState)
        {
            case PlayerState.Topdown:
                break;
            case PlayerState.Side:
                var playerPrefab = Instantiate(player2D);
                playerPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
                break;
            default:
                break;
        }
    }
}
