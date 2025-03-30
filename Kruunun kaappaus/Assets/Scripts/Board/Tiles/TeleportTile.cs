using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeleportTile : BoardTile
{
    [SerializeField] private GameObject teleportTileSFX;
    private TeleportTile selectedTeleportTile;
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.teleportTile.tileSprite;
        tileName = GameManager.instance.teleportTile.name;
        minimapSprite = GameManager.instance.teleportTile.minimapSprite;
        teleportTileSFX = GameManager.instance.teleportTile.GetComponent<TeleportTile>().teleportTileSFX;


        anim.Play("TeleportTile_Idle");
    }

    public void TeleportationEvent(int teleportTowardTile)
    {
        selectedTeleportTile = BoardPath.instance.tiles[teleportTowardTile].GetComponent<TeleportTile>();

        BoardPath.instance.PlayTileAnimationRpc(GetTilePosition(), "Teleport_Tile_Activate");
        BoardPath.instance.PlayTileAnimationRpc(selectedTeleportTile.GetTilePosition(), "Teleport_Tile_Activate");
    }

    // This gets used in "Teleport_Tile_Activate"
    private void AnimationEvent()
    {
        InstantiateSFX();
    }

    private void InstantiateSFX()
    {
        // Will instantiate on transform.position
        var sfx = Instantiate(teleportTileSFX, transform.position, teleportTileSFX.transform.rotation);

        if (selectedTeleportTile != null)
        {
            sfx.GetComponent<TeleportTileAnimation>().SetupAnimation(BoardUIManager.instance.localPlayer, this, selectedTeleportTile);

            // also reset the tile to prevent weird animation issues
            selectedTeleportTile = null;
        }
    }

    public override void InvokeTile()
    {
        BoardUIManager.instance.teleportTileUI.StartUI(BoardUIManager.instance.localParent.currentBoardPosition.Value);
    }
}
