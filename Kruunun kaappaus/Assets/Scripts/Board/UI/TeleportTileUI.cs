using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TeleportTileUI : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private GameObject purchaseTab;
    [SerializeField] private GameObject selectionTab;
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI mainText;
    [SerializeField] private int gatewayCost;
    [Header("Buttons")]
    [SerializeField] private Button[] cancelButtons;
    [SerializeField] private Button confirmPurchaseButton;
    [SerializeField] private Button confirmGatewayButton;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    public List<int> teleportIndexList = new();
    private int selectedIndex;

    private string defaultText = "Would you like to spend {0} coins to instantly travel between selected gateways?";

    private void Awake()
    {
        // Button stuff
        foreach (var button in cancelButtons)
        {
            button.onClick.AddListener(() => { CancelGateway(); });
        }
        confirmPurchaseButton.onClick.AddListener(() => { BuyGatewayPass(); });
        confirmGatewayButton.onClick.AddListener(() => { ActivateGateway(); });
        leftArrowButton.onClick.AddListener(() => { ChooseTeleportIndex(-1); });
        rightArrowButton.onClick.AddListener(() => { ChooseTeleportIndex(1); });
    }

    public void GetTeleportTiles()
    {
        // Clears previous in case you debug randomize new tiles
        teleportIndexList.Clear();

        // Get all available teleport tiles, and add it to array to cycle through later
        for (int i = 0; i < GameManager.instance.tilesIndex.Count; i++)
        {
            var currentTileIndex = GameManager.instance.tilesIndex[i];

            if (currentTileIndex == (int)Tiles.TeleportTile)
            {
                teleportIndexList.Add(i);
            }
        }
    }

    public void StartUI(int teleportTileIndex)
    {
        // Find which teleport tile called, to properly get selected index

        for (int i = 0; i < teleportIndexList.Count; i++)
        {
            if (teleportIndexList[i] == teleportTileIndex)
            {
                selectedIndex = i;
            }
        }

        confirmPurchaseButton.interactable = BoardUIManager.instance.localParent.coinAmount.Value >= gatewayCost;
        mainText.text = string.Format(defaultText, gatewayCost);

        purchaseTab.SetActive(true);
    }

    private void BuyGatewayPass()
    {
        // If, for some insane reason you got to access this even though the button should be disabled, cancel it
        var player = BoardUIManager.instance.localParent;
        if (player.coinAmount.Value < gatewayCost)
        {
            Debug.LogError("Not enough coins to purchase");
            return;
        }

        player.coinAmount.Value -= gatewayCost;

        // You "could" add animation but I can't be asked rn
        purchaseTab.SetActive(false);
        selectionTab.SetActive(true);

        confirmGatewayButton.interactable = teleportIndexList[selectedIndex] != BoardUIManager.instance.localParent.currentBoardPosition.Value;

        GameManager.instance.EventNotificationTextRpc("{0} is selecting teleportation gateway...");
    }

    private void CancelGateway()
    {
        // If false, the purchase went through, so just refund the money
        if (!purchaseTab.activeSelf)
        {
            BoardUIManager.instance.localParent.coinAmount.Value += gatewayCost;
        }

        GameManager.instance.ChangeGameStateServerRpc(BoardState.SelectingPlayer);

        // Turn off both tabs just in case
        purchaseTab.SetActive(false);
        selectionTab.SetActive(false);

        BoardUIManager.instance.animationActive = false;

        GameManager.instance.ToggleEventNotificationRpc(false);
    }

    private void ActivateGateway()
    {
        BoardUIManager.instance.boardCamera.UpdateCameraFollowRpc();
        var currentTileIndex = BoardUIManager.instance.localParent.currentBoardPosition.Value;
        var selectedTileIndex = teleportIndexList[selectedIndex];
        BoardPath.instance.tiles[currentTileIndex].GetComponent<TeleportTile>().TeleportationEvent(selectedTileIndex);
        BoardUIManager.instance.localParent.currentBoardPosition.Value = selectedTileIndex;

        purchaseTab.SetActive(false);
        selectionTab.SetActive(false);

        GameManager.instance.ToggleEventNotificationRpc(false);
    }

    private void ChooseTeleportIndex(int indexForward)
    {
        selectedIndex = (selectedIndex + indexForward + teleportIndexList.Count) % teleportIndexList.Count;
        var nextIndex = teleportIndexList[selectedIndex];
        BoardUIManager.instance.boardCamera.TeleportTileCameraRpc(nextIndex);

        confirmGatewayButton.interactable = nextIndex != BoardUIManager.instance.localParent.currentBoardPosition.Value;
    }
}
