using Unity.Multiplayer.Playmode;
using UnityEngine;

[CreateAssetMenu(menuName = "Dice/DefaultDice")]
public class DefaultDice : BoardDice
{
    public override void SpecialAbility()
    {
        GameManager.instance.playerMovement.MovePlayer(GameManager.instance.currentPlayer, GameManager.instance.lastRolledNumber.Value);
    }
}
