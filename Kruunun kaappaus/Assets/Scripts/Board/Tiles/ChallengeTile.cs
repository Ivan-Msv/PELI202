using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ChallengeTile : BoardTile
{
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.challengeTile.tileSprite;
        tileName = GameManager.instance.challengeTile.name;
    }

    public override void InvokeTile()
    {
        var emptyTiles = BoardPath.instance.tiles.FindAll(tile => tile.GetComponent<EmptyTile>());

        int randomIndex = Random.Range(0, emptyTiles.Count);
        var newIndex = BoardPath.instance.tiles.IndexOf(emptyTiles[randomIndex]);

        BoardPath.instance.ChangeTileIndexServerRpc(transform.GetSiblingIndex(), (int)Tiles.EmptyTile);
        BoardPath.instance.ChangeTileIndexServerRpc(newIndex, (int)Tiles.ChallengeTile);

        // Change later to challegne selection
        GameManager.instance.LoadSceneServerRpc("Level MiniGame1");
    }
}
