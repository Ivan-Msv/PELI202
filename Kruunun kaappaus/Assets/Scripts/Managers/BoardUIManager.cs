using System.Collections;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BoardUIManager : MonoBehaviour
{
    [SerializeField] private BoardPlayerInfo localPlayer;
    [SerializeField] private MainPlayerInfo localParent;

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
    }

    private void UpdateButtons()
    {
        if (!LocalPlayerTurn())
        {
            rollButton.interactable = false;
            openStoreButton.interactable = false;
            return;
        }

        rollButton.interactable = true;
        openStoreButton.interactable = true;
    }

    private void UpdateText()
    {
        coinCounter.text = $"Coins: {localParent.coinAmount.Value}";
        crownCounter.text = $"Crowns: {localParent.crownAmount.Value}";

        playerTurn.text = $"{(LocalPlayerTurn() ? "Your" : currentTurnPlayerName) } turn: {(int)GameManager.instance.TurnTimer} sec left";
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
}
