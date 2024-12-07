using System.Collections;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BoardUIManager : MonoBehaviour
{
    public static BoardUIManager instance;
    public BoardPlayerInfo localPlayer;
    public MainPlayerInfo localParent;
    public bool onShopTile;
    public bool localPlayerTurn;

    [Header("UI")]
    public BoardShop shopUI;
    public GameObject gameEndUI;
    public GameObject rollDiceUI;
    public SpecialDiceUI diceUI;
    public BoardCamera virtualCamera;
    public Animator diceAnimator;

    [Header("Buttons")]
    [SerializeField] private Button rollButton;
    [SerializeField] private Button openStoreButton;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI coinCounter;
    [SerializeField] private TextMeshProUGUI crownCounter;
    [SerializeField] private TextMeshProUGUI playerTurn;
    [SerializeField] private TextMeshProUGUI playerTurnTimer;
    [SerializeField] private TextMeshProUGUI gameEndHeader;
    [SerializeField] private TextMeshProUGUI gameEndResults;
    [SerializeField] private TextMeshProUGUI rollDiceName;
    [SerializeField] private string currentTurnPlayerName;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        GameManager.instance.OnPlayerValueChange += UpdateLocalPlayers;
        GameManager.instance.OnCurrentPlayerChange += UpdateCurrentPlayerName;

        rollButton.onClick.AddListener(() => { GameManager.instance.RollDiceServerRpc(); });
        openStoreButton.onClick.AddListener(() => { shopUI.OpenStore(); });
    }

    private void OnDisable()
    {
        GameManager.instance.OnPlayerValueChange -= UpdateLocalPlayers;
        GameManager.instance.OnCurrentPlayerChange -= UpdateCurrentPlayerName;
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
        if (!LocalPlayerTurn() || GameManager.instance.currentState.Value == BoardState.PlayerMoving)
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

        playerTurn.text = $"{(LocalPlayerTurn() ? "Your" : currentTurnPlayerName) } turn";

        var timeInMinutes = Mathf.FloorToInt((GameManager.instance.TurnTimer.Value + 1) / 60);
        var timeInSeconds = Mathf.FloorToInt((GameManager.instance.TurnTimer.Value + 1) - timeInMinutes * 60);
        string timerText = string.Format("{0:00}:{1:00}", timeInMinutes, timeInSeconds);
        playerTurnTimer.text = timerText;
    }
    private void UpdateLocalPlayers()
    {
        localPlayer = GameManager.instance.availablePlayers.Find(player => player.OwnerClientId == NetworkManager.Singleton.LocalClientId);
        localParent = localPlayer.GetComponentInParent<MainPlayerInfo>();

        diceUI.AddEvent();
        CheckForGameEnd();
    }
    private void UpdateCurrentPlayerName(FixedString64Bytes newName)
    {
        currentTurnPlayerName = newName.ToString();
    }
    private bool LocalPlayerTurn()
    {
        return localPlayer == GameManager.instance.currentPlayer;
    }
    public bool LocalPlayerOnShopTile()
    {
        return BoardPath.instance.GetIndexTile(GameManager.instance.tilesIndex[localParent.currentBoardPosition.Value]) == GameManager.instance.shopTile;
    }

    private void CheckForGameEnd()
    {
        if (localParent.crownAmount.Value >= 5)
        {
            GameManager.instance.TriggerGameEndServerRpc();
        }
    }

    public void UpdateRollDiceUI()
    {
        rollDiceUI.SetActive(true);
        rollDiceName.text = $"{currentTurnPlayerName} rolls...";
    }

    public void ShowEndMenu()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= localParent.GetComponent<PlayerSetup>().ReturnToLobby;
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

        gameEndHeader.text = $"Winner: {winnerName} !";
        gameEndResults.text = finalResults;

        gameEndUI.SetActive(true);
    }
}
