using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum BoardState
{
    WaitingForPlayers, SelectingPlayer, PlayerTurnCount, PlayerMoving, Idle
}
public enum DiceIndex
{
    DefaultDice = 1, GambleDice = 2, MinusDice = 3, TeleportDice = 4, SmallDice = 5
}

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    [Header("Player Related")]
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
    public NetworkList<int> tilesIndex = new();

    [Header("Dice & UI")]
    [SerializeField] private float afterDiceDelaySeconds;
    public Animator diceAnimator;
    public NetworkVariable<int> lastRolledNumber = new();
    [Space]
    public DefaultDice defaultDice;
    public GambleDice gambleDice;
    public MinusDice minusDice;
    [Space]

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
            NetworkObject.DestroyWithScene = false;
        }
    }

    private void Start()
    {
        if (IsServer)
        {
            SceneManager.activeSceneChanged += OnSceneChangedServer;
        }
        ComponentInitialization();
        SceneManager.sceneLoaded += OnSceneChanged;
        BoardPath.instance.InitTiles();
    }

    private void OnSceneChanged(Scene newScene, LoadSceneMode arg1)
    {
        if (newScene.name.Contains("level", StringComparison.OrdinalIgnoreCase))
        {
            availablePlayers.Clear();
        }
        else if (newScene.name.Contains("board", StringComparison.OrdinalIgnoreCase))
        {
            ComponentInitialization();
        }
    }

    private void ComponentInitialization()
    {
        Debug.Log("initialized components.");
        BoardPath.instance = GameObject.FindGameObjectWithTag("Board Path").GetComponent<BoardPath>();
        diceAnimator = BoardUIManager.instance.diceAnimator;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkObject.DestroyWithScene = false;
        DontDestroyOnLoad(this);
    }

    private void OnSceneChangedServer(Scene oldScene, Scene newScene)
    {
        if (newScene.name.Contains("level", StringComparison.OrdinalIgnoreCase) && IsServer)
        {
            playersLoaded.Value = 0;
            currentState.Value = BoardState.Idle;
        }
        else if (newScene.name.Contains("board", StringComparison.OrdinalIgnoreCase))
        {
            currentState.Value = BoardState.WaitingForPlayers;
        }
        else
        {
            Debug.LogError("Error in updating scene (Server)");
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
        BoardUIManager.instance.shopUI.UpdateItems();
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
        int diceIndex = UseDice();
        lastRolledNumber.Value = GetDiceFromIndex(diceIndex).RollDiceNumber();
        DiceAnimationClientRpc(diceIndex, lastRolledNumber.Value);
    }
    private int UseDice()
    {
        switch (currentPlayerInfo.specialDiceEnabled.Value)
        {
            case true:
                var specialDiceValue = currentPlayerInfo.specialDiceIndex.Value;
                currentPlayerInfo.specialDiceIndex.Value = 0;
                currentPlayerInfo.specialDiceEnabled.Value = false;
                return specialDiceValue;
            case false:
                return (int)DiceIndex.DefaultDice;
        }
    }
    [ClientRpc]
    public void DiceAnimationClientRpc(int diceIndex, int number)
    {
        string animString = GetDiceFromIndex(diceIndex).DiceAnimationString(number);
        diceAnimator.gameObject.SetActive(true);
        diceAnimator.Play(animString);
    }
    public IEnumerator AnimationEventCoroutine()
    {
        yield return new WaitForSeconds(afterDiceDelaySeconds);
        diceAnimator.gameObject.SetActive(false);

        if (currentPlayer.OwnerClientId == NetworkManager.LocalClientId)
        {
            playerMovement.MovePlayer(currentPlayer, lastRolledNumber.Value);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void LoadPlayerServerRpc()
    {
        playersLoaded.Value++;
    }
    [ServerRpc(RequireOwnership = false)]
    public void LoadSceneServerRpc(string sceneName)
    {
        ChangeSceneClientRpc(sceneName);
    }
    [ServerRpc(RequireOwnership = false)]
    public void FromGhostToPlayerServerRpc(int[] ghosts, int[] players)
    {
        foreach (var index in ghosts)
        {
            GameManager.instance.availablePlayers[index].GetComponentInParent<MainPlayerInfo>().isGhost.Value = true;
        }

        foreach (var index in players)
        {
            GameManager.instance.availablePlayers[index].GetComponentInParent<MainPlayerInfo>().isGhost.Value = false;
        }
    }
    [ClientRpc]
    private void ChangeSceneClientRpc(string sceneName)
    {
        StartCoroutine(SceneChangeCoroutine(sceneName));
    }
    private IEnumerator SceneChangeCoroutine(string sceneName)
    {
        // Play animation for everyone
        yield return new WaitForSeconds(1);
        if (IsServer)
        {
            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
    public BoardDice GetDiceFromIndex(int index)
    {
        switch ((DiceIndex)index)
        {
            case DiceIndex.DefaultDice:
                return defaultDice;
            case DiceIndex.GambleDice:
                return gambleDice;
            case DiceIndex.MinusDice:
                return minusDice;
        }

        Debug.LogError("Couldn't find from given index, returning default");
        return defaultDice;
    }
}
