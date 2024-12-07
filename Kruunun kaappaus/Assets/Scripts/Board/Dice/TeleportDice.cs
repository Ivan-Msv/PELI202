using UnityEngine;

[CreateAssetMenu(menuName = "Dice/TeleportDice")]
public class TeleportDice : BoardDice
{
    public override void SpecialAbility()
    {
        int givenRange = 0;
        switch (GameManager.instance.lastRolledNumber.Value)
        {
            case 1:
                givenRange = GetClosestTileRange(GameManager.instance.emptyTile);
                break;
            case 2:
                givenRange = GetClosestTileRange(GameManager.instance.shopTile);
                break;
            case 3:
                givenRange = GetClosestTileRange(GameManager.instance.minigameTile);
                break;
            case 4:
                givenRange = GetClosestTileRange(GameManager.instance.challengeTile);
                break;
        }

        GameManager.instance.playerMovement.MovePlayer(GameManager.instance.currentPlayer, givenRange);
    }

    private int GetClosestTileRange(BoardTile tile)
    {
        int playerPosition = GameManager.instance.currentPlayerInfo.currentBoardPosition.Value;
        int infront = 1;
        int behind = -1;
        int tries = 100;
        var targetTileType = tile.GetComponent<BoardTile>().GetType();
        while (true)
        {
            // Jotta peli ei crashais / freezais jos sitä tiletype ei oo vielä lisätty
            if (tries < 1)
            {
                Debug.LogError($"Did not find any {tile}");
                break;
            }
            // Check in front
            var frontTile = BoardPath.instance.tiles[(playerPosition + infront) % BoardPath.instance.tiles.Count].GetComponent<BoardTile>();
            // Check behind
            var backTile = BoardPath.instance.tiles[(playerPosition - behind + BoardPath.instance.tiles.Count) % BoardPath.instance.tiles.Count].GetComponent<BoardTile>();

            if (frontTile.GetType().Equals(targetTileType))
            {
                return infront;
            }

            if (backTile.GetType().Equals(targetTileType))
            {
                return behind;
            }

            infront++;
            behind--;
            tries--;
        }

        return 0;
    }
}
