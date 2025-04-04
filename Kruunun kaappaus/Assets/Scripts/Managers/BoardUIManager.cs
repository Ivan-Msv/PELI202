﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BoardUIManager : MonoBehaviour
{
    public static BoardUIManager instance;
    public BoardPlayerInfo localPlayer;
    public MainPlayerInfo localParent;
    public bool onShopTile;
    public bool localPlayerTurn;
    public bool animationActive;
    private bool eventNotificationEnabled;

    [Header("Enemy Info")]
    [SerializeField] private GameObject enemyInfoPanel;
    [SerializeField] private EnemyPlayerInfo enemyInfoPrefab;
    [SerializeField] private Dictionary<MainPlayerInfo, EnemyPlayerInfo> enemyPlayerDictionary = new();

    [Header("Main UI")]
    public GameObject gameEndUI;
    public GameObject mapUI;
    public BoardMap mapComponent;
    public GameObject rollDiceUI;
    public SpecialDiceUI diceUI;
    public GameObject eventNotificationUI;
    public BoardCamera boardCamera;
    public Animator diceAnimator;

    [Header("Event UI")]
    public BoardShop shopUI;
    public TeleportTileUI teleportTileUI;

    [Header("Buttons")]
    [SerializeField] private Button[] mapButtons;
    [SerializeField] private Button rollButton;
    [SerializeField] private Toggle eventNotificationToggle;
    public Button rerollButton;
    public Button confirmRollButton;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI coinCounter;
    [SerializeField] private TextMeshProUGUI crownCounter;
    [SerializeField] private TextMeshProUGUI playerTurn;
    [SerializeField] private TextMeshProUGUI playerTurnTimer;
    [SerializeField] private TextMeshProUGUI gameEndHeader;
    [SerializeField] private TextMeshProUGUI gameEndResults;
    [SerializeField] private TextMeshProUGUI rollDiceName;
    [SerializeField] private TextMeshProUGUI eventNotificationText;
    public string CurrentTurnPlayerName { get; private set; }


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        rollButton.onClick.AddListener(() => { GameManager.instance.RollDiceServerRpc(); });
        rerollButton.onClick.AddListener(() => { GameManager.instance.RerollButtonEventRpc(); });
        confirmRollButton.onClick.AddListener(() => { GameManager.instance.ConfirmButtonEventRpc(); });

        foreach (var button in mapButtons)
        {
            button.onClick.AddListener(() => { ToggleMap(); });
        }
    }

    public void AddListeners()
    {
        GameManager.instance.OnPlayerValueChange += UpdateLocalPlayers;
        GameManager.instance.OnCurrentPlayerChange += UpdateCurrentPlayerName;
    }

    private void Update()
    {
        if (localParent == null)
        {
            return;
        }
        UpdateButtons();
        UpdateText();
        onShopTile = LocalPlayerOnShopTile();
        localPlayerTurn = LocalPlayerTurn();
    }

    private void UpdateButtons()
    {
        if (!LocalPlayerTurn() || GameManager.instance.currentState.Value == BoardState.PlayerMoving || animationActive)
        {
            rollButton.interactable = false;
            return;
        }

        rollButton.interactable = true;
    }

    private void UpdateText()
    {
        coinCounter.text = $"{localParent.coinAmount.Value}";
        crownCounter.text = $"{localParent.crownAmount.Value}";

        playerTurn.text = $"{(LocalPlayerTurn() ? "Your" : CurrentTurnPlayerName) } turn";

        var timeInMinutes = Mathf.FloorToInt((GameManager.instance.TurnTimer.Value + 1) / 60);
        var timeInSeconds = Mathf.FloorToInt((GameManager.instance.TurnTimer.Value + 1) - timeInMinutes * 60);
        string timerText = string.Format("{0:00}:{1:00}", timeInMinutes, timeInSeconds);
        playerTurnTimer.text = timerText;
    }

    private void ToggleMap()
    {
        mapUI.SetActive(!mapUI.activeSelf);
        mapComponent.ResetMapPosition();
    }

    private void UpdateLocalPlayers()
    {
        localPlayer = GameManager.instance.availablePlayers.Find(player => player.OwnerClientId == NetworkManager.Singleton.LocalClientId);
        localParent = localPlayer.GetComponentInParent<MainPlayerInfo>();

        CreateEnemyPlayerUI();
        CheckForGameEnd();
    }
    private void UpdateCurrentPlayerName(FixedString64Bytes newName)
    {
        CurrentTurnPlayerName = newName.ToString();
        UpdateEnemyPlayerUI();
    }

    public void UpdateEnemyPlayerUI()
    {
        foreach (var enemyPlayer in enemyPlayerDictionary)
        {
            // I dont remember making this
            enemyPlayer.Value.UpdatePlayerInfo(enemyPlayer.Key.playerName.Value.ToString(), enemyPlayer.Key.coinAmount.Value, enemyPlayer.Key.crownAmount.Value, GameManager.instance.GetDiceFromIndex(enemyPlayer.Key.specialDiceIndex.Value).image);
        }
    }

    private void CreateEnemyPlayerUI()
    {
        foreach (var playerChild in GameManager.instance.availablePlayers)
        {
            if (playerChild == localPlayer)
            {
                continue;
            }

            MainPlayerInfo getInfo = playerChild.GetComponentInParent<MainPlayerInfo>();

            EnemyPlayerInfo newEnemyInfo = Instantiate(enemyInfoPrefab, enemyInfoPanel.transform);
            newEnemyInfo.UpdatePlayerInfo(getInfo.playerName.Value.ToString(), getInfo.coinAmount.Value, getInfo.crownAmount.Value, GameManager.instance.GetDiceFromIndex(getInfo.specialDiceIndex.Value).image);
            enemyPlayerDictionary.Add(getInfo, newEnemyInfo);
        }

        UpdateEnemyPlayerUI();
    }

    private bool LocalPlayerTurn()
    {
        return localPlayer == GameManager.instance.currentPlayer;
    }

    public bool LocalPlayerOnShopTile()
    {
        var hisTurn = LocalPlayerTurn();
        var onTile = BoardPath.instance.GetIndexTile(GameManager.instance.tilesIndex[localParent.currentBoardPosition.Value]) == GameManager.instance.shopTile;
        return hisTurn && onTile;
    }

    private void CheckForGameEnd()
    {
        if (localParent.crownAmount.Value >= GameManager.instance.CrownsToWin)
        {
            GameManager.instance.TriggerGameEndRpc();
        }
    }

    public void ShowEventNotification(string notificationText, Action<bool> notificationToggleAction = null)
    {
        // Reverse enable
        eventNotificationEnabled = !eventNotificationEnabled;

        eventNotificationToggle.onValueChanged.RemoveAllListeners();
        // Give functionality to toggle if functionality isn't null
        if (notificationToggleAction != null)
        {
            eventNotificationToggle.onValueChanged.AddListener((enabled) => { notificationToggleAction(enabled); });

            eventNotificationToggle.interactable = eventNotificationEnabled;
            BlackScreen.instance.screenFade.StartFade(eventNotificationToggle.transform, eventNotificationEnabled);
        }

        BlackScreen.instance.screenFade.StartFade(eventNotificationUI.transform, eventNotificationEnabled);
        BlackScreen.instance.screenFade.StartFade(eventNotificationText.transform, eventNotificationEnabled);
        eventNotificationText.text = string.Format(notificationText, CurrentTurnPlayerName);
    }

    public void ToggleEventNotification(bool enable)
    {
        eventNotificationEnabled = enable;

        BlackScreen.instance.screenFade.StartFade(eventNotificationUI.transform, eventNotificationEnabled);
        BlackScreen.instance.screenFade.StartFade(eventNotificationText.transform, eventNotificationEnabled);
    }

    public void UpdateRollDiceUI()
    {
        rollDiceUI.SetActive(true);
        string rollText = GameManager.instance.currentPlayerInfo.specialDiceEnabled.Value ? "uses a special item!" : "rolls...";
        rollDiceName.text = $"{CurrentTurnPlayerName} {rollText}";
    }

    public void UpdateLoadingPlayerUI(bool fadeIn, float speed)
    {
        AudioManager.instance.EnableLoading(fadeIn);
        AudioManager.instance.ChangeMusic(MusicType.BoardMusic);
        AudioManager.instance.ChangeMusicLayer(fadeIn ? MusicLayer.LightLayer : MusicLayer.MediumLayer);
        BlackScreen.instance.screenFade.StartFade(BlackScreen.instance.transform, fadeIn, speed);
    }

    public void ShowEndMenu()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= localParent.GetComponent<PlayerSetup>().ReturnToMenu;
        gameEndUI.GetComponentInChildren<Button>().onClick.AddListener(() => { DisconnectClient(); });
        SetupGameEndUI();
    }

    private void DisconnectClient()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
    }

    private void SetupGameEndUI()
    {
        string winnerName = "";
        // Niin ei tarvi tehä toisen foreach loop missä sä etit voittajan ja sen nimen (toinen tapa ois laittaa se RPCn kautta)
        int winningNumber = 0;
        string finalResults = "Final results:";

        foreach (var player in GameManager.instance.availablePlayers)
        {
            var playerParent = player.GetComponentInParent<MainPlayerInfo>();
            finalResults += $"\n{playerParent.playerName.Value} : [ Coins: {playerParent.coinAmount.Value}, Crowns: {playerParent.crownAmount.Value} ]";
            if (playerParent.crownAmount.Value > winningNumber)
            {
                winningNumber = playerParent.crownAmount.Value;
                winnerName = playerParent.playerName.Value.ToString();
            }
        }

        gameEndHeader.text = $"Winner: {winnerName}!";
        gameEndResults.text = finalResults;

        gameEndUI.SetActive(true);
    }
}
