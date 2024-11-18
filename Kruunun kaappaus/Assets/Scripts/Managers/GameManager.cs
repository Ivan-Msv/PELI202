using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public enum BoardState
{
    WaitingForPlayers, SelectingPlayer, PlayerTurnCount, PlayerMoving, Idle
}

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    [Header("Player Related")]
    public BoardPath currentPath;
    public BoardPlayerInfo currentPlayer;
    [SerializeField] private float playerTurnTime;
    private float turnTimer;
    private int playerTurn;
    [Header("Tiles")]
    public BoardTile emptyTile;
    public BoardTile minigameTile;
    public BoardTile challengeTile;
    public BoardTile shopTile;

    [SerializeField] private BoardState currentState;
    private NetworkVariable<int> playersLoaded = new();
    private List<BoardPlayerInfo> availablePlayers = new List<BoardPlayerInfo>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        NetworkObject.Spawn();
    }

    private void Start()
    {
        currentState = BoardState.WaitingForPlayers;
    }

    private void Update()
    {
        GameLoop();
    }
    private void GameLoop()
    {
        switch (currentState)
        {
            case BoardState.WaitingForPlayers:
                PlayerLoadState();
                break;
            case BoardState.SelectingPlayer:
                PlayerSelection();
                break;
            case BoardState.PlayerTurnCount:
                break;
            case BoardState.PlayerMoving:
                break;
        }
    }
    private void PlayerLoadState()
    {
        if (playersLoaded.Value < NetworkManager.Singleton.SpawnManager.GetConnectedPlayers().Count)
        {
            Time.timeScale = 0;
            return;
        }

        Time.timeScale = 1;
        GetAllPlayers();
        currentState = BoardState.SelectingPlayer;
    }
    private void GetAllPlayers()
    {
        foreach (BoardPlayerInfo player in FindObjectsByType<BoardPlayerInfo>(FindObjectsSortMode.None))
        {
            availablePlayers.Add(player);
        }
    }
    private void PlayerSelection()
    {
        turnTimer = playerTurnTime;
        currentPlayer = availablePlayers[playerTurn % availablePlayers.Count];
        playerTurn++;
        currentState = BoardState.PlayerTurnCount;
    }
    [ServerRpc(RequireOwnership = false)]
    public void LoadPlayerServerRpc()
    {
        playersLoaded.Value++;
    }
    // Kaikki liittyen peliin tulee tähän
}
