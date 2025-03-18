using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum BoardState
{
    WaitingForPlayers, SelectingPlayer, PlayerTurnCount, PlayerMoving, Idle
}

[Serializable]
public struct TileRandom
{
    [Space]
    public bool randomize;
    public Tiles tileType;
    public int tileWeight;
    [Space]
    public bool distanceTiles;
    public int tileDistance;
    public int distanceRerollStartChance;
    public int distanceRerollChanceIncrease;
    [Space]
    public bool limitTiles;
    public int tileLimit;
}

public class GameManager : NetworkBehaviour
{
    // Singleton
    public static GameManager instance;
    public string MainBoardScene { get; private set; }

    // Game Manager Related
    public NetworkVariable<BoardState> currentState = new();
    private NetworkVariable<int> playersLoaded = new();

    [Header("Tile randomization")]
    public bool randomizeTiles;
    [SerializeField] private TileRandom[] randomTiles;

    [Header("Game Related")]
    [SerializeField] private int turnsToAddChallengeTile;
    public int nonChallengeTileCount;
    [field: SerializeField] public int CrownsToWin { get; private set; }

    [Header("Player Related")]
    public BoardPlayerInfo currentPlayer;
    public MainPlayerInfo currentPlayerInfo;
    public BoardPlayerMovement playerMovement;
    public List<BoardPlayerInfo> availablePlayers = new();
    public NetworkVariable<float> TurnTimer = new();
    [SerializeField] private float playerTurnTime;
    [SerializeField] private NetworkVariable<int> playerTurn = new();

    // Used to the game doesn't continue with end screen turned on
    private NetworkVariable<bool> gameEnd = new();

    [Header("Tile Prefabs")]
    public BoardTile emptyTile;
    public BoardTile minigameTile;
    public BoardTile challengeTile;
    public BoardTile shopTile;
    public BoardTile teleportTile;
    // Index list which BoardPath script uses to refresh tiles in case they have changed.
    public NetworkList<int> tilesIndex = new();

    [Header("Dice & UI")]
    public BoardDice[] dice;
    public Animator diceAnimator;
    public NetworkVariable<int> lastRolledNumber = new();
    [SerializeField] private float afterDiceDelaySeconds;

    // Events
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
            MainBoardScene = SceneManager.GetActiveScene().name;
            SceneManager.activeSceneChanged += OnSceneChangedServer;

            var playerObject = NetworkManager.SpawnManager.GetLocalPlayerObject();
            randomizeTiles = playerObject.GetComponent<PlayerSetup>().randomizeTiles;
        }

        ComponentInitialization();
        SceneManager.sceneLoaded += OnSceneChanged;
        BoardPath.instance.InitTiles();
    }
    private void ComponentInitialization()
    {
        BoardPath.instance = GameObject.FindGameObjectWithTag("Board Path").GetComponent<BoardPath>();
        diceAnimator = BoardUIManager.instance.diceAnimator;
        instance = this;
    }

    #region Randomness

    public void TileRandomization()
    {
        // Rules: 
        // Only empty tiles can be stacked (e.g. 3 empties in a row)
        // Although empties should never stack more than 3 in a row
        // Teleport tiles should have at least distance of [teleportTileDistance] tiles from each other
        // Shop tiles should be static (so already placed in scene), or experimentally enabled via [experimentalShopRandomization] boolean
        // The problem is that shop tiles look ugly if misplaced and not rotated accordingly

        // This was weird, just getting values will return array
        // And for this I needed to turn them into IEnumerable first, and then to dictionary
        var availableTiles = Enum.GetValues(typeof(Tiles)).Cast<Tiles>().ToDictionary(tile => tile, tile => 0);

        // This takes the tile count itself, so if you want to make a new board
        // All you have to do is place empty tiles however you like
        // This only randomizes tiles itself, not the "placement"
        for (int i = 0; i < tilesIndex.Count; i++)
        {
            while (true)
            {
                // Roll between currently available tiles
                Tiles randomTile = GetRandomTile(availableTiles);
                TileRandom tileData = FindRandomTile(randomTile);

                // if randomization disabled break early, to prevent it being changed
                if (!tileData.randomize || !FindRandomTile((Tiles)tilesIndex[i]).randomize)
                {
                    break;
                }

                // Replace with empty ? 
                // So the old tiles don't persist, not sure if it even works to be fair
                tilesIndex[i] = (int)Tiles.EmptyTile;

                // Attempts to remove tile when it reaches limit
                // Not sure if this works either LOL
                if (tileData.limitTiles && availableTiles[randomTile] >= tileData.tileLimit)
                {
                    continue;
                }

                var tileDistance = tileData.tileDistance;
                var rerollStartChance = tileData.distanceRerollStartChance;
                var rerollChanceIncrease = tileData.distanceRerollChanceIncrease;

                var tileBehindIncreasedChance = 0;
                bool rerollTile = false;
                for (int j = 1; j < tileDistance + 1; j++)
                {
                    // If disabled, just break
                    if (!tileData.distanceTiles) { break; }

                    // i is the current tile index
                    // j here is the amount of tiles behind
                    var previousIndex = (i - j + BoardPath.instance.tiles.Count) % BoardPath.instance.tiles.Count;
                    var previousTile = BoardPath.instance.tiles[previousIndex].GetComponent<BoardTile>();

                    if ((Tiles)BoardPath.instance.GetTileIndex(previousTile) == randomTile)
                    {
                        // Rolls with start + increasing chance
                        // On success it continues, otherwise it rerolls a tile
                        if (!RollWithChance(rerollStartChance + tileBehindIncreasedChance))
                        {
                            rerollTile = true;
                            break;
                        }
                    }

                    tileBehindIncreasedChance += rerollChanceIncrease;
                }

                if (rerollTile) { continue; }

                availableTiles[randomTile]++;
                tilesIndex[i] = (int)randomTile;

                // Breaks the while true if everything went right, continues the loop
                break;
            }
        }
    }

    private TileRandom FindRandomTile(Tiles givenTile)
    {
        TileRandom returnTile = randomTiles.FirstOrDefault(rngTile => rngTile.tileType == givenTile);

        return returnTile;
    }

    // https://stackoverflow.com/questions/1761626/weighted-random-numbers
    // Took inspiration
    private Tiles GetRandomTile(Dictionary<Tiles, int> givenTiles)
    {
        var weightedTiles = new Dictionary<Tiles, int>(givenTiles);

        foreach (var tile in givenTiles)
        {
            weightedTiles[tile.Key] = FindRandomTile(tile.Key).tileWeight;
        }

        // Get total weight to use for a random gen
        var totalWeight = weightedTiles.Sum(tiles => tiles.Value);

        var randomWeight = Random.Range(0, totalWeight);

        foreach (var tile in weightedTiles)
        {
            randomWeight -= tile.Value;

            if (randomWeight < 0)
            {
                return tile.Key;
            }
        }

        return weightedTiles.Keys.First();
    }

    private bool RollWithChance(int chancePercent)
    {
        var randomPercent = Random.Range(0, 100);

        return randomPercent < chancePercent;
    }

    #endregion

    #region OnEvents

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkObject.DestroyWithScene = false;
        DontDestroyOnLoad(this);
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

    private void OnSceneChangedServer(Scene oldScene, Scene newScene)
    {
        if (newScene.name.Contains("level", StringComparison.OrdinalIgnoreCase))
        {
            playersLoaded.Value = 0;
            currentState.Value = BoardState.Idle;
        }
        else if (newScene.name.Contains("board", StringComparison.OrdinalIgnoreCase))
        {
            playerTurn.Value--;
            currentState.Value = BoardState.WaitingForPlayers;
        }
        else
        {
            Debug.LogError("Error in updating scene (Server)");
        }
    }

    #endregion


    private void Update()
    {
        if (IsServer)
        {
            GameLoop();

            // DEBUG
            if (Input.GetKeyDown(KeyCode.F8))
            {
                TileRandomization();
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                playerTurnTime = 329494922934;
                TurnTimer.Value = 329494922934;
            }
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
                currentState.Value = BoardState.PlayerTurnCount;
                break;
            case BoardState.PlayerTurnCount:
                PlayerTurnState();
                break;
        }
    }

    #region States

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

    private void PlayerTurnState()
    {
        TurnTimer.Value -= Time.deltaTime;

        if (TurnTimer.Value <= 0)
        {
            RollDiceServerRpc();
        }
    }

    private void TryAddChallengeTile()
    {
        if (nonChallengeTileCount >= turnsToAddChallengeTile)
        {
            AddRandomTile(Tiles.ChallengeTile, new Tiles[] { Tiles.EmptyTile });
            nonChallengeTileCount = 0;
        }
    }

    private void AddRandomTile(Tiles tileToAdd, Tiles[] replaceableTiles)
    {
        bool replaced = false;
        int tries = 100;
        while (true)
        {
            if (tries <= 0)
            {
                Debug.LogError("Couldn't find any tiles to replace, are you using this right?");
                break;
            }

            var randomIndex = Random.Range(0, tilesIndex.Count);

            foreach (var tile in replaceableTiles)
            {
                if ((int)tile == tilesIndex[randomIndex])
                {
                    tilesIndex[randomIndex] = (int)tileToAdd;
                    replaced = true;
                    break;
                }
            }

            if (replaced) { break; }

            tries--;
        }
    }

    #endregion

    #region Dice

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

    public void DestroyActiveDice()
    {
        if (!BoardUIManager.instance.localPlayerTurn)
        {
            return;
        }

        currentPlayerInfo.specialDiceIndex.Value = 0;
        currentPlayerInfo.specialDiceEnabled.Value = false;
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

    #endregion

    #region Animation

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

    private IEnumerator SceneChangeCoroutine(string sceneName)
    {
        BoardUIManager.instance.UpdateLoadingPlayerUI(true, 1);
        yield return new WaitForSeconds(1);
        if (IsServer)
        {
            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

    #endregion

    #region Server Rpc

    [Rpc(SendTo.Server)]
    public void RollDiceServerRpc()
    {
        currentState.Value = BoardState.PlayerMoving;
        int diceIndex = GetActiveDice();
        lastRolledNumber.Value = GetDiceFromIndex(diceIndex).RollDiceNumber();
        DiceAnimationRpc(diceIndex, lastRolledNumber.Value);
    }

    [Rpc(SendTo.Server)]
    public void LoadPlayerServerRpc()
    {
        playersLoaded.Value++;
    }

    [Rpc(SendTo.Server)]
    public void ChangeGameStateServerRpc(BoardState newState)
    {
        currentState.Value = newState;
    }

    [Rpc(SendTo.Server)]
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

    [Rpc(SendTo.Server)]
    public void SkipTurnServerRpc(bool forward)
    {
        playerTurn.Value += forward ? 1 : -1;
    }

    #endregion

    #region Client Rpc

    [Rpc(SendTo.Everyone)]
    private void GetAllPlayersClientRpc()
    {
        BoardUIManager.instance.UpdateLoadingPlayerUI(false, 1);

        foreach (BoardPlayerInfo player in FindObjectsByType<BoardPlayerInfo>(sortMode: FindObjectsSortMode.InstanceID))
        {
            availablePlayers.Add(player);
        }

        availablePlayers = availablePlayers.OrderBy(x => x.OwnerClientId).ToList();
        OnPlayerValueChange?.Invoke();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayerSelectionClientRpc()
    {
        currentPlayer = availablePlayers[playerTurn.Value % availablePlayers.Count];
        currentPlayerInfo = currentPlayer.GetComponentInParent<MainPlayerInfo>();
        OnCurrentPlayerChange?.Invoke(currentPlayerInfo.playerName.Value);
        BoardUIManager.instance.boardCamera.UpdateCameraFollow();

        if (IsServer)
        {
            TryAddChallengeTile();
            BoardUIManager.instance.shopUI.UpdateItems();
            TurnTimer.Value = playerTurnTime;
            playerTurn.Value++;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void LoadSceneRpc(string sceneName)
    {
        StartCoroutine(SceneChangeCoroutine(sceneName));
    }

    [Rpc(SendTo.Everyone)]
    public void TriggerGameEndRpc()
    {
        BoardUIManager.instance.ShowEndMenu();

        if (IsServer)
        {
            gameEnd.Value = true;
        }
    }

    [Rpc(SendTo.NotMe)]
    public void PlayerShopOpenRpc(bool open)
    {
        BoardUIManager.instance.shopUI.ShopOpenInfo(open);
        UpdateEnemyPlayerInfoRpc();
    }

    // For more complex notifications (like having actions) you need to make a different void
    [Rpc(SendTo.NotMe)]
    public void EventNotificationTextRpc(string notificationText)
    {
        BoardUIManager.instance.ShowEventNotification(notificationText);
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateEnemyPlayerInfoRpc()
    {
        BoardUIManager.instance.UpdateEnemyPlayerUI();
    }

    [Rpc(SendTo.Everyone)]
    public void EnableAnimationRpc(bool enable)
    {
        BoardUIManager.instance.animationActive = enable;
    }

    [Rpc(SendTo.Everyone)]
    public void DiceAnimationRpc(int diceIndex, int number)
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

    #endregion
}
