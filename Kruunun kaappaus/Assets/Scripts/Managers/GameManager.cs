using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public enum BoardState
{
    WaitingForPlayers, SelectingPlayer, PlayerTurnCount, PlayerMoving, Idle
}
public enum DiceIndex
{
    DefaultDice = 1, GambleDice = 2,
}

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    [Header("Player Related")]
    public BoardPath currentPath;
    public BoardPlayerInfo currentPlayer;
    private BoardPlayerMovement playerMovement;
    [SerializeField] private float playerTurnTime;
    public float TurnTimer { get; private set; }
    private int playerTurn;
    [Header("Tiles")]
    public BoardTile emptyTile;
    public BoardTile minigameTile;
    public BoardTile challengeTile;
    public BoardTile shopTile;

    [Header("Dice")]
    public BoardDice defaultDice;
    public BoardDice gambleDice;

    [SerializeField] private BoardState currentState;
    private NetworkVariable<int> playersLoaded = new();
    public List<BoardPlayerInfo> availablePlayers = new();
    public delegate void OnPlayerValueChangeDelegate();
    public delegate void OnCurrentPlayerChangeDelegate(FixedString64Bytes playerName);
    public event OnPlayerValueChangeDelegate OnPlayerValueChange;
    public event OnCurrentPlayerChangeDelegate OnCurrentPlayerChange;

    private void Awake()
    {
        currentState = BoardState.WaitingForPlayers;
        playerMovement = GetComponent<BoardPlayerMovement>();
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if (!IsServer)
        {
            return;
        }

        NetworkObject.Spawn();
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
                PlayerTurnState();
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
        foreach (BoardPlayerInfo player in FindObjectsByType<BoardPlayerInfo>(FindObjectsSortMode.InstanceID))
        {
            availablePlayers.Add(player);
        }

        OnPlayerValueChange?.Invoke();
    }
    private void PlayerSelection()
    {
        TurnTimer = playerTurnTime;

        currentPlayer = availablePlayers[playerTurn % availablePlayers.Count];
        OnCurrentPlayerChange?.Invoke(currentPlayer.GetComponentInParent<MainPlayerInfo>().playerName.Value);

        playerTurn++;
        currentState = BoardState.PlayerTurnCount;
    }
    private void PlayerTurnState()
    {
        TurnTimer -= Time.deltaTime;

        if (TurnTimer <= 0)
        {
            currentState = BoardState.SelectingPlayer;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RollDiceServerRpc()
    {
        Debug.Log("?");
        playerMovement.MovePlayer(currentPlayer, 1);
    }

    [ServerRpc(RequireOwnership = false)]
    public void LoadPlayerServerRpc()
    {
        playersLoaded.Value++;
    }
}
