using System.Collections.Generic;
using UnityEngine;

public class TeleportTile : BoardTile
{
    private Dictionary<GameObject, int> teleportTiles = new();

    public override void SetupTile()
    {
        tileSprite = GameManager.instance.teleportTile.tileSprite;
        tileName = GameManager.instance.teleportTile.name;
        minimapSprite = GameManager.instance.teleportTile.minimapSprite;

        GetAllTeleportTiles();
    }

    private void GetAllTeleportTiles()
    {
        // Gets all available teleports and stores them for later
        for (int i = 0; i < BoardPath.instance.tiles.Count; i++)
        {

        }
    }

    public override void InvokeTile()
    {
        throw new System.NotImplementedException();
    }
}
