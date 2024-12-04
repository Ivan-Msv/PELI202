using UnityEngine;


[CreateAssetMenu(menuName = "ShopItem/Dice")]
public class DiceShopItem : ShopItem
{
    public override void GiveItem(MainPlayerInfo player)
    {
        if (player.specialDiceIndex.Value == itemId)
        {
            player.coinAmount.Value += itemCost;
            Debug.LogError("Already has the same dice");
            return;
        }
        player.specialDiceIndex.Value = itemId;
    }
}
