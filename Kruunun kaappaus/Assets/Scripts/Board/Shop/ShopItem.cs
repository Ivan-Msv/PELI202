using UnityEngine;

public abstract class ShopItem : ScriptableObject
{
    public string itemName;
    [TextArea]
    public string itemDescription;
    public int itemCost;
    public int itemId;
    public Sprite itemSprite;

    public void PurchaseItem(MainPlayerInfo player)
    {
        if (!IsEnoughCoins(player))
        {
            
            Debug.LogError("Not enough coins.");
            return;
        }
        AudioManager.PlaySound(SoundType.BoardSelect);
        player.coinAmount.Value -= itemCost;
        GiveItem(player);
    }

    public abstract void GiveItem(MainPlayerInfo player);

    public bool IsEnoughCoins(MainPlayerInfo player)
    {
        return player.coinAmount.Value >= itemCost;
    }
}
