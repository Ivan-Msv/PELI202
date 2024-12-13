using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    [Header("Player Related")]
    public BoardPlayerInfo currentPlayer;
    public MainPlayerInfo currentPlayerInfo;
    public BoardPlayerMovement playerMovement;
    [SerializeField] private float playerTurnTime;
    public NetworkVariable<float> TurnTimer = new();
    [SerializeField] private NetworkVariable<int> playerTurn = new();
    private NetworkVariable<bool> gameEnd = new();

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
    public BoardDice[] dice;
    [Space]

    public NetworkVariable<BoardState> currentState = new();
    private NetworkVariable<int> playersLoaded = new();
    public List<BoardPlayerInfo> availablePlayers = new();
    public delegate void OnPlayerValueChangeDelegate();
    public delegate void OnCurrentPlayerChangeDelegate(FixedString64Bytes playerName);
    public event OnPlayerValueChangeDelegate OnPlayerValueChange;
    public event OnCurrentPlayerChangeDelegate OnCurrentPlayerChange;

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

        playerMovement = GetComponent<BoardPlayerMovement>();

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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        SceneManager.sceneLoaded -= OnSceneChanged;
        SceneManager.activeSceneChanged -= OnSceneChangedServer;
        Destroy(gameObject);
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
        if (gameEnd.Value)
        {
            return;
        }

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
        BoardPath.instance.SplitPlayersOnTiles();
        currentState.Value = BoardState.SelectingPlayer;
    }
    [ClientRpc]
    private void PlayerSelectionClientRpc()
    {
        currentPlayer = availablePlayers[playerTurn.Value % availablePlayers.Count];
        currentPlayerInfo = currentPlayer.GetComponentInParent<MainPlayerInfo>();
        OnCurrentPlayerChange?.Invoke(currentPlayerInfo.playerName.Value);
        BoardUIManager.instance.boardCamera.UpdateCameraFollow();
        BoardUIManager.instance.shopUI.UpdateItems();

        if (IsServer)
        {
            currentState.Value = BoardState.PlayerTurnCount;
            TurnTimer.Value = playerTurnTime;
            playerTurn.Value++;
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
        BoardUIManager.instance.UpdateLoadingPlayerUI(false, 1);

        foreach (BoardPlayerInfo player in FindObjectsByType<BoardPlayerInfo>(sortMode: FindObjectsSortMode.InstanceID))
        {
            availablePlayers.Add(player);
        }

        var sortedList = availablePlayers.OrderBy(x => x.OwnerClientId).ToList();
        availablePlayers = sortedList;

        OnPlayerValueChange?.Invoke();
    }
    [ServerRpc(RequireOwnership = false)]
    public void RollDiceServerRpc()
    {
        currentState.Value = BoardState.PlayerMoving;
        int diceIndex = GetActiveDice();
        lastRolledNumber.Value = GetDiceFromIndex(diceIndex).RollDiceNumber();
        DiceAnimationClientRpc(diceIndex, lastRolledNumber.Value);
    }
    private int GetActiveDice()
    {
        switch (currentPlayerInfo.specialDiceEnabled.Value)
        {
            case true:
                return currentPlayerInfo.specialDiceIndex.Value;
            case false:
                return 0;
        }
    }

    public void DestroyActiveDice()
    {
        currentPlayerInfo.specialDiceIndex.Value = 0;
        currentPlayerInfo.specialDiceEnabled.Value = false;
    }

    private void UseActiveDice()
    {
        GetDiceFromIndex(GetActiveDice()).SpecialAbility();

        if (!currentPlayerInfo.specialDiceEnabled.Value)
        {
            return;
        }

        currentPlayerInfo.specialDiceIndex.Value = 0;
        currentPlayerInfo.specialDiceEnabled.Value = false;
    }

    [ClientRpc]
    public void DiceAnimationClientRpc(int diceIndex, int number)
    {
        string animString = GetDiceFromIndex(diceIndex).DiceAnimationString(number);
        BoardUIManager.instance.UpdateRollDiceUI();
        diceAnimator.Play(animString);
    }

    [Rpc(SendTo.Everyone)]
    public void RerollButtonEventRpc()
    {
        BoardUIManager.instance.rerollButton.gameObject.SetActive(false);
        BoardUIManager.instance.confirmRollButton.gameObject.SetActive(false);

        if (BoardUIManager.instance.localPlayerTurn)
        {
            DestroyActiveDice();
            RollDiceServerRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void ConfirmButtonEventRpc()
    {
        BoardUIManager.instance.rerollButton.gameObject.SetActive(false);
        BoardUIManager.instance.confirmRollButton.gameObject.SetActive(false);
        ForceEndAnimation();
    }

    public void ForceEndAnimation()
    {
        diceAnimator.transform.parent.gameObject.SetActive(false);

        if (currentPlayer.OwnerClientId == NetworkManager.LocalClientId)
        {
            UseActiveDice();
        }
    }

    public IEnumerator AnimationEventCoroutine()
    {
        yield return new WaitForSeconds(afterDiceDelaySeconds);
        diceAnimator.transform.parent.gameObject.SetActive(false);

        if (currentPlayer.OwnerClientId == NetworkManager.LocalClientId)
        {
            UseActiveDice();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void LoadPlayerServerRpc()
    {
        playersLoaded.Value++;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ChangeGameStateServerRpc(BoardState newState)
    {
        currentState.Value = newState;
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
            availablePlayers[index].GetComponentInParent<MainPlayerInfo>().isGhost.Value = true;
        }

        foreach (var index in players)
        {
            availablePlayers[index].GetComponentInParent<MainPlayerInfo>().isGhost.Value = false;
        }
    }
    [ClientRpc]
    private void ChangeSceneClientRpc(string sceneName)
    {
        StartCoroutine(SceneChangeCoroutine(sceneName));
    }
    [ServerRpc(RequireOwnership = false)]
    public void TriggerGameEndServerRpc()
    {
        TriggerGameEndClientRpc();
        gameEnd.Value = true;
    }
    [ClientRpc]
    private void TriggerGameEndClientRpc()
    {
        BoardUIManager.instance.ShowEndMenu();
    }

    [Rpc(SendTo.NotMe)]
    public void PlayerShopOpenRpc(bool open)
    {
        BoardUIManager.instance.shopUI.ShopOpenInfo(open);
    }
    private IEnumerator SceneChangeCoroutine(string sceneName)
    {
        BoardUIManager.instance.UpdateLoadingPlayerUI(true, 1);
        yield return new WaitForSeconds(1);
        if (IsServer)
        {
            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
    public BoardDice GetDiceFromIndex(int index)
    {
        var returnDice = dice.FirstOrDefault(dice => dice.networkIndex == index);
        if (returnDice == null)
        {
            return dice[0];
        }

        return returnDice;
    }
}
