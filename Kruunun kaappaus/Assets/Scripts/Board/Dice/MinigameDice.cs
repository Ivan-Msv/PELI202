using UnityEngine;

[CreateAssetMenu(menuName = "Dice/MinigameDice")]
public class MinigameDice : BoardDice
{
    public override void SpecialAbility()
    {
        GameManager.instance.minigameTile.GetComponent<MinigameTile>().SelectCustomMinigame(GameManager.instance.lastRolledNumber.Value);
        GameManager.instance.SkipTurnServerRpc(true);
    }
}
