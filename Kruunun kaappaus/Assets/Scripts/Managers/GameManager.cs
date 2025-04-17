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
    [Header("Main")]
    public bool randomize;
    public Tiles tileType;
    public int tileWeight;
    [Space]
    [Header("Distancing")]
    public bool distanceTiles;
    public int minTileDistance;
    public int maxTileDistance;
    public int distanceRerollStartChance;
    public int distanceRerollChanceIncrease;
    [Space]
    [Header("Tile Limits")]
    public bool limitTiles;
    public int minTileAmount;
    public int maxTileAmount;
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
        BoardUIManager.instance.AddListeners();
        diceAnimator = BoardUIManager.instance.diceAnimator;
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

        // ^ The rules might already be outdated since I only change the code, not the rules afterwards
        // That was just theory craft before actual code writing

        // This was weird, just getting values will return array
        // And for this I needed to turn them into IEnumerable first, and then to dictionary
        var availableTiles = Enum.GetValues(typeof(Tiles)).Cast<Tiles>().ToDictionary(tile => tile, tile => 0);

        // Since we randomize, let's clear the board from unnecessary in-scene tiles.

        for (int i = 0; i < tilesIndex.Count; i++)
        {
            TileRandom currentTileData = FindRandomTile((Tiles)tilesIndex[i]);

            if (!currentTileData.randomize)
            {
                continue;
            }

            tilesIndex[i] = (int)Tiles.EmptyTile;
        }

        // While there's still available tiles, keep randomizing.
        // The tiles that don't have "minimum" get removed at the end of first iteration
        while (availableTiles.Count > 0)
        {
            for (int i = 0; i < tilesIndex.Count; i++)
            {
                // To prevent errors in while (true)
                if (availableTiles.Count < 1)
                {
                    break;
                }

                // This is a fail save for rerolls, in case of 1-2 availabletiles left which 
                // Could result in an infinite loops
                int rerollTries = 3;

                // A new while loop where you can "reroll" by just continuing the given iteration
                // And break the while loop once the tile is good to go
                while (true)
                {
                    if (rerollTries <= 0)
                    {
                        break;
                    }

                    if (availableTiles.Count < 1)
                    {
                        break;
                    }

                    // Roll between currently available tiles
                    Tiles randomTile = GetRandomTile(availableTiles);
                    // Get the Random Tile data and also currently placed tile data
                    TileRandom randomTileData = FindRandomTile(randomTile);
                    TileRandom currentTileData = FindRandomTile((Tiles)tilesIndex[i]);

                    // if randomization disabled break early, and remove from future available tiles.
                    if (!randomTileData.randomize)
                    {
                        availableTiles.Remove(randomTile);
                        continue;
                    }

                    // But if it's current tile data that has randomization disabled
                    // Probably means it's static in-scene tile that should NOT be replaced
                    // In that case continue on to the next tile
                    if (!currentTileData.randomize)
                    {
                        break;
                    }

                    // In case random tile has it's limits and reached them, also remove them from pool
                    // And reroll current tile
                    if (randomTileData.limitTiles && availableTiles[randomTile] >= randomTileData.maxTileAmount)
                    {
                        availableTiles.Remove(randomTile);
                        continue;
                    }

                    // This basically checks if there are any identical tiles behind it
                    // and rerolls based on given settings
                    // Start with 0, increase once it reaches minimum distance
                    var tileBehindIncreasedChance = 0;
                    bool rerollTile = false;
                    for (int j = 1; j <= randomTileData.maxTileDistance; j++)
                    {
                        // If disabled, just break
                        if (!randomTileData.distanceTiles) { break; }

                        // i is the current tile index
                        // j here is the amount of tiles behind
                        var backwardIndex = (i - j + tilesIndex.Count) % tilesIndex.Count;
                        var forwardIndex = (i + j + tilesIndex.Count) % tilesIndex.Count;

                        if (j >= randomTileData.minTileDistance)
                        {
                            tileBehindIncreasedChance += randomTileData.distanceRerollChanceIncrease;
                        }

                        bool isIdenticalTile = (Tiles)tilesIndex[backwardIndex] == randomTile || (Tiles)tilesIndex[forwardIndex] == randomTile;
                        if (isIdenticalTile)
                        {
                            // Rolls with start + increasing chance
                            // On success it continues, otherwise it rerolls a tile
                            if (!RollWithChance(randomTileData.distanceRerollStartChance + tileBehindIncreasedChance))
                            {
                                rerollTile = true;
                                break;
                            }
                        }
                    }

                    if (rerollTile)
                    {
                        rerollTries--;
                        continue;
                    }

                    availableTiles[randomTile]++;
                    tilesIndex[i] = (int)randomTile;

                    // Breaks the while true if everything went right, continues the loop
                    break;
                }
            }

            // Handles minimum tile amount limit
            // And edge cases on iteration end
            var tilesToRemove = new List<Tiles>();
            foreach (var tile in availableTiles)
            {
                var tileStruct = FindRandomTile(tile.Key);

                if (tileStruct.minTileDistance > tilesIndex.Count)
                {
                    Debug.LogError("Minimum Tile Distance larger than available tile count. Removing to prevent further issues.");
                    tilesToRemove.Add(tile.Key);
                }

                if (tileStruct.minTileAmount > tilesIndex.Count)
                {
                    Debug.LogError("Minimum Tile Amount larger than available tile count. Removing to prevent further issues.");
                    tilesToRemove.Add(tile.Key);
                }

                if (tile.Value >= tileStruct.minTileAmount)
                {
                    tilesToRemove.Add(tile.Key);
                }
            }

            foreach (var removableTile in tilesToRemove)
            {
                availableTiles.Remove(removableTile);
            }
        }

        // After everything is done, find new teleport tiles.
        BoardUIManager.instance.teleportTileUI.GetTeleportTiles();
    }

    // The random here refers to the struct... its not random I just have naming issues
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

            // DEBUG DELETE LATER
            if (Input.GetKeyDown(KeyCode.F8))
            {
                TileRandomization();
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                playerTurnTime = 329494922934;
                TurnTimer.Value = 329494922934;
            }
            if (Input.GetKeyDown(KeyCode.F10))
            {
                BoardUIManager.instance.localParent.coinAmount.Value = 9000;
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

            ChatManager.instance.SendMessageRpc(ChatType.Server, $"A new Challenge Tile has appeared...");
        }
    }

    public List<int> GetTileTypeIndexList(Tiles[] tileTypes)
    {
        List<int> availableTileIndexes = new();

        for (int i = 0; i < tilesIndex.Count; i++)
        {
            foreach (var tile in tileTypes)
            {
                if ((int)tile == tilesIndex[i])
                {
                    availableTileIndexes.Add(i);
                }
            }
        }

        return availableTileIndexes;
    }

    public void AddRandomTile(Tiles tileToAdd, Tiles[] replaceableTiles)
    {
        var availableTiles = GetTileTypeIndexList(replaceableTiles);

        if (availableTiles.Count < 1)
        {
            Debug.LogError("Couldn't find any tiles to replace, are you using this right?");
            return;
        }

        var randomIndex = availableTiles[Random.Range(0, availableTiles.Count)];
        tilesIndex[randomIndex] = (int)tileToAdd;
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
        var returnDice = dice.First(dice => dice.networkIndex == index);
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

    [Rpc(SendTo.NotMe)]
    public void ToggleEventNotificationRpc(bool toggle)
    {
        BoardUIManager.instance.ToggleEventNotification(toggle);
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
