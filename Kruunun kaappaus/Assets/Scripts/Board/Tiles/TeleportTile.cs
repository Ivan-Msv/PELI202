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

        PlayAnimationRpc("Teleport_Tile_Activate");
        selectedTeleportTile.PlayAnimationRpc("Teleport_Tile_Activate");
    }

    // This gets used in "Teleport_Tile_Activate"
    private void AnimationEvent()
    {
        if (selectedTeleportTile == null) { return; }

        InstantiateSFXRpc(selectedTeleportTile.transform.position);
        InstantiateSFXRpc(transform.position);
    }

    [Rpc(SendTo.Everyone)]
    private void InstantiateSFXRpc(Vector2 position)
    {
        var sfx = Instantiate(teleportTileSFX, position, teleportTileSFX.transform.rotation);

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
