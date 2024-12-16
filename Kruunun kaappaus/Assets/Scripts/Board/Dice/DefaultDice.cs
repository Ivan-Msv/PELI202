using Unity.Multiplayer.Playmode;
using UnityEngine;

[CreateAssetMenu(menuName = "Dice/DefaultDice")]
public class DefaultDice : BoardDice
{
    // No special ability, moves player with the rolled number value
    public override void SpecialAbility()
    {
        GameManager.instance.playerMovement.MovePlayer(GameManager.instance.currentPlayer, GameManager.instance.lastRolledNumber.Value);
    }
}
