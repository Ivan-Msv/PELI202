using UnityEngine;

public class TeleportTileAnimation : TimedAnimation
{
    private BoardPlayerInfo playerToTeleport;
    public TeleportTile teleportedFromTile;
    public TeleportTile teleportedToTile;

    public void SetupAnimation(BoardPlayerInfo player, TeleportTile currentTile, TeleportTile nextTile)
    {
        playerToTeleport = player;
        teleportedFromTile = currentTile;
        teleportedToTile = nextTile;
    }

    // Used in animation "Teleport_Tile_Activate_SFX"
    private void TeleportationEvent()
    {
        // The object spawns 2 of these "sfx", so to prevent errors and double teleports, return here
        if (playerToTeleport == null)
        {
            return;
        }

        playerToTeleport.UpdatePlayerPositionClientRpc(teleportedToTile.transform.position);
        BoardPath.instance.SplitPlayersRpc();
    }

    // Also used in animation "Teleport_Tile_Activate_SFX"
    private void DeactivationEvent()
    {
        if (teleportedFromTile == null) { return; }

        teleportedFromTile.PlayAnimationRpc("Teleport_Tile_Deactivate");
        teleportedToTile.PlayAnimationRpc("Teleport_Tile_Deactivate");

        GameManager.instance.ChangeGameStateServerRpc(BoardState.SelectingPlayer);
    }
}
