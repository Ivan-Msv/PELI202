using System.Collections;
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
    public MainPlayerInfo currentPlayerInfo;
    public BoardPlayerMovement playerMovement;
    [SerializeField] private float playerTurnTime;
    public NetworkVariable<float> TurnTimer = new();
    private int playerTurn;

    [Header("Tiles")]
    public BoardTile emptyTile;
    public BoardTile minigameTile;
    public BoardTile challengeTile;
    public BoardTile shopTile;

    [Header("Dice")]
    [SerializeField] private float afterDiceDelaySeconds;
    public Animator diceAnimator;
    public NetworkVariable<int> lastRolledNumber = new();

    [SerializeField] private NetworkVariable<BoardState> currentState = new();
    private NetworkVariable<int> playersLoaded = new();
    public List<BoardPlayerInfo> availablePlayers = new();
    public delegate void OnPlayerValueChangeDelegate();
    public delegate void OnCurrentPlayerChangeDelegate(FixedString64Bytes playerName);
    public event OnPlayerValueChangeDelegate OnPlayerValueChange;
    public event OnCurrentPlayerChangeDelegate OnCurrentPlayerChange;

    private void Awake()
    {
        playerMovement = GetComponent<BoardPlayerMovement>();
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if (IsServer)
        {
            NetworkObject.Spawn();
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            GameLoop();
        }
    }

    private void GameLoop()
    {
        switch (currentState.Value)
        {
            case BoardState.WaitingForPlayers:
                PlayerLoadState();
                break;
            case BoardState.SelectingPlayer:
                PlayerSelectionClientRpc();
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
        GetAllPlayersClientRpc();
        currentState.Value = BoardState.SelectingPlayer;
    }
    [ClientRpc]
    private void PlayerSelectionClientRpc()
    {
        currentPlayer = availablePlayers[playerTurn % availablePlayers.Count];
        currentPlayerInfo = currentPlayer.GetComponentInParent<MainPlayerInfo>();
        OnCurrentPlayerChange?.Invoke(currentPlayerInfo.playerName.Value);

        playerTurn++;

        if (IsServer)
        {
            TurnTimer.Value = playerTurnTime;
            currentState.Value = BoardState.PlayerTurnCount;
        }
    }
    private void PlayerTurnState()
    {
        TurnTimer.Value -= Time.deltaTime;

        if (TurnTimer.Value <= 0)
        {
            RollDiceServerRpc();
        }
    }
    [ClientRpc]
    private void GetAllPlayersClientRpc()
    {
        foreach (BoardPlayerInfo player in FindObjectsByType<BoardPlayerInfo>(FindObjectsSortMode.InstanceID))
        {
            availablePlayers.Add(player);
        }

        OnPlayerValueChange?.Invoke();
    }
    [ServerRpc(RequireOwnership = false)]
    public void RollDiceServerRpc()
    {
        currentState.Value = BoardState.Idle;
        lastRolledNumber.Value = currentPlayerInfo.test.RollDiceNumber();
        DiceAnimationClientRpc(lastRolledNumber.Value);
    }

    [ClientRpc]
    public void DiceAnimationClientRpc(int number)
    {
        string animString = currentPlayerInfo.test.DiceAnimationString(number);
        diceAnimator.gameObject.SetActive(true);
        diceAnimator.Play(animString);
    }
    public IEnumerator AnimationEventCoroutine()
    {
        yield return new WaitForSeconds(afterDiceDelaySeconds);
        diceAnimator.gameObject.SetActive(false);
        playerMovement.MovePlayer(currentPlayer, lastRolledNumber.Value);
    }
    [ServerRpc(RequireOwnership = false)]
    public void LoadPlayerServerRpc()
    {
        playersLoaded.Value++;
    }
}
