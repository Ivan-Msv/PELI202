using System.Collections;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
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
    public Animator diceAnimator;

    [Header("Buttons")]
    [SerializeField] private Button rollButton;
    [SerializeField] private Button openStoreButton;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI coinCounter;
    [SerializeField] private TextMeshProUGUI crownCounter;
    [SerializeField] private TextMeshProUGUI playerTurn;
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
        if (!LocalPlayerTurn())
        {
            rollButton.interactable = false;
            return;
        }

        rollButton.interactable = true;
    }

    private void UpdateText()
    {
        coinCounter.text = $"Coins: {localParent.coinAmount.Value}";
        crownCounter.text = $"Crowns: {localParent.crownAmount.Value}";

        playerTurn.text = $"{(LocalPlayerTurn() ? "Your" : currentTurnPlayerName) } turn: {(int)GameManager.instance.TurnTimer.Value + 1} sec left";
    }
    private void UpdateLocalPlayers()
    {
        localPlayer = GameManager.instance.availablePlayers.Find(player => player.OwnerClientId == NetworkManager.Singleton.LocalClientId);
        localParent = localPlayer.GetComponentInParent<MainPlayerInfo>();
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
}
