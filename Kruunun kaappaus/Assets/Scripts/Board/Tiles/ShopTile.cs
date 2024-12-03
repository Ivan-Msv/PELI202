using Unity.Netcode;
using UnityEngine;

public class ShopTile : BoardTile
{
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.shopTile.tileSprite;
        tileName = GameManager.instance.shopTile.name;
    }
    public override void InvokeTile()
    {
        Debug.Log("Shop tile invoked!");
    }
}
