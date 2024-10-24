using Unity.Netcode;
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

    private void Start()
    {
        currentState = PlayerState.Menu;
    }
    private void Update()
    {
        CheckPlayerState();
        UpdatePlayerState();
    }

    private void CheckPlayerState()
    {
        switch (currentState)
        {
            case PlayerState.Topdown:
                playerTopDown.SetActive(true);
                break;
            case PlayerState.Side:
                player2D.SetActive(true);
                break;
            case PlayerState.Menu:
                player2D.SetActive(false);
                playerTopDown.SetActive(false);
                break;
        }
    }
    private void UpdatePlayerState()
    {
        if (SceneManager.GetActiveScene().name.Contains("level", System.StringComparison.OrdinalIgnoreCase))
        {
            currentState = PlayerState.Side;
        }
        else if (SceneManager.GetActiveScene().name.Contains("game", System.StringComparison.OrdinalIgnoreCase))
        {
            currentState = PlayerState.Topdown;
        }
    }
}
