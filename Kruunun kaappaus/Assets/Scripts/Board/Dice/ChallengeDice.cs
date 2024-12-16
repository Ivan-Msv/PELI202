using UnityEngine;

[CreateAssetMenu(menuName = "Dice/ChallengeDice")]
public class ChallengeDice : BoardDice
{
    // Uses challenge tile's function to select a custom challenge with the rolled number
    public override void SpecialAbility()
    {
        GameManager.instance.challengeTile.GetComponent<ChallengeTile>().SelectCustomChallenge(GameManager.instance.lastRolledNumber.Value);
        GameManager.instance.SkipTurnServerRpc(true);
    }
}
