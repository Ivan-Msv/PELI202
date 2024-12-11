using UnityEngine;

[CreateAssetMenu(menuName = "Dice/ChallengeDice")]
public class ChallengeDice : BoardDice
{
    public override void SpecialAbility()
    {
        GameManager.instance.challengeTile.GetComponent<ChallengeTile>().SelectCustomChallenge(GameManager.instance.lastRolledNumber.Value);
    }
}
