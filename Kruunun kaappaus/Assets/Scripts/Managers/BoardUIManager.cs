using System.Collections;
using System.Linq;
using TMPro;
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


    private void Start()
    {
        StartCoroutine(LateInit());
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

        playerTurn.text = $"{(LocalPlayerTurn() ? "Your" : GameManager.instance.currentPlayer) } turn: {(int)GameManager.instance.TurnTimer} sec left";
    }

    private IEnumerator LateInit()
    {
        // jatka tätä
        yield return new WaitForEndOfFrame();
        localPlayer = GameManager.instance.availablePlayers.FirstOrDefault(player => player.NetworkObject.OwnerClientId == NetworkManager.Singleton.LocalClientId);
        localParent = localPlayer.GetComponentInParent<MainPlayerInfo>();
    }

    private bool LocalPlayerTurn()
    {
        return localPlayer == GameManager.instance.currentPlayer;
    }
}
