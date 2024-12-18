using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Dice/FriendDice")]
public class FriendDice : BoardDice
{
    // Rolls numbers 1 and 2,
    // searching for closest player either in front or behind,
    // judging by the number rolled
    public override void SpecialAbility()
    {
        int newPosition = 0;
        switch (GameManager.instance.lastRolledNumber.Value)
        {
            case 1:
                newPosition = GetClosestPlayerTile(true);
                break;
            case 2:
                newPosition = GetClosestPlayerTile(false);
                break;
        }

        GameManager.instance.playerMovement.MovePlayer(GameManager.instance.currentPlayer, newPosition);
    }

    // The function that actually does the searching, taking every player position value and searching for the smallest difference in them
    private int GetClosestPlayerTile(bool forward)
    {
        List<int> positions = new();
        int currentPosition = GameManager.instance.currentPlayerInfo.currentBoardPosition.Value;
        foreach (var player in GameManager.instance.availablePlayers)
        {
            if (player == GameManager.instance.currentPlayer)
            {
                continue;
            }

            var info = player.GetComponentInParent<MainPlayerInfo>();

            if (forward ? info.currentBoardPosition.Value > currentPosition : info.currentBoardPosition.Value < currentPosition)
            {
                positions.Add(info.currentBoardPosition.Value);
            }
        }

        if (positions.Count < 1)
        {
            return 0;
        }

        return positions.Min() - currentPosition;
    }
}
