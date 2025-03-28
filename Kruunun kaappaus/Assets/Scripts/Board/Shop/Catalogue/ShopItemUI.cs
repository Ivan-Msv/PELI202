using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    public Image itemImage;
    public Button infoButton;
    public Button purchaseButton;
    public int cost;
    [SerializeField] private TextMeshProUGUI costText;

    private void Start()
    {
        costText.text = $"{cost} Coins";
    }

    private void Update()
    {
        bool canPurchase = BoardUIManager.instance.onShopTile && BoardUIManager.instance.localPlayerTurn && BoardUIManager.instance.localParent.coinAmount.Value >= cost;
        purchaseButton.interactable = canPurchase;
    }
}
