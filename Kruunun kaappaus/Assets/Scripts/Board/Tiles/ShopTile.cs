using Unity.Netcode;
using UnityEngine;

public class ShopTile : BoardTile
{
    [SerializeField] private GameObject ShopUI;
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.shopTile.tileSprite;
        tileName = GameManager.instance.shopTile.name;
    }
    public override void InvokeTile()
    {
        ShopUI.SetActive(true);
    }
}
