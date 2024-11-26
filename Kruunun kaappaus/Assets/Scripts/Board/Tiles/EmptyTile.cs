using Unity.Netcode;
using UnityEngine;

public class EmptyTile : BoardTile
{
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.emptyTile.tileSprite;
        tileName = GameManager.instance.emptyTile.name;
    }
    public override void InvokeTile()
    {
        Debug.Log("Empty Tile Invoked");
    }
}
