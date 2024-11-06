using UnityEngine;

public class ShopTile : BoardTile
{
    [SerializeField] private GameObject ShopUI;
    public override void InvokeTile()
    {
        ShopUI.SetActive(true);
    }
}
