using Unity.Netcode;
using UnityEngine;

public class ShopTile : BoardTile
{
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.shopTile.tileSprite;
        tileName = GameManager.instance.shopTile.name;
        minimapSprite = GameManager.instance.shopTile.minimapSprite;

        anim.Play("Shop_Idle");
    }
    public override void InvokeTile()
    {
        Debug.Log("This tile doesn't actually do anything on it's own...");
    }
}
