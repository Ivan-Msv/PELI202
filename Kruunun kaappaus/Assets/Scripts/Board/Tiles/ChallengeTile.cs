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
        var emptyTiles = GameManager.instance.currentPath.tiles.FindAll(tile => tile.GetComponent<EmptyTile>());

        int randomIndex = Random.Range(0, emptyTiles.Count);
        var newIndex = GameManager.instance.currentPath.tiles.IndexOf(emptyTiles[randomIndex]);

        // Käytetään array jotta ei tulis monta ServerRpc syötettä pienessä ajassa.
        int[] indexArray = { transform.GetSiblingIndex(), newIndex };

        // Käytin enum jotta olisi helpompi ymmärtää, numerot riittää
        int[] tileIndexArray = { (int)Tiles.EmptyTile, (int)Tiles.ChallengeTile };

        GameManager.instance.currentPath.ChangeTileIndexServerRpc(indexArray, tileIndexArray);
    }
}
