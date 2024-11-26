using Unity.Netcode;
using UnityEngine;

public class MinigameTile : BoardTile
{
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.minigameTile.tileSprite;
        tileName = GameManager.instance.minigameTile.name;
    }
    public override void InvokeTile()
    {
        Debug.Log($"Minigame tile invoked!");
    }
}
